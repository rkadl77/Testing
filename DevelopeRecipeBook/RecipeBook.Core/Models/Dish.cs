using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Core.Models;

public class Dish
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Название блюда обязательно")]
    [MinLength(2, ErrorMessage = "Название блюда должно содержать минимум 2 символа")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5, ErrorMessage = "Нельзя добавить более 5 фото")]
    public List<string> Photos { get; set; } = new();

    [Range(0, double.MaxValue, ErrorMessage = "Калории не могут быть отрицательными")]
    public double Calories { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Белки не могут быть отрицательными")]
    public double Proteins { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Жиры не могут быть отрицательными")]
    public double Fats { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Углеводы не могут быть отрицательными")]
    public double Carbs { get; set; }

    [Range(0.1, double.MaxValue, ErrorMessage = "Размер порции должен быть больше 0")]
    public double PortionSize { get; set; }

    [Required(ErrorMessage = "Категория блюда обязательна")]
    public DishCategory Category { get; set; }

    public ProductFlags Flags { get; set; } = ProductFlags.None;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MinLength(1, ErrorMessage = "Блюдо должно содержать хотя бы один продукт")]
    public List<DishProduct> DishProducts { get; set; } = new();
}