using System;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Experimental.U2D.Common;

namespace UnityEngine.Experimental.U2D.Animation
{
    public class SpriteSkinEntity : GameObjectEntity
    {
        SpriteSkin m_SpriteSkin;
        SpriteSkin spriteSkin
        {
            get
            {
                if (m_SpriteSkin == null)
                    m_SpriteSkin = GetComponent<SpriteSkin>();
                return m_SpriteSkin;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetupEntity();
            SetupSpriteSkin();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DeactivateSkinning();
        }

        private void SetupEntity()
        {
            if (EntityManager == null)
                return;

            EntityManager.AddBuffer<Vertex>(Entity);
            EntityManager.AddBuffer<BoneTransform>(Entity);
            EntityManager.AddComponent(Entity, typeof(WorldToLocal));
            EntityManager.AddSharedComponentData(Entity, new SpriteComponent() { Value = null });
        }

        private void SetupSpriteSkin()
        {
            if (spriteSkin != null)
            {
                spriteSkin.ForceSkinning = true;
                
                if (spriteSkin.bounds.extents != Vector3.zero) //Maybe log a warning?
                    InternalEngineBridge.SetLocalAABB(spriteSkin.spriteRenderer, spriteSkin.bounds);
            }
        }

        private void DeactivateSkinning()
        {
            if (spriteSkin != null)
                spriteSkin.DeactivateSkinning();
        }
    } 
}
