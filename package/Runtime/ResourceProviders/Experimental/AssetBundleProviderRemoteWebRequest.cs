using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.ResourceManagement.Diagnostics;
#if !UNITY_METRO
namespace UnityEngine.ResourceManagement
{
    public class AssetBundleProviderRemoteWebRequest : ResourceProviderBase
    {
        internal class InternalOp<TObject> : AsyncOperationBase<TObject>, IDisposable
            where TObject : class
        {
            bool m_complete;
            int m_startFrame;
            ChunkedMemoryStream m_data;
            byte[] m_buffer = new byte[1024 * 1024];

            public InternalOp<TObject> Start(IResourceLocation location)
            {
                Result = null;
                m_context = location;
                m_complete = false;
                m_startFrame = Time.frameCount;
                m_data = new ChunkedMemoryStream();
                CompletionUpdater.UpdateUntilComplete("WebRequest" + location.InternalId, CompleteInMainThread);
                var req = WebRequest.Create(location.InternalId);
                req.BeginGetResponse(AsyncCallback, req);
                return this;
            }

            void AsyncCallback(IAsyncResult ar)
            {
                HttpWebRequest req = ar.AsyncState as HttpWebRequest;
                var response = req.EndGetResponse(ar);
                var stream = (response as HttpWebResponse).GetResponseStream();
                stream.BeginRead(m_buffer, 0, m_buffer.Length, OnRead, stream);
            }

            void OnRead(IAsyncResult ar)
            {
                var responseStream = ar.AsyncState as System.IO.Stream;
                int read = responseStream.EndRead(ar);
                if (read > 0)
                {
                    m_data.Write(m_buffer, 0, read);
                    responseStream.BeginRead(m_buffer, 0, m_buffer.Length, OnRead, responseStream);
                }
                else
                {
                    m_data.Position = 0;
                    m_complete = true;
                    responseStream.Close();
                }
            }

            public bool CompleteInMainThread()
            {
                if (!m_complete)
                    return false;
                AssetBundle.LoadFromStreamAsync(m_data).completed += InternalOp_completed;
                return true;
            }

            void InternalOp_completed(AsyncOperation obj)
            {
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, m_context, Time.frameCount - m_startFrame);
                Result = (obj as AssetBundleCreateRequest).assetBundle as TObject;
                InvokeCompletionEvent();
                AsyncOperationCache.Instance.Release<TObject>(this);
                m_data.Close();
                m_data.Dispose();
                m_data = null;
            }

            public void Dispose()
            {
                if (m_data != null)
                {
                    m_data.Close();
                    m_data.Dispose();
                    m_data = null;
                }
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(location);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            (asset as AssetBundle).Unload(true);
            return true;
        }
    }

    public sealed class ChunkedMemoryStream : Stream
    {
        const int BufferSize = 65536;
        readonly List<byte[]> m_chunks;
        long m_length;
        long m_position;

        public ChunkedMemoryStream()
        {
            m_chunks = new List<byte[]> { new byte[BufferSize], new byte[BufferSize] };
            m_position = 0;
            m_length = 0;
        }

        public void Reset()
        {
            m_position = 0;
            m_length = 0;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { return m_length; } }
        long Capacity { get { return m_chunks.Count * BufferSize; } }
        byte[] CurrentChunk { get { return m_chunks[Convert.ToInt32(m_position / BufferSize)]; } }
        int PositionInChunk { get { return Convert.ToInt32(m_position % BufferSize); } }
        int RemainingBytesInCurrentChunk { get { return CurrentChunk.Length - PositionInChunk; } }
        public override void Flush() {}

        public override long Position
        {
            get { return m_position; }
            set
            {
                m_position = value;
                if (m_position > m_length)
                    m_position = m_length - 1;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesToRead = count;
            if (m_length - m_position < bytesToRead)
                bytesToRead = Convert.ToInt32(m_length - m_position);

            int bytesreaded = 0;
            while (bytesToRead > 0)
            {
                int remainingBytesInCurrentChunk = RemainingBytesInCurrentChunk;
                if (remainingBytesInCurrentChunk > bytesToRead)
                    remainingBytesInCurrentChunk = bytesToRead;
                Buffer.BlockCopy(CurrentChunk, PositionInChunk, buffer, offset, remainingBytesInCurrentChunk);
                m_position += remainingBytesInCurrentChunk;
                offset += remainingBytesInCurrentChunk;
                bytesToRead -= remainingBytesInCurrentChunk;
                bytesreaded += remainingBytesInCurrentChunk;
            }
            return bytesreaded;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            if (value > m_length)
            {
                while (value > Capacity)
                {
                    var item = new byte[BufferSize];
                    m_chunks.Add(item);
                }
            }
            else if (value < m_length)
            {
                var decimalValue = Convert.ToDecimal(value);
                var valueToBeCompared = decimalValue % BufferSize == 0 ? Capacity : Capacity - BufferSize;
                while (value < valueToBeCompared && m_chunks.Count > 2)
                {
                    byte[] lastChunk = m_chunks.Last();
                    m_chunks.Remove(lastChunk);
                }
            }
            m_length = value;
            if (m_position > m_length - 1)
                m_position = m_length == 0 ? 0 : m_length - 1;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int bytesToWrite = count;
            while (bytesToWrite > 0)
            {
                int remainingBytesInCurrentChunk = RemainingBytesInCurrentChunk;
                if (remainingBytesInCurrentChunk > bytesToWrite)
                    remainingBytesInCurrentChunk = bytesToWrite;

                if (remainingBytesInCurrentChunk > 0)
                {
                    Buffer.BlockCopy(buffer, offset, CurrentChunk, PositionInChunk, remainingBytesInCurrentChunk);
                    offset += remainingBytesInCurrentChunk;
                    bytesToWrite -= remainingBytesInCurrentChunk;
                    m_length += remainingBytesInCurrentChunk;
                    m_position += remainingBytesInCurrentChunk;
                }

                if (Capacity == m_position)
                    m_chunks.Add(new byte[BufferSize]);
            }
        }
    }
}
#endif