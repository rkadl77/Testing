namespace CookBook.Domain.Enums;

[Flags]
public enum DishFlag
{
    None = 0,
    Веган = 1,
    БезГлютена = 2,
    БезСахара = 4
}