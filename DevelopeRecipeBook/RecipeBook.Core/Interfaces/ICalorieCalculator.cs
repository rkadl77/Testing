using RecipeBook.Core.Models;

namespace RecipeBook.Core.Interfaces;

public interface ICalorieCalculator
{
    // Рассчитывает КБЖУ порции блюда исходя из состава.
    // Формула для каждого нутриента: Σ(значение_на_100г × количество_в_порции / 100)
    (double Calories, double Proteins, double Fats, double Carbs) Calculate(Dish dish);
}