//using Marketplace.DAL.Data;
//using Marketplace.Domain.Entities;
//using Marketplace.Domain.Enums;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;

//namespace Marketplace.Web.Seed
//{
//    // ВРЕМЕННЫЙ seeder для тестирования CRUD.
  
//    public static class DbSeeder
//    {
//        public static readonly Guid TempBusinessId = Guid.Parse("99999999-9999-9999-9999-999999999999");
//        public static readonly Guid TempCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

//        private const string TestUserEmail = "test-seller@marketplace.local";
//        private const string TestUserPassword = "TestSeller123!"; 

//        public static async Task SeedAsync(IServiceProvider services)
//        {
//            using var scope = services.CreateScope();
//            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        
//            var testUser = await userManager.FindByEmailAsync(TestUserEmail);
//            if (testUser == null)
//            {
//                testUser = new User
//                {
//                    UserName = TestUserEmail,
//                    Email = TestUserEmail,
//                    EmailConfirmed = true
//                };

//                var result = await userManager.CreateAsync(testUser, TestUserPassword);
//                if (!result.Succeeded)
//                {
//                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
//                    throw new InvalidOperationException($"Не удалось создать тестового пользователя: {errors}");
//                }
//            }

//            // 2. Тестовая категория
//            var categoryExists = await context.Categories.AnyAsync(c => c.Id == TempCategoryId);
//            if (!categoryExists)
//            {
//                context.Categories.Add(new Category
//                {
//                    Id = TempCategoryId,
//                    Name = "Тестовая категория"
//                });
//            }

//            // 3. Тестовый бизнес
//            var businessExists = await context.Businesses.AnyAsync(b => b.Id == TempBusinessId);
//            if (!businessExists)
//            {
//                context.Businesses.Add(new Business
//                {
//                    Id = TempBusinessId,
//                    OwnerId = testUser.Id,
//                    StoreName = "Test Business",
//                    Description = "Тестовий магазин для розробки",
//                    Status = BusinessStatus.Active
//                });
//            }

//            await context.SaveChangesAsync();
//        }
//    }
//}