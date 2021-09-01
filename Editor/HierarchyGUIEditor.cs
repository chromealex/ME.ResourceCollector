using System.Linq;

namespace ME.ResourceCollector {

    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class HierarchyGUIEditor {

        static HierarchyGUIEditor() {

            EditorApplication.projectWindowItemOnGUI += HierarchyGUIEditor.OnElementGUI;

        }

        private static GUIStyle labelStyle;
        private static ResourceCollectorData resourceCollectorData;
        public static void OnElementGUI(string guid, UnityEngine.Rect rect) {

            if (HierarchyGUIEditor.labelStyle == null) {
                HierarchyGUIEditor.labelStyle = new GUIStyle(EditorStyles.miniPullDown);
                HierarchyGUIEditor.labelStyle.alignment = TextAnchor.MiddleRight;
            }
            if (HierarchyGUIEditor.resourceCollectorData == null) HierarchyGUIEditor.resourceCollectorData = ResourceCollector.GetData();
            if (HierarchyGUIEditor.resourceCollectorData == null) return;

            var size = HierarchyGUIEditor.resourceCollectorData.GetSizeStr(guid);
            if (string.IsNullOrEmpty(size) == false) {
                var selectionRect = new Rect(rect);
                var s = HierarchyGUIEditor.labelStyle.CalcSize(new GUIContent(size));
                selectionRect.width = s.x;
                selectionRect.x = rect.x + rect.width - selectionRect.width;
                if (GUI.Button(selectionRect, size, HierarchyGUIEditor.labelStyle) == true) {

                    var p = GUIUtility.GUIToScreenPoint(selectionRect.min);
                    selectionRect.x = p.x;
                    selectionRect.y = p.y;
                    DependenciesPopup.Show(selectionRect, new Vector2(200f, 300f), HierarchyGUIEditor.resourceCollectorData, guid);

                }
            }

        }

    }

    public class DependenciesPopup : EditorWindow {

        public Vector2 scrollPosition;
        public Rect rect;
        public Vector2 size;
        private ResourceCollectorData data;
        private string guid;
        private System.Collections.Generic.List<Object> deps;

        private float height;
        private bool heightCalc;
        
        public static void Show(Rect buttonRect, Vector2 size, ResourceCollectorData data, string guid) {
            
            var popup = DependenciesPopup.CreateInstance<DependenciesPopup>();
            popup.data = data;
            popup.guid = guid;
            popup.size = size;
            popup.deps = data.GetDependencies(guid).OrderByDescending(x => {

                if (data.GetSize(x, out var size) == true) {
                    
                    return size;
                    
                }

                return -1L;

            }).ToList();
            popup.rect = buttonRect;
            popup.ShowAsDropDown(buttonRect, size);
            
        }

        private void OnGUI() {
            
            if (this.deps != null) {

                GUILayout.Label("References Count: " + this.deps.Count);
                if (this.heightCalc == false) {

                    this.height += EditorGUIUtility.singleLineHeight;
                    this.height += EditorGUIUtility.singleLineHeight;
                    this.height += EditorGUIUtility.singleLineHeight;

                }
                
                this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);
                foreach (var dep in this.deps) {

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(dep, typeof(Object), allowSceneObjects: true);
                    EditorGUI.EndDisabledGroup();
                    var rect = GUILayoutUtility.GetLastRect();
                    HierarchyGUIEditor.OnElementGUI(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dep)), rect);
                    if (this.heightCalc == false) {

                        this.height += EditorGUIUtility.singleLineHeight;

                    }
                    
                }
                GUILayout.EndScrollView();

                if (this.heightCalc == false) {

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

}