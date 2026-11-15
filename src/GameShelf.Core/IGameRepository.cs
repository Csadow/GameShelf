namespace GameShelf.Core;

public interface IGameRepository
{
    Task<IReadOnlyList<GameEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<GameEntry>> SaveAsync(GameDraft draft, Guid? id = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
