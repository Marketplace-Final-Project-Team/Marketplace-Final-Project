using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public ActivityLogService(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task LogUserActionAsync(string userId, string action, string description, string? actorUserId = null, Guid? relatedEntityId = null)
        {
            var user = await _userManager.FindByIdAsync(userId);

            _context.UserActionLogs.Add(new UserActionLog
            {
                UserId = userId,
                UserEmail = user?.Email ?? string.Empty,
                ActorUserId = actorUserId,
                Action = action,
                Description = description,
                RelatedEntityId = relatedEntityId
            });

            await _context.SaveChangesAsync();
        }

        public async Task LogBusinessActionAsync(Guid businessId, string action, string description, string? actorUserId = null, Guid? relatedEntityId = null)
        {
            var businessName = await _context.Businesses
                .Where(b => b.Id == businessId)
                .Select(b => b.StoreName)
                .FirstOrDefaultAsync();

            _context.BusinessActionLogs.Add(new BusinessActionLog
            {
                BusinessId = businessId,
                BusinessName = businessName ?? string.Empty,
                ActorUserId = actorUserId,
                Action = action,
                Description = description,
                RelatedEntityId = relatedEntityId
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<UserActionLog>> GetRecentUserActionsAsync(int take = 200)
        {
            return await _context.UserActionLogs
                .AsNoTracking()
                .OrderByDescending(log => log.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<BusinessActionLog>> GetRecentBusinessActionsAsync(int take = 200)
        {
            return await _context.BusinessActionLogs
                .AsNoTracking()
                .OrderByDescending(log => log.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<BusinessActionLog>> GetRecentBusinessActionsAsync(Guid businessId, int take = 15)
        {
            return await _context.BusinessActionLogs
                .AsNoTracking()
                .Where(log => log.BusinessId == businessId)
                .OrderByDescending(log => log.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}
