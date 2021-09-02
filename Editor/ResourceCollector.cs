using System.Collections;
using System.Collections.Generic;

namespace ME.ResourceCollector {

    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    
    #region MAIN
    public class ResourceCollector : ScriptableObject {

        public static ResourceCollector GetData() {

            var dataPath = "Assets/ME.ResourceCollector/Editor/EditorData.asset";
            var data = AssetDatabase.LoadAssetAtPath<ResourceCollector>(dataPath);
            if (data == null) {
                var instance = ResourceCollector.CreateInstance<ResourceCollector>();
                AssetDatabase.CreateAsset(instance, dataPath);
                AssetDatabase.ImportAsset(dataPath);
                data = AssetDatabase.LoadAssetAtPath<ResourceCollector>(dataPath);
            }
            return data;

        }
        
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

        public void Save() {
            
            EditorUtility.SetDirty(this);
            
        }

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
                    
                    this.cacheSizes.Add(guid, 0L);
                    
                    if (item.deps != null) item.deps.Clear();
                    if (item.deps == null) item.deps = new System.Collections.Generic.List<Object>();
                    this.UpdateSize(ref item, visited);
                    this.items[i] = item;
                    this.Save();
                    
                    var str = UnityEditor.EditorUtility.FormatBytes(item.size);
                    if (this.cacheSizes.ContainsKey(guid) == false) this.cacheSizes.Add(guid, item.size);
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

            if (this.cacheSizesStr.Count < 100) this.BuildCache();

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
            this.Save();
            
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
                    return size > 0L;
                    
                }

                size = this.GetItem(guid).size;
                if (size > 0L) {
                    size = this.UpdateSize(obj, size);
                    return true;
                }
                
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
            this.Save();
            
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
    #endregion
    
    #region MENU
    public static class Menu {

        [UnityEditor.MenuItem("Tools/ME.ResourceCollector/Recalculate Resource Sizes")]
        public static void CalculateSizes() {

            var data = ResourceCollector.GetData();
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

            var data = ResourceCollector.GetData();
            if (data == null) return;
            
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

            Debug.Log("Done");

        }
        
    }
    #endregion
    
    #region AssetsImporter
    public class AssetsImporter : AssetPostprocessor {

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
    #endregion
    
    #region GUI:Hierarchy
    [InitializeOnLoad]
    public static class HierarchyGUIEditor {

        static HierarchyGUIEditor() {

            HierarchyGUIEditor.Reassign();

        }

        [UnityEditor.Callbacks.DidReloadScripts]
        public static void OnAssemblyReload() {
            
            HierarchyGUIEditor.Reassign();

            EditorApplication.delayCall += () => {
                
                HierarchyGUIEditor.Reassign();

            };

        }

        [InitializeOnLoadMethod]
        public static void Reassign() {
            
            EditorApplication.projectWindowItemOnGUI -= HierarchyGUIEditor.OnElementGUI;
            EditorApplication.projectWindowItemOnGUI += HierarchyGUIEditor.OnElementGUI;

        }

        private static GUIStyle buttonStyle;
        private static ResourceCollector resourceCollector;
        public static void OnElementGUI(string guid, UnityEngine.Rect rect) {

            if (HierarchyGUIEditor.buttonStyle == null) {
                HierarchyGUIEditor.buttonStyle = new GUIStyle(EditorStyles.miniPullDown);
                HierarchyGUIEditor.buttonStyle.fontSize = 10;
                HierarchyGUIEditor.buttonStyle.alignment = TextAnchor.MiddleRight;
            }
            
            if (HierarchyGUIEditor.resourceCollector == null) HierarchyGUIEditor.resourceCollector = ResourceCollector.GetData();
            if (HierarchyGUIEditor.resourceCollector == null) return;

            var size = HierarchyGUIEditor.resourceCollector.GetSizeStr(guid);
            if (string.IsNullOrEmpty(size) == false) {
                
                var selectionRect = new Rect(rect);
                var s = HierarchyGUIEditor.buttonStyle.CalcSize(new GUIContent(size));
                selectionRect.width = s.x;
                selectionRect.height = EditorGUIUtility.singleLineHeight;
                selectionRect.x = rect.x + rect.width - selectionRect.width;
                
                if (GUI.Button(selectionRect, size, HierarchyGUIEditor.buttonStyle) == true) {

                    var p = GUIUtility.GUIToScreenPoint(selectionRect.min);
                    selectionRect.x = p.x;
                    selectionRect.y = p.y;
                    DependenciesPopup.Show(selectionRect, new Vector2(200f, 300f), HierarchyGUIEditor.resourceCollector, guid);

                }
                
            }

        }

    }

