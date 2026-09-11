using Prototype.Domain;

namespace Prototype.Domain
{
    /// <summary>
    /// Applies a tool to a target tile. Pure logic: no MonoBehaviour, no scene, no rendering.
    /// The core loop (AGENT_DEV §5.3): validate tool/stamina/tile, mutate TileData, return a ToolResult.
    /// Every branch returns a ToolResult so QA can assert behaviour deterministically from CLI.
    /// </summary>
    public class ToolController
    {
        private readonly GridMap _grid;
        private readonly Stamina _stamina;
        private readonly Wallet _wallet;
        private readonly Prototype.Domain.Inventory _inventory;

        public ToolController(GridMap grid, Stamina stamina, Wallet wallet, Prototype.Domain.Inventory inventory)
        {
            _grid = grid; _stamina = stamina; _wallet = wallet; _inventory = inventory;
        }

        public ToolResult TryUse(ToolType tool, GridCoord coord)
        {
            if (!_grid.InBounds(coord))
                return ToolResult.Fail(ToolResultCode.InvalidTile, FeedbackKind.Miss);
            if (_grid.IsStaticBlocked(coord))
                return ToolResult.Fail(ToolResultCode.Blocked, FeedbackKind.Miss);

            var tile = _grid.GetTile(coord);
            switch (tool)
            {
                case ToolType.Hoe:         return Till(tile);
                case ToolType.Seed:        return Plant(tile);
                case ToolType.WateringCan: return Water(tile);
                case ToolType.Harvest:     return Harvest(tile);
                case ToolType.Chop:        return Chop(tile);
                default:                   return ToolResult.Fail(ToolResultCode.WrongTool, FeedbackKind.Miss);
            }
        }

        ToolResult Till(TileData tile)
        {
            // S2-DEV-03: a standing tree blocks tilling — distinct from WrongTool so QA can tell
            // "wrong terrain" apart from "something is physically in the way" in tests.
            if (tile.Object != null)
                return ToolResult.Fail(ToolResultCode.Blocked, FeedbackKind.Miss);
            if (tile.Type != TileType.Grass)
                return ToolResult.Fail(ToolResultCode.WrongTool, FeedbackKind.Miss);
            if (!_stamina.TrySpend(BalanceConfig.TillStaminaCost))
                return ToolResult.Fail(ToolResultCode.NoStamina, FeedbackKind.Miss);
            tile.Type = TileType.Tilled;
            return ToolResult.Success(FeedbackKind.Till);
        }

        /// <summary>
        /// S2-DEV-02/03 (DESIGN_BRIEFS.md [DSN-030]): chops the tree standing on this tile, or fails
        /// with NotChoppable if there's no live tree here (bare tile, or an already-felled stump still
        /// waiting to respawn) — mirrors the Miss feedback the design brief spec's S2-DES-04 table
        /// gives that exact case ("vung rìu vào gốc cây hoặc ô trống").
        /// </summary>
        ToolResult Chop(TileData tile)
        {
            if (tile.Object == null || tile.Object.Type != TileObjectType.Tree || !tile.Object.IsAlive)
                return ToolResult.Fail(ToolResultCode.NotChoppable, FeedbackKind.Miss);
            if (!_stamina.TrySpend(BalanceConfig.ChopStaminaCost))
                return ToolResult.Fail(ToolResultCode.NoStamina, FeedbackKind.Miss);

            tile.Object.HP--;
            if (tile.Object.HP > 0)
                return ToolResult.Success(FeedbackKind.Chop); // hit landed, tree still standing

            // Felling hit: tree becomes a stump (HP already 0), drops wood into the inventory (S2-DEV-05
            // — unlike crops, wood is a real carried item, not an instant wallet credit), and starts its
            // respawn countdown (TileObject.AdvanceDay, driven by GameState.OnDayEnded).
            tile.Object.RespawnDaysLeft = TreeDefinition.RespawnDays;
            _inventory.Add(TreeDefinition.WoodItemId, TreeDefinition.WoodPerTree);
            return ToolResult.Success(FeedbackKind.Chop, TreeDefinition.WoodPerTree);
        }

