

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Unity.Tiny;

using static Unity.Tiny.EditorInspectorAttributes;

internal static class TinyBindingsRegistration
{
    private delegate void AttributeBinder(IRegistry registry, TinyType.Reference type);
    private delegate AttributeBinder CreateBinder(Type editor);

    [TinyInitializeOnLoad(int.MinValue)]
    private static void HandleTinyDomainReload()
    {
        RegisterBinder<TinyComponentCallbackAttribute>(CreateBindingsBinder);
    }

    private static void RegisterBinder<TAttribute>(CreateBinder binder)
        where TAttribute : TinyAttribute, IIdentified<TinyId>
    {
        foreach (var typeAttribute in TinyAttributeScanner.GetTypeAttributes<TAttribute>())
        {
            var attribute = typeAttribute.Attribute as IIdentified<TinyId>;
            List<AttributeBinder> binders;
            if (!s_Lookup.TryGetValue(attribute.Id, out binders))
            {
                s_Lookup[attribute.Id] = binders = new List<AttributeBinder>();
            }

            binders.Add(binder(typeAttribute.Type));
        }
    }

    static Dictionary<TinyId, List<AttributeBinder>> s_Lookup = new Dictionary<TinyId, List<AttributeBinder>>();

    [TinyInitializeOnLoad]
    private static void Register()
    {
        TinyEventDispatcher.AddListener<TinyRegistryEventType, IRegistryObject>(TinyRegistryEventType.Registered, HandleCoreTypeRegistered);
    }

    private static void HandleCoreTypeRegistered(TinyRegistryEventType eventType, IRegistryObject obj)
    {
        if (!(obj is TinyType) || null == obj.Registry)
        {
            return;
        }

        var type = (TinyType)obj;

        List<AttributeBinder> binders;
        if (!s_Lookup.TryGetValue(obj.Id, out binders))
        {
            return;
        }
        foreach(var binder in binders)
        {
            binder(obj.Registry, (TinyType.Reference)type);
        }
    }

    private static AttributeBinder CreateBindingsBinder(Type drawer)
    {
        return CreateBinderMethod(drawer, nameof(AddBindings));
    }

    private static AttributeBinder CreateBinderMethod(Type type, string methodName)
    {
        var addBindingsMethod = typeof(TinyBindingsRegistration).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        var genericAddEditorMethod = addBindingsMethod.MakeGenericMethod(type);
        return (AttributeBinder)Delegate.CreateDelegate(typeof(AttributeBinder), genericAddEditorMethod);
    }

    private static void AddBindings<TBinding>(IRegistry registry, TinyType.Reference type)
        where TBinding : IComponentCallback, new()
    {
        type.Dereference(registry)?.AddAttribute(Callbacks(new TBinding(), type));
    }
}

