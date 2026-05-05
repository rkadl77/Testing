using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RecipeBook.Core.Models;

namespace RecipeBook.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<DishProduct> DishProducts => Set<DishProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var photosComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()
        );

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Calories).IsRequired();
            entity.Property(p => p.Proteins).IsRequired();
            entity.Property(p => p.Fats).IsRequired();
            entity.Property(p => p.Carbs).IsRequired();
            entity.Property(p => p.Composition).IsRequired(false);
            entity.Property(p => p.Category).IsRequired().HasConversion<string>();
            entity.Property(p => p.CookingRequirement).IsRequired().HasConversion<string>();
            entity.Property(p => p.Flags).HasConversion<int>();
            entity.Property(p => p.Photos).HasConversion(
                v => string.Join(";", v),
                v => v.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList(),
                photosComparer
            );
        });

        // Dish
        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
            entity.Property(d => d.PortionSize).IsRequired();
            entity.Property(d => d.Category).IsRequired().HasConversion<string>();
            entity.Property(d => d.Flags).HasConversion<int>();
            entity.Property(d => d.Photos).HasConversion(
                v => string.Join(";", v),
                v => v.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList(),
                photosComparer
            );
        });

        // DishProduct (M:M)
        modelBuilder.Entity<DishProduct>(entity =>
        {
            entity.HasKey(dp => new { dp.DishId, dp.ProductId });

            entity.HasOne(dp => dp.Dish)
                  .WithMany(d => d.DishProducts)
                  .HasForeignKey(dp => dp.DishId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dp => dp.Product)
                  .WithMany(p => p.DishProducts)
                  .HasForeignKey(dp => dp.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}