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

            var spriteRenderer = spriteSkin.gameObject.GetComponent<SpriteRenderer>();

            if (GUILayout.Button("Generate Bones GOs"))
            {
                if (spriteRenderer == null || spriteRenderer.sprite == null)
                {
                    Debug.LogError("Unable to Generate Bones GOs. Check spriteRenderer or spriteRenderer.sprite for null");
                }
                else
                {
                    GameObject go = SpriteBoneUtility.CreateSkeleton(spriteRenderer.sprite.GetBones(), spriteSkin.gameObject, spriteSkin.rootBone);
                    if (go)
                        spriteSkin.rootBone = go.transform;
                    EditorUtility.SetDirty(spriteSkin);
                }
            }

            if (GUILayout.Button("Reset to Bind-pose"))
            {
                if (spriteRenderer == null || spriteRenderer.sprite == null)
                {
                    Debug.LogError("Unable to Reset to Bind-pose. Check spriteRenderer or spriteRenderer.sprite for null");
                }
                else
                {
                    SpriteBoneUtility.ResetBindPose(spriteRenderer.sprite.GetBones(), spriteSkin.boneTransforms);
                    EditorUtility.SetDirty(spriteSkin);
                }
            }
        }
    }
}
