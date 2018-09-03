using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.Animation
{
    [CustomEditor(typeof(SpriteSkin))]
    class SpriteSkinEditor : Editor
    {
        SerializedProperty rootBoneProperty;
        SerializedProperty mergeRootBoneProperty;

        void OnEnable()
        {
            rootBoneProperty = serializedObject.FindProperty("m_RootBone");
        }

        public override void OnInspectorGUI()
        {
            SpriteSkin spriteSkin = (SpriteSkin)target;
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(rootBoneProperty);
            if (EditorGUI.EndChangeCheck())
            {
                spriteSkin.rootBone = (Transform)(rootBoneProperty.objectReferenceValue);
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("Generate Bones GOs"))
            {
                GameObject go = SpriteBoneUtility.CreateSkeleton(spriteSkin.spriteRenderer.sprite.GetBones(), spriteSkin.gameObject, spriteSkin.rootBone);
                if (go)
                    spriteSkin.rootBone = go.transform;
                EditorUtility.SetDirty(spriteSkin);
            }
        }
    }
}