    public class DependenciesPopup : EditorWindow {

        public Vector2 scrollPosition;
        public Rect rect;
        public Vector2 size;
        private ResourceCollector data;
        private string guid;
        private System.Collections.Generic.List<Object> deps;

        private float height;
        private bool heightCalc;
        
        public static void Show(Rect buttonRect, Vector2 size, ResourceCollector data, string guid) {
            
            var popup = DependenciesPopup.CreateInstance<DependenciesPopup>();
            popup.data = data;
            popup.guid = guid;
            popup.size = size;
            popup.rect = buttonRect;
            popup.UpdateDeps();
            popup.ShowAsDropDown(buttonRect, size);
            
        }

        private void UpdateDeps() {
            
            this.deps = this.data.GetDependencies(this.guid).OrderByDescending(x => {

                if (this.data.GetSize(x, out var size) == true) {
                    
                    return size;
                    
                }

                return -1L;

            }).ToList();
            
        }

        private void OnGUI() {
            
            if (this.deps != null) {

                GUILayout.BeginHorizontal(GUILayout.Height(20f));
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label("References Count: " + this.deps.Count, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.miniButton) == true) {

                    if (this.data.Refresh(this.guid) == true) {

                        this.UpdateDeps();

                    }

                }
                GUILayout.EndHorizontal();
                this.height = 0f;
                if (this.heightCalc == false) {

                    var rect = GUILayoutUtility.GetLastRect();
                    this.height += rect.height;

                }
                
                this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);
                foreach (var dep in this.deps) {

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(dep, typeof(Object), allowSceneObjects: true);
                    EditorGUI.EndDisabledGroup();
                    var rect = GUILayoutUtility.GetLastRect();
                    HierarchyGUIEditor.OnElementGUI(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dep)), rect);
                    if (this.heightCalc == false) {

                        this.height += rect.height + 2f;

                    }
                    
                }
                GUILayout.EndScrollView();
                
