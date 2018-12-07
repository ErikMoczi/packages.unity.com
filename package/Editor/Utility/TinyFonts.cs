using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyFonts
    {
        #region Properties
        private static Font SansSerifRegular { get; }
        private static Font SansSerifBold { get; }
        private static Font SansSerifItalic { get; }
        private static Font SansSerifBoldItalic { get; }
        
        private static Font SerifRegular { get; }
        private static Font SerifBold { get; }
        private static Font SerifItalic { get; }
        private static Font SerifBoldItalic { get; }
        
        private static Font MonoSpaceRegular { get; }
        private static Font MonoSpaceBold { get; }
        private static Font MonoSpaceItalic { get; }
        private static Font MonoSpaceBoldItalic { get; }
        
        #endregion
        
        #region Private Methods

        private static Font Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Font>(TinyConstants.PackagePath + "/Editor Default Resources/Tiny/fonts/" + name);
        }

        static TinyFonts()
        {
            const string sansSerifRoot = "liberation-sans/";
            SansSerifRegular = Load(sansSerifRoot + "LiberationSans-Regular.ttf");
            SansSerifBold = Load(sansSerifRoot + "LiberationSans-Bold.ttf");
            SansSerifItalic = Load(sansSerifRoot + "LiberationSans-Italic.ttf");
            SansSerifBoldItalic = Load(sansSerifRoot + "LiberationSans-BoldItalic.ttf");
            
            const string serifRoot = "liberation-serif/";
            SerifRegular = Load(serifRoot + "LiberationSerif-Regular.ttf");
            SerifBold = Load(serifRoot + "LiberationSerif-Bold.ttf");
            SerifItalic = Load(serifRoot + "LiberationSerif-Italic.ttf");
            SerifBoldItalic = Load(serifRoot + "LiberationSerif-BoldItalic.ttf");

            const string monospaceRoot = "liberation-mono/";
            MonoSpaceRegular = Load(monospaceRoot + "LiberationMono-Regular.ttf");
            MonoSpaceBold = Load(monospaceRoot + "LiberationMono-Bold.ttf");
            MonoSpaceItalic = Load(monospaceRoot + "LiberationMono-Italic.ttf");
            MonoSpaceBoldItalic = Load(monospaceRoot + "LiberationMono-BoldItalic.ttf");
        }
        #endregion
        
        #region API

        public static Font GetSansSerifFont(bool bold, bool italic)
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
        
        public static Font GetSerifFont(bool bold, bool italic)
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
        
        public static Font GetMonoSpaceFont(bool bold, bool italic)
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