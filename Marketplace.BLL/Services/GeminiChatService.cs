using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Marketplace.BLL.DTOs.Gemini;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Marketplace.BLL.Services
{
    public class GeminiChatService: IGeminiChatService
    {
        private const int MaxHistoryMessages = 20;
        private const int MaxFunctionCallItterations = 5;


        private const string SystemInstruction =
            """
            Ти - асистент з пошуку товарів на маркетплейсі. Допомагай користувачу знаходити товари,
            порівнювати їх, перевіряти наявність і знижки, а також додавати/видаляти товари з кошика.

            Категорії товарів у базі даних зберігаються англійською мовою. Якщо користувач пише категорію
            українською (наприклад, "навушники", "одяг"), перед пошуком спочатку виклич get_categories,
            щоб отримати список реальних категорій, і підбери найбільш відповідну англійську назву.
            Не вигадуй назву категорії, якої немає у отриманому списку.

            Перед додаванням або видаленням товару з кошика (add_to_cart, remove_from_cart) завжди
            запитуй у користувача явне підтвердження.

            Відповідай той самою мовою, якою до тебе звертається користувач, дружньо й лаконічно.
            """;

        private readonly IGeminiChatApiClient _apiClient;
        private readonly ChatToolDispatcher _dispatcher;
        private readonly IGenericRepository<AIChatMessage> _chatMessageRepo;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<GeminiChatService> _logger;

        public GeminiChatService(
            IGeminiChatApiClient apiClient,
            ChatToolDispatcher dispatcher,
            IGenericRepository<AIChatMessage> chatMessageRepo,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GeminiChatService> logger,
            UserManager<User> userManager)
        {
            _apiClient = apiClient;
            _dispatcher = dispatcher;
            _context = context;
            _chatMessageRepo = chatMessageRepo;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<string> SendMessageAsync(string userMessage)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            if (userId == null) throw new UnauthorizedAccessException("Authorization is needed to use AI chat");

            _logger.LogWarning("DEBUG: userId from session = {UserId}", userId);

            var userChatMessage = new AIChatMessage
            {
                Role = "user",
                UserId = userId,
                Text = userMessage,
            };
            await _chatMessageRepo.AddAsync(userChatMessage);
            await _context.SaveChangesAsync();

            var historyMessages = await _chatMessageRepo.GetQueryable()
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(MaxHistoryMessages)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var contents = historyMessages.Select(BuildGeminiContent).ToList();

            for (int iteration = 0; iteration < MaxFunctionCallItterations; iteration++)
            {
                var response = await _apiClient.GenerateContentAsync(contents, GeminiToolDeclarations.GetAll(), SystemInstruction);
                var candidate = response.Candidates?.FirstOrDefault();
                var part = candidate?.Content?.Parts?.FirstOrDefault();

                if (part?.FunctionCall != null)
                {
                    var functionCall = part.FunctionCall;
                    var thoughtSignature = part.ThoughtSignature;

                    var modelCallMessage = new AIChatMessage
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Role = "model",
                        FunctionName = functionCall.Name,
                        FunctionArgsJson = functionCall.Args != null ? JsonSerializer.Serialize(functionCall.Args) : null,
                        ThoughtSignature = thoughtSignature,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _chatMessageRepo.AddAsync(modelCallMessage);

                    var modelContent = new GeminiContent
                    {
                        Role = "model",
                        Parts = new List<GeminiPart> { part }
                    };

                    contents.Add(modelContent);

                    object functionResult;
                    try
                    {
                        functionResult = await _dispatcher.ExecuteAsync(functionCall.Name, functionCall.Args);
                    }
                    catch (Exception ex)
                    {
                        functionResult = new { error = ex.Message };
                    }

                    functionResult = WrapForGemini(functionResult);
                    var functionResultJson = JsonSerializer.Serialize(functionResult);

                    var functionResultMessage = new AIChatMessage
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Role = "function",
                        FunctionName = functionCall.Name,
                        FunctionResponseJson = functionResultJson,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _chatMessageRepo.AddAsync(functionResultMessage);

                    var functionContent = new GeminiContent
                    {
                        Role = "user",
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart
                            {
                                FunctionResponse = new GeminiFunctionResponse
                                {
                                    Name = functionCall.Name,
                                    Response = functionResult
                                }
                            }
                        }
                    };
                    contents.Add(functionContent);

                    await _context.SaveChangesAsync();
                    continue;
                }

                var responseText = part?.Text ?? "We are sorry, but there are no response.";

                var modelTextMessage = new AIChatMessage
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Role = "model",
                    Text = responseText,
                    CreatedAt = DateTime.UtcNow
                };
                await _chatMessageRepo.AddAsync(modelTextMessage);

                await _context.SaveChangesAsync();
                return responseText;
            }

            return "Sorry, we couldn't process your request - too many steps. Please try rephrasing your question.";
        }

        private static GeminiContent BuildGeminiContent(AIChatMessage message)
        {
            var part = new GeminiPart();
            string role = message.Role;

            if (message.FunctionResponseJson != null)
            {
                part.FunctionResponse = new GeminiFunctionResponse
                {
                    Name = message.FunctionName!,
                    Response = JsonSerializer.Deserialize<object>(message.FunctionResponseJson)!
                };
                role = "user";
            }
            else if (message.FunctionArgsJson != null || (message.Role == "model" && message.FunctionName != null))
            {
                part.FunctionCall = new GeminiFunctionCall
                {
                    Name = message.FunctionName!,
                    Args = message.FunctionArgsJson != null
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(message.FunctionArgsJson)
                        : null
                };
                part.ThoughtSignature = message.ThoughtSignature;
            }
            else
            {
                part.Text = message.Text;
            }

            return new GeminiContent
            {
                Role = role,
                Parts = new List<GeminiPart> { part }
            };
        }

        private static object WrapForGemini(object result)
        {
            if (result is System.Collections.IEnumerable && result is not string)
            {
                return new { result };
            }
            return result;
        }
    }
}
