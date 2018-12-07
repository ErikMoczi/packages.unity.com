

using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using UnityEngine;

namespace Unity.Tiny
{
    using EntityRef = TinyEntity.Reference;

    [ContextManager(~(ContextUsage.Edit | ContextUsage.LiveLink))]
    [UsedImplicitly]
    internal class NullBindingsManager : ContextManager, IBindingsManager
    {
        public NullBindingsManager(TinyContext context) : base(context) { }
        public void SetConfigurationDirty(TinyEntity entity) { }
        public void SetTemporaryDependency(EntityRef @from, EntityRef to) { }
        public void RemoveTemporaryDependency(EntityRef @from, EntityRef to) { }
        public void Transfer(TinyEntity entity) { }
        public void SetAllDirty() {}
        public void TransferAll() {}
    }

    [ContextManager(ContextUsage.Edit | ContextUsage.LiveLink)]
    [UsedImplicitly]
    internal class BindingsManager : ContextManager, IBindingsManager
    {
        private class TwoWayDependency
        {
            private static List<BindingProfile>  NoProfiles  { get; } = new List<BindingProfile>();
            private static List<TinyId> NoIds { get; } = new List<TinyId>();

            private Dictionary<BindingProfile, List<TinyId>> FirstToSecond { get; } = new Dictionary<BindingProfile, List<TinyId>>();
            private Dictionary<TinyId, List<BindingProfile>> SecondToFirst { get; } = new Dictionary<TinyId, List<BindingProfile>>();

            public void Add(BindingProfile t, TinyId s)
            {
                Add(FirstToSecond, t, s);
                Add(SecondToFirst, s, t);
            }

            public List<TinyId> this[BindingProfile t]  => Get(FirstToSecond, t) ?? NoIds;
            public List<BindingProfile>  this[TinyId s] => Get(SecondToFirst, s) ?? NoProfiles;

            private static void Add<T1, T2>(IDictionary<T1, List<T2>> dict, T1 t1, T2 t2)
            {
                if (!dict.TryGetValue(t1, out var list))
                {
                    dict[t1] = list = new List<T2>();
                }
                list.Add(t2);
            }

            private static List<T2> Get<T1, T2>(IReadOnlyDictionary<T1, List<T2>> dict, T1 t1)
            {
                dict.TryGetValue(t1, out var list);
                return list;
            }
        }

        #region Static
        private static Dictionary<Type, BindingProfile> BindingTypeToInstance { get; } = new Dictionary<Type, BindingProfile>();
        private static List<BindingProfile> AllProfileInstances { get; } = new List<BindingProfile>();

        private static TwoWayDependency WithComponents { get; } = new TwoWayDependency();
        private static TwoWayDependency WithoutComponents { get; } = new TwoWayDependency();
        private static Dictionary<BindingProfile, List<BindingProfile>> ProfileDependencies { get; } = new Dictionary<BindingProfile, List<BindingProfile>>();
        #endregion

        #region Properties
        public IEntityGroupManager GroupManager { get; private set; }
        private Dictionary<EntityRef, BindingConfiguration> EntityToBindingConfiguration { get; } = new Dictionary<EntityRef, BindingConfiguration>();
        private HashSet<EntityRef> DirtyConfigurations { get; } = new HashSet<EntityRef>();
        private Dictionary<EntityRef, HashSet<EntityRef>> TransferDependencies { get; } = new Dictionary<EntityRef, HashSet<EntityRef>>();
        private HashSet<TinyEntity> Reentrance { get; } = new HashSet<TinyEntity>();
        #endregion

        #region API

        public BindingsManager(TinyContext context)
            :base(context)
        {
        }

        public override void Load()
        {
            GroupManager = Context.GetManager<IEntityGroupManager>();
        }

        public void SetAllDirty()
        {
            foreach (var entity in GroupManager.LoadedEntityGroups.Deref(Registry).Entities())
            {
                DirtyConfigurations.Add((EntityRef)entity);
            }
        }

        public void TransferAll()
        {
            foreach (var entity in GroupManager.LoadedEntityGroups.Deref(Registry).Entities())
            {
                Transfer(entity);
            }
        }

        public void SetConfigurationDirty(TinyEntity entity)
        {
            DirtyConfigurations.Add((TinyEntity.Reference)entity);
        }

        public void SetTemporaryDependency(EntityRef from, EntityRef to)
        {
            if (!TransferDependencies.TryGetValue(from, out var set))
            {
                TransferDependencies[from] = set = new HashSet<EntityRef>();
            }

            set.Add(to);
        }

