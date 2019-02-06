using System;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace TMPro
{
    [Serializable]
    public class TMP_FontAsset : TMP_Asset
    {
        /// <summary>
        /// The version of the font asset class.
        /// Version 1.1.0 adds support for the new TextCore.FontEngine and Dynamic SDF system.
        /// </summary>
        public string version
        {
            get { return m_Version; }
            internal set { m_Version = value; }
        }
        [SerializeField]
        private string m_Version;

        /// <summary>
        /// This field is set when the font asset is first created.
        /// </summary>
        [SerializeField]
        internal string m_SourceFontFileGUID;
        
        #if UNITY_EDITOR
        /// <summary>
        /// Persistent reference to the source font file maintained in the editor.
        /// </summary>
        [SerializeField]
        internal Font m_SourceFontFile_EditorRef;
        #endif

        /// <summary>
        /// Source font file when atlas population mode is set to dynamic. Null when the atlas population mode is set to static. 
        /// </summary>
        public Font sourceFontFile
        {
            get { return m_SourceFontFile; }
        }
        [SerializeField]
        internal Font m_SourceFontFile;


        public enum AtlasPopulationMode
        {
            Static  = 0x0,
            Dynamic = 0x1,
        }


        public AtlasPopulationMode atlasPopulationMode
        {
            get { return m_AtlasPopulationMode; }

            #if UNITY_EDITOR
            set
            {
                m_AtlasPopulationMode = value;

                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    m_SourceFontFile = null;
                else if (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic)
                    m_SourceFontFile = m_SourceFontFile_EditorRef;
            }
            #endif
        }
        [SerializeField]
        private AtlasPopulationMode m_AtlasPopulationMode;


        /// <summary>
        /// Information about the font face.
        /// </summary>
        public FaceInfo faceInfo
        {
            get { return m_FaceInfo; }
            internal set { m_FaceInfo = value; }
        }
        [SerializeField]
        private FaceInfo m_FaceInfo;


        /// <summary>
        /// List of glyphs contained in the font asset.
        /// </summary>
        public List<Glyph> glyphTable
        {
            get { return m_GlyphTable; }
            internal set { m_GlyphTable = value; }
        }
        [SerializeField]
        private List<Glyph> m_GlyphTable = new List<Glyph>();


        /// <summary>
        /// Dictionary used to lookup glyphs contained in the font asset by their index.
        /// </summary>
        public Dictionary<uint, Glyph> glyphLookupTable
        {
            get
            {
                if (m_GlyphLookupDictionary == null)
                    ReadFontAssetDefinition();

                return m_GlyphLookupDictionary;
            }
        }
        private Dictionary<uint, Glyph> m_GlyphLookupDictionary;


        /// <summary>
        /// List containing the characters of the given font asset.
        /// </summary>
        public List<TMP_Character> characterTable
        {
            get { return m_CharacterTable; }
            internal set { m_CharacterTable = value; }
        }
        [SerializeField]
        private List<TMP_Character> m_CharacterTable = new List<TMP_Character>();

        /// <summary>
        /// Dictionary used to lookup characters contained in the font asset by their unicode values.
        /// </summary>
        public Dictionary<uint, TMP_Character> characterLookupTable
        {
            get
            {
                if (m_CharacterLookupDictionary == null)
                    ReadFontAssetDefinition();


                return m_CharacterLookupDictionary;
            }
        }
        private Dictionary<uint, TMP_Character> m_CharacterLookupDictionary;


        /// <summary>
        /// The font atlas used by this font asset.
        /// This is always the texture at index [0] of the fontAtlasTextures.
        /// </summary>
        public Texture2D atlasTexture
        {
            get
            {
                if (m_AtlasTexture == null)
                {
                    m_AtlasTexture = atlasTextures[0];
                }

                return m_AtlasTexture;
            }
        }
        private Texture2D m_AtlasTexture;

        /// <summary>
        /// Array of atlas textures that contain the glyphs used by this font asset.
        /// </summary>
        public Texture2D[] atlasTextures
        {
            get
            {
                if (m_AtlasTextures == null)
                {
                    //
                }

                return m_AtlasTextures;
            }

            set
            {
                m_AtlasTextures = value;
            }
        }
        [SerializeField]
        private Texture2D[] m_AtlasTextures;

        /// <summary>
        /// Index of the font atlas texture that still has available space to add new glyphs.
        /// </summary>
        [SerializeField]
        internal int m_AtlasTextureIndex;

        /// <summary>
        /// List of spaces occupied by glyphs in a given texture.
        /// </summary>
        internal List<GlyphRect> usedGlyphRects
        {
            get { return m_UsedGlyphRects; }
            set { m_UsedGlyphRects = value; }
        }
        [SerializeField]
        public List<GlyphRect> m_UsedGlyphRects;

        /// <summary>
        /// List of spaces available in a given texture to add new glyphs.
        /// </summary>
        internal List<GlyphRect> freeGlyphRects
        {
            get { return m_FreeGlyphRects; }
            set { m_FreeGlyphRects = value; }
        }
        [SerializeField]
        public List<GlyphRect> m_FreeGlyphRects;

        /// <summary>
        /// The general information about the font.
        /// This property and FaceInfo_Legacy type are not longer used in version 1.1.0 of the font asset.
        /// </summary>
        internal FaceInfo_Legacy fontInfo
        {
            get { return m_fontInfo; }
        }

        [SerializeField]
        private FaceInfo_Legacy m_fontInfo = null;

        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        public Texture2D atlas; // Should add a property to make this read-only.

        /// <summary>
        /// The width of the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasWidth
        {
            get { return m_AtlasWidth; }
            internal set { m_AtlasWidth = value; }
        }
        [SerializeField]
        private int m_AtlasWidth;

        /// <summary>
        /// The height of the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasHeight
        {
            get { return m_AtlasHeight; }
            internal set { m_AtlasHeight = value; }
        }
        [SerializeField]
        private int m_AtlasHeight;

        /// <summary>
        /// The padding used between glyphs contained in the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasPadding
        {
            get { return m_AtlasPadding; }
            internal set { m_AtlasPadding = value; }
        }
        [SerializeField]
        private int m_AtlasPadding;

        public GlyphRenderMode atlasRenderMode
        {
            get { return m_AtlasRenderMode; }
            internal set { m_AtlasRenderMode = value; }
        }
        [SerializeField]
        private GlyphRenderMode m_AtlasRenderMode;

        // Glyph Info
        [SerializeField]
        private List<TMP_Glyph> m_glyphInfoList;

        /// <summary>
        /// Dictionary containing the kerning data
        /// </summary>
        public Dictionary<int, KerningPair> kerningLookupDictionary
        {
            get { return m_KerningLookupDictionary; }
        }
        private Dictionary<int, KerningPair> m_KerningLookupDictionary;

        /// <summary>
        /// 
        /// </summary>
        public KerningTable kerningTable
        {
            get { return m_KerningTable; }
            internal set { m_KerningTable = value; }
        }

        [SerializeField]
        private KerningTable m_KerningTable = new KerningTable();

        [SerializeField]
#pragma warning disable 0169 // Property is used to create an empty Kerning Pair in the editor.
        private KerningPair m_kerningPair;  // Used for creating a new kerning pair in Editor Panel.

        /// <summary>
        /// List containing the Fallback font assets for this font.
        /// </summary>
        public List<TMP_FontAsset> fallbackFontAssetTable
        {
            get { return m_FallbackFontAssetTable; }
            set { m_FallbackFontAssetTable = value; }
        }
        [SerializeField]
        public List<TMP_FontAsset> m_FallbackFontAssetTable;

        /// <summary>
        /// The settings used in the Font Asset Creator when this font asset was created or edited.
        /// </summary>
        public FontAssetCreationSettings creationSettings
        {
            get { return m_CreationSettings; }
            set { m_CreationSettings = value; }
        }
        [SerializeField]
        internal FontAssetCreationSettings m_CreationSettings;

        /// <summary>
        /// Array containing font assets to be used as alternative typefaces for the various potential font weights of this font asset.
        /// </summary>
        public TMP_FontWeightPair[] fontWeightTable
        {
            get { return m_FontWeightTable; }
            internal set { m_FontWeightTable = value; }
        }
        [SerializeField]
        private TMP_FontWeightPair[] m_FontWeightTable = new TMP_FontWeightPair[10];

        // FONT WEIGHTS
        /// <summary>
        /// Font weights used by font asset prior to version 1.1.0.
        /// This is legacy and will be removed at some point in the future.
        /// </summary>
        [SerializeField]
        private TMP_FontWeightPair[] fontWeights = null;

        //private int[] m_characterSet; // Array containing all the characters in this font asset.

        public float normalStyle = 0;
        public float normalSpacingOffset = 0;

        public float boldStyle = 0.75f;
        public float boldSpacing = 7f;
        public byte italicStyle = 35;
        public byte tabSize = 10;

        private byte m_oldTabSize;

        /// <summary>
        /// Create new Font Asset instance.
        /// </summary>
        /// <param name="font"></param>
        /// <param name="samplingPointSize"></param>
        /// <param name="atlasPadding"></param>
        /// <param name="renderMode"></param>
        /// <param name="atlasWidth"></param>
        /// <param name="atlasHeight"></param>
        /// <param name="atlasPopulationMode"></param>
        /// <returns></returns>
        public TMP_FontAsset Init(Font font, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic)
        {
            m_Version = "1.1.0";

            // Set face information
            FontEngine.InitializeFontEngine();
            FontEngine.LoadFontFace(font, samplingPointSize);
            m_FaceInfo = FontEngine.GetFaceInfo();

            // Set font reference and GUID
            if (atlasPopulationMode == AtlasPopulationMode.Dynamic)
                m_SourceFontFile = font;

            // Set persistent reference to source font file in the Editor only.
            #if UNITY_EDITOR
            m_SourceFontFile_EditorRef = font;
            #endif

            m_AtlasPopulationMode = atlasPopulationMode;

            m_AtlasWidth = atlasWidth;
            m_AtlasHeight = atlasHeight;
            m_AtlasPadding = atlasPadding;
            m_AtlasRenderMode = renderMode;

            // Initialize array for the font atlas textures.
            m_AtlasTextures = new Texture2D[1];

            // Create and add font atlas texture
            Texture2D texture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false);
            Color32[] colors = new Color32[atlasWidth * atlasHeight];
            texture.SetPixels32(colors);
            texture.Apply();

            //texture.name = assetName + " Atlas";
            m_AtlasTextures[0] = texture;

            // Add free rectangle of the size of the texture.
            int packingModifier;
            if (((GlyphRasterModes)atlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                packingModifier = 0;

                Shader shader = Shader.Find("TextMeshPro/Bitmap");
                Material tmp_material = new Material(shader);

                //tmp_material.name = texture.name + " Material";
                tmp_material.SetTexture(ShaderUtilities.ID_MainTex, texture);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);

                material = tmp_material;
            }
            else
            {
                packingModifier = 1;

                Shader shader = Shader.Find("TextMeshPro/Distance Field");
                Material tmp_material = new Material(shader);

                //tmp_material.name = texture.name + " Material";
                tmp_material.SetTexture(ShaderUtilities.ID_MainTex, texture);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);

                tmp_material.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);

                tmp_material.SetFloat(ShaderUtilities.ID_WeightNormal, normalStyle);
                tmp_material.SetFloat(ShaderUtilities.ID_WeightBold, boldStyle);

                material = tmp_material;
            }

            m_FreeGlyphRects = new List<GlyphRect>() { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) };
            m_UsedGlyphRects = new List<GlyphRect>();

            return this;
        }

        void OnEnable()
        {
            //Debug.Log("TMP Font Asset [" + this.name + "] with Version #" + m_Version + " has been enabled!");

            // Check version number of font asset to see if it needs to be upgraded.
            if (this.material != null && (m_Version == null || m_Version == string.Empty))
                UpgradeFontAsset();
        }


        void OnDisable()
        {
            //Debug.Log("TextMeshPro Font Asset [" + this.name + "] has been disabled!");
        }


        #if UNITY_EDITOR
        /// <summary>
        /// 
        /// </summary>
        void OnValidate()
        {
            //if (m_oldTabSize != tabSize)
            //{
            //    m_oldTabSize = tabSize;
            //    ReadFontAssetDefinition();
            //}

            // Handle changes to atlas population mode
            //if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
            //    m_SourceFontFile = null;
            //else
            //{
            //    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(m_SourceFontFileGUID);

            //    if (path != string.Empty)
            //        m_SourceFontFile = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(path);
            //}
        }
        #endif


        /// <summary>
        /// 
        /// </summary>
        /// <param name="kerningTable"></param>
        //public void AddKerningInfo(KerningTable kerningTable)
        //{
        //    m_KerningTable = kerningTable;
        //}


        /// <summary>
        /// Read the various data tables of the font asset to populate its different dictionaries to allow for faster lookup of related font asset data.
        /// </summary>
        internal void InitializeDictionaryLookupTables()
        {
            //Debug.Log("Reading [" + this.name + "] font definition.");

            // Create new instance of the character lookup dictionary or clear the existing one.
            if (m_CharacterLookupDictionary == null)
                m_CharacterLookupDictionary = new Dictionary<uint, TMP_Character>();
            else
                m_CharacterLookupDictionary.Clear();

            // Add the characters contained in the character table into the dictionary for faster lookup.
            TMP_Character character = null;
            for (int i = 0; i < m_CharacterTable.Count; i++)
            {
                uint unicode = m_CharacterTable[i].unicode;

                if (m_CharacterLookupDictionary.TryGetValue(unicode, out character) == false)
                    m_CharacterLookupDictionary.Add(unicode, m_CharacterTable[i]);
            }

            // Create new instance of the glyph lookup dictionary or clear the existing one.
            if (m_GlyphLookupDictionary == null)
                m_GlyphLookupDictionary = new Dictionary<uint, Glyph>();
            else
                m_GlyphLookupDictionary.Clear();

            // Add the characters contained in the character table into the dictionary for faster lookup.
            Glyph glyph = null;
            for (int i = 0; i < m_GlyphTable.Count; i++)
            {
                uint index = m_GlyphTable[i].index;

                if (m_GlyphLookupDictionary.TryGetValue(index, out glyph) == false)
                    m_GlyphLookupDictionary.Add(index, m_GlyphTable[i]);
            }

            // Read Font Features which will include kerning data.
            // TODO

            // Read Kerning pairs and update Kerning pair dictionary for faster lookup.
            // TODO
        }


        /// <summary>
        /// 
        /// </summary>
        public void ReadFontAssetDefinition()
        {
            //Debug.Log("Reading Font Definition for " + this.name + ".");

            // Initialize lookup tables for characters and glyphs.
            InitializeDictionaryLookupTables();

            /*
            // Add Character (10) LineFeed, (13) Carriage Return & Space (32) to Dictionary if they don't exists.
            if (m_CharacterLookupDictionary.ContainsKey(32) == false)
            {
                //Debug.Log("Adding Character 32 (Space) to Dictionary for Font (" + m_fontInfo.Name + ").");
                Glyph glyph = new Glyph (
                    0,
                    new GlyphMetrics((int)(m_FaceInfo.ascentline / 5), (int)(m_FaceInfo.ascentline - m_FaceInfo.descender), 0, (int)m_FaceInfo.ascentline, (int)(m_FaceInfo.pointSize / 4)),
                    new GlyphRect(0, 0, 0, 0),
                    1,
                    0);

                m_CharacterLookupDictionary.Add(32, new Character(32, glyph));
            }

            Glyph space = m_CharacterLookupDictionary[32].glyph;

            // Add Non-Breaking Space (160)
            if (m_CharacterLookupDictionary.ContainsKey(160) == false)
                m_CharacterLookupDictionary.Add(160, new Character(160, space));

            // Add Zero Width Space (8203)
            if (!m_CharacterLookupDictionary.ContainsKey(8203))
            {
                Glyph glyph = new Glyph(space);
                glyph.metrics.width = 0;
                glyph.metrics.horizontalAdvance = 0;
                m_CharacterLookupDictionary.Add(8203, new Character(8203, glyph));
            }

            //Add Zero Width no-break space (8288)
            if (!m_CharacterLookupDictionary.ContainsKey(8288))
            {
                Glyph glyph = new Glyph(space);
                glyph.metrics.width = 0;
                glyph.metrics.horizontalAdvance = 0;
                m_CharacterLookupDictionary.Add(8288, new Character(8288, glyph));
            }
            */

            // Add Linefeed LF char(10) and Carriage Return CR char(13)
            if (m_CharacterLookupDictionary.ContainsKey(10) == false)
            {
                Glyph glyph = new Glyph(0, new GlyphMetrics(10, 0, 0, 0, 0), GlyphRect.zero, 1.0f, 0);
                m_CharacterLookupDictionary.Add(10, new TMP_Character(10, glyph));

                if (!m_CharacterLookupDictionary.ContainsKey(13))
                    m_CharacterLookupDictionary.Add(13, new TMP_Character(13, glyph));
            }

            // Add Tab char(9) to Dictionary.
            if (m_CharacterLookupDictionary.ContainsKey(9) == false)
            {
                Glyph glyph = new Glyph(0, new GlyphMetrics(0, 0, 0, 0, m_FaceInfo.tabWidth * tabSize), GlyphRect.zero, 1.0f, 0);
                m_CharacterLookupDictionary.Add(9, new TMP_Character(9, glyph));
            }

            // Set Cap Height
            if (m_FaceInfo.capLine == 0 && m_CharacterLookupDictionary.ContainsKey(72))
                m_FaceInfo.capLine = m_CharacterLookupDictionary[72].glyph.metrics.horizontalBearingY;

            // Adjust Font Scale for compatibility reasons
            if (m_FaceInfo.scale == 0)
                m_FaceInfo.scale = 1.0f;

            // Set Strikethrough Offset (if needed)
            if (m_FaceInfo.strikethroughOffset == 0)
                m_FaceInfo.strikethroughOffset = m_FaceInfo.capLine / 2.5f;

            // Set Padding value for legacy font assets.
            if (m_AtlasPadding == 0)
            {
                if (material.HasProperty(ShaderUtilities.ID_GradientScale))
                    m_AtlasPadding = (int)material.GetFloat(ShaderUtilities.ID_GradientScale) - 1;
            }

            // Populate Dictionary with Kerning Information
            m_KerningLookupDictionary = new Dictionary<int, KerningPair>();
            List<KerningPair> pairs = m_KerningTable.kerningPairs;

            //Debug.Log(m_fontInfo.Name + " has " + pairs.Count +  " Kerning Pairs.");
            for (int i = 0; i < pairs.Count; i++)
            {
                KerningPair pair = pairs[i];

                // Convert legacy kerning data
                if (pair.xOffset != 0)
                    pairs[i].ConvertLegacyKerningData();

                KerningPairKey uniqueKey = new KerningPairKey(pair.firstGlyph, pair.secondGlyph);

                if (m_KerningLookupDictionary.ContainsKey((int)uniqueKey.key) == false)
                {
                    m_KerningLookupDictionary.Add((int)uniqueKey.key, pair);
                }
                else
                {
                    if (!TMP_Settings.warningsDisabled)
                        Debug.LogWarning("Kerning Key for [" + uniqueKey.ascii_Left + "] and [" + uniqueKey.ascii_Right + "] already exists.");
                }
            }

            // Compute Hashcode for the font asset name
            hashCode = TMP_TextUtilities.GetSimpleHashCode(this.name);

            // Compute Hashcode for the material name
            materialHashCode = TMP_TextUtilities.GetSimpleHashCode(material.name);

        }


        /// <summary>
        /// Function to sort the list of glyphs.
        /// </summary>
        public void SortGlyphs()
        {
            if (m_glyphInfoList == null || m_glyphInfoList.Count == 0) return;

            m_glyphInfoList = m_glyphInfoList.OrderBy(item => item.id).ToList();
        }

        /// <summary>
        /// Sort the Character table by Unicode values.
        /// </summary>
        internal void SortCharacterTable()
        {
            if (m_CharacterTable != null && m_CharacterTable.Count > 0)
                m_CharacterTable = m_CharacterTable.OrderBy(c => c.unicode).ToList();
        }

        /// <summary>
        /// Sort the Glyph table by index values.
        /// </summary>
        internal void SortGlyphTable()
        {
            if (m_GlyphTable != null && m_GlyphTable.Count > 0)
                m_GlyphTable = m_GlyphTable.OrderBy(c => c.index).ToList();
        }

        /// <summary>
        /// Sort both glyph and character tables.
        /// </summary>
        internal void SortGlyphAndCharacterTables()
        {
            SortGlyphTable();
            SortCharacterTable();
        }


        /// <summary>
        /// Function to check if a certain character exists in the font asset.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool HasCharacter(int character)
        {
            if (m_CharacterLookupDictionary == null)
                return false;

            if (m_CharacterLookupDictionary.ContainsKey((uint)character))
                return true;

            return false;
        }


        /// <summary>
        /// Function to check if a certain character exists in the font asset.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool HasCharacter(char character)
        {
            if (m_CharacterLookupDictionary == null)
                return false;

            if (m_CharacterLookupDictionary.ContainsKey(character))
                return true;

            return false;
        }


        /// <summary>
        /// Function to check if a character is contained in the font asset with the option to also check through fallback font assets.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <returns></returns>
        public bool HasCharacter(char character, bool searchFallbacks)
        {
            // Read font asset definition if it hasn't already been done.
            if (m_CharacterLookupDictionary == null)
            {
                ReadFontAssetDefinition();

                if (m_CharacterLookupDictionary == null)
                    return false;
            }

            // Check font asset
            if (m_CharacterLookupDictionary.ContainsKey(character))
                return true;

            if (searchFallbacks)
            {
                // Check font asset fallbacks
                if (fallbackFontAssetTable != null && fallbackFontAssetTable.Count > 0)
                {
                    for (int i = 0; i < fallbackFontAssetTable.Count && fallbackFontAssetTable[i] != null; i++)
                    {
                        if (fallbackFontAssetTable[i].HasCharacter_Internal(character, searchFallbacks))
                            return true;
                    }
                }

                // Check general fallback font assets.
                if (TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                {
                    for (int i = 0; i < TMP_Settings.fallbackFontAssets.Count && TMP_Settings.fallbackFontAssets[i] != null; i++)
                    {
                        if (TMP_Settings.fallbackFontAssets[i].m_CharacterLookupDictionary == null)
                            TMP_Settings.fallbackFontAssets[i].ReadFontAssetDefinition();

                        if (TMP_Settings.fallbackFontAssets[i].m_CharacterLookupDictionary != null && TMP_Settings.fallbackFontAssets[i].HasCharacter_Internal(character, searchFallbacks))
                            return true;
                    }
                }

                // Check TMP Settings Default Font Asset
                if (TMP_Settings.defaultFontAsset != null)
                {
                    if (TMP_Settings.defaultFontAsset.m_CharacterLookupDictionary == null)
                        TMP_Settings.defaultFontAsset.ReadFontAssetDefinition();

                    if (TMP_Settings.defaultFontAsset.m_CharacterLookupDictionary != null && TMP_Settings.defaultFontAsset.HasCharacter_Internal(character, searchFallbacks))
                        return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Function to check if a character is contained in a font asset with the option to also check through fallback font assets.
        /// This private implementation does not search the fallback font asset in the TMP Settings file.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <returns></returns>
        bool HasCharacter_Internal(char character, bool searchFallbacks)
        {
            // Read font asset definition if it hasn't already been done.
            if (m_CharacterLookupDictionary == null)
            {
                ReadFontAssetDefinition();

                if (m_CharacterLookupDictionary == null)
                    return false;
            }

            // Check font asset
            if (m_CharacterLookupDictionary.ContainsKey(character))
                return true;

            if (searchFallbacks)
            {
                // Check Font Asset Fallback fonts.
                if (fallbackFontAssetTable != null && fallbackFontAssetTable.Count > 0)
                {
                    for (int i = 0; i < fallbackFontAssetTable.Count && fallbackFontAssetTable[i] != null; i++)
                    {
                        if (fallbackFontAssetTable[i].HasCharacter_Internal(character, searchFallbacks))
                            return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Function to check if certain characters exists in the font asset. Function returns a list of missing characters.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool HasCharacters(string text, out List<char> missingCharacters)
        {
            if (m_CharacterLookupDictionary == null)
            {
                missingCharacters = null;
                return false;
            }

            missingCharacters = new List<char>();

            for (int i = 0; i < text.Length; i++)
            {
                if (!m_CharacterLookupDictionary.ContainsKey(text[i]))
                    missingCharacters.Add(text[i]);
            }

            if (missingCharacters.Count == 0)
                return true;

            return false;
        }


        /// <summary>
        /// Function to check if certain characters exists in the font asset. Function returns false if any characters are missing.
        /// </summary>
        /// <param name="text">String containing the characters to check</param>
        /// <returns></returns>
        public bool HasCharacters(string text)
        {
            if (m_CharacterLookupDictionary == null)
                return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (!m_CharacterLookupDictionary.ContainsKey(text[i]))
                    return false;
            }

            return true;
        }


        /// <summary>
        /// Function to extract all the characters from a font asset.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static string GetCharacters(TMP_FontAsset fontAsset)
        {
            string characters = string.Empty;

            for (int i = 0; i < fontAsset.characterTable.Count; i++)
            {
                characters += (char)fontAsset.characterTable[i].unicode;
            }

            return characters;
        }


        /// <summary>
        /// Function which returns an array that contains all the characters from a font asset.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static int[] GetCharactersArray(TMP_FontAsset fontAsset)
        {
            int[] characters = new int[fontAsset.characterTable.Count];

            for (int i = 0; i < fontAsset.characterTable.Count; i++)
            {
                characters[i] = (int)fontAsset.characterTable[i].unicode;
            }

            return characters;
        }


        // ================================================================================
        // Properties and functions related to character and glyph additions as well as
        // tacking glyphs that need to be added to various font asset atlas textures.
        // ================================================================================

        /// <summary>
        /// Determines if the font asset is already registered to be updated.
        /// </summary>
        //private bool m_IsAlreadyRegisteredForUpdate;

        /// <summary>
        /// List of glyphs that need to be added / packed in atlas texture.
        /// </summary>
        private List<Glyph> m_GlyphsToPack = new List<Glyph>();

        /// <summary>
        /// List of glyphs that have been packed in the atlas texture and ready to be rendered.
        /// </summary>
        private List<Glyph> m_GlyphsPacked = new List<Glyph>();

        /// <summary>
        /// 
        /// </summary>
        private List<Glyph> m_GlyphsToRender = new List<Glyph>();

        /// <summary>
        /// List used in the process of adding new glyphs to the atlas texture.
        /// </summary>
        private List<uint> m_GlyphIndexes = new List<uint>();
        private Dictionary<uint, List<uint>> s_GlyphLookupMap = new Dictionary<uint, List<uint>>();
        //private Dictionary<uint, uint> s_CharacterLookupMap = new Dictionary<uint, uint>();

        public bool TryAddCharacters(uint[] unicodes)
        {
            bool isMissingCharacters = false;

            // Clear list of glyph indexes.
            m_GlyphIndexes.Clear();
            s_GlyphLookupMap.Clear();

            // Load the font face
            FontEngine.LoadFontFace(m_SourceFontFile, m_FaceInfo.pointSize);

            for (int i = 0; i < unicodes.Length; i++)
            {
                uint unicode = unicodes[i];

                // Check if character is already contained in the character table.
                if (m_CharacterLookupDictionary.ContainsKey(unicode))
                    continue;

                // Get the index of the glyph for this unicode value.
                uint glyphIndex = FontEngine.GetGlyphIndex(unicode);

                if (glyphIndex == 0)
                {
                    isMissingCharacters = true;
                    continue;
                }

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
                {
                    TMP_Character character = new TMP_Character(unicode, m_GlyphLookupDictionary[glyphIndex]);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);

                    continue;
                }

                // Check if glyph is already on the list of glyphs to add
                if (s_GlyphLookupMap.ContainsKey(glyphIndex))
                {
                    s_GlyphLookupMap[glyphIndex].Add(unicode);
                    continue;
                }

                s_GlyphLookupMap.Add(glyphIndex, new List<uint> { unicode });
                m_GlyphIndexes.Add(glyphIndex);
            }

            if (m_GlyphIndexes == null || m_GlyphIndexes.Count == 0)
                return true;

            Glyph[] glyphs;
            bool allCharactersAdded = FontEngine.TryAddGlyphsToTexture(m_GlyphIndexes, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            for (int i = 0; i < glyphs.Length; i++)
            {
                Glyph glyph = glyphs[i];
                
                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyph.index, glyph);

                // Add new character(s)
                foreach (uint unicode in s_GlyphLookupMap[glyph.index])
                {
                    TMP_Character character = new TMP_Character(unicode, glyph);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);
                }
            }

            return allCharactersAdded && !isMissingCharacters;
        }


        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="characters">String containing the characters to add to the font asset.</param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(string characters)
        {
            // Make sure font asset is set to dynamic and that we have a valid list of characters.
            if (string.IsNullOrEmpty(characters) || m_AtlasPopulationMode == AtlasPopulationMode.Static)
            {
                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because its AtlasPopulationMode is set to Static.", this);
                else
                {
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because the provided character list is Null or Empty.", this);
                }

                return false;
            }

            // Load font face.
            if (FontEngine.LoadFontFace(m_SourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
                return false;

            bool isMissingCharacters = false;
            int characterCount = characters.Length;

            // Clear list / dictionary used to track which glyph needs to be added to atlas texture.
            m_GlyphIndexes.Clear();
            s_GlyphLookupMap.Clear();

            // Iterate over each of the requested characters.
            for (int i = 0; i < characterCount; i++)
            {
                uint unicode = characters[i];

                // Check if character is already contained in the character table.
                if (m_CharacterLookupDictionary.ContainsKey(unicode))
                    continue;

                // Get the index of the glyph for this unicode value.
                uint glyphIndex = FontEngine.GetGlyphIndex(unicode);

                // Skip missing glyphs
                if (glyphIndex == 0)
                {
                    // Might want to keep track and report the missing characters.
                    isMissingCharacters = true;
                    continue;
                }

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
                {
                    TMP_Character character = new TMP_Character(unicode, m_GlyphLookupDictionary[glyphIndex]);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);

                    continue;
                }

                // Check if glyph is already on the list of glyphs to added.
                if (s_GlyphLookupMap.ContainsKey(glyphIndex))
                {
                    // Exclude duplicates.
                    if (s_GlyphLookupMap[glyphIndex].Contains(unicode))
                        continue;

                    s_GlyphLookupMap[glyphIndex].Add(unicode);
                    continue;
                }

                // Add glyph to list of glyphs to add and glyph lookup map.
                s_GlyphLookupMap.Add(glyphIndex, new List<uint> { unicode } );
                m_GlyphIndexes.Add(glyphIndex);
            }

            if (m_GlyphIndexes == null || m_GlyphIndexes.Count == 0)
            {
                Debug.LogWarning("No characters will be added to font asset [" + this.name + "] either because they are already present in the font asset or missing from the font file.");
                return true;
            }

            Glyph[] glyphs;
            bool allCharactersAdded = FontEngine.TryAddGlyphsToTexture(m_GlyphIndexes, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            for (int i = 0; i < glyphs.Length; i++)
            {
                Glyph glyph = glyphs[i];

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyph.index, glyph);

                // Add new character(s)
                List<uint> unicodes = s_GlyphLookupMap[glyph.index];
                int unicodeCount = unicodes.Count;

                for (int j = 0; j < unicodeCount; j++)
                {
                    uint unicode = unicodes[j];

                    TMP_Character character = new TMP_Character(unicode, glyph);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);
                }
            }

            return allCharactersAdded && !isMissingCharacters;
        }

        /*
        public bool TryAddCharacters(string characters)
        {
            // Make sure font asset is set to dynamic and that we have a valid list of characters.
            if (string.IsNullOrEmpty(characters) || m_AtlasPopulationMode == AtlasPopulationMode.Static)
            {
                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because its AtlasPopulationMode is set to Static.", this);
                else
                {
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because the provided character list is Null or Empty.", this);
                }

                return false;
            }

            // Load font face.
            if (FontEngine.LoadFontFace(sourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
                return false;

            for (int i = 0; i < characters.Length; i++)
            {
                uint unicode = characters[i];

                if (TryAddCharacter_Internal(unicode))
                {

                }
            }

            return true;
        }
        */

        /// <summary>
        /// Try adding character using Unicode value to font asset.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character.</param>
        /// <param name="character">The character data if successfully added to the font asset. Null otherwise.</param>
        /// <returns>Returns true if the character has been added. False otherwise.</returns>
        internal bool TryAddCharacter_Internal(uint unicode)
        {
            TMP_Character character = null;
            
            // Check if character is already contained in the character table.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
            {
                character = m_CharacterLookupDictionary[unicode];
                return true;
            }

            uint glyphIndex = FontEngine.GetGlyphIndex(unicode);
            if (glyphIndex == 0)
                return false;

            Glyph glyph;
            // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
            {
                character = new TMP_Character(unicode, m_GlyphLookupDictionary[glyphIndex]);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                //#if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                //UnityEditor.EditorUtility.SetDirty(this);
                //#endif

                return true;
            }

            if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
            {
                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character
                character = new TMP_Character(unicode, glyph);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                //#if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                //UnityEditor.EditorUtility.SetDirty(this);
                //#endif

                return true;
            }

            return false;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="glyph"></param>
        internal TMP_Character AddCharacter_Internal(uint unicode, Glyph glyph)
        {
            // Check if character is already contained in the character table.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return m_CharacterLookupDictionary[unicode];

            uint glyphIndex = glyph.index;

            // Check if glyph is already contained in the glyph table.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex) == false)
            {
                if (glyph.glyphRect.width == 0 || glyph.glyphRect.width == 0)
                {
                    // Glyphs with zero width and / or height can be automatically added to font asset.
                    m_GlyphTable.Add(glyph);
                }
                else
                {
                    // Try packing new glyph 
                    if (FontEngine.TryPackGlyphInAtlas(glyph, m_AtlasPadding, GlyphPackingMode.ContactPointRule, m_AtlasRenderMode, m_AtlasWidth, m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects) == false)
                    {
                        // TODO: Add handling to create new atlas texture to fit glyph.

                        return null;
                    }

                    m_GlyphsToRender.Add(glyph);
                }
            }

            // Add character to font asset.
            TMP_Character character = new TMP_Character(unicode, glyph);
            m_CharacterTable.Add(character);
            m_CharacterLookupDictionary.Add(unicode, character);

            Debug.Log("Adding character [" + (char)unicode + "] with Unicode (" + unicode + ") to [" + this.name + "] font asset.");

            // Schedule glyph to be added to the font atlas texture
            //TM_FontAssetUpdateManager.RegisterFontAssetForUpdate(this);
            UpdateAtlasTexture(); // Temporary until callback system is revised.

            //#if UNITY_EDITOR
            // Makes the changes to the font asset persistent.
            // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
            // Could also add some update registry to handle this.
            //SortGlyphTable();
            //UnityEditor.EditorUtility.SetDirty(this);
            //#endif

            return character;
        }

        /// <summary>
        /// Try adding character using Unicode value to font asset.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character.</param>
        /// <param name="character">The character data if successfully added to the font asset. Null otherwise.</param>
        /// <returns>Returns true if the character has been added. False otherwise.</returns>
        internal bool TryAddCharacter(uint unicode, out TMP_Character character)
        {
            // Check if character is already contained in the character table.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
            {
                character = m_CharacterLookupDictionary[unicode];
                return true;
            }

            character = null;

            // Load font face.
            if (FontEngine.LoadFontFace(sourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
                return false;

            uint glyphIndex = FontEngine.GetGlyphIndex(unicode);
            if (glyphIndex == 0)
                return false;

            Glyph glyph;
            // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
            {
                character = new TMP_Character(unicode, m_GlyphLookupDictionary[glyphIndex]);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                //#if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                //UnityEditor.EditorUtility.SetDirty(this);
                //#endif

                return true;
            }

            if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
            {
                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character
                character = new TMP_Character(unicode, glyph);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                //#if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                //UnityEditor.EditorUtility.SetDirty(this);
                //#endif

                return true;
            }

            return false;
        }


        public void UpdateAtlasTexture()
        {
            // Return if we don't have any glyphs to add to atlas texture.
            // This is possible if UpdateAtlasTexture() was called manually.
            //if (m_GlyphsToPack.Count == 0)
            //    return;

            if (m_GlyphsToRender.Count == 0)
                return;

            Debug.Log("Updating [" + this.name + "]'s atlas texture.");

            // Pack glyphs in the given atlas texture size. 
            // TODO: Packing and glyph render modes should be defined in the font asset.
            //FontEngine.PackGlyphsInAtlas(m_GlyphsToPack, m_GlyphsPacked, m_AtlasPadding, GlyphPackingMode.ContactPointRule, GlyphRenderMode.SDFAA, m_AtlasWidth, m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects);
            //FontEngine.RenderGlyphsToTexture(m_GlyphsPacked, m_AtlasPadding, GlyphRenderMode.SDFAA, m_AtlasTextures[m_AtlasTextureIndex]);

            FontEngine.RenderGlyphsToTexture(m_GlyphsToRender, m_AtlasPadding, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex]);

            // Apply changes to atlas texture
            m_AtlasTextures[m_AtlasTextureIndex].Apply(false, false);

            // Add glyphs that were successfully packed to the glyph table.
            for (int i = 0; i < m_GlyphsToRender.Count /* m_GlyphsPacked.Count */; i++)
            {
                Glyph glyph = m_GlyphsToRender[i]; // m_GlyphsPacked[i];

                // Update atlas texture index
                glyph.atlasIndex = m_AtlasTextureIndex;

                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyph.index, glyph);
            }

            // Clear list of glyphs
            m_GlyphsPacked.Clear();
            m_GlyphsToRender.Clear();

            // Add any remaining glyphs into new atlas texture if multi texture support if enabled.
            if (m_GlyphsToPack.Count > 0)
            {
                /*
                // Create new atlas texture 
                Texture2D tex = new Texture2D(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false, true);
                tex.SetPixels32(new Color32[m_AtlasWidth * m_AtlasHeight]);
                tex.Apply();

                m_AtlasTextureIndex++;

                if (m_AtlasTextures.Length == m_AtlasTextureIndex)
                    Array.Resize(ref m_AtlasTextures, Mathf.NextPowerOfTwo(m_AtlasTextureIndex + 1));

                m_AtlasTextures[m_AtlasTextureIndex] = tex;
                */
            }

            #if UNITY_EDITOR
            // Makes the changes to the font asset persistent.
            SortGlyphAndCharacterTables();
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }


        /// <summary>
        /// Clears font asset data including the glyph and character tables and textures.
        /// Function might be changed to Internal and only used in tests.
        /// </summary>
        public void ClearFontAssetData()
        {
            // Clear glyph and character tables
            if (m_GlyphTable != null)
                m_GlyphTable.Clear();

            if (m_CharacterTable != null)
                m_CharacterTable.Clear();

            // Clear glyph rectangles
            if (m_UsedGlyphRects != null)
                m_UsedGlyphRects.Clear();

            if (m_FreeGlyphRects != null)
            {
                int packingModifier = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
                m_FreeGlyphRects = new List<GlyphRect>() { new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier) };
            }

            if (m_GlyphsToPack != null)
                m_GlyphsToPack.Clear();

            if (m_GlyphsPacked != null)
                m_GlyphsPacked.Clear();

            // Clear Glyph Adjustment Table
            if (m_KerningTable != null && m_KerningTable.kerningPairs != null)
                m_KerningTable.kerningPairs.Clear();

            m_AtlasTextureIndex = 0;

            // Clear atlas textures
            if (m_AtlasTextures != null)
            {
                for (int i = 0; i < m_AtlasTextures.Length; i++)
                {
                    Texture2D texture = m_AtlasTextures[i];

                    if (texture == null)
                        continue;

                    // Verify texture size hasn't changed.
                    if (texture.width != m_AtlasWidth || texture.height != m_AtlasHeight)
                        texture.Resize(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false);

                    // Clear texture atlas
                    FontEngine.ResetAtlasTexture(texture);
                    texture.Apply();

                    if (i == 0)
                        m_AtlasTexture = texture;

                    m_AtlasTextures[i] = texture;
                }
            }

            ReadFontAssetDefinition();
        }



        /// <summary>
        /// Internal method used to upgrade font asset to support Dynamic SDF.
        /// </summary>
        private void UpgradeFontAsset()
        {
            m_Version = "1.1.0";

            Debug.LogWarning("Upgrading font asset [" + this.name + "] to version " + m_Version + ".", this);

            m_FaceInfo.familyName = m_fontInfo.Name;
            m_FaceInfo.styleName = string.Empty;

            m_FaceInfo.pointSize = (int)m_fontInfo.PointSize;
            m_FaceInfo.scale = m_fontInfo.Scale;

            m_FaceInfo.lineHeight = m_fontInfo.LineHeight;
            m_FaceInfo.ascentLine = m_fontInfo.Ascender;
            m_FaceInfo.capLine = m_fontInfo.CapHeight;
            m_FaceInfo.meanLine = m_fontInfo.CenterLine;
            m_FaceInfo.baseline = m_fontInfo.Baseline;
            m_FaceInfo.descentLine = m_fontInfo.Descender;

            m_FaceInfo.superscriptOffset = m_fontInfo.SuperscriptOffset;
            m_FaceInfo.superscriptSize = m_fontInfo.SubSize;
            m_FaceInfo.subscriptOffset = m_fontInfo.SubscriptOffset;
            m_FaceInfo.subscriptSize = m_fontInfo.SubSize;

            m_FaceInfo.underlineOffset = m_fontInfo.Underline;
            m_FaceInfo.underlineThickness = m_fontInfo.UnderlineThickness;
            m_FaceInfo.strikethroughOffset = m_fontInfo.strikethrough;
            m_FaceInfo.strikethroughThickness = m_fontInfo.strikethroughThickness;

            m_FaceInfo.tabWidth = m_fontInfo.TabWidth;

            if (m_AtlasTextures == null || m_AtlasTextures.Length == 0)
                m_AtlasTextures = new Texture2D[1];

            m_AtlasTextures[0] = atlas;

            //atlas = null;

            m_AtlasWidth = (int)m_fontInfo.AtlasWidth;
            m_AtlasHeight = (int)m_fontInfo.AtlasHeight;
            m_AtlasPadding = (int)m_fontInfo.Padding;

            switch(m_CreationSettings.renderMode)
            {
                case 0:
                    m_AtlasRenderMode = GlyphRenderMode.SMOOTH_HINTED;
                    break;
                case 1:
                    m_AtlasRenderMode = GlyphRenderMode.SMOOTH;
                    break;
                case 2:
                    m_AtlasRenderMode = GlyphRenderMode.RASTER_HINTED;
                    break;
                case 3:
                    m_AtlasRenderMode = GlyphRenderMode.RASTER;
                    break;
                case 6:
                    m_AtlasRenderMode = GlyphRenderMode.SDF16;
                    break;
                case 7:
                    m_AtlasRenderMode = GlyphRenderMode.SDF32;
                    break;
            }

            //m_fontInfo = null;

            // Convert font weight table
            if (fontWeights != null)
            {
                m_FontWeightTable[4] = fontWeights[4];
                m_FontWeightTable[7] = fontWeights[7];

                // Clear old fontWeight
                //fontWeights = null;
            }

            // Check if font asset creation settings contains a reference to the source font file GUID
            if (m_CreationSettings.sourceFontFileGUID != null || m_CreationSettings.sourceFontFileGUID != string.Empty)
            {
                m_SourceFontFileGUID = m_CreationSettings.sourceFontFileGUID;
            }
            else
            {
                Debug.LogWarning("Font asset [" + this.name + "] doesn't have a reference to its source font file. Please assign the appropriate source font file for this asset in the Font Atlas & Material section of font asset inspector.", this);
            }

            // Convert legacy glyph and character tables to new format
            m_GlyphTable.Clear();
            m_CharacterTable.Clear();

            #if UNITY_EDITOR
            // Load font to allow us to get the glyph index.
            if (m_SourceFontFile_EditorRef != null)
            {
                FontEngine.LoadFontFace(m_SourceFontFile_EditorRef);
            }
            #endif


            for (int i = 0; i < m_glyphInfoList.Count; i++)
            {
                TMP_Glyph oldGlyph = m_glyphInfoList[i];

                Glyph glyph = new Glyph();

                glyph.index = 0;
                
                #if UNITY_EDITOR
                if (m_SourceFontFile_EditorRef != null)
                    glyph.index = FontEngine.GetGlyphIndex((uint)oldGlyph.id);
                #endif

                glyph.glyphRect = new GlyphRect((int)oldGlyph.x, m_AtlasHeight - (int)(oldGlyph.y + oldGlyph.height + 0.5f), (int)(oldGlyph.width + 0.5f), (int)(oldGlyph.height + 0.5f));
                glyph.metrics = new GlyphMetrics(oldGlyph.width, oldGlyph.height, oldGlyph.xOffset, oldGlyph.yOffset, oldGlyph.xAdvance);
                glyph.scale = oldGlyph.scale;
                glyph.atlasIndex = 0;

                m_GlyphTable.Add(glyph);

                TMP_Character character = new TMP_Character((uint)oldGlyph.id, glyph);

                m_CharacterTable.Add(character);
            }

            // Clear legacy glyph info list.
            //m_glyphInfoList.Clear();

            ReadFontAssetDefinition();

            // Convert atlas textures data to new format
            // TODO
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }

    }
}