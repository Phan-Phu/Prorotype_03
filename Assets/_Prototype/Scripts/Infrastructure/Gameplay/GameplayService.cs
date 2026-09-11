using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>Infrastructure implementation for tool, shop and economy behavior.</summary>
    public sealed class GameplayService : IGameplayService
    {
        readonly Prototype.Domain.IInventoryService _inventory;

        public GameplayService(Prototype.Domain.IInventoryService inventory) => _inventory = inventory;

        public ToolActionDto UseTool(GameState state, ToolType tool, GridCoord coord)
        {
            if (state == null || !state.Grid.InBounds(coord)) return Fail(ToolResultCode.InvalidTile);
            if (state.Grid.IsStaticBlocked(coord)) return Fail(ToolResultCode.Blocked);
            var tile = state.Grid.GetTile(coord);
            switch (tool)
            {
                case ToolType.Hoe: return Till(state, tile);
                case ToolType.Seed: return Plant(state, tile);
                case ToolType.WateringCan: return Water(state, tile);
                case ToolType.Harvest: return Harvest(state, tile);
                case ToolType.Chop: return Chop(state, tile);
                default: return Fail(ToolResultCode.WrongTool);
            }
        }

        public UniTask<ToolActionDto> UseToolAsync(GameState state, ToolType tool, GridCoord coord)
            => UniTask.FromResult(UseTool(state, tool, coord));

        public ToolActionDto PlantSpecific(GameState state, CropId crop, GridCoord coord)
        {
            if (state == null || !state.Grid.InBounds(coord)) return Fail(ToolResultCode.InvalidTile);
            var tile = state.Grid.GetTile(coord);
            if (!CanPlant(tile, out var failure)) return failure;
            if (!_inventory.Remove(state.InventorySystem, CropDefinition.SeedItemId(crop)).GetAwaiter().GetResult())
                return Fail(ToolResultCode.NoSeed);
            tile.Crop = new CropInstance(crop);
            return Success(FeedbackKind.Plant);
        }

        public ShopPurchaseDto BuySeed(GameState state, CropId crop)
        {
            string itemId = CropDefinition.SeedItemId(crop);
            int price = CropDefinition.SeedPrice(crop);
            int moneyBefore = state.Wallet.Money;
            int countBefore = _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult();
            ShopPurchaseResultCode code;

            if (moneyBefore < price) code = ShopPurchaseResultCode.InsufficientFunds;
            else if (!_inventory.CanAdd(state.InventorySystem, itemId).GetAwaiter().GetResult()) code = ShopPurchaseResultCode.InventoryFull;
            else
            {
                state.Wallet.Money -= price;
                _inventory.Add(state.InventorySystem, itemId).GetAwaiter().GetResult();
                code = ShopPurchaseResultCode.Success;
            }

            var result = new ShopPurchaseResult(code, crop, itemId, price, moneyBefore, state.Wallet.Money,
                countBefore, _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult());
            return new ShopPurchaseDto(result);
        }

        public UniTask<ShopPurchaseDto> BuySeedAsync(GameState state, CropId crop)
            => UniTask.FromResult(BuySeed(state, crop));

        public ShopSellDto SellItem(GameState state, string itemId, int pricePerUnit, int count)
        {
            int moneyBefore = state.Wallet.Money;
            int countBefore = _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult();
            if (count <= 0 || countBefore < count || !_inventory.Remove(state.InventorySystem, itemId, count).GetAwaiter().GetResult())
                return new ShopSellDto(new ShopSellResult(ShopSellResultCode.EmptyInventory, itemId,
                    pricePerUnit, count, 0, moneyBefore, moneyBefore, countBefore,
                    _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult()));

            int earned = pricePerUnit * count;
            state.Wallet.Money += earned;
            return new ShopSellDto(new ShopSellResult(ShopSellResultCode.Success, itemId, pricePerUnit,
                count, earned, moneyBefore, state.Wallet.Money, countBefore,
                _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult()));
        }

        public UniTask<ShopSellDto> SellItemAsync(GameState state, string itemId, int pricePerUnit, int count)
            => UniTask.FromResult(SellItem(state, itemId, pricePerUnit, count));

        ToolActionDto Till(GameState state, TileData tile)
        {
            if (tile.Object != null) return Fail(ToolResultCode.Blocked);
            if (tile.Type != TileType.Grass) return Fail(ToolResultCode.WrongTool);
            if (!Spend(state, BalanceConfig.TillStaminaCost)) return Fail(ToolResultCode.NoStamina);
            tile.Type = TileType.Tilled;
            return Success(FeedbackKind.Till);
        }

        ToolActionDto Chop(GameState state, TileData tile)
        {
            if (tile.Object == null || tile.Object.Type != TileObjectType.Tree || !tile.Object.IsAlive)
                return Fail(ToolResultCode.NotChoppable);
            if (!Spend(state, BalanceConfig.ChopStaminaCost)) return Fail(ToolResultCode.NoStamina);
            tile.Object.HP--;
            if (tile.Object.HP > 0) return Success(FeedbackKind.Chop);
            tile.Object.RespawnDaysLeft = TreeDefinition.RespawnDays;
            _inventory.Add(state.InventorySystem, TreeDefinition.WoodItemId, TreeDefinition.WoodPerTree).GetAwaiter().GetResult();
            return Success(FeedbackKind.Chop, TreeDefinition.WoodPerTree);
        }

        ToolActionDto Plant(GameState state, TileData tile)
        {
            if (!CanPlant(tile, out var failure)) return failure;
            CropId? crop = null;
            if (_inventory.Remove(state.InventorySystem, CropDefinition.SeedItemId(CropId.Turnip)).GetAwaiter().GetResult()) crop = CropId.Turnip;
            else if (_inventory.Remove(state.InventorySystem, CropDefinition.SeedItemId(CropId.Potato)).GetAwaiter().GetResult()) crop = CropId.Potato;
            if (!crop.HasValue) return Fail(ToolResultCode.NoSeed);
            tile.Crop = new CropInstance(crop.Value);
            return Success(FeedbackKind.Plant);
        }

        ToolActionDto Water(GameState state, TileData tile)
        {
            if (tile.Type != TileType.Tilled) return Fail(ToolResultCode.WrongTool);
            if (tile.IsWatered) return Success(FeedbackKind.Water);
            if (!Spend(state, BalanceConfig.WaterStaminaCost)) return Fail(ToolResultCode.NoStamina);
            tile.IsWatered = true;
            if (tile.Crop != null) tile.Crop.WateredToday = true;
            return Success(FeedbackKind.Water);
        }

        ToolActionDto Harvest(GameState state, TileData tile)
        {
            if (tile.Crop == null || !tile.Crop.IsRipe) return Fail(ToolResultCode.WrongTool);
            string itemId = CropDefinition.ProduceItemId(tile.Crop.Id);
            if (!_inventory.CanAdd(state.InventorySystem, itemId).GetAwaiter().GetResult()) return Fail(ToolResultCode.InventoryFull);
            _inventory.Add(state.InventorySystem, itemId).GetAwaiter().GetResult();
            tile.Crop = null;
            return Success(FeedbackKind.Harvest, 1);
        }

        static bool CanPlant(TileData tile, out ToolActionDto failure)
        {
            if (tile.Object != null) { failure = Fail(ToolResultCode.Blocked); return false; }
            if (tile.Type != TileType.Tilled || tile.Crop != null) { failure = Fail(ToolResultCode.WrongTool); return false; }
            failure = default;
            return true;
        }

        static bool Spend(GameState state, int amount)
        {
            if (state.Stamina.Current < amount) return false;
            state.Stamina.Current -= amount;
            return true;
        }

        static ToolActionDto Success(FeedbackKind feedback, int amount = 0)
            => new ToolActionDto(ToolResultCode.Success, feedback, amount);

        static ToolActionDto Fail(ToolResultCode code)
            => new ToolActionDto(code, FeedbackKind.Miss);
    }
}
