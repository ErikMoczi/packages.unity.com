using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;
using UnityTexture2D = UnityEngine.Texture2D;

namespace UnityEditor.U2D
{
    [InitializeOnLoad]
    public static class SceneDragAndDrop
    {
        static SceneDragAndDrop()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        static class Contents
        {
            public static readonly string createString = "Create Sprite Shape";
        }

        static List<Object> s_SceneDragObjects;
        static DragType s_DragType;
        enum DragType { NotInitialized, CreateMultiple }

        public delegate string ShowFileDialogDelegate(string title, string defaultName, string extension, string message, string defaultPath);

        static void OnSceneGUI(SceneView sceneView)
        {
            HandleSceneDrag(sceneView, Event.current, DragAndDrop.objectReferences, DragAndDrop.paths);
        }

        static List<SpriteShape> GetSpriteShapeFromPathsOrObjects(Object[] objects, string[] paths, EventType currentEventType)
        {
            List<SpriteShape> result = new List<SpriteShape>();

            foreach (Object obj in objects)
            {
                if (AssetDatabase.Contains(obj))
                {
                    if (obj is SpriteShape)
                        result.Add(obj as SpriteShape);
                }
            }
            return result;
        }

        static void HandleSceneDrag(SceneView sceneView, Event evt, Object[] objectReferences, string[] paths)
        {
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform && evt.type != EventType.DragExited)
                return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                {
                    DragType newDragType = DragType.CreateMultiple;

                    if (s_DragType != newDragType || s_SceneDragObjects == null)
                    // Either this is first time we are here OR evt.alt changed during drag
                    {
                        if (ExistingAssets(objectReferences))     // External drag with images that are not in the project
                        {
                            List<SpriteShape> assets = GetSpriteShapeFromPathsOrObjects(objectReferences, paths,
                                    evt.type);

                            if (assets.Count == 0)
                                return;

                            if (s_DragType != DragType.NotInitialized)
                                // evt.alt changed during drag, so we need to cleanup and start over
                                CleanUp(true);

                            s_DragType = newDragType;
                            CreateSceneDragObjects(assets);
                        }
                    }

                    if (s_SceneDragObjects != null)
                    {
                        PositionSceneDragObjects(s_SceneDragObjects, sceneView, evt.mousePosition);

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        evt.Use();
                    }
                }
                break;
                case EventType.DragPerform:
                {
                    List<SpriteShape> assets = GetSpriteShapeFromPathsOrObjects(objectReferences, paths, evt.type);

                    if (assets.Count > 0 && s_SceneDragObjects != null)
                    {
                        // For external drags, we have delayed all creation to DragPerform because only now we have the imported sprite assets
                        if (s_SceneDragObjects.Count == 0)
                        {
                            CreateSceneDragObjects(assets);
                            PositionSceneDragObjects(s_SceneDragObjects, sceneView, evt.mousePosition);
                        }

                        foreach (GameObject dragGO in s_SceneDragObjects)
                        {
                            Undo.RegisterCreatedObjectUndo(dragGO, "Create Shape");
                            dragGO.hideFlags = HideFlags.None;
                        }

                        Selection.objects = s_SceneDragObjects.ToArray();

                        CleanUp(false);
                        evt.Use();
                    }
                }
                break;
                case EventType.DragExited:
                {
                    if (s_SceneDragObjects != null)
                    {
                        CleanUp(true);
                        evt.Use();
                    }
                }
                break;
            }
        }

        static void PositionSceneDragObjects(List<Object> objects, SceneView sceneView, Vector2 mousePosition)
        {
            Vector3 position = Vector3.zero;
            position = HandleUtility.GUIPointToWorldRay(mousePosition).GetPoint(10);
            if (sceneView.in2DMode)
            {
                position.z = 0f;
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(mousePosition));
                if (hit != null)
                {
                    RaycastHit rh = (RaycastHit)hit;
                    position = rh.point;
                }
            }

            foreach (GameObject gameObject in objects)
            {
                gameObject.transform.position = position;
            }
        }

        static void CreateSceneDragObjects(List<SpriteShape> shapes)
        {
            if (s_SceneDragObjects == null)
                s_SceneDragObjects = new List<Object>();

            if (s_DragType == DragType.CreateMultiple)
            {
                foreach (SpriteShape sprite in shapes)
                    s_SceneDragObjects.Add(CreateDragGO(sprite, Vector3.zero));
            }
            else
            {
                s_SceneDragObjects.Add(CreateDragGO(shapes[0], Vector3.zero));
            }
        }

        static void CleanUp(bool deleteTempSceneObject)
        {
            if (deleteTempSceneObject)
            {
                foreach (GameObject gameObject in s_SceneDragObjects)
                    Object.DestroyImmediate(gameObject, false);
            }

            if (s_SceneDragObjects != null)
            {
                s_SceneDragObjects.Clear();
                s_SceneDragObjects = null;
            }

            s_DragType = DragType.NotInitialized;
        }

        static bool ExistingAssets(Object[] objects)
        {
            foreach (Object obj in objects)
            {
                if (AssetDatabase.Contains(obj))
                    return true;
            }
            return false;
        }

        static GameObject CreateDragGO(SpriteShape spriteShape, Vector3 position)
        {
            SpriteShapeController spriteShapeController = SpriteShapeEditorUtility.CreateSpriteShapeController();
            GameObject gameObject = spriteShapeController.gameObject;
            gameObject.transform.position = position;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            spriteShapeController.spriteShape = spriteShape;

            SpriteShapeEditorUtility.SetShapeFromAsset(spriteShapeController);

            return gameObject;
        }
    }
}
