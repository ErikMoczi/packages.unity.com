﻿using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SpatialTracking;

namespace UnityEditor.XR.ARFoundation
{
    internal static class SceneUtils
    {
        static readonly string k_DebugPlaneMaterial = "Packages/com.unity.xr.arfoundation/Materials/DebugPlane.mat";

        static readonly string k_ParticleMaterial = "Default-Particle.mat";

        static readonly string k_LineMaterial = "Default-Line.mat";

        static readonly Color k_ParticleColor = new Color(253f / 255f, 184f / 255f, 19f / 255f);

        static readonly float k_ParticleSize = 0.02f;

        [MenuItem("GameObject/XR/AR Session Origin", false, 10)]
        static void CreateARSessionOrigin()
        {
            var originGo = ObjectFactory.CreateGameObject("AR Session Origin", typeof(ARSessionOrigin));
            var cameraGo = ObjectFactory.CreateGameObject("AR Camera",
                typeof(Camera), typeof(TrackedPoseDriver), typeof(ARBackgroundRenderer));

            Undo.SetTransformParent(cameraGo.transform, originGo.transform, "Parent camera to session origin");

            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = Color.black;

            var origin = originGo.GetComponent<ARSessionOrigin>();
            origin.camera = camera;

            var tpd = cameraGo.GetComponent<TrackedPoseDriver>();
            tpd.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.ColorCamera);
        }

        [MenuItem("GameObject/XR/AR Session", false, 10)]
        static void CreateARSession()
        {
            ObjectFactory.CreateGameObject("AR Session", typeof(ARSession));
        }

        [MenuItem("GameObject/XR/AR Point Cloud Debug Visualizer", false, 10)]
        static void CreateARPointCloudVisualizer()
        {
            var go = ObjectFactory.CreateGameObject("AR Point Cloud Debug Visualizer", typeof(ARPointCloudParticleVisualizer));
            var particleSystem = go.GetComponent<ParticleSystem>();

            var main = particleSystem.main;
            main.loop = false;
            main.startSize = k_ParticleSize;
            main.startColor = k_ParticleColor;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.playOnAwake = false;

            var emission = particleSystem.emission;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = false;

            var renderer = particleSystem.GetComponent<Renderer>();
            renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>(k_ParticleMaterial);
        }

        [MenuItem("GameObject/XR/AR Plane Debug Visualizer", false, 10)]
        static void CreateARPlaneVisualizer()
        {
            var go = ObjectFactory.CreateGameObject("AR Plane Debug Visualizer",
                typeof(ARPlaneMeshVisualizer), typeof(MeshCollider), typeof(MeshFilter),
                typeof(MeshRenderer), typeof(LineRenderer));
            SetupMeshRenderer(go.GetComponent<MeshRenderer>());
            SetupLineRenderer(go.GetComponent<LineRenderer>());
        }

        static void SetupLineRenderer(LineRenderer lineRenderer)
        {
            var materials = new Material[1];
            materials[0] = AssetDatabase.GetBuiltinExtraResource<Material>(k_LineMaterial);
            lineRenderer.materials = materials;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = 0.005f;
            lineRenderer.startColor = Color.black;
            lineRenderer.endColor = Color.black;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.numCapVertices = 4;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.useWorldSpace = false;
        }

        static void SetupMeshRenderer(MeshRenderer meshRenderer)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_DebugPlaneMaterial);
            meshRenderer.materials = new Material[] { material };
        }
    }
}
