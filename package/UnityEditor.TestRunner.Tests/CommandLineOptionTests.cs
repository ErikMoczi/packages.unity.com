using NUnit.Framework;
using UnityEditor.TestRunner.CommandLineParser;

namespace FrameworkTests
{
    public class CommandLineOptionTests
    {
        [Test]
        public void CommandLineOptionTriggersAction()
        {
            var name = "theName";
            var actionInvoked = false;
            var optionUnderTest = new CommandLineOption(name, () => { actionInvoked = true; });

            optionUnderTest.ApplyValue("value");

            Assert.AreEqual(name, optionUnderTest.ArgName);
            Assert.IsTrue(actionInvoked, "The action was not invoked.");
        }

        [Test]
        public void CommandLineOptionTriggersActionWithString()
        {
            var name = "theName";
            var actionInvoked = false;
            var expectedValue = "theValue";
            string actualValue = null;
            var optionUnderTest = new CommandLineOption(name, (value) =>
            {
                actionInvoked = true;
                actualValue = value;
            });

            optionUnderTest.ApplyValue(expectedValue);

            Assert.AreEqual(name, optionUnderTest.ArgName);
            Assert.IsTrue(actionInvoked, "The action was not invoked.");
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void CommandLineOptionTriggersActionWithStringSplitted()
        {
            var name = "theName";
            var actionInvoked = false;
            var listValue = "valueA;valueB;;valueD;'value E'";
            var expectedValue = new[] { "valueA", "valueB", "", "valueD", "'value E'" };
            string[] actualValue = null;
            var optionUnderTest = new CommandLineOption(name, (value) =>
            {
                actionInvoked = true;
                actualValue = value;
            });

            optionUnderTest.ApplyValue(listValue);

            Assert.AreEqual(name, optionUnderTest.ArgName);
            Assert.IsTrue(actionInvoked, "The action was not invoked.");
            CollectionAssert.AreEqual(expectedValue, actualValue);
        }
    }
}
