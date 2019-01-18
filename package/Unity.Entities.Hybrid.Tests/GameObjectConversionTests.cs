using NUnit.Framework;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityEngine.Entities.Tests
{
    class GameObjectConversionTests : ECSTestsFixture
    {
        [Test]
        public void ConvertCubePrimitive([Values]bool useDiffing)
        {
            // Prepare scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            SceneManager.SetActiveScene(scene);
            
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localPosition = new Vector3(1, 2, 3);
            var renderer = go.GetComponent<MeshRenderer>();
            
            // Convert
            if (useDiffing)
            {
                var shadowWorld = new World("Shadow");
                GameObjectConversionUtility.ConvertSceneAndApplyDiff(scene, shadowWorld, m_Manager.World);
                shadowWorld.Dispose();
            }
            else
            {
                GameObjectConversionUtility.ConvertScene(scene, m_Manager.World);
            }
            
            // Check
            var entities = m_Manager.GetAllEntities();
            Assert.AreEqual(1, entities.Length);
            var entity = entities[0];

            Assert.AreEqual(useDiffing ? 4 : 3, m_Manager.GetComponentCount(entity));
            Assert.IsTrue(m_Manager.HasComponent<Position>(entity));
            Assert.IsTrue(m_Manager.HasComponent<Rotation>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RenderMesh>(entity));
            if (useDiffing)
                Assert.IsTrue(m_Manager.HasComponent<EntityGuid>(entity));

            Assert.AreEqual(new float3(1, 2, 3), m_Manager.GetComponentData<Position>(entity).Value);
            Assert.AreEqual(quaternion.identity, m_Manager.GetComponentData<Rotation>(entity).Value);
            Assert.AreEqual(renderer.sharedMaterial, m_Manager.GetSharedComponentData<RenderMesh>(entity).material);
            
            // Unload
            EditorSceneManager.UnloadSceneAsync(scene);
        }
        
        //@TODO: Test Prefabs
        //@TODO: Test GameObject -> Entity ID mapping

    }
}
