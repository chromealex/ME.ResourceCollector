namespace ME.ResourceCollector {

    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(Object), true)]
    public class ObjectFieldDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            EditorGUI.PropertyField(position, property, label);
            //base.OnGUI(position, property, label);

            var obj = property.objectReferenceValue;
            if (obj != null) {

                var path = AssetDatabase.GetAssetPath(obj);
                var guid = AssetDatabase.AssetPathToGUID(path);
                position.width -= 20f;
                HierarchyGUIEditor.OnElementGUI(guid, position);

            }

        }

    }

}