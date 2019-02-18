using System;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Entities.Tests
{
	class DisableComponentTests : ECSTestsFixture
	{
		[Test]
		public void DIS_DontFindDisabledInComponentGroup()
		{
		    var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

		    var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));

			var entity0 = m_Manager.CreateEntity(archetype0);
			var entity1 = m_Manager.CreateEntity(archetype1);

			var arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(1, arr.Length);
            group.Dispose();

			m_Manager.DestroyEntity(entity0);
			m_Manager.DestroyEntity(entity1);
		}

	    [Test]
	    public void DIS_DontFindDisabledInChunkIterator()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

	        var entity0 = m_Manager.CreateEntity(archetype0);
	        var entity1 = m_Manager.CreateEntity(archetype1);

            var group = m_Manager.CreateComponentGroup(ComponentType.Create<EcsTestData>());
	        var chunks = group.CreateArchetypeChunkArray(Allocator.TempJob);
            group.Dispose();
	        var count = ArchetypeChunkArray.CalculateEntityCount(chunks);
	        chunks.Dispose();

	        Assert.AreEqual(1, count);

	        m_Manager.DestroyEntity(entity0);
	        m_Manager.DestroyEntity(entity1);
	    }

		[Test]
		public void DIS_FindDisabledIfRequestedInComponentGroup()
		{
		    var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

		    var group = m_Manager.CreateComponentGroup(ComponentType.Create<EcsTestData>(), ComponentType.Create<Disabled>());

			var entity0 = m_Manager.CreateEntity(archetype0);
			var entity1 = m_Manager.CreateEntity(archetype1);
			var entity2 = m_Manager.CreateEntity(archetype1);

			var arr = group.GetComponentDataArray<EcsTestData>();
            group.Dispose();
			Assert.AreEqual(2, arr.Length);

			m_Manager.DestroyEntity(entity0);
			m_Manager.DestroyEntity(entity1);
			m_Manager.DestroyEntity(entity2);
		}

	    [Test]
	    public void DIS_FindDisabledIfRequestedInChunkIterator()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

	        var entity0 = m_Manager.CreateEntity(archetype0);
	        var entity1 = m_Manager.CreateEntity(archetype1);
	        var entity2 = m_Manager.CreateEntity(archetype1);

            var group = m_Manager.CreateComponentGroup(ComponentType.Create<EcsTestData>(), ComponentType.Create<Disabled>());
	        var chunks = group.CreateArchetypeChunkArray(Allocator.TempJob);
            group.Dispose();
	        var count = ArchetypeChunkArray.CalculateEntityCount(chunks);
	        chunks.Dispose();

	        Assert.AreEqual(2, count);

	        m_Manager.DestroyEntity(entity0);
	        m_Manager.DestroyEntity(entity1);
	        m_Manager.DestroyEntity(entity2);
	    }

	    [Test]
	    public void DIS_GetAllIncludesDisabled()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

	        var entity0 = m_Manager.CreateEntity(archetype0);
	        var entity1 = m_Manager.CreateEntity(archetype1);
	        var entity2 = m_Manager.CreateEntity(archetype1);

	        var entities = m_Manager.GetAllEntities();
	        Assert.AreEqual(3,entities.Length);
	        entities.Dispose();

	        m_Manager.DestroyEntity(entity0);
	        m_Manager.DestroyEntity(entity1);
	        m_Manager.DestroyEntity(entity2);
	    }
	}
}
