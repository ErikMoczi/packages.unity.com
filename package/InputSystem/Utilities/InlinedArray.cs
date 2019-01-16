using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Input.Utilities
{
    /// <summary>
    /// Helper to avoid array allocations if there's only a single value in the
    /// array.
    /// </summary>
    /// <typeparam name="TValue">Element type for the array.</typeparam>
    internal struct InlinedArray<TValue>
    {
        // We inline the first value so if there's only one, there's
        // no additional allocation. If more are added, we allocate an array.
        public int length;
        public TValue firstValue;
        public TValue[] additionalValues;

        public TValue this[int index]
        {
            get
            {
                if (index < 0 || index >= length)
                    throw new ArgumentOutOfRangeException("index");

                if (index == 0)
                    return firstValue;

                return additionalValues[index - 1];
            }
            set
            {
                if (index < 0 || index >= length)
                    throw new ArgumentOutOfRangeException("index");

                if (index == 0)
                    firstValue = value;
                else
                    additionalValues[index - 1] = value;
            }
        }

        public void Clear()
        {
            length = 0;
            firstValue = default(TValue);
            additionalValues = null;
        }

        public InlinedArray<TValue> Clone()
        {
            return new InlinedArray<TValue>
            {
                length = length,
                firstValue = firstValue,
                additionalValues = additionalValues != null ? (TValue[])additionalValues.Clone() : null
            };
        }

        public void SetLength(int size)
        {
            // Null out everything we're cutting off.
            if (size < length)
            {
                for (var i = size; i < length; ++i)
                    this[i] = default(TValue);
            }

            length = size;

            if (size > 1 && (additionalValues == null || additionalValues.Length < size - 1))
                additionalValues = new TValue[size - 1];
        }

        public TValue[] ToArray()
        {
            return ArrayHelpers.Join(firstValue, additionalValues);
        }

        public int IndexOf(TValue value)
        {
            var comparer = EqualityComparer<TValue>.Default;
            if (length > 0)
            {
                if (comparer.Equals(firstValue, value))
                    return 0;
                if (additionalValues != null)
                {
                    for (var i = 0; i < length - 1; ++i)
                        if (comparer.Equals(additionalValues[i], value))
                            return i + 1;
                }
            }

            return -1;
        }

        public void Append(TValue value)
        {
            if (length == 0)
            {
                firstValue = value;
            }
            else if (additionalValues == null)
            {
                additionalValues = new TValue[1];
                additionalValues[0] = value;
            }
            else
            {
                Array.Resize(ref additionalValues, length);
                additionalValues[length - 1] = value;
            }

            ++length;
        }

        public void AppendWithCapacity(TValue value)
        {
            if (length == 0)
            {
                firstValue = value;
            }
            else
            {
                var numAdditionalValues = length - 1;
                ArrayHelpers.AppendWithCapacity(ref additionalValues, ref numAdditionalValues, value);
            }
            ++length;
        }

        public void Remove(TValue value)
        {
            if (length < 1)
                return;

            if (EqualityComparer<TValue>.Default.Equals(firstValue, value))
            {
                RemoveAt(0);
            }
            else if (additionalValues != null)
            {
                for (var i = 0; i < length - 1; ++i)
                {
                    if (EqualityComparer<TValue>.Default.Equals(additionalValues[i], value))
                    {
                        RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException("index");

            if (index == 0)
            {
                if (additionalValues != null)
                {
                    firstValue = additionalValues[0];
                    if (additionalValues.Length == 1)
                        additionalValues = null;
                    else
                    {
                        Array.Copy(additionalValues, 1, additionalValues, 0, additionalValues.Length - 1);
                        Array.Resize(ref additionalValues, additionalValues.Length - 1);
                    }
                }
                else
                {
                    firstValue = default(TValue);
                }
            }
            else
            {
                Debug.Assert(additionalValues != null);

                var numAdditionalValues = length - 1;
                if (numAdditionalValues == 1)
                {
                    // Remove only entry in array.
                    additionalValues = null;
                }
                else if (index == numAdditionalValues - 1)
                {
                    // Remove entry at end.
                    Array.Resize(ref additionalValues, numAdditionalValues - 1);
                }
                else
                {
                    // Remove entry at beginning or in middle by pasting together
                    // into a new array.
                    var newAdditionalProcessors = new TValue[numAdditionalValues - 1];
                    if (index > 0)
                    {
                        // Copy element before entry.
                        Array.Copy(additionalValues, 0, newAdditionalProcessors, 0, index);
                    }
                    if (index != numAdditionalValues - 1)
                    {
                        // Copy elements after entry.
                        Array.Copy(additionalValues, index + 1, newAdditionalProcessors, index,
                            numAdditionalValues - index - 1);
                    }
                }
            }

            --length;
        }

        public void RemoveAtByMovingTailWithCapacity(int index)
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException("index");

            if (index == 0)
            {
                if (additionalValues != null)
                {
                    firstValue = additionalValues[length - 1];
                    additionalValues[length - 1] = default(TValue);
                }
                else
                {
                    firstValue = default(TValue);
                }
            }
            else
            {
                Debug.Assert(additionalValues != null);

                var numAdditionalValues = length - 1;
                ArrayHelpers.EraseAtByMovingTail(additionalValues, ref numAdditionalValues, index - 1);
            }

            --length;
        }

        public bool RemoveAtByMovingTailWithCapacity(TValue value)
        {
            var index = IndexOf(value);
            if (index == -1)
                return false;

            RemoveAtByMovingTailWithCapacity(index);
            return true;
        }
    }
}