        ToolResult Plant(TileData tile)
        {
            if (!CanPlantOn(tile, out var fail)) return fail;

            // Plants whichever seed the player is carrying; Turnip takes priority if both are held.
            // Used by the plain ToolType.Seed path (tests, headless sim). The hotbar (ToolbarUI) calls
            // PlantSpecific instead, since there the player has picked a specific seed-stack slot.
            CropId? toPlant = null;
            if (_inventory.TryRemove(CropDefinition.SeedItemId(CropId.Turnip))) toPlant = CropId.Turnip;
            else if (_inventory.TryRemove(CropDefinition.SeedItemId(CropId.Potato))) toPlant = CropId.Potato;

            if (toPlant == null)
                return ToolResult.Fail(ToolResultCode.NoSeed, FeedbackKind.Miss);

            tile.Crop = new CropInstance(toPlant.Value);
            return ToolResult.Success(FeedbackKind.Plant);
        }

        /// <summary>Plants exactly cropId (consuming its seed specifically), instead of Plant()'s
        /// auto-priority — this is what the hotbar uses when a particular seed-stack slot is active.</summary>
        public ToolResult PlantSpecific(CropId cropId, GridCoord coord)
        {
            if (!_grid.InBounds(coord))
                return ToolResult.Fail(ToolResultCode.InvalidTile, FeedbackKind.Miss);
            var tile = _grid.GetTile(coord);
            if (!CanPlantOn(tile, out var fail)) return fail;

            if (!_inventory.TryRemove(CropDefinition.SeedItemId(cropId)))
                return ToolResult.Fail(ToolResultCode.NoSeed, FeedbackKind.Miss);

            tile.Crop = new CropInstance(cropId);
            return ToolResult.Success(FeedbackKind.Plant);
        }

        static bool CanPlantOn(TileData tile, out ToolResult fail)
        {
            // S2-DEV-03: a tree occupying the tile blocks planting too (reachable via PlantSpecific
            // even though Till() already refuses to turn a treed tile into Tilled ground in the first
            // place) — keep this check ahead of the terrain/crop checks below.
            if (tile.Object != null)          { fail = ToolResult.Fail(ToolResultCode.Blocked, FeedbackKind.Miss); return false; }
            if (tile.Type != TileType.Tilled) { fail = ToolResult.Fail(ToolResultCode.WrongTool, FeedbackKind.Miss); return false; }
            if (tile.Crop != null)            { fail = ToolResult.Fail(ToolResultCode.WrongTool, FeedbackKind.Miss); return false; }
            fail = default;
            return true;
        }

        ToolResult Water(TileData tile)
        {
            if (tile.Type != TileType.Tilled)
                return ToolResult.Fail(ToolResultCode.WrongTool, FeedbackKind.Miss);
            if (tile.IsWatered)
                return ToolResult.Success(FeedbackKind.Water); // idempotent, no stamina
            if (!_stamina.TrySpend(BalanceConfig.WaterStaminaCost))
                return ToolResult.Fail(ToolResultCode.NoStamina, FeedbackKind.Miss);
            tile.IsWatered = true;
            if (tile.Crop != null) tile.Crop.WateredToday = true;
            return ToolResult.Success(FeedbackKind.Water);
        }

        ToolResult Harvest(TileData tile)
        {
            if (tile.Crop == null)
                return ToolResult.Fail(ToolResultCode.WrongTool, FeedbackKind.Miss);
            if (!tile.Crop.IsRipe)
                return ToolResult.Fail(ToolResultCode.WrongTool, FeedbackKind.Miss);
            string itemId = CropDefinition.ProduceItemId(tile.Crop.Id);
            if (!_inventory.CanAdd(itemId))
                return ToolResult.Fail(ToolResultCode.InventoryFull, FeedbackKind.Miss);

            _inventory.Add(itemId);
            tile.Crop = null;
            return ToolResult.Success(FeedbackKind.Harvest, 1);
        }
    }
}
