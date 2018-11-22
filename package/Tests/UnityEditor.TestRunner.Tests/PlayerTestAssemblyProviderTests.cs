using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEngine.TestTools.Utils;

namespace Assets.editor
{
    public class PlayerTestAssemblyProviderTests
    {
        private Mock<IAssemblyLoadProxy> m_AssemblyLoadProxyMock;
        private PlayerTestAssemblyProvider m_PlayerTestAssemblyProvider;

        [SetUp]
        public void Setup()
        {
            m_AssemblyLoadProxyMock = new Mock<IAssemblyLoadProxy>();
        }

        [Test]
        public void WhenLoadingAssembliesThenOnlyLoadableAssembliesAreLoaded()
        {
            var assemblyWrapperMock = new Mock<IAssemblyWrapper>();
            assemblyWrapperMock.Setup(y => y.Location).Returns("SomeAssembly1");

            m_AssemblyLoadProxyMock.Setup(x => x.Load("SomeAssembly1")).Returns(assemblyWrapperMock.Object);
            m_AssemblyLoadProxyMock.Setup(x => x.Load("SomeAssembly2")).Throws<FileNotFoundException>();
            m_PlayerTestAssemblyProvider = new PlayerTestAssemblyProvider(m_AssemblyLoadProxyMock.Object, new[] { "SomeAssembly1", "SomeAssembly2" }.ToList());
            var assemblyWrappers = m_PlayerTestAssemblyProvider.GetUserAssemblies();

            Assert.AreEqual(assemblyWrappers.Count, 1, "Should only contain one Element");
            Assert.AreEqual(assemblyWrappers.Single().Location, "SomeAssembly1");
        }
    }
}
