namespace GameShelf.Core;

public enum PlayStatus { WantToPlay, Playing, Completed, Paused, Dropped }

public enum GameSort { RecentlyAdded, Title, HighestScore, MostPlayed }

public sealed record GameEntry(
    Guid Id,
    string Title,
    string Platform,
    string Genre,
    PlayStatus Status,
    int? Score,
    decimal HoursPlayed,
    string Notes,
    bool Favorite,
    DateTimeOffset AddedAt,
    DateOnly? CompletedOn);

public sealed record GameDraft(
    string Title,
    string Platform,
    string Genre,
    PlayStatus Status,
    int? Score,
    decimal HoursPlayed,
    string Notes,
    bool Favorite);

public sealed record CatalogStats(
    int Total,
    int Playing,
    int Completed,
    int WantToPlay,
    int Favorites,
    decimal TotalHours,
    double? AverageScore)
{
    public int CompletionPercent => Total == 0 ? 0 : (int)Math.Round(100.0 * Completed / Total);
}
