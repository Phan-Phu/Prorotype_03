using UnityEngine;
using Prototype.Application;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>
    /// Prototype placeholder art: coloured squares. No external assets required (AGENT_DEV §3.2.
    /// Art_Placeholder). Provides a 1x1 white sprite for feedback flashes and colour lookup per tile.
    /// </summary>
    public static class PlaceholderArt
    {
        private static Sprite _white;
        public static Sprite WhiteSprite
        {
            get
            {
                if (_white == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    // pixelsPerUnit=1 (NOT the Sprite.Create default of 100): every caller sizes this
                    // sprite by setting transform.localScale directly (e.g. localScale = TileSize),
                    // assuming the base sprite is exactly 1x1 world unit. Left at the default 100 PPU,
                    // the sprite was 0.01x0.01 units — grid tiles and feedback flashes were rendering,
                    // just 100x too small to see (bug found 2026-08-15: grid looked completely empty).
                    _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }
                return _white;
            }
        }

        private static ArtCatalog _art;
        private static bool _artLoadAttempted;

        /// <summary>
        /// Real sprite references baked by CIArt.BuildArtCatalog (Editor-only tool). Null if the
        /// catalog hasn't been baked yet — every caller must fall back to placeholder colour art
        /// in that case, never throw.
        /// </summary>
        public static ArtCatalog Art
        {
            get
            {
                if (!_artLoadAttempted)
                {
                    _artLoadAttempted = true;
                    _art = Resources.Load<ArtCatalog>("ArtCatalog");
                }
                return _art;
            }
        }

        /// <summary>Background tile colour. Crop growth is now conveyed by CropSprite (art), not colour.</summary>
        public static Color TileColor(Prototype.Domain.TileData t)
        {
            switch (t.Type)
            {
                case Prototype.Domain.TileType.Tilled:
                    return t.IsWatered ? new Color(0.30f, 0.18f, 0.10f) : new Color(0.45f, 0.30f, 0.15f);
                case Prototype.Domain.TileType.Water:
                    return new Color(0.15f, 0.35f, 0.75f);
                case Prototype.Domain.TileType.Blocked:
                    return new Color(0.25f, 0.25f, 0.25f);
                default:
                    return new Color(0.30f, 0.60f, 0.20f); // Grass
            }
        }

        /// <summary>
        /// Growth-stage sprite for a planted crop, or null if the art catalog isn't baked (caller
        /// falls back to placeholder colour). ParsnipCrop_0..4 run smallest -> ripe left-to-right
        /// in the source sheet, so DaysGrown/GrowthDays maps linearly onto the 5 stages.
        /// </summary>
        public static Sprite CropSprite(Prototype.Domain.CropInstance crop)
        {
            var stages = Art?.CropStages;
            if (stages == null || stages.Length != 5) return null;
            int growthDays = Prototype.Domain.CropDefinition.GrowthDays(crop.Id);
            int stage = Mathf.Clamp(Mathf.RoundToInt(crop.DaysGrown / (float)growthDays * 4f), 0, 4);
            return stages[stage];
        }

        public static Color FeedbackColor(Prototype.Domain.FeedbackKind k) => k switch
        {
            Prototype.Domain.FeedbackKind.Till    => new Color(0.55f, 0.35f, 0.18f),
            Prototype.Domain.FeedbackKind.Water   => new Color(0.2f, 0.5f, 1.0f),
            Prototype.Domain.FeedbackKind.Plant   => new Color(0.4f, 0.9f, 0.4f),
            Prototype.Domain.FeedbackKind.Harvest => new Color(1.0f, 0.85f, 0.2f),
            // S2-DEV-02/04 (DESIGN_BRIEFS.md [DSN-030] S2-DES-04): bark brown, distinct from Till's
            // flatter earth tone so a chop flash doesn't read as "you tilled this tile" at a glance.
            Prototype.Domain.FeedbackKind.Chop    => new Color(0.65f, 0.42f, 0.18f),
            _                                          => new Color(1.0f, 0.2f, 0.2f) // Miss
        };

        /// <summary>
        /// Grid-object sprite for a tile's TileObject (S2-DEV-06): the standing-tree art while
        /// choppable, the stump art once felled — swapping sprite on the felling hit is the "must be
        /// distinguishable by sight, not just by miss-feedback" requirement from DESIGN_BRIEFS.md
        /// [DSN-030] S2-DES-04. Null if the catalog isn't baked yet; caller falls back to TreeObjectColor.
        /// </summary>
        public static Sprite TreeObjectSprite(Prototype.Domain.TileObject obj)
        {
            if (obj == null || obj.Type != Prototype.Domain.TileObjectType.Tree) return null;
            return obj.IsAlive ? Art?.TreeStanding : Art?.TreeStump;
        }

        /// <summary>Placeholder-square colour for a tile object when the art catalog isn't baked yet — same fallback role PlaceholderArt.WhiteSprite+TileColor play for terrain.</summary>
        public static Color TreeObjectColor(Prototype.Domain.TileObject obj) =>
            obj.IsAlive ? new Color(0.15f, 0.45f, 0.12f) : new Color(0.42f, 0.30f, 0.16f);

        /// <summary>
        /// Inventory-slot icon for an item id, or null if there's no art for it (caller draws nothing).
        /// ToolIcons order matches ToolType (Hoe, Seed, WateringCan, Harvest) — see ArtCatalog doc.
        /// Axe/Wood use explicit named catalog fields so they cannot silently drift with ToolIcons'
        /// legacy seed-inclusive order.
        /// Both seed items share the one seed sprite in the source pack — there is no distinct potato
        /// seed asset (a real source-art limitation, not a bug). B2 (Sprint 1): tint the shared sprite
        /// per crop via ItemTint instead — caller must apply it (GUI.color / SpriteRenderer.color)
        /// alongside this icon so turnip vs. potato is distinguishable without new art.
        /// </summary>
        public static Sprite ItemIcon(string itemId)
        {
            var icons = Art?.ToolIcons;
            if (icons == null || icons.Length < 4) return null;
            if (itemId == Prototype.Domain.ToolItemIds.Hoe)         return icons[0];
            if (itemId == Prototype.Domain.ToolItemIds.WateringCan) return icons[2];
            if (itemId == Prototype.Domain.ToolItemIds.Harvest)     return icons[3];
            if (itemId == Prototype.Domain.ToolItemIds.Axe)         return Art?.AxeIcon ?? icons[0];
            if (itemId == Prototype.Domain.TreeDefinition.WoodItemId) return Art?.WoodIcon ?? icons[3];
            if (itemId == Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Turnip)) return icons[1];
            if (itemId == Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Potato)) return icons[1];
            if (itemId == Prototype.Domain.CropDefinition.ProduceItemId(Prototype.Domain.CropId.Turnip)) return icons[1];
            if (itemId == Prototype.Domain.CropDefinition.ProduceItemId(Prototype.Domain.CropId.Potato)) return icons[1];
            return null;
        }

        /// <summary>
        /// B2 (Sprint 1): Turnip and Potato share one seed sprite and one crop-stage sheet (no distinct
        /// source art for either) — recolour instead of leaving them visually identical. Applied to the
        /// seed icon (hotbar/inventory, via GUI.color) and to the planted crop sprite (WorldView, via
        /// SpriteRenderer.color). Turnip stays near the sheet's natural white; Potato gets an earthy
        /// tint, matching each crop's real-world colouring closely enough to read as intentional, not
        /// arbitrary. White (no tint) for anything that isn't a crop/seed item.
        /// </summary>
        public static Color ItemTint(string itemId)
        {
            if (itemId == Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Turnip)) return TurnipTint;
            if (itemId == Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Potato)) return PotatoTint;
            if (itemId == Prototype.Domain.CropDefinition.ProduceItemId(Prototype.Domain.CropId.Turnip)) return TurnipTint;
            if (itemId == Prototype.Domain.CropDefinition.ProduceItemId(Prototype.Domain.CropId.Potato)) return PotatoTint;
            if (itemId == Prototype.Domain.TreeDefinition.WoodItemId) return new Color(0.62f, 0.42f, 0.20f);
            return Color.white;
        }

        public static Color CropTint(Prototype.Domain.CropId id) =>
            id == Prototype.Domain.CropId.Turnip ? TurnipTint : PotatoTint;

        static readonly Color TurnipTint = new Color(0.85f, 0.55f, 0.95f); // pale purple, like a turnip bulb
        static readonly Color PotatoTint = new Color(0.80f, 0.60f, 0.30f); // earthy tan/brown, like a potato skin

        /// <summary>
        /// Draws a Sprite via its texture + UV sub-rect. Works uniformly whether the sprite is
        /// Single-mode (UV covers the whole texture) or packed in a multi-sprite atlas (UV is its
        /// sub-region) — shared by every OnGUI-based screen (ToolbarUI, InventoryScreenUI) so there's
        /// one place that gets the UV maths right.
        /// </summary>
        public static void DrawSprite(Rect screenRect, Sprite sprite)
        {
            var r = sprite.rect;
            var tex = sprite.texture;
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            GUI.DrawTextureWithTexCoords(screenRect, tex, uv);
        }

        /// <summary>Draws an icon without stretching it, centered inside the supplied slot.</summary>
        public static void DrawSpriteFit(Rect screenRect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            float sourceAspect = sprite.rect.width / (float)sprite.rect.height;
            float targetAspect = screenRect.width / screenRect.height;
            Rect fitted = screenRect;
            if (sourceAspect > targetAspect)
            {
                fitted.height = screenRect.width / sourceAspect;
                fitted.y += (screenRect.height - fitted.height) * 0.5f;
            }
            else
            {
                fitted.width = screenRect.height * sourceAspect;
                fitted.x += (screenRect.width - fitted.width) * 0.5f;
            }
            // The source item sheets include generous transparent padding. Scale the fitted
            // canvas up so the visible pixel art matches the design's slot occupancy.
            const float PixelArtIconScale = 1.45f;
            float extraW = fitted.width * (PixelArtIconScale - 1f) * 0.5f;
            float extraH = fitted.height * (PixelArtIconScale - 1f) * 0.5f;
            fitted = new Rect(fitted.x - extraW, fitted.y - extraH,
                fitted.width + extraW * 2f, fitted.height + extraH * 2f);
            DrawSprite(fitted, sprite);
        }

        /// <summary>Shrinks a Rect toward its centre by pad01 (0..0.5) on each side — used to inset icons within a slot.</summary>
        public static Rect Shrink(Rect r, float pad01)
        {
            float dx = r.width * pad01, dy = r.height * pad01;
            return new Rect(r.x + dx, r.y + dy, r.width - dx * 2f, r.height - dy * 2f);
        }
    }
}
