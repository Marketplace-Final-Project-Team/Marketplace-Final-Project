using Marketplace.Domain.Common;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Marketplace.DAL.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Business> Businesses { get; set; }
        public DbSet<BusinessMember> BusinessMembers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductTranslation> ProductTranslations { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }

        public DbSet<WishlistCollection> WishlistCollections { get; set; }


        public DbSet<Discount> Discounts { get; set; }

        public DbSet<PostSaleRequest> PostSaleRequests { get; set; }
        public DbSet<PostSaleRequestImage> PostSaleRequestImages { get; set; }

        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<SupportTicketMessage> SupportTicketMessages { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<UserActionLog> UserActionLogs { get; set; }
        public DbSet<BusinessActionLog> BusinessActionLogs { get; set; }
        public DbSet<MarketplaceBalance> MarketplaceBalances { get; set; }
        public DbSet<ProductEvent> ProductEvents { get; set; }

        public DbSet<UserDiscount> UserDiscounts { get; set; }
        public DbSet<RefundChatMessage> RefundChatMessages { get; set; }

        public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
        public DbSet<SavedPayoutCard> SavedPayoutCards { get; set; }
        public DbSet<AIChatMessage> AIChatMessages { get; set; }

        public DbSet<ReviewAppeal> ReviewAppeals { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            builder.Entity<Business>().HasQueryFilter(b => !b.IsDeleted);
            builder.Entity<BusinessMember>().HasQueryFilter(bm => !bm.IsDeleted);
            builder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
            builder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            builder.Entity<ProductImage>().HasQueryFilter(pi => !pi.IsDeleted);
            builder.Entity<Cart>().HasQueryFilter(cart => !cart.IsDeleted);
            builder.Entity<CartItem>().HasQueryFilter(ci => !ci.IsDeleted);
            builder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
            builder.Entity<OrderItem>().HasQueryFilter(oi => !oi.IsDeleted);
            builder.Entity<Address>().HasQueryFilter(a => !a.IsDeleted);
            builder.Entity<Wishlist>().HasQueryFilter(w => !w.IsDeleted);
            builder.Entity<WishlistItem>().HasQueryFilter(wi => !wi.IsDeleted);
            builder.Entity<Review>().HasQueryFilter(r => !r.IsDeleted);

            builder.Entity<Discount>().HasQueryFilter(d => !d.IsDeleted);
            builder.Entity<PostSaleRequest>().HasQueryFilter(psr => !psr.IsDeleted);
            builder.Entity<PostSaleRequestImage>().HasQueryFilter(psi => !psi.IsDeleted);
            builder.Entity<PromoCode>().HasQueryFilter(pc => !pc.IsDeleted);
            builder.Entity<SystemSetting>().HasQueryFilter(s => !s.IsDeleted);
            builder.Entity<MarketplaceBalance>().HasQueryFilter(mb => !mb.IsDeleted);
            builder.Entity<RefundChatMessage>().HasQueryFilter(m => !m.IsDeleted);
            builder.Entity<AIChatMessage>().HasQueryFilter(d => !d.IsDeleted);
            builder.Entity<SavedPayoutCard>().HasOne(c => c.Business).WithMany(b => b.SavedPayoutCards).HasForeignKey(c => c.BusinessId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Business>()
            .HasOne(b => b.ScheduledPayoutCard)
            .WithMany()
            .HasForeignKey(b => b.ScheduledPayoutCardId)
            .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<Business>()
                .HasOne(b => b.Owner)
                .WithMany()
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BusinessMember>()
                .HasOne(bm => bm.Business)
                .WithMany(b => b.Members)
                .HasForeignKey(bm => bm.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BusinessMember>()
                .HasOne(bm => bm.User)
                .WithMany()
                .HasForeignKey(bm => bm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasOne(p => p.Business)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany(u => u.Wishlists)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasOne(wi => wi.Wishlist)
                .WithMany(w => w.WishlistItems)
                .HasForeignKey(wi => wi.WishlistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WishlistItem>()
                .HasOne(wi => wi.Product)
                .WithMany()
                .HasForeignKey(wi => wi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
    .HasOne(r => r.User)
    .WithMany(u => u.Reviews)
    .HasForeignKey(r => r.UserId)
    .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReviewAppeal>().HasQueryFilter(ra => !ra.IsDeleted);
            builder.Entity<ReviewAppeal>()
                .HasOne(ra => ra.Review)
                .WithMany()
                .HasForeignKey(ra => ra.ReviewId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ReviewAppeal>()
                .HasOne(ra => ra.User)
                .WithMany()
                .HasForeignKey(ra => ra.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Discount>()
                .HasOne(d => d.Product)
                .WithMany(p => p.Discounts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Discount>()
                .HasOne(d => d.Business)
                .WithMany(b => b.Discounts)
                .HasForeignKey(d => d.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistCollection>()
                .HasOne(wc => wc.User)
                .WithMany(u => u.WishlistCollections)
                .HasForeignKey(wc => wc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistCollection>()
                .HasMany(wc => wc.Items)
                .WithOne(wi => wi.WishlistCollection)
                .HasForeignKey(wi => wi.WishlistCollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostSaleRequest>()
                .HasOne(psr => psr.Order)
                .WithMany()
                .HasForeignKey(psr => psr.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostSaleRequest>()
                .HasOne(psr => psr.Buyer)
                .WithMany()
                .HasForeignKey(psr => psr.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostSaleRequest>()
                .HasOne(psr => psr.OrderItem)
                .WithMany()
                .HasForeignKey(psr => psr.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostSaleRequestImage>()
                .HasOne(psi => psi.PostSaleRequest)
                .WithMany(psr => psr.Images)
                .HasForeignKey(psi => psi.PostSaleRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PromoCode>()
                .HasOne(pc => pc.Business)
                .WithMany()
                .HasForeignKey(pc => pc.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PromoCode>()
                .HasIndex(pc => pc.Code)
                .IsUnique();

            builder.Entity<SystemSetting>()
                .HasIndex(s => new { s.Category, s.Key })
                .IsUnique();

            builder.Entity<ProductTranslation>()
                .HasQueryFilter(pt => !pt.IsDeleted);

            builder.Entity<ProductTranslation>()
                .HasOne(pt => pt.Product)
                .WithMany()
                .HasForeignKey(pt => pt.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductTranslation>()
                .HasIndex(pt => new { pt.ProductId, pt.Culture })
                .IsUnique();

            builder.Entity<BusinessActionLog>()
                .HasOne<Business>()
                .WithMany()
                .HasForeignKey(bal => bal.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);



            builder.Entity<ProductEvent>()
                .HasOne<Product>()
                .WithMany()
                .HasForeignKey(pe => pe.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductEvent>()
                .HasIndex(pe => new { pe.BusinessId, pe.CreatedAt });

            builder.Entity<UserDiscount>()
                .HasOne(ud => ud.User)
                .WithMany(ud => ud.UserDiscounts)
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<UserDiscount>()
                .HasOne(ud => ud.Business)
                .WithMany(b => b.UserDiscounts)
                .HasForeignKey(ud => ud.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RefundChatMessage>()
                .HasOne(m => m.PostSaleRequest)
                .WithMany()
                .HasForeignKey(m => m.PostSaleRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WithdrawalRequest>()
                .HasOne(wr => wr.Business)
                .WithMany()
                .HasForeignKey(wr => wr.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AIChatMessage>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.AIChatMessages)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    builder.Entity(entityType.ClrType)
                        .Property(nameof(BaseEntity.Id))
                        .ValueGeneratedNever();
                }
            }

            builder.Entity<Product>().Navigation(p => p.Category).AutoInclude();
            builder.Entity<Product>().Navigation(p => p.Images).AutoInclude();
            builder.Entity<Product>().Navigation(p => p.Business).AutoInclude();
            builder.Entity<Wishlist>().Navigation(w => w.WishlistItems).AutoInclude();
            builder.Entity<Review>().Navigation(r => r.User).AutoInclude();


            builder.Entity<Product>().Navigation(p => p.Discounts).AutoInclude();
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(ct);
        }

        private void UpdateTimestamps()
        {
            foreach (var entry in ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = DateTime.UtcNow;

                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
