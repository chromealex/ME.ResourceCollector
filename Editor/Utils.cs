using System.Linq;

namespace ME.ResourceCollector {

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

        public static long GetObjectSize(ResourceCollectorData data, object obj, System.Collections.Generic.HashSet<object> visited,
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

            } else if (type == typeof(UnityEngine.Texture) || type == typeof(UnityEngine.Texture2D)) {

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

}