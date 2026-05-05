using RecipeBook.Core.Interfaces;
using RecipeBook.Core.Models;

namespace RecipeBook.Core.Services;

// Сервис автоматического расчёта КБЖУ блюда на основе состава.
// Формулы:
//   Калорийность порции = Σ(кал_продукта_на_100г × кол-во_в_порции / 100)
//  Белки порции         = Σ(белки_продукта_на_100г × кол-во_в_порции / 100)
//   Жиры порции          = Σ(жиры_продукта_на_100г × кол-во_в_порции / 100)
//   Углеводы порции      = Σ(углеводы_продукта_на_100г × кол-во_в_порции / 100)
public class CalorieCalculator : ICalorieCalculator
{
    public (double Calories, double Proteins, double Fats, double Carbs) Calculate(Dish dish)
    {
        if (dish.DishProducts == null || dish.DishProducts.Count == 0)
            return (0, 0, 0, 0);

        double totalCalories = 0;
        double totalProteins = 0;
        double totalFats = 0;
        double totalCarbs = 0;

        foreach (var dp in dish.DishProducts)
        {
            var product = dp.Product;
            var quantity = dp.Quantity;

            totalCalories += product.Calories * quantity / 100.0;
            totalProteins += product.Proteins * quantity / 100.0;
            totalFats += product.Fats * quantity / 100.0;
            totalCarbs += product.Carbs * quantity / 100.0;
        }

        return (
            Math.Round(totalCalories, 2),
            Math.Round(totalProteins, 2),
            Math.Round(totalFats, 2),
            Math.Round(totalCarbs, 2)
        );
    }
}