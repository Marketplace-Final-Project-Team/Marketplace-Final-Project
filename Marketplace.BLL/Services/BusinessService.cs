using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Quartz;

namespace Marketplace.BLL.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly IGenericRepository<BusinessMember> _businessMemberRepo;
        private readonly IBusinessRepository _businessRepo;
        private readonly IImageStorageService _imageService;
        private readonly UserManager<User> _userManager;
        private readonly IOrderRepository _orderRepository;
        private readonly AppDbContext _context;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IActivityLogService _activityLog;

        private DateTime Today = DateTime.UtcNow.Date;

        public BusinessService(
            IGenericRepository<BusinessMember> businessMemberRepo,
            IBusinessRepository businessRepo,
            IImageStorageService imageService,
            UserManager<User> userManager,
            IOrderRepository orderRepository,
            AppDbContext context,
            ISchedulerFactory schedulerFactory,
            IActivityLogService activityLog)
        {
            _businessMemberRepo = businessMemberRepo;
            _businessRepo = businessRepo;
            _imageService = imageService;
            _userManager = userManager;
            _orderRepository = orderRepository;
            _context = context;
            _schedulerFactory = schedulerFactory;
            _activityLog = activityLog;
        }

        private async Task ScheduleModerationJobAsync(Guid businessId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey($"business-moderation-job-{businessId}", "moderation");

            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.DeleteJob(jobKey);
            }

            var jobData = new JobDataMap
            {
                { "BusinessId", businessId }
            };

            var job = JobBuilder.Create<BusinessModerationJob>()
                .WithIdentity(jobKey)
                .UsingJobData(jobData)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"business-moderation-trigger-{businessId}", "moderation")
                .StartAt(DateBuilder.FutureDate(5, IntervalUnit.Second))
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        private async Task<DataResult<(DateTime startDate, DateTime endDate)>> ValidateStatsRequestAsync(Guid businessId, DateTime? startDate, DateTime? endDate)
        {
            if ((await _businessRepo.GetByIdAsync(businessId)) == null)
            {
                return new DataResult<(DateTime startDate, DateTime endDate)>
                {
                    Message = "Бізнес не знайдено",
                    Succeeded = false,
                };
            }

            var actualEndDate = endDate ?? Today;
            if (endDate.HasValue && actualEndDate.Date > DateTime.UtcNow.Date)
            {
                return new DataResult<(DateTime startDate, DateTime endDate)>
                {
                    Message = "Кінцева дата знаходиться у майбутньому",
                    Succeeded = false
                };
            }

            var actualStartDate = startDate ?? Today.AddDays(-7);
            if (startDate.HasValue && actualStartDate > actualEndDate)
            {
                return new DataResult<(DateTime startDate, DateTime endDate)>
                {
                    Message = "Початкова дата більша за кінцеву",
                    Succeeded = false
                };
            }

            return new DataResult<(DateTime startDate, DateTime endDate)>
            {
                Message = string.Empty,
                Succeeded = true,
                Data = (actualStartDate, actualEndDate)
            };
        }

        private async Task<(ServiceResult, BusinessMember?)> VerifyMembershipRequest(string userId, Guid businessId, string token, BusinessMemberRole role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (new ServiceResult { Succeeded = false, Message = "Користувача не знайдено" }, null);
            }

            var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, $"role-confirm-{role}", token);
            if (!isValid)
            {
                return (new ServiceResult { Succeeded = false, Message = "Невірний токен" }, null);
            }

            var businessMember = await _businessMemberRepo.FirstOrDefaultAsync(bm => bm.UserId == userId && bm.BusinessId == businessId && bm.Role == role);
            if (businessMember == null)
            {
                return (new ServiceResult { Succeeded = false, Message = "Запит на членство не знайдено" }, null);
            }

            return (new ServiceResult { Succeeded = true, Message = string.Empty }, businessMember);
        }

        public async Task<ServiceResult> CreateBusiness(BusinessCreateDto dto)
        {
            if (await _businessRepo.ExistsAsync(b => b.OwnerId == dto.UserId))
            {
                return new ServiceResult { Succeeded = false, Message = "Ви вже маєте бізнес на своєму аккауті" };
            }

            if (await _businessRepo.ExistsAsync(b => b.StoreName == dto.StoreName))
            {
                return new ServiceResult { Succeeded = false, Message = "Це ім'я вже використовується" };
            }

            var logoUrl = await _imageService.UploadImageAsync(dto.Logo);
            if (logoUrl == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Не вдалося завантажити логотип" };
            }

            var bannerUrl = await _imageService.UploadImageAsync(dto.Banner);
            if (bannerUrl == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Не вдалося завантажити банер" };
            }

            var business = new Business
            {
                OwnerId = dto.UserId,
                StoreName = dto.StoreName,
                Description = dto.Description,
                LogoUrl = logoUrl,
                BannerUrl = bannerUrl
            };

            await _businessRepo.AddAsync(business);
            await _context.SaveChangesAsync();

            await ScheduleModerationJobAsync(business.Id);

            await _activityLog.LogBusinessActionAsync(business.Id, "BusinessCreated",
                $"Бізнес «{business.StoreName}» створено та відправлено на перевірку.",
                actorUserId: business.OwnerId);

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<BusinessMemberResult> AddBusinessMember(BusinessMemberCreateDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null)
            {
                return new BusinessMemberResult { Succeeded = false, Message = "Користувача не знайдено" };
            }

            if (await _businessMemberRepo.ExistsAsync(bm => bm.UserId == dto.UserId && bm.BusinessId == dto.BusinessId))
            {
                return new BusinessMemberResult { Succeeded = false, Message = "Користувач вже є членом цього бізнесу" };
            }

            var businessMember = new BusinessMember
            {
                UserId = dto.UserId,
                BusinessId = dto.BusinessId,
                Role = dto.Role
            };

            await _businessMemberRepo.AddAsync(businessMember);
            await _context.SaveChangesAsync();

            var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, $"role-confirm-{dto.Role}");

            return new BusinessMemberResult { Succeeded = true, Message = string.Empty, UserId = user.Id, Email = user.Email, Token = token, Role = dto.Role };
        }

        public async Task<ServiceResult> ApproveMembership(string userId, Guid businessId, string token, BusinessMemberRole role)
        {
            var (verificationResult, businessMember) = await VerifyMembershipRequest(userId, businessId, token, role);

            if (verificationResult.Succeeded == false)
            {
                return verificationResult;
            }

            businessMember!.IsAccepted = true;
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Запит прийнято" };
        }

        public async Task<ServiceResult> DenyMembership(string userId, Guid businessId, string token, BusinessMemberRole role)
        {
            var (verificationResult, businessMember) = await VerifyMembershipRequest(userId, businessId, token, role);

            if (verificationResult.Succeeded == false)
            {
                return verificationResult;
            }

            _businessMemberRepo.Remove(businessMember!);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Запит відхилено" };
        }


        public async Task<DataResult<BusinessStatsByPeriodDto>> GetBusinessStatsByPeriodAsync(Guid businessId, DateTime? startDate, DateTime? endDate)
        {
            var validationResult = await ValidateStatsRequestAsync(businessId, startDate, endDate);

            if (validationResult.Succeeded == false)
            {
                return new DataResult<BusinessStatsByPeriodDto>
                {
                    Message = validationResult.Message,
                    Succeeded = false,
                };
            }

            var (actualStartDate, actualEndDate) = validationResult.Data;
            var stats = await _orderRepository.GetBusinessStatByPeriodAsync(businessId, actualStartDate, actualEndDate);

            var totalRevenue = stats.Sum(s => s.Revenue);
            var totalOrders = stats.Sum(s => s.OrderCount);
            var aov = totalOrders > 0 ? (float)totalRevenue / totalOrders : 0f;

            return new DataResult<BusinessStatsByPeriodDto>
            {
                Message = string.Empty,
                Succeeded = true,
                Data = new BusinessStatsByPeriodDto
                {
                    StartDate = actualStartDate,
                    EndDate = actualEndDate,
                    Stats = stats,
                    TotalRevenue = totalRevenue,
                    AOV = aov
                }
            };
        }

        public async Task<DataResult<ProductsStatsByPeriodDto>> GetProductsStatsByPeriodAsnyc(Guid businessId, DateTime? startDate, DateTime? endDate, bool sortDescending = true)
        {
            var validationResult = await ValidateStatsRequestAsync(businessId, startDate, endDate);

            if (validationResult.Succeeded == false)
            {
                return new DataResult<ProductsStatsByPeriodDto>
                {
                    Message = validationResult.Message,
                    Succeeded = false,
                };
            }

            var (actualStartDate, actualEndDate) = validationResult.Data;
            var stats = await _orderRepository.GetProductsStatsByPeriodAsync(businessId, actualStartDate, actualEndDate);
            stats = sortDescending ? stats.OrderByDescending(s => s.Revenue).ToList() : stats.OrderBy(s => s.Revenue).ToList();

            return new DataResult<ProductsStatsByPeriodDto>
            {
                Message = string.Empty,
                Succeeded = true,
                Data = new ProductsStatsByPeriodDto
                {
                    StartDate = actualStartDate,
                    EndDate = actualEndDate,
                    Stats = stats
                }
            };
        }

        public async Task<DataResult<CustomersStatsByPeriodDto>> GetCustomersStatsByPeriodAsync(Guid businessId, DateTime? startDate, DateTime? endDate, bool sortDescending = true)
        {
            var validationResult = await ValidateStatsRequestAsync(businessId, startDate, endDate);

            if (validationResult.Succeeded == false)
            {
                return new DataResult<CustomersStatsByPeriodDto>
                {
                    Message = validationResult.Message,
                    Succeeded = false,
                };
            }

            var (actualStartDate, actualEndDate) = validationResult.Data;
            var stats = await _orderRepository.GetCustomersStatsByPeriodAsync(businessId, actualStartDate, actualEndDate);
            stats = sortDescending ? stats.OrderByDescending(s => s.Revenue).ToList() : stats.OrderBy(s => s.Revenue).ToList();

            return new DataResult<CustomersStatsByPeriodDto>
            {
                Message = string.Empty,
                Succeeded = true,
                Data = new CustomersStatsByPeriodDto
                {
                    StartDate = actualStartDate,
                    EndDate = actualEndDate,
                    Stats = stats,
                    UniqueCustomerCount = stats.Count
                }
            };
        }

        public async Task<ServiceResult> RequestAdultStatusAsync(Guid businessId, string userId)
        {
            var business = await _businessRepo.GetByIdAsync(businessId);
            if (business == null)
                return new ServiceResult { Succeeded = false, Message = "Магазин не знайдено" };

            var isOwner = business.OwnerId == userId;
            if (!isOwner)
                return new ServiceResult { Succeeded = false, Message = "Немає прав для здійснення цієї операції" };

            business.AdultStatus = Marketplace.Domain.Entities.AdultContentStatus.Pending;
            business.AdultRejectionReason = null;

            _businessRepo.Update(business);
            await _context.SaveChangesAsync();
            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<decimal> GetBusinessBalance(Guid businessid)
        {

            var business = await _businessRepo.GetByIdAsync(businessid);
            if (business == null)
            {
                return 0;
            }
            return business.Balance;
        }

        public async Task<Business> GetByIdAsync(Guid id)
        {
            var business = await _businessRepo.GetByIdAsync(id);
            if (business == null)
            {
                throw new Exception("Business not found");
            }
            return business;
        }
    }
}