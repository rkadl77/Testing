using CookBook.Domain.Entities;
using CookBook.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<DishProduct> DishProducts => Set<DishProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Calories).IsRequired();
            entity.Property(p => p.Proteins).IsRequired();
            entity.Property(p => p.Fats).IsRequired();
            entity.Property(p => p.Carbs).IsRequired();
            entity.Property(p => p.Category).IsRequired().HasConversion<string>();
            entity.Property(p => p.CookingRequired).IsRequired().HasConversion<string>();
            entity.Property(p => p.Flags).IsRequired().HasConversion<string>();
            entity.Property(p => p.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
            entity.Property(d => d.Calories).IsRequired();
            entity.Property(d => d.Proteins).IsRequired();
            entity.Property(d => d.Fats).IsRequired();
            entity.Property(d => d.Carbs).IsRequired();
            entity.Property(d => d.PortionSize).IsRequired();
            entity.Property(d => d.Category).IsRequired().HasConversion<string>();
            entity.Property(d => d.Flags).IsRequired().HasConversion<string>();
            entity.Property(d => d.CreatedAt).IsRequired();
        });

        // DishProduct (many-to-many)
        modelBuilder.Entity<DishProduct>(entity =>
        {
            entity.HasKey(dp => dp.Id);

            entity.HasOne(dp => dp.Dish)
                .WithMany(d => d.DishProducts)
                .HasForeignKey(dp => dp.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dp => dp.Product)
                .WithMany(p => p.DishProducts)
                .HasForeignKey(dp => dp.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // нельзя удалить продукт, если он в блюде

            entity.Property(dp => dp.Amount).IsRequired();
        });
    }
}