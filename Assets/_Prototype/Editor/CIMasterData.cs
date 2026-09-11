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

        [MenuItem("Prototype/Master Data/Create or Reset Prototype Asset")]
        public static void BuildMasterData()
        {
            EnsureFolder();
            var asset = AssetDatabase.LoadAssetAtPath<MasterDataAsset>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MasterDataAsset>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.ResetToPrototypeDefaults();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            Debug.Log($"[CI] MasterData asset ready: {AssetPath}");
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Prototype/Resources"))
                AssetDatabase.CreateFolder("Assets/_Prototype", "Resources");
        }
    }
}
