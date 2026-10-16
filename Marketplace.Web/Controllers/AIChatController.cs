using Marketplace.BLL.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    [Route("api/chat")]
    [ApiController]
    public class AIChatController : Controller
    {
        private readonly IGeminiChatService _chatService;
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(IGeminiChatService chatService, ILogger<AIChatController> logger) 
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request) 
        {
            if(string.IsNullOrEmpty(request.Message)) 
            {
                return BadRequest(new { error = "Message is required" });
            }

            try 
            {
                var responseText = await _chatService.SendMessageAsync(request.Message);
                return Ok(new { message = responseText });
            }
            catch(UnauthorizedAccessException) 
            {
                return Unauthorized();
            }
            catch(Exception ex) 
            {
                _logger.LogError(ex, "Error while processing chat message");
                return StatusCode(500, new { error = "Something went wrong. Please try again." });
            }
        }

    }
}
