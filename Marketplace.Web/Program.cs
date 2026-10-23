using Amazon.S3;
using CloudinaryDotNet;
using Marketplace.BLL.Common;
using Marketplace.BLL.Configs;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Jobs;
using Marketplace.BLL.Services;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.DAL.Repositories;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Globalization;
using Marketplace.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(Marketplace.Web.SharedResource)));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHealthChecks();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//S3
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();
}
else
{
    builder.Services.AddScoped<IImageStorageService, S3ImageStorageService>();
}

// Quartz
builder.Services.AddQuartz(q =>
{
    var pendingOrderExpiryJobKey = new JobKey("pending-order-expiry-job", "cleanup");
    q.AddJob<PendingOrderExpiryJob>(opts => opts.WithIdentity(pendingOrderExpiryJobKey));
    q.AddTrigger(opts => opts
        .ForJob(pendingOrderExpiryJobKey)
        .WithIdentity("pending-order-expiry-trigger", "cleanup")
        .StartNow()
        .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever()));

    var scheduledPayoutJobKey = new JobKey("scheduled-payout-job", "cleanup");
    q.AddJob<ScheduledPayoutJob>(opts => opts.WithIdentity(scheduledPayoutJobKey));
    q.AddTrigger(opts => opts
        .ForJob(scheduledPayoutJobKey)
        .WithIdentity("scheduled-payout-trigger", "cleanup")
        .StartNow()
        .WithSimpleSchedule(x => x.WithIntervalInHours(24).RepeatForever()));

    var releaseHeldBalanceJobKey = new JobKey("release-held-balance-job", "cleanup");
    q.AddJob<ReleaseHeldBalanceJob>(opts => opts.WithIdentity(releaseHeldBalanceJobKey));
    q.AddTrigger(opts => opts
        .ForJob(releaseHeldBalanceJobKey)
        .WithIdentity("release-held-balance-trigger", "cleanup")
        .StartNow()
        .WithSimpleSchedule(x => x.WithIntervalInHours(24).RepeatForever()));

    var cleanupJobKey = new JobKey("cleanup-old-ai-chat-messages-job", "cleanup");

    q.AddJob<CleanupOldAIChatMessagesJob>(opts => opts.WithIdentity(cleanupJobKey));

    q.AddTrigger(opts => opts
        .ForJob(cleanupJobKey)
        .WithIdentity("cleanup-old-ai-chat-messages-trigger", "cleanup")
        .WithCronSchedule("0 0 3 * * ?"));
});


builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

builder.Services.AddHttpClient();
builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/forbidden";
});

// Cloudinary
var cloudinaryAccount = new Account(
    builder.Configuration["Cloudinary:CloudName"] ?? "not-configured",
    builder.Configuration["Cloudinary:ApiKey"] ?? "not-configured",
    builder.Configuration["Cloudinary:ApiSecret"] ?? "not-configured");

builder.Services.AddSingleton(new Cloudinary(cloudinaryAccount));

// Repositories & Services
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<IBusinessAccessService, BusinessAccessService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWishlistCollectionService, WishlistCollectionService>();
builder.Services.AddScoped<IPostSaleService, PostSaleService>();
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
builder.Services.AddScoped<IProductAnalyticsService, ProductAnalyticsService>();
builder.Services.AddScoped<IAddressService, AddressService>();

builder.Services.AddMemoryCache();
builder.Services.AddDataProtection();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IMarketplaceBalanceService, MarketplaceBalanceService>();

builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
builder.Services.AddScoped<IProductTranslationService, ProductTranslationService>();

builder.Services.AddScoped<IUserDiscountService, UserDiscountService>();
builder.Services.AddScoped<IRefundChatService, RefundChatService>();
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddScoped<IWithdrawalService, WithdrawalService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPusher, SignalRNotificationPusher>();
builder.Services.AddScoped<IShedulerAdminService, ShedulerAdminService>();
builder.Services.AddScoped<IAdminInboxService, AdminInboxService>();

builder.Services.AddScoped<IGeminiChatApiClient, GeminiChatApiClient>();
builder.Services.AddScoped<ChatToolDispatcher>();
builder.Services.AddScoped<IChatToolsService, ChatToolsService>();
builder.Services.AddScoped<IGeminiChatService, GeminiChatService>();

// LiqPay configuration
builder.Services.Configure<LiqPayOptions>(builder.Configuration.GetSection("LiqPay"));
builder.Services.AddScoped<IPaymentService, LiqPayPaymentService>();

// Delivery API configuration
builder.Services.Configure<DeliveryApiOptions>(builder.Configuration.GetSection("DeliveryApi"));
builder.Services.AddScoped<IDeliveryProviderService, DeliveryProviderService>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

var app = builder.Build();

app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    await Marketplace.DAL.Seed.DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var defaultCulture = new CultureInfo("uk");
var supportedCultures = new List<CultureInfo>
{
    defaultCulture,
    new CultureInfo("en"),
    new CultureInfo("de"),
    new CultureInfo("fr"),
    new CultureInfo("es")
};
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Language switcher
localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
{
    new CookieRequestCultureProvider { CookieName = CookieRequestCultureProvider.DefaultCookieName },
    new AcceptLanguageHeaderRequestCultureProvider()
};

app.UseRequestLocalization(localizationOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapHub<Marketplace.Web.Hubs.RefundChatHub>("/refundChatHub");
app.MapHub<Marketplace.Web.Hubs.SupportTicketChatHub>("/supportTicketChatHub");
app.MapHub<Marketplace.Web.Hubs.NotificationHub>("/notificationHub");
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();