using RecipeBook.Core.Interfaces;
using RecipeBook.Core.Models;

namespace RecipeBook.Core.Services;

public class ValidationService : IValidationService
{
    private static readonly Dictionary<string, string> CategoryMacros = new()
    {
        { "!десерт", "Десерт" },
        { "!первое", "Первое" },
        { "!второе", "Второе" },
        { "!напиток", "Напиток" },
        { "!салат", "Салат" },
        { "!суп", "Суп" },
        { "!перекус", "Перекус" }
    };

    public bool IsBjuSumValid(Product product)
    {
        return product.Proteins + product.Fats + product.Carbs <= 100;
    }

    public bool IsBjuSumValid(Dish dish)
    {
        return true; 
    }

    public ProductFlags GetAvailableFlags(Dish dish)
    {
        if (dish.DishProducts == null || dish.DishProducts.Count == 0)
            return ProductFlags.None;

        var available = ProductFlags.Веган | ProductFlags.Без_глютена | ProductFlags.Без_сахара;

        foreach (var dp in dish.DishProducts)
        {
            var productFlags = dp.Product.Flags;

            if (!productFlags.HasFlag(ProductFlags.Веган))
                available &= ~ProductFlags.Веган;

            if (!productFlags.HasFlag(ProductFlags.Без_глютена))
                available &= ~ProductFlags.Без_глютена;

            if (!productFlags.HasFlag(ProductFlags.Без_сахара))
                available &= ~ProductFlags.Без_сахара;
        }

        return available;
    }

    public string? ExtractCategoryFromName(string name)
    {
        // Ищем ПЕРВЫЙ макрос в строке (по наименьшему индексу)
        var lower = name.ToLower();
        string? firstMacro = null;
        int firstIndex = int.MaxValue;

        foreach (var macro in CategoryMacros.Keys)
        {
            var index = lower.IndexOf(macro, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && index < firstIndex)
            {
                firstIndex = index;
                firstMacro = macro;
            }
        }

        return firstMacro != null ? CategoryMacros[firstMacro] : null;
    }

    public string RemoveCategoryMacros(string name)
    {
        // Удаляем только ПЕРВЫЙ макрос
        var lower = name.ToLower();
        string? firstMacro = null;
        int firstIndex = int.MaxValue;

        foreach (var macro in CategoryMacros.Keys)
        {
            var index = lower.IndexOf(macro, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && index < firstIndex)
            {
                firstIndex = index;
                firstMacro = macro;
            }
        }

        if (firstMacro != null)
        {
            name = name.Remove(firstIndex, firstMacro.Length);
        }

        return name.Trim();
    }
}