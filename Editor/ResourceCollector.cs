using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ME.ResourceCollector {

    public static class ResourceCollector {

        public static ResourceCollectorData GetData() {
            
            var dataPath = "Assets/ME.ResourceCollector/Editor/EditorData.asset";
            return AssetDatabase.LoadAssetAtPath<ResourceCollectorData>(dataPath);
            
        }

        [UnityEditor.MenuItem("Tools/ME.ResourceCollector/Recalculate Resource Sizes")]
        public static void CalculateSizes() {

            var data = GetData();
            if (data == null) return;
            
            try {

                data.CalculateSizes();

            } catch (System.Exception ex) {
                Debug.LogException(ex);
            }

            EditorUtility.SetDirty(data);
            EditorUtility.ClearProgressBar();
            
            Debug.Log("Done");

        }
        
        [UnityEditor.MenuItem("Tools/ME.ResourceCollector/Update Resources")]
        public static void Collect() {

            var dataPath = "Assets/ME.ResourceCollector/Editor/EditorData.asset";
            var data = AssetDatabase.LoadAssetAtPath<ResourceCollectorData>(dataPath);
            if (data == null) {
                var instance = ResourceCollectorData.CreateInstance<ResourceCollectorData>();
                AssetDatabase.CreateAsset(instance, dataPath);
                AssetDatabase.ImportAsset(dataPath);
                data = AssetDatabase.LoadAssetAtPath<ResourceCollectorData>(dataPath);
            }
            
            data.items.Clear();

            var searches = new string[] {
                "t:Texture",
                "t:Material",
                "t:Sprite",
                "t:ScriptableObject",
                "t:prefab",
            };

            try {

                for (int i = 0; i < searches.Length; ++i) {
                    
                    EditorUtility.DisplayProgressBar("Collect Objects", searches[i], i / (float)searches.Length);

                    var objs = AssetDatabase.FindAssets(searches[i]);
                    foreach (var guid in objs) {
                    
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                        if (obj != null) {

                            data.Add(obj);

                        }
                    
                    }
                    
                }

                EditorUtility.SetDirty(data);

                data.CalculateSizes();

            } catch (System.Exception ex) {
                Debug.LogException(ex);
            }

            EditorUtility.SetDirty(data);
            EditorUtility.ClearProgressBar();

        }
        
    }

}
