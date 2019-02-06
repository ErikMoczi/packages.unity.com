using System;

namespace Unity.Tiny.Runtime.Tilemap2D
{
    internal partial struct TinyTile : IEquatable<TinyTile>
    {
        public override bool Equals(object obj) => obj is TinyTile o && Equals(o);
        public bool Equals(TinyTile other) => other.sprite == sprite && other.color == color && other.colliderType == colliderType;
        public override int GetHashCode() => sprite.GetHashCode() ^ color.GetHashCode() ^ colliderType.GetHashCode();
    }
}
