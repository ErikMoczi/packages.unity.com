using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using static Unity.Physics.Math;

namespace Unity.Physics
{
    // The concrete type of a collider
    public enum ColliderType : byte
    {
        // Convex types
        Convex = 0,
        Sphere = 1,
        Capsule = 2,
        Triangle = 3,
        Quad = 4,
        Box = 5,

        // Composite types
        Mesh = 6,
        Compound = 7
    }

    // The base type of a collider
    public enum CollisionType : byte
    {
        Convex = 0,
        Composite = 1
    }

    // Interface for colliders
    internal interface ICollider : ICollidable
    {
        ColliderType Type { get; }
        CollisionType CollisionType { get; }
        MassProperties MassProperties { get; }

        // The total size of the collider in memory
        int MemorySize { get; }
    }

    // Interface for convex colliders
    internal interface IConvexCollider : ICollider
    {
        CollisionFilter Filter { get; set; }
        Material Material { get; set; }
    }

    // Interface for composite colliders
    internal interface ICompositeCollider : ICollider
    {
        // The combined filter of all the child colliders.
        CollisionFilter Filter { get; }

        // The maximum number of bits needed to identify a child of this collider.
        uint NumColliderKeyBits { get; }

        // Get a child of this collider.
        // Return false if the key is not valid.
        bool GetChild(ref ColliderKey key, out ChildCollider child);

        // Get a leaf of this collider.
        // Return false if the key is not valid.
        bool GetLeaf(ColliderKey key, out ChildCollider leaf);

        // Get all the leaves of this collider.
        void GetLeaves<T>(ref T collector) where T : struct, ILeafColliderCollector;
    }

    // Interface for collecting leaf colliders
    public interface ILeafColliderCollector
    {
        void AddLeaf(ColliderKey key, ref ChildCollider leaf);

        void PushCompositeCollider(ColliderKeyPath compositeKey, MTransform parentFromComposite, out MTransform worldFromParent);

        void PopCompositeCollider(uint numCompositeKeyBits, MTransform worldFromParent);
    }

    // Base struct common to all colliders.
    // Dispatches the interface methods to appropriate implementations for the collider type.
    public struct Collider : ICompositeCollider
    {
        private ColliderHeader m_Header;

        #region ICollider

        public ColliderType Type => m_Header.Type;
        public CollisionType CollisionType => m_Header.CollisionType;

        public unsafe int MemorySize
        {
            get
            {
                fixed (Collider* collider = &this)
                {
                    switch (collider->Type)
                    {
                        case ColliderType.Convex:
                            return ((ConvexCollider*)collider)->MemorySize;
                        case ColliderType.Sphere:
                            return ((SphereCollider*)collider)->MemorySize;
                        case ColliderType.Capsule:
                            return ((CapsuleCollider*)collider)->MemorySize;
                        case ColliderType.Triangle:
                        case ColliderType.Quad:
                            return ((PolygonCollider*)collider)->MemorySize;
                        case ColliderType.Box:
                            return ((BoxCollider*)collider)->MemorySize;
                        case ColliderType.Mesh:
                            return ((MeshCollider*)collider)->MemorySize;
                        case ColliderType.Compound:
                            return ((CompoundCollider*)collider)->MemorySize;
                        default:
                            //Assert.IsTrue(Enum.IsDefined(typeof(ColliderType), collider->Type));
                            return 0;
                    }
                }
            }
        }

        public CollisionFilter Filter
        {
            get => m_Header.Filter;
            set
            {
                // Disallow changing the filter of composite types directly, since that is a combination of its children
                if(m_Header.CollisionType == CollisionType.Convex)
                {
                    m_Header.Filter = value;
                }
            }
        }

        public unsafe MassProperties MassProperties
        {
            get
            {
                fixed (Collider* collider = &this)
                {
                    switch (collider->Type)
                    {
                        case ColliderType.Convex:
                            return ((ConvexCollider*)collider)->MassProperties;
                        case ColliderType.Sphere:
                            return ((SphereCollider*)collider)->MassProperties;
                        case ColliderType.Capsule:
                            return ((CapsuleCollider*)collider)->MassProperties;
                        case ColliderType.Triangle:
                        case ColliderType.Quad:
                            return ((PolygonCollider*)collider)->MassProperties;
                        case ColliderType.Box:
                            return ((BoxCollider*)collider)->MassProperties;
                        case ColliderType.Mesh:
                            return ((MeshCollider*)collider)->MassProperties;
                        case ColliderType.Compound:
                            return ((CompoundCollider*)collider)->MassProperties;
                        default:
                            //Assert.IsTrue(Enum.IsDefined(typeof(ColliderType), collider->Type));
                            return MassProperties.UnitSphere;
                    }
                }
            }
        }

