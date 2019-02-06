

using Unity.Properties;
using Unity.Tiny.Serialization;

namespace Unity.Tiny
{
    internal static class TinyTypeConversion
    {
        [UnityEditor.InitializeOnLoadMethod]
        private static void Register()
        {
            TypeConversion.Register<string, TinyId>(v => new TinyId(v));
            
            TypeConversion.Register<IPropertyContainer, TinyEntity.Reference>(v =>
            {
                var id = v.GetValue<TinyId>("Id");
                var name = v.GetValue<string>("Name");
                return new TinyEntity.Reference(id, name);
            });
            
            TypeConversion.Register<IPropertyContainer, TinyType.Reference>(v =>
            {
                var id = v.GetValue<TinyId>("Id");
                var name = v.GetValue<string>("Name");
                return new TinyType.Reference(id, name);
            });

            TypeConversion.Register<IPropertyContainer, TinyEntityGroup.Reference>(v =>
            {
                var id = v.GetValue<TinyId>("Id");
                var name = v.GetValue<string>("Name");
                return new TinyEntityGroup.Reference(id, name);
            });
            
            TypeConversion.Register<IPropertyContainer, TinyPrefabInstance.Reference>(v =>
            {
                var id = v.GetValue<TinyId>("Id");
                var name = v.GetValue<string>("Name");
                return new TinyPrefabInstance.Reference(id, name);
            });

            TypeConversion.Register<UnityEngine.Object, UnityEngine.Texture2D>(v => v as UnityEngine.Texture2D);
            TypeConversion.Register<UnityEngine.Object, UnityEngine.Sprite>(v => v as UnityEngine.Sprite);
            TypeConversion.Register<UnityEngine.Object, TMPro.TMP_FontAsset>(v => v as TMPro.TMP_FontAsset);
            TypeConversion.Register<UnityEngine.Object, UnityEngine.TextAsset>(v => v as UnityEngine.TextAsset);
            TypeConversion.Register<UnityEngine.Object, UnityEngine.Tilemaps.TileBase>(v => v as UnityEngine.Tilemaps.TileBase);
            TypeConversion.Register<UnityEngine.Object, UnityEngine.Tilemaps.Tilemap>(v => v as UnityEngine.Tilemaps.Tilemap);

            TypeConversion.Register<IPropertyContainer, UnityEngine.Object>(UnityObjectSerializer.FromObjectHandle);
            TypeConversion.Register<IPropertyContainer, UnityEngine.Texture2D>(v => UnityObjectSerializer.FromObjectHandle(v) as UnityEngine.Texture2D);
            TypeConversion.Register<IPropertyContainer, UnityEngine.Sprite>(v => UnityObjectSerializer.FromObjectHandle(v) as UnityEngine.Sprite);
            TypeConversion.Register<IPropertyContainer, UnityEngine.Tilemaps.TileBase>(v => UnityObjectSerializer.FromObjectHandle(v) as UnityEngine.Tilemaps.TileBase);
            TypeConversion.Register<IPropertyContainer, UnityEngine.Tilemaps.Tilemap>(v => UnityObjectSerializer.FromObjectHandle(v) as UnityEngine.Tilemaps.Tilemap);
            TypeConversion.Register<IPropertyContainer, UnityEngine.AudioClip>(v => UnityObjectSerializer.FromObjectHandle(v) as UnityEngine.AudioClip);
            TypeConversion.Register<IPropertyContainer, UnityEngine.AnimationClip>(v => UnityObjectSerializer.FromObjectHandle(v) as UnityEngine.AnimationClip);
            TypeConversion.Register<IPropertyContainer, TMPro.TMP_FontAsset>(v => UnityObjectSerializer.FromObjectHandle(v) as TMPro.TMP_FontAsset);
            TypeConversion.Register<IPropertyContainer, UnityEngine.TextAsset>(v => UnityObjectSerializer.FromObjectHandle(v) as UnityEngine.TextAsset);

            TypeConversion.Register<IPropertyContainer, TinyAssetExportSettings>(v =>
            {
                var typeId = v.GetValueOrDefault("$TypeId", TinyAssetTypeId.Unknown);

                TinyAssetExportSettings instance = null;
                
                switch (typeId)
                {
                    case TinyAssetTypeId.Unknown:
                        break;
                    case TinyAssetTypeId.Generic:
                        instance = new TinyGenericAssetExportSettings();
                        break;
                    case TinyAssetTypeId.Texture:
                        instance = new TinyTextureSettings();
                        break;
                    case TinyAssetTypeId.AudioClip:
                        instance = new TinyAudioClipSettings();
                        break;
                    case TinyAssetTypeId.AnimationClip:
                        instance = new TinyAnimationClipSettings();
                        break;
                }

                if (null != instance)
                {
                    PropertyContainer.Transfer(v, instance);
                }

                return instance;
            });
            
            TypeConversion.Register<IPropertyContainer, IPropertyModification>(PropertyModificationConverter.Convert);
            
            TypeConversion.Register<IPropertyContainer, TinyList>(v =>
            {
                var type = v.GetValue<TinyType.Reference>("Type");
                var list = new TinyList(null, type);
                PropertyContainer.Transfer(v, list);
                return list; 
            });
            
            TypeConversion.Register<IPropertyContainer, TinyEnum.Reference>(v => new TinyEnum.Reference(
                v.GetValue<TinyType.Reference>("Type"),
                v.GetValue<TinyId>("Id"),
                v.GetValue<string>("Name"),
                v.GetValue<int>("Value")));
        }
    }
}

