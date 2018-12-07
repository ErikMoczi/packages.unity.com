using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Animations.Rigging.AnimationJobCache
{
    public struct Index
    {
        internal int idx;
    }

    public struct Cache : System.IDisposable
    {
        NativeArray<float> m_Data;

        public Cache(float[] data)
        {
            m_Data = new NativeArray<float>(data, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_Data.Dispose();
        }

        public int GetInt(Index index)
        {
            return (int)m_Data[index.idx];
        }

        public float GetFloat(Index index)
        {
            return m_Data[index.idx];
        }

        public Vector2 GetVector2(Index index)
        {
            return new Vector2(m_Data[index.idx], m_Data[index.idx + 1]);
        }

        public Vector3 GetVector3(Index index)
        {
            return new Vector3(m_Data[index.idx], m_Data[index.idx + 1], m_Data[index.idx + 2]);
        }

        public Vector4 GetVector4(Index index)
        {
            return new Vector4(m_Data[index.idx], m_Data[index.idx + 1], m_Data[index.idx + 2], m_Data[index.idx + 3]);
        }

        public Quaternion GetQuaternion(Index index)
        {
            return new Quaternion(m_Data[index.idx], m_Data[index.idx + 1], m_Data[index.idx + 2], m_Data[index.idx + 3]);
        }

        public AffineTransform GetTransform(Index index)
        {
            return new AffineTransform(
                new Vector3(m_Data[index.idx], m_Data[index.idx + 1], m_Data[index.idx + 2]),
                new Quaternion(m_Data[index.idx + 3], m_Data[index.idx + 4], m_Data[index.idx + 5], m_Data[index.idx + 6])
                );
        }

        public void SetInt(Index index, int v)
        {
            m_Data[index.idx] = v;
        }

        public void SetFloat(Index index, float v)
        {
            m_Data[index.idx] = v;
        }

        public void SetVector2(Index index, Vector2 v)
        {
            m_Data[index.idx] = v.x;
            m_Data[index.idx + 1] = v.y;
        }

        public void SetVector3(Index index, Vector3 v)
        {
            m_Data[index.idx] = v.x;
            m_Data[index.idx + 1] = v.y;
            m_Data[index.idx + 2] = v.z;
        }

        public void SetVector4(Index index, Vector4 v)
        {
            m_Data[index.idx] = v.x;
            m_Data[index.idx + 1] = v.y;
            m_Data[index.idx + 2] = v.z;
            m_Data[index.idx + 3] = v.w;
        }

        public void SetQuaternion(Index index, Quaternion v)
        {
            m_Data[index.idx] = v.x;
            m_Data[index.idx + 1] = v.y;
            m_Data[index.idx + 2] = v.z;
            m_Data[index.idx + 3] = v.w;
        }

        public void SetTransform(Index index, AffineTransform tx)
        {
            m_Data[index.idx] = tx.translation.x;
            m_Data[index.idx + 1] = tx.translation.y;
            m_Data[index.idx + 2] = tx.translation.z;
            m_Data[index.idx + 3] = tx.rotation.x;
            m_Data[index.idx + 4] = tx.rotation.y;
            m_Data[index.idx + 5] = tx.rotation.z;
            m_Data[index.idx + 6] = tx.rotation.w;
        }

        public void SetArray(Index[] indices, float[] v)
        {
            int count = Mathf.Min(indices.Length, v.Length);
            for (int i = 0; i < count; ++i)
                m_Data[indices[i].idx] = v[i];
        }
    }

    public class CacheBuilder
    {
        List<float> m_Data;

        public CacheBuilder()
        {
            m_Data = new List<float>();
        }

        public Index Add(float v)
        {
            m_Data.Add(v);
            return new Index { idx = m_Data.Count - 1 };
        }

        public Index Add(Vector2 v)
        {
            m_Data.Add(v.x);
            m_Data.Add(v.y);
            return new Index { idx = m_Data.Count - 2 };
        }

        public Index Add(Vector3 v)
        {
            m_Data.Add(v.x);
            m_Data.Add(v.y);
            m_Data.Add(v.z);
            return new Index { idx = m_Data.Count - 3 };
        }

        public Index Add(Vector4 v)
        {
            m_Data.Add(v.x);
            m_Data.Add(v.y);
            m_Data.Add(v.z);
            m_Data.Add(v.w);
            return new Index { idx = m_Data.Count - 4 };
        }

        public Index Add(Quaternion v)
        {
            return Add(new Vector4(v.x, v.y, v.z, v.w));
        }

        public Index Add(AffineTransform tx)
        {
            Add(tx.translation);
            Add(tx.rotation);
            return new Index { idx = m_Data.Count - 7 };
        }

        public Index[] Add(float[] v)
        {
            Index[] indices = new Index[v.Length];
            for (int i = 0, index = m_Data.Count; i < v.Length; ++i, ++index)
            {
                m_Data.Add(v[i]);
                indices[i].idx = index;
            }

            return indices;
        }

        public Cache Create() => new Cache(m_Data.ToArray());
    }
}
