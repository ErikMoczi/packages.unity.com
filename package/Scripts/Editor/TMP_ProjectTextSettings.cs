using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace UnityEditor.TextCore
{

    public static class TMP_ProjectTextSettings
    {

        // Open Project Text Settings
        [MenuItem("Edit/Project Settings/Text", false, 300)]
        public static void SelectProjectTextSettings()
        {
            TMP_Settings textSettings = TMP_Settings.instance;
            Selection.activeObject = textSettings;

            // TODO: Do we want to ping the Project Text Settings asset in the Project Inspector
            //EditorUtility.FocusProjectWindow();
            //EditorGUIUtility.PingObject(textSettings);
        }
    }
}
