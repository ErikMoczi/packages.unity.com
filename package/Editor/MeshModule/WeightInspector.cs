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
        const float kSingleLineHeight = 16;

        public SpriteMeshData spriteMeshData { get; set; }
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

        public float CalculateInspectorHeight(Rect viewRect)
        {
            return 5f * (WeightInspector.kSingleLineHeight + 3f) + 4f;
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
            bool channelEnabled;
            int boneIndex;
            float weight;
            bool isChannelEnabledMixed;
            bool isBoneIndexMixed;
            bool isWeightMixed;

            for (int channelIndex = 0; channelIndex < 4; ++channelIndex)
            {
                GetChannelDataInSelection(channelIndex, out channelEnabled, out boneIndex, out weight, out isChannelEnabledMixed, out isBoneIndexMixed, out isWeightMixed);

                bool newChannelEnabled = channelEnabled;
                int newBoneIndex = boneIndex;
                float newWeight = weight;

                EditorGUI.BeginChangeCheck();

                WeightChannelDrawer(ref newChannelEnabled, ref newBoneIndex, ref newWeight, isChannelEnabledMixed, isBoneIndexMixed, isWeightMixed);

                if (EditorGUI.EndChangeCheck())
                {
                    BoneWeightData referenceData = new BoneWeightData(boneIndex, weight);
                    BoneWeightData newData = new BoneWeightData(newBoneIndex, newWeight);

                    foreach (int i in selection)
                    {
                        EditableBoneWeight editableBoneWeight = spriteMeshData.vertices[i].editableBoneWeight;
                        SetChannelData(editableBoneWeight, channelIndex, channelEnabled, newChannelEnabled, referenceData, newData);
                    }
                }
            }
        }

        public void OnGUI()
        {
        }

        private void WeightChannelDrawer(
            ref bool isChannelEnabled, ref int boneIndex, ref float weight,
            bool isChannelEnabledMixed = false, bool isBoneIndexMixed = false, bool isWeightMixed = false)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUIUtility.fieldWidth = 1f;
            EditorGUIUtility.labelWidth = 1f;

            EditorGUI.showMixedValue = isChannelEnabledMixed;
            isChannelEnabled = EditorGUILayout.Toggle(GUIContent.none, isChannelEnabled);

            EditorGUIUtility.fieldWidth = 30f;
            EditorGUIUtility.labelWidth = 30f;
            EditorGUI.showMixedValue = isBoneIndexMixed;
            boneIndex = EditorGUILayout.Popup(boneIndex, GetBoneNameList());

            using (new EditorGUI.DisabledScope(!isChannelEnabled && !isChannelEnabledMixed))
            {
                EditorGUIUtility.fieldWidth = 45f;

                EditorGUI.showMixedValue = isWeightMixed;
                weight = EditorGUILayout.Slider(GUIContent.none, weight, 0f, 1f);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.showMixedValue = false;
            EditorGUIUtility.labelWidth = -1;
            EditorGUIUtility.fieldWidth = -1;
        }

        private void GetChannelDataInSelection(int channelIndex,
            out bool channelEnabled, out int boneIndex, out float weight,
            out bool isChannelEnabledMixed, out bool isBoneIndexMixed, out bool isWeightMixed)
        {
            bool first = true;
            channelEnabled = false;
            boneIndex = 0;
            weight = 0f;
            isChannelEnabledMixed = false;
            isBoneIndexMixed = false;
            isWeightMixed = false;

            foreach (int i in selection)
            {
                EditableBoneWeight editableBoneWeight = spriteMeshData.vertices[i].editableBoneWeight;

                BoneWeightData data = editableBoneWeight.GetBoneWeightData(channelIndex);

                if (first)
                {
                    channelEnabled = editableBoneWeight.IsChannelEnabled(channelIndex);
                    boneIndex = data.boneIndex;
                    weight = data.weight;

                    first = false;
                }
                else
                {
                    if (channelEnabled != editableBoneWeight.IsChannelEnabled(channelIndex))
                    {
                        isChannelEnabledMixed = true;
                        channelEnabled = false;
                    }

                    if (boneIndex != data.boneIndex)
                        isBoneIndexMixed = true;

                    if (weight != data.weight)
                        isWeightMixed = true;
                }
            }
        }

        private void SetChannelData(EditableBoneWeight editableBoneWeight, int channelIndex,  bool referenceChannelEnabled, bool newChannelEnabled,  BoneWeightData referenceData, BoneWeightData newData)
        {
            bool channelEnabledChanged = referenceChannelEnabled != newChannelEnabled;
            bool boneIndexChanged = referenceData.boneIndex != newData.boneIndex;
            bool weightChanged = referenceData.weight != newData.weight;

            BoneWeightData data = editableBoneWeight.GetBoneWeightData(channelIndex);

            if (channelEnabledChanged)
                editableBoneWeight.EnableChannel(channelIndex, newChannelEnabled);

            if (boneIndexChanged)
                data.boneIndex = newData.boneIndex;

            if (weightChanged)
                data.weight = newData.weight;

            editableBoneWeight.SetBoneWeightData(channelIndex, data);

            if (channelEnabledChanged || weightChanged)
                editableBoneWeight.CompensateOtherChannels(channelIndex);
        }

        private string[] GetBoneNameList()
        {
            List<string> names = new List<string>();

            for (int i = 0; i < spriteMeshData.bones.Count; i++)
            {
                var bone = spriteMeshData.bones[i];
                names.Add(i + " " + bone.name);
            }

            return names.ToArray();
        }
    }
}
