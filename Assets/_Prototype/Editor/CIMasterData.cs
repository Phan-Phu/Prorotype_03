using System.IO;
using UnityEditor;
using UnityEngine;
using Prototype.Infrastructure;

namespace Prototype.Application
{
    /// <summary>Creates the editor-authored runtime MasterData asset used by the composition root.</summary>
    public static class CIMasterData
    {
        const string Folder = "Assets/_Prototype/Resources";
        const string AssetPath = Folder + "/MasterData.asset";
        const string CsvFolder = "Assets/_Prototype/MasterData/CSV";

        [MenuItem("Prototype/Master Data/Create or Reset Prototype Asset")]
        public static void BuildMasterData()
        {
            BuildMasterDataFromCsv();
        }

        [MenuItem("Prototype/Master Data/Import CSV to Scriptable Asset")]
        public static void BuildMasterDataFromCsv()
        {
            try
            {
                EnsureFolder();
                var asset = GetOrCreateAsset();
                var source = new MasterDataCsvBundle(
                    ReadCsv("player.csv"), ReadCsv("time.csv"), ReadCsv("tools.csv"),
                    ReadCsv("crops.csv"), ReadCsv("tree.csv"), ReadCsv("items.csv"),
                    ReadCsv("starting_inventory.csv"), ReadCsv("npcs.csv"));
                var result = MasterDataCsvImporter.Apply(asset, source, ResolveIcon);
                if (!result.IsSuccess)
                {
                    Debug.LogError($"[CI] MasterData CSV import failed: {result.Failure.Message} ({result.Failure.Context})");
                    return;
                }

                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = asset;
                Debug.Log($"[CI] MasterData CSV imported into {AssetPath}");
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void BuildMasterDataFromCsvCli() => BuildMasterDataFromCsv();

        static MasterDataAsset GetOrCreateAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MasterDataAsset>(AssetPath);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<MasterDataAsset>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            return asset;
        }

        static string ReadCsv(string fileName)
        {
            // This class lives in Prototype.Application, so an unqualified Application resolves to
            // the project namespace instead of UnityEngine.Application. Keep the editor API explicit.
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            var path = Path.Combine(projectRoot, CsvFolder, fileName).Replace('\\', '/');
            if (!File.Exists(path)) throw new FileNotFoundException("Master Data CSV is missing", path);
            return File.ReadAllText(path);
        }

        static Sprite ResolveIcon(string path, string spriteName)
        {
            if (!string.IsNullOrWhiteSpace(spriteName))
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite && asset.name == spriteName) return (Sprite)asset;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Prototype/Resources"))
                AssetDatabase.CreateFolder("Assets/_Prototype", "Resources");
        }
    }
}
