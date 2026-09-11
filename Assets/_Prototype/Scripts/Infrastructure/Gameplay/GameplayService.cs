using System;
using Cysharp.Threading.Tasks;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Infrastructure implementation for tool, shop and economy behavior. Public operations are
    /// UniTask-based and always complete with an explicit success or failure result.
    /// </summary>
    public sealed class GameplayService : IGameplayService
    {
        readonly Prototype.Domain.IInventoryService _inventory;

        public GameplayService(Prototype.Domain.IInventoryService inventory) => _inventory = inventory;

        public UniTask<OperationResult<ToolActionDto>> UseTool(GameState state, ToolType tool, GridCoord coord)
            => UniTask.FromResult(SafeUseTool(state, tool, coord));

        public UniTask<OperationResult<ToolActionDto>> PlantSpecific(GameState state, CropId crop, GridCoord coord)
            => UniTask.FromResult(SafePlantSpecific(state, crop, coord));

        public UniTask<OperationResult<ShopPurchaseDto>> BuySeed(GameState state, CropId crop)
            => UniTask.FromResult(SafeBuySeed(state, crop));

        public UniTask<OperationResult<ShopSellDto>> SellItem(GameState state, string itemId, int pricePerUnit, int count)
            => UniTask.FromResult(SafeSellItem(state, itemId, pricePerUnit, count));

        OperationResult<ToolActionDto> SafeUseTool(GameState state, ToolType tool, GridCoord coord)
        {
            try
            {
                var dto = UseToolInternal(state, tool, coord);
                return ToToolResult(dto, "gameplay.use_tool");
            }
            catch (Exception ex)
            {
                return SystemFailure<ToolActionDto>("gameplay.use_tool", ex, Fail(ToolResultCode.WrongTool));
            }
        }

        OperationResult<ToolActionDto> SafePlantSpecific(GameState state, CropId crop, GridCoord coord)
        {
            try
            {
                var dto = PlantSpecificInternal(state, crop, coord);
                return ToToolResult(dto, "gameplay.plant_specific");
            }
            catch (Exception ex)
            {
                return SystemFailure<ToolActionDto>("gameplay.plant_specific", ex, Fail(ToolResultCode.WrongTool));
            }
        }

        OperationResult<ShopPurchaseDto> SafeBuySeed(GameState state, CropId crop)
        {
            try
            {
                if (state == null) return OperationResult<ShopPurchaseDto>.Failed(FailureCode.NotInitialized, "game_state");

                string itemId = CropDefinition.SeedItemId(crop);
                int price = CropDefinition.SeedPrice(crop);
                int moneyBefore = state.Wallet.Money;
                var countBeforeResult = _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult();
                if (!countBeforeResult.IsSuccess) return Propagate<ShopPurchaseDto>(countBeforeResult, itemId);

                int countBefore = countBeforeResult.Data;
                if (moneyBefore < price)
                    return OperationResult<ShopPurchaseDto>.Failed(FailureCode.NotEnoughMoney, itemId, price, moneyBefore);

                var canAdd = _inventory.CanAdd(state.InventorySystem, itemId).GetAwaiter().GetResult();
                if (!canAdd.IsSuccess) return Propagate<ShopPurchaseDto>(canAdd, itemId);

                var add = _inventory.Add(state.InventorySystem, itemId).GetAwaiter().GetResult();
                if (!add.IsSuccess) return Propagate<ShopPurchaseDto>(add, itemId);

                state.Wallet.Money -= price;
                var result = new ShopPurchaseResult(
                    ShopPurchaseResultCode.Success, crop, itemId, price,
                    moneyBefore, state.Wallet.Money, countBefore, countBefore + 1);
                return OperationResult<ShopPurchaseDto>.Success(new ShopPurchaseDto(result));
            }
            catch (Exception ex)
            {
                return SystemFailure<ShopPurchaseDto>("gameplay.buy_seed", ex);
            }
        }

        OperationResult<ShopSellDto> SafeSellItem(GameState state, string itemId, int pricePerUnit, int count)
        {
            try
            {
                if (state == null) return OperationResult<ShopSellDto>.Failed(FailureCode.NotInitialized, "game_state");
                if (string.IsNullOrEmpty(itemId) || pricePerUnit < 0 || count <= 0)
                    return OperationResult<ShopSellDto>.Failed(FailureCode.InvalidArgument, itemId);

                int moneyBefore = state.Wallet.Money;
                var countBeforeResult = _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult();
                if (!countBeforeResult.IsSuccess) return Propagate<ShopSellDto>(countBeforeResult, itemId);
                int countBefore = countBeforeResult.Data;
                if (countBefore < count)
                    return OperationResult<ShopSellDto>.Failed(FailureCode.InsufficientInventory, itemId, count, countBefore);

                var remove = _inventory.Remove(state.InventorySystem, itemId, count).GetAwaiter().GetResult();
                if (!remove.IsSuccess) return Propagate<ShopSellDto>(remove, itemId);

                int earned = pricePerUnit * count;
                state.Wallet.Money += earned;
                var result = new ShopSellResult(
                    ShopSellResultCode.Success, itemId, pricePerUnit, count, earned,
                    moneyBefore, state.Wallet.Money, countBefore, countBefore - count);
                return OperationResult<ShopSellDto>.Success(new ShopSellDto(result));
            }
            catch (Exception ex)
            {
                return SystemFailure<ShopSellDto>("gameplay.sell_item", ex);
            }
        }

        ToolActionDto UseToolInternal(GameState state, ToolType tool, GridCoord coord)
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

        ToolActionDto PlantSpecificInternal(GameState state, CropId crop, GridCoord coord)
        {
            if (state == null || !state.Grid.InBounds(coord)) return Fail(ToolResultCode.InvalidTile);
            var tile = state.Grid.GetTile(coord);
            if (!CanPlant(tile, out var failure)) return failure;
            var remove = _inventory.Remove(state.InventorySystem, CropDefinition.SeedItemId(crop)).GetAwaiter().GetResult();
            if (!remove.IsSuccess) return Fail(MapInventoryFailure(remove.FailureCode, ToolResultCode.NoSeed));
            tile.Crop = new CropInstance(crop);
            return Success(FeedbackKind.Plant);
        }

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
            if (tile.Object.HP <= 1
                && !_inventory.CanAdd(state.InventorySystem, TreeDefinition.WoodItemId).GetAwaiter().GetResult().IsSuccess)
                return Fail(ToolResultCode.InventoryFull);

            tile.Object.HP--;
            if (tile.Object.HP > 0) return Success(FeedbackKind.Chop);
            tile.Object.RespawnDaysLeft = TreeDefinition.RespawnDays;
            var add = _inventory.Add(state.InventorySystem, TreeDefinition.WoodItemId, TreeDefinition.WoodPerTree).GetAwaiter().GetResult();
            if (!add.IsSuccess) return Fail(MapInventoryFailure(add.FailureCode, ToolResultCode.SystemError));
            return Success(FeedbackKind.Chop, TreeDefinition.WoodPerTree);
        }

        ToolActionDto Plant(GameState state, TileData tile)
        {
            if (!CanPlant(tile, out var failure)) return failure;
            CropId? crop = null;
            if (_inventory.Remove(state.InventorySystem, CropDefinition.SeedItemId(CropId.Turnip)).GetAwaiter().GetResult().IsSuccess) crop = CropId.Turnip;
            else if (_inventory.Remove(state.InventorySystem, CropDefinition.SeedItemId(CropId.Potato)).GetAwaiter().GetResult().IsSuccess) crop = CropId.Potato;
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
            if (!_inventory.CanAdd(state.InventorySystem, itemId).GetAwaiter().GetResult().IsSuccess) return Fail(ToolResultCode.InventoryFull);
            var add = _inventory.Add(state.InventorySystem, itemId).GetAwaiter().GetResult();
            if (!add.IsSuccess) return Fail(MapInventoryFailure(add.FailureCode, ToolResultCode.SystemError));
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

        static OperationResult<ToolActionDto> ToToolResult(ToolActionDto dto, string context)
            => dto.IsSuccess
                ? OperationResult<ToolActionDto>.Success(dto)
                : OperationResult<ToolActionDto>.Failed(MapFailure(dto.Code), context, data: dto);

        static FailureCode MapFailure(ToolResultCode code)
        {
            switch (code)
            {
                case ToolResultCode.InvalidTile: return FailureCode.InvalidTile;
                case ToolResultCode.NoStamina: return FailureCode.NoStamina;
                case ToolResultCode.NoSeed: return FailureCode.NoSeed;
                case ToolResultCode.NotChoppable: return FailureCode.NotChoppable;
                case ToolResultCode.Blocked: return FailureCode.Blocked;
                case ToolResultCode.InventoryFull: return FailureCode.InventoryFull;
                case ToolResultCode.SystemError: return FailureCode.SystemError;
                default: return FailureCode.WrongTool;
            }
        }

        static ToolResultCode MapInventoryFailure(FailureCode code, ToolResultCode fallback)
        {
            switch (code)
            {
                case FailureCode.InventoryFull:
                case FailureCode.LockedSlot: return ToolResultCode.InventoryFull;
                case FailureCode.SystemError: return ToolResultCode.SystemError;
                case FailureCode.ItemNotFound:
                case FailureCode.InsufficientInventory: return ToolResultCode.NoSeed;
                default: return fallback;
            }
        }

        static OperationResult<T> Propagate<T>(OperationResult source, string context)
            => OperationResult<T>.Failed(source.FailureCode, context, source.Error.Expected, source.Error.Actual);

        static OperationResult<T> Propagate<T>(OperationResult<int> source, string context)
            => OperationResult<T>.Failed(source.FailureCode, context, source.Error.Expected, source.Error.Actual);

        static OperationResult<T> SystemFailure<T>(string context, Exception exception, T data = default(T))
        {
            Debug.LogException(exception);
            return OperationResult<T>.Failed(FailureCode.SystemError, context, data: data);
        }

        static ToolActionDto Success(FeedbackKind feedback, int amount = 0)
            => new ToolActionDto(ToolResultCode.Success, feedback, amount);

        static ToolActionDto Fail(ToolResultCode code)
            => new ToolActionDto(code, FeedbackKind.Miss);
    }
}
