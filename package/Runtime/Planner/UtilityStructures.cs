using System;
using Unity.Entities;

namespace Unity.AI.Planner
{
    // TODO: Replace with default bool when it is supported in ECS
    /// <summary>
    /// Blittable replacement for bool in ECS structs
    /// </summary>
    [Serializable]
    public struct Bool : IEquatable<Bool>
    {
        /// <summary>
        /// Enum defining possible values: true or false
        /// </summary>
        public enum BoolValue
        {
            False,
            True
        }

        /// <summary>
        /// The value of the bool.
        /// </summary>
        public BoolValue Value;

        /// <summary>
        /// Operator for checking if Bool value is True
        /// </summary>
        /// <param name="b">The Bool value to evaluate</param>
        /// <returns>Returns true if value is True</returns>
        public static bool operator true(Bool b) => b.Value == BoolValue.True;

        /// <summary>
        /// Operator for checking if Bool value is False
        /// </summary>
        /// <param name="b">The Bool value to evaluate</param>
        /// <returns>Returns false if the value is False</returns>
        public static bool operator false(Bool b) => b.Value == BoolValue.False;

        /// <summary>
        /// Operator for getting the negated Bool value
        /// </summary>
        /// <param name="b">The Bool value to evaluate</param>
        /// <returns>Returns the negated value</returns>
        public static bool operator !(Bool b) => b.Value == BoolValue.False;

        /// <summary>
        /// Operator for converting a bool to a Bool
        /// </summary>
        /// <param name="b">The bool value to convert</param>
        /// <returns>Returns the Bool value of the argument</returns>
        public static implicit operator Bool(bool b) => new Bool { Value = b ? BoolValue.True : BoolValue.False };

        /// <summary>
        /// Operator for converting a Bool to a bool
        /// </summary>
        /// <param name="b">The Bool value to convert</param>
        /// <returns>Returns the bool value of the argument</returns>
        public static implicit operator bool(Bool b) => b.Value == BoolValue.True;

        /// <summary>
        /// Compares the value of two Bools
        /// </summary>
        /// <param name="other">The Bool with which to compare values</param>
        /// <returns>Returns true if both Bools have equal values</returns>
        public bool Equals(Bool other) => Value == other.Value;

        /// <summary>
        /// Computes the hash code of the Bool
        /// </summary>
        /// <returns>Returns the hash code of the Bool</returns>
        public override int GetHashCode() => (int)Value;

        /// <summary>
        /// Converts the Bool to a string
        /// </summary>
        /// <returns>Returns the string representation of the Bool</returns>
        public override string ToString() => ((bool)this).ToString();
    }

    struct HashCode : IComponentData, IEquatable<HashCode>
    {
        public int Value;
        public uint TraitMask;

        public override bool Equals(object o) => (o is HashCode other) && Equals(other);
        public bool Equals(HashCode other) => Value == other.Value && TraitMask == other.TraitMask;
        public static bool operator ==(HashCode x, HashCode y) => x.Value == y.Value && x.TraitMask == y.TraitMask;
        public static bool operator !=(HashCode x, HashCode y) => x.Value != y.Value || x.TraitMask != y.TraitMask;

        public override int GetHashCode() => Value; // Could add in TraitMask.
    }
}
