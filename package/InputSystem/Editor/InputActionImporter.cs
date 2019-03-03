#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Experimental.Input.Utilities;

#pragma warning disable 0649
namespace UnityEngine.Experimental.Input.Editor
{
    /// <summary>
    /// Imports an <see cref="InputActionAsset"/> from JSON.
    /// </summary>
    /// <remarks>
    /// Can generate code wrappers for the contained action sets as a convenience.
    /// Will not overwrite existing wrappers except if the generated code actually differs.
    /// </remarks>
    [ScriptedImporter(kVersion, InputActionAsset.kExtension)]
    public class InputActionImporter : ScriptedImporter
    {
        private const int kVersion = 4;

        private const string kActionIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/Add Action.png";
        private const string kAssetIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/Add ActionMap.png";
        private const string kActionIconDark = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/d_Add Action.png";
        private const string kAssetIconDark = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/d_Add ActionMap.png";

        [SerializeField] private bool m_GenerateWrapperCode;
        [SerializeField] private string m_WrapperCodePath;
        [SerializeField] private string m_WrapperClassName;
        [SerializeField] private string m_WrapperCodeNamespace;
        [SerializeField] private bool m_GenerateActionEvents;
        [SerializeField] private bool m_GenerateInterfaces;

        // Actions and maps coming in from JSON may not have IDs assigned to them. However,
        // once imported, we want them to have stable IDs. So we do the same thing that Unity's
        // model importer does and remember the GUID<->name correlations used in the file.
        [SerializeField] private RememberedGuid[] m_ActionGuids;
        [SerializeField] private RememberedGuid[] m_ActionMapGuids;

        [Serializable]
        internal struct RememberedGuid
        {
            public string name;
            public string guid;
        }

        private static InlinedArray<Action> s_OnImportCallbacks;

        public static event Action onImport
        {
            add => s_OnImportCallbacks.Append(value);
            remove => s_OnImportCallbacks.Remove(value);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            foreach (var callback in s_OnImportCallbacks)
                callback();

            ////REVIEW: need to check with version control here?
            // Read file.
            string text;
            try
            {
                text = File.ReadAllText(ctx.assetPath);
            }
            catch (Exception exception)
            {
                ctx.LogImportError($"Could not read file '{ctx.assetPath}' ({exception})");
                return;
            }

            // Create asset.
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            // Parse JSON.
            try
            {
                ////TODO: make sure action names are unique
                asset.LoadFromJson(text);
            }
            catch (Exception exception)
            {
                ctx.LogImportError($"Could not parse input actions in JSON format from '{ctx.assetPath}' ({exception})");
                DestroyImmediate(asset);
                return;
            }

            // Load icons.
            ////REVIEW: the icons won't change if the user changes skin; not sure it makes sense to differentiate here
            var isDarkSkin = EditorGUIUtility.isProSkin;
            var assetIcon = (Texture2D)EditorGUIUtility.Load(isDarkSkin ? kAssetIconDark : kAssetIcon);
            var actionIcon = (Texture2D)EditorGUIUtility.Load(isDarkSkin ? kActionIconDark : kActionIcon);

            // Add asset.
            ctx.AddObjectToAsset("<root>", asset, assetIcon);
            ctx.SetMainObject(asset);

            // Make sure every map and every action has a stable ID assigned to it.
            var maps = asset.actionMaps;
            foreach (var map in maps)
            {
                if (map.idDontGenerate == Guid.Empty)
                {
                    // Generate and remember GUID.
                    var id = map.id;
                    ArrayHelpers.Append(ref m_ActionMapGuids, new RememberedGuid
                    {
                        guid = id.ToString(),
                        name = map.name,
                    });
                }
                else
                {
                    // Retrieve remembered GUIDs.
                    if (m_ActionMapGuids != null)
                    {
                        for (var i = 0; i < m_ActionMapGuids.Length; ++i)
                        {
                            if (string.Compare(m_ActionMapGuids[i].name, map.name,
                                StringComparison.InvariantCultureIgnoreCase) == 0)
                            {
                                map.m_Guid = Guid.Empty;
                                map.m_Id = m_ActionMapGuids[i].guid;
                                break;
                            }
                        }
                    }
                }

                foreach (var action in map.actions)
                {
                    var actionName = $"{map.name}/{action.name}";
                    if (action.idDontGenerate == Guid.Empty)
                    {
                        // Generate and remember GUID.
                        var id = action.id;
                        ArrayHelpers.Append(ref m_ActionGuids, new RememberedGuid
                        {
                            guid = id.ToString(),
                            name = actionName,
                        });
                    }
                    else
                    {
                        // Retrieve remembered GUIDs.
                        if (m_ActionGuids != null)
                        {
                            for (var i = 0; i < m_ActionGuids.Length; ++i)
                            {
                                if (string.Compare(m_ActionGuids[i].name, actionName,
                                    StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    action.m_Guid = Guid.Empty;
                                    action.m_Id = m_ActionGuids[i].guid;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Create subasset for each action.
            foreach (var map in maps)
            {
                var haveSetName = !string.IsNullOrEmpty(map.name);

                foreach (var action in map.actions)
                {
                    var actionReference = ScriptableObject.CreateInstance<InputActionReference>();
                    actionReference.Set(action);

                    var objectName = action.name;
                    if (haveSetName)
                        objectName = $"{map.name}/{action.name}";

                    actionReference.name = objectName;
                    ctx.AddObjectToAsset(objectName, actionReference, actionIcon);
                }
            }

            // Generate wrapper code, if enabled.
            if (m_GenerateWrapperCode)
            {
                var wrapperFilePath = m_WrapperCodePath;
                if (string.IsNullOrEmpty(wrapperFilePath))
                {
                    var assetPath = ctx.assetPath;
                    var directory = Path.GetDirectoryName(assetPath);
                    var fileName = Path.GetFileNameWithoutExtension(assetPath);
                    wrapperFilePath = Path.Combine(directory, fileName) + ".cs";
                }

                var options = new InputActionCodeGenerator.Options
                {
                    sourceAssetPath = ctx.assetPath,
                    namespaceName = m_WrapperCodeNamespace,
                    className = m_WrapperClassName,
                    generateEvents = m_GenerateActionEvents,
                    generateInterfaces = m_GenerateInterfaces,
                };

                if (InputActionCodeGenerator.GenerateWrapperCode(wrapperFilePath, maps, asset.controlSchemes, options))
                {
                    // Inform database that we modified a source asset *during* import.
                    AssetDatabase.ImportAsset(wrapperFilePath);
                }
            }

            // Refresh editors.
            AssetInspectorWindow.RefreshAllOnAssetReimport();
        }

        ////REVIEW: actually pre-populate with some stuff?
        private const string kDefaultAssetLayout = "{}";

        // Add item to plop an .inputactions asset into the project.
        [MenuItem("Assets/Create/Input Actions")]
        public static void CreateInputAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New Controls." + InputActionAsset.kExtension,
                kDefaultAssetLayout);
        }
    }
}
#endif // UNITY_EDITOR
