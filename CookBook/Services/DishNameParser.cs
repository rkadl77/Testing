using CookBook.Domain.Enums;

namespace CookBook.Domain.Services;

public static class DishNameParser
{
    private static readonly Dictionary<string, DishCategory> Macros = new()
    {
        { "!десерт", DishCategory.Десерт },
        { "!первое", DishCategory.Первое },
        { "!второе", DishCategory.Второе },
        { "!напиток", DishCategory.Напиток },
        { "!салат", DishCategory.Салат },
        { "!суп", DishCategory.Суп },
        { "!перекус", DishCategory.Перекус }
    };

    public static (string CleanedName, DishCategory? Category) Parse(string name)
    {
        foreach (var (macro, category) in Macros)
        {
            if (name.StartsWith(macro, StringComparison.OrdinalIgnoreCase))
            {
                var cleanedName = name[macro.Length..].TrimStart();
                return (cleanedName, category);
            }
        }

        return (name, null);
    }
}