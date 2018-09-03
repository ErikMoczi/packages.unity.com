#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Unity.Properties.Editor.Serialization
{

    public abstract class GenerationBackend
    {
        public void GenerateContainer(PropertyTypeNode c)
        {
            throw new NotImplementedException();
        }

        private void DoGenerateContainer(PropertyTypeNode c)
        {
            ResetInternalGenerationStates();

            OnPropertyContainerGenerationStarted(c);

            {
                if (c.Properties.Count != 0)
                {
                    foreach (var propertyType in c.Properties)
                    {
                        try
                        {
                            OnPropertyGenerationStarted(c, propertyType);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }

                OnGenerateUserHooksForContainer(c);

                // Add inherited properties if it applies
                /*
                var propertyBagElementNames = PropertyBagItemNames;
                if (!string.IsNullOrEmpty(c.OverrideDefaultBaseClass) && dependancyLookupFunc != null)
                {
                    var cachedContainer = dependancyLookupFunc(c.OverrideDefaultBaseClass);
                    if (cachedContainer != null)
                    {
                        propertyBagElementNames = PropertyBagItemNames.Select(n => n).ToList();

                        propertyBagElementNames.AddRange(cachedContainer.GeneratedPropertyFieldNames);
                    }
                }
                */

                OnGeneratePropertyBagForContainer(c);
                OnGenerateConstructorForContainer(c);
                OnGenerateStaticConstructorForContainer(c);

                // @TODO Cleanup
                // Recurse to collect nested container definitions

                foreach (var nestedContainer in c.NestedContainers)
                {
                    if (nestedContainer == null)
                        continue;

                    OnGenerateNestedContainer(c, nestedContainer);
                }

                OnPropertyContainerGenerationCompleted(c);
            }
        }

        public abstract void OnPropertyContainerGenerationStarted(PropertyTypeNode c);
        public abstract void OnPropertyGenerationStarted(PropertyTypeNode container, PropertyTypeNode property);
        public abstract void OnGenerateUserHooksForContainer(PropertyTypeNode container);
        public abstract void OnGeneratePropertyBagForContainer(PropertyTypeNode container);
        public abstract void OnGenerateConstructorForContainer(PropertyTypeNode container);
        public abstract void OnGenerateStaticConstructorForContainer(PropertyTypeNode container);
        public abstract void OnGenerateNestedContainer(PropertyTypeNode container, PropertyTypeNode nestedContainer);
        public abstract void OnPropertyContainerGenerationCompleted(PropertyTypeNode c);

        // @TODO improve way to abstract variablity for various generation stages

        protected abstract class FragmentContext
        {
            public virtual void AddStringFragment() { }
            public virtual void AddIlFragment(ILProcessor ilProcessor) { }
        }

        private void ResetInternalGenerationStates()
        {
            StaticConstructorInitializerFragments = new Dictionary<ConstructorStage, StaticConstructorStagePrePostFragments>();
            ConstructorInitializerFragments = new List<FragmentContext>();
        }

        protected List<FragmentContext> ConstructorInitializerFragments { get; set; }

        protected enum ConstructorStage
        {
            PropertyInitializationStage,
            PropertyFreezeStage,
        };

        protected class StaticConstructorStagePrePostFragments
        {
            public StaticConstructorStagePrePostFragments(ConstructorStage stage)
            {
                Stage = stage;
            }

            public ConstructorStage Stage { get; internal set; }

            public List<FragmentContext> InStageFragments { get; set; } = new List<FragmentContext>();

            public List<FragmentContext> PostStageFragments { get; set; } = new List<FragmentContext>();
        };

        protected Dictionary<ConstructorStage, StaticConstructorStagePrePostFragments> StaticConstructorInitializerFragments { get; set; }

        protected void AddStaticConstructorInStageFragment(ConstructorStage s, FragmentContext f)
        {
            if ( ! StaticConstructorInitializerFragments.ContainsKey(s))
            {
                StaticConstructorInitializerFragments[s] = new StaticConstructorStagePrePostFragments(s);
            }

            StaticConstructorInitializerFragments[s].InStageFragments.Add(f);
        }

        protected void AddStaticConstructorPostStageFragment(ConstructorStage s, FragmentContext f)
        {
            if ( ! StaticConstructorInitializerFragments.ContainsKey(s))
            {
                StaticConstructorInitializerFragments[s] = new StaticConstructorStagePrePostFragments(s);
            }

            StaticConstructorInitializerFragments[s].PostStageFragments.Add(f);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)