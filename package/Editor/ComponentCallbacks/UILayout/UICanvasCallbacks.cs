

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyComponentCallback(
        CoreGuids.UILayout.UICanvas)]
    [UsedImplicitly]
    internal class UICanvasCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject uiCanvas)
        {
            var project = TinyEditorApplication.Project;
            if (null == project)
            {
                return;
            }

            var referenceResolution = new Vector2(project.Settings.CanvasWidth, project.Settings.CanvasHeight);
            if (referenceResolution.x == 0 || referenceResolution.y == 0)
            {
                referenceResolution.x = 1080;
                referenceResolution.y = 1920;
            }
            uiCanvas.AssignPropertyFrom("referenceResolution", referenceResolution);
            uiCanvas.AssignPropertyFrom("matchWidthOrHeight", referenceResolution.y > referenceResolution.x ? 1.0f : 0.0f);
        }
    }
}

