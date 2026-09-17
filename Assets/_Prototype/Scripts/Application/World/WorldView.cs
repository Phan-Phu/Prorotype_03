using Prototype.Domain;
using Prototype.Application;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Renders the GridMap as coloured squares (AGENT_DEV §3.2 placeholder art). Reads GameState and
    /// recolours tiles every frame so tilling/watering/ripening are visible immediately (feedback rule §5.3).
    /// </summary>
    public class WorldView : MonoBehaviour
    {
        public GameState State;
        [Header("Editor-authored world root")]
        [SerializeField] Transform _worldRoot;
        private SpriteRenderer[,] _tiles;
        private Sprite[,] _baseTiles;
        private SpriteRenderer[,] _crops;   // real art (ParsnipCrop stages) drawn above the tile square
        private SpriteRenderer[,] _objects; // static tile objects (trees/stumps, S2-DEV-06) drawn above the tile square

        // CanyonOak.png's tree/stump pieces aren't tile-sized (measured via CIArt: TreeStanding is
        // 3x6 world units at its native PPU, TreeStump 1x2) — scaled down here so a standing tree's
        // canopy overflows its own tile a little (like the player sprite does) instead of covering
        // several neighbours. Two different constants because the stump's source rect is mostly padding
        // around a squat graphic, so the same scale as the tree would render it too small to read.
        const float TreeObjectScale = 0.32f;
        const float StumpObjectScale = 0.55f;

        /// <summary>
        /// Builds the tile GameObjects. NOT called from Awake(): GameManager does
        /// `go.AddComponent&lt;WorldView&gt;()` then `wv.State = State` — Unity calls Awake()
        /// synchronously INSIDE AddComponent, before the State assignment on the next line ever runs,
        /// so an Awake()-based build always saw State == null and silently built nothing (bug found
        /// 2026-08-15: the grid never appeared in any build, no matter what the tiles looked like).
        /// Update() below lazily calls this once State is actually set, which works regardless of
        /// setup order.
        /// </summary>
        void Build()
        {
            var g = State.Grid;
            _tiles = new SpriteRenderer[g.Width, g.Height];
            _baseTiles = new Sprite[g.Width, g.Height];
            _crops = new SpriteRenderer[g.Width, g.Height];
            _objects = new SpriteRenderer[g.Width, g.Height];
            for (int x = 0; x < g.Width; x++)
            {
                for (int y = 0; y < g.Height; y++)
                {
                    var pos = g.GridToWorld(new GridCoord(x, y));

                    var sr = FindOrCreateRenderer($"Tile_{x}_{y}");
                    var authoredTile = sr.sprite;
                    if (authoredTile == null) authoredTile = PlaceholderArt.WhiteSprite;
                    sr.sprite = authoredTile;
                    _baseTiles[x, y] = authoredTile;
                    sr.transform.position = pos;
                    sr.transform.localScale = new Vector3(g.TileSize, g.TileSize, 1f);
                    _tiles[x, y] = sr;

                    var cropSr = FindOrCreateRenderer($"Crop_{x}_{y}");
                    cropSr.transform.position = pos;
                    cropSr.sortingOrder = 1; // above the tile square
                    cropSr.enabled = false;
                    _crops[x, y] = cropSr;

                    var objSr = FindOrCreateRenderer($"Object_{x}_{y}");
                    objSr.transform.position = pos;
                    objSr.sortingOrder = 1; // same layer as crops — a tile never has both at once
                    objSr.enabled = false;
                    _objects[x, y] = objSr;
                }
            }
        }

        SpriteRenderer FindOrCreateRenderer(string objectName)
        {
            var root = _worldRoot != null ? _worldRoot : transform;
            foreach (var existing in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (existing.gameObject.name == objectName) return existing;

            var go = new GameObject(objectName);
            go.transform.SetParent(root, false);
            return go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (State == null) return;
            if (_tiles == null) Build();
            var g = State.Grid;
            for (int x = 0; x < g.Width; x++)
            {
                for (int y = 0; y < g.Height; y++)
                {
                    var tile = g.GetTile(new GridCoord(x, y));
                    var tileSr = _tiles[x, y];
                    var tilledSprite = tile.Type == TileType.Tilled ? PlaceholderArt.Art?.TilledSoil : null;
                    if (tilledSprite != null)
                    {
                        tileSr.sprite = tilledSprite;
                        // Real art has its own colour; just darken it a bit when watered (feedback
                        // rule — player must see wet vs dry at a glance) instead of a flat colour swap.
                        tileSr.color = tile.IsWatered ? new Color(0.55f, 0.55f, 0.6f) : Color.white;
                    }
                    else
                    {
                        tileSr.sprite = _baseTiles[x, y] != null ? _baseTiles[x, y] : PlaceholderArt.WhiteSprite;
                        // Authored pixel art already contains its palette. Applying the gameplay
                        // grass tint on top made the brown design tile render green at runtime.
                        tileSr.color = _baseTiles[x, y] != null && _baseTiles[x, y] != PlaceholderArt.WhiteSprite
                            ? Color.white
                            : PlaceholderArt.TileColor(tile);
                    }

                    var cropSr = _crops[x, y];
                    Sprite cropSprite = tile.Crop != null ? PlaceholderArt.CropSprite(tile.Crop) : null;
                    cropSr.enabled = cropSprite != null;
                    if (cropSprite != null)
                    {
                        cropSr.sprite = cropSprite;
                        // B2 (Sprint 1): both crops reuse the ParsnipCrop stage sheet — tint per crop
                        // id so Turnip vs. Potato is distinguishable on the field (PlaceholderArt.CropTint).
                        cropSr.color = PlaceholderArt.CropTint(tile.Crop.Id);
                    }

                    // S2-DEV-06 (DESIGN_BRIEFS.md [DSN-030]): standing tree vs. felled stump must be
                    // distinguishable by sight alone, not just by the Miss feedback chopping it gives —
                    // sprite (or fallback colour, if the art catalog isn't baked yet) swaps on the fell.
                    var objSr = _objects[x, y];
                    objSr.enabled = tile.Object != null;
                    if (tile.Object != null)
                    {
                        var objSprite = PlaceholderArt.TreeObjectSprite(tile.Object);
                        float scale = tile.Object.IsAlive ? TreeObjectScale : StumpObjectScale;
                        if (objSprite != null)
                        {
                            objSr.sprite = objSprite;
                            objSr.color = Color.white;
                        }
                        else
                        {
                            objSr.sprite = PlaceholderArt.WhiteSprite;
                            objSr.color = PlaceholderArt.TreeObjectColor(tile.Object);
                            scale = g.TileSize * 0.75f;
                        }
                        objSr.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                }
            }
        }
    }
}
