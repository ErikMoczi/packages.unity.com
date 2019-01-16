using NUnit.Framework;
using Unity.MemoryProfiler.Editor.Extensions.String;

namespace Unity.MemoryProfiler.Editor.Tests
{
    [TestFixture]
    public class StringExtensionsTests
    {
        const string source = "Dream as if you'll live forever. Live as if you'll die today.";
        const int presentKeyMultipleFirstIndex = 6;
        const string presentKeyMultiple = "as";
        const string missingKey = "pie";
        const string presentKeyAtStart = "Dream";
        const string presentKeySingle = "die";
        const string presentKeyAtEnd = "today";
        const string presentSingleCharKey = ".";

        [Test]
        public void IndexOf_WithMultipleOccurencesOfThePattern_ReturnsIndexFirstOccurrence()
        {
            int keyIndex = source.IndexOf(0, presentKeyMultiple);
            Assert.AreEqual(presentKeyMultipleFirstIndex, keyIndex);
            Assert.AreEqual(presentKeyMultiple, source.Substring(keyIndex, presentKeyMultiple.Length));
        }

        [Test]
        public void IndexOf_WithOnlyOneOccurenceOfThePattern_ReturnsIndexToOccurrence()
        {
            int keyIndex = source.IndexOf(0, presentKeySingle);
            Assert.AreNotEqual(-1, keyIndex);
            Assert.AreEqual(presentKeySingle, source.Substring(keyIndex, presentKeySingle.Length));
        }

        [Test]
        public void IndexOf_WithOnlyOneOccurenceOfThePatternAtTheStart_ReturnsIndexToOccurrence()
        {
            int keyIndex = source.IndexOf(0, presentKeyAtStart);
            Assert.AreNotEqual(-1, keyIndex);
            Assert.AreEqual(presentKeyAtStart, source.Substring(keyIndex, presentKeyAtStart.Length));
        }

        [Test]
        public void IndexOf_WithOnlyOneOccurenceOfThePatternAtTheEnd_ReturnsIndexToOccurrence()
        {
            int keyIndex = source.IndexOf(0, presentKeyAtEnd);
            Assert.AreNotEqual(-1, keyIndex);
            Assert.AreEqual(presentKeyAtEnd, source.Substring(keyIndex, presentKeyAtEnd.Length));
        }

        [Test]
        public void IndexOf_WithEmptyPattern_ReturnsInvalidIndex()
        {
            int keyIndex = source.IndexOf(0, string.Empty);
            Assert.AreEqual(-1, keyIndex);
        }

        [Test]
        public void IndexOf_WithEmptyPatternAndSource_ThrowsIndexOutOfRangeException()
        {
            Assert.Throws(typeof(System.IndexOutOfRangeException), 
                () => {
                    string.Empty.IndexOf(0, string.Empty);
                });
        }

        [Test]
        public void IndexOf_WithEmptySource_ThrowsIndexOutOfRangeException()
        {
            Assert.Throws(typeof(System.IndexOutOfRangeException),
                () =>
                {
                    string.Empty.IndexOf(0, presentSingleCharKey);
                });
        }

        [Test]
        public void IndexOf_WithOffsetGreaterThanSourceLength_ThrowsIndexOutOfRangeException()
        {
            Assert.Throws(typeof(System.IndexOutOfRangeException),
                () =>
                {
                    source.IndexOf(source.Length, presentSingleCharKey);
                });
        }

        [Test]
        public void IndexOf_WithOffsetLessThanZero_ThrowsIndexOutOfRangeException()
        {
            Assert.Throws(typeof(System.IndexOutOfRangeException),
                () =>
                {
                    source.IndexOf(-1, presentSingleCharKey);
                });
        }

        [Test]
        public void IndexOf_WithPatternGreaterThanSource_ThrowsArgumentException()
        {
            Assert.Throws(typeof(System.ArgumentException),
                () =>
                {
                    string largePattern = source + "I will throw an exception";
                    source.IndexOf(0, largePattern);
                });
        }

        [Test]
        public void IndexOf_WithSingleCharacterPattern_ReturnsIndexToFirstOccurrence()
        {
            int keyIndex = source.IndexOf(0, presentSingleCharKey);
            Assert.AreNotEqual(-1, keyIndex);
            Assert.AreEqual(presentSingleCharKey, source.Substring(keyIndex, presentSingleCharKey.Length));
        }

        [Test]
        public void IndexOf_WithPatternNotPresentInSource_ReturnsInvalidIndex()
        {
            int keyIndex = source.IndexOf(0, missingKey);
            Assert.AreEqual(-1, keyIndex);
        }

        [Test]
        public void IndexOf_WithSourceHavingBlocksEndingWithTheLastLetterOfThePattern_ReturnsFirstOccurenceOfthePattern()
        {
            string source_blocks = "ABABCBABCBABCBAB";
            string key = "ABCB";

            int keyIndex = source_blocks.IndexOf(0, key);
            Assert.AreNotEqual(-1, keyIndex);
            Assert.AreEqual(key, source_blocks.Substring(keyIndex, key.Length));
        }

        [Test]
        public void IndexOf_WithPatternHavingMatchingLetterBlocksAtBothStartAndEnd_ReturnsIndexToFirstOccurence()
        {
            string source_blocks_matching = "TS.--AT-THAT-AB";
            string key = "AT-THAT";

            int keyIndex = source_blocks_matching.IndexOf(0, key);
            Assert.AreNotEqual(-1, keyIndex);
            Assert.AreEqual(key, source_blocks_matching.Substring(keyIndex, key.Length));
        }

        [Test]
        public void IndexOf_WithOffsetPriorToOccurrence_ReturnsIndexToOccurrence()
        {
            int offset = 10; //after the first occurrence of the key "as"
            int keyIndex = source.IndexOf(offset, presentKeyMultiple);
            Assert.AreNotEqual(-1, keyIndex);
            Assert.AreEqual(presentKeyMultiple, source.Substring(keyIndex, presentKeyMultiple.Length));
        }

        [Test]
        public void IndexOf_WithOffsetAfterAllOccurences_ReturnsInvalidIndex()
        {
            int offset = source.Length - presentKeyAtEnd.Length; //after the second occurrence of the key "as"
            int keyIndex = source.IndexOf(offset, presentKeyMultiple);
            Assert.AreEqual(-1, keyIndex);
        }

        [Test]
        public void IndexOf_WithPatternOccuringAfterTheFirstLetterInSource_ReturnsIndexToOccurrence()
        {
            string key = "CDDD";
            string source_withPatternOccuringAfterOneLetter = "ACDDDA";
            int keyIndex = source_withPatternOccuringAfterOneLetter.IndexOf(0, key);
            Assert.AreNotEqual(-1, keyIndex);
            Assert.AreEqual(key, source_withPatternOccuringAfterOneLetter.Substring(keyIndex, key.Length));
        }
    }
}
