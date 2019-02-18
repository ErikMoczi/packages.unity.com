﻿#if !UNITY_2019_1_OR_NEWER
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.Rendering
{
    [ExecuteAlways]
    public class RenderingSystemBootstrap : ComponentSystem
    {
        protected override void OnCreateManager()
        {
            RenderPipeline.beginCameraRendering += OnBeforeCull;
            Camera.onPreCull += OnBeforeCull;
        }

        protected override void OnDestroyManager()
        {
            RenderPipeline.beginCameraRendering -= OnBeforeCull;
            Camera.onPreCull -= OnBeforeCull;
        }
        
        protected override void OnUpdate()
        {
        }

        [Inject]
#pragma warning disable 649
        RenderMeshSystem m_MeshRendererSystem;

        [Inject] 
        LODGroupSystemV1 m_LODSystem;

#pragma warning restore 649      
        public void OnBeforeCull(Camera camera)
        {
#if UNITY_EDITOR && UNITY_2018_3_OR_NEWER
            var prefabEditMode = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle() !=
                                 UnityEditor.SceneManagement.StageUtility.GetMainStageHandle();
            var gameCamera = (camera.hideFlags & HideFlags.DontSave) == 0;
            if (prefabEditMode && !gameCamera)
                return;
#endif
            
            m_LODSystem.ActiveCamera = camera;
            m_LODSystem.Update();
            m_LODSystem.ActiveCamera = null;

            
            m_MeshRendererSystem.ActiveCamera = camera;
            m_MeshRendererSystem.Update();
            m_MeshRendererSystem.ActiveCamera = null;
            
        }
    }
}
#endif
