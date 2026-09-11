using System;
using Cysharp.Threading.Tasks;
using Prototype.Application;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Infrastructure
{
    /// <summary>
    /// Infrastructure implementation for tool, shop and economy behavior. Public operations are
    /// UniTask-based and return the feature-specific GameplayFailure through the generic Result.
    /// </summary>
    public sealed class GameplayService : IGameplayService
    {
        readonly Prototype.Domain.IInventoryService _inventory;

        public GameplayService(Prototype.Domain.IInventoryService inventory) => _inventory = inventory;

        public UniTask<Result<GameplayFailure, ToolActionDto>> UseTool(GameState state, ToolType tool, GridCoord coord)
            => UniTask.FromResult(SafeUseTool(state, tool, coord));

        public UniTask<Result<GameplayFailure, ToolActionDto>> PlantSpecific(GameState state, CropId crop, GridCoord coord)
            => UniTask.FromResult(SafePlantSpecific(state, crop, coord));

        public UniTask<Result<GameplayFailure, ShopPurchaseDto>> BuySeed(GameState state, CropId crop)
            => UniTask.FromResult(SafeBuySeed(state, crop));

        public UniTask<Result<GameplayFailure, ShopSellDto>> SellItem(GameState state, string itemId, int pricePerUnit, int count)
            => UniTask.FromResult(SafeSellItem(state, itemId, pricePerUnit, count));

        Result<GameplayFailure, ToolActionDto> SafeUseTool(GameState state, ToolType tool, GridCoord coord)
        {
            try { return ToToolResult(UseToolInternal(state, tool, coord), "gameplay.use_tool"); }
            catch (Exception ex) { return SystemFailure("gameplay.use_tool", ex, Fail(ToolResultCode.SystemError)); }
        }

        Result<GameplayFailure, ToolActionDto> SafePlantSpecific(GameState state, CropId crop, GridCoord coord)
        {
            try { return ToToolResult(PlantSpecificInternal(state, crop, coord), "gameplay.plant_specific"); }
            catch (Exception ex) { return SystemFailure("gameplay.plant_specific", ex, Fail(ToolResultCode.SystemError)); }
        }

        Result<GameplayFailure, ShopPurchaseDto> SafeBuySeed(GameState state, CropId crop)
        {
            try
            {
                if (state == null)
                    return ResultFactory.Failure<GameplayFailure, ShopPurchaseDto>(GameplayFailure.NotInitialized("game_state"));

                string itemId = CropDefinition.SeedItemId(crop);
                int price = CropDefinition.SeedPrice(crop);
                int moneyBefore = state.Wallet.Money;
                var countBefore = _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult();
                if (!countBefore.IsSuccess) return Propagate<ShopPurchaseDto>(countBefore, itemId);

                if (moneyBefore < price)
                    return ResultFactory.Failure<GameplayFailure, ShopPurchaseDto>(GameplayFailure.NotEnoughMoney(itemId, price, moneyBefore));

                var canAdd = _inventory.CanAdd(state.InventorySystem, itemId).GetAwaiter().GetResult();
                if (!canAdd.IsSuccess) return Propagate<ShopPurchaseDto>(canAdd, itemId);

                var add = _inventory.Add(state.InventorySystem, itemId).GetAwaiter().GetResult();
                if (!add.IsSuccess) return Propagate<ShopPurchaseDto>(add, itemId);

                state.Wallet.Money -= price;
                var result = new ShopPurchaseResult(
                    ShopPurchaseResultCode.Success, crop, itemId, price,
                    moneyBefore, state.Wallet.Money, countBefore.Value, countBefore.Value + 1);
                return ResultFactory.Success<GameplayFailure, ShopPurchaseDto>(new ShopPurchaseDto(result));
            }
            catch (Exception ex) { return SystemFailure<ShopPurchaseDto>("gameplay.buy_seed", ex); }
        }

        Result<GameplayFailure, ShopSellDto> SafeSellItem(GameState state, string itemId, int pricePerUnit, int count)
        {
            try
            {
                if (state == null)
                    return ResultFactory.Failure<GameplayFailure, ShopSellDto>(GameplayFailure.NotInitialized("game_state"));
                if (string.IsNullOrEmpty(itemId) || pricePerUnit < 0 || count <= 0)
                    return ResultFactory.Failure<GameplayFailure, ShopSellDto>(GameplayFailure.InvalidArgument(itemId));

                int moneyBefore = state.Wallet.Money;
                var countBefore = _inventory.Count(state.InventorySystem, itemId).GetAwaiter().GetResult();
                if (!countBefore.IsSuccess) return Propagate<ShopSellDto>(countBefore, itemId);
                if (countBefore.Value < count)
                    return ResultFactory.Failure<GameplayFailure, ShopSellDto>(GameplayFailure.InsufficientInventory(itemId, count, countBefore.Value));

                var remove = _inventory.Remove(state.InventorySystem, itemId, count).GetAwaiter().GetResult();
                if (!remove.IsSuccess) return Propagate<ShopSellDto>(remove, itemId);

                int earned = pricePerUnit * count;
                state.Wallet.Money += earned;
                var result = new ShopSellResult(
                    ShopSellResultCode.Success, itemId, pricePerUnit, count, earned,
                    moneyBefore, state.Wallet.Money, countBefore.Value, countBefore.Value - count);
                return ResultFactory.Success<GameplayFailure, ShopSellDto>(new ShopSellDto(result));
            }
            catch (Exception ex) { return SystemFailure<ShopSellDto>("gameplay.sell_item", ex); }
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
            if (!remove.IsSuccess) return Fail(MapInventoryFailure(remove.Failure.Code, ToolResultCode.NoSeed));
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
            if (tile.Object.HP <= 1)
            {
                var canAdd = _inventory.CanAdd(state.InventorySystem, TreeDefinition.WoodItemId).GetAwaiter().GetResult();
                if (!canAdd.IsSuccess) return Fail(MapInventoryFailure(canAdd.Failure.Code, ToolResultCode.InventoryFull));
            }

            tile.Object.HP--;
            if (tile.Object.HP > 0) return Success(FeedbackKind.Chop);
            tile.Object.RespawnDaysLeft = TreeDefinition.RespawnDays;
            var add = _inventory.Add(state.InventorySystem, TreeDefinition.WoodItemId, TreeDefinition.WoodPerTree).GetAwaiter().GetResult();
            if (!add.IsSuccess) return Fail(MapInventoryFailure(add.Failure.Code, ToolResultCode.SystemError));
            return Success(FeedbackKind.Chop, TreeDefinition.WoodPerTree);
        }

        ToolActionDto Plant(GameState state, TileData tile)
        {
            if (!CanPlant(tile, out var failure)) return failure;
            CropId? crop = null;
            var turnip = _inventory.Remove(state.InventorySystem, CropDefinition.SeedItemId(CropId.Turnip)).GetAwaiter().GetResult();
            if (turnip.IsSuccess) crop = CropId.Turnip;
            else
            {
                var potato = _inventory.Remove(state.InventorySystem, CropDefinition.SeedItemId(CropId.Potato)).GetAwaiter().GetResult();
                if (potato.IsSuccess) crop = CropId.Potato;
                else if (turnip.Failure.Code == FailureCode.SystemError || potato.Failure.Code == FailureCode.SystemError)
                    return Fail(ToolResultCode.SystemError);
            }
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
            var canAdd = _inventory.CanAdd(state.InventorySystem, itemId).GetAwaiter().GetResult();
            if (!canAdd.IsSuccess) return Fail(MapInventoryFailure(canAdd.Failure.Code, ToolResultCode.InventoryFull));
            var add = _inventory.Add(state.InventorySystem, itemId).GetAwaiter().GetResult();
            if (!add.IsSuccess) return Fail(MapInventoryFailure(add.Failure.Code, ToolResultCode.SystemError));
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

        static Result<GameplayFailure, ToolActionDto> ToToolResult(ToolActionDto dto, string context)
            => dto.IsSuccess
                ? ResultFactory.Success<GameplayFailure, ToolActionDto>(dto)
                : ResultFactory.Failure<GameplayFailure, ToolActionDto>(GameplayFailure.Tool(MapFailure(dto.Code), context), dto);

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

        static Result<GameplayFailure, TValue> Propagate<TValue>(Result<InventoryFailure, Unit> source, string context)
            => ResultFactory.Failure<GameplayFailure, TValue>(GameplayFailure.FromInventory(source.Failure, context));

        static Result<GameplayFailure, TValue> Propagate<TValue>(Result<InventoryFailure, int> source, string context)
            => ResultFactory.Failure<GameplayFailure, TValue>(GameplayFailure.FromInventory(source.Failure, context));

        static Result<GameplayFailure, TValue> SystemFailure<TValue>(string context, Exception exception, TValue data = default(TValue))
        {
            Debug.LogException(exception);
            return ResultFactory.Failure<GameplayFailure, TValue>(GameplayFailure.System(context), data);
        }

        static ToolActionDto Success(FeedbackKind feedback, int amount = 0)
            => new ToolActionDto(ToolResultCode.Success, feedback, amount);

        static ToolActionDto Fail(ToolResultCode code)
            => new ToolActionDto(code, FeedbackKind.Miss);
    }
}
