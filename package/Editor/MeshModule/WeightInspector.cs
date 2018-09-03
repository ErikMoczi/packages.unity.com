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
    internal class WeightInspector : IGUITool
    {
        public SpriteMeshData spriteMeshData
        {
            get { return m_SpriteMeshData; }
            set
            {
                if(m_SpriteMeshData != value)
                {
                    m_SpriteMeshData = value;
                    m_CachedBoneNames = null;
                    
                    if(m_SpriteMeshData != null)
                        m_CachedBoneNames = MeshModuleUtility.GetBoneNameList(m_SpriteMeshData);
                }
            }
        }

        public GUIContent[] boneNames { get { return m_CachedBoneNames; } }
        public ScriptableObject undoableObject { get; private set; }
        public ISelection selection { get; set; }
        public int controlID { get { return 0; } }

        protected ISpriteEditor spriteEditor
        {
            get; private set;
        }

        public WeightInspector(ScriptableObject undoObject)
        {
            undoableObject = undoObject;
        }

        public float CalculateHeight(Rect viewRect)
        {
            return MeshModuleUtility.kEditorLineHeight * 5f + 4f;
        }

        public void OnInspectorGUI()
        {
            //Temporal solution for slider registering too many undos
            if (GUIUtility.hotControl == 0 && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Undo.RegisterCompleteObjectUndo(undoableObject, "Edit Weights");
            }

            ChannelsGUI();
        }

        private void ChannelsGUI()
        {
            for (int channelIndex = 0; channelIndex < 4; ++channelIndex)
            {
                bool channelEnabled;
                bool isChannelEnabledMixed;
                bool isBoneIndexMixed;
                bool isWeightMixed;
                BoneWeightData boneWeightData;
                spriteMeshData.GetMultiEditChannelData(selection, channelIndex, out channelEnabled, out boneWeightData, out isChannelEnabledMixed, out isBoneIndexMixed, out isWeightMixed);

                BoneWeightData newBoneWeightData = new BoneWeightData();
                newBoneWeightData = boneWeightData;
                bool newChannelEnabled = channelEnabled;

                EditorGUI.BeginChangeCheck();

                WeightChannelDrawer(ref newChannelEnabled, ref newBoneWeightData, isChannelEnabledMixed, isBoneIndexMixed, isWeightMixed);

                if (EditorGUI.EndChangeCheck())
                    spriteMeshData.SetMultiEditChannelData(selection, channelIndex, channelEnabled, newChannelEnabled, boneWeightData, newBoneWeightData);
            }
        }

        public void OnGUI()
        {
        }

        private void WeightChannelDrawer(
            ref bool isChannelEnabled, ref BoneWeightData boneWeightData,
            bool isChannelEnabledMixed = false, bool isBoneIndexMixed = false, bool isWeightMixed = false)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUIUtility.fieldWidth = 1f;
            EditorGUIUtility.labelWidth = 1f;

            EditorGUI.showMixedValue = isChannelEnabledMixed;
            isChannelEnabled = EditorGUILayout.Toggle(GUIContent.none, isChannelEnabled);

            EditorGUIUtility.fieldWidth = 30f;
            EditorGUIUtility.labelWidth = 30f;

            using (new EditorGUI.DisabledScope(!isChannelEnabled && !isChannelEnabledMixed))
            {
                int tempBoneIndex = GUI.enabled ? boneWeightData.boneIndex : -1;

                EditorGUI.BeginChangeCheck();

                EditorGUI.showMixedValue = GUI.enabled && isBoneIndexMixed;
                tempBoneIndex = EditorGUILayout.Popup(tempBoneIndex, MeshModuleUtility.GetBoneNameList(spriteMeshData));

                if (EditorGUI.EndChangeCheck())
                    boneWeightData.boneIndex = tempBoneIndex;

                EditorGUIUtility.fieldWidth = 45f;

                EditorGUI.showMixedValue = isWeightMixed;
                boneWeightData.weight = EditorGUILayout.Slider(GUIContent.none, boneWeightData.weight, 0f, 1f);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.showMixedValue = false;
            EditorGUIUtility.labelWidth = -1;
            EditorGUIUtility.fieldWidth = -1;
        }

        private SpriteMeshData m_SpriteMeshData;
        private GUIContent[] m_CachedBoneNames;
    }
}
