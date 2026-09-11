using Prototype.Domain;

namespace Prototype.Domain
{
    public enum ShopPurchaseResultCode
    {
        Success,
        InsufficientFunds,
        InventoryFull
    }

    public enum ShopSellResultCode
    {
        Success,
        EmptyInventory,
        InvalidCount
    }

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
            Code = code;
            Crop = crop;
            ItemId = itemId;
            Price = price;
            MoneyBefore = moneyBefore;
            MoneyAfter = moneyAfter;
            CountBefore = countBefore;
            CountAfter = countAfter;
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
            Code = code;
            ItemId = itemId;
            PricePerUnit = pricePerUnit;
            RequestedCount = requestedCount;
            Earned = earned;
            MoneyBefore = moneyBefore;
            MoneyAfter = moneyAfter;
            CountBefore = countBefore;
            CountAfter = countAfter;
        }

        public static implicit operator int(ShopSellResult result) => result.Earned;
    }

    /// <summary>
    /// Plain-C# seed shop transaction logic for DSN-040. Checks every failure before spending money so
    /// insufficient funds (and full inventory) never partially mutate Wallet/Inventory.
    /// </summary>
    public static class SeedShop
    {
        public static ShopPurchaseResult BuySeed(Wallet wallet, Prototype.Domain.Inventory inventory, CropId crop)
        {
            string itemId = CropDefinition.SeedItemId(crop);
            int price = CropDefinition.SeedPrice(crop);
            int moneyBefore = wallet.Money;
            int countBefore = inventory.Count(itemId);

            if (moneyBefore < price)
                return Result(ShopPurchaseResultCode.InsufficientFunds, crop, itemId, price, moneyBefore, wallet.Money, countBefore, inventory.Count(itemId));

            if (!inventory.CanAdd(itemId))
                return Result(ShopPurchaseResultCode.InventoryFull, crop, itemId, price, moneyBefore, wallet.Money, countBefore, inventory.Count(itemId));

            wallet.TrySpend(price);
            inventory.Add(itemId);
            return Result(ShopPurchaseResultCode.Success, crop, itemId, price, moneyBefore, wallet.Money, countBefore, inventory.Count(itemId));
        }

        static ShopPurchaseResult Result(ShopPurchaseResultCode code, CropId crop, string itemId, int price,
            int moneyBefore, int moneyAfter, int countBefore, int countAfter)
            => new ShopPurchaseResult(code, crop, itemId, price, moneyBefore, moneyAfter, countBefore, countAfter);
    }
}
