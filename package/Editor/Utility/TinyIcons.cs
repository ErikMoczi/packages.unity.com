using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyIcons
    {
        private const string KIconsDirectory = TinyConstants.PackagePath + "/Editor Default Resources/Icons";

        #region Properties
        
        public static Texture2D EntityGroup { get; private set; }
        public static Texture2D Component { get; private set; }
        public static Texture2D Struct { get; private set; }
        public static Texture2D Enum { get; private set; }
        public static Texture2D Visible { get; private set; }
        public static Texture2D Invisible { get; private set; }
        public static Texture2D Array { get; private set; }
        public static Texture2D NonArray { get; private set; }
        public static Texture2D Project { get; private set; }
        public static Texture2D Module { get; private set; }
        public static Texture2D System { get; private set; }
        public static Texture2D PillSprite { get; private set; }
        public static Texture2D EntityGroupActive { get; private set; }
        public static Texture2D Remove { get; private set; }
        public static Texture2D Prefab { get; private set; }
        public static Texture2D Entity { get; private set; }
        public static Texture2D TypeScript { get; private set; }

        public static Texture2D GetIconForTypeCode(TinyTypeCode typeCode)
        {
            switch (typeCode)
            {
                case TinyTypeCode.Configuration:
                case TinyTypeCode.Component:
                    return Component;
                
                case TinyTypeCode.Struct:
                    return Struct;
                
                case TinyTypeCode.Enum:
                    return Enum;
            }

            return null;
        }
        
        #endregion
        
        #region Private Methods

        /// <summary>
        /// Workaround for `EditorGUIUtility.LoadIcon` not working with packages. This can be removed once it does
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Texture2D LoadIcon(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (EditorGUIUtility.isProSkin)
            {
                name = "d_" + name;
            }

            // Try to use high DPI if possible
            if (GUIUtilityBridge.pixelsPerPoint > 1.0)
            {
                var texture = LoadIconTexture($"{KIconsDirectory}/{name}@2x.png");
                if (null != texture)
                {
                    return texture;
                }
            }
            
            // Fallback to low DPI if we couldn't find the high res or we are on a low res screen
            return LoadIconTexture($"{KIconsDirectory}/{name}.png");
        }

        private static Texture2D LoadIconTexture(string path)
        {
            var texture = (Texture2D) AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

            if (texture != null && 
                !Mathf.Approximately(texture.GetPixelsPerPoint(), (float) GUIUtilityBridge.pixelsPerPoint) &&
                !Mathf.Approximately((float) GUIUtilityBridge.pixelsPerPoint % 1f, 0.0f))
            {
                texture.filterMode = FilterMode.Bilinear;
            }

            return texture;
        }
        
        private static void LoadIcons()
        {
            EntityGroup = LoadIcon("entityGroup");
            Component = LoadIcon("tinyComponent");
            Struct = LoadIcon("tinyStruct");
            Enum = LoadIcon("tinyEnum");
            Visible = LoadIcon("visibleInInspector");
            Invisible = LoadIcon("hideInInspector");
            Array = LoadIcon("isArray");
            NonArray = LoadIcon("isNotArray");
            Project = LoadIcon("tinyProject");
            Module = LoadIcon("tinyModule");
            System = LoadIcon("system");
            PillSprite = LoadIcon("pillSprite");   
            EntityGroupActive = LoadIcon("entityGroupActive");
            Remove = LoadIcon("remove"); 
            // Use the built in unity icon until we have the tinyPrefabIcon
            Prefab = EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
            Entity = LoadIcon("entity");
            TypeScript = LoadIcon("typeScript");   
        }
        
        static TinyIcons()
        {
            LoadIcons();   
        }
        
        #endregion
    }
}

