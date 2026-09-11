namespace Prototype.Domain
{
    public enum ToolType { Hoe, Seed, WateringCan, Harvest, Chop }

    /// <summary>
    /// Inventory item ids for the non-consumable tools (Hoe/WateringCan/Harvest/Axe). GameState grants
    /// one of each at the start so the hotbar (which now mirrors Inventory row 0 — see ToolbarCanvasUI) has
    /// something to show; PlayerController resolves whichever item sits in the active slot back to an
    /// action. Seed isn't here: it's not a fixed tool, it's however many turnip_seed/potato_seed stacks
    /// the player is actually carrying (see CropDefinition.SeedItemId).
    /// Axe (S2-DEV-02, DESIGN_BRIEFS.md [DSN-030]): resolves through the same ResolveAction path as
    /// every other tool — there is no separate "current tool" enum/selection added for it.
    /// </summary>
    public static class ToolItemIds
    {
        public const string Hoe = "tool_hoe";
        public const string WateringCan = "tool_wateringcan";
        public const string Harvest = "tool_harvest";
        public const string Axe = "tool_axe";
    }

}
