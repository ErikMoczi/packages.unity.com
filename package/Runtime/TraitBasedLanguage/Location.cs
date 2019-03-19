using System;
using Unity.Entities;
using Unity.Properties;
using UnityEngine;

namespace Unity.AI.Planner.DomainLanguage.TraitBased
{
    /// <summary>
    /// A custom trait for locations, since it is commonly used in domains
    /// </summary>
    [Serializable]
    public struct Location : ICustomTrait<Location>, IEquatable<Location>
    {
        const uint TraitMask = 1U << 31;

        int m_TransformInstanceID;
        Vector3 m_Position;
        Vector3 m_Forward;

        static ValueStructProperty<Location, int> TransformInstanceIDProperty { get; set; }
        static ValueStructProperty<Location, Transform> TransformProperty { get; set; }
        static ValueStructProperty<Location, Vector3> PositionProperty { get; set; }
        static ValueStructProperty<Location, Vector3> ForwardProperty { get; set; }

        static StructPropertyBag<Location> s_PropertyBag { get; set; }

        /// <inheritdoc />
        public IPropertyBag PropertyBag => s_PropertyBag;

        /// <inheritdoc />
        public IVersionStorage VersionStorage => null;


        static void InitializeProperties()
        {
            TransformInstanceIDProperty = new ValueStructProperty<Location, int>(
                "TransformInstanceID",
                (ref Location c) => c.m_TransformInstanceID,
                (ref Location c, int v) => c.m_TransformInstanceID = v
            );
            TransformProperty = new ValueStructProperty<Location, Transform>(
                "Transform",
                (ref Location c) => null,
                (ref Location c, Transform v) =>
                {
                    c.m_TransformInstanceID = v.GetInstanceID();
                    c.m_Position = v.position;
                    c.m_Forward = v.forward;
                });
            PositionProperty = new ValueStructProperty<Location, Vector3>(
                "Position",
                (ref Location c) => c.m_Position,
                (ref Location c, Vector3 v) => c.m_Position = v
            );
            ForwardProperty = new ValueStructProperty<Location, Vector3>(
                "Forward",
                (ref Location c) => c.m_Forward,
                (ref Location c, Vector3 v) => c.m_Forward = v
            );
        }

        static void InitializePropertyBag()
        {
            s_PropertyBag = new StructPropertyBag<Location>(
                TransformInstanceIDProperty,
                TransformProperty,
                PositionProperty,
                ForwardProperty
            );
        }

        static Location()
        {
            InitializeProperties();
            InitializePropertyBag();
        }

        /// <summary>
        /// The transform of the location
        /// </summary>
        public Transform Transform
        {
            get => null;
            set => TransformProperty.SetValue(ref this, value);
        }

        /// <summary>
        /// The ID of the transform of the location
        /// </summary>
        public int TransformInstanceID
        {
            get => TransformInstanceIDProperty.GetValue(ref this);
            set => TransformInstanceIDProperty.SetValue(ref this, value);
        }

        /// <summary>
        /// The position of the location
        /// </summary>
        public Vector3 Position
        {
            get => PositionProperty.GetValue(ref this);
            set => PositionProperty.SetValue(ref this, value);
        }

        /// <summary>
        /// The forward vector of the location
        /// </summary>
        public Vector3 Forward
        {
            get => ForwardProperty.GetValue(ref this);
            set => ForwardProperty.SetValue(ref this, value);
        }

        /// <summary>
        /// Compares the location to another
        /// </summary>
        /// <param name="other">Another location to which the location is compared</param>
        /// <returns>Returns true if the two locations are equal</returns>
        public bool Equals(Location other)
        {
            return m_TransformInstanceID.Equals(other.m_TransformInstanceID)
                && m_Position == other.m_Position
                && m_Forward == other.m_Forward;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return 397 ^ m_TransformInstanceID.GetHashCode();
        }

        /// <inheritdoc />
        public void SetComponentData(EntityManager entityManager, Entity domainObjectEntity)
        {
            SetTraitMask(entityManager, domainObjectEntity);
            entityManager.SetComponentData(domainObjectEntity, this);
        }

        /// <inheritdoc />
        public void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity)
        {
            var objectHash = entityManager.GetComponentData<HashCode>(domainObjectEntity);
            objectHash.TraitMask = objectHash.TraitMask | TraitMask;
            entityManager.SetComponentData(domainObjectEntity, objectHash);
        }

        /// <inheritdoc />
        public void MakeRef<TContext>(ByRef<Location, TContext> byRef, TContext context)
        {
            byRef(ref this, context);
        }

        /// <inheritdoc />
        public TReturn MakeRef<TContext, TReturn>(ByRef<Location, TContext, TReturn> byRef, TContext context)
        {
            return byRef(ref this, context);
        }
    }
}
