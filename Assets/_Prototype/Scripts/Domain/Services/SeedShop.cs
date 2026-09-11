using Prototype.Domain;

namespace Prototype.Domain
{
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
