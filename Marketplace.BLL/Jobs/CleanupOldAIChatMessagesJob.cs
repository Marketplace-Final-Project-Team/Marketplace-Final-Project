using Marketplace.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Services
{
    public class CleanupOldAIChatMessagesJob: IJob
    {
        private const int RetentionDays = 30;

        private readonly AppDbContext _context;
        private readonly ILogger<CleanupOldAIChatMessagesJob> _logger;

        public CleanupOldAIChatMessagesJob(
            AppDbContext context,
            ILogger<CleanupOldAIChatMessagesJob> logger
            )
        {
            _context = context;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context) 
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);

            var deletedCount = await _context.AIChatMessages.
                Where(m => m.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync();


            if (deletedCount > 0) 
            {
                _logger.LogInformation("Було видалено {Count} AI-чату старших за {Days} днів", deletedCount, RetentionDays);
            }
        }

    }
}
