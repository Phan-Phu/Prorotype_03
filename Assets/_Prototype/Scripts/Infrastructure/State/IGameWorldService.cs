using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>Infrastructure boundary for world queries and debug/runtime state mutations.</summary>
    public interface IGameWorldService
    {
        GridCoord SeedShopCoord(GameState state);
        bool IsSeedShopTile(GameState state, GridCoord coord);
        void SetMoney(GameState state, int amount);
        void RefillStamina(GameState state);
        bool DebugSpawnTree(GameState state, GridCoord coord);
        void ForceRespawnTrees(GameState state);
        void ForceRipeAll(GameState state);
        UniTask ForceRespawnTreesAsync(GameState state);
        UniTask ForceRipeAllAsync(GameState state);
    }
}
