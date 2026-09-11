using System.IO;
using UnityEditor;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// One-shot bake of real art references into Resources/ArtCatalog.asset (AGENT_DEV art rule:
    /// runtime code stays free of AssetDatabase). Run via
    /// -executeMethod Prototype.Application.CIArt.BuildArtCatalog whenever the source sprite sheets
    /// change. Picks named sub-sprites explicitly (never by guessed index/order) so a resliced sheet
    /// can't silently swap in the wrong frame. Row-to-direction mapping for the farmer walk cycles was
    /// visually confirmed via a magnified contact-sheet export (Artifacts/preview_rows_*.png), not
    /// guessed. Farmhouse is a building TILESET (78 pieces), not one sprite — we use the whole texture
    /// as a single image instead of guessing which pieces reconstruct "the house".
    /// </summary>
    public static class CIArt
    {
        const string ParsnipCropPath = "Assets/Sprite Textures/Crops/ParsnipCrop.png";
        const string FarmerPath      = "Assets/Output Textures/customised_farmer.png";
        const string TreePath        = "Assets/Sprite Textures/Trees/CanyonOak.png";
        const string FarmhousePath   = "Assets/Sprite Textures/Buildings/Farmhouse1.png";
        const string GrassPath       = "Assets/Sprite Textures/Objects/Grass1.png";
        const string StonePath       = "Assets/Sprite Textures/Objects/Stone.png";
        const string DugGroundPath   = "Assets/Sprite Textures/Tile Sprites/DugGround.png";
        const string CursorValidPath   = "Assets/Sprite Textures/UI/GreenGridCursor.png";
        const string CursorInvalidPath = "Assets/Sprite Textures/UI/RedGridCursor.png";
        const string ToolsPath        = "Assets/Sprite Textures/Tools/tools.png";
        const string SeedPath         = "Assets/Sprite Textures/Objects/ParsnipSeeds.png";
        const string WoodPath         = "Assets/Sprite Textures/Objects/Wood.png";
        const string InventoryBarPath       = "Assets/Sprite Textures/UI/InventoryBar.png";
        const string InventoryHighlightPath = "Assets/Sprite Textures/UI/InventoryHighlight.png";
        const string InventoryPlayerPath    = "Assets/Sprite Textures/UI/InventoryPlayer.png";
        const string CoraPath          = "Assets/Sprite Textures/NPCs/Cora/Cora.png";
        const string CoraPortraitPath  = "Assets/Sprite Textures/NPCs/Cora/Cora_Portrait.png";
        const string ButchPath         = "Assets/Sprite Textures/NPCs/Butch/Butch.png";
        const string ButchPortraitPath = "Assets/Sprite Textures/NPCs/Butch/Butch_Portrait.png";
        const string DialogueBoxPath   = "Assets/Sprite Textures/NPCs/Dialogue/DialogueBox.png";
        const string OutPath         = "Assets/_Prototype/Resources/ArtCatalog.asset";

        public static void BuildArtCatalog()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));

            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(OutPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArtCatalog>();
                AssetDatabase.CreateAsset(catalog, OutPath);
            }

            catalog.CropStages = new Sprite[5];
            for (int i = 0; i < 5; i++)
                catalog.CropStages[i] = LoadNamedSprite(ParsnipCropPath, $"ParsnipCrop_{i}");

            // DugGround_10 measured (Artifacts/analyze.log) as the only 100%-opaque piece of the
            // 32-tile autotile set — every other piece has a transparent edge/corner for neighbour
            // blending, which would show broken gaps since GridMap doesn't do neighbour-aware tiling.
            catalog.TilledSoil = LoadNamedSprite(DugGroundPath, "DugGround_10");

            // Row layout confirmed by visual export (Artifacts/preview_rows_*.png), 24 cols/row:
            // row 0 = facing down, row 1 = facing left, row 2 = facing up. No right-facing row —
            // PlayerController mirrors WalkLeft instead. Column 0 = standing pose (kept as Idle*);
            // columns 1-4 = the actual walk-cycle frames (column 0 excluded — see ArtCatalog doc).
            catalog.IdleDown = LoadNamedSprite(FarmerPath, "customised_farmer_0");
            catalog.IdleLeft = LoadNamedSprite(FarmerPath, "customised_farmer_24");
            catalog.IdleUp   = LoadNamedSprite(FarmerPath, "customised_farmer_48");
            catalog.WalkDown = LoadRow(FarmerPath, rowIndex: 0, startCol: 1, count: 4);
            catalog.WalkLeft = LoadRow(FarmerPath, rowIndex: 1, startCol: 1, count: 4);
            catalog.WalkUp   = LoadRow(FarmerPath, rowIndex: 2, startCol: 1, count: 4);

            catalog.Tree      = LoadNamedSprite(TreePath, "CanyonOak_0"); // the tree itself (largest piece, top of sheet)
            catalog.GrassTuft = AssetDatabase.LoadAssetAtPath<Sprite>(GrassPath); // Single-mode texture
            catalog.Stone     = AssetDatabase.LoadAssetAtPath<Sprite>(StonePath); // Single-mode texture
            catalog.Farmhouse = LoadOrCreateWholeTextureSprite(FarmhousePath, catalog);

            // S2-DEV-06 (DESIGN_BRIEFS.md [DSN-030]): choppable tree object on the grid. CanyonOak_0
            // reused for the standing tree (same asset as the decorative Tree above); CanyonOak_3
            // confirmed via a magnified crop export (not guessed by index) to be a genuine stump piece.
            catalog.TreeStanding = LoadNamedSprite(TreePath, "CanyonOak_0");
            catalog.TreeStump    = LoadNamedSprite(TreePath, "CanyonOak_3");

            catalog.CursorValid   = AssetDatabase.LoadAssetAtPath<Sprite>(CursorValidPath);   // Single-mode
            catalog.CursorInvalid = AssetDatabase.LoadAssetAtPath<Sprite>(CursorInvalidPath); // Single-mode

            // Order matches ToolType enum (Hoe, Seed, WateringCan, Harvest) — see ArtCatalog doc for
            // how each icon was identified (magnified export, not guessed).
            catalog.ToolIcons = new[]
            {
                LoadNamedSprite(ToolsPath, "tools_21"),                  // Hoe
                AssetDatabase.LoadAssetAtPath<Sprite>(SeedPath),         // Seed (Single-mode)
                LoadNamedSprite(ToolsPath, "tools_63"),                  // WateringCan
                LoadNamedSprite(ToolsPath, "tools_252"),                 // Harvest (basket — no sickle in the sheet)
            };
            // BUG-050-3: starting Axe and chopped Wood are inventory items too. tools_0 is the
            // clearest axe-shaped sprite in the source sheet; Wood.png is a dedicated object sprite.
            catalog.AxeIcon = LoadNamedSprite(ToolsPath, "tools_0");
            catalog.WoodIcon = AssetDatabase.LoadAssetAtPath<Sprite>(WoodPath); // Single-mode
            catalog.InventoryBar       = AssetDatabase.LoadAssetAtPath<Sprite>(InventoryBarPath);       // Single-mode
            catalog.InventoryHighlight = AssetDatabase.LoadAssetAtPath<Sprite>(InventoryHighlightPath); // Single-mode
            catalog.InventoryPlayer    = AssetDatabase.LoadAssetAtPath<Sprite>(InventoryPlayerPath);    // Single-mode

            catalog.CoraSprite    = LoadNamedSprite(CoraPath, "Cora_0");
            catalog.CoraPortrait  = LoadNamedSprite(CoraPortraitPath, "Cora_Portrait_0");
            catalog.ButchSprite   = LoadNamedSprite(ButchPath, "Butch_0");
            catalog.ButchPortrait = LoadNamedSprite(ButchPortraitPath, "Butch_Portrait_0");
            catalog.DialogueBox   = AssetDatabase.LoadAssetAtPath<Sprite>(DialogueBoxPath);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            int cropsFound = CountNonNull(catalog.CropStages);
            int downFound  = CountNonNull(catalog.WalkDown);
            int leftFound  = CountNonNull(catalog.WalkLeft);
            int upFound    = CountNonNull(catalog.WalkUp);

            UnityEngine.Debug.Log($"[CI] ArtCatalog built: cropStages={cropsFound}/5 " +
                $"walkDown={downFound}/4 walkLeft={leftFound}/4 walkUp={upFound}/4 " +
                $"tilledSoil={catalog.TilledSoil != null} " +
                $"tree={catalog.Tree != null} farmhouse={catalog.Farmhouse != null} " +
                $"grass={catalog.GrassTuft != null} stone={catalog.Stone != null} " +
                $"treeStanding={catalog.TreeStanding != null} treeStump={catalog.TreeStump != null} " +
                $"cursor={catalog.CursorValid != null && catalog.CursorInvalid != null} " +
                $"toolIcons={CountNonNull(catalog.ToolIcons)}/4 axe={catalog.AxeIcon != null} wood={catalog.WoodIcon != null} " +
                $"hotbar={catalog.InventoryBar != null && catalog.InventoryHighlight != null} " +
                $"inventoryPanel={catalog.InventoryPlayer != null} " +
                $"npcs={catalog.CoraSprite != null && catalog.CoraPortrait != null && catalog.ButchSprite != null && catalog.ButchPortrait != null} " +
                $"dialogueBox={catalog.DialogueBox != null}");

            if (cropsFound != 5 || downFound != 4 || leftFound != 4 || upFound != 4
                || catalog.IdleDown == null || catalog.IdleLeft == null || catalog.IdleUp == null
                || catalog.TilledSoil == null
                || catalog.Tree == null || catalog.Farmhouse == null
                || catalog.GrassTuft == null || catalog.Stone == null
                || catalog.TreeStanding == null || catalog.TreeStump == null
                || catalog.CursorValid == null || catalog.CursorInvalid == null
                || CountNonNull(catalog.ToolIcons) != 4
                || catalog.AxeIcon == null || catalog.WoodIcon == null
                || catalog.InventoryBar == null || catalog.InventoryHighlight == null
                || catalog.InventoryPlayer == null
                || catalog.CoraSprite == null || catalog.CoraPortrait == null
                || catalog.ButchSprite == null || catalog.ButchPortrait == null
                || catalog.DialogueBox == null)
            {
                UnityEngine.Debug.LogError("[CI] ArtCatalog missing expected sprites — check source sheet names.");
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        static int CountNonNull(Sprite[] arr)
        {
            int n = 0;
            foreach (var s in arr) if (s != null) n++;
            return n;
        }

        static Sprite LoadNamedSprite(string path, string spriteName)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is Sprite s && s.name == spriteName) return s;
            return null;
        }

        /// <summary>Loads customised_farmer_{rowIndex*24 + startCol .. +count-1} from the 24-col sheet.</summary>
        static Sprite[] LoadRow(string path, int rowIndex, int startCol, int count)
        {
            var result = new Sprite[count];
            for (int c = 0; c < count; c++)
                result[c] = LoadNamedSprite(path, $"customised_farmer_{rowIndex * 24 + startCol + c}");
            return result;
        }

        /// <summary>
        /// Farmhouse1.png is a 16x16 building TILESET (78 pieces, meant for hand-assembling a house),
        /// not one sprite. Rather than guess which piece(s) reconstruct the house, wrap the whole raw
        /// texture as a single sprite — cheap and correct since the full canvas already renders as one
        /// coherent house image. Sprite.Create only needs the Texture2D reference (no CPU pixel read),
        /// so this works even though the source texture has isReadable=false.
        /// </summary>
        static Sprite LoadOrCreateWholeTextureSprite(string path, ArtCatalog catalog)
        {
            if (catalog.Farmhouse != null) return catalog.Farmhouse; // already baked as a sub-asset, reuse
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return null;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 16f);
            sprite.name = "Farmhouse1_Whole";
            AssetDatabase.AddObjectToAsset(sprite, catalog);
            return sprite;
        }
    }
}
