using System.Collections.Specialized;
using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Services;
using Marketplace.DAL.Data;
using Marketplace.DAL.Repositories;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Quartz.Impl;
using Xunit;

namespace Marketplace.Tests;

public sealed class ProductServiceImageTests
{
    [Fact]
    public async Task CreateAsync_PreservesMainImageAdditionalImagesQuantityAndGeneratedSku()
    {
        await using var fixture = await ProductFixture.CreateAsync();

        var created = await fixture.Service.CreateAsync(new ProductCreateDto
        {
            BusinessId = fixture.BusinessId,
            CategoryId = fixture.CategoryId,
            Name = "New product",
            Description = "New product description",
            Price = 150,
            Quantity = 7,
            Sku = "",
            MainImage = Image("new-main.png"),
            AdditionalImages = [Image("new-extra.png")]
        });

        fixture.Context.ChangeTracker.Clear();
        var saved = await fixture.Context.Products.IgnoreAutoIncludes()
            .Include(product => product.Images)
            .SingleAsync(product => product.Id == created.Id);

        Assert.Equal(7, saved.Quantity);
        Assert.EndsWith("new-main.png", saved.MainImageUrl);
        Assert.EndsWith("new-extra.png", Assert.Single(saved.Images).ImageUrl);
        Assert.False(string.IsNullOrWhiteSpace(saved.Sku));
        Assert.NotEqual(ProductFixture.InitialSku, saved.Sku);
    }

    [Fact]
    public async Task UpdateAsync_UsesSubmittedQuantityWithoutVariants()
    {
        await using var fixture = await ProductFixture.CreateAsync();
        var edit = fixture.Edit();
        edit.Quantity = 17;

        await fixture.Service.UpdateAsync(edit);

        Assert.Equal(17, (await fixture.ReloadAsync()).Quantity);
    }