        public void RemoveTemporaryDependency(EntityRef from, EntityRef to)
        {
            if (TransferDependencies.TryGetValue(from, out var set))
            {
                set.Remove(to);
            }
        }

        public void Transfer(TinyEntity entity)
        {
            if (!ValidateEntity(entity))
            {
                return;
            }

            if (!Reentrance.Add(entity))
            {
                return;
            }

            try
            {
                var entityRef = (TinyEntity.Reference) entity;

                var current = GetBindingConfiguration(entity);
                var configuration = current;

                if (null == configuration || DirtyConfigurations.Contains(entityRef))
                {
                    configuration = GenerateBindingConfiguration(entity);
                    DirtyConfigurations.Remove(entityRef);
                }

                if (null != current && !configuration.Equals(current))
                {
                    foreach (var profile in current.ReversedOrderBindings.Except((configuration.ReversedOrderBindings)))
                    {
                        profile.UnloadBindings(entity);
                    }
                }

                EntityToBindingConfiguration[entityRef] = configuration;

                foreach (var component in entity.Components)
                {
                    component.Refresh();
                }

                foreach (var profile in configuration.Bindings)
                {
                    profile.LoadBindings(entity);
                }

                foreach (var profile in configuration.Bindings)
                {
                    profile.Transfer(entity);
                }

                if (TransferDependencies.TryGetValue(entityRef, out var next))
                {
                    foreach (var e in next.ToList().Deref(Registry))
                    {
                        Transfer(e);
                    }
                }

                Reentrance.Remove(entity);
            }
            finally
            {
                UnityEditor.SceneView.RepaintAll();
                Unity.Tiny.Bridge.GameView.RepaintAll();
            }
        }

        #endregion // API

        #region Implementation

        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Init()
        {
            RegisterBindingProfiles();

            foreach (var with in TinyAttributeScanner.GetTypeAttributes<WithComponentAttribute>())
            {
                RegisterTwoWayDependencies(with.Type, with.Attribute.TypeIds, WithComponents);
            }

            foreach (var without in TinyAttributeScanner.GetTypeAttributes<WithoutComponentAttribute>())
            {
                RegisterTwoWayDependencies(without.Type, without.Attribute.TypeIds, WithoutComponents);
            }

            foreach (var dependency in TinyAttributeScanner.GetTypeAttributes<BindingDependencyAttribute>())
            {
                RegisterDependencies(dependency.Type, dependency.Attribute.Types);
            }

            foreach (var profile in AllProfileInstances)
            {
                profile.WithComponents    = WithComponents[profile];
                profile.WithoutComponents = WithoutComponents[profile];
            }

            TinyEditorApplication.OnLoadProject += (project, context) =>
            {
                foreach (var profile in AllProfileInstances)
                {
                    profile.SetContext(context);
                }
            };
        }

        private static bool ValidateEntity(TinyEntity entity)
        {
            return null != entity      &&
                   null != entity.View &&
                           entity.View;
        }

        private static void RegisterBindingProfiles()
        {
            var profileType = typeof(BindingProfile);
            foreach (var type in TinyAttributeScanner.CompiledTypesInEditor)
            {
                if (type.IsAbstract || type.ContainsGenericParameters || !type.IsSubclassOf(profileType))
                {
                    continue;
                }
                RegisterBindingProfileType(type);
            }
        }

        private static void RegisterBindingProfileType(Type type)
        {
            var profile = (BindingProfile)Activator.CreateInstance(type);
            AllProfileInstances.Add(profile);
            BindingTypeToInstance.Add(type, profile);
        }

        private static void RegisterTwoWayDependencies(Type type, TinyId[] ids, TwoWayDependency deps)
        {
            var profile = GetProfileFromType(type);
            if (null == profile)
            {
                return;
            }

            foreach (var id in ids)
            {
                deps.Add(profile, id);
            }
        }

        private static void RegisterDependencies(Type self, Type[] dependentTypes)
        {
            var profile = BindingTypeToInstance[self];
            var dependencies = dependentTypes.Select(type => BindingTypeToInstance[type]).ToList();
            ProfileDependencies.Add(profile, dependencies);
        }

        private BindingConfiguration GetBindingConfiguration(TinyEntity entity)
        {
            BindingConfiguration configuration;
            EntityToBindingConfiguration.TryGetValue((EntityRef)entity, out configuration);
            return configuration;
        }

