using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Tiny.Runtime.Core2D;
using Unity.Tiny.Runtime.UIControls;
using UnityEngine.UI;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.UIControls.Button)]
    [UsedImplicitly]
    internal class ButtonBindings : BindingProfile
    {
        private static readonly Dictionary<TinyEntity.Reference, TinyEntity.Reference> k_TemporaryLink =
            new Dictionary<TinyEntity.Reference, TinyEntity.Reference>();
        
        public override void Transfer(TinyEntity entity)
        {
            var button = entity.GetComponent<TinyButton>();
            var targetRef = button.sprite2DRenderer;
            var target = targetRef.Dereference(Registry);
            
            var selfRef = entity.Ref;
            if (k_TemporaryLink.TryGetValue(entity.Ref, out var otherRef))
            {
                Bindings.RemoveTemporaryDependency(targetRef, selfRef);
                if (!otherRef.Equals(selfRef))
                {
                    Bindings.Transfer(otherRef.Dereference(Registry));
                }
            }

            if (null == target)
            {
                k_TemporaryLink.Remove(selfRef);
            }
            else
            {
                k_TemporaryLink[selfRef] = targetRef;
                Bindings.SetTemporaryDependency(targetRef, selfRef);
            }

            var renderer = target.GetComponent<TinySprite2DRenderer>();
            var image = GetComponent<Image>(target);

            if (!renderer.IsValid || null == image)
            {
                return;
            }
            
            UIControlsBindingsHelper.TransferTransition(entity, renderer, button.transition, image);
        }
    }
}