using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Web.Seed
{
    // ВРЕМЕННЫЙ seeder для тестирования CRUD.
  
    public static class DbSeeder
    {
        public static readonly Guid TempBusinessId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        public static readonly Guid TempCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid HeadphonesId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid KeyboardId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid LampId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        private const string TestUserEmail = "test-seller@marketplace.local";
        private const string TestUserPassword = "TestSeller123!"; 

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        
            var testUser = await userManager.FindByEmailAsync(TestUserEmail);
            if (testUser == null)
            {
                testUser = new User
                {
                    UserName = TestUserEmail,
                    Email = TestUserEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(testUser, TestUserPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Не удалось создать тестового пользователя: {errors}");
                }
            }

            // 2. Тестовая категория
            var categoryExists = await context.Categories.AnyAsync(c => c.Id == TempCategoryId);
            if (!categoryExists)
            {
                context.Categories.Add(new Category
                {
                    Id = TempCategoryId,
                    Name = "Тестовая категория"
                });
            }

            // 3. Тестовый бизнес
            var businessExists = await context.Businesses.AnyAsync(b => b.Id == TempBusinessId);
            if (!businessExists)
            {
                context.Businesses.Add(new Business
                {
                    Id = TempBusinessId,
                    OwnerId = testUser.Id,
                    StoreName = "Test Business",
                    Description = "Тестовий магазин для розробки",
                    Status = BusinessStatus.Active
                });
            }

            var sampleProducts = new[]
            {
                new Product
                {
                    Id = HeadphonesId,
                    BusinessId = TempBusinessId,
                    CategoryId = TempCategoryId,
                    Name = "Бездротові навушники AirSound",
                    Description = "Легкі навушники з активним шумозаглушенням, чистим звуком і зарядом до 30 годин.",
                    Price = 2499,
                    Quantity = 12,
                    Sku = "DEMO-001",
                    Status = ProductStatus.Active
                },
                new Product
                {
                    Id = KeyboardId,
                    BusinessId = TempBusinessId,
                    CategoryId = TempCategoryId,
                    Name = "Механічна клавіатура KeyPro",
                    Description = "Компактна клавіатура з підсвічуванням, тихими перемикачами та підключенням через USB-C.",
                    Price = 3199,
                    Quantity = 8,
                    Sku = "DEMO-002",
                    Status = ProductStatus.Active
                },
                new Product
                {
                    Id = LampId,
                    BusinessId = TempBusinessId,
                    CategoryId = TempCategoryId,
                    Name = "Настільна LED-лампа Flex",
                    Description = "Регульована лампа для роботи й навчання з трьома режимами кольору та сенсорним керуванням.",
                    Price = 1199,
                    Quantity = 20,
                    Sku = "DEMO-003",
                    Status = ProductStatus.Active
                }
            };

            foreach (var product in sampleProducts)
            {
                if (!await context.Products.AnyAsync(existing => existing.Id == product.Id))
                {
                    context.Products.Add(product);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
