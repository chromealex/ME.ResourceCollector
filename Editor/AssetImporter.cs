namespace ME.ResourceCollector {

    using UnityEngine;
    using UnityEditor;
    
    public class AssetImporter : AssetPostprocessor {

        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {

            var data = ResourceCollector.GetData();
            if (data == null) return;

            foreach (var path in importedAssets) {

                data.Refresh(AssetDatabase.AssetPathToGUID(path));

            }

            foreach (var path in deletedAssets) {

                data.Delete(AssetDatabase.AssetPathToGUID(path));

            }

        }

    }

}