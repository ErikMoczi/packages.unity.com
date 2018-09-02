using System;
using System.Collections.Generic;
using UnityEditor.Build.Interfaces;

namespace UnityEditor.Build
{
    public class BuildContext : IBuildContext
    {
        Dictionary<Type, IContextObject> m_ContextObjects;

        public BuildContext()
        {
            m_ContextObjects = new Dictionary<Type, IContextObject>();
        }

        public BuildContext(params IContextObject[] buildParams)
        {
            m_ContextObjects = new Dictionary<Type, IContextObject>();
            foreach (var buildParam in buildParams)
                SetContextObject(buildParam);
        }

        public void SetContextObject<T>(IContextObject contextObject) where T : IContextObject
        {
            var type = typeof(T);
            if (!type.IsInterface)
                throw new InvalidOperationException(string.Format("Passed in type '{0}' is not an interface.", type));
            if (!(contextObject is T))
                throw new InvalidOperationException(string.Format("'{0}' is not of passed in type '{1}'.", contextObject.GetType(), type));
            m_ContextObjects[typeof(T)] = contextObject;
        }

        public void SetContextObject(IContextObject contextObject)
        {
            var iCType = typeof(IContextObject);
            Type[] iTypes = contextObject.GetType().GetInterfaces();
            foreach (var iType in iTypes)
            {
                if (!iCType.IsAssignableFrom(iType) || iType == iCType)
                    continue;
                m_ContextObjects[iType] = contextObject;
            }
        }

        public bool ContainsContextObject<T>() where T : IContextObject
        {
            return ContainsContextObject(typeof(T));
        }

        public bool ContainsContextObject(Type type)
        {
            return m_ContextObjects.ContainsKey(type);
        }

        public T GetContextObject<T>() where T : IContextObject
        {
            return (T)m_ContextObjects[typeof(T)];
        }

        public IContextObject GetContextObject(Type type)
        {
            return m_ContextObjects[type];
        }

        public bool TryGetContextObject<T>(out T contextObject) where T : IContextObject
        {
            IContextObject cachedContextObject;
            if (m_ContextObjects.TryGetValue(typeof(T), out cachedContextObject) && cachedContextObject is T)
            {
                contextObject = (T)cachedContextObject;
                return true;
            }

            contextObject = default(T);
            return false;
        }
    }
}