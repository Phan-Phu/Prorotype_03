using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    public interface IGameWorldService
    {
        UniTask<Result<WorldFailure, GridCoord>> SeedShopCoord(GameState state);
        UniTask<Result<WorldFailure, bool>> IsSeedShopTile(GameState state, GridCoord coord);
        UniTask<Result<WorldFailure, Unit>> SetMoney(GameState state, int amount);
        UniTask<Result<WorldFailure, Unit>> RefillStamina(GameState state);
        UniTask<Result<WorldFailure, bool>> DebugSpawnTree(GameState state, GridCoord coord);
        UniTask<Result<WorldFailure, Unit>> ForceRespawnTrees(GameState state);
        UniTask<Result<WorldFailure, Unit>> ForceRipeAll(GameState state);
    }
}
