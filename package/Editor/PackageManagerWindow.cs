#if ENABLE_PACKMAN

using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VisualStudioIntegration;
using UnityEditorInternal;
using UnityEditor.Web;
using UnityEditor.PackageManager;

namespace UnityEditor.PackageManager
{
    /// <summary>
    /// Simple text based progression display. Will render a string that change
    /// overtime to show progression.
    /// </summary>
    class Progression
    {
        private float progressionTime = 0.0f;
        private static string[] progressionGlyphs = new[] {"", ".", "..", "..."};
        private string progressionGlyph = "";

        /// <summary>
        /// Render the *progression* text.
        /// </summary>
        ///
        /// <returns>
        /// The rendered text (string).
        /// ve
        /// </returns>
        public string Render()
        {
            return "Processing " + progressionGlyph;
        }

        /// <summary>
        /// Move progression forward according to time elapsed since last call.
        ///
        /// Usually, this method should be called within an `Editor::Update` or
        /// `Editor::FixedUpdate` delegate.
        /// </summary>
        ///
        /// <returns>
        /// `true` if the render text has changed since last call. `false` if
        /// the frame has not changed.
        /// </returns>
        public bool Tick()
        {
            progressionTime += Time.deltaTime;
            string newProgressionGlyph = progressionGlyphs[(int)(progressionTime % 4)];
            if (newProgressionGlyph != progressionGlyph)
            {
                progressionGlyph = newProgressionGlyph;
                return true;
            }
            return false;
        }
    }

    //This is an example class on how to use the Unity Package Manager.
    class PackageManagerWindow : EditorWindow
    {
        private string listButton = "List";
        private string addButton = "Add";
        private string removeButton = "Remove";
        private string searchButton = "Search";
        private string resolveButton = "Resolve";
        private string outdatedButton = "Outdated";
        private long operationId = -1;
        private int type = 0;
        private string resultText = "";
        private string packageId = "";
        private Vector2 scrollPosition;
        private bool operationRunning = false;
        private bool operationError = false;
        private Progression progression = new Progression();

        private string addInputStr = "stitchmeup@1.0.1";
        private string removeInputStr = "stitchmeup";
        private string searchInputStr = "stitchmeup";

        private StringBuilder sb = new StringBuilder();

        [MenuItem("Window/Package Manager", false, 1012)]
        public static void  ShowWindow()
        {
            EditorWindow.GetWindow(typeof(PackageManagerWindow));
        }

        private void UpmPackageInfoToStringBuffer(UpmPackageInfo packageInfo, bool addIndentation)
        {
            String tabs = addIndentation ? "\t" : "";
            sb.AppendLine();
            sb.Append(tabs);
            sb.Append("packageInfo.packageId: " + packageInfo.packageId);
            sb.AppendLine();
            sb.Append(tabs);
            sb.Append("packageInfo.tag: " + packageInfo.tag);
            sb.AppendLine();
            sb.Append(tabs);
            sb.Append("packageInfo.version: " + packageInfo.version);
            sb.AppendLine();
            sb.Append(tabs);
            sb.Append("packageInfo.originType: " + packageInfo.originType);
            sb.AppendLine();
            sb.Append(tabs);
            sb.Append("packageInfo.originLocation: " + packageInfo.originLocation);
            sb.AppendLine();
            sb.Append(tabs);
            sb.Append("packageInfo.relationType: " + packageInfo.relationType);
            sb.AppendLine();
            sb.Append(tabs);
            sb.Append("packageInfo.resolvedPath: " + packageInfo.resolvedPath);
            sb.AppendLine();
        }

        private void OperationStatusToStringBuffer(OperationStatus operationStatus)
        {
            Debug.Log("operationStatus");
            sb.AppendLine();
            sb.Append("operationStatus.status: " + operationStatus.status);
            sb.AppendLine();
            sb.Append("operationStatus.id: " + operationStatus.id);
            sb.AppendLine();
            sb.Append("operationStatus.type: " + operationStatus.type);
            sb.AppendLine();
            sb.Append("operationStatus.progress: " + operationStatus.progress);
            sb.AppendLine();
            for (int i = 0; i < operationStatus.packageList.Length; ++i)
            {
                sb.Append("\n\tPackageInfo #" + i);
                UpmPackageInfo packageInfo = operationStatus.packageList[i];
                UpmPackageInfoToStringBuffer(packageInfo, true);
            }
        }

        private void ResetView()
        {
            resultText = "";
            operationRunning = true;
            operationError = false;
        }

