

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny
{
	/// <summary>
	/// Helper class to build and maintain Tiny management objects
	/// </summary>
	internal sealed class TinyContext : IContext
	{
		public TinyRegistry Registry { get; }
		public TinyCaretaker Caretaker { get; }
		public TinyVersionStorage VersionStorage { get; }
        public ContextUsage Usage { get; }

		private readonly Dictionary<Type, IContextManager> m_ContextManagers = new Dictionary<Type, IContextManager>();
		private readonly Dictionary<Type, Type> m_TypeToRegisteredManagerType = new Dictionary<Type, Type>();
		private readonly List<IContextManager> m_OrderedContextManagers = new List<IContextManager>();
		
		public TinyContext(ContextUsage usage)
		{
            Usage = usage;
			VersionStorage = new TinyVersionStorage(trackChanges: !usage.HasFlag(ContextUsage.LiveLink));
			Registry = new TinyRegistry(this, VersionStorage);
			Caretaker = new TinyCaretaker(VersionStorage);
			TinyDomain.LoadDomain();

			RegisterContextManagers(usage);
		}

		public TManager GetManager<TManager>()
			where TManager : class, IContextManager
		{
			if (!m_TypeToRegisteredManagerType.TryGetValue(typeof(TManager), out var type))
			{
				type = typeof(TManager);
			}

			m_ContextManagers.TryGetValue(type, out var manager);
			return manager as TManager;
		}

		internal void LoadManagers()
		{
			foreach (var manager in m_OrderedContextManagers)
			{
				manager.Load();
			}
		}

		internal void UnloadManagers()
		{
			foreach (var manager in m_OrderedContextManagers.AsEnumerable().Reverse())
			{
				manager.Unload();
			}
		}

		private void RegisterContextManagers(ContextUsage usage)
		{
			var baseType = typeof(ContextManager);
			foreach (var typeAttribute in TinyAttributeScanner.GetTypeAttributes<ContextManagerAttribute>())
			{
				var type = typeAttribute.Type;
				if (type.IsAbstract)
				{
					Debug.Log($"{TinyConstants.ApplicationName}: A context manager cannot be an abstract class.");
					continue;
				}

				if (type.IsGenericType)
				{
					Debug.Log($"{TinyConstants.ApplicationName}: A context manager cannot be a generic class.");
					continue;
				}

				if (!type.IsSubclassOf(baseType))
				{
					Debug.Log($"{TinyConstants.ApplicationName}: A context manager needs to derive from the {nameof(ContextManager)} base class.");
					continue;
				}

				if (null == type.GetConstructor(new[] { typeof(TinyContext) }))
				{
					Debug.Log($"{TinyConstants.ApplicationName}: A context manager needs to have a constructor taking an {nameof(TinyContext)} parameter.");
					continue;
				}

				if ((typeAttribute.Attribute.Usage & usage) == 0)
				{
					continue;
				}

				var attributes = type.GetInterfaces().Where(t => null != t.GetInterface(nameof(IContextManager))).ToList();
				// User provided an interface deriving from IContextManager, bind it to the interface instead of the type directly
				if (attributes.Count > 0)
				{
					// Bind the manager to the interface that derives directly from IContextManager.
					var last = attributes.Last();
					CreateManagerInstance(type, last);

					// Rebind all the other interfaces to the registered one.
					foreach (var attribute in attributes)
					{
						m_TypeToRegisteredManagerType.Add(attribute, last);
					}
				}
				// The type directly
				else
				{
					CreateManagerInstance(type, type);
				}
			}
		}

		private void CreateManagerInstance(Type concreteType, Type bindType)
		{
			if (!m_ContextManagers.ContainsKey(bindType))
			{
				var manager = (IContextManager)Activator.CreateInstance(concreteType, this);
				m_ContextManagers.Add(bindType, manager);
				m_OrderedContextManagers.Add(manager);
				m_TypeToRegisteredManagerType.Add(concreteType, bindType);
			}
		}
	}
}

