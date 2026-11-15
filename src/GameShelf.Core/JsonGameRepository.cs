using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameShelf.Core;

// All file effects are isolated here; catalog rules and queries remain pure.
public sealed class JsonGameRepository(string filePath, TimeProvider? clock = null) : IGameRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<GameEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return (await ReadAsync(cancellationToken)).ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task<Result<GameEntry>> SaveAsync(
        GameDraft draft, Guid? id = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var games = await ReadAsync(cancellationToken);
            var previous = id.HasValue ? games.FirstOrDefault(game => game.Id == id.Value) : null;
            if (id.HasValue && previous is null)
                return Result<GameEntry>.Failure("This game no longer exists. Refresh the page.");

            var result = GameCatalog.Build(draft, id ?? Guid.NewGuid(), _clock.GetLocalNow(), previous);
            if (!result.IsSuccess) return result;

            var updated = previous is null
                ? games.Append(result.Value!).ToArray()
                : games.Select(game => game.Id == previous.Id ? result.Value! : game).ToArray();
            await WriteAsync(updated, cancellationToken);
            return result;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var games = await ReadAsync(cancellationToken);
            var updated = games.Where(game => game.Id != id).ToArray();
            if (updated.Length == games.Count) return false;
            await WriteAsync(updated, cancellationToken);
            return true;
        }
        finally { _gate.Release(); }
    }

    private async Task<IReadOnlyList<GameEntry>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            var demo = DemoCatalog.Create(_clock.GetLocalNow());
            await WriteAsync(demo, cancellationToken);
            return demo;
        }

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<GameEntry[]>(stream, Options, cancellationToken)
            ?? throw new InvalidDataException("The games data file is empty or invalid.");
    }

    private async Task WriteAsync(IReadOnlyList<GameEntry> games, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        var temporary = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = File.Create(temporary))
                await JsonSerializer.SerializeAsync(stream, games, Options, cancellationToken);
            File.Move(temporary, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