    [Fact]
    public async Task UpdateAsync_AddsAdditionalImage_AfterAccessCheckRead()
    {
        await using var fixture = await ProductFixture.CreateAsync();

        var accessCopy = await fixture.Service.GetByIdAsync(fixture.ProductId);
        Assert.NotNull(accessCopy);
        Assert.Empty(fixture.Context.ChangeTracker.Entries<Product>());

        await fixture.Service.UpdateAsync(fixture.Edit(additionalImages: [Image("additional.png")]));

        var saved = await fixture.ReloadAsync();
        Assert.Single(saved.Images);
        Assert.EndsWith("additional.png", saved.Images.Single().ImageUrl);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesMainImage()
    {
        await using var fixture = await ProductFixture.CreateAsync();

        await fixture.Service.UpdateAsync(fixture.Edit(mainImage: Image("replacement.png")));

        var saved = await fixture.ReloadAsync();
        Assert.EndsWith("replacement.png", saved.MainImageUrl);
    }

    [Fact]
    public async Task UpdateAsync_RemovesSelectedAdditionalImage()
    {
        await using var fixture = await ProductFixture.CreateAsync("old-a.png", "old-b.png");
        var imageToRemove = fixture.ImageIds[0];

        await fixture.Service.UpdateAsync(fixture.Edit(removedImageIds: [imageToRemove]));

        var saved = await fixture.ReloadAsync();
        Assert.Single(saved.Images);
        Assert.DoesNotContain(saved.Images, image => image.Id == imageToRemove);
    }

    [Fact]
    public async Task UpdateAsync_AddsAndRemovesAdditionalImagesTogether()
    {
        await using var fixture = await ProductFixture.CreateAsync("old-a.png", "old-b.png");
        var imageToRemove = fixture.ImageIds[0];

        await fixture.Service.UpdateAsync(fixture.Edit(
            additionalImages: [Image("new.png")],
            removedImageIds: [imageToRemove]));

        var saved = await fixture.ReloadAsync();
        Assert.Equal(2, saved.Images.Count);
        Assert.DoesNotContain(saved.Images, image => image.Id == imageToRemove);
        Assert.Contains(saved.Images, image => image.ImageUrl.EndsWith("new.png", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateAsync_WithoutImages_PreservesExistingImages()
    {
        await using var fixture = await ProductFixture.CreateAsync("old-a.png", "old-b.png");

        await fixture.Service.UpdateAsync(fixture.Edit());

        var saved = await fixture.ReloadAsync();
        Assert.Equal("https://test.local/main.png", saved.MainImageUrl);
        Assert.Equal(2, saved.Images.Count);
    }

    [Fact]
    public async Task UpdateAsync_WithBlankSku_GeneratesUniqueSku()
    {
        await using var fixture = await ProductFixture.CreateAsync();

        await fixture.Service.UpdateAsync(fixture.Edit(sku: ""));

        var saved = await fixture.ReloadAsync();
        Assert.False(string.IsNullOrWhiteSpace(saved.Sku));
        Assert.NotEqual(ProductFixture.InitialSku, saved.Sku);
    }

    [Fact]
    public async Task UpdateAsync_WithManualSku_PreservesNormalizedSku()
    {
        await using var fixture = await ProductFixture.CreateAsync();

        await fixture.Service.UpdateAsync(fixture.Edit(sku: "  TEST-MANUAL-001  "));

        var saved = await fixture.ReloadAsync();
        Assert.Equal("TEST-MANUAL-001", saved.Sku);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateManualSku_ThrowsFriendlyException()
    {
        await using var fixture = await ProductFixture.CreateAsync();
        fixture.Context.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = fixture.BusinessId,
            CategoryId = fixture.CategoryId,
            Name = "Other product",
            Description = "Other product description",
            Price = 10,
            Quantity = 1,
            Sku = "DUPLICATE-SKU",
            MainImageUrl = "https://test.local/other.png",
            Status = ProductStatus.Active
        });
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<DuplicateProductSkuException>(
            () => fixture.Service.UpdateAsync(fixture.Edit(sku: "DUPLICATE-SKU")));
    }

    private static IFormFile Image(string name)
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "image", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    private sealed class ProductFixture : IAsyncDisposable
    {
        public const string InitialSku = "TEST-INITIAL-001";

        private readonly StdSchedulerFactory _schedulerFactory;

        private ProductFixture(
            AppDbContext context,
            ProductService service,
            StdSchedulerFactory schedulerFactory,
            Guid productId,
            Guid businessId,
            Guid categoryId,
            List<Guid> imageIds)
        {
            Context = context;
            Service = service;
            _schedulerFactory = schedulerFactory;
            ProductId = productId;
            BusinessId = businessId;
            CategoryId = categoryId;
            ImageIds = imageIds;
        }

        public AppDbContext Context { get; }
        public ProductService Service { get; }
        public Guid ProductId { get; }
        public Guid BusinessId { get; }
        public Guid CategoryId { get; }
        public List<Guid> ImageIds { get; }

        public static async Task<ProductFixture> CreateAsync(params string[] additionalImageNames)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"product-images-{Guid.NewGuid():N}")
                .Options;
            var context = new AppDbContext(options);

            var businessId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var imageIds = new List<Guid>();

            context.Businesses.Add(new Business
            {
                Id = businessId,
                OwnerId = "test-owner",
                StoreName = "Test store",
                Description = "Test store",
                Status = BusinessStatus.Active
            });
            context.Categories.Add(new Category { Id = categoryId, Name = "Test category" });

            var product = new Product
            {
                Id = productId,
                BusinessId = businessId,
                CategoryId = categoryId,
                Name = "Test product",
                Description = "Test product description",
                Price = 100,
                Quantity = 5,
                Sku = InitialSku,
                MainImageUrl = "https://test.local/main.png",
                Status = ProductStatus.Active
            };

            foreach (var imageName in additionalImageNames)
            {
                var image = new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = $"https://test.local/{imageName}"
                };
                imageIds.Add(image.Id);
                product.Images.Add(image);
            }

            context.Products.Add(product);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var schedulerFactory = new StdSchedulerFactory(new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = $"ProductServiceTests-{Guid.NewGuid():N}"
            });
            var service = new ProductService(
                new GenericRepository<Product>(context),
                new FakeImageStorageService(),
                context,
                schedulerFactory);

            return new ProductFixture(
                context,
                service,
                schedulerFactory,
                productId,
                businessId,
                categoryId,
                imageIds);
        }

        public ProductEditDto Edit(
            IFormFile? mainImage = null,
            List<IFormFile>? additionalImages = null,
            List<Guid>? removedImageIds = null,
            string? sku = InitialSku)
        {
            return new ProductEditDto
            {
                Id = ProductId,
                CategoryId = CategoryId,
                Name = "Test product",
                Description = "Test product description",
                Price = 100,
                Quantity = 5,
                Sku = sku,
                MainImage = mainImage,
                AdditionalImages = additionalImages,
                RemovedAdditionalImageIds = removedImageIds ?? []
            };
        }

        public async Task<Product> ReloadAsync()
        {
            Context.ChangeTracker.Clear();
            return await Context.Products
                .IgnoreAutoIncludes()
                .Include(product => product.Images)
                .SingleAsync(product => product.Id == ProductId);
        }

        public async ValueTask DisposeAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.Shutdown(waitForJobsToComplete: false);
            await Context.DisposeAsync();
        }
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public Task<string?> UploadImageAsync(IFormFile file, string folder)
            => Task.FromResult<string?>($"https://test.local/{file.FileName}");
    }
}
