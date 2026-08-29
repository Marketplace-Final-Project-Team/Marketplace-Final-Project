using System.Globalization;
using CloudinaryDotNet;
using Marketplace.BLL.Common;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Services;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.DAL.Repositories;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Marketplace.BLL.Configs;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    //if (builder.Environment.IsDevelopment())
    //{
    //    options.UseInMemoryDatabase("MarketplaceFrontendDb");
    //    return;
    //}

    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddQuartz();

builder.Services.AddQuartzHostedService(opt => 
{
    opt.WaitForJobsToComplete = true;
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IContentModerationService, ContentModerationService>();


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

var cloudinaryAccount = new Account(
    builder.Configuration["Cloudinary:CloudName"] ?? "not-configured",
    builder.Configuration["Cloudinary:ApiKey"] ?? "not-configured",
    builder.Configuration["Cloudinary:ApiSecret"] ?? "not-configured");

builder.Services.AddSingleton(new Cloudinary(cloudinaryAccount));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
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

// System settings (admin-editable key/value store) and activity logs — new,
// additive infrastructure that doesn't touch any existing service's behavior.
builder.Services.AddMemoryCache();
builder.Services.AddDataProtection();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IMarketplaceBalanceService, MarketplaceBalanceService>();

builder.Services.AddScoped<IContentModerationService, ContentModerationService>();


// LiqPay configuration
builder.Services.Configure<LiqPayOptions>(builder.Configuration.GetSection("LiqPay"));
builder.Services.AddScoped<IPaymentService, LiqPayPaymentService>();

// Delivery API configuration
builder.Services.Configure<DeliveryApiOptions>(builder.Configuration.GetSection("DeliveryApi"));
builder.Services.AddScoped<IDeliveryProviderService, DeliveryProviderService>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailSender, EmailSender>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await Marketplace.DAL.Seed.DbSeeder.SeedAsync(scope.ServiceProvider);
}



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var defaultCulture = new CultureInfo("uk-UA");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
