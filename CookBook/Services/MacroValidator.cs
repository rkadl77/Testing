namespace CookBook.Domain.Services;

public static class MacroValidator
{
    public static bool IsValidMacroSum(float proteins, float fats, float carbs)
    {
        return proteins + fats + carbs <= 100;
    }
}