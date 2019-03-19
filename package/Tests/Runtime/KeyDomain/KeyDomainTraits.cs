using System;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Entities;
using Unity.Properties;

namespace Unity.AI.Planner.Tests
{
    enum Color
    {
        Black,
        White
    }

    enum KeyTraitMask
    {
        ColorTrait = 1,
        CarrierTrait = 2,
        CarriableTrait = 4,
        LocalizedTrait = 8,
        LockableTrait = 16,
        EndTrait = 32
    }

    struct ColorTrait : IEquatable<ColorTrait>, ITrait
    {
        public Color Color;

        public static ValueStructProperty<ColorTrait, Color> ColorProperty { get; private set; }

        static StructPropertyBag<ColorTrait> s_PropertyBag { get; set; }
        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        static ColorTrait()
        {
            InitializeProperties();
            InitializePropertyBag();
        }

        static void InitializePropertyBag()
        {
            s_PropertyBag = new StructPropertyBag<ColorTrait>(ColorProperty);
        }

        static void InitializeProperties()
        {
            ColorProperty = new ValueStructProperty<ColorTrait, Color>(
                "Color",
                (ref ColorTrait c) => c.Color,
                (ref ColorTrait c, Color v) => c.Color = v
            );
        }

        public void SetComponentData(EntityManager entityManager, Entity domainObjectEntity)
        {
            entityManager.SetComponentData(domainObjectEntity, this);
        }

        public void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity) { }

        public bool Equals(ColorTrait other)
        {
            return Color == other.Color;
        }

        public override int GetHashCode()
        {
            return Color.GetHashCode();
        }
    }

    struct CarrierTrait : IEquatable<CarrierTrait>, ITrait
    {
        public DomainObjectID CarriedObject;

        public static ValueStructProperty<CarrierTrait, DomainObjectID> CarriedProperty { get; private set; }

        static StructPropertyBag<CarrierTrait> s_PropertyBag { get; set; }
        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        static CarrierTrait()
        {
            InitializeProperties();
            InitializePropertyBag();
        }

        static void InitializePropertyBag()
        {
            s_PropertyBag = new StructPropertyBag<CarrierTrait>(CarriedProperty);
        }

        static void InitializeProperties()
        {
            CarriedProperty = new ValueStructProperty<CarrierTrait, DomainObjectID>(
                "CarriedObject",
                (ref CarrierTrait c) => c.CarriedObject,
                (ref CarrierTrait c, DomainObjectID v) => c.CarriedObject = v
            );
        }

        public void SetComponentData(EntityManager entityManager, Entity domainObjectEntity)
        {
            entityManager.SetComponentData(domainObjectEntity, this);
        }

        public void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity) { }

        public bool Equals(CarrierTrait other)
        {
            return CarriedObject.Equals(other.CarriedObject);
        }

        public override int GetHashCode()
        {
            return CarriedObject.GetHashCode();
        }
    }

    struct CarriableTrait : IEquatable<CarriableTrait>, ITrait
    {
        public DomainObjectID Carrier;

        public static ValueStructProperty<CarriableTrait, DomainObjectID> CarrierProperty { get; private set; }

        static StructPropertyBag<CarriableTrait> s_PropertyBag { get; set; }
        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        static CarriableTrait()
        {
            InitializeProperties();
            InitializePropertyBag();
        }

        static void InitializePropertyBag()
        {
            s_PropertyBag = new StructPropertyBag<CarriableTrait>(CarrierProperty);
        }

        static void InitializeProperties()
        {
            CarrierProperty = new ValueStructProperty<CarriableTrait, DomainObjectID>(
                "Carrier",
                (ref CarriableTrait c) => c.Carrier,
                (ref CarriableTrait c, DomainObjectID v) => c.Carrier = v
            );
        }

        public void SetComponentData(EntityManager entityManager, Entity domainObjectEntity)
        {
            entityManager.SetComponentData(domainObjectEntity, this);
        }

        public void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity) { }

        public bool Equals(CarriableTrait other)
        {
            return Carrier.Equals(other.Carrier);
        }

        public override int GetHashCode()
        {
            return Carrier.GetHashCode();
        }
    }

    struct LocalizedTrait : IEquatable<LocalizedTrait>, ITrait
    {
        public DomainObjectID Location;

        public static ValueStructProperty<LocalizedTrait, DomainObjectID> LocationProperty { get; private set; }

        static StructPropertyBag<LocalizedTrait> s_PropertyBag { get; set; }
        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        static LocalizedTrait()
        {
            InitializeProperties();
            InitializePropertyBag();
        }

        static void InitializePropertyBag()
        {
            s_PropertyBag = new StructPropertyBag<LocalizedTrait>(LocationProperty);
        }

        static void InitializeProperties()
        {
            LocationProperty = new ValueStructProperty<LocalizedTrait, DomainObjectID>(
                "Location",
                (ref LocalizedTrait c) => c.Location,
                (ref LocalizedTrait c, DomainObjectID v) => c.Location = v
            );
        }

        public void SetComponentData(EntityManager entityManager, Entity domainObjectEntity)
        {
            entityManager.SetComponentData(domainObjectEntity, this);
        }

        public void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity) { }

        public bool Equals(LocalizedTrait other)
        {
            return Location.Equals(other.Location);
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode();
        }
    }

    struct LockableTrait : IEquatable<LockableTrait>, ITrait
    {
        public Bool Locked;

        public static ValueStructProperty<LockableTrait, bool> LockedProperty { get; private set; }

        static StructPropertyBag<LockableTrait> s_PropertyBag { get; set; }
        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        static LockableTrait()
        {
            InitializeProperties();
            InitializePropertyBag();
        }

        static void InitializePropertyBag()
        {
            s_PropertyBag = new StructPropertyBag<LockableTrait>(LockedProperty);
        }

        static void InitializeProperties()
        {
            LockedProperty = new ValueStructProperty<LockableTrait, bool>(
                "Locked",
                (ref LockableTrait c) => c.Locked,
                (ref LockableTrait c, bool v) => c.Locked = v
            );
        }

        public void SetComponentData(EntityManager entityManager, Entity domainObjectEntity)
        {
            entityManager.SetComponentData(domainObjectEntity, this);
        }

        public void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity) { }

        public bool Equals(LockableTrait other)
        {
            return Locked.Equals(other.Locked);
        }

        public override int GetHashCode()
        {
            return Locked.GetHashCode();
        }
    }

    struct EndTrait : IEquatable<EndTrait>, ITrait
    {
        static StructPropertyBag<EndTrait> s_PropertyBag { get; set; }
        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        static EndTrait()
        {
            InitializeProperties();
            InitializePropertyBag();
        }

        static void InitializePropertyBag()
        {
            s_PropertyBag = new StructPropertyBag<EndTrait>();
        }

        static void InitializeProperties() { }

        public void SetComponentData(EntityManager entityManager, Entity domainObjectEntity)
        {
            entityManager.SetComponentData(domainObjectEntity, this);
        }

        public void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity) { }

        public bool Equals(EndTrait other)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
