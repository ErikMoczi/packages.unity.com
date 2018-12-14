using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class SpriteEditorWindowFake : EditorWindow, ISpriteEditor
    {
        public List<SpriteRect> spriteRects { get; set; }
        public SpriteRect selectedSpriteRect { get; set; }
        public bool enableMouseMoveEvent { get; set; }
        public bool editingDisabled { get; private set; }
        public Rect windowDimension { get; private set; }

        private ISpriteEditorDataProvider m_DataProvider;
        private SkinningModule m_SkinningModule;
        public SkinningModule skinningModule
        {
            get { return m_SkinningModule; }
        }

        protected virtual ISpriteEditorDataProvider GetSpriteEditorDataProvider()
        {
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Resources.Load<Texture2D>("Texture2"))) as ISpriteEditorDataProvider;
        }

        public virtual T GetDataProvider<T>() where T : class
        {
            return m_DataProvider.GetDataProvider<T>();
        }
        
        public void OnEnable()
        {
            m_DataProvider = GetSpriteEditorDataProvider();
            m_DataProvider.InitSpriteEditorDataProvider();
            m_SkinningModule = new SkinningModule();
            m_SkinningModule.GetType()
                .GetProperty("spriteEditor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(m_SkinningModule, this, null);
        }

        public bool HandleSpriteSelection()
        {
            return true;
        }

        public void RequestRepaint()
        {
        }

        public void SetDataModified()
        {
        }

        public void ApplyOrRevertModification(bool apply)
        {
        }

        public VisualElement GetMainVisualContainer()
        {
            return this.GetRootVisualContainer();
        }

        public void SetPreviewTexture(Texture2D texture, int width, int height)
        {
        }

        public void Activate()
        {
            m_SkinningModule.OnModuleActivate();
        }

        public void Deactivate()
        {
            m_SkinningModule.OnModuleDeactivate();
        }
    }

    public class SkinningModuleTestBase
    {
        protected List<Sprite> m_Sprites = new List<Sprite>();
        protected Texture2D m_Texture;
        protected SpriteEditorWindowFake m_Window;
        internal SkinningCache skinningCache
        {
            get { return m_Window.skinningModule.skinningCache; }
        }

        private static string kTestTempFolder = "Assets/Temp/";

        private int m_AlphaTolerance;
        private int m_OutlineDetail;
        private int m_Subdivide;

        [OneTimeTearDown]
        public void FullTeardown()
        {
            GenerateGeomertySettings.alphaTolerance = m_AlphaTolerance;
            GenerateGeomertySettings.outlineDetail = m_OutlineDetail;
            GenerateGeomertySettings.subdivide = m_Subdivide;

            // Delete cloned sprites
            AssetDatabase.DeleteAsset(Path.GetDirectoryName(kTestTempFolder));
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_AlphaTolerance = GenerateGeomertySettings.alphaTolerance;
            m_OutlineDetail = GenerateGeomertySettings.outlineDetail;
            m_Subdivide = GenerateGeomertySettings.subdivide;

            GenerateGeomertySettings.alphaTolerance = 15;
            GenerateGeomertySettings.outlineDetail = 50;
            GenerateGeomertySettings.subdivide = 100;
            
            CloneTextureForTest("Texture2");
        }

        private static void CloneTextureForTest(string textureName)
        {
            ValidateDirectory(kTestTempFolder);

            var texture = Resources.Load<Texture2D>(textureName);
            var path = AssetDatabase.GetAssetPath(texture);

            AssetDatabase.CopyAsset(path, kTestTempFolder + Path.GetFileName(path));
            AssetDatabase.Refresh();
        }

        private static void ValidateDirectory(string path)
        {
            var dirPath = Path.GetDirectoryName(path);

            if (Directory.Exists(dirPath) == false)
                Directory.CreateDirectory(dirPath);
        }

        [SetUp]
        public void Setup()
        {
            SetupWindow();
            m_Window.Focus();
            m_Window.Show();
            m_Window.Activate();
            DoOtherSetup();
        }

        public virtual void SetupWindow()
        {
            m_Window = EditorWindow.GetWindow<SpriteEditorWindowFake>();
        }

        public virtual void DoOtherSetup()
        {
        }

        public virtual void DoOtherTeardown()
        {
        }

        [TearDown]
        public void Teardown()
        {
            DoOtherTeardown();
            m_Window.Deactivate();
            m_Window.Close();
        }
    }
}