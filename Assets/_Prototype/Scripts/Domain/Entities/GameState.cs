using Prototype.Domain;
using UnityEngine;

namespace Prototype.Domain
{
    /// <summary>
    /// The whole prototype simulation state. Plain C# (no MonoBehaviour) so it can be driven from
    /// EditMode tests, the headless sim, AND the runtime GameManager without a scene.
    /// It is a raw aggregate only; runtime behavior belongs to Infrastructure services.
    /// </summary>
    public class GameState
    {
        public GridMap Grid;
        public GameClock Clock;
        public Stamina Stamina;
        public Wallet Wallet;
        public Prototype.Domain.Inventory InventorySystem;
        public readonly MasterDataSnapshot MasterData;
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

        public GameState(int width = 20, int height = 20, int seed = 0, MasterDataSnapshot masterData = null)
        {
            Seed = seed;
            MasterData = masterData ?? MasterDataSnapshot.CreatePrototypeDefaults();
            Rng = new System.Random(seed);
            Grid = new GridMap(width, height);
            Clock = new GameClock();
            Stamina = new Stamina(MasterData.Player.MaxStamina);
            Wallet = new Wallet(MasterData.Player.StartMoney);
            InventorySystem = new Prototype.Domain.Inventory();
            GrantStartingTools();
            SeedTrees();
            SeedNpcBlockers();
        }

        /// <summary>Initial raw inventory composition for the prototype session.</summary>
        public void GrantStartingTools()
        {
            for (int i = 0; i < MasterData.StartingItems.Length && i < InventorySystem.Slots.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(MasterData.StartingItems[i]))
                    InventorySystem.Slots[i] = new ItemStack(MasterData.StartingItems[i]);
            }
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
            int count = System.Math.Min(MasterData.Tree.InitialCount, Grid.Width);
            int row = Grid.Height - 1;
            for (int i = 0; i < count; i++)
            {
                var c = new GridCoord(Grid.Width - 1 - i, row);
                var tile = Grid.GetTile(c);
                if (tile != null && tile.Type == TileType.Grass && tile.Object == null)
                    tile.Object = TileObject.NewTree(MasterData.Tree.MaxHP);
            }
        }

        void SeedNpcBlockers()
        {
            foreach (var npc in MasterData.Npcs)
                Grid.SetStaticBlocker(npc.Coord, true);
        }


        // Runtime behavior, selling, debug cheats and shop coordinates are implemented by
        // Infrastructure services. Domain keeps only the raw aggregate state.
    }
}
