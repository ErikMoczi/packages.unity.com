using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.Animations.Rigging
{
    using Rendering;

    using Shape = BoneRenderer.Shape;
    using UnityObject = UnityEngine.Object;

    [InitializeOnLoad]
    public static class BoneRendererUtil
    {
        private class BatchRenderer
        {
            const int kMaxDrawMeshInstanceCount = 1023;

            public enum SubMeshType
            {
                BoneFaces,
                BoneWire,
                Count
            }

            public Mesh mesh;
            public Material material;

            private List<Matrix4x4> m_Matrices = new List<Matrix4x4>();
            private List<Vector4> m_Colors = new List<Vector4>();

            public void AddInstance(Matrix4x4 matrix, Color color)
            {
                m_Matrices.Add(matrix);
                m_Colors.Add(color);
            }

            public void Clear()
            {
                m_Matrices.Clear();
                m_Colors.Clear();
            }

            private static int RenderChunkCount(int totalCount)
            {
                return Mathf.CeilToInt((totalCount / (float)kMaxDrawMeshInstanceCount));
            }

            private static T[] GetRenderChunk<T>(List<T> array, int chunkIndex)
            {
                int rangeCount = (chunkIndex < (RenderChunkCount(array.Count) - 1)) ?
                    kMaxDrawMeshInstanceCount : array.Count - (chunkIndex * kMaxDrawMeshInstanceCount);

                return array.GetRange(chunkIndex * kMaxDrawMeshInstanceCount, rangeCount).ToArray();
            }

            public void Render()
            {
                if (m_Matrices.Count == 0 || m_Colors.Count == 0)
                    return;

                int count = System.Math.Min(m_Matrices.Count, m_Colors.Count);

                Material mat = material;
                mat.SetPass(0);

                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                CommandBuffer cb = new CommandBuffer();

                Matrix4x4[] matrices = null;

                int chunkCount = RenderChunkCount(count);
                for (int i = 0; i < chunkCount; ++i)
                {
                    cb.Clear();
                    matrices = GetRenderChunk(m_Matrices, i);
                    propertyBlock.SetVectorArray("_Color", GetRenderChunk(m_Colors, i));

                    material.DisableKeyword("WIRE_ON");
                    cb.DrawMeshInstanced(mesh, (int)SubMeshType.BoneFaces, material, 0, matrices, matrices.Length, propertyBlock);
                    Graphics.ExecuteCommandBuffer(cb);

                    cb.Clear();
                    material.EnableKeyword("WIRE_ON");
                    cb.DrawMeshInstanced(mesh, (int)SubMeshType.BoneWire, material, 0, matrices, matrices.Length, propertyBlock);
                    Graphics.ExecuteCommandBuffer(cb);
                }
            }
        }

        static List<BoneRenderer> s_BoneRendererComponents = new List<BoneRenderer>();

        private static BatchRenderer s_PyramidMeshRenderer;
        private static BatchRenderer s_BoxMeshRenderer;

        private static Material s_Material;

        private const float k_Epsilon = 1e-5f;

        private const float k_BoneBaseSize = 2f;
        private const float k_BoneTipSize = 0.5f;

        private static int s_ButtonHash = "BoneHandle".GetHashCode();

        static BoneRendererUtil()
        {
            SceneView.onSceneGUIDelegate += DrawSkeletons;
        }

        private static Material material
        {
            get
            {
                if (!s_Material)
                {
                    Shader shader = (Shader)EditorGUIUtility.LoadRequired("BoneHandles.shader");
                    s_Material = new Material(shader);
                    s_Material.hideFlags = HideFlags.DontSaveInEditor;
                    s_Material.enableInstancing = true;
                }

                return s_Material;
            }
        }

        private static BatchRenderer pyramidMeshRenderer
        {
            get
            {
                if (s_PyramidMeshRenderer == null)
                {
                    var mesh = new Mesh();
                    mesh.name = "BoneRendererPyramidMesh";
                    mesh.subMeshCount = (int)BatchRenderer.SubMeshType.Count;
                    mesh.hideFlags = HideFlags.DontSave;

                    // Bone vertices
                    Vector3[] vertices = new Vector3[]
                    {
                        new Vector3(0.0f, 1.0f, 0.0f),
                        new Vector3(0.0f, 0.0f, -1.0f),
                        new Vector3(-0.9f, 0.0f, 0.5f),
                        new Vector3(0.9f, 0.0f, 0.5f),
                    };

                    mesh.vertices = vertices;

                    // Build indices for different sub meshes
                    int[] boneFaceIndices = new int[]
                    {
                        0, 2, 1,
                        0, 1, 3,
                        0, 3, 2,
                        1, 2, 3
                    };
                    mesh.SetIndices(boneFaceIndices, MeshTopology.Triangles, (int)BatchRenderer.SubMeshType.BoneFaces);

                    int[] boneWireIndices = new int[]
                    {
                        0, 1, 0, 2, 0, 3, 1, 2, 2, 3, 3, 1
                    };
                    mesh.SetIndices(boneWireIndices, MeshTopology.Lines, (int)BatchRenderer.SubMeshType.BoneWire);

                    s_PyramidMeshRenderer = new BatchRenderer()
                    {
                        mesh = mesh,
                        material = material
                    };
                }

                return s_PyramidMeshRenderer;
            }
        }

        private static BatchRenderer boxMeshRenderer
        {
            get
            {
                if (s_BoxMeshRenderer == null)
                {
                    var mesh = new Mesh();
                    mesh.name = "BoneRendererBoxMesh";
                    mesh.subMeshCount = (int)BatchRenderer.SubMeshType.Count;
                    mesh.hideFlags = HideFlags.DontSave;

                    // Bone vertices
                    Vector3[] vertices = new Vector3[]
                    {
                        new Vector3(-0.5f, 0.0f, 0.5f),
                        new Vector3(0.5f, 0.0f, 0.5f),
                        new Vector3(0.5f, 0.0f, -0.5f),
                        new Vector3(-0.5f, 0.0f, -0.5f),
                        new Vector3(-0.5f, 1.0f, 0.5f),
                        new Vector3(0.5f, 1.0f, 0.5f),
                        new Vector3(0.5f, 1.0f, -0.5f),
                        new Vector3(-0.5f, 1.0f, -0.5f)
                    };

                    mesh.vertices = vertices;

                    // Build indices for different sub meshes
                    int[] boneFaceIndices = new int[]
                    {
                        0, 2, 1,
                        0, 3, 2,

                        0, 1, 5,
                        0, 5, 4,

                        1, 2, 6,
                        1, 6, 5,

                        2, 3, 7,
                        2, 7, 6,

                        3, 0, 4,
                        3, 4, 7,

                        4, 5, 6,
                        4, 6, 7
                    };
                    mesh.SetIndices(boneFaceIndices, MeshTopology.Triangles, (int)BatchRenderer.SubMeshType.BoneFaces);

                    int[] boneWireIndices = new int[]
                    {
                        0, 1, 1, 2, 2, 3, 3, 0,
                        4, 5, 5, 6, 6, 7, 7, 4,
                        0, 4, 1, 5, 2, 6, 3, 7
                    };
                    mesh.SetIndices(boneWireIndices, MeshTopology.Lines, (int)BatchRenderer.SubMeshType.BoneWire);

                    s_BoxMeshRenderer = new BatchRenderer()
                    {
                        mesh = mesh,
                        material = material
                    };

                }

                return s_BoxMeshRenderer;
            }
        }

        private static Matrix4x4 ComputeBoneMatrix(Vector3 start, Vector3 end, float length, float size)
        {
            Vector3 direction = (end - start) / length;
            Vector3 tangent = Vector3.Cross(direction, Vector3.up);
            if (Vector3.SqrMagnitude(tangent) < 0.1f)
                tangent = Vector3.Cross(direction, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(direction, tangent);

            float scale = length * k_BoneBaseSize * size;

            return new Matrix4x4(
                new Vector4(tangent.x   * scale,  tangent.y   * scale,  tangent.z   * scale , 0f),
                new Vector4(direction.x * length, direction.y * length, direction.z * length, 0f),
                new Vector4(bitangent.x * scale,  bitangent.y * scale,  bitangent.z * scale , 0f),
                new Vector4(start.x, start.y, start.z, 1f));
        }

        static void DrawSkeletons(SceneView sceneview)
        {
            var gizmoColor = Gizmos.color;

            pyramidMeshRenderer.Clear();
            boxMeshRenderer.Clear();

            for (var i = 0; i < s_BoneRendererComponents.Count; i++)
            {
                var boneRenderer = s_BoneRendererComponents[i];

                if (boneRenderer.bones == null)
                    continue;

                var size = boneRenderer.boneSize * 0.025f;

                if (boneRenderer.drawSkeleton)
                {
                    var shape = boneRenderer.shape;
                    var color = boneRenderer.skeletonColor;
                    var nubColor = new Color(color.r, color.g, color.b, color.a);
                    var selectionColor = Color.white;

                    for (var j = 0; j < boneRenderer.bones.Length; j++)
                    {
                        var bone = boneRenderer.bones[j];
                        DoBoneRender(bone.first, bone.second, shape, color, size);
                    }

                    for (var k = 0; k < boneRenderer.tips.Length; k++)
                    {
                        var tip = boneRenderer.tips[k];
                        DoBoneRender(tip, null, shape, color, size);
                    }
                }

                if (boneRenderer.drawTripods)
                {
                    for (var j = 0; j < boneRenderer.transforms.Length; j++)
                    {
                        var tripodSize = 1f;
                        var transform = boneRenderer.transforms[j];
                        if (transform == null)
                            continue;

                        var position = transform.position;
                        var xAxis = position + transform.rotation * Vector3.right * size * tripodSize;
                        var yAxis = position + transform.rotation * Vector3.up * size * tripodSize;
                        var zAxis = position + transform.rotation * Vector3.forward * size * tripodSize;

                        Handles.color = Color.red;
                        Handles.DrawLine(position, xAxis);
                        Handles.color = Color.green;
                        Handles.DrawLine(position, yAxis);
                        Handles.color = Color.blue;
                        Handles.DrawLine(position, zAxis);
                    }
                }
            }

            pyramidMeshRenderer.Render();
            boxMeshRenderer.Render();

            Gizmos.color = gizmoColor;
         }


        private static void DoBoneRender(Transform transform, Transform childTransform, Shape shape, Color color, float size)
        {
            Vector3 start = transform.position;
            Vector3 end = childTransform != null ? childTransform.position : start;

            GameObject boneGO = transform.gameObject;

            float length = (end - start).magnitude;
            bool tipBone = (length < k_Epsilon);

            int id = GUIUtility.GetControlID(s_ButtonHash, FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    {
                        HandleUtility.AddControl(id, tipBone ? HandleUtility.DistanceToCircle(start, k_BoneTipSize * size * 0.5f) : HandleUtility.DistanceToLine(start, end));
                        break;
                    }
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
                case EventType.MouseDown:
                    {
                        if (HandleUtility.nearestControl == id && evt.button == 0)
                        {
                            GUIUtility.hotControl = id; // Grab mouse focus
                            if (evt.shift || EditorGUI.actionKey)
                            {
                                UnityObject[] existingSelection = Selection.objects;

                                // For shift, we check if EXACTLY the active GO is hovered by mouse and then subtract. Otherwise additive.
                                // For control/cmd, we check if ANY of the selected GO is hovered by mouse and then subtract. Otherwise additive.
                                // Control/cmd takes priority over shift.
                                if (EditorGUI.actionKey ? Selection.Contains(boneGO) : Selection.activeGameObject == boneGO)
                                {
                                    // subtract from selection
                                    var newSelection = new UnityObject[existingSelection.Length - 1];

                                    int index = Array.IndexOf(existingSelection, boneGO);

                                    System.Array.Copy(existingSelection, newSelection, index);
                                    System.Array.Copy(existingSelection, index + 1, newSelection, index, newSelection.Length - index);

                                    Selection.objects = newSelection;
                                }
                                else
                                {
                                    // add to selection
                                    var newSelection = new UnityObject[existingSelection.Length + 1];
                                    System.Array.Copy(existingSelection, newSelection, existingSelection.Length);
                                    newSelection[existingSelection.Length] = boneGO;

                                    Selection.objects = newSelection;
                                }
                            }
                            else
                                Selection.activeObject = boneGO;

                            evt.Use();
                        }
                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (!evt.alt && GUIUtility.hotControl == id)
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.objectReferences = new UnityEngine.Object[] {transform};
                            DragAndDrop.StartDrag(ObjectNames.GetDragAndDropTitle(transform));

                            // having a hot control set during drag makes the control eat the drag events
                            // and dragging of bones no longer works over the avatar configure window
                            // see case 912016
                            GUIUtility.hotControl = 0;

                            evt.Use();
                        }
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                        {
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    }
                case EventType.Repaint:
                    {
                        if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id)
                        {
                            color = Handles.preselectionColor;
                        }
                        else if (Selection.Contains(boneGO) || Selection.activeObject == boneGO)
                        {
                            color = Handles.selectedColor;
                        }

                        if (tipBone)
                        {
                            Handles.color = color;
                            Handles.SphereHandleCap(0, start, Quaternion.identity, k_BoneTipSize * size, EventType.Repaint);
                        }
                        else if (shape == Shape.Line)
                        {
                            Handles.color = color;
                            Handles.DrawLine(start, end);
                        }
                        else
                        {
                            if (shape == Shape.Pyramid)
                                pyramidMeshRenderer.AddInstance(ComputeBoneMatrix(start, end, length, size), color);
                            else // if (shape == Shape.Box)
                                boxMeshRenderer.AddInstance(ComputeBoneMatrix(start, end, length, size), color);
                        }

                    }
                    break;
            }
        }

        public static void OnBoneRendererEnabled(BoneRenderer obj)
        {
            s_BoneRendererComponents.Add(obj);
        }

        public static void OnBoneRendererDisabled(BoneRenderer obj)
        {
            s_BoneRendererComponents.Remove(obj);
        }
    }
    #endif // UNITY_EDITOR
}