        #endregion

        #region ICompositeCollider

        public unsafe uint NumColliderKeyBits
        {
            get
            {
                fixed (Collider* collider = &this)
                {
                    switch (collider->Type)
                    {
                        case ColliderType.Mesh:
                            return ((MeshCollider*)collider)->NumColliderKeyBits;
                        case ColliderType.Compound:
                            return ((CompoundCollider*)collider)->NumColliderKeyBits;
                        default:
                            //Assert.IsTrue(Enum.IsDefined(typeof(ColliderType), collider->Type));
                            return 0;
                    }
                }
            }
        }

        public unsafe bool GetChild(ref ColliderKey key, out ChildCollider child)
        {
            fixed (Collider* collider = &this)
            {
                switch (collider->Type)
                {
                    case ColliderType.Mesh:
                        return ((MeshCollider*)collider)->GetChild(ref key, out child);
                    case ColliderType.Compound:
                        return ((CompoundCollider*)collider)->GetChild(ref key, out child);
                    default:
                        //Assert.IsTrue(Enum.IsDefined(typeof(ColliderType), collider->Type));
                        child = new ChildCollider();
                        return false;
                }
            }
        }

        public unsafe bool GetLeaf(ColliderKey key, out ChildCollider leaf)
        {
            fixed (Collider* collider = &this)
            {
                return GetLeafCollider(collider, RigidTransform.identity, key, out leaf);
            }
        }

        public unsafe void GetLeaves<T>(ref T collector) where T : struct, ILeafColliderCollector
        {
            fixed (Collider* collider = &this)
            {
                switch (collider->Type)
                {
                    case ColliderType.Mesh:
                        ((MeshCollider*)collider)->GetLeaves(ref collector);
                        break;
                    case ColliderType.Compound:
                        ((CompoundCollider*)collider)->GetLeaves(ref collector);
                        break;
                }
            }
        }

        // Get a leaf of a collider hierarchy.
        // Return false if the key is not valid for the collider.
        public static unsafe bool GetLeafCollider(Collider* root, RigidTransform rootTransform, ColliderKey key, out ChildCollider leaf)
        {
            leaf = new ChildCollider(root, rootTransform);
            while (leaf.Collider != null)
            {
                if (!leaf.Collider->GetChild(ref key, out ChildCollider child))
                {
                    break;
                }
                leaf = new ChildCollider(leaf, child);
            }
            return (leaf.Collider == null || leaf.Collider->CollisionType == CollisionType.Convex);
        }

        #endregion

        #region ICollidable

        // Calculate a bounding box around this collider.
        public Aabb CalculateAabb()
        {
            return CalculateAabb(RigidTransform.identity);
        }

        // Calculate a bounding box around this collider, at the given transform.
        public unsafe Aabb CalculateAabb(RigidTransform transform)
        {
            fixed (Collider* collider = &this)
            {
                switch (collider->Type)
                {
                    case ColliderType.Convex:
                        return ((ConvexCollider*)collider)->CalculateAabb(transform);
                    case ColliderType.Sphere:
                        return ((SphereCollider*)collider)->CalculateAabb(transform);
                    case ColliderType.Capsule:
                        return ((CapsuleCollider*)collider)->CalculateAabb(transform);
                    case ColliderType.Triangle:
                    case ColliderType.Quad:
                        return ((PolygonCollider*)collider)->CalculateAabb(transform);
                    case ColliderType.Box:
                        return ((BoxCollider*)collider)->CalculateAabb(transform);
                    case ColliderType.Mesh:
                        return ((MeshCollider*)collider)->CalculateAabb(transform);
                    case ColliderType.Compound:
                        return ((CompoundCollider*)collider)->CalculateAabb(transform);
                    default:
                        //Assert.IsTrue(Enum.IsDefined(typeof(ColliderType), collider->Type));
                        return Aabb.Empty;
                }
            }
        }

