﻿using UnityEngine;
using UnityEditor;
using System.Collections;


namespace TMPro.EditorUtilities
{

    public static class TMP_UIStyleManager
    {

        //private static bool m_isInitialized;

        public static GUISkin TMP_GUISkin;

        public static GUIStyle Label;
        //public static GUIStyle ScrollView;
        //public static GUIStyle VerticalScrollBar;
        //public static GUIStyle HorizontalScrollBar;
        public static GUIStyle Group_Label;
        public static GUIStyle Group_Label_Left;
        public static GUIStyle TextAreaBoxEditor;
        public static GUIStyle TextAreaBoxWindow;
        public static GUIStyle TextureAreaBox;
        public static GUIStyle Section_Label;
        public static GUIStyle SquareAreaBox85G;


        // Alignment Button Textures
        public static Texture2D alignLeft;
        public static Texture2D alignCenter;
        public static Texture2D alignRight;
        public static Texture2D alignJustified;
        public static Texture2D alignFlush;
        public static Texture2D alignGeoCenter;
        public static Texture2D alignTop;
        public static Texture2D alignMiddle;
        public static Texture2D alignBottom;
        public static Texture2D alignBaseline;
        public static Texture2D alignMidline;
        public static Texture2D alignCapline;

        //public static Texture2D progressTexture;
        public static Texture2D selectionBox;

        public static GUIContent[] alignContent_A;
        public static GUIContent[] alignContent_B;

        // Icons
        //public static GUIContent strikethroughIcon;



        public static void GetUIStyles()
        {
            if (TMP_GUISkin != null)
                return;

            // Find to location of the TextMesh Pro Asset Folder (as users may have moved it)
            string tmproAssetFolderPath = TMP_EditorUtility.packageRelativePath;

            if (EditorGUIUtility.isProSkin)
            {
                TMP_GUISkin = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/TMPro_DarkSkin.guiskin", typeof(GUISkin)) as GUISkin;

                alignLeft = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignLeft.psd", typeof(Texture2D)) as Texture2D;
                alignCenter = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignCenter.psd", typeof(Texture2D)) as Texture2D;
                alignRight = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignRight.psd", typeof(Texture2D)) as Texture2D;
                alignJustified = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignJustified.psd", typeof(Texture2D)) as Texture2D;
                alignFlush = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignFlush.psd", typeof(Texture2D)) as Texture2D;
                alignGeoCenter = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignCenterGeo.psd", typeof(Texture2D)) as Texture2D;
                alignTop = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignTop.psd", typeof(Texture2D)) as Texture2D;
                alignMiddle = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignMiddle.psd", typeof(Texture2D)) as Texture2D;
                alignBottom = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignBottom.psd", typeof(Texture2D)) as Texture2D;
                alignBaseline = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignBaseLine.psd", typeof(Texture2D)) as Texture2D;
                alignMidline = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignMidLine.psd", typeof(Texture2D)) as Texture2D;
                alignCapline = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignCapLine.psd", typeof(Texture2D)) as Texture2D;

                //progressTexture = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/Progress Bar.psd", typeof(Texture2D)) as Texture2D;
                //selectionBox = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/SelectionBox.psd", typeof(Texture2D)) as Texture2D;
                //strikethroughIcon = new GUIContent(AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/icon_strikethrough.psd", typeof(Texture2D)) as Texture2D);
                selectionBox = EditorGUIUtility.Load("IN thumbnailshadow On@2x") as Texture2D;


            }
            else
            {
                TMP_GUISkin = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/TMPro_LightSkin.guiskin", typeof(GUISkin)) as GUISkin;

                alignLeft = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignLeft_Light.psd", typeof(Texture2D)) as Texture2D;
                alignCenter = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignCenter_Light.psd", typeof(Texture2D)) as Texture2D;
                alignRight = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignRight_Light.psd", typeof(Texture2D)) as Texture2D;
                alignJustified = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignJustified_Light.psd", typeof(Texture2D)) as Texture2D;
                alignFlush = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignFlush_Light.psd", typeof(Texture2D)) as Texture2D;
                alignGeoCenter = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignCenterGeo_Light.psd", typeof(Texture2D)) as Texture2D;
                alignTop = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignTop_Light.psd", typeof(Texture2D)) as Texture2D;
                alignMiddle = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignMiddle_Light.psd", typeof(Texture2D)) as Texture2D;
                alignBottom = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignBottom_Light.psd", typeof(Texture2D)) as Texture2D;
                alignBaseline = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignBaseLine_Light.psd", typeof(Texture2D)) as Texture2D;
                alignMidline = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignMidLine_Light.psd", typeof(Texture2D)) as Texture2D;
                alignCapline = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/btn_AlignCapLine_Light.psd", typeof(Texture2D)) as Texture2D;

                //progressTexture = AssetDatabase.LoadAssetAtPath(tmproAssetFolderPath + "/Editor Resources/Textures/Progress Bar (Light).psd", typeof(Texture2D)) as Texture2D;
                selectionBox = EditorGUIUtility.Load("IN thumbnailshadow On@2x") as Texture2D;
            }

            if (TMP_GUISkin != null)
            {
                Label = TMP_GUISkin.FindStyle("Label");
                //ScrollView = TMP_GUISkin.FindStyle("ScrollView");
                //VerticalScrollBar = TMP_GUISkin.FindStyle("VerticalScrollbar");
                //HorizontalScrollBar = TMP_GUISkin.FindStyle("HorizontalScrollbar");
                Section_Label = TMP_GUISkin.FindStyle("Section Label");
                Group_Label = TMP_GUISkin.FindStyle("Group Label");
                Group_Label_Left = TMP_GUISkin.FindStyle("Group Label - Left Half");
                TextAreaBoxEditor = TMP_GUISkin.FindStyle("Text Area Box (Editor)");
                TextAreaBoxWindow = TMP_GUISkin.FindStyle("Text Area Box (Window)");
                TextureAreaBox = TMP_GUISkin.FindStyle("Texture Area Box");
                SquareAreaBox85G = TMP_GUISkin.FindStyle("Square Area Box (85 Grey)");


                alignContent_A = new GUIContent[]
                { 
                    new GUIContent(alignLeft, "Left"), 
                    new GUIContent(alignCenter, "Center"), 
                    new GUIContent(alignRight, "Right"), 
                    new GUIContent(alignJustified, "Justified"),
                    new GUIContent(alignFlush, "Flush"),
                    new GUIContent(alignGeoCenter, "Geometry Center")
                };

                alignContent_B = new GUIContent[]
                { 
                    new GUIContent(alignTop, "Top"), 
                    new GUIContent(alignMiddle, "Middle"), 
                    new GUIContent(alignBottom, "Bottom"),
                    new GUIContent(alignBaseline, "Baseline"),
                    new GUIContent(alignMidline, "Midline"),
                    new GUIContent(alignCapline, "Capline")
                };


            }

            //m_isInitialized = true;
        }

       
    }
}