        void OnGUI()
        {
            GUI.enabled = !operationRunning;
            GUILayout.Label("Press this button to list the project packages.");
            if (GUILayout.Button(listButton, GUILayout.Width(120)))
            {
                resultText = "";
                StatusCode code = Client.List(out operationId);
                if (code == StatusCode.Error)
                {
                    Debug.Log("List: Error");
                    return;
                }
                type = 0;
                Debug.Log("List operationId: " + operationId);
                ResetView();
            }
            GUILayout.Space(20);

            GUILayout.Label("Press this button to resolve the project packages and their dependencies.");
            if (GUILayout.Button(resolveButton, GUILayout.Width(120)))
            {
                resultText = "";
                StatusCode code = Client.Resolve(out operationId);
                if (code == StatusCode.Error)
                {
                    Debug.Log("List: Error");
                    return;
                }
                type = 0;
                Debug.Log("Resolve operationId: " + operationId);
                ResetView();
            }
            GUILayout.Space(20);

            GUILayout.Label("Select a name@version and press this button to add the package to the project.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(addButton, GUILayout.Width(120)))
            {
                StatusCode code = Client.Add(out operationId, addInputStr);
                if (code == StatusCode.Error)
                {
                    Debug.Log("Add: Error");
                    return;
                }
                type = 1;
                Debug.Log("Add operationId: " + operationId);
                ResetView();
            }
            addInputStr = GUILayout.TextField(addInputStr, 30);
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.Label("Select a name@version and press this button to remove the package from the project.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(removeButton, GUILayout.Width(120)))
            {
                StatusCode code = Client.Remove(out operationId, removeInputStr);
                if (code == StatusCode.Error)
                {
                    Debug.Log("Remove: Error");
                    return;
                }
                type = 2;
                Debug.Log("Remove operationId: " + operationId);
                packageId = removeInputStr;
                ResetView();
            }
            removeInputStr = GUILayout.TextField(removeInputStr, 30);
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.Label("Select a  name@version and press this button to search for the package on the registry.");
            GUILayout.Label("*Note*: If the version is omitted, the latest version is returned.");
            GUILayout.BeginHorizontal();
            searchInputStr = GUILayout.TextField(searchInputStr, 30);
            if (GUILayout.Button(searchButton, GUILayout.Width(120)))
            {
                StatusCode code = Client.Search(out operationId, searchInputStr);
                if (code == StatusCode.Error)
                {
                    Debug.Log("Search: Error");
                    return;
                }
                type = 3;
                Debug.Log("Search operationId: " + operationId);
                ResetView();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.Label("Press this button to get the list of outdated packages");
            if (GUILayout.Button(outdatedButton, GUILayout.Width(120)))
            {
                StatusCode code = Client.Outdated(out operationId);
                if (code == StatusCode.Error)
                {
                    Debug.Log("Outdated: Error");
                    return;
                }
                type = 4;
                Debug.Log("Outdated operationId: " + operationId);
                ResetView();
            }
            GUILayout.Space(20);

            if (operationRunning)
            {
                GUILayout.Label(progression.Render());
            }
            else
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                if (operationError)
                {
                    GUI.color = Color.red;
                }
                GUILayout.Label(resultText);
                GUILayout.EndScrollView();
            }
        }

        void Update()
        {
            if (operationRunning == true)
            {
                if (progression.Tick())
                {
                    Repaint();
                }

                //operation is running let's get the target
                StatusCode lCode = Client.GetOperationStatus(operationId);
                if (lCode == StatusCode.InProgress)
                    Debug.Log("OperationID " + operationId + " -> In Progress!");
                else if (lCode == StatusCode.Done)
                {
                    sb.Clear();
                    switch (type)
                    {
                        case 0:
                        {
                            OperationStatus operationStatus = Client.GetListOperationData(operationId);
                            OperationStatusToStringBuffer(operationStatus);
                            break;
                        }
                        case 1:
                        {
                            UpmPackageInfo packageInfo = Client.GetAddOperationData(operationId);
                            UpmPackageInfoToStringBuffer(packageInfo, false);
                            break;
                        }
                        case 2:
                        {
                            String result = Client.GetRemoveOperationData(operationId);
                            if (result == "ok")
                            {
                                sb.Append("Package `" + packageId + "` was removed!");
                            }
                            else
                            {
                                sb.Append("There was an error. Package was not removed!");
                            }
                            break;
                        }
                        case 3:
                        {
                            UpmPackageInfo[] packageList = Client.GetSearchOperationData(operationId);
                            sb.Append("Found " + packageList.Length + " packages: ");
                            sb.AppendLine();
                            for (int i = 0; i < packageList.Length; ++i)
                            {
                                UpmPackageInfo packageInfo = packageList[i];
                                UpmPackageInfoToStringBuffer(packageInfo, false);
                            }
                            break;
                        }
                        case 4:
                        {
                            Dictionary<string, OutdatedPackage> outdated = Client.GetOutdatedOperationData(operationId);
                            sb.Append("Found " + outdated.Count + " outdated packages: ");
                            sb.AppendLine();
                            foreach (KeyValuePair<string, OutdatedPackage> package in outdated)
                            {
                                sb.Append(package.Key + ":");
                                sb.AppendLine();
                                sb.Append("Current: ");
                                sb.AppendLine();
                                UpmPackageInfoToStringBuffer(package.Value.current, true);
                                sb.Append("Latest: ");
                                sb.AppendLine();
                                UpmPackageInfoToStringBuffer(package.Value.latest, true);
                                sb.AppendLine();
                            }
                            break;
                        }
                        default:
                        {
                            Debug.Log("Type Not Supported");
                            break;
                        }
                    }
                    resultText = sb.ToString();
                    Debug.Log("OperationID " + operationId + " -> Done!");
                    Repaint();
                    operationRunning = false;
                }
                else if (lCode == StatusCode.Error)
                {
                    sb.Clear();
                    Error error = Client.GetOperationError(operationId);
                    sb.Append("Error: " + error.message);
                    resultText = sb.ToString();
                    Debug.Log("operationID " + operationId + " -> Error!");
                    operationError = true;
                    operationRunning = false;
                    Repaint();
                }
                else if (lCode == StatusCode.NotFound)
                {
                    Debug.Log("Operation Not Found");
                    operationRunning = false;
                }
            }
        }
    }
} // namespace

#endif
