using RecipeBook.Core.Models;

namespace RecipeBook.Core.Interfaces;

public interface IValidationService
{
    // Проверяет, что сумма Б+Ж+У на 100г не превышает 100.
    bool IsBjuSumValid(Product product);

    // Проверяет, что сумма Б+Ж+У на 100г не превышает 100 для блюда.
    /// Пересчёт на 100г: (нутриент_порции / размер_порции) * 100
    bool IsBjuSumValid(Dish dish);

    // Проверяет доступность флагов для блюда на основе состава.
    /// Возвращает список флагов, которые можно установить.
    ProductFlags GetAvailableFlags(Dish dish);

    // Определяет категорию блюда по макросу в названии (!десерт, !первое и т.д.)
    // Возвращает null, если макрос не найден.
    string? ExtractCategoryFromName(string name);

    // Удаляет макрос категории из названия.
    string RemoveCategoryMacros(string name);
}