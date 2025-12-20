using Microsoft.EntityFrameworkCore;
using Book_Store.Models;
using Book_Store.Helpers; // ✅ Import converter
using System;

namespace Book_Store.Data
{
    public class BookStoreDbContext : DbContext
    {
        public BookStoreDbContext(DbContextOptions<BookStoreDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductDetail> ProductDetails { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentHistory> PaymentHistories { get; set; }

        public DbSet<ChatSession> ChatSessions { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<Subscriber> Subscribers { get; set; }
        




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Quan hệ 1-n: Category - Product
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n: Author - Product  
            modelBuilder.Entity<Author>()
                .HasMany(a => a.Products)
                .WithOne(p => p.Author)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-1: Product - ProductDetail
            modelBuilder.Entity<Product>()
                .HasOne(p => p.ProductDetail)
                .WithOne(pd => pd.Product)
                .HasForeignKey<ProductDetail>(pd => pd.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ 1-n: User - Order
            modelBuilder.Entity<User>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n: Order - OrderDetail
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderDetails)
                .WithOne(od => od.Order)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n: Product - OrderDetail
            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderDetails)
                .WithOne(od => od.Product)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n: User - CartItem
            modelBuilder.Entity<User>()
                .HasMany(u => u.CartItems)
                .WithOne(ci => ci.User)
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n: Product - CartItem
            modelBuilder.Entity<Product>()
                .HasMany(p => p.CartItems)
                .WithOne(ci => ci.Product)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n: Order - Payment
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Payments)
                .WithOne(p => p.Order)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n: User - Payment
            modelBuilder.Entity<User>()
                .HasMany(u => u.Payments)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n: Payment - PaymentHistory
            modelBuilder.Entity<Payment>()
                .HasMany(p => p.PaymentHistories)
                .WithOne(ph => ph.Payment)
                .HasForeignKey(ph => ph.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tạo unique index cho Email của User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ⚡ Cấu hình enum OrderStatus -> string tiếng Việt
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion(new OrderStatusConverter())
                .HasMaxLength(50);
        }
    }
}