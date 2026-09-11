namespace Prototype.Domain
{
    public enum ShopPurchaseResultCode { Success, InsufficientFunds, InventoryFull, InvalidItem, SystemError }
    public enum ShopSellResultCode { Success, EmptyInventory, InvalidCount, InvalidItem, SystemError }

    public readonly struct ShopPurchaseResult
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

        public ShopPurchaseResult(ShopPurchaseResultCode code, CropId crop, string itemId, int price,
            int moneyBefore, int moneyAfter, int countBefore, int countAfter)
        {
            Code = code; Crop = crop; ItemId = itemId; Price = price;
            MoneyBefore = moneyBefore; MoneyAfter = moneyAfter;
            CountBefore = countBefore; CountAfter = countAfter;
        }
    }

    public readonly struct ShopSellResult
    {
        public readonly ShopSellResultCode Code;
        public readonly string ItemId;
        public readonly int PricePerUnit;
        public readonly int RequestedCount;
        public readonly int Earned;
        public readonly int MoneyBefore;
        public readonly int MoneyAfter;
        public readonly int CountBefore;
        public readonly int CountAfter;

        public bool IsSuccess => Code == ShopSellResultCode.Success;

        public ShopSellResult(ShopSellResultCode code, string itemId, int pricePerUnit, int requestedCount,
            int earned, int moneyBefore, int moneyAfter, int countBefore, int countAfter)
        {
            Code = code; ItemId = itemId; PricePerUnit = pricePerUnit; RequestedCount = requestedCount;
            Earned = earned; MoneyBefore = moneyBefore; MoneyAfter = moneyAfter;
            CountBefore = countBefore; CountAfter = countAfter;
        }

        public static implicit operator int(ShopSellResult result) => result.Earned;
    }
}
