using System.Linq;
using Unity.InteractiveTutorials;
using UnityEditor;
using UnityEngine;

public static class TutorialAssetsUtil
{
    [MenuItem("Assets/Create/Tutorial")]
    public static void CreateAssetTutorial()
    {
        var assetName = "New Tutorial.asset";
        var tutorial = ScriptableObject.CreateInstance<Tutorial>();
        CreateAsset(assetName, tutorial);
    }

    [MenuItem("Assets/Create/Tutorial Page")]
    public static void CreateAssetTutorialPage()
    {
        var assetName = "New Tutorial Page.asset";
        var tutorial = ScriptableObject.CreateInstance<TutorialPage>();
        CreateAsset(assetName, tutorial);
    }

    [MenuItem("Assets/Create/Tutorial Styles")]
    public static void CreateAssetTutorialStyles()
    {
        var assetName = "New Tutorial Styles.asset";
        var tutorial = ScriptableObject.CreateInstance<TutorialStyles>();
        CreateAsset(assetName, tutorial);
    }

    [MenuItem("Assets/Create/Tutorial Welcome Page")]
    public static void CreateAssetTutorialWelcomePage()
    {
        var assetName = "New Tutorial Welcome Page.asset";
        var tutorial = ScriptableObject.CreateInstance<TutorialWelcomePage>();
        CreateAsset(assetName, tutorial);
    }

    [MenuItem("Assets/Create/Tutorial Project Settings")]
    public static void CreateAssetTutorialProjectSettings()
    {
        const string assetName = "Tutorial Project Settings.asset";
        var tutorialProjectSettings = ScriptableObject.CreateInstance<TutorialProjectSettings>();
        CreateAsset(assetName, tutorialProjectSettings);
        TutorialProjectSettings.ReloadInstance();
    }

    static void CreateAsset(string assetName, UnityEngine.Object asset)
    {
        //GetActiveFolderPath always returns path with forward slashes
        var path = ProjectWindowUtil.GetActiveFolderPath() + "/" + assetName;
        ProjectWindowUtil.CreateAsset(asset, path);
    }

    [MenuItem("Tutorials/Layout/Save Current Layout to Asset")]
    public static void SaveCurrentLayoutToAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save layout", "layout.wlt", "wlt", "Choose the location to save the layout");
        if (path.Length != 0)
        {
            WindowLayout.SaveWindowLayout(path);
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tutorials/Masking/Remove TutorialWindow")]
    public static void RemoveTutorialWindowFromMaskingSettings()
    {
        foreach (TutorialPage page in Selection.objects)
        {
            if (page == null)
                continue;

            var tutorialWindowTypeName = typeof(TutorialWindow).AssemblyQualifiedName;

            var so = new SerializedObject(page);
            var paragraphs = so.FindProperty("m_Paragraphs.m_Items");
            for (int parIdx = 0; parIdx < paragraphs.arraySize; ++parIdx)
            {
                var paragraph = paragraphs.GetArrayElementAtIndex(parIdx);
                var unmaskedViews = paragraph.FindPropertyRelative("m_MaskingSettings.m_UnmaskedViews");
                for (var viewIdx = unmaskedViews.arraySize - 1; viewIdx >= 0; --viewIdx)
                {
                    var editorWindowTypeName =
                        unmaskedViews.GetArrayElementAtIndex(viewIdx).FindPropertyRelative("m_EditorWindowType.m_TypeName");
                    if (editorWindowTypeName.stringValue == tutorialWindowTypeName)
                    {
                        unmaskedViews.DeleteArrayElementAtIndex(viewIdx);
                    }
                }
            }
            so.ApplyModifiedProperties();
        }
    }

    [MenuItem("Tutorials/Masking/Configure Play Button")]
    public static void AddPlayButtonControlMaskingToParagraphsWithPlayModeCriteria()
    {
        var playButtonName = "ToolbarPlayModePlayButton";

        foreach (TutorialPage page in Selection.objects)
        {
            if (page == null)
                continue;

            var so = new SerializedObject(page);
            var paragraphs = so.FindProperty("m_Paragraphs.m_Items");
            for (int parIdx = 0; parIdx < paragraphs.arraySize; ++parIdx)
            {
                var paragraph = paragraphs.GetArrayElementAtIndex(parIdx);
                var criteria = paragraph.FindPropertyRelative("m_Criteria.m_Items");
                if (
                    Enumerable.Range(0, criteria.arraySize).Select(
                        i => criteria.GetArrayElementAtIndex(i).FindPropertyRelative("criterion").objectReferenceValue).Any(c => c is PlayModeStateCriterion)
                    )
                {
                    var unmaskedViews = paragraph.FindPropertyRelative("m_MaskingSettings.m_UnmaskedViews");
                    var configured = false;
                    var toolbarIdx = -1;
                    for (int viewIdx = 0; viewIdx < unmaskedViews.arraySize; ++viewIdx)
                    {
                        var unmaskedView = unmaskedViews.GetArrayElementAtIndex(viewIdx);
                        var viewTypeName = unmaskedView.FindPropertyRelative("m_ViewType.m_TypeName");
                        if (viewTypeName.stringValue != typeof(Toolbar).AssemblyQualifiedName)
                            continue;

                        toolbarIdx = viewIdx;
                        var unmaskedControls = unmaskedView.FindPropertyRelative("m_UnmaskedControls");
                        for (int ctlIdx = 0; ctlIdx < unmaskedControls.arraySize; ++ctlIdx)
                        {
                            var control = unmaskedControls.GetArrayElementAtIndex(ctlIdx);
                            if (
                                control.FindPropertyRelative("m_ControlName").stringValue == playButtonName &&
                                control.FindPropertyRelative("m_SelectorMode").intValue == (int)GUIControlSelector.Mode.NamedControl
                                )
                            {
                                configured = true;
                                break;
                            }
                        }
                        if (configured)
                            break;
                    }

                    if (!configured)
                    {
                        SerializedProperty unmaskedToolbar;
                        if (toolbarIdx < 0)
                        {
                            unmaskedViews.InsertArrayElementAtIndex(unmaskedViews.arraySize);
                            unmaskedToolbar = unmaskedViews.GetArrayElementAtIndex(toolbarIdx);
                        }
                        else
                        {
                            unmaskedToolbar = unmaskedViews.GetArrayElementAtIndex(toolbarIdx);
                        }
                        var toolbarControls = unmaskedToolbar.FindPropertyRelative("m_UnmaskedControls");
                        toolbarControls.InsertArrayElementAtIndex(toolbarControls.arraySize);
                        var control = toolbarControls.GetArrayElementAtIndex(toolbarControls.arraySize - 1);
                        control.FindPropertyRelative("m_ControlName").stringValue = playButtonName;
                        control.FindPropertyRelative("m_SelectorMode").intValue = (int)GUIControlSelector.Mode.NamedControl;
                    }
                }
            }
            so.ApplyModifiedProperties();
        }
    }
}
