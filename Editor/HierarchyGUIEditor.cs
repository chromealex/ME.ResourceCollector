using System.Linq;

namespace ME.ResourceCollector {

    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class HierarchyGUIEditor {

        static HierarchyGUIEditor() {

            HierarchyGUIEditor.Reassign();

        }

        [InitializeOnLoadMethod]
        public static void Reassign() {
            
            EditorApplication.projectWindowItemOnGUI -= HierarchyGUIEditor.OnElementGUI;
            EditorApplication.projectWindowItemOnGUI += HierarchyGUIEditor.OnElementGUI;

        }

        private static GUIStyle buttonStyle;
        private static ResourceCollectorData resourceCollectorData;
        public static void OnElementGUI(string guid, UnityEngine.Rect rect) {

            if (HierarchyGUIEditor.buttonStyle == null) {
                HierarchyGUIEditor.buttonStyle = new GUIStyle(EditorStyles.miniPullDown);
                HierarchyGUIEditor.buttonStyle.fontSize = 10;
                HierarchyGUIEditor.buttonStyle.alignment = TextAnchor.MiddleRight;
            }
            
            if (HierarchyGUIEditor.resourceCollectorData == null) HierarchyGUIEditor.resourceCollectorData = ResourceCollector.GetData();
            if (HierarchyGUIEditor.resourceCollectorData == null) return;

            var size = HierarchyGUIEditor.resourceCollectorData.GetSizeStr(guid);
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

}