        // Cast a ray against this collider.
        public bool CastRay(RaycastInput input) => QueryWrappers.RayCast(ref this, input);
        public bool CastRay(RaycastInput input, out RaycastHit closestHit) => QueryWrappers.RayCast(ref this, input, out closestHit);
        public bool CastRay(RaycastInput input, ref NativeList<RaycastHit> allHits) => QueryWrappers.RayCast(ref this, input, ref allHits);
        public unsafe bool CastRay<T>(RaycastInput input, ref T collector) where T : struct, ICollector<RaycastHit>
        {
            fixed (Collider* target = &this)
            {
                return RaycastQueries.RayCollider(input, target, ref collector);
            }
        }

        // Cast another collider against this one.
        public bool CastCollider(ColliderCastInput input) => QueryWrappers.ColliderCast(ref this, input);
        public bool CastCollider(ColliderCastInput input, out ColliderCastHit closestHit) => QueryWrappers.ColliderCast(ref this, input, out closestHit);
        public bool CastCollider(ColliderCastInput input, ref NativeList<ColliderCastHit> allHits) => QueryWrappers.ColliderCast(ref this, input, ref allHits);
        public unsafe bool CastCollider<T>(ColliderCastInput input, ref T collector) where T : struct, ICollector<ColliderCastHit>
        {
            fixed (Collider* target = &this)
            {
                return ColliderCastQueries.ColliderCollider(input, target, ref collector);
            }
        }

        // Calculate the distance from a point to this collider.
        public bool CalculateDistance(PointDistanceInput input) => QueryWrappers.CalculateDistance(ref this, input);
        public bool CalculateDistance(PointDistanceInput input, out DistanceHit closestHit) => QueryWrappers.CalculateDistance(ref this, input, out closestHit);
        public bool CalculateDistance(PointDistanceInput input, ref NativeList<DistanceHit> allHits) => QueryWrappers.CalculateDistance(ref this, input, ref allHits);
        public unsafe bool CalculateDistance<T>(PointDistanceInput input, ref T collector) where T : struct, ICollector<DistanceHit>
        {
            fixed (Collider* target = &this)
            {
                return DistanceQueries.PointCollider(input, target, ref collector);
            }
        }

        // Calculate the distance from another collider to this one.
        public bool CalculateDistance(ColliderDistanceInput input) => QueryWrappers.CalculateDistance(ref this, input);
        public bool CalculateDistance(ColliderDistanceInput input, out DistanceHit closestHit) => QueryWrappers.CalculateDistance(ref this, input, out closestHit);
        public bool CalculateDistance(ColliderDistanceInput input, ref NativeList<DistanceHit> allHits) => QueryWrappers.CalculateDistance(ref this, input, ref allHits);
        public unsafe bool CalculateDistance<T>(ColliderDistanceInput input, ref T collector) where T : struct, ICollector<DistanceHit>
        {
            fixed (Collider* target = &this)
            {
                return DistanceQueries.ColliderCollider(input, target, ref collector);
            }
        }

        #endregion
    }

    // Header common to all colliders
    public struct ColliderHeader
    {
        public ColliderType Type;
        public CollisionType CollisionType;
        public byte Version;    // increment whenever the collider data has changed
        public byte Magic;      // always = 0xff (for validation)

        public CollisionFilter Filter;
    }

    // Header common to all convex colliders
    public struct ConvexColliderHeader
    {
        public ColliderType Type;
        public CollisionType CollisionType;
        public byte Version;
        public byte Magic;

        public CollisionFilter Filter;
        public Material Material;
    }

    // An opaque key which packs a path to a specific leaf of a collider hierarchy into a single integer.
    public struct ColliderKey : IEquatable<ColliderKey>
    {
        public uint Value { get; internal set; }

        public static readonly ColliderKey Empty = new ColliderKey { Value = uint.MaxValue };

        internal ColliderKey(uint numSubKeyBits, uint subKey)
        {
            Value = uint.MaxValue;
            PushSubKey(numSubKeyBits, subKey);
        }

        public bool Equals(ColliderKey other)
        {
            return Value == other.Value;
        }

        // Append a sub key to the front of the path
        // "numSubKeyBits" is the maximum number of bits required to store any value for this sub key.
        // Returns false if the key is empty.
        public void PushSubKey(uint numSubKeyBits, uint subKey)
        {
            uint parentPart = (uint)((ulong)subKey << 32 - (int)numSubKeyBits);
            uint childPart = Value >> (int)numSubKeyBits;
            Value = parentPart | childPart;
        }

