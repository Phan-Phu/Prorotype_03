using Prototype.Domain;

namespace Prototype.Domain
{
    /// <summary>Kind of static object a tile can hold. Only Tree exists today (S2-DEV-01).</summary>
    public enum TileObjectType { Tree }

    /// <summary>
    /// Static blocking object occupying a tile (S2-DEV-01, DESIGN_BRIEFS.md [DSN-030]). Plain C# so
    /// GridMap/TileData stay constructible and testable with no scene, same rule the rest of the grid
    /// layer follows. HP/RespawnDaysLeft are Tree-specific fields kept flat on one class rather than
    /// behind an interface, since exactly one object type (Tree) uses them so far — split this out if
    /// a second object type ever needs different fields.
    /// </summary>
    public class TileObject
    {
        public readonly TileObjectType Type;

        /// <summary>Hits remaining. &gt;0 = standing/choppable tree. 0 = felled stump, not choppable until it respawns.</summary>
        public int HP;

        /// <summary>
        /// Days left before a felled tree (HP==0) regrows back to a full tree. Counts down once per
        /// day rollover (GameState.OnDayEnded -> AdvanceDay) — "đếm như crop growth, không cần chăm
        /// sóc/tưới" (DESIGN_BRIEFS.md [DSN-030]): no watering/care needed, same cadence as
        /// CropInstance.AdvanceDay. A countdown (not an absolute GameClock.Day target) so this class
        /// stays clock-agnostic and ToolController doesn't need a GameClock reference to fell a tree.
        /// </summary>
        public int RespawnDaysLeft;

        public TileObject(TileObjectType type, int hp)
        {
            Type = type;
            HP = hp;
        }

        public static TileObject NewTree(int maxHP = -1)
            => new TileObject(TileObjectType.Tree, maxHP > 0 ? maxHP : TreeDefinition.MaxHP);

        public bool IsAlive => HP > 0;

        /// <summary>Called once per day (GameState.OnDayEnded), same cadence as CropInstance.AdvanceDay.</summary>
        public void AdvanceDay()
        {
            if (IsAlive) return; // nothing to advance on a standing tree
            if (RespawnDaysLeft > 0) RespawnDaysLeft--;
            if (RespawnDaysLeft <= 0) HP = TreeDefinition.MaxHP; // stump regrows into a full tree
        }
    }
}
