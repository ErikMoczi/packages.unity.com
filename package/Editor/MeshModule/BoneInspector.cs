using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;
using UnityEditor.U2D.Interface;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class BoneInspector
    {
        public SpriteMeshData spriteMeshData { get; set; }
        public ScriptableObject undoableObject { get; private set; }
        public ISelection selection { get; set; }

        protected ISpriteEditor spriteEditor
        {
            get; private set;
        }

        public BoneInspector(ScriptableObject undoObject)
        {
            undoableObject = undoObject;
        }

        public float CalculateHeight(Rect viewRect)
        {
            return MeshModuleUtility.kEditorLineHeight * 2f + 4f;
        }

        public void OnInspectorGUI()
        {
            Debug.Assert(spriteMeshData != null);
            Debug.Assert(selection != null);
            Debug.Assert(selection.Count == 1);

            SpriteBoneData bone = spriteMeshData.bones[selection.single];

            EditorGUI.BeginChangeCheck();

            float depth = (float)EditorGUILayout.IntField("Bone Depth", Mathf.RoundToInt(bone.depth));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(undoableObject, "Edit Depth");

                bone.depth = depth;
            }
        }
    }
}
