using UnityEditor;
using UnityEngine;
using TMPro;

namespace Unity.Tiny
{
    internal static class TinyFonts
    {
        #region Properties
        private static TMP_FontAsset SansSerifRegular { get; }
        private static TMP_FontAsset SansSerifBold { get; }
        private static TMP_FontAsset SansSerifItalic { get; }
        private static TMP_FontAsset SansSerifBoldItalic { get; }
        
        private static TMP_FontAsset SerifRegular { get; }
        private static TMP_FontAsset SerifBold { get; }
        private static TMP_FontAsset SerifItalic { get; }
        private static TMP_FontAsset SerifBoldItalic { get; }
        
        private static TMP_FontAsset MonoSpaceRegular { get; }
        private static TMP_FontAsset MonoSpaceBold { get; }
        private static TMP_FontAsset MonoSpaceItalic { get; }
        private static TMP_FontAsset MonoSpaceBoldItalic { get; }
        
        #endregion
        
        #region Private Methods

        private static TMP_FontAsset Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TinyConstants.PackagePath + "/Editor Default Resources/Tiny/fonts/" + name);
        }

        static TinyFonts()
        {
            const string sansSerifRoot = "liberation-sans/";
            SansSerifRegular = Load(sansSerifRoot + "LiberationSans-Regular.asset");
            SansSerifBold = Load(sansSerifRoot + "LiberationSans-Bold.asset");
            SansSerifItalic = Load(sansSerifRoot + "LiberationSans-Italic.asset");
            SansSerifBoldItalic = Load(sansSerifRoot + "LiberationSans-BoldItalic.asset");
            
            const string serifRoot = "liberation-serif/";
            SerifRegular = Load(serifRoot + "LiberationSerif-Regular.asset");
            SerifBold = Load(serifRoot + "LiberationSerif-Bold.asset");
            SerifItalic = Load(serifRoot + "LiberationSerif-Italic.asset");
            SerifBoldItalic = Load(serifRoot + "LiberationSerif-BoldItalic.asset");

            const string monospaceRoot = "liberation-mono/";
            MonoSpaceRegular = Load(monospaceRoot + "LiberationMono-Regular.asset");
            MonoSpaceBold = Load(monospaceRoot + "LiberationMono-Bold.asset");
            MonoSpaceItalic = Load(monospaceRoot + "LiberationMono-Italic.asset");
            MonoSpaceBoldItalic = Load(monospaceRoot + "LiberationMono-BoldItalic.asset");
        }
        #endregion
        
        #region API

        public static TMP_FontAsset GetSansSerifFont(bool bold, bool italic)
        {
            if (bold && italic)
            {
                return SansSerifBoldItalic;
            }

            if (bold)
            {
                return SansSerifBold;
            }

            if (italic)
            {
                return SansSerifItalic;
            }

            return SansSerifRegular;
        }
        
        public static TMP_FontAsset GetSerifFont(bool bold, bool italic)
        {
            if (bold && italic)
            {
                return SerifBoldItalic;
            }

            if (bold)
            {
                return SerifBold;
            }

            if (italic)
            {
                return SerifItalic;
            }

            return SerifRegular;
        }
        
        public static TMPro.TMP_FontAsset GetMonoSpaceFont(bool bold, bool italic)
        {
            if (bold && italic)
            {
                return MonoSpaceBoldItalic;
            }

            if (bold)
            {
                return MonoSpaceBold;
            }

            if (italic)
            {
                return MonoSpaceItalic;
            }

            return MonoSpaceRegular;
        }
        
        #endregion
    }
}