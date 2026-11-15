using GameShelf.Core;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAILED: {name}");
    Console.WriteLine($"PASS: {name}");
    passed++;
}

var now = new DateTimeOffset(2026, 11, 14, 18, 0, 0, TimeSpan.FromHours(5));
var draft = new GameDraft("  Test Game  ", "  PC ", " RPG ", PlayStatus.WantToPlay, null, 0, "  First note  ", false);
var id = Guid.NewGuid();
var built = GameCatalog.Build(draft, id, now);
Check(built.IsSuccess && built.Value!.Title == "Test Game" && built.Value.Platform == "PC" && built.Value.Notes == "First note", "build trims input");
Check(!GameCatalog.Build(draft with { Title = " " }, id, now).IsSuccess, "empty title is rejected");
Check(!GameCatalog.Build(draft with { Score = 11 }, id, now).IsSuccess, "out-of-range score is rejected");
Check(!GameCatalog.Build(draft with { HoursPlayed = -1 }, id, now).IsSuccess, "negative hours are rejected");
Check(!GameCatalog.Build(draft with { Status = (PlayStatus)99 }, id, now).IsSuccess, "invalid status is rejected");
var completed = GameCatalog.Build(draft with { Status = PlayStatus.Completed, Score = 9 }, id, now.AddDays(1), built.Value).Value!;
Check(completed.CompletedOn == new DateOnly(2026, 11, 15) && completed.AddedAt == now, "completion date and original date are preserved");
var reopened = GameCatalog.Build(draft with { Status = PlayStatus.Playing }, id, now.AddDays(2), completed).Value!;
Check(reopened.CompletedOn is null, "reopening clears completion date");
var items = new[] { built.Value!, completed with { Id = Guid.NewGuid(), Title = "Another Game", Genre = "Action" } };
Check(GameCatalog.Query(items, "rPg").Count == 1, "search is case-insensitive");
Check(GameCatalog.Query(items, status: PlayStatus.Completed).Count == 1, "status filter works");
Check(GameCatalog.Summarize(items).AverageScore == 9 && GameCatalog.Summarize(items).Completed == 1, "statistics summarize scored games");
Check(GameCatalog.TopGenres(items).Count == 2, "genre grouping works");
Check(Result<int>.Success(3).Map(x => x * 2).Value == 6, "Result maps success");
Check(!Result<int>.Failure("bad").Map(x => x * 2).IsSuccess, "Result preserves failure");

var directory = Path.Combine(Path.GetTempPath(), "gameshelf-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    var file = Path.Combine(directory, "games.json");
    var repository = new JsonGameRepository(file);
    var seeded = await repository.GetAllAsync();
    Check(seeded.Count == 6 && File.Exists(file), "first load creates editable demo data");
    var saved = await repository.SaveAsync(draft);
    Check(saved.IsSuccess && (await repository.GetAllAsync()).Count == 7, "save persists a game");
    var replacement = await repository.SaveAsync(draft with { Title = "Updated Game" }, saved.Value!.Id);
    Check(replacement.IsSuccess && (await repository.GetAllAsync()).Any(g => g.Title == "Updated Game"), "update persists changes");
    Check(await repository.DeleteAsync(saved.Value.Id) && (await repository.GetAllAsync()).Count == 6, "delete persists removal");
    var writes = Enumerable.Range(0, 12).Select(i => repository.SaveAsync(draft with { Title = $"Concurrent {i}" }));
    await Task.WhenAll(writes);
    Check((await repository.GetAllAsync()).Count == 18, "concurrent writes do not lose entries");
    var reloaded = new JsonGameRepository(file);
    Check((await reloaded.GetAllAsync()).Count == 18, "data survives repository restart");
}
finally
{
    Directory.Delete(directory, recursive: true);
}

Console.WriteLine($"{passed} checks passed.");
