using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using Unity.InteractiveTutorials;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(Readme))]
[InitializeOnLoad]
public class ReadmeEditor : Editor {
	
	static string kShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";
	
	static ReadmeEditor()
	{
		EditorApplication.delayCall += SelectReadmeAutomatically;
	}
	
	static void SelectReadmeAutomatically()
	{
		if (!SessionState.GetBool(kShowedReadmeSessionStateName, false ))
		{
			var readme = SelectReadme();
			SessionState.SetBool(kShowedReadmeSessionStateName, true);
			
			if (readme && !readme.loadedLayout)
			{
				LoadLayout();
				readme.loadedLayout = true;
			}
		} 
	}
	
	static void LoadLayout()
	{
		var assembly = typeof(EditorApplication).Assembly; 
		var windowLayoutType = assembly.GetType("UnityEditor.WindowLayout", true);
		var method = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public | BindingFlags.Static);
		method.Invoke(null, new object[]{Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt"), false});
	}
	
	[MenuItem("Help/Template Walkthroughs")]
	static Readme SelectReadme() 
	{
		var ids = AssetDatabase.FindAssets("t:Readme");
		if (ids.Length == 1)
		{
			var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));
			
			Selection.objects = new UnityEngine.Object[]{readmeObject};
			
			return (Readme)readmeObject;
		}

		return null;
	}
	
	protected override void OnHeaderGUI()
	{
		var readme = (Readme)target;
		Init();
		
		var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth/3f - 20f, 128f);
		
		GUILayout.BeginHorizontal("In BigTitle");
		{
			GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
		    GUILayout.BeginVertical();
            {
                GUILayout.Label(readme.title, TitleStyle);
                GUILayout.Label(readme.description, DescriptionStyle);
            }
		    GUILayout.EndVertical();
		}
		GUILayout.EndHorizontal();
	}
	
	public override void OnInspectorGUI()
	{
		var readme = (Readme)target;
		Init();

	    for (int index = 0; index < readme.sections.Length; index++)
	    {
	        var section = readme.sections[index];
	        if (!string.IsNullOrEmpty(section.heading))
	        {
	            GUILayout.Label(section.heading, HeadingStyle);
	        }

	        if (!string.IsNullOrEmpty(section.text))
	        {
	            GUILayout.Label(section.text, BodyStyle);
	        }

	        if (!string.IsNullOrEmpty(section.linkText))
	        {
	            if (LinkLabel(new GUIContent(section.linkText)))
	            {
	                Application.OpenURL(section.url);
	            }
	        }

	        if (section.CanDrawButton)
	        {
	            if (Button(new GUIContent(section.buttonText)))
	            {
	                section.StartTutorial();
	                GUIUtility.ExitGUI();
	            }
	        }

	        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
	    }
	}
	
	[NonSerialized]
	bool m_Initialized;
	
    GUIStyle ButtonStyle { get { return m_ButtonStyle; } }
    [SerializeField] GUIStyle m_ButtonStyle;

	GUIStyle LinkStyle { get { return m_LinkStyle; } }
	[SerializeField] GUIStyle m_LinkStyle;
	
	GUIStyle TitleStyle { get { return m_TitleStyle; } }
	[SerializeField] GUIStyle m_TitleStyle;

    GUIStyle DescriptionStyle { get { return m_DescriptionStyle; } }
    [SerializeField] GUIStyle m_DescriptionStyle;
	
	GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
	[SerializeField] GUIStyle m_HeadingStyle;
	
	GUIStyle BodyStyle { get { return m_BodyStyle; } }
	[SerializeField] GUIStyle m_BodyStyle;
	
	void Init()
	{
	    var readme = (Readme)target;
        if (m_Initialized)
			return;

		m_BodyStyle = new GUIStyle(EditorStyles.label);
		m_BodyStyle.wordWrap = true;
		m_BodyStyle.fontSize = readme.TextBodySize;
        
		m_TitleStyle = new GUIStyle(m_BodyStyle);
		m_TitleStyle.fontSize = readme.TextTitleSize;
		
	    m_DescriptionStyle = new GUIStyle(m_BodyStyle);
	    m_DescriptionStyle.fontSize = readme.TextProjectDescriptionSize;

		m_HeadingStyle = new GUIStyle(m_BodyStyle);
		m_HeadingStyle.fontSize = readme.TextHeadingSize;
		
		m_LinkStyle = new GUIStyle(m_BodyStyle);
		m_LinkStyle.wordWrap = false;
		// Match selection color which works nicely for both light and dark skins
		m_LinkStyle.normal.textColor = new Color (0x00/255f, 0x78/255f, 0xDA/255f, 1f);
		m_LinkStyle.stretchWidth = false;

	    m_ButtonStyle = new GUIStyle(GUI.skin.button);
	    m_ButtonStyle.fontSize = readme.ButtonTextSize;
        m_ButtonStyle.stretchWidth=false;
        
	    if(EditorGUIUtility.isProSkin)
	    {
	        TitleStyle.normal.textColor = readme.TextColorMainDarkSkin;
	        DescriptionStyle.normal.textColor = readme.TextColorMainDarkSkin;
	        HeadingStyle.normal.textColor = readme.TextColorMainDarkSkin;
	        BodyStyle.normal.textColor = readme.TextColorSecondaryDarkSkin;
	    }
	    else
	    {
	        TitleStyle.normal.textColor = readme.TextColorMainLightSkin;
	        DescriptionStyle.normal.textColor = readme.TextColorMainLightSkin;
	        HeadingStyle.normal.textColor = readme.TextColorMainLightSkin;
	        BodyStyle.normal.textColor = readme.TextColorSecondaryLightSkin;
	    }
        
		m_Initialized = true;
	}
	
	bool LinkLabel (GUIContent label, params GUILayoutOption[] options)
	{
		var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

		Handles.BeginGUI ();
		Handles.color = LinkStyle.normal.textColor;
		Handles.DrawLine (new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
		Handles.color = Color.white;
		Handles.EndGUI ();

		EditorGUIUtility.AddCursorRect (position, MouseCursor.Link);

		return GUI.Button (position, label, LinkStyle);
	}

    bool Button (GUIContent label, params GUILayoutOption[] options)
    {
        return GUILayout.Button(label, ButtonStyle,GUILayout.Height(40),GUILayout.Width(200));
    }
}

