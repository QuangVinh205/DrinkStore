namespace DrinkStore.Models;

public static class DrinkSizes
{
    public const string Default = "M";
    public const string Pattern = "^(M|L)$";
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[] { "M", "L" });
    public static bool IsValid(string? size) => size is "M" or "L";

    public static decimal GetPrice(decimal basePrice, string size) => size switch
    {
        "M" => basePrice,
        "L" => basePrice + 5000m,
        _ => throw new ArgumentOutOfRangeException(nameof(size), "Size phải là M hoặc L.")
    };
}
