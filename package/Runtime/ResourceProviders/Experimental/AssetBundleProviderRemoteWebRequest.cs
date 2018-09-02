using System;
using UnityEngine;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ResourceManagement.AsyncOperations;
using ResourceManagement.Util;

namespace ResourceManagement.ResourceProviders.Experimental
{
    public class AssetBundleProviderRemoteWebRequest : ResourceProviderBase
    {
        internal class InternalOp<TObject> : AsyncOperationBase<TObject>
            where TObject : class
        {
            IResourceLocation m_location;
            int m_startFrame;
            ChunkedMemoryStream data;
            byte[] buffer = new byte[1024 * 1024];
            bool complete;
            public InternalOp() : base("") {}

            public InternalOp<TObject> Start(IResourceLocation loc)
            {
                m_result = null;
                m_location = loc;
                m_id = loc.id;
                complete = false;
                m_startFrame = Time.frameCount;
                data = new ChunkedMemoryStream();
                CompletionUpdater.UpdateUntilComplete("WebRequest" + loc.id, CompleteInMainThread);
                var req = WebRequest.Create(m_location.id);
                req.BeginGetResponse(AsyncCallback, req);
                return this;
            }

            void AsyncCallback(IAsyncResult ar)
            {
                HttpWebRequest req = ar.AsyncState as HttpWebRequest;
                var response = req.EndGetResponse(ar);
                var stream = (response as HttpWebResponse).GetResponseStream();
                stream.BeginRead(buffer, 0, buffer.Length, OnRead, stream);
            }

            void OnRead(IAsyncResult ar)
            {
                var responseStream = ar.AsyncState as System.IO.Stream;
                int read = responseStream.EndRead(ar);
                if (read > 0)
                {
                    data.Write(buffer, 0, read);
                    responseStream.BeginRead(buffer, 0, buffer.Length, OnRead, responseStream);
                }
                else
                {
                    data.Position = 0;
                    complete = true;
                    responseStream.Close();
                }
            }

            public bool CompleteInMainThread()
            {
                if (!complete)
                    return false;
                AssetBundle.LoadFromStreamAsync(data).completed += InternalOp_completed;
                return true;
            }

            void InternalOp_completed(AsyncOperation obj)
            {
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, m_location, Time.frameCount - m_startFrame);
                m_result = (obj as AssetBundleCreateRequest).assetBundle as TObject;
                InvokeCompletionEvent(this);
                AsyncOperationCache.Instance.Release<TObject>(this);
                data.Close();
                data.Dispose();
                data = null;
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(loc);
        }

        public override bool Release(IResourceLocation loc, object asset)
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