                if (Event.current.type == EventType.Repaint && this.heightCalc == false) {

                    this.height += 10f;
                    if (this.size.y > this.height) {
                        this.size.y = this.height;
                        this.ShowAsDropDown(this.rect, this.size);
                    }

                    this.heightCalc = true;

                }

            } else {
                
                GUILayout.Label("References Count: 0");
                
            }

        }

    }
    #endregion
    
    #region GUI:ObjectFieldDrawer
    [CustomPropertyDrawer(typeof(Object), true)]
    public class ObjectFieldDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            EditorGUI.PropertyField(position, property, label);

            var obj = property.objectReferenceValue;
            if (obj != null) {

                var path = AssetDatabase.GetAssetPath(obj);
                var guid = AssetDatabase.AssetPathToGUID(path);
                position.width -= 20f;
                HierarchyGUIEditor.OnElementGUI(guid, position);

            }

        }

    }
    #endregion
    
    #region Parser
    public static class Utils {

        private static System.Reflection.MethodInfo getTextureSizeMethod;

        public static long GetTextureSize(UnityEngine.Texture texture) {

            if (Utils.getTextureSizeMethod == null) {

                var textureUtils = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.TextureUtil");
                Utils.getTextureSizeMethod = textureUtils.GetMethod("GetStorageMemorySize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            }

            return (int)Utils.getTextureSizeMethod.Invoke(null, new[] { texture });

        }

        public static long GetMeshSize(UnityEngine.Mesh mesh) {

            var size = 0L;

            var attributes = mesh.GetVertexAttributes();
            var vertexSize = attributes.Sum(attr => Utils.ConvertFormatToSize(attr.format) * attr.dimension);
            size += mesh.vertexCount * vertexSize;

            var indexCount = Utils.CalcTotalIndices(mesh);
            var indexSize = mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16 ? 2 : 4;
            size += indexCount * indexSize;

            return size;

        }

        public static long GetObjectSize(ResourceCollector data, object obj, System.Collections.Generic.HashSet<object> visited,
                                         System.Collections.Generic.List<UnityEngine.Object> deps, bool collectUnityObjects) {

            if (visited == null) {
                visited = new System.Collections.Generic.HashSet<object>();
            }

            var ptr = obj;
            if (visited.Contains(ptr) == true) {
                return 0L;
            }

            visited.Add(ptr);

            var pointerSize = System.IntPtr.Size;
            var size = 0L;
            if (obj == null) {
                return pointerSize;
            }

            var charSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<char>();

            var type = obj.GetType();
            if (typeof(UnityEngine.Renderer).IsAssignableFrom(type) == true) {

                var typedObj = (UnityEngine.Renderer)obj;
                if (typedObj == null) {
                    return pointerSize;
                }

                if (typedObj.sharedMaterials != null) {

                    foreach (var mat in typedObj.sharedMaterials) {

                        if (mat == null) {
                            continue;
                        }

                        size += Utils.GetObjectSize(data, mat, visited, deps, collectUnityObjects);

                    }

                } else {

                    size += Utils.GetObjectSize(data, typedObj.sharedMaterial, visited, deps, collectUnityObjects);

                }

            }

            if (type == typeof(UnityEngine.Mesh)) {

                var typedObj = (UnityEngine.Mesh)obj;
                if (typedObj == null) {
                    return pointerSize;
                }

                var mesh = typedObj;
                deps.Add(mesh);
                if (data.GetSize(mesh, out var s) == true) {
                    size += s;
                } else {
                    size += data.UpdateSize(mesh, Utils.GetMeshSize(mesh));
                }

            }

            if (type == typeof(UnityEngine.MeshFilter)) {

                var typedObj = (UnityEngine.MeshFilter)obj;
                if (typedObj == null) {
                    return pointerSize;
                }

                var mesh = typedObj.sharedMesh;
                if (mesh == null) {
                    return pointerSize;
                }

                deps.Add(mesh);

                if (data.GetSize(mesh, out var s) == true) {
                    size += s;
                } else {
                    size += data.UpdateSize(mesh, Utils.GetMeshSize(mesh));
                }

            }

            if (type == typeof(UnityEngine.SpriteRenderer)) {

                var typedObj = (UnityEngine.SpriteRenderer)obj;
                if (typedObj == null) {
                    return pointerSize;
                }

                size += Utils.GetObjectSize(data, typedObj.sprite, visited, deps, collectUnityObjects);

            }

            if (typeof(UnityEngine.Component).IsAssignableFrom(type) == true) {

                var comp = (UnityEngine.Component)obj;
                if (comp == null) {
                    return pointerSize;
                }

                var go = ((UnityEngine.Component)obj).gameObject;
                if (go == null) {
                    return pointerSize;
                }

                var tempResults = new System.Collections.Generic.List<UnityEngine.Component>();
                tempResults.AddRange(go.GetComponents<UnityEngine.Component>());
                foreach (var item in tempResults) {

                    size += Utils.GetObjectSize(data, item, visited, deps, collectUnityObjects);

                }

            }

            if (type == typeof(UnityEngine.GameObject)) {

                var go = (UnityEngine.GameObject)obj;
                var tempResults = new System.Collections.Generic.List<UnityEngine.Component>();
                var nextGo = go;

                if (go == null) {
                    return pointerSize;
                }

                tempResults.AddRange(go.GetComponents<UnityEngine.Component>());

                System.Action<UnityEngine.GameObject> collectGo = null;
                collectGo = (UnityEngine.GameObject goRoot) => {

                    if (goRoot == null) {
                        return;
                    }

                    for (var i = 0; i < goRoot.transform.childCount; ++i) {

                        var child = goRoot.transform.GetChild(i);
                        tempResults.AddRange(child.GetComponents<UnityEngine.Component>());
                        collectGo.Invoke(child.gameObject);

                    }

                };
                collectGo.Invoke(nextGo);

                foreach (var item in tempResults) {

                    size += Utils.GetObjectSize(data, item, visited, deps, collectUnityObjects);

                }

            } else if (type == typeof(UnityEngine.Material)) {

                var mat = (UnityEngine.Material)obj;
                if (mat == null) {
                    return pointerSize;
                }

                deps.Add(mat);
                var props = mat.GetTexturePropertyNames();
                foreach (var prop in props) {

                    var tex = mat.GetTexture(prop);
                    if (tex == null) {
                        continue;
                    }

                    size += Utils.GetObjectSize(data, tex, visited, deps, collectUnityObjects);

                }

            } else if (type == typeof(UnityEngine.Sprite)) {

                var sprite = (UnityEngine.Sprite)obj;
                if (sprite == null) {
                    return pointerSize;
                }

                var tex = sprite.texture;
                deps.Add((UnityEngine.Sprite)obj);
                if (data.GetSize(tex, out var s) == true) {
                    size += s;
                } else {
                    size += data.UpdateSize(tex, Utils.GetTextureSize(tex));
                }

            } else if (type == typeof(UnityEngine.Texture) ||
                       type == typeof(UnityEngine.Texture2D) ||
                       type == typeof(UnityEngine.RenderTexture) ||
                       typeof(UnityEngine.Texture).IsAssignableFrom(type) == true) {

                var tex = (UnityEngine.Texture)obj;
                if (tex == null) {
                    return pointerSize;
                }

                deps.Add(tex);
                if (data.GetSize(tex, out var s) == true) {
                    size += s;
                } else {
                    size += data.UpdateSize(tex, Utils.GetTextureSize(tex));
                }

            } else if (collectUnityObjects == false && typeof(UnityEngine.Object).IsAssignableFrom(type) == true) {

                size += pointerSize;

            } else if (type.IsEnum == true) {

                size += sizeof(int);

            } else if (type.IsPointer == true) {

                size += pointerSize;

            } else if (type.IsArray == false && type.IsValueType == true &&
                       (type.IsPrimitive == true || Unity.Collections.LowLevel.Unsafe.UnsafeUtility.IsBlittable(type) == true)) {

                size += System.Runtime.InteropServices.Marshal.SizeOf(obj);

            } else {

                var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (fields.Length == 0 && type.IsValueType == true) {

                    size += System.Runtime.InteropServices.Marshal.SizeOf(obj);

                } else {

                    foreach (var field in fields) {

                        var fieldType = field.FieldType;
                        if (collectUnityObjects == false && typeof(UnityEngine.Object).IsAssignableFrom(fieldType) == true) {

                            size += pointerSize;
                            continue;

                        }

                        if (fieldType.IsEnum == true) {

                            size += sizeof(int);

                        } else if (fieldType.IsPointer == true) {

                            size += pointerSize;

                        } else if (fieldType.IsArray == false && fieldType.IsValueType == true &&
                                   (fieldType.IsPrimitive == true || Unity.Collections.LowLevel.Unsafe.UnsafeUtility.IsBlittable(fieldType) == true)) {

                            size += System.Runtime.InteropServices.Marshal.SizeOf(fieldType);

                        } else if (fieldType == typeof(string)) {

                            var str = (string)field.GetValue(obj);
                            if (str != null) {

                                size += charSize * str.Length;

                            }

                        } else if (fieldType.IsValueType == true) {

                            size += Utils.GetObjectSize(data, field.GetValue(obj), visited, deps,
                                                        collectUnityObjects); //Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(fieldType);

                        } else if (fieldType.IsArray == true && fieldType.GetArrayRank() == 1) {

                            var arr = (System.Array)field.GetValue(obj);
                            if (arr != null) {

                                for (var i = 0; i < arr.Length; ++i) {
                                    size += Utils.GetObjectSize(data, arr.GetValue(i), visited, deps, collectUnityObjects);
                                }

                            }

                        } else {

                            size += Utils.GetObjectSize(data, field.GetValue(obj), visited, deps, collectUnityObjects);

                        }

                    }

                }

            }

            return size;

        }

        private static int CalcTotalIndices(UnityEngine.Mesh mesh) {
            var totalCount = 0;
            for (var i = 0; i < mesh.subMeshCount; i++) {
                totalCount += (int)mesh.GetIndexCount(i);
            }

            return totalCount;
        }

        private static int ConvertFormatToSize(UnityEngine.Rendering.VertexAttributeFormat format) {
            switch (format) {
                case UnityEngine.Rendering.VertexAttributeFormat.Float32:
                case UnityEngine.Rendering.VertexAttributeFormat.UInt32:
                case UnityEngine.Rendering.VertexAttributeFormat.SInt32:
                    return 4;

                case UnityEngine.Rendering.VertexAttributeFormat.Float16:
                case UnityEngine.Rendering.VertexAttributeFormat.UNorm16:
                case UnityEngine.Rendering.VertexAttributeFormat.SNorm16:
                case UnityEngine.Rendering.VertexAttributeFormat.UInt16:
                case UnityEngine.Rendering.VertexAttributeFormat.SInt16:
                    return 2;

                case UnityEngine.Rendering.VertexAttributeFormat.UNorm8:
                case UnityEngine.Rendering.VertexAttributeFormat.SNorm8:
                case UnityEngine.Rendering.VertexAttributeFormat.UInt8:
                case UnityEngine.Rendering.VertexAttributeFormat.SInt8:
                    return 1;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(format), format, $"Unknown vertex format {format}");
            }
        }

    }
    #endregion

}
