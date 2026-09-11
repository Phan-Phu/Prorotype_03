namespace Prototype.Domain
{
    /// <summary>
    /// Tree/wood constants, mirroring CropDefinition's indirection into BalanceConfig
    /// ([DESIGN-OWNED], DESIGN_BRIEFS.md [DSN-030] S2-DES-01). Kept as a static lookup (not
    /// hardcoded on TileObject/ToolController) so Design can retune wood numbers in one place,
    /// same "no hardcoded numbers elsewhere" rule the crop side already follows.
    /// </summary>
    public static class TreeDefinition
    {
        public static int MaxHP        => Prototype.Domain.BalanceConfig.TreeMaxHP;
        public static int RespawnDays  => Prototype.Domain.BalanceConfig.TreeRespawnDays;
        public static int WoodPerTree  => Prototype.Domain.BalanceConfig.WoodPerTree;
        public static int WoodSellPrice => Prototype.Domain.BalanceConfig.WoodSellPrice;
        public static int InitialTreeCount => Prototype.Domain.BalanceConfig.InitialTreeCount;

        /// <summary>Inventory item id for felled wood (S2-DEV-05) — same string-id convention as CropDefinition.SeedItemId.</summary>
        public const string WoodItemId = "wood";
    }
}
