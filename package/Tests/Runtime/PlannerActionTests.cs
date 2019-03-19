//using System;
//using System.Linq;
//using NUnit.Framework;
//using Unity.AI.Planner.DomainLanguage.TraitBased;
//
//namespace Unity.AI.Planner.Tests.KeyDomain
//{
//    [TestFixture]
//    class PlannerActionTests
//    {
//        [Test]
//        public void BindOneAction()
//        {
//            var domain = new KeyDomain();
//            var state = (State)domain.InitialState();
//
//            // Act
//            var action = new MoveAction();
//            var actions = action.BindArguments(state).ToList();
//
//            // Test
//            Assert.AreEqual(1, actions.Count);
//        }
//
//        [Test]
//        public void CompareCopiedAction()
//        {
//            var action = new MoveAction();
//
//            // Bind one argument to make sure that gets copied properly
//            action.Arguments[0] = new DomainObject(new LocalizedTrait(), new CarrierTrait());
//            var actionCopy = action.Copy();
//
//            Assert.AreEqual(action, actionCopy);
//        }
//
//        [Test]
//        public void CompareDifferentActions()
//        {
//            var action1 = new MoveAction();
//            var action2 = new PickupKeyAction();
//
//            Assert.AreNotEqual(action1, action2);
//        }
//
//        [Test]
//        public void BindMultiplePermutationsOfAction()
//        {
//            var domain = new KeyDomain();
//            var state = (State)domain.InitialState();
//            state.AddObject(KeyDomain.MakeRoom(Color.White, 2)); // Add an additional room, so we have 3 total and 2 possible rooms to move to
//
//            // Act
//            var action = new MoveAction();
//            var actions = action.BindArguments(state).ToList();
//
//            // Test
//            Assert.AreEqual(2, actions.Count);
//            Assert.AreNotEqual(actions[0], actions[1]);
//        }
//
//        [Test]
//        public void BindNoActions()
//        {
//            var state = new State();
//
//            // Act
//            var action = new MoveAction();
//            var actions = action.BindArguments(state).ToList();
//
//            // Test
//            Assert.AreEqual(0, actions.Count);
//        }
//    }
//}

using System;
