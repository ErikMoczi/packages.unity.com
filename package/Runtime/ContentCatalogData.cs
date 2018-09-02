using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement;
using System.Linq;

namespace UnityEngine.AddressableAssets
{

    [Serializable]
    public class ContentCatalogData
    {
        public enum ObjectType
        {
            ASCIIString,
            UnicodeString,
            UInt16,
            UInt32,
            Int32,
            Hash128,
            JsonObject
        }

        [SerializeField]
        string[] m_providerIds;
        [SerializeField]
        string[] m_internalIds;
        [SerializeField]
        string m_keyDataString;
        [SerializeField]
        string m_bucketDataString;
        [SerializeField]
        string m_entryDataString;
        [SerializeField]
        string m_extraDataString;

        struct Bucket
        {
            public int dataOffset;
            public int[] entries;
        }

        struct CompactLocation : IResourceLocation
        {
            ResourceLocationMap m_locator;
            string m_internalId;
            string m_providerId;
            object m_dependency;
            object m_data;
            public string InternalId { get { return m_internalId; } }
            public string ProviderId { get { return m_providerId; } }
            public IList<IResourceLocation> Dependencies
            {
                get
                {
                    if (m_dependency == null)
                        return null;
                    IList<IResourceLocation> results;
                    m_locator.Locate(m_dependency, out results);
                    return results;
                }
            }
            public bool HasDependencies { get { return m_dependency != null; } }

            public object Data { get { return m_data; } }

            public override string ToString()
            {
                return m_internalId;
            }

            public CompactLocation(ResourceLocationMap locator, string internalId, string providerId, object dependencyKey, object data)
            {
                m_locator = locator;
                m_internalId = internalId;
                m_providerId = providerId;
                m_dependency = dependencyKey;
                m_data = data;
            }
        }

