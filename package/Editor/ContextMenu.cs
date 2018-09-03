using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;

namespace UnityEditor.U2D
{
    public class ContextMenu
    {
        private static AngleRange CreateAngleRange(float start, float end, int order)
        {
            AngleRange angleRange = new AngleRange();

            angleRange.start = start;
            angleRange.end = end;
            angleRange.order = order;

            return angleRange;
        }

        [MenuItem("Assets/Create/Sprite Shape Profile/Empty")]
        public static void CreateNewEmptySpriteShape()
        {
            SpriteShapeEditorUtility.CreateSpriteShapeAsset();
        }

        [MenuItem("Assets/Create/Sprite Shape Profile/Strip")]
        public static void CreateNewSpriteStrip()
        {
            SpriteShape newSpriteShape = SpriteShapeEditorUtility.CreateSpriteShapeAsset();
            newSpriteShape.angleRanges.Add(CreateAngleRange(-180.0f, 180.0f, 0));
        }

        [MenuItem("Assets/Create/Sprite Shape Profile/Shape")]
        public static void CreateNewSpriteShape()
        {
            SpriteShape newSpriteShape = SpriteShapeEditorUtility.CreateSpriteShapeAsset();
            newSpriteShape.angleRanges.Add(CreateAngleRange(-22.5f, 22.5f, 7));
            newSpriteShape.angleRanges.Add(CreateAngleRange(22.5f, 67.5f, 6));
            newSpriteShape.angleRanges.Add(CreateAngleRange(67.5f, 112.5f, 4));
            newSpriteShape.angleRanges.Add(CreateAngleRange(112.5f, 157.5f, 2));
            newSpriteShape.angleRanges.Add(CreateAngleRange(157.5f, 202.5f, 8));
            newSpriteShape.angleRanges.Add(CreateAngleRange(-157.5f, -112.5f, 1));
            newSpriteShape.angleRanges.Add(CreateAngleRange(-112.5f, -67.5f, 3));
            newSpriteShape.angleRanges.Add(CreateAngleRange(-67.5f, -22.5f, 5));
        }

        [MenuItem("GameObject/2D Object/Sprite Shape")]
        internal static void CreateSpriteShapeEmpty()
        {
            SpriteShapeEditorUtility.SetShapeFromAsset(SpriteShapeEditorUtility.CreateSpriteShapeControllerFromSelection());
        }
    }
}
