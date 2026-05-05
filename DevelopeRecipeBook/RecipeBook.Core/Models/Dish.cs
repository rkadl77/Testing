namespace RecipeBook.Core.Models;

public class Dish
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;           // обязательное, мин 2 символа

    public List<string> Photos { get; set; } = new();          // 0-5 фото

    public double Calories { get; set; }                       // ккал/порция, >= 0 (авторасчёт + ручная корректировка)

    public double Proteins { get; set; }                       // г/порция

    public double Fats { get; set; }                           // г/порция

    public double Carbs { get; set; }                          // г/порция

    public double PortionSize { get; set; }                    // масса порции в г, > 0

    public DishCategory Category { get; set; }

    public ProductFlags Flags { get; set; } = ProductFlags.None;

    public DateTime CreatedAt { get; set; }                    // заполняется системой

    public DateTime? UpdatedAt { get; set; }                 

    public List<DishProduct> DishProducts { get; set; } = new();
}