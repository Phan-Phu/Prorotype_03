using UnityEngine;

namespace Prototype.Infrastructure
{
    /// <summary>
    /// Real pixel-art sprite references, baked once (Editor-only) by
    /// Prototype.Application.Editor.CIArt.BuildArtCatalog into Resources/ArtCatalog.asset, then loaded
    /// at runtime via Resources.Load. Indirection exists because the source art lives under
    /// Assets/Sprite Textures/ (not a Resources folder) and runtime code cannot use AssetDatabase
    /// (Editor-only API) to find it directly.
    /// </summary>
    public class ArtCatalog : ScriptableObject
    {
        /// <summary>ParsnipCrop_0..4 (smallest -> ripe), left-to-right order in the source sheet.</summary>
        public Sprite[] CropStages;

        /// <summary>
        /// DugGround_10 — the one sub-sprite in the 32-piece autotile set measured (not guessed) to be
        /// 100% opaque (every other piece has a transparent edge/corner meant for neighbour blending,
        /// which our GridMap doesn't do). Used for TileType.Tilled instead of a flat colour square.
        /// </summary>
        public Sprite TilledSoil;

        /// <summary>
        /// Directional walk cycles, visually confirmed by exporting a magnified contact sheet of
        /// customised_farmer.png (24 cols x 21 rows) — row 0 = facing down, row 1 = facing left,
        /// row 2 = facing up (back of head visible). There is no separate right-facing row, so
        /// PlayerController mirrors WalkLeft (SpriteRenderer.flipX) instead of guessing another row.
        /// Column 0 of each row is a neutral standing pose, not a walk frame — mixing it into the
        /// cycle made the walk visibly snap back to standing once per loop, so it's kept separate as
        /// Idle* and the Walk* arrays only hold columns 1-4 (genuine mid-stride poses).
        /// </summary>
        public Sprite IdleDown;
        public Sprite IdleLeft;
        public Sprite IdleUp;
        public Sprite[] WalkDown;
        public Sprite[] WalkLeft;
        public Sprite[] WalkUp;

        /// <summary>Static scenery placed around the playable grid (not tied to any gameplay tile).</summary>
        public Sprite Tree;
        public Sprite Farmhouse;
        public Sprite GrassTuft;
        public Sprite Stone;

        /// <summary>
        /// Choppable tree object rendered ON the grid (S2-DEV-06, DESIGN_BRIEFS.md [DSN-030]). Picked by
        /// exact sprite name from the same CanyonOak.png sheet as the decorative Tree above — kept as
        /// separate fields (not reusing Tree/one shared reference) since they serve different roles
        /// (gameplay object vs. background scenery) even though TreeStanding happens to be the same
        /// source sprite (CanyonOak_0) today. CanyonOak_3 was confirmed by a magnified crop export to be
        /// a genuine tree-stump graphic (not guessed by index) — visually distinct from the standing
        /// tree so a felled tile reads as "already chopped" at a glance, not just via miss-feedback.
        /// </summary>
        public Sprite TreeStanding;
        public Sprite TreeStump;

        /// <summary>Tile-targeting reticle: green over the tile a tool-use would land on, red if out of bounds.</summary>
        public Sprite CursorValid;
        public Sprite CursorInvalid;

        /// <summary>
        /// Hotbar UI (ToolbarUI). Icons visually identified from tools.png (a tiered tool-in-hand
        /// sheet, not a plain icon set — most of it is unusable as a flat icon) and confirmed by
        /// exporting a magnified contact sheet: tools_21 = hoe, tools_63 = watering can, tools_252 =
        /// a harvest basket (no sickle exists in the sheet, a basket is the closest fit). Seed uses a
        /// dedicated seed-bag asset instead since tools.png has none. Order matches ToolType enum
        /// (Hoe, Seed, WateringCan, Harvest) so ToolbarUI can index it directly.
        /// </summary>
        public Sprite[] ToolIcons;

        /// <summary>
        /// Explicit inventory icons for axe/wood (BUG-050-3). These are not part of ToolIcons' legacy
        /// 4-entry order because that array already includes the seed-bag pseudo-tool slot; keeping
        /// them named avoids guessing indices in runtime UI code.
        /// </summary>
        public Sprite AxeIcon;
        public Sprite WoodIcon;

        public Sprite InventoryBar;
        public Sprite InventoryHighlight;

        /// <summary>
        /// Full inventory panel background (I key): 10-slot hotbar row + 3x10 backpack grid — see
        /// Inventory.SlotCount. Same source pack as InventoryBar, just the bigger version.
        /// </summary>
        public Sprite InventoryPlayer;

        /// <summary>DEV-041 static NPCs and dialogue UI, baked from Assets/Sprite Textures/NPCs.</summary>
        public Sprite CoraSprite;
        public Sprite CoraPortrait;
        public Sprite ButchSprite;
        public Sprite ButchPortrait;
        public Sprite DialogueBox;
    }
}
