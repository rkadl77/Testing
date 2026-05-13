using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Core.DTOs;

public class CreateDishDto
{
    [Required(ErrorMessage = "Название блюда обязательно")]
    [MinLength(2, ErrorMessage = "Название блюда должно содержать минимум 2 символа")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5, ErrorMessage = "Нельзя добавить более 5 фото")]
    public List<string> Photos { get; set; } = new();

    [Range(0, double.MaxValue, ErrorMessage = "Калории не могут быть отрицательными")]
    public double? Calories { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Белки не могут быть отрицательными")]
    public double? Proteins { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Жиры не могут быть отрицательными")]
    public double? Fats { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Углеводы не могут быть отрицательными")]
    public double? Carbs { get; set; }

    [Range(0.1, double.MaxValue, ErrorMessage = "Размер порции должен быть больше 0")]
    public double PortionSize { get; set; }

    public string? Category { get; set; }

    public string Flags { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Блюдо должно содержать хотя бы один продукт")]
    public List<DishProductDto> Ingredients { get; set; } = new();
}

public class DishProductDto
{
    [Required(ErrorMessage = "ID продукта обязателен")]
    public Guid ProductId { get; set; }

    [Range(0.1, double.MaxValue, ErrorMessage = "Количество продукта должно быть больше 0")]
    public double Quantity { get; set; }
}