        // Extract a sub key from the front of the path.
        // "numSubKeyBits" is the maximum number of bits required to store any value for this sub key.
        // Returns false if the key is empty.
        public bool PopSubKey(uint numSubKeyBits, out uint subKey)
        {
            if (Value != uint.MaxValue)
            {
                subKey = Value >> (32 - (int)numSubKeyBits);
                Value = ((1 + Value) << (int)numSubKeyBits) - 1;
                return true;
            }

            subKey = uint.MaxValue;
            return false;
        }
    }

    // Stores a ColliderKey along with the number of bits in it that are used.
    // This is useful for building keys from root to leaf, the bit count shows where to place the child key bits
    public struct ColliderKeyPath
    {
        private ColliderKey m_Key;
        private uint m_NumKeyBits;

        public ColliderKey Key => m_Key;

        public static ColliderKeyPath Empty => new ColliderKeyPath(ColliderKey.Empty, 0);

        public ColliderKeyPath(ColliderKey key, uint numKeyBits)
        {
            m_Key = key;
            m_NumKeyBits = numKeyBits;
        }

        // Append the local key for a child of the shape referenced by this path
        public void PushChildKey(ColliderKeyPath child)
        {
            m_Key.Value &= (uint)(child.m_Key.Value >> (int)m_NumKeyBits | (ulong)0xffffffff << (int)(32 - m_NumKeyBits));
            m_NumKeyBits += child.m_NumKeyBits;
        }

        // Remove the most leafward shape's key from this path
        public void PopChildKey(uint numChildKeyBits)
        {
            m_NumKeyBits -= numChildKeyBits;
            m_Key.Value |= (uint)((ulong)0xffffffff >> (int)m_NumKeyBits);
        }

        // Get the collider key for a leaf shape that is a child of the shape referenced by this path
        public ColliderKey GetLeafKey(ColliderKey leafKeyLocal)
        {
            ColliderKeyPath leafPath = this;
            leafPath.PushChildKey(new ColliderKeyPath(leafKeyLocal, 0));
            return leafPath.Key;
        }
    }

    // A pair of collider keys.
    public struct ColliderKeyPair
    {
        // B before A for consistency with other pairs
        public ColliderKey ColliderKeyB;
        public ColliderKey ColliderKeyA;

        public static readonly ColliderKeyPair Empty = new ColliderKeyPair { ColliderKeyB = ColliderKey.Empty, ColliderKeyA = ColliderKey.Empty };
    }

    // A child/leaf collider.
    public unsafe struct ChildCollider
    {
        private readonly Collider* m_Collider; // if null, the result is in "Polygon" instead
        private PolygonCollider m_Polygon;

        // The transform of the child collider in whatever space it was queried from
        public RigidTransform TransformFromChild;

        public unsafe Collider* Collider
        {
            get
            {
                //Assert.IsTrue(m_Collider != null || m_Polygon.Vertices.Length > 0, "Accessing uninitialized Collider");
                fixed (ChildCollider* self = &this)
                {
                    return (self->m_Collider != null) ? (Collider*)self->m_Collider : (Collider*)&self->m_Polygon;
                }
            }
        }

        // Create from collider
        public ChildCollider(Collider* collider)
        {
            m_Collider = collider;
            m_Polygon = new PolygonCollider();
            TransformFromChild = new RigidTransform(quaternion.identity, float3.zero);
        }

        // Create from body
        public ChildCollider(Collider* collider, RigidTransform transform)
        {
            m_Collider = collider;
            m_Polygon = new PolygonCollider();
            TransformFromChild = transform;
        }

        // Create as triangle, from 3 vertices
        public ChildCollider(float3 a, float3 b, float3 c, CollisionFilter filter, Material material)
        {
            m_Collider = null;
            m_Polygon = new PolygonCollider();
            m_Polygon.InitAsTriangle(a, b, c, filter, material);
            TransformFromChild = new RigidTransform(quaternion.identity, float3.zero);
        }

        // Create as quad, from 4 coplanar vertices
        public ChildCollider(float3 a, float3 b, float3 c, float3 d, CollisionFilter filter, Material material)
        {
            m_Collider = null;
            m_Polygon = new PolygonCollider();
            m_Polygon.InitAsQuad(a, b, c, d, filter, material);
            TransformFromChild = new RigidTransform(quaternion.identity, float3.zero);
        }

        // Combine a parent ChildCollider with another ChildCollider describing one of its children
        public ChildCollider(ChildCollider parent, ChildCollider child)
        {
            m_Collider = child.m_Collider;
            m_Polygon = child.m_Polygon;
            TransformFromChild = math.mul(parent.TransformFromChild, child.TransformFromChild);
        }
    }
}
