using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.DAL.Seed
{
    public class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            await CategoriesSeedAsync(context);
            await SeedAdminAsync(userManager, roleManager);
        }

        public static async Task CategoriesSeedAsync(AppDbContext context)
        {
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Електроніка" },
                    new Category { Name = "Книги" },
                    new Category { Name = "Одяг" },
                    new Category { Name = "Дом і кухня" },
                    new Category { Name = "Спорт і відпочинок" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedAdminAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));

                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Не вдалося створити роль Admin: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            var existingAdmin = await userManager.FindByNameAsync("admin");

            if (existingAdmin == null)
            {
                var admin = new User
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(admin, "Admin123!");

                if (!createResult.Succeeded)
                {
                    throw new Exception($"Не вдалося створити адміна: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }

                existingAdmin = admin;
            }

            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(existingAdmin, "Admin");

                if (!addToRoleResult.Succeeded)
                {
                    throw new Exception($"Не вдалося додати роль адміну: " + string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
