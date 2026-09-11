using System.Collections.Generic;
using System.Text;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>
    /// Headless economy simulation (AGENT_DEV §5.4). Runnable from CLI with no scene, no
    /// MonoBehaviour — drives the same GameState/ToolController the real game uses (GameState.UseTool /
    /// PlantSpecific, the exact entry points PlayerController calls), so the sim can never drift from
    /// actual gameplay rules the way a hand-rolled spreadsheet formula could.
    ///
    /// Sprint 1 (B4) reason for existing now: the project's new scope adds wood + NPC as a second
    /// income source. Without this sim, wood vs. crop pricing gets tuned by feel, and it's easy to
    /// land on "chopping wood beats farming" by accident — this sim exists to catch that in 5 seconds
    /// (compare a wood-income column once it lands) instead of 3 weeks of playtests in.
    /// </summary>
    public static class HeadlessSim
    {
        /// <summary>
        /// Simulation assumption, NOT a design number: how many plots one greedy farmer keeps working
        /// concurrently. Not from BalanceConfig — it's a property of the SIM's farmer (bounded by
        /// nothing but stamina/cash here), not of the real player (bounded by walking time / session
        /// length, which this headless sim doesn't model).
        /// </summary>
        const int MaxWorkingPlots = 12;

        /// <summary>
        /// Drives a greedy farmer through <paramref name="days"/> in-game days on a fresh GameState:
        /// each day — harvest whatever's ripe, chop every standing tree it can reach (S2-DEV-07,
        /// stamina permitting), claim new tilled plots up to MaxWorkingPlots (stamina permitting), plant
        /// every bare plot (buying seed from the wallet), water everything planted, sell any wood
        /// carried, then sleep (GameClock.ForceEndDay via GameState.SkipDay). Returns the run as a CSV
        /// string (header: day,money,plots,stamina_used,harvested,wood_harvested,wood_income), one row
        /// per simulated day.
        /// </summary>
        /// <param name="forceCrop">
        /// null = true "greedy" farmer: plants Potato whenever affordable (higher profit/day per
        /// BootstrapBalanceTests.BalanceConfig_ProfitPerDay_InternalBalanceRule), falls back to the
        /// cheaper Turnip otherwise so a low-cash plot doesn't sit idle. Pass a specific CropId to run
        /// a single-crop baseline instead — used by HeadlessSimTests to compare Potato-only vs.
        /// Turnip-only profit at the simulation level, not just the balance-formula level.
        /// </param>
        public static string RunGreedyFarmer(int days, int seed, CropId? forceCrop = null)
        {
            var state = new GameState(seed: seed);
            var plots = new List<GridCoord>(MaxWorkingPlots);
            var claimed = new HashSet<GridCoord>();

            var sb = new StringBuilder();
            sb.AppendLine("day,money,plots,stamina_used,harvested,wood_harvested,wood_income");

            for (int d = 0; d < days; d++)
            {
                int staminaStart = state.Stamina.Current;
                int harvested = 0;

                // 1. Harvest whatever's ripe first: frees the plot for replanting and costs 0 stamina
                // (BalanceConfig.HarvestStaminaCost), so it's always the first move of the day.
                foreach (var c in plots)
                {
                    var tile = state.Grid.GetTile(c);
                    if (tile?.Crop != null && tile.Crop.IsRipe)
                        if (state.UseTool(ToolType.Harvest, c).IsSuccess) harvested++;
                }

                // 2. Chop every standing tree the farmer can reach this day (S2-DEV-07,
                // DESIGN_BRIEFS.md [DSN-030] Q6: this sim exists specifically to catch wood
                // accidentally out-earning crops, so the "greedy" farmer must actually chop, not just
                // farm). Reachability/travel time isn't modelled by this sim (see MaxWorkingPlots doc);
                // it works every tree it can afford in stamina before moving on to farming for the day.
                int woodHarvested = ChopAllTrees(state);

                // 3. Claim new plots (till fresh grass) up to the working cap, stamina permitting.
                while (plots.Count < MaxWorkingPlots)
                {
                    var found = FindFreeGrassTile(state, claimed);
                    if (found == null) break; // ran out of grass on this grid
                    if (!state.UseTool(ToolType.Hoe, found.Value).IsSuccess) break; // out of stamina
                    plots.Add(found.Value);
                    claimed.Add(found.Value);
                }

                // 4. Plant every bare tilled plot, buying its seed from the wallet first.
                foreach (var c in plots)
                {
                    var tile = state.Grid.GetTile(c);
                    if (tile == null || tile.Crop != null) continue;
                    var crop = ChooseCrop(state, forceCrop);
                    if (crop == null) continue; // can't afford anything for this plot right now
                    state.Wallet.TrySpend(CropDefinition.SeedPrice(crop.Value));
                    state.InventorySystem.Add(CropDefinition.SeedItemId(crop.Value), 1);
                    state.PlantSpecific(crop.Value, c);
                }

                // 5. Water everything planted (IsWatered resets every day rollover — CropInstance only
                // advances on days it was watered, see CropInstance.AdvanceDay).
                foreach (var c in plots)
                {
                    var tile = state.Grid.GetTile(c);
                    if (tile?.Crop != null && !tile.IsWatered)
                        state.UseTool(ToolType.WateringCan, c);
                }

                // 6. Sell carried produce and wood through the same sell channel a real interaction
                // uses; harvest itself only adds produce to inventory, it no longer credits Wallet.
                state.SellAllCrops();
                int woodIncome = state.SellAllWood();

                int staminaUsed = staminaStart - state.Stamina.Current;
                sb.AppendLine($"{state.Clock.Day},{state.Wallet.Money},{plots.Count},{staminaUsed},{harvested},{woodHarvested},{woodIncome}");

                state.SkipDay();
            }

            return sb.ToString();
        }

        /// <summary>Deterministic top-left-to-bottom-right scan for the first unclaimed, unoccupied grass tile
        /// (S2-DEV-07: a tree can sit on a Grass tile without changing its TileType, so Type alone isn't
        /// enough to know a tile is actually tillable — see GridMap.IsOccupied).</summary>
        static GridCoord? FindFreeGrassTile(GameState state, HashSet<GridCoord> claimed)
        {
            for (int y = 0; y < state.Grid.Height; y++)
                for (int x = 0; x < state.Grid.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    if (claimed.Contains(c)) continue;
                    if (state.Grid.GetTile(c).Type == TileType.Grass && !state.Grid.IsOccupied(c)) return c;
                }
            return null;
        }

        /// <summary>Deterministic top-left-to-bottom-right scan, chopping every standing tree down to a
        /// stump (or until stamina runs out). Returns total wood gained this call.</summary>
        static int ChopAllTrees(GameState state)
        {
            int wood = 0;
            for (int y = 0; y < state.Grid.Height; y++)
                for (int x = 0; x < state.Grid.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    var tile = state.Grid.GetTile(c);
                    while (tile.Object != null && tile.Object.IsAlive)
                    {
                        var r = state.UseTool(ToolType.Chop, c);
                        if (!r.IsSuccess) return wood; // out of stamina — stop for the day
                        wood += r.Amount; // >0 only on the felling hit
                    }
                }
            return wood;
        }

        static CropId? ChooseCrop(GameState state, CropId? forceCrop)
        {
            if (forceCrop != null)
                return state.Wallet.Money >= CropDefinition.SeedPrice(forceCrop.Value) ? forceCrop : null;

            if (state.Wallet.Money >= CropDefinition.SeedPrice(CropId.Potato)) return CropId.Potato;
            if (state.Wallet.Money >= CropDefinition.SeedPrice(CropId.Turnip)) return CropId.Turnip;
            return null;
        }
    }
}
