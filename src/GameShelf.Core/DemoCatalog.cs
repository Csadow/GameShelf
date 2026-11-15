namespace GameShelf.Core;

public static class DemoCatalog
{
    public static IReadOnlyList<GameEntry> Create(DateTimeOffset now)
    {
        var samples = new (string Title, string Platform, string Genre, PlayStatus Status, int? Score, decimal Hours, bool Favorite)[]
        {
            ("Hollow Knight", "PC", "Metroidvania", PlayStatus.Playing, 9, 32, true),
            ("Stardew Valley", "PC", "Simulation", PlayStatus.Completed, 10, 118, true),
            ("Celeste", "Nintendo Switch", "Platformer", PlayStatus.Completed, 9, 24, true),
            ("Outer Wilds", "PC", "Adventure", PlayStatus.WantToPlay, null, 0, false),
            ("Hades", "PlayStation", "Action", PlayStatus.Paused, 8, 41, false),
            ("Disco Elysium", "PC", "RPG", PlayStatus.WantToPlay, null, 0, false)
        };

        return samples.Select((sample, index) =>
            GameCatalog.Build(
                new GameDraft(sample.Title, sample.Platform, sample.Genre, sample.Status,
                    sample.Score, sample.Hours, "Demo entry. Add your own thoughts here.", sample.Favorite),
                Guid.NewGuid(), now.AddDays(-index), null).Value!).ToArray();
    }
}
