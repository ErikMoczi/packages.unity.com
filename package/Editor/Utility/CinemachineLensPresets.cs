using UnityEngine;
using System;
using UnityEditor;

namespace Cinemachine.Editor
{
    /// <summary>
    /// User-definable named presets for lenses.  This is a Singleton asset, available in editor only
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [Serializable]
    public sealed class CinemachineLensPresets : ScriptableObject
    {
        private static CinemachineLensPresets sInstance = null;

        /// <summary>Get the singleton instance of this object</summary>
        public static CinemachineLensPresets Instance
        {
            get
            {
                if (sInstance == null)
                {
                    var guids = AssetDatabase.FindAssets("t:CinemachineLensPresets");
                    for (int i = 0; i < guids.Length && sInstance == null; ++i)
                        sInstance = AssetDatabase.LoadAssetAtPath<CinemachineLensPresets>(
                            AssetDatabase.GUIDToAssetPath(guids[i]));
                }
                if (sInstance == null)
                {
                    sInstance = CreateInstance<CinemachineLensPresets>();
                    AssetDatabase.CreateAsset(sInstance, "Assets/CinemachineLensPresets.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                return sInstance;
            }
        }
        
        /// <summary>Lens Preset</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public struct Preset
        {
            [Tooltip("Lens Name")]
            public string m_Name;

            /// <summary>
            /// This is the camera view in vertical degrees. For cinematic people, a 50mm lens
            /// on a super-35mm sensor would equal a 19.6 degree FOV
            /// </summary>
            [Range(1f, 179f)]
            [Tooltip("This is the camera view in vertical degrees. For cinematic people, a 50mm lens on a super-35mm sensor would equal a 19.6 degree FOV")]
            public float m_FieldOfView;
        }
        /// <summary>The array containing Preset definitions for nonphysical cameras</summary>
        [Tooltip("The array containing Preset definitions, for nonphysical cameras")]
        public Preset[] m_Presets = new Preset[0];

        /// <summary>Physical Lens Preset</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public struct PhysicalPreset
        {
            /// <summary>
            /// This is the camera focal length in mm
            /// </summary>
            [Tooltip("This is the camera focal length in mm")]
            public float m_FocalLength;
        }

        /// <summary>The array containing Preset definitions, for physical cameras</summary>
        [Tooltip("The array containing Preset definitions, for physical cameras")]
        public PhysicalPreset[] m_PhysicalPresets = new PhysicalPreset[0];

        /// <summary>Get the index of the preset that matches the lens settings</summary>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetMatchingPreset(float fov)
        {
            for (int i = 0; i < m_Presets.Length; ++i)
                if (Mathf.Approximately(m_Presets[i].m_FieldOfView, fov))
                    return i;
            return -1;
        }

        /// <summary>Get the index of the physical preset that matches the lens settings</summary>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetMatchingPhysicalPreset(float focalLength)
        {
            for (int i = 0; i < m_PhysicalPresets.Length; ++i)
                if (Mathf.Approximately(m_PhysicalPresets[i].m_FocalLength, focalLength))
                    return i;
            return -1;
        }
    }
}
