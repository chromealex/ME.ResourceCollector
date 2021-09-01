using System.Linq;

namespace ME.ResourceCollector {
    
    using UnityEngine;

    public class ResourceCollectorData : ScriptableObject {

        [System.Serializable]
        public struct Item {

            public Object obj;
            public string guid;
            public long size;

            public System.Collections.Generic.List<Object> deps;

        }

        public System.Collections.Generic.List<Item> items = new System.Collections.Generic.List<Item>();
        private readonly System.Collections.Generic.Dictionary<string, long> cacheSizes = new System.Collections.Generic.Dictionary<string, long>();
        private readonly System.Collections.Generic.Dictionary<string, string> cacheSizesStr = new System.Collections.Generic.Dictionary<string, string>();

        public bool Delete(string guid) {

            for (int i = 0; i < this.items.Count; ++i) {

                var item = this.items[i];
                if (item.guid == guid) {
                    
                    this.cacheSizes.Remove(guid);
                    this.cacheSizesStr.Remove(guid);
                    this.items.RemoveAt(i);
                    return true;
                    
                }

            }
            
            return false;

        }

        public bool Refresh(string guid, bool addOnFail = true) {
            
            var visited = new System.Collections.Generic.HashSet<object>();
            for (int i = 0; i < this.items.Count; ++i) {

                var item = this.items[i];
                if (item.guid == guid) {

                    this.cacheSizes.Remove(guid);
                    this.cacheSizesStr.Remove(guid);
                    
                    if (item.deps != null) item.deps.Clear();
                    if (item.deps == null) item.deps = new System.Collections.Generic.List<Object>();
                    this.UpdateSize(ref item, visited);
                    this.items[i] = item;
                    
                    var str = UnityEditor.EditorUtility.FormatBytes(item.size);
                    this.cacheSizesStr.Add(guid, str);
                    return true;
                    
                }

            }

            if (addOnFail == false) return false;
            
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            this.Add(asset);
            return this.Refresh(guid, addOnFail: false);
            
        }

        public System.Collections.Generic.List<Object> GetDependencies(string guid) {

            for (int i = 0; i < this.items.Count; ++i) {

                if (this.items[i].guid == guid) return this.items[i].deps;

            }
            
            return null;

        }
        
        public string GetSizeStr(string guid) {

            if (this.cacheSizesStr.Count == 0) this.BuildCache();

            if (this.cacheSizesStr.TryGetValue(guid, out var str) == true) return str;
            return string.Empty;

        }

        public void BuildCache() {
            
            this.cacheSizesStr.Clear();
            for (int i = 0; i < this.items.Count; ++i) {

                var item = this.items[i];
                if (this.cacheSizesStr.ContainsKey(item.guid) == false) {
                    
                    this.cacheSizesStr.Add(item.guid, UnityEditor.EditorUtility.FormatBytes(item.size));
                    
                }
                
            }
            
            this.cacheSizes.Clear();
            for (int i = 0; i < this.items.Count; ++i) {

                var item = this.items[i];
                if (this.cacheSizes.ContainsKey(item.guid) == false) {
                    
                    this.cacheSizes.Add(item.guid, item.size);
                    
                }
                
            }
            
        }

        private void UpdateSize(ref Item item, System.Collections.Generic.HashSet<object> visited = null) {
            
            item.size = Utils.GetObjectSize(this, item.obj, visited, item.deps, collectUnityObjects: true);
            item.deps.Remove(item.obj);

        }

        public void CalculateSizes() {

            this.cacheSizes.Clear();
            
            var visited = new System.Collections.Generic.HashSet<object>();
            for (int i = 0; i < this.items.Count; ++i) {

                var item = this.items[i];
                UnityEditor.EditorUtility.DisplayProgressBar("Calculate Sizes", item.obj != null ? item.obj.ToString() : "Null", i / (float)this.items.Count);

                {
                    visited.Clear();
                    if (item.deps == null) item.deps = new System.Collections.Generic.List<Object>();
                    this.UpdateSize(ref item, visited);
                }
                this.items[i] = item;

            }

            this.BuildCache();
            
            UnityEditor.EditorUtility.ClearProgressBar();

        }

        public long UpdateSize(object obj, long size) {
            
            if (obj is Object tObj) {

                var p = UnityEditor.AssetDatabase.GetAssetPath(tObj);
                var guid = UnityEditor.AssetDatabase.AssetPathToGUID(p);
                if (this.cacheSizes.TryGetValue(guid, out var count) == true) {
                    
                    this.cacheSizes[guid] = size;
                    
                } else {
                    
                    this.cacheSizes.Add(guid, size);
                    
                }

            }
            
            return size;
            
        }

        private Item GetItem(string guid) {
            
            for (int i = 0; i < this.items.Count; ++i) {

                var item = this.items[i];
                if (item.guid == guid) {

                    return item;

                }

            }

            return default;

        }

        public bool GetSize(object obj, out long size) {

            size = -1L;
            if (obj is Object tObj) {

                var p = UnityEditor.AssetDatabase.GetAssetPath(tObj);
                var guid = UnityEditor.AssetDatabase.AssetPathToGUID(p);
                if (this.cacheSizes.TryGetValue(guid, out var count) == true) {
                    
                    size = count;
                    return true;
                    
                }

                size = this.GetItem(guid).size;
                size = this.UpdateSize(obj, size);
                return true;

            }
            
            return false;

        }

        public void Add(Object obj) {
            
            if (obj == this) return;
            
            this.items.Add(new Item() {
                obj = obj,
                guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(obj)),
                size = 0L,
            });
            
        }

        public string testGuid;
        [ContextMenu("Test")]
        public void Test() {
            
            var visited = new System.Collections.Generic.HashSet<object>();
            var item = this.items.FirstOrDefault(x => x.guid == this.testGuid);
            if (item.deps == null) item.deps = new System.Collections.Generic.List<Object>();
            item.size = Utils.GetObjectSize(this, item.obj, visited, item.deps, collectUnityObjects: true);
            Debug.Log("Obj size: " + item.size);
            foreach (var obj in visited) {
                Debug.Log(obj);
            }

        }

    }

}