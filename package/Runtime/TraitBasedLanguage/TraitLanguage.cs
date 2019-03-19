using System;
using Unity.Entities;
using Unity.Properties;

namespace Unity.AI.Planner.DomainLanguage.TraitBased
{
    /// <summary>
    /// Component used to mark state entities
    /// </summary>
    public struct State : IComponentData { }

    /// <summary>
    /// A unique identifier assigned to each domain object within a state
    /// </summary>
    public struct DomainObjectID : IStructPropertyContainer<DomainObjectID>, IEquatable<DomainObjectID>
    {
        static ValueStructProperty<DomainObjectID, int> s_ValueProperty{ get; }
            = new ValueStructProperty<DomainObjectID, int>(
                "Value",
                (ref DomainObjectID c) => c.m_Value,
                (ref DomainObjectID c, int v) => c.m_Value = v
            );

        static StructPropertyBag<DomainObjectID> s_PropertyBag { get; }
            = new StructPropertyBag<DomainObjectID>(s_ValueProperty);

        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.PropertyBag" />
        public IPropertyBag PropertyBag => s_PropertyBag;

        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.VersionStorage" />
        public IVersionStorage VersionStorage => null;

        int m_Value;

        /// <summary>
        /// The reserved DomainObjectID value specifying a reference to no domain object
        /// </summary>
        public static DomainObjectID None = new DomainObjectID { m_Value = 0 };

        static int s_DomainObjectIDs = 1; // 0 is the same as default (uninitialized)

        /// <summary>
        /// Provides a new DomainObjectID with an unassigned ID
        /// </summary>
        /// <returns>Returns a new DomainObjectID with an unassigned ID</returns>
        public static DomainObjectID GetNext()
        {
            return new DomainObjectID { m_Value = s_DomainObjectIDs++ };
        }

        /// <summary>
        /// Compares two given DomainObjectIDs
        /// </summary>
        /// <param name="x">A DomainObjectID</param>
        /// <param name="y">A DomainObjectID</param>
        /// <returns>Returns if two DomainObjectIDs are equal</returns>
        public static bool operator ==(DomainObjectID x, DomainObjectID y) => x.m_Value == y.m_Value;

        /// <summary>
        /// Compares two given DomainObjectIDs
        /// </summary>
        /// <param name="x">A DomainObjectID</param>
        /// <param name="y">A DomainObjectID</param>
        /// <returns>Returns if two DomainObjectIDs are not equal</returns>
        public static bool operator !=(DomainObjectID x, DomainObjectID y) => x.m_Value != y.m_Value;

        /// <summary>
        /// Compares the DomainObjectID to another DomainObjectID
        /// </summary>
        /// <param name="other">The DomainObjectID for comparison</param>
        /// <returns>Returns true if the DomainObjectIDs are equal</returns>
        public bool Equals(DomainObjectID other) => m_Value == other.m_Value;

        /// <inheritdoc />
        public override bool Equals(object obj) => !(obj is null) && obj is DomainObjectID other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => m_Value;

        /// <inheritdoc />
        public void MakeRef<TContext>(ByRef<DomainObjectID, TContext> byRef, TContext context)
        {
            byRef(ref this, context);
        }

        /// <inheritdoc />
        public TReturn MakeRef<TContext, TReturn>(ByRef<DomainObjectID, TContext, TReturn> byRef, TContext context)
        {
            return byRef(ref this, context);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Equals(None) ? "None" : $"<< {m_Value} >>";
        }
    }

    /// <summary>
    /// The trait denoting that an entity represents a domain object
    /// </summary>
    public struct DomainObjectTrait : ITrait<DomainObjectTrait>, IEquatable<DomainObjectTrait>
    {
        /// <summary>
        /// A unique ID assigned to the domain object
        /// </summary>
        public DomainObjectID ID;

        static StructValueStructProperty<DomainObjectTrait, DomainObjectID> s_IDProperty { get; }
            = new StructValueStructProperty<DomainObjectTrait, DomainObjectID>(
                "ID",
                (ref DomainObjectTrait c) => c.ID,
                (ref DomainObjectTrait c, DomainObjectID v) => c.ID = v,
                GetValueRef
            );

        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.PropertyBag" />
        public IPropertyBag PropertyBag => s_PropertyBag;

        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.VersionStorage" />
        public IVersionStorage VersionStorage => null;

        static StructPropertyBag<DomainObjectTrait> s_PropertyBag = new StructPropertyBag<DomainObjectTrait>(s_IDProperty);

        /// <inheritdoc />
        public bool Equals(DomainObjectTrait other) => ID.Equals(other.ID);

        /// <inheritdoc />
        public override int GetHashCode() => ID.GetHashCode();

        /// <summary>
        /// Provides a new DomainObjectTrait with a unique DomainObjectID
        /// </summary>
        /// <returns>Returns a new DomainObjectTrait with an new unique DomainObjectID</returns>
        public static DomainObjectTrait GetNext()
        {
            return new DomainObjectTrait { ID = DomainObjectID.GetNext() };
        }

        /// <inheritdoc />
        public void SetComponentData(EntityManager entityManager, Entity domainObjectEntity)
        {
            entityManager.SetComponentData(domainObjectEntity, this);
        }

        /// <inheritdoc />
        public void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity) { }

        static void GetValueRef(StructValueStructProperty<DomainObjectTrait, DomainObjectID>.ByRef byRef,
            StructValueStructProperty<DomainObjectTrait, DomainObjectID> property, ref DomainObjectTrait container,
            IPropertyVisitor visitor)
        {
            byRef(property, ref container, ref container.ID, visitor);
        }

        /// <inheritdoc />
        public void MakeRef<TContext>(ByRef<DomainObjectTrait, TContext> byRef, TContext context)
        {
            byRef(ref this, context);
        }

        /// <inheritdoc />
        public TReturn MakeRef<TContext, TReturn>(ByRef<DomainObjectTrait, TContext, TReturn> byRef, TContext context)
        {
            return byRef(ref this, context);
        }
    }

    /// <summary>
    /// A container used as a reference to another entity, for use in buffers
    /// </summary>
    [InternalBufferCapacity(3)]
    public struct DomainObjectReference : IBufferElementData
    {
        /// <summary>
        /// The entity to which this refers
        /// </summary>
        public Entity DomainObjectEntity;
    }
}
