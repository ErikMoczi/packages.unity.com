

using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyIcons
    {
        #region Properties
        
        public static Texture2D Export { get; private set; }
        public static Texture2D EntityGroup { get; private set; }
        public static Texture2D ActiveEntityGroup { get; private set; }
        public static Texture2D Variable { get; private set; }
        public static Texture2D Function { get; private set; }
        public static Texture2D Library { get; private set; }
        public static Texture2D Component { get; private set; }
        public static Texture2D Struct { get; private set; }
        public static Texture2D Enum { get; private set; }
        public static Texture2D Add { get; private set; }
        public static Texture2D Expand { get; private set; }
        public static Texture2D Collapse { get; private set; }
        public static Texture2D Visible { get; private set; }
        public static Texture2D Invisible { get; private set; }
        public static Texture2D Array { get; private set; }
        public static Texture2D NonArray { get; private set; }
        public static Texture2D Project { get; private set; }
        public static Texture2D Module { get; private set; }
        public static Texture2D Warning { get; private set; }
        public static Texture2D Trash { get; private set; }
        public static Texture2D System { get; private set; }
        public static Texture2D SeparatorVertical { get; private set; }
        public static Texture2D SeparatorHorizontal { get; private set; }
        public static Texture2D FoldoutOn { get; private set; }
        public static Texture2D FoldoutOff { get; private set; }
        public static Texture2D Locked { get; private set; }
        public static Texture2D Unlocked { get; private set; }
        public static Texture2D X_Icon_8 { get; private set; }
        public static Texture2D X_Icon_16 { get; private set; }
        public static Texture2D X_Icon_32 { get; private set; }
        public static Texture2D X_Icon_64 { get; private set; }
        public static Texture2D TinyQR { get; private set; }
        public static Texture2D PillSprite { get; }

        public static class ScriptableObjects
        {
            public static Texture2D Project { get; }
            public static Texture2D Module { get; }
            public static Texture2D EntityGroup { get; }
            public static Texture2D Prefab { get; }
            public static Texture2D Component { get; }
            public static Texture2D Struct { get; }
            public static Texture2D Enum { get; }
            public static Texture2D TypeScript { get; }
            
            static ScriptableObjects()
            {
                Project = Load($"Tiny/{Theme}/ScriptableObjects/Project.png");
                Module = Load($"Tiny/{Theme}/ScriptableObjects/Module.png");
                EntityGroup = Load($"Tiny/{Theme}/ScriptableObjects/EntityGroup.png");
                Prefab = Load($"Tiny/{Theme}/ScriptableObjects/Prefab.png");
                Component = Load($"Tiny/{Theme}/ScriptableObjects/Component.png");
                Struct = Load($"Tiny/{Theme}/ScriptableObjects/Struct.png");
                Enum = Load($"Tiny/{Theme}/ScriptableObjects/Enum.png");
                TypeScript = Load($"Tiny/{Theme}/ScriptableObjects/TypeScript.png");
            }

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
        }
        
        #endregion
        
        #region Private Methods

        private static Texture2D Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TinyConstants.PackagePath + "/Editor Default Resources/" + path);
        }

        private static string Theme => EditorGUIUtility.isProSkin ? "Dark" : "Light";

        static TinyIcons()
        {
            var proSkin = EditorGUIUtility.isProSkin;
            Export = Load("Tiny/Export.png");
            EntityGroup = Load($"Tiny/EntityGroup_icon{(EditorGUIUtility.isProSkin ? "" : "_personal")}.png");
            ActiveEntityGroup = Load($"Tiny/EntityGroup_icon_active{(EditorGUIUtility.isProSkin ? "" : "_personal")}.png");
            Variable = Load("Tiny/Variable.png");
            Function = Load("Tiny/Function.png");
            Library = Load("Tiny/Library.png");
            Component = Load("Tiny/Component.png");
            Struct = Load("Tiny/Class.png");
            Enum = Load("Tiny/Enum.png");
            Add = Load("Tiny/Add.png");
            Expand = Load("Tiny/Expand.png");
            Collapse = Load("Tiny/Collapse.png");
            Visible = Load($"Tiny/{Theme}/visibleInInspector@2x.png");
            Invisible = Load($"Tiny/{Theme}/hideInInspector@2x.png");
            Project = Load("Tiny/Project.png");
            Module = Load("Tiny/Module.png");
            Warning = Load("Tiny/Warning.psd");
            Trash = Load("Tiny/Trash.png");
            Array = Load($"Tiny/{Theme}/isArray@2x.png");
            NonArray = Load($"Tiny/{Theme}/isNotArray@2x.png");
            System = Load("Tiny/System.png");
            SeparatorVertical = Load("Tiny/SeparatorHorizontal.png");
            SeparatorHorizontal = Load("Tiny/SeparatorVertical.png");
            FoldoutOn = Load("Tiny/Foldout_On.png");
            FoldoutOff = Load("Tiny/Foldout_Off.png");
            Locked = Load("Tiny/Locked.png");
            Unlocked = Load("Tiny/Unlocked.png");
            X_Icon_8 = Load($"Tiny/x_icon_8{(proSkin? "":"_personal")}.png");
            X_Icon_16 = Load($"Tiny/x_icon_16{(proSkin? "":"_personal")}.png");
            X_Icon_32 = Load($"Tiny/x_icon_32{(proSkin? "":"_personal")}.png");
            X_Icon_64 = Load($"Tiny/x_icon_64{(proSkin? "":"_personal")}.png");
            TinyQR = Load("Tiny/TinyQR.png");
            PillSprite = Load($"Tiny/{Theme}/pillSprite.png");        
        }
        #endregion
    }
}

