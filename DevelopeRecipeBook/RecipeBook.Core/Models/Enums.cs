namespace RecipeBook.Core.Models;

public enum ProductCategory
{
    Замороженный,
    Мясной,
    Овощи,
    Зелень,
    Специи,
    Крупы,
    Консервы,
    Жидкость,
    Сладости
}

public enum CookingRequirement
{
    Готовый_к_употреблению,
    Полуфабрикат,
    Требует_приготовления
}

[Flags]
public enum ProductFlags
{
    None = 0,
    Веган = 1,
    Без_глютена = 2,
    Без_сахара = 4
}

public enum DishCategory
{
    Десерт,
    Первое,
    Второе,
    Напиток,
    Салат,
    Суп,
    Перекус
}