using Marketplace.DAL.Data;
using Marketplace.Domain.Common;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.DAL.Seed;

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
        await EnsureFallbackCategoryAsync(context);
        await SeedAdminAsync(userManager, roleManager);
        await ProductsAndDiscountsSeedAsync(context);
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

    public static async Task EnsureFallbackCategoryAsync(AppDbContext context)
    {
        if (!await context.Categories.AnyAsync(c => c.Name == SystemCategories.Other))
        {
            await context.Categories.AddAsync(new Category { Name = SystemCategories.Other, IsAdultOnly = false });
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

    public static async Task ProductsAndDiscountsSeedAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Name != SystemCategories.Other);
        if (category == null) return;

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
        if (adminUser == null) return;

        var business = await context.Businesses.FirstOrDefaultAsync();
        if (business == null)
        {
            business = new Business
            {
                OwnerId = adminUser.Id,
                StoreName = "Тестовий Магазин",
                Description = "Тестовий магазин для перевірки товарів та знижок",
                Status = BusinessStatus.Active
            };

            await context.Businesses.AddAsync(business);
            await context.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;

        var products = new List<Product>
        {
            new Product
            {
                BusinessId = business.Id,
                CategoryId = category.Id,
                Name = "Ноутбук зі знижкою 20%",
                Description = "Тестовий ноутбук для перевірки активної знижки",
                Price = 20000m,
                Quantity = 10,
                Sku = "SEED-001",
                Status = ProductStatus.Active,
                Discounts = new List<Discount>
                {
                    new Discount
                    {
                        Percent = 20,
                        StartDate = now.AddDays(-2),
                        EndDate = now.AddDays(5)
                    }
                }
            },
            new Product
            {
                BusinessId = business.Id,
                CategoryId = category.Id,
                Name = "Смартфон із застарілою знижкою",
                Description = "Знижка вже закінчилася",
                Price = 10000m,
                Quantity = 5,
                Sku = "SEED-002",
                Status = ProductStatus.Active,
                Discounts = new List<Discount>
                {
                    new Discount
                    {
                        Percent = 50,
                        StartDate = now.AddDays(-10),
                        EndDate = now.AddDays(-1)
                    }
                }
            },
            new Product
            {
                BusinessId = business.Id,
                CategoryId = category.Id,
                Name = "Навушники з двома знижками",
                Description = "Має обратися знижка 30% замість 15%",
                Price = 3000m,
                Quantity = 15,
                Sku = "SEED-003",
                Status = ProductStatus.Active,
                Discounts = new List<Discount>
                {
                    new Discount
                    {
                        Percent = 15,
                        StartDate = now.AddDays(-1),
                        EndDate = now.AddDays(3)
                    },
                    new Discount
                    {
                        Percent = 30,
                        StartDate = now.AddDays(-1),
                        EndDate = now.AddDays(3)
                    }
                }
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
