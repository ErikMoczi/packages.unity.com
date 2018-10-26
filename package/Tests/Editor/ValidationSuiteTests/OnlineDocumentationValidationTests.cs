using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.PackageManager.ValidationSuite.Mocks;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class OnlineDocumentationValidationTests
    {
        private const string version = "1.1.0";
        private const string previewVersion = "1.1.0-preview.1";

        [Test]
        public void When_Online_Documentation_Returns_200_Validation_Succeeds()
        {
            // arrange
            var req = new MockHttpWebRequestWrap("200");
            var http = new MockHttpWebRequestFactory(req);

            // act
            var documentationValidation = new OnlineDocumentationValidation(http);
            documentationValidation.Context = PrepareVettingContext(version);
            documentationValidation.RunTest();

            // assert
            Assert.AreEqual(TestState.Succeeded, documentationValidation.TestState);
            Assert.AreEqual(0, documentationValidation.TestOutput.Count);
        }

        [Test]
        public void When_Preview_Online_Documentation_Returns_204_Validation_Succeeds_With_Warning()
        {
            // arrange
            var req = new MockHttpWebRequestWrap("204");
            var http = new MockHttpWebRequestFactory(req);

            // act
            var documentationValidation = new OnlineDocumentationValidation(http);
            documentationValidation.Context = PrepareVettingContext(previewVersion);
            documentationValidation.RunTest();

            // assert
            Assert.AreEqual(TestState.Succeeded, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> {
                "Warning: The documentation website could not complete your request, which is required before it can be removed from \"Preview\".  Contact the documentation team for assistance. (StatusCode: NoContent)"
            };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [Test]
        public void When_Online_Documentation_Returns_204_Validation_Fails()
        {
            // arrange
            var req = new MockHttpWebRequestWrap("204");
            var http = new MockHttpWebRequestFactory(req);

            // act
            var documentationValidation = new OnlineDocumentationValidation(http);
            documentationValidation.Context = PrepareVettingContext(version);
            documentationValidation.RunTest();

            // assert
            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> {
                "Error: The documentation website could not complete your request. Please contact the docs team to ensure a site is up before you publish to production. (StatusCode: NoContent)",
                "Expected Website: https://docs.unity3d.com/Packages/@1.1/manual/index.html"
            };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [Test]
        public void When_Preview_Online_Documentation_Returns_404_Validation_Succeeds_With_Warning()
        {
            // arrange
            var req = new MockHttpWebRequestWrap("404");
            var http = new MockHttpWebRequestFactory(req);

            // act
            var documentationValidation = new OnlineDocumentationValidation(http);
            documentationValidation.Context = PrepareVettingContext(previewVersion);
            documentationValidation.RunTest();

            // assert
            Assert.AreEqual(TestState.Succeeded, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> {
                "Warning: This package contains no web based documentation, which is required before it can be removed from \"Preview\".  Contact the documentation team for assistance."
            };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [Test]
        public void When_Online_Documentation_Returns_404_Validation_Fails()
        {
            // arrange
            var req = new MockHttpWebRequestWrap("404");
            var http = new MockHttpWebRequestFactory(req);

            // act
            var documentationValidation = new OnlineDocumentationValidation(http);
            documentationValidation.Context = PrepareVettingContext(version);
            documentationValidation.RunTest();

            // assert
            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> {
                "Error: Couldn't find a documentation website for this package.  Please contact the docs team to ensure a site is up before you publish to production.",
                "Expected Website: https://docs.unity3d.com/Packages/@1.1/manual/index.html",
                "The remote server returned an error: (404) Not Found."
            };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [Test]
        public void When_Preview_Online_Documentation_Returns_500_Validation_Succeeds_With_Warning()
        {
            // arrange
            var req = new MockHttpWebRequestWrap("500");
            var http = new MockHttpWebRequestFactory(req);
            // act
            var documentationValidation = new OnlineDocumentationValidation(http);
            documentationValidation.Context = PrepareVettingContext(previewVersion);
            documentationValidation.RunTest();

            // assert
            Assert.AreEqual(TestState.Succeeded, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> {
                "Warning: This package contains no web based documentation, which is required before it can be removed from \"Preview\".  Contact the documentation team for assistance."
            };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [Test]
        public void When_Online_Documentation_Returns_500_Validation_Fails()
        {
            // arrange
            var req = new MockHttpWebRequestWrap("500");
            var http = new MockHttpWebRequestFactory(req);
            // act
            var documentationValidation = new OnlineDocumentationValidation(http);
            documentationValidation.Context = PrepareVettingContext(version);
            documentationValidation.RunTest();

            // assert
            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> {
                "Error: Couldn't find a documentation website for this package.  Please contact the docs team to ensure a site is up before you publish to production.",
                "Expected Website: https://docs.unity3d.com/Packages/@1.1/manual/index.html",
                "The remote server returned an error: (500) Internal Server error."
            };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        private VettingContext PrepareVettingContext(string version)
        {
            return new VettingContext()
            {
                ProjectPackageInfo = new VettingContext.ManifestData()
                {
                    version = version
                },
                PublishPackageInfo = new VettingContext.ManifestData()
                {
                    version = version
                },
                PreviousPackageInfo = new VettingContext.ManifestData()
                {
                    version = version
                }
            };
        }
    }
}
