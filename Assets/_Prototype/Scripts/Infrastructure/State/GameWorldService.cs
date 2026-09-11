using System;
using Cysharp.Threading.Tasks;
using Prototype.Application;
using Prototype.Domain;

namespace Prototype.Infrastructure
{
    /// <summary>Infrastructure implementation of world queries and debug mutations.</summary>
    public sealed class GameWorldService : IGameWorldService
    {
        public UniTask<Result<WorldFailure, GridCoord>> SeedShopCoord(GameState state)
        {
            if (state?.Grid == null)
                return ResultFactory.UniTaskFailure<WorldFailure, GridCoord>(WorldFailure.NotInitialized());
            return ResultFactory.UniTaskSuccess<WorldFailure, GridCoord>(
                new GridCoord(Math.Max(0, state.Grid.Width / 2 - 4), state.Grid.Height - 1));
        }

        public UniTask<Result<WorldFailure, bool>> IsSeedShopTile(GameState state, GridCoord coord)
        {
            if (state?.Grid == null)
                return ResultFactory.UniTaskFailure<WorldFailure, bool>(WorldFailure.NotInitialized());
            var shopCoord = new GridCoord(Math.Max(0, state.Grid.Width / 2 - 4), state.Grid.Height - 1);
            return ResultFactory.UniTaskSuccess<WorldFailure, bool>(coord.Equals(shopCoord));
        }

        public UniTask<Result<WorldFailure, Unit>> SetMoney(GameState state, int amount)
        {
            if (state?.Wallet == null)
                return ResultFactory.UniTaskFailure<WorldFailure>(WorldFailure.NotInitialized("wallet"));
            if (amount < 0)
                return ResultFactory.UniTaskFailure<WorldFailure>(WorldFailure.InvalidArgument("money"));
            return Safe("world.set_money", () => state.Wallet.Money = amount);
        }

        public UniTask<Result<WorldFailure, Unit>> RefillStamina(GameState state)
        {
            if (state?.Stamina == null)
                return ResultFactory.UniTaskFailure<WorldFailure>(WorldFailure.NotInitialized("stamina"));
            return Safe("world.refill_stamina", () => state.Stamina.Current = state.Stamina.Max);
        }

        public UniTask<Result<WorldFailure, bool>> DebugSpawnTree(GameState state, GridCoord coord)
        {
            if (state?.Grid == null)
                return ResultFactory.UniTaskFailure<WorldFailure, bool>(WorldFailure.NotInitialized());
            return SafeBool("world.spawn_tree", () =>
            {
                var tile = state.Grid.GetTile(coord);
                if (tile == null || tile.Type != TileType.Grass || tile.Object != null) return false;
                tile.Object = TileObject.NewTree(state.MasterData.Tree.MaxHP);
                return true;
            });
        }

        public UniTask<Result<WorldFailure, Unit>> ForceRespawnTrees(GameState state)
        {
            if (state?.Grid == null)
                return ResultFactory.UniTaskFailure<WorldFailure>(WorldFailure.NotInitialized());
            return Safe("world.respawn_trees", () =>
            {
                foreach (var tile in state.Grid.AllTiles())
                    if (tile.Object != null && !tile.Object.IsAlive)
                    {
                        tile.Object.HP = state.MasterData.Tree.MaxHP;
                        tile.Object.RespawnDaysLeft = 0;
                    }
            });
        }

        public UniTask<Result<WorldFailure, Unit>> ForceRipeAll(GameState state)
        {
            if (state?.Grid == null)
                return ResultFactory.UniTaskFailure<WorldFailure>(WorldFailure.NotInitialized());
            return Safe("world.ripe_all", () =>
            {
                foreach (var tile in state.Grid.AllTiles())
                    if (tile.Crop != null)
                    {
                        var crop = state.MasterData.GetCrop(tile.Crop.Id);
                        if (crop != null) tile.Crop.DaysGrown = crop.GrowthDays;
                    }
            });
        }

        static UniTask<Result<WorldFailure, Unit>> Safe(string context, Action operation)
        {
            try
            {
                operation();
                return ResultFactory.UniTaskSuccess<WorldFailure, Unit>(Unit.Value);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                return ResultFactory.UniTaskFailure<WorldFailure>(WorldFailure.System(context));
            }
        }

        static UniTask<Result<WorldFailure, bool>> SafeBool(string context, Func<bool> operation)
        {
            try
            {
                return ResultFactory.UniTaskSuccess<WorldFailure, bool>(operation());
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                return ResultFactory.UniTaskFailure<WorldFailure, bool>(WorldFailure.System(context));
            }
        }
    }
}
