using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.CacheServer;

namespace UnityEditor.Build.Pipeline.Utilities
{
    class CacheServerUploader : IDisposable
    {
        Queue<WorkItem> m_WorkItems = new Queue<WorkItem>();
        Semaphore m_Semaphore = new Semaphore(0, Int32.MaxValue);

        Client m_Client;

        bool m_Disposed;

        struct WorkItem
        {
            public FileId fileId;
            public string artifactsPath;
            public MemoryStream stream;
        }

        public CacheServerUploader(string host, int port = 8126)
        {
            m_Client = new Client(host, port);
            m_Client.Connect();

            var uploadThread = new Thread(ThreadedUploader);
            uploadThread.Start();
        }

        // We return from this function before all uploads are complete. So we must wait to dispose until all uploads are finished.
        public void QueueUpload(CacheEntry entry, string artifactsPath, MemoryStream stream)
        {
            var item = new WorkItem();
            item.fileId = FileId.From(entry.Guid.ToString(), entry.Hash.ToString());
            item.artifactsPath = artifactsPath;
            item.stream = stream;

            lock (m_WorkItems)
                m_WorkItems.Enqueue(item);
            m_Semaphore.Release();
        }

        // Called on background thread
        void ThreadedUploader()
        {
            while (true)
            {
                m_Semaphore.WaitOne();

                WorkItem item;
                lock (m_WorkItems)
                {
                    // If we got past the semaphore, and no items are left, time to clean up
                    if (m_WorkItems.Count == 0)
                    {
                        ((IDisposable)m_Semaphore).Dispose();
                        m_Client.Close();
                        return;
                    }

                    item = m_WorkItems.Dequeue();
                }

                m_Client.BeginTransaction(item.fileId);
                m_Client.Upload(FileType.Info, item.stream);

                string artifactsZip = Path.GetTempFileName();
                if (FileCompressor.Compress(item.artifactsPath, artifactsZip))
                {
                    using (var stream = new FileStream(artifactsZip, FileMode.Open, FileAccess.Read))
                        m_Client.Upload(FileType.Resource, stream);
                }
                File.Delete(artifactsZip);

                m_Client.EndTransaction();
            }
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                m_Disposed = true;
                m_Semaphore.Release();
            }
        }
    }
}
