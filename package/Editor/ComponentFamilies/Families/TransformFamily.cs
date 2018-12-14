
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [ComponentFamily(
         requiredGuids: new []
             {
        CoreGuids.Core2D.TransformNode
                 },
        optionalGuids: new []
        {
            CoreGuids.Core2D.TransformLocalPosition,
            CoreGuids.Core2D.TransformLocalRotation,
            CoreGuids.Core2D.TransformLocalScale,
        }),
     ComponentFamilyHidden(new []
     {
         CoreGuids.Core2D.TransformNode
     }),
    UsedImplicitly]
    internal class TransformFamily : ComponentFamily
    {
        public override string Name => "Transform";

        public TransformFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext) { }

        protected override bool VisitFamilyComponent(ref UIVisitContext<TinyObject> context)
        {
            var editor = CustomEditors.GetEditor(context.Value.Type);
            return editor.Visit(ref context);
        }

        public override void AddRequiredComponent(IEnumerable<TinyEntity> entities)
        {
            foreach (var entity in entities)
            {
                foreach (var all in Definition.All)
                {
                    entity.GetOrAddComponent(all);
                }
            }
        }

        protected override void ResetFamilyValues(List<TinyEntity> targets)
        {
            // We use optional types here because we do not want to reset the transform node.
            foreach (var type in GetOptionalTypes())
            {
                foreach (var entity in targets)
                {
                    var component = entity.GetComponent(type);
                    component.Reset();
                }
            }
        }
    }

}
