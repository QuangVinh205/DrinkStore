
using DrinkStore.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Data
{
    public class DrinkStoreDb : IdentityDbContext<ApplicationUser>
    {
        public DrinkStoreDb(DbContextOptions<DrinkStoreDb> options): base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<ShiftManager> ShiftManagers => Set<ShiftManager>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Ingredient> Ingredients => Set<Ingredient>();
        public DbSet<Recipe> Recipes => Set<Recipe>();
        public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Kế thừa User -> Customer / ShiftManager
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<User>("User")
                .HasValue<Customer>("Customer")
                .HasValue<ShiftManager>("ShiftManager");

            // Liên kết Customer với tài khoản ASP.NET Core Identity
            modelBuilder.Entity<Customer>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.IdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mỗi tài khoản Identity chỉ có tối đa một Customer
            modelBuilder.Entity<Customer>()
                .HasIndex(x => x.IdentityUserId)
                .IsUnique()
                .HasFilter("[IdentityUserId] IS NOT NULL");
            // Khóa chính
            modelBuilder.Entity<MenuItem>()
                .HasKey(x => x.ItemId);

            modelBuilder.Entity<Order>()
                .HasKey(x => x.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasKey(x => x.OrderItemId);

            modelBuilder.Entity<Payment>()
                .HasKey(x => x.PaymentId);

            modelBuilder.Entity<Ingredient>()
                .HasKey(x => x.IngredientId);

            modelBuilder.Entity<Recipe>()
                .HasKey(x => x.RecipeId);

            modelBuilder.Entity<RecipeItem>()
                .HasKey(x => x.RecipeItemId);

            // Customer - Order
            modelBuilder.Entity<Order>()
                .HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order - OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId);

            // MenuItem - OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(x => x.MenuItem)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order - Coupon
            modelBuilder.Entity<Order>()
                .HasOne(x => x.Coupon)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CouponCode)
                .OnDelete(DeleteBehavior.Restrict);

            // Order - Payment
            modelBuilder.Entity<Payment>()
                .HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId);

            // MenuItem - Recipe
            modelBuilder.Entity<Recipe>()
                .HasOne(x => x.MenuItem)
                .WithMany(x => x.Recipes)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Recipe - RecipeItem
            modelBuilder.Entity<RecipeItem>()
                .HasOne(x => x.Recipe)
                .WithMany(x => x.RecipeItems)
                .HasForeignKey(x => x.RecipeId);

            // Ingredient - RecipeItem
            modelBuilder.Entity<RecipeItem>()
                .HasOne(x => x.Ingredient)
                .WithMany(x => x.RecipeItems)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Độ chính xác của tiền và định lượng
            modelBuilder.Entity<MenuItem>()
                .Property(x => x.BasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(x => x.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(x => x.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(x => x.Discount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(x => x.UnitPriceSnapshot)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Coupon>()
                .Property(x => x.Percentage)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Coupon>()
                .Property(x => x.MinOrderValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Ingredient>()
                .Property(x => x.OnHand)
                .HasPrecision(18, 3);

            modelBuilder.Entity<Ingredient>()
                .Property(x => x.Reserved)
                .HasPrecision(18, 3);

            modelBuilder.Entity<Ingredient>()
                .Property(x => x.ReorderThreshold)
                .HasPrecision(18, 3);

            modelBuilder.Entity<RecipeItem>()
                .Property(x => x.RequiredAmount)
                .HasPrecision(18, 3);

            // Không cho trùng công thức theo kích cỡ
            modelBuilder.Entity<Recipe>()
                .HasIndex(x => new { x.MenuItemId, x.Size })
                .IsUnique();

            // Không cho trùng nguyên liệu trong công thức
            modelBuilder.Entity<RecipeItem>()
                .HasIndex(x => new { x.RecipeId, x.IngredientId })
                .IsUnique();
        }
    }
}
