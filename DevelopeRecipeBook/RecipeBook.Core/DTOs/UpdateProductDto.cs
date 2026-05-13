using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Core.DTOs;

public class UpdateProductDto
{
    [Required(ErrorMessage = "Название продукта обязательно")]
    [MinLength(2, ErrorMessage = "Название продукта должно содержать минимум 2 символа")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5, ErrorMessage = "Нельзя добавить более 5 фото")]
    public List<string> Photos { get; set; } = new();

    [Range(0, double.MaxValue, ErrorMessage = "Калории не могут быть отрицательными")]
    public double Calories { get; set; }

    [Range(0, 100, ErrorMessage = "Белки должны быть от 0 до 100")]
    public double Proteins { get; set; }

    [Range(0, 100, ErrorMessage = "Жиры должны быть от 0 до 100")]
    public double Fats { get; set; }

    [Range(0, 100, ErrorMessage = "Углеводы должны быть от 0 до 100")]
    public double Carbs { get; set; }

    public string? Composition { get; set; }

    [Required(ErrorMessage = "Категория обязательна")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Способ приготовления обязателен")]
    public string CookingRequirement { get; set; } = string.Empty;

    public string Flags { get; set; } = string.Empty;
}