using CookBook.Domain.Entities;

namespace CookBook.Domain.Services;

public static class CaloriesCalculator
{
    public static (float Calories, float Proteins, float Fats, float Carbs) Calculate(
        IEnumerable<(Product product, float amount)> composition)
    {
        float totalCalories = 0;
        float totalProteins = 0;
        float totalFats = 0;
        float totalCarbs = 0;

        foreach (var (product, amount) in composition)
        {
            totalCalories += product.Calories * amount / 100f;
            totalProteins += product.Proteins * amount / 100f;
            totalFats += product.Fats * amount / 100f;
            totalCarbs += product.Carbs * amount / 100f;
        }

        return (totalCalories, totalProteins, totalFats, totalCarbs);
    }
}