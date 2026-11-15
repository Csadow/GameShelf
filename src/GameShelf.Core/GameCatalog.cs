namespace GameShelf.Core;

public static class GameCatalog
{
    public static Result<GameEntry> Build(GameDraft draft, Guid id, DateTimeOffset now, GameEntry? previous = null)
    {
        var title = (draft.Title ?? "").Trim();
        var platform = (draft.Platform ?? "").Trim();
        var genre = (draft.Genre ?? "").Trim();
        var notes = (draft.Notes ?? "").Trim();

        if (title.Length is < 1 or > 120)
            return Result<GameEntry>.Failure("Title must be between 1 and 120 characters.");
        if (platform.Length is < 1 or > 60)
            return Result<GameEntry>.Failure("Platform must be between 1 and 60 characters.");
        if (genre.Length is < 1 or > 60)
            return Result<GameEntry>.Failure("Genre must be between 1 and 60 characters.");
        if (!Enum.IsDefined(draft.Status))
            return Result<GameEntry>.Failure("Choose a valid play status.");
        if (draft.Score is < 1 or > 10)
            return Result<GameEntry>.Failure("Score must be between 1 and 10.");
        if (draft.HoursPlayed is < 0 or > 100000)
            return Result<GameEntry>.Failure("Hours played must be between 0 and 100,000.");
        if (notes.Length > 2000)
            return Result<GameEntry>.Failure("Notes cannot exceed 2,000 characters.");

        DateOnly? completedOn = draft.Status == PlayStatus.Completed
            ? previous?.CompletedOn ?? DateOnly.FromDateTime(now.Date)
            : null;

        return Result<GameEntry>.Success(new GameEntry(
            id, title, platform, genre, draft.Status, draft.Score,
            draft.HoursPlayed, notes, draft.Favorite, previous?.AddedAt ?? now, completedOn));
    }

    public static IReadOnlyList<GameEntry> Query(
        IEnumerable<GameEntry> games, string? search = null,
        PlayStatus? status = null, GameSort sort = GameSort.RecentlyAdded)
    {
        var text = search?.Trim();
        var filtered = games
            .Where(game => status is null || game.Status == status)
            .Where(game => string.IsNullOrEmpty(text) ||
                game.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                game.Platform.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                game.Genre.Contains(text, StringComparison.OrdinalIgnoreCase));

        return (sort switch
        {
            GameSort.Title => filtered.OrderBy(game => game.Title, StringComparer.OrdinalIgnoreCase),
            GameSort.HighestScore => filtered.OrderByDescending(game => game.Score).ThenBy(game => game.Title),
            GameSort.MostPlayed => filtered.OrderByDescending(game => game.HoursPlayed).ThenBy(game => game.Title),
            _ => filtered.OrderByDescending(game => game.AddedAt).ThenBy(game => game.Title)
        }).ToArray();
    }

    public static CatalogStats Summarize(IEnumerable<GameEntry> games)
    {
        var list = games.ToArray();
        var scores = list.Where(game => game.Score.HasValue).Select(game => game.Score!.Value).ToArray();
        return new CatalogStats(
            list.Length,
            list.Count(game => game.Status == PlayStatus.Playing),
            list.Count(game => game.Status == PlayStatus.Completed),
            list.Count(game => game.Status == PlayStatus.WantToPlay),
            list.Count(game => game.Favorite),
            list.Sum(game => game.HoursPlayed),
            scores.Length == 0 ? null : scores.Average());
    }

    public static IReadOnlyList<(string Genre, int Count)> TopGenres(IEnumerable<GameEntry> games, int limit = 5) =>
        games.GroupBy(game => game.Genre, StringComparer.OrdinalIgnoreCase)
            .Select(group => (Genre: group.First().Genre, Count: group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Genre)
            .Take(Math.Max(0, limit))
            .ToArray();

    public static string Label(this PlayStatus status) => status switch
    {
        PlayStatus.WantToPlay => "Want to play",
        PlayStatus.Playing => "Playing",
        PlayStatus.Completed => "Completed",
        PlayStatus.Paused => "Paused",
        PlayStatus.Dropped => "Dropped",
        _ => "Unknown"
    };
}
