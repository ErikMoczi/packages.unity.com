using NUnit.Framework;
using NSubstitute;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.Experimental.U2D.Common;

namespace UnityEditor.U2D
{
    public class AngleRangeControllerTests
    {
        private IAngleRangeCache m_Cache;
        private AngleRangeController m_Controller;
        private IAngleRangeView m_View;
        private List<AngleRange> m_AngleRanges;
        private int m_HoveredIndex;
        private float m_AngleFromPosition;

        [SetUp]
        public void SetUp()
        {
            SetupCache();
            SetupView();
            SetupController();
        }

        private void SetupCache()
        {
            m_AngleRanges = new List<AngleRange>();

            m_Cache = Substitute.For<IAngleRangeCache>();
            m_Cache.angleRanges.Returns(x => { return m_AngleRanges; });
            m_Cache.previewAngle = 0f;
            m_Cache.selectedIndex = -1;
        }

        private void SetupView()
        {
            m_HoveredIndex = -1;
            m_AngleFromPosition = 0f;
            m_View = Substitute.For<IAngleRangeView>();
            m_View.hoveredRangeIndex.Returns(x => { return m_HoveredIndex; });
            m_View.GetAngleFromPosition(Arg.Any<Rect>(), Arg.Any<float>()).Returns(x => { return m_AngleFromPosition; });
        }

        private void SetupController()
        {
            Debug.Assert(m_Cache != null);
            Debug.Assert(m_View != null);

            m_Controller = new AngleRangeController();
            m_Controller.view = m_View;
            m_Controller.cache = m_Cache;
        }

        private void AddRange(float start, float end)
        {
            m_AngleRanges.Add(
                new AngleRange()
            {
                start = start,
                end = end
            }
                );
        }

        [Test]
        public void CreateSingleRangeIs90DegAndSelected()
        {
            m_AngleFromPosition = 0f;
            m_View.IsActionActive(AngleRangeAction.CreateRange).Returns(true);
            m_View.DoCreateRange(Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>()).Returns(true);

            m_Controller.OnGUI();

            Assert.AreEqual(1, m_AngleRanges.Count);
            Assert.AreEqual(90f, m_AngleRanges[0].end - m_AngleRanges[0].start);
            Assert.AreEqual(0, m_Cache.selectedIndex);
            m_Cache.Received().RegisterUndo(Arg.Any<string>());
        }

        [Test]
        public void CreateRangeInBetweenTwoRangesFillsTheGapIfGapIsLessThan90Deg()
        {
            AddRange(0f, 10f);
            AddRange(20f, 30f);

            m_AngleFromPosition = 15f;
            m_View.IsActionActive(AngleRangeAction.CreateRange).Returns(true);
            m_View.DoCreateRange(Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>()).Returns(true);

            m_Controller.OnGUI();

            Assert.AreEqual(3, m_AngleRanges.Count);
            Assert.AreEqual(10, m_AngleRanges[2].start);
            Assert.AreEqual(20, m_AngleRanges[2].end);
        }

        [Test]
        public void CreateRangeInBetweenTwoRangesDoesntFillTheGapIfGapIsGreaterThan90Deg()
        {
            AddRange(-60f, -50f);
            AddRange(50f, 60f);

            m_AngleFromPosition = 0f;
            m_View.IsActionActive(AngleRangeAction.CreateRange).Returns(true);
            m_View.DoCreateRange(Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>()).Returns(true);

            m_Controller.OnGUI();

            Assert.AreEqual(3, m_AngleRanges.Count);
            Assert.AreEqual(-45f, m_AngleRanges[2].start);
            Assert.AreEqual(45f, m_AngleRanges[2].end);
        }

        [Test]
        public void CreateRangeInBetweenTwoRangesFillsTheGapIfGapIsLessThan90DegWrapAround180()
        {
            AddRange(100f, 145f);
            AddRange(-145f, -100f);

            m_AngleFromPosition = -170f;
            m_View.IsActionActive(AngleRangeAction.CreateRange).Returns(true);
            m_View.DoCreateRange(Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>()).Returns(true);

            m_Controller.OnGUI();

            Assert.AreEqual(3, m_AngleRanges.Count);
            Assert.AreEqual(-215f, m_AngleRanges[2].start);
            Assert.AreEqual(-145f, m_AngleRanges[2].end);
        }

        [Test]
        public void CreateRangeInBetweenTwoRangesFillsTheGapIfGapIsLessThan90DegWrapAroundMinus180()
        {
            AddRange(100f, 145f);
            AddRange(-145f, -100f);

            m_AngleFromPosition = 170f;
            m_View.IsActionActive(AngleRangeAction.CreateRange).Returns(true);
            m_View.DoCreateRange(Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>()).Returns(true);

            m_Controller.OnGUI();

            Assert.AreEqual(3, m_AngleRanges.Count);
            Assert.AreEqual(145f, m_AngleRanges[2].start);
            Assert.AreEqual(215f, m_AngleRanges[2].end);
        }

        [Test]
        public void ModifyRange()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Any<int>(), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start;
                    x[5] = 50f;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(1, m_AngleRanges.Count);
            Assert.AreEqual(-45f, m_AngleRanges[0].start);
            Assert.AreEqual(50f, m_AngleRanges[0].end);
            m_Cache.Received().RegisterUndo(Arg.Any<string>());
        }

        [Test]
        public void ModifyRangeEndCanBeGreaterThan180()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Any<int>(), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start;
                    x[5] = 190f;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(1, m_AngleRanges.Count);
            Assert.AreEqual(-45f, m_AngleRanges[0].start);
            Assert.AreEqual(190f, m_AngleRanges[0].end);
        }

        [Test]
        public void ModifyRangeStartAndEndGreaterThan180WrapToMinus180()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Any<int>(), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = 181f;
                    x[5] = 200f;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(1, m_AngleRanges.Count);
            Assert.AreEqual(-179f, m_AngleRanges[0].start);
            Assert.AreEqual(-160f, m_AngleRanges[0].end);
        }

        [Test]
        public void ModifyRangeStartAndEndLessThanMinus180WrapTo180()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Any<int>(), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = -200f;
                    x[5] = -181f;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(1, m_AngleRanges.Count);
            Assert.AreEqual(160f, m_AngleRanges[0].start);
            Assert.AreEqual(179f, m_AngleRanges[0].end);
        }

        [Test]
        public void IncreaseRangeStartUntilEndDeletesRange()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.IsActionFinishing(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Any<int>(), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = end;
                    x[5] = end;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(0, m_AngleRanges.Count);
        }

        [Test]
        public void DecreaseRangeEndToStartDeletesRange()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.IsActionFinishing(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Any<int>(), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start;
                    x[5] = start;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(0, m_AngleRanges.Count);
        }

        [Test]
        public void IncreaseRangeEndClampToStart()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Any<int>(), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start;
                    x[5] = end + 360f;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(1, m_AngleRanges.Count);
            Assert.AreEqual(-45f, m_AngleRanges[0].start);
            Assert.AreEqual(315f, m_AngleRanges[0].end);
        }

        [Test]
        public void DecreaseRangeStartClampToEnd()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Any<int>(), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start - 360f;
                    x[5] = end;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(1, m_AngleRanges.Count);
            Assert.AreEqual(-315f, m_AngleRanges[0].start);
            Assert.AreEqual(45f, m_AngleRanges[0].end);
        }

        [Test]
        public void RangeStartClampToPreviousRangeEnd()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);
            AddRange(50f, 80f);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Is(0), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start - 360f;
                    x[5] = end;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(2, m_AngleRanges.Count);
            Assert.AreEqual(-280f, m_AngleRanges[0].start);
            Assert.AreEqual(45f, m_AngleRanges[0].end);
        }

        [Test]
        public void RangeEndClampToNextRangeStart()
        {
            var start = -45f;
            var end = 45f;
            AddRange(start, end);
            AddRange(50f, 80f);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Is(0), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start;
                    x[5] = end + 360f;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(2, m_AngleRanges.Count);
            Assert.AreEqual(-45f, m_AngleRanges[0].start);
            Assert.AreEqual(50f, m_AngleRanges[0].end);
        }

        [Test]
        public void RangeEndGraterThan180ClampToOtherRangeStartGreaterThanMinus180()
        {
            var start = 50f;
            var end = 181f;
            AddRange(-45, 45);
            AddRange(50f, 181f);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Is(1), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start;
                    x[5] = end + 360f;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(2, m_AngleRanges.Count);
            Assert.AreEqual(50f, m_AngleRanges[1].start);
            Assert.AreEqual(315f, m_AngleRanges[1].end);
        }

        [Test]
        public void RangeStartLessThanMinus180ClampToOtherRangeEndGreaterThanMinus180()
        {
            var start = -181f;
            var end = 45f;
            AddRange(-181, 45);
            AddRange(50f, 80f);

            m_AngleFromPosition = 0f;
            m_View.IsActionTriggering(AngleRangeAction.ModifyRange).Returns(true);
            m_View.DoAngleRange(Arg.Is(0), Arg.Any<Rect>(), Arg.Any<float>(), Arg.Any<float>(), ref start, ref end, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Color>(), Arg.Any<Color>(), Arg.Any<Color>()).Returns(x =>
                {
                    x[4] = start - 360f;
                    x[5] = end;
                    return true;
                });

            m_Controller.OnGUI();

            Assert.AreEqual(2, m_AngleRanges.Count);
            Assert.AreEqual(-280f, m_AngleRanges[0].start);
            Assert.AreEqual(45f, m_AngleRanges[0].end);
        }

        [Test]
        public void RemoveRange()
        {
            AddRange(-45f, 45f);
            m_Cache.selectedIndex = 0;
            m_View.DoRemoveRange().Returns(true);

            m_Controller.OnGUI();

            Assert.AreEqual(0, m_AngleRanges.Count);
            Assert.AreEqual(-1, m_Cache.selectedIndex);
            m_Cache.Received().RegisterUndo(Arg.Any<string>());
        }
    }
}
