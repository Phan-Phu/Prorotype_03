using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>Owns world-level runtime/debug behavior outside raw Domain entities.</summary>
    public sealed class GameWorldService : IGameWorldService
    {
        public GridCoord SeedShopCoord(GameState state)
            => new GridCoord(System.Math.Max(0, state.Grid.Width / 2 - 4), state.Grid.Height - 1);

        public bool IsSeedShopTile(GameState state, GridCoord coord)
            => state != null && coord.Equals(SeedShopCoord(state));

        public void SetMoney(GameState state, int amount)
        {
            if (state != null && state.Wallet != null) state.Wallet.Money = amount;
        }

        public void RefillStamina(GameState state)
        {
            if (state != null && state.Stamina != null) state.Stamina.Current = state.Stamina.Max;
        }

        public bool DebugSpawnTree(GameState state, GridCoord coord)
        {
            if (state == null) return false;
            var tile = state.Grid.GetTile(coord);
            if (tile == null || tile.Type != TileType.Grass || tile.Object != null) return false;
            tile.Object = TileObject.NewTree();
            return true;
        }

        public void ForceRespawnTrees(GameState state)
        {
            if (state == null) return;
            foreach (var tile in state.Grid.AllTiles())
                if (tile.Object != null && !tile.Object.IsAlive)
                {
                    tile.Object.HP = TreeDefinition.MaxHP;
                    tile.Object.RespawnDaysLeft = 0;
                }
        }

        public void ForceRipeAll(GameState state)
        {
            if (state == null) return;
            foreach (var tile in state.Grid.AllTiles())
                if (tile.Crop != null) tile.Crop.DaysGrown = CropDefinition.GrowthDays(tile.Crop.Id);
        }

        public UniTask ForceRespawnTreesAsync(GameState state)
        {
            ForceRespawnTrees(state);
            return UniTask.CompletedTask;
        }

        public UniTask ForceRipeAllAsync(GameState state)
        {
            ForceRipeAll(state);
            return UniTask.CompletedTask;
        }
    }
}
