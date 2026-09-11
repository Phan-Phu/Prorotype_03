namespace Prototype.Domain
{
    public enum CropId { Turnip, Potato }

    /// <summary>
    /// Crop catalogue. All numeric values come from BalanceConfig ([DESIGN-OWNED]) so the
    /// "no hardcoded numbers" rule holds and Design can retune in one file.
    /// Static fields (Turnip/Potato) exist so tests can write CropDefinition.Turnip.
    /// </summary>
    public static class CropDefinition
    {
        public const CropId Turnip = CropId.Turnip;
        public const CropId Potato = CropId.Potato;

        public static int GrowthDays(CropId id) =>
            id == CropId.Turnip ? Prototype.Domain.BalanceConfig.TurnipGrowthDays
                                : Prototype.Domain.BalanceConfig.PotatoGrowthDays;

        public static int SeedPrice(CropId id) =>
            id == CropId.Turnip ? Prototype.Domain.BalanceConfig.TurnipSeedPrice
                                : Prototype.Domain.BalanceConfig.PotatoSeedPrice;

        public static int SellPrice(CropId id) =>
            id == CropId.Turnip ? Prototype.Domain.BalanceConfig.TurnipSellPrice
                                : Prototype.Domain.BalanceConfig.PotatoSellPrice;

        /// <summary>Inventory item id for this crop's seed (ToolController.Plant consumes 1 to plant).</summary>
        public static string SeedItemId(CropId id) => id == CropId.Turnip ? "turnip_seed" : "potato_seed";

        /// <summary>Inventory item id for harvested produce. Selling remains a separate shop interaction.</summary>
        public static string ProduceItemId(CropId id) => id == CropId.Turnip ? "turnip" : "potato";
    }
}