        private static BindingConfiguration GenerateBindingConfiguration(TinyEntity entity)
        {
            BindingConfiguration config;
            var profilesForEntity = ListPool<BindingProfile>.Get();
            var orderedProfiles = ListPool<BindingProfile>.Get();
            var set = HashSetPool<BindingProfile>.Get();
            var usedProfiles = ListPool<BindingProfile>.Get();

            try
            {
                // Get all the matching profiles
                GetMatchingProfiles(entity, profilesForEntity);

                // In order
                for (var pIndex = 0; pIndex < profilesForEntity.Count; ++pIndex)
                {
                    var profile = profilesForEntity[pIndex];
                    orderedProfiles.AddRange(EnumerateDependencies(profile).Reverse());
                }

                // Use each one only once
                for (var oIndex = 0; oIndex < orderedProfiles.Count; ++oIndex)
                {
                    var oProfile = orderedProfiles[oIndex];
                    if (set.Add(oProfile))
                    {
                        usedProfiles.Add(oProfile);
                    }
                }

                config = new BindingConfiguration(usedProfiles.ToArray());
            }
            finally
            {
                ListPool<BindingProfile>.Release(usedProfiles);
                HashSetPool<BindingProfile>.Release(set);
                ListPool<BindingProfile>.Release(orderedProfiles);
                ListPool<BindingProfile>.Release(profilesForEntity);
            }

            return config;
        }

        private static void GetMatchingProfiles(TinyEntity entity, List<BindingProfile> result)
        {
            var profiles = ListPool<BindingProfile>.Get();
            var set = HashSetPool<BindingProfile>.Get();
            var reduced = ListPool<BindingProfile>.Get();

            try
            {
                GetProfiles(entity.Components, entity, profiles);

                for(var pIndex = 0; pIndex < profiles.Count; ++pIndex)
                {
                    var profile = profiles[pIndex];
                    if (!set.Add(profile))
                    {
                        continue;
                    }

                    var any = false;
                    for (var wIndex = 0; wIndex < profile.WithoutComponents.Count; ++wIndex)
                    {
                        var id = profile.WithoutComponents[wIndex];
                        any |= null != entity.GetComponent(id);
                    }

                    if (!any)
                    {
                        reduced.Add(profile);
                    }
                }

                for (var i = 0; i < reduced.Count; ++i)
                {
                    var profile = reduced[i];
                    var keep = true;
                    foreach (var dependency in GetDependencies(profile))
                    {
                        if (!profiles.Contains(dependency))
                        {
                            keep = false;
                        }
                    }

                    if (keep)
                    {
                        result.Add(profile);
                    }
                }
            }
            finally
            {
                ListPool<BindingProfile>.Release(reduced);
                HashSetPool<BindingProfile>.Release(set);
                ListPool<BindingProfile>.Release(profiles);
            }
        }

        private static void GetProfiles(IList<TinyObject> components, TinyEntity entity, List<BindingProfile> result)
        {
            for (var cIndex = 0; cIndex < components.Count; ++cIndex)
            {
                var component = components[cIndex];

                var profilesList = WithComponents[component.Type.Id];
                for (var pIndex = 0; pIndex < profilesList.Count; ++pIndex)
                {
                    var profile = profilesList[pIndex];
                    var any = false;
                    for (var wIndex = 0; wIndex < profile.WithComponents.Count; ++wIndex)
                    {
                        var id = profile.WithComponents[wIndex];
                        any |= null == entity.GetComponent(id);
                    }

                    if (!any)
                    {
                        result.Add(profile);
                    }
                }
            }
        }

        private static IEnumerable<BindingProfile> EnumerateDependencies(BindingProfile profile)
        {
            yield return profile;

            foreach (var dependence in GetDependencies(profile).SelectMany(EnumerateDependencies))
            {
                yield return dependence;
            }
        }

        private static List<BindingProfile> GetDependencies(BindingProfile profile)
        {
            if (!ProfileDependencies.TryGetValue(profile, out var dependences))
            {
                dependences = new List<BindingProfile>();
            }

            return dependences;
        }

        private static BindingProfile GetProfileFromType(Type type)
        {
            if (!BindingTypeToInstance.TryGetValue(type, out var profile))
            {
                Debug.Log($"{TinyConstants.ApplicationName}: Could not get binding profile from type {type.Name}.");
            }
            return profile;
        }
        #endregion
    }
}

