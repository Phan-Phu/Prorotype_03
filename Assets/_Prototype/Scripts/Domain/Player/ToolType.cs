namespace Prototype.Domain
{
    public enum ToolType { Hoe, Seed, WateringCan, Harvest, Chop }

    /// <summary>
    /// Inventory item ids for the non-consumable tools (Hoe/WateringCan/Harvest/Axe). GameState grants
    /// one of each at the start so the hotbar (which now mirrors Inventory row 0 — see ToolbarUI) has
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

    /// <summary>
    /// Result of a tool action. QA asserts the Code in EditMode tests instead of watching the screen.
    /// Feedback carries the visual/audio cue kind so the view layer can flash the right colour/sound.
    /// NotChoppable / Blocked added S2-DEV-03 (DESIGN_BRIEFS.md [DSN-030]): NotChoppable = Chop aimed
    /// at a tile with no live tree (bare ground or an already-felled stump); Blocked = Till/Plant aimed
    /// at a tile occupied by a static object (a standing tree) — kept distinct from WrongTool so QA can
    /// tell "wrong tool for this terrain" apart from "something is physically in the way" in tests.
    /// </summary>
    public enum ToolResultCode { Success, WrongTool, NoStamina, InvalidTile, NoSeed, NotChoppable, Blocked, InventoryFull }

    /// <summary>Chop added S2-DEV-02/04 for FeedbackKind.Chop (DESIGN_BRIEFS.md [DSN-030] S2-DES-04):
    /// every tree hit (including the felling hit) uses this, never Miss.</summary>
    public enum FeedbackKind { Till, Plant, Water, Harvest, Chop, Miss }

    public readonly struct ToolResult
    {
        public readonly ToolResultCode Code;
        public readonly FeedbackKind Feedback;
        public readonly int Amount;   // produce gained on harvest, wood gained on a felling chop, 0 otherwise

        public ToolResult(ToolResultCode code, FeedbackKind feedback, int amount = 0)
        {
            Code = code; Feedback = feedback; Amount = amount;
        }

        public bool IsSuccess => Code == ToolResultCode.Success;

        public static ToolResult Success(FeedbackKind f, int amount = 0) => new ToolResult(ToolResultCode.Success, f, amount);
        public static ToolResult Fail(ToolResultCode c, FeedbackKind f) => new ToolResult(c, f, 0);
    }
}
