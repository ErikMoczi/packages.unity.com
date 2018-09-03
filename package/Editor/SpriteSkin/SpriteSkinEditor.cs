using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.Animation
{
    [CustomEditor(typeof(SpriteSkin))]
    [CanEditMultipleObjects]
    class SpriteSkinEditor : Editor
    {
        private static class Contents
        {
            public static readonly string listHeaderLabel = "Bones";
            public static readonly GUIContent spriteNotFound = new GUIContent("Sprite not found in SpriteRenderer");
            public static readonly GUIContent spriteHaseNoSkinningInformation = new GUIContent("Sprite has no Bind Poses");
            public static readonly GUIContent rootTransformNotFound = new GUIContent("Root Bone not set");
            public static readonly GUIContent rootTransformNotFoundInArray = new GUIContent("Bone list doesn't contain a reference to the Root Bone");
            public static readonly GUIContent InvalidTransformArray = new GUIContent("Bone list is invalid");
            public static readonly GUIContent transformArrayContainsNull = new GUIContent("Bone list contains unassigned references");
            public static readonly GUIContent InvalidTransformArrayLength = new GUIContent("The number of Sprite's Bind Poses and the number of Transforms should match");
        }

        private SerializedProperty m_RootBoneProperty;
        private SerializedProperty m_BoneTransformsProperty;
        private SpriteSkin m_SpriteSkin;
        private ReorderableList m_ReorderableList;
        private Sprite m_CurrentSprite;
        private bool m_NeedsRebind = false;

        void OnEnable()
        {
            m_SpriteSkin = (SpriteSkin)target;
            m_RootBoneProperty = serializedObject.FindProperty("m_RootBone");
            m_BoneTransformsProperty = serializedObject.FindProperty("m_BoneTransforms");
        }

        private void SetupReorderableList()
        {
            m_ReorderableList = new ReorderableList(serializedObject, m_BoneTransformsProperty, false, true, false, false);
            m_ReorderableList.drawHeaderCallback = (Rect rect) =>
                {
                    GUI.Label(rect, Contents.listHeaderLabel);
                };
            m_ReorderableList.elementHeightCallback = (int index) =>
                {
                    return EditorGUIUtility.singleLineHeight + 6;
                };
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isactive, bool isfocused) =>
                {
                    var content = GUIContent.none;

                    if (m_CurrentSprite != null)
                    {
                        var bones = m_CurrentSprite.GetBones();
                        if (index < bones.Length)
                            content = new GUIContent(bones[index].name);
                    }

                    rect.y += 2f;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    SerializedProperty element = m_BoneTransformsProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, content);
                };
        }

        private void InitializeBoneTransformArray()
        {
            if (m_CurrentSprite)
            {
                var elementCount = m_BoneTransformsProperty.arraySize;
                var bindPoses = m_CurrentSprite.GetBindPoses();

                if (elementCount != bindPoses.Length)
                {
                    m_BoneTransformsProperty.arraySize = bindPoses.Length;

                    for (int i = elementCount; i < m_BoneTransformsProperty.arraySize; ++i)
                        m_BoneTransformsProperty.GetArrayElementAtIndex(i).objectReferenceValue = null;

                    m_NeedsRebind = true;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var sprite = m_SpriteSkin.spriteRenderer.sprite;
            if (m_ReorderableList == null || m_CurrentSprite != sprite)
            {
                m_CurrentSprite = sprite;
                InitializeBoneTransformArray();
                SetupReorderableList();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_RootBoneProperty);
            if (EditorGUI.EndChangeCheck())
                m_NeedsRebind = true;

            EditorGUILayout.Space();

            if (!serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.BeginDisabledGroup(m_SpriteSkin.rootBone == null);
                m_ReorderableList.DoLayoutList();
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();

            if (m_NeedsRebind)
                Rebind();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(!EnableCreateBones());
            DoGenerateBonesButton();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!EnableSetBindPose());
            DoResetBindPoseButton();
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            DoValidationWarnings();
        }

        private void Rebind()
        {
            foreach (var t in targets)
            {
                var spriteSkin = t as SpriteSkin;

                if(spriteSkin.spriteRenderer.sprite == null || spriteSkin.rootBone == null)
                    continue;

                spriteSkin.Rebind();
            }

            m_NeedsRebind = false;
        }

        private bool EnableCreateBones()
        {
            foreach (var t in targets)
            {
                var spriteSkin = t as SpriteSkin;
                var sprite = spriteSkin.spriteRenderer.sprite;

                if (sprite != null && spriteSkin.rootBone == null)
                    return true;
            }
            return false;
        }

        private bool EnableSetBindPose()
        {
            foreach (var t in targets)
            {
                var spriteSkin = t as SpriteSkin;

                if (spriteSkin.isValid)
                    return true;
            }
            return false;
        }

        private void DoGenerateBonesButton()
        {
            if (GUILayout.Button("Create Bones", GUILayout.MaxWidth(125f)))
            {
                foreach (var t in targets)
                {
                    var spriteSkin = t as SpriteSkin;
                    var sprite = spriteSkin.spriteRenderer.sprite;

                    if (sprite == null || spriteSkin.rootBone != null)
                        continue;

                    Undo.RegisterCompleteObjectUndo(spriteSkin, "Create Bones");

                    spriteSkin.CreateBoneHierarchy();

                    foreach (var transform in spriteSkin.boneTransforms)
                        Undo.RegisterCreatedObjectUndo(transform.gameObject, "Create Bones");

                    EditorUtility.SetDirty(spriteSkin);
                }
            }
        }

        private void DoResetBindPoseButton()
        {
            if (GUILayout.Button("Reset Bind Pose", GUILayout.MaxWidth(125f)))
            {
                foreach (var t in targets)
                {
                    var spriteSkin = t as SpriteSkin;

                    if (!spriteSkin.isValid)
                        continue;

                    Undo.RecordObjects(spriteSkin.boneTransforms, "Reset Bind Pose");
                    spriteSkin.ResetBindPose();
                }
            }
        }

        private void DoValidationWarnings()
        {
            EditorGUILayout.Space();

            bool preAppendObjectName = targets.Length > 1;

            foreach (var t in targets)
            {
                var spriteSkin = t as SpriteSkin;

                var validationResult = spriteSkin.Validate();

                if (validationResult == SpriteSkinValidationResult.Ready)
                    continue;

                var content = GUIContent.none;

                switch (validationResult)
                {
                    case SpriteSkinValidationResult.SpriteNotFound:
                        content = Contents.spriteNotFound;
                        break;
                    case SpriteSkinValidationResult.SpriteHasNoSkinningInformation:
                        content = Contents.spriteHaseNoSkinningInformation;
                        break;
                    case SpriteSkinValidationResult.RootTransformNotFound:
                        content = Contents.rootTransformNotFound;
                        break;
                    case SpriteSkinValidationResult.RootNotFoundInTransformArray:
                        content = Contents.rootTransformNotFoundInArray;
                        break;
                    case SpriteSkinValidationResult.InvalidTransformArray:
                        content = Contents.InvalidTransformArray;
                        break;
                    case SpriteSkinValidationResult.InvalidTransformArrayLength:
                        content = Contents.InvalidTransformArrayLength;
                        break;
                    case SpriteSkinValidationResult.TransformArrayContainsNull:
                        content = Contents.transformArrayContainsNull;
                        break;
                }

                string text = content.text;

                if (preAppendObjectName)
                    text = spriteSkin.name + ": " + text;

                EditorGUILayout.HelpBox(text, MessageType.Warning);
            }
        }
    }
}
