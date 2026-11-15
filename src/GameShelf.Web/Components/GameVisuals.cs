namespace GameShelf.Web.Components;

public static class GameVisuals
{
    private static readonly string[] Palettes = ["cover-violet", "cover-blue", "cover-coral", "cover-green", "cover-gold", "cover-pink"];

    public static string Palette(string title)
    {
        // A stable cover treatment without a third-party image service.
        var hash = title.Aggregate(17u, (value, letter) => value * 31 + letter);
        return Palettes[hash % Palettes.Length];
    }

    public static string Initials(string title) => string.Concat(
        title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(word => char.ToUpperInvariant(word[0])));
}
