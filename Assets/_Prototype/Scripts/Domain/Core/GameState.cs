using Prototype.Application;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Domain
{
    /// <summary>
    /// The whole prototype simulation state. Plain C# (no MonoBehaviour) so it can be driven from
    /// EditMode tests, the headless sim, AND the runtime GameManager without a scene.
    /// Construction order and the day-rollover rules live here; the view layer only reads it.
    /// </summary>
    public class GameState
    {
        public GridMap Grid;
        public GameClock Clock;
        public Stamina Stamina;
        public Wallet Wallet;
        public Prototype.Domain.Inventory InventorySystem;
        public System.Random Rng;
        public Vector2 PlayerPosition;

        /// <summary>
        /// The seed this GameState was actually constructed with (S2-QA-06 fix). SessionLogger reads
        /// this instead of the global BootArgs.Seed — in the real game the two always match
        /// (GameManager does `new GameState(seed: BootArgs.Seed)`), but reading the state's own value
        /// is the honest source of truth and can't silently diverge if a GameState is ever constructed
        /// some other way (e.g. exactly what a manual QA check or a future tool would do).
        /// </summary>
        public readonly int Seed;

        public GameState(int width = 20, int height = 20, int seed = 0)
        {
            Seed = seed;
            Rng = new System.Random(seed);
            Grid = new GridMap(width, height);
            Clock = new GameClock();
            Stamina = new Stamina(BalanceConfig.MaxStamina);
            Wallet = new Wallet(BalanceConfig.StartMoney);
            InventorySystem = new Prototype.Domain.Inventory();
            GrantStartingTools();
            SeedTrees();
            SeedNpcBlockers();
            Clock.OnDayEnded += OnDayEnded;
        }

        /// <summary>
        /// Hoe/WateringCan/Harvest/Axe are real (non-consumable) inventory items now — the hotbar
        /// mirrors Inventory row 0 (ToolbarUI), so they have to actually be IN the inventory to show up
        /// there. Called once at construction, and again by the debug "Clear inventory" cheat so
        /// clearing doesn't leave the player unable to do anything. Axe added S2-DEV-02
        /// (DESIGN_BRIEFS.md [DSN-030]) — resolves through the same ResolveAction path as every other
        /// tool, no separate "current tool" enum/selection.
        /// </summary>
        public void GrantStartingTools()
        {
            InventorySystem.Add(ToolItemIds.Hoe);
            InventorySystem.Add(ToolItemIds.WateringCan);
            InventorySystem.Add(ToolItemIds.Harvest);
            InventorySystem.Add(ToolItemIds.Axe);
        }

        /// <summary>
        /// Places BalanceConfig.InitialTreeCount trees (S2-DEV-01, DESIGN_BRIEFS.md [DSN-030] —
        /// finite by design, not a self-renewing forest). Placement is a fixed deterministic pattern
        /// (bottom-right corner of the grid, growing leftward) rather than drawn from Rng: several
        /// EditMode tests construct GameState with a fixed seed and act on fixed coordinates near the
        /// grid centre/origin (e.g. (5,5), (2,2), (10,10)) expecting plain Grass there — a seeded random
        /// placement could occasionally land a tree on one of those tiles and make a test flaky purely
        /// by seed coincidence. A fixed corner strip sidesteps that entirely while still guaranteeing
        /// exactly InitialTreeCount trees exist for the player/HeadlessSim to chop, and doesn't consume
        /// the shared Rng stream other systems (GameManager.SetupScenery) rely on for their own layout.
        /// </summary>
        void SeedTrees()
        {
            int count = System.Math.Min(BalanceConfig.InitialTreeCount, Grid.Width);
            int row = Grid.Height - 1;
            for (int i = 0; i < count; i++)
            {
                var c = new GridCoord(Grid.Width - 1 - i, row);
                var tile = Grid.GetTile(c);
                if (tile != null && tile.Type == TileType.Grass && tile.Object == null)
                    tile.Object = TileObject.NewTree();
            }
        }

        void SeedNpcBlockers()
        {
            foreach (var npc in NpcDefinitions.All)
                Grid.SetStaticBlocker(npc.Coord, true);
        }

        /// <summary>New-day rules (AGENT_DEV §5.2): advance every crop and tree, reset watered, refill stamina.</summary>
        void OnDayEnded()
        {
            foreach (var t in Grid.AllTiles())
            {
                if (t.Crop != null) t.Crop.AdvanceDay();
                if (t.Object != null) t.Object.AdvanceDay(); // stump respawn countdown, S2-DEV-01
                t.IsWatered = false;
            }
            Stamina.Refill();
        }

        /// <summary>
        /// Generic "sell a carried item" channel. Returns a structured result so player-facing UI can
        /// show wallet/count deltas and empty-inventory feedback without guessing from a raw int.
        /// </summary>
        public Prototype.Domain.ShopSellResult SellItem(string itemId, int pricePerUnit, int count)
        {
            int moneyBefore = Wallet.Money;
            int countBefore = InventorySystem.Count(itemId);

            if (count <= 0)
                return SellResult(Prototype.Domain.ShopSellResultCode.EmptyInventory, itemId, pricePerUnit, count,
                    0, moneyBefore, Wallet.Money, countBefore, InventorySystem.Count(itemId));

            if (countBefore < count || !InventorySystem.TryRemove(itemId, count))
                return SellResult(Prototype.Domain.ShopSellResultCode.EmptyInventory, itemId, pricePerUnit, count,
                    0, moneyBefore, Wallet.Money, countBefore, InventorySystem.Count(itemId));

            int total = pricePerUnit * count;
            Wallet.Add(total);
            return SellResult(Prototype.Domain.ShopSellResultCode.Success, itemId, pricePerUnit, count,
                total, moneyBefore, Wallet.Money, countBefore, InventorySystem.Count(itemId));
        }

        static Prototype.Domain.ShopSellResult SellResult(Prototype.Domain.ShopSellResultCode code,
            string itemId, int pricePerUnit, int requestedCount, int earned, int moneyBefore, int moneyAfter,
            int countBefore, int countAfter)
            => new Prototype.Domain.ShopSellResult(code, itemId, pricePerUnit, requestedCount, earned,
                moneyBefore, moneyAfter, countBefore, countAfter);

        /// <summary>Sells every unit of wood currently carried. Returns the money earned.</summary>
        public int SellAllWood()
        {
            int count = InventorySystem.Count(TreeDefinition.WoodItemId);
            return SellItem(TreeDefinition.WoodItemId, TreeDefinition.WoodSellPrice, count).Earned;
        }

        /// <summary>Sells every harvested produce item of the requested crop. Returns the money earned.</summary>
        public int SellAllCrop(CropId crop)
        {
            string itemId = CropDefinition.ProduceItemId(crop);
            int count = InventorySystem.Count(itemId);
            return SellItem(itemId, CropDefinition.SellPrice(crop), count).Earned;
        }

        /// <summary>Sells all currently carried crop produce. Returns total money earned.</summary>
        public int SellAllCrops() => SellAllCrop(CropId.Turnip) + SellAllCrop(CropId.Potato);

        /// <summary>
        /// Debug cheat (S2-QA-05): drops a fresh tree onto coord if it's free (Grass, unoccupied).
        /// Returns true if placed. Used by DebugPanel's "+ Tree" button so QA can spawn a tree right
        /// next to the player instead of trekking to the corner GameState.SeedTrees seeds at start.
        /// </summary>
        public bool DebugSpawnTree(GridCoord coord)
        {
            var tile = Grid.GetTile(coord);
            if (tile == null || tile.Type != TileType.Grass || tile.Object != null) return false;
            tile.Object = TileObject.NewTree();
            return true;
        }

        /// <summary>
        /// Debug cheat (S2-QA-05): instantly regrows every felled stump on the map, skipping its
        /// RespawnDaysLeft countdown — same "force" pattern as ForceRipeAll for crops, so QA can test
        /// respawn behaviour without waiting TreeRespawnDays real sleeps.
        /// </summary>
        public void ForceRespawnTrees()
        {
            foreach (var t in Grid.AllTiles())
                if (t.Object != null && !t.Object.IsAlive)
                {
                    t.Object.HP = TreeDefinition.MaxHP;
                    t.Object.RespawnDaysLeft = 0;
                }
        }

        // --- High-level actions (used by GameManager, tests, headless sim) ---

        public ToolResult UseTool(ToolType tool, GridCoord coord)
            => new ToolController(Grid, Stamina, Wallet, InventorySystem).TryUse(tool, coord);

        /// <summary>Plants exactly cropId — see ToolController.PlantSpecific.</summary>
        public ToolResult PlantSpecific(CropId cropId, GridCoord coord)
            => new ToolController(Grid, Stamina, Wallet, InventorySystem).PlantSpecific(cropId, coord);

        public void ForceRipeAll()
        {
            foreach (var t in Grid.AllTiles())
                if (t.Crop != null) t.Crop.DaysGrown = CropDefinition.GrowthDays(t.Crop.Id);
        }

        /// <summary>
        /// DSN-040: reachable one-tile seed-shop sign, directly below the Farmhouse decoration that
        /// SetupScenery places around x=-4 on the top edge for the default 20x20 prototype grid.
        /// </summary>
        public GridCoord SeedShopCoord => new GridCoord(System.Math.Max(0, Grid.Width / 2 - 4), Grid.Height - 1);

        public bool IsSeedShopTile(GridCoord coord) => coord.Equals(SeedShopCoord);

        public Prototype.Domain.ShopPurchaseResult BuySeed(CropId crop)
            => Prototype.Domain.SeedShop.BuySeed(Wallet, InventorySystem, crop);

        public void SetMoney(int amount) => Wallet.Money = amount;
        public void RefillStamina() => Stamina.Refill();
        public void SkipDay() => Clock.ForceEndDay();
    }
}
