
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinySprites
    {
        #region Properties
        
        
        private static Texture2D WhiteTexture { get; set; }
        public static Sprite WhiteSprite { get; set; }
        
        #endregion
        
        #region Private Methods

        private static Sprite Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(TinyConstants.PackagePath + "/Editor Default Resources/" + path);
        }

        static TinySprites()
        {
            WhiteSprite = Load("Tiny/WhiteTexture.png");
        }
        #endregion
    }
}
