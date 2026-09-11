using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    public readonly struct ToolActionDto
    {
        public readonly ToolResultCode Code;
        public readonly FeedbackKind Feedback;
        public readonly int Amount;
        public bool IsSuccess => Code == ToolResultCode.Success;

        public ToolActionDto(ToolResultCode code, FeedbackKind feedback, int amount = 0)
        {
            Code = code; Feedback = feedback; Amount = amount;
        }
    }

    public readonly struct ShopPurchaseDto
    {
        public readonly ShopPurchaseResultCode Code;
        public readonly CropId Crop;
        public readonly string ItemId;
        public readonly int Price;
        public readonly int MoneyBefore;
        public readonly int MoneyAfter;
        public readonly int CountBefore;
        public readonly int CountAfter;
        public bool IsSuccess => Code == ShopPurchaseResultCode.Success;

        public ShopPurchaseDto(ShopPurchaseResult result)
        {
            Code = result.Code; Crop = result.Crop; ItemId = result.ItemId; Price = result.Price;
            MoneyBefore = result.MoneyBefore; MoneyAfter = result.MoneyAfter;
            CountBefore = result.CountBefore; CountAfter = result.CountAfter;
        }
    }

    public readonly struct ShopSellDto
    {
        public readonly ShopSellResultCode Code;
        public readonly string ItemId;
        public readonly int Earned;
        public readonly int MoneyBefore;
        public readonly int MoneyAfter;
        public readonly int CountBefore;
        public readonly int CountAfter;
        public bool IsSuccess => Code == ShopSellResultCode.Success;

        public ShopSellDto(ShopSellResult result)
        {
            Code = result.Code; ItemId = result.ItemId; Earned = result.Earned;
            MoneyBefore = result.MoneyBefore; MoneyAfter = result.MoneyAfter;
            CountBefore = result.CountBefore; CountAfter = result.CountAfter;
        }
    }

    public interface IGameplayService
    {
        UniTask<Result<GameplayFailure, ToolActionDto>> UseTool(GameState state, ToolType tool, GridCoord coord);
        UniTask<Result<GameplayFailure, ToolActionDto>> PlantSpecific(GameState state, CropId crop, GridCoord coord);
        UniTask<Result<GameplayFailure, ShopPurchaseDto>> BuySeed(GameState state, CropId crop);
        UniTask<Result<GameplayFailure, ShopSellDto>> SellItem(GameState state, string itemId, int pricePerUnit, int count);
    }
}
