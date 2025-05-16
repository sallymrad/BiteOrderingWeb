using BiteOrderWeb.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace BiteOrderWeb.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        { }
      
        public DbSet<Order> Orders { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<ShoppingCartProduct> ShoppingCartProducts { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<RestaurantProduct> RestaurantProducts { get; set; }
        public DbSet<OrderProduct> OrderProduct { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<OrderRejection> OrderRejections { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Setting> Settings { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);



            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasColumnType("decimal(10,2)");


            modelBuilder.Entity<ProductSize>()
    .Property(p => p.Price)
    .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
    .HasOne(p => p.Order)
    .WithMany()
    .HasForeignKey(p => p.OrderId)
    .IsRequired()
    .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Users>()
           .HasOne(u => u.Address)
           .WithMany(a => a.Users)
           .HasForeignKey(u => u.AddressId)
           .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Restaurant>()
            .HasOne(r => r.Address)
             .WithMany(a => a.Restaurants)
             .HasForeignKey(r => r.AddressId)
             .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<OrderProduct>()
    .HasKey(op => new { op.OrderId, op.ProductId, op.Size });


            modelBuilder.Entity<OrderProduct>()
                .HasOne(op => op.Order)
                .WithMany(o => o.OrderProducts)
                .HasForeignKey(op => op.OrderId);

            modelBuilder.Entity<OrderProduct>()
                .HasOne(op => op.Product)
                .WithMany(p => p.OrderProducts)
                .HasForeignKey(op => op.ProductId);

            modelBuilder.Entity<RestaurantProduct>()
                .HasKey(rp => new { rp.RestaurantId, rp.ProductId });

            modelBuilder.Entity<RestaurantProduct>()
                .HasOne(rp => rp.Restaurant)
                .WithMany(r => r.RestaurantProducts)
                .HasForeignKey(rp => rp.RestaurantId);

            modelBuilder.Entity<RestaurantProduct>()
                .HasOne(rp => rp.Product)
                .WithMany(p => p.RestaurantProducts)
                .HasForeignKey(rp => rp.ProductId);


            modelBuilder.Entity<ShoppingCartProduct>()
      .HasKey(sp => new { sp.ShoppingCartId, sp.ProductId, sp.Size });



            modelBuilder.Entity<ShoppingCartProduct>()
                .HasOne(sp => sp.ShoppingCart)
                .WithMany(sc => sc.ShoppingCartProducts)
                .HasForeignKey(sp => sp.ShoppingCartId);

            modelBuilder.Entity<ShoppingCartProduct>()
                .HasOne(sp => sp.Product)
                .WithMany(p => p.ShoppingCartProducts)
                .HasForeignKey(sp => sp.ProductId);

            modelBuilder.Entity<Order>()
     .HasOne(o => o.Driver)
     .WithMany()
     .HasForeignKey(o => o.DriverId)
     .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderRejection>()
    .HasOne(or => or.Order)
    .WithMany()
    .HasForeignKey(or => or.OrderId)
    .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