        static object ReadObjectFromByteArray(byte[] keyData, int dataIndex)
        {
            try
            {
                ObjectType keyType = (ObjectType)keyData[dataIndex];
                dataIndex++;
                switch (keyType)
                {
                    case ObjectType.UnicodeString:
                        {
                            var dataLength = BitConverter.ToInt32(keyData, dataIndex);
                            return System.Text.Encoding.Unicode.GetString(keyData, dataIndex + 4, dataLength);
                        }
                    case ObjectType.ASCIIString:
                        {
                            var dataLength = BitConverter.ToInt32(keyData, dataIndex);
                            return System.Text.Encoding.ASCII.GetString(keyData, dataIndex + 4, dataLength);
                        }
                    case ObjectType.UInt16: return BitConverter.ToUInt16(keyData, dataIndex);
                    case ObjectType.UInt32: return BitConverter.ToUInt32(keyData, dataIndex);
                    case ObjectType.Int32: return BitConverter.ToInt32(keyData, dataIndex);
                    case ObjectType.Hash128: return Hash128.Parse(System.Text.Encoding.ASCII.GetString(keyData, dataIndex + 1, keyData[dataIndex]));
                    case ObjectType.JsonObject:
                        {
                            int assemblyNameLength = keyData[dataIndex];
                            dataIndex++;
                            string assemblyName = System.Text.Encoding.ASCII.GetString(keyData, dataIndex, assemblyNameLength);
                            dataIndex += assemblyNameLength;

                            int classNameLength = keyData[dataIndex];
                            dataIndex++;
                            string className = System.Text.Encoding.ASCII.GetString(keyData, dataIndex, classNameLength);
                            dataIndex += classNameLength;
                            int jsonLength = BitConverter.ToInt32(keyData, dataIndex);
                            dataIndex += 4;
                            string jsonText = System.Text.Encoding.Unicode.GetString(keyData, dataIndex, jsonLength);
                            var assembly = System.Reflection.Assembly.Load(assemblyName);
                            var t = assembly.GetType(className);
                            return JsonUtility.FromJson(jsonText, t);
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            return null;
        }

        public ResourceLocationMap CreateLocator()
        {
            var bucketData = Convert.FromBase64String(m_bucketDataString);
            int bucketCount = BitConverter.ToInt32(bucketData, 0);
            var buckets = new Bucket[bucketCount];
            int bi = 4;
            for (int i = 0; i < bucketCount; i++)
            {
                var index = Deserialize(bucketData, bi);
                bi += 4;
                var entryCount = Deserialize(bucketData, bi);
                bi += 4;
                var entryArray = new int[entryCount];
                for (int c = 0; c < entryCount; c++)
                {
                    entryArray[c] = Deserialize(bucketData, bi);
                    bi += 4;
                }
                buckets[i] = new Bucket() { entries = entryArray, dataOffset = index };
            }

            var extraData = Convert.FromBase64String(m_extraDataString);
            var keyData = Convert.FromBase64String(m_keyDataString);
            var keyCount = BitConverter.ToInt32(keyData, 0);
            var keys = new object[keyCount];
            for (int i = 0; i < buckets.Length; i++)
                keys[i] = ReadObjectFromByteArray(keyData, buckets[i].dataOffset);

            var locator = new ResourceLocationMap(buckets.Length);

            var entryData = Convert.FromBase64String(m_entryDataString);
            int count = Deserialize(entryData, 0);
            List<IResourceLocation> locations = new List<IResourceLocation>(count);
            for (int i = 0; i < count; i++)
            {
                var index = 4 + i * 4 * 4;
                var internalId = Deserialize(entryData, index);
                var providerIndex = Deserialize(entryData, index + 4);
                var dependency = Deserialize(entryData, index + 8);
                var dataIndex = Deserialize(entryData, index + 12);
                object data = dataIndex < 0 ? null : ReadObjectFromByteArray(extraData, dataIndex);
                locations.Add(new CompactLocation(locator, AAConfig.ExpandPathWithGlobalVariables(m_internalIds[internalId]),
                    m_providerIds[providerIndex], dependency < 0 ? null : keys[dependency], data));
            }

            for (int i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                var key = keys[i];
                var locs = new List<IResourceLocation>(bucket.entries.Length);
                foreach (var index in bucket.entries)
                    locs.Add(locations[index]);
                locator.Add(key, locs);
            }
            return locator;
        }

        static int Deserialize(byte[] data, int offset)
        {
            return ((int)data[offset]) | (((int)data[offset + 1]) << 8) | (((int)data[offset + 2]) << 16) | (((int)data[offset + 3]) << 24);
        }

#if UNITY_EDITOR

        public bool Save(string path, bool binary)
        {
            try
            {
                if (binary)
                {
                    return false;
                }
                else
                {
                    var data = JsonUtility.ToJson(this);
                    System.IO.File.WriteAllText(path, data);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        static int WriteIntToByteArray(byte[] data, int val, int offset)
        {
            data[offset] = (byte)(val & 0xFF);
            data[offset + 1] = (byte)((val >> 8) & 0xFF);
            data[offset + 2] = (byte)((val >> 16) & 0xFF);
            data[offset + 3] = (byte)((val >> 24) & 0xFF);
            return offset + 4;
        }

        public class DataEntry
        {
            public string m_internalId;
            public string m_provider;
            public List<object> m_keys;
            public List<object> m_dependencies;
            public object m_data;
            public DataEntry(string internalId, string provider, IEnumerable<object> keys, IEnumerable<object> deps, object extraData)
            {
                m_internalId = internalId;
                m_provider = provider;
                m_keys = new List<object>(keys);
                m_dependencies = deps == null ?  new List<object>() : new List<object>(deps);
                m_data = extraData;
            }
            public DataEntry(string address, string guid, string internalId, System.Type provider, HashSet<string> labels = null, IEnumerable<object> deps = null, object extraData = null)
            {
                m_internalId = internalId;
                m_provider = provider.FullName;
                m_keys = new List<object>(labels == null ? 2 : labels.Count + 2);
                m_keys.Add(address);
                if (!string.IsNullOrEmpty(guid))
                    m_keys.Add(Hash128.Parse(guid));
                if (labels != null)
                {
                    foreach (var l in labels)
                        m_keys.Add(l);
                }
                m_dependencies = deps == null ? new List<object>() : new List<object>(deps);
                m_data = extraData;
            }

        }

        class KeyIndexer<T>
        {
            public List<T> values;
            public Dictionary<T, int> map;
            public KeyIndexer(IEnumerable<T> keyCollection, int capacity)
            {
                values = new List<T>(capacity);
                map = new Dictionary<T, int>(capacity);
                if (keyCollection != null)
                    Add(keyCollection);
            }

            public bool Add(IEnumerable<T> keyCollection)
            {
                bool isNew = false;
                foreach (var key in keyCollection)
                    Add(key, ref isNew);
                return isNew;
            }

            public int Add(T key, ref bool isNew)
            {
                int index;
                if (!map.TryGetValue(key, out index))
                {
                    isNew = true;
                    map.Add(key, index = values.Count);
                    values.Add(key);
                }
                return index;
            }
        }

        class KeyIndexer<TVal, TKey>
        {
            public List<TVal> values;
            public Dictionary<TKey, int> map;

            public KeyIndexer(IEnumerable<TKey> keyCollection, Func<TKey, TVal> func, int capacity)
            {
                values = new List<TVal>(capacity);
                map = new Dictionary<TKey, int>(capacity);
                if (keyCollection != null)
                    Add(keyCollection, func);
            }

            public KeyIndexer(IEnumerable<TVal> valCollection, Func<TVal, TKey> func, int capacity)
            {
                values = new List<TVal>(capacity);
                map = new Dictionary<TKey, int>(capacity);
                if (valCollection != null)
                    Add(valCollection, func);
            }

            public void Add(IEnumerable<TKey> keyCollection, Func<TKey, TVal> func)
            {
                foreach (var key in keyCollection)
                    Add(key, func(key));
            }

            public void Add(IEnumerable<TVal> valCollection, Func<TVal, TKey> func)
            {
                foreach (var val in valCollection)
                    Add(func(val), val);
            }

            public int Add(TKey key, TVal val)
            {
                int index;
                if (!map.TryGetValue(key, out index))
                {
                    map.Add(key, index = values.Count);
                    values.Add(val);
                }
                return index;
            }

            public TVal this[TKey key] { get { return values[map[key]]; } }
        }

        public void SetData(IList<DataEntry> data)
        {
            var providers = new KeyIndexer<string>(data.Select(s => s.m_provider), 10);
            var internalIds = new KeyIndexer<string>(data.Select(s => s.m_internalId), data.Count);
            var keys = new KeyIndexer<object>(data.SelectMany(s => s.m_keys), data.Count * 3);
            keys.Add(data.SelectMany(s => s.m_dependencies));
            var keyIndexToEntries = new KeyIndexer<List<DataEntry>, object>(keys.values, s => new List<DataEntry>(), keys.values.Count);
            var entryToIndex = new Dictionary<DataEntry, int>(data.Count);
            var extraDataList = new List<byte>();
            var entryIndexToExtraDataIndex = new Dictionary<int, int>();

            int extraDataIndex = 0;
            //create buckets of key to data entry
            for(int i = 0; i < data.Count; i++)
            {
                var e = data[i];
                int extraDataOffset = -1;
                if (e.m_data != null)
                {
                    var len = WriteObjectToByteList(e.m_data, extraDataList);
                    if (len > 0)
                    {
                        extraDataOffset = extraDataIndex;
                        extraDataIndex += len;
                    }
                }
                entryIndexToExtraDataIndex.Add(i, extraDataOffset);
                entryToIndex.Add(e, i);
                foreach (var k in e.m_keys)
                    keyIndexToEntries[k].Add(e);
            }
            m_extraDataString = Convert.ToBase64String(extraDataList.ToArray());

            //create extra entries for dependency sets
            int originalEntryCount = data.Count;
            for (int i = 0; i < originalEntryCount; i++)
            {
                var entry = data[i];
                if (entry.m_dependencies == null || entry.m_dependencies.Count < 2)
                    continue;

                //seed and and factor values taken from https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                int hashCode = 1009;
                foreach (var dep in entry.m_dependencies)
                    hashCode = hashCode * 9176 + dep.GetHashCode();
                bool isNew = false;
                keys.Add(hashCode, ref isNew);
                if (isNew)
                {
                    //if this combination of dependecies is new, add a new entry and add its key to all contained entries
                    var deps = entry.m_dependencies.Select(d => keyIndexToEntries[d][0]).ToList();
                    keyIndexToEntries.Add(hashCode, deps);
                    foreach (var dep in deps)
                        dep.m_keys.Add(hashCode);
                }

                //reset the dependency list to only contain the key of the new set
                entry.m_dependencies.Clear();
                entry.m_dependencies.Add(hashCode);
            }

            //serialize internal ids and providers
            m_internalIds = internalIds.values.ToArray();
            m_providerIds = providers.values.ToArray();

            //serialize entries
            {
                var entryData = new byte[data.Count * 4 * 4 + 4];
                var entryDataOffset = WriteIntToByteArray(entryData, data.Count, 0);
                for (int i = 0; i < data.Count; i++)
                {
                    var e = data[i];
                    entryDataOffset = WriteIntToByteArray(entryData, internalIds.map[e.m_internalId], entryDataOffset);
                    entryDataOffset = WriteIntToByteArray(entryData, providers.map[e.m_provider], entryDataOffset);
                    entryDataOffset = WriteIntToByteArray(entryData, e.m_dependencies.Count == 0 ? -1 : keyIndexToEntries.map[e.m_dependencies[0]], entryDataOffset);
                    entryDataOffset = WriteIntToByteArray(entryData, entryIndexToExtraDataIndex[i], entryDataOffset);
                }
                m_entryDataString = Convert.ToBase64String(entryData);
            }

            //serialize keys and mappings
            {
                var entryCount = keyIndexToEntries.values.Aggregate(0, (a, s) => a += s.Count);
                var bucketData = new byte[4 + keys.values.Count * 8 + entryCount * 4];
                var keyData = new List<byte>(keys.values.Count * 10);
                keyData.AddRange(BitConverter.GetBytes(keys.values.Count));
                int keyDataOffset = 4;
                int bucketDataOffset = WriteIntToByteArray(bucketData, keys.values.Count, 0);
                for (int i = 0; i < keys.values.Count; i++)
                {
                    var key = keys.values[i];
                    bucketDataOffset = WriteIntToByteArray(bucketData, keyDataOffset, bucketDataOffset);
                    keyDataOffset += WriteObjectToByteList(key, keyData);
                    var entries = keyIndexToEntries[key];
                    bucketDataOffset = WriteIntToByteArray(bucketData, entries.Count, bucketDataOffset);
                    foreach (var e in entries)
                        bucketDataOffset = WriteIntToByteArray(bucketData, entryToIndex[e], bucketDataOffset);
                }
                m_bucketDataString = Convert.ToBase64String(bucketData);
                m_keyDataString = Convert.ToBase64String(keyData.ToArray());
            }
        }

        static int WriteObjectToByteList(object obj, List<byte> buffer)
        {
            var objectType = obj.GetType();
            if (objectType == typeof(string))
            {
                string str = obj as string;
                byte[] tmp = System.Text.Encoding.Unicode.GetBytes(str);
                byte[] tmp2 = System.Text.Encoding.ASCII.GetBytes(str);
                if (System.Text.Encoding.Unicode.GetString(tmp) == System.Text.Encoding.ASCII.GetString(tmp2))
                {
                    buffer.Add((byte)ObjectType.ASCIIString);
                    buffer.AddRange(BitConverter.GetBytes(tmp2.Length));
                    buffer.AddRange(tmp2);
                    return tmp2.Length + 5;
                }
                else
                {
                    buffer.Add((byte)ObjectType.UnicodeString);
                    buffer.AddRange(BitConverter.GetBytes(tmp.Length));
                    buffer.AddRange(tmp);
                    return tmp.Length + 5;
                }
            }
            else if (objectType == typeof(UInt32))
            {
                byte[] tmp = BitConverter.GetBytes((UInt32)obj);
                buffer.Add((byte)ObjectType.UInt32);
                buffer.AddRange(tmp);
                return tmp.Length + 1;
            }
            else if (objectType == typeof(UInt16))
            {
                byte[] tmp = BitConverter.GetBytes((UInt16)obj);
                buffer.Add((byte)ObjectType.UInt16);
                buffer.AddRange(tmp);
                return tmp.Length + 1;
            }
            else if (objectType == typeof(Int32))
            {
                byte[] tmp = BitConverter.GetBytes((Int32)obj);
                buffer.Add((byte)ObjectType.Int32);
                buffer.AddRange(tmp);
                return tmp.Length + 1;
            }
            else if (objectType == typeof(int))
            {
                byte[] tmp = BitConverter.GetBytes((UInt32)obj);
                buffer.Add((byte)ObjectType.UInt32);
                buffer.AddRange(tmp);
                return tmp.Length + 1;
            }
            else if (objectType == typeof(Hash128))
            {
                var guid = (Hash128)obj;
                byte[] tmp = System.Text.Encoding.ASCII.GetBytes(guid.ToString());
                buffer.Add((byte)ObjectType.Hash128);
                buffer.Add((byte)tmp.Length);
                buffer.AddRange(tmp);
                return tmp.Length + 2;
            }
            else
            {
                var attrs = objectType.GetCustomAttributes(typeof(System.SerializableAttribute), true);
                if (attrs == null || attrs.Length == 0)
                    return 0;
                int length = 0;
                buffer.Add((byte)ObjectType.JsonObject);
                length++;

                //write assembly name
                byte[] tmpAssemblyName = System.Text.Encoding.ASCII.GetBytes(objectType.Assembly.FullName);
                buffer.Add((byte)tmpAssemblyName.Length);
                length++;
                buffer.AddRange(tmpAssemblyName);
                length += tmpAssemblyName.Length;

                //write class name
                byte[] tmpClassName = System.Text.Encoding.ASCII.GetBytes(objectType.FullName);
                buffer.Add((byte)tmpClassName.Length);
                length++;
                buffer.AddRange(tmpClassName);
                length += tmpClassName.Length;

                //write json data
                byte[] tmpJson = System.Text.Encoding.Unicode.GetBytes(JsonUtility.ToJson(obj));
                buffer.AddRange(BitConverter.GetBytes((Int32)tmpJson.Length));
                length += 4;
                buffer.AddRange(tmpJson);
                length += tmpJson.Length;
                return length;
            }
        }

#if REFERENCE_IMPLEMENTATION
        public void SetDataOld(List<ResourceLocationData> locations, List<string> labels)
        {
            var tmpEntries = new List<Entry>(locations.Count);
            var providers = new List<string>(10);
            var providerIndices = new Dictionary<string, int>(10);
            var countEstimate = locations.Count * 2 + labels.Count;
            var internalIdToEntryIndex = new Dictionary<string, int>(countEstimate);
            var internalIdList = new List<string>(countEstimate);
            List<object> keys = new List<object>(countEstimate);

            var keyToIndex = new Dictionary<object, int>(countEstimate);
            var tmpBuckets = new Dictionary<int, List<int>>(countEstimate);
            
            for (int i = 0; i < locations.Count; i++)
            {
                var rld = locations[i];
                int providerIndex = 0;
                if (!providerIndices.TryGetValue(rld.m_provider, out providerIndex))
                {
                    providerIndices.Add(rld.m_provider, providerIndex = providers.Count);
                    providers.Add(rld.m_provider);
                }

                int internalIdIndex = 0;
                if (!internalIdToEntryIndex.TryGetValue(rld.m_internalId, out internalIdIndex))
                {
                    internalIdToEntryIndex.Add(rld.m_internalId, internalIdIndex = internalIdList.Count);
                    internalIdList.Add(rld.m_internalId);
                }

                var e = new Entry() { internalId = internalIdIndex, providerIndex = (byte)providerIndex, dependency = -1 };
                if (rld.m_type == ResourceLocationData.LocationType.Int)
                    AddToBucket(tmpBuckets, keyToIndex, keys, int.Parse(rld.m_address), tmpEntries.Count, 1);
                else if (rld.m_type == ResourceLocationData.LocationType.String)
                    AddToBucket(tmpBuckets, keyToIndex, keys, rld.m_address, tmpEntries.Count, 1);
                if (!string.IsNullOrEmpty(rld.m_guid))
                    AddToBucket(tmpBuckets, keyToIndex, keys, Hash128.Parse(rld.m_guid), tmpEntries.Count, 1);
                if (rld.m_labelMask != 0)
                {
                    for (int t = 0; t < labels.Count; t++)
                    {
                        if ((rld.m_labelMask & (1 << t)) != 0)
                            AddToBucket(tmpBuckets, keyToIndex, keys, labels[t], tmpEntries.Count, 100);
                    }
                }

                tmpEntries.Add(e);
            }

            for (int i = 0; i < locations.Count; i++)
            {
                var rld = locations[i];
                int dependency = -1;
                if (rld.m_dependencies != null && rld.m_dependencies.Length > 0)
                {
                    if (rld.m_dependencies.Length == 1)
                    {
                        dependency = keyToIndex[rld.m_dependencies[0]];
                    }
                    else
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        foreach (var d in rld.m_dependencies)
                            sb.Append(d);
                        var key = sb.ToString().GetHashCode();
                        int keyIndex = -1;
                        foreach (var d in rld.m_dependencies)
                        {
                            var ki = keyToIndex[d];
                            var depBucket = tmpBuckets[ki];
                            keyIndex = AddToBucket(tmpBuckets, keyToIndex, keys, key, depBucket[0], 10);
                        }
                        dependency = keyIndex;
                    }
                    var e = tmpEntries[i];
                    e.dependency = dependency;
                    tmpEntries[i] = e;
                }
            }

            m_internalIds = internalIdList.ToArray();
            m_providerIds = providers.ToArray();
            var entryData = new byte[tmpEntries.Count * 4 * 3 + 4];
            var offset = Serialize(entryData, tmpEntries.Count, 0);
            for (int i = 0; i < tmpEntries.Count; i++)
            {
                var e = tmpEntries[i];
                offset = Serialize(entryData, e.internalId, offset);
                offset = Serialize(entryData, e.providerIndex, offset);
                offset = Serialize(entryData, e.dependency, offset);
            }
            m_entryDataString = Convert.ToBase64String(entryData);

            int bucketEntryCount = 0;
            var bucketList = new List<Bucket>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                var bucketIndex = keyToIndex[keys[i]];
                List<int> entries = tmpBuckets[bucketIndex];
                bucketList.Add(new Bucket() { entries = entries.ToArray() });
                bucketEntryCount += entries.Count;
            }

            var keyData = new List<byte>(bucketList.Count * 10);
            keyData.AddRange(BitConverter.GetBytes(bucketList.Count));
            int dataOffset = 4;
            for (int i = 0; i < bucketList.Count; i++)
            {
                var bucket = bucketList[i];
                bucket.dataOffset = dataOffset;
                bucketList[i] = bucket;
                var key = keys[i];
                var kt = key.GetType();
                if (kt == typeof(string))
                {
                    string str = key as string;
                    byte[] tmp = System.Text.Encoding.Unicode.GetBytes(str);
                    byte[] tmp2 = System.Text.Encoding.ASCII.GetBytes(str);
                    if (System.Text.Encoding.Unicode.GetString(tmp) == System.Text.Encoding.ASCII.GetString(tmp2))
                    {
                        keyData.Add((byte)KeyType.ASCIIString);
                        keyData.AddRange(tmp2);
                        dataOffset += tmp2.Length + 1;
                    }
                    else
                    {
                        keyData.Add((byte)KeyType.UnicodeString);
                        keyData.AddRange(tmp);
                        dataOffset += tmp.Length + 1;
                    }
                }
                else if (kt == typeof(UInt32))
                {
                    byte[] tmp = BitConverter.GetBytes((UInt32)key);
                    keyData.Add((byte)KeyType.UInt32);
                    keyData.AddRange(tmp);
                    dataOffset += tmp.Length + 1;
                }
                else if (kt == typeof(UInt16))
                {
                    byte[] tmp = BitConverter.GetBytes((UInt16)key);
                    keyData.Add((byte)KeyType.UInt16);
                    keyData.AddRange(tmp);
                    dataOffset += tmp.Length + 1;
                }
                else if (kt == typeof(Int32))
                {
                    byte[] tmp = BitConverter.GetBytes((Int32)key);
                    keyData.Add((byte)KeyType.Int32);
                    keyData.AddRange(tmp);
                    dataOffset += tmp.Length + 1;
                }
                else if (kt == typeof(int))
                {
                    byte[] tmp = BitConverter.GetBytes((UInt32)key);
                    keyData.Add((byte)KeyType.UInt32);
                    keyData.AddRange(tmp);
                    dataOffset += tmp.Length + 1;
                }
                else if (kt == typeof(Hash128))
                {
                    var guid = (Hash128)key;
                    byte[] tmp = System.Text.Encoding.ASCII.GetBytes(guid.ToString());
                    keyData.Add((byte)KeyType.Hash128);
                    keyData.AddRange(tmp);
                    dataOffset += tmp.Length + 1;
                }
            }
            m_keyDataString = Convert.ToBase64String(keyData.ToArray());

            var bucketData = new byte[4 + bucketList.Count * 8 + bucketEntryCount * 4];
            offset = Serialize(bucketData, bucketList.Count, 0);
            for (int i = 0; i < bucketList.Count; i++)
            {
                offset = Serialize(bucketData, bucketList[i].dataOffset, offset);
                offset = Serialize(bucketData, bucketList[i].entries.Length, offset);
                foreach (var e in bucketList[i].entries)
                    offset = Serialize(bucketData, e, offset);
            }
            m_bucketDataString = Convert.ToBase64String(bucketData);

#if SERIALIZE_CATALOG_AS_BINARY
            //TODO: investigate saving catalog as binary - roughly 20% size decrease, still needs a provider implementation
            var stream = new System.IO.MemoryStream();
            var bw = new System.IO.BinaryWriter(stream);
            foreach (var i in m_internalIds)
                 bw.Write(i);
             foreach (var p in m_providerIds)
                 bw.Write(p);
            bw.Write(entryData);
            bw.Write(keyData.ToArray());
            bw.Write(bucketData);
                        bw.Flush();
                        bw.Close();
                        stream.Flush();
                        System.IO.File.WriteAllBytes("Library/catalog_binary.bytes", stream.ToArray());
                        System.IO.File.WriteAllText("Library/catalog_binary.txt", Convert.ToBase64String(stream.ToArray()));
                        stream.Close();
#endif
        }

        private int AddToBucket(Dictionary<int, List<int>> buckets, Dictionary<object, int> keyToIndex, List<object> keys, object key, int index, int sizeHint)
        {
            int keyIndex = -1;
            if (!keyToIndex.TryGetValue(key, out keyIndex))
            {
                keyToIndex.Add(key, keyIndex = keys.Count);
                keys.Add(key);
            }

            List<int> bucket;
            if (!buckets.TryGetValue(keyIndex, out bucket))
                buckets.Add(keyIndex, bucket = new List<int>(sizeHint));
            bucket.Add(index);
            return keyIndex;
        }
#endif
#endif
    }
}
