using Prototype.Domain;

namespace Prototype.Domain
{
    public enum TileType { Grass, Tilled, Water, Blocked }

    /// <summary>State of one farm tile. Plain C# so GridMap can be driven headless / in EditMode tests.</summary>
    public class TileData
    {
        public TileType Type = TileType.Grass;
        public bool IsWatered;
        public CropInstance Crop;   // null when nothing planted
        public TileObject Object;   // null when empty; static blocking object (e.g. a tree, S2-DEV-01)
    }
}
