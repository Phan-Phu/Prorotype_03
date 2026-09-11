using System.Collections.Generic;
using UnityEngine;

namespace Prototype.Domain
{
    /// <summary>
    /// Plain-C# farm grid. No MonoBehaviour -> constructible directly in EditMode tests and headless sim.
    /// World<->grid conversion happens ONLY at this boundary; callers inside the sim use GridCoord.
    /// </summary>
    public class GridMap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float TileSize;
        private readonly TileData[,] _tiles;
        private readonly HashSet<GridCoord> _staticBlockers = new HashSet<GridCoord>();

        public GridMap(int width = 20, int height = 20, float tileSize = 1f)
        {
            Width = width; Height = height; TileSize = tileSize;
            _tiles = new TileData[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _tiles[x, y] = new TileData();
        }

        public bool InBounds(GridCoord c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;
        public TileData GetTile(GridCoord c) => InBounds(c) ? _tiles[c.X, c.Y] : null;

        public bool IsWalkable(GridCoord c)
        {
            var t = GetTile(c);
            if (t == null) return false;
            return t.Type != TileType.Blocked && t.Type != TileType.Water;
        }

        /// <summary>
        /// True when a static object (S2-DEV-01, e.g. a standing tree or its stump) occupies the tile.
        /// PlayerController reads this to block movement and redden the cursor reticle (S2-DEV-04);
        /// ToolController reads it to refuse Till/Plant with ToolResultCode.Blocked instead of the
        /// default WrongTool. Separate from IsWalkable (which is about TileType only) so object
        /// occupancy stays its own concept, not folded into terrain type.
        /// </summary>
        public bool IsOccupied(GridCoord c) => GetTile(c)?.Object != null || _staticBlockers.Contains(c);

        /// <summary>
        /// Marks a non-tile-object blocker (DEV-041 static NPCs). Kept outside TileData.Object so
        /// choppable-tree logic does not accidentally treat an NPC like a tree/stump lifecycle object.
        /// </summary>
        public void SetStaticBlocker(GridCoord c, bool blocked)
        {
            if (!InBounds(c)) return;
            if (blocked) _staticBlockers.Add(c);
            else _staticBlockers.Remove(c);
        }

        public bool IsStaticBlocked(GridCoord c) => _staticBlockers.Contains(c);

        public GridCoord WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x / TileSize + Width * 0.5f);
            int y = Mathf.FloorToInt(worldPos.y / TileSize + Height * 0.5f);
            return new GridCoord(x, y);
        }

        public Vector3 GridToWorld(GridCoord c)
        {
            // returns the CENTRE of the tile
            return new Vector3(
                (c.X - Width * 0.5f + 0.5f) * TileSize,
                (c.Y - Height * 0.5f + 0.5f) * TileSize,
                0f);
        }

        public IEnumerable<TileData> AllTiles()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    yield return _tiles[x, y];
        }
    }
}
