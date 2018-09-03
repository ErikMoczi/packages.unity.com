using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.Collections;
using UnityEngine.Experimental.U2D;

// ToDo: Once we migrate to ECS, migrate all native tests to this class too.
public class SpriteShapeTests
{
    private GameObject go;
    private SpriteShapeController spriteShapeController;

    [SetUp]
    public void Setup()
    {
        go = new GameObject("TestObject");
        spriteShapeController = go.AddComponent<SpriteShapeController>();
    }

    [TearDown]
    public void Teardown()
    {
        GameObject.DestroyImmediate(go);
    }

    [Test, Description("(case 1033772) Crash when changing Spline Control Points for a Sprite Shape Controller in debug Inspector")]
    public void InvalidShapeControlPoints_DoesNotGenerateSpriteShape()
    {
        SerializedObject splineSO = new SerializedObject(spriteShapeController);
        SerializedProperty sp = splineSO.FindProperty("m_Spline.m_ControlPoints"); ;

        // This will insert default points with (0,0,0) pos just like what happens in the Inspector for the case mentioned above.
        sp.arraySize = 10;
        splineSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(spriteShapeController);
        spriteShapeController.BakeMesh();
        LogAssert.Expect(LogType.Warning, "Control points 3 & 4 are too close to each other. SpriteShape will not be generated.");
    }
}
