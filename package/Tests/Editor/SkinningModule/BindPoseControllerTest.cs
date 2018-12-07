/*
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor;
using NSubstitute;
using NUnit.Framework.Constraints;

namespace UnityEditor.Experimental.U2D.Animation.Test.MeshModule.BindPose
{
    [TestFixture]
    public class BindPoseControllerTest
    {
        private SpriteMeshData m_SpriteMeshData;
        private BindPoseController m_BindPoseController;
        private IBindPoseView m_View;
        private ISelection<int> m_Selection;
        private IUndoObject m_UndoObject;

        private int m_HoveredBone;
        List<int> m_SelectedBones;

        [SetUp]
        public void Setup()
        {
            m_HoveredBone = -1;

            m_View = Substitute.For<IBindPoseView>();
            m_View.hoveredBone.Returns(x => m_HoveredBone);

            m_SelectedBones = new List<int>();
            m_Selection = Substitute.For<ISelection<int>>();
            m_Selection.Count.Returns(x => m_SelectedBones.Count);
            m_Selection.Contains(Arg.Any<int>()).Returns(x => m_SelectedBones.Contains((int)x[0]));
            m_Selection.activeElement.Returns(x =>
                {
                    if (m_SelectedBones.Count == 0)
                        return -1;
                    return m_SelectedBones[0];
                });

            m_UndoObject = Substitute.For<IUndoObject>();

            m_SpriteMeshData = new SpriteMeshData();
            m_SpriteMeshData.frame = new Rect(0f, 0f, 100f, 100f);
            m_SpriteMeshData.bones = CreateSpriteBoneData();
            m_BindPoseController = new BindPoseController();
            m_BindPoseController.spriteMeshData = m_SpriteMeshData;
            m_BindPoseController.bindPoseView = m_View;
            m_BindPoseController.selection = m_Selection;
            m_BindPoseController.undoObject = m_UndoObject;
        }

        private List<SpriteBoneData> CreateSpriteBoneData()
        {
            var spriteBones = new SpriteBone[2]
            {
                new SpriteBone()
                {
                    name = "root",
                    parentId = -1,
                    position = Vector2.one,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 30.0f),
                    length = 1.0f
                },
                new SpriteBone()
                {
                    name = "child1",
                    parentId = 0,
                    position = Vector3.up,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 60.0f),
                    length = 1.5f
                }
            };

            return MeshModuleUtility.CreateSpriteBoneData(spriteBones.ToList(), Matrix4x4.identity);
        }

        private void AssertQuaternion(Quaternion expected, Quaternion q)
        {
            Assert.AreEqual(expected.x, q.x, 0.01f, "Quaternion x is different");
            Assert.AreEqual(expected.y, q.y, 0.01f, "Quaternion y is different");
            Assert.AreEqual(expected.z, q.z, 0.01f, "Quaternion z is different");
            Assert.AreEqual(expected.w, q.w, 0.01f, "Quaternion w is different");
        }

        private void AssertVector(Vector2 expected, Vector2 v)
        {
            Assert.AreEqual(expected.x, v.x, 0.01f, "Vector x is different");
            Assert.AreEqual(expected.y, v.y, 0.01f, "Vector y is different");
        }

        [Test]
        public void RotateBone_NoSingleBoneSelected_ThrowsException()
        {
            Vector2 lookAtPosition;
            m_View.DoRotateBone(out lookAtPosition).Returns(true);

            Assert.Throws<ArgumentException>(() => m_BindPoseController.DoBoneGUI());
        }

        [Test]
        public void RotateBone_SingleBoneSelected_SetsLocalRotation()
        {
            m_SelectedBones.Add(1);

            m_View.IsActionTriggering(BindPoseAction.RotateBone).Returns(true);
            m_View.IsActionFinishing(BindPoseAction.RotateBone).Returns(true);

            Vector2 lookAtPosition;
            m_View.DoRotateBone(out lookAtPosition).Returns(x =>
                {
                    x[0] = Vector2.down * 10f;
                    return true;
                });

            m_BindPoseController.DoBoneGUI();

            AssertQuaternion(new Quaternion(0f, 0f, 0.87f, -0.48f), m_SpriteMeshData.bones[1].localRotation);
            m_UndoObject.Received(1).RegisterCompleteObjectUndo(Arg.Any<string>());
            m_UndoObject.Received(1).RevertAllInCurrentGroup();
        }

        [Test]
        public void MoveBone_NoSingleBoneSelected_ThrowsException()
        {
            Vector2 position;
            m_View.DoMoveBone(out position).Returns(true);

            Assert.Throws<ArgumentException>(() => m_BindPoseController.DoBoneGUI());
        }

        [Test]
        public void MoveBone_SingleBoneSelected_SetsLocalPosition()
        {
            m_SelectedBones.Add(1);

            m_View.IsActionTriggering(BindPoseAction.MoveBone).Returns(true);
            m_View.IsActionFinishing(BindPoseAction.MoveBone).Returns(true);

            Vector2 worldPosition;
            m_View.DoMoveBone(out worldPosition).Returns(x =>
                {
                    x[0] = Vector2.down * 10f;
                    return true;
                });

            m_BindPoseController.DoBoneGUI();

            AssertVector(new Vector2(-6.36f, -9.02f), m_SpriteMeshData.bones[1].localPosition);
            m_UndoObject.Received(1).RegisterCompleteObjectUndo(Arg.Any<string>());
            m_UndoObject.Received(1).RevertAllInCurrentGroup();
        }

        [Test]
        public void SelectBone_SelectsHoveredBone()
        {
            m_HoveredBone = 1;

            m_View.DoSelectBone().Returns(true);

            m_BindPoseController.DoBoneGUI();

            m_UndoObject.Received(1).RegisterCompleteObjectUndo("Select Bone");
            m_UndoObject.Received(1).IncrementCurrentGroup();
            m_Selection.Received(1).Clear();
            m_Selection.Received(1).Select(1, true);
        }
    }
}
*/
