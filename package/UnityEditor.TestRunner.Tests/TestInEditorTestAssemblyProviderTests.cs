using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Components.DictionaryAdapter;
using Moq;
using NUnit.Framework;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.TestTools.TestRunner;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

public class TestInEditorTestAssemblyProviderTests
{
    private List<PrecompiledAssembly> m_PrecompiledAssemblies;
    private EditorLoadedTestAssemblyProvider m_EditorLoadedTestAssemblyProvider;
    private const string k_NunitAssemblyName = "nunit.framework";
    private const string k_TestRunnerAssemblyName = "UnityEngine.TestRunner";

    [SetUp]
    public void Setup()
    {
        List<TestAssembly> m_AllAssemblies = new List<TestAssembly>
        {
            new TestAssembly { Path = "SomeUnityEditorAssembly.dll", IsPrecompiled = false, ReferencesTestAssemblies = false, Flags = AssemblyFlags.EditorOnly},
            new TestAssembly { Path = "SomeUnityAssembly.dll", IsPrecompiled = false, ReferencesTestAssemblies = false},
            new TestAssembly { Path = "SomePath/SomeNormalPrecompiledUserAssembly.dll", IsPrecompiled = true, ReferencesTestAssemblies = false},
            new TestAssembly { Path = "SomePath/Neasted/SomeTestEditorOnlyPrecompiledUserAssembly.dll", IsPrecompiled = true, ReferencesTestAssemblies = true, Flags = AssemblyFlags.EditorOnly},
            new TestAssembly { Path = "SomePath/Neasted/SomeTestPrecompiledUserAssembly.dll", IsPrecompiled = true, ReferencesTestAssemblies = true},
            new TestAssembly { Path = "SomeScriptAssembly.dll", IsPrecompiled = false, ReferencesTestAssemblies = false},
            new TestAssembly { Path = "SomeTestEditorOnlyScriptAssembly.dll", IsPrecompiled = false, ReferencesTestAssemblies = true, Flags = AssemblyFlags.EditorOnly},
            new TestAssembly { Path = "SomeTestScriptAssembly.dll", IsPrecompiled = false, ReferencesTestAssemblies = true},
        };

        var editorAssembliesProxMock = new Mock<IEditorAssembliesProxy>();
        List<Mock<IAssemblyWrapper>> assemblies = m_AllAssemblies.Select(x => CreateMockedAssembly(x.Path, x.ReferencesTestAssemblies)).ToList();
        editorAssembliesProxMock.Setup(x => x.loadedAssemblies).Returns(assemblies.Select(x => x.Object).ToArray);

        var editorCompilationInterfaceProxyMock = new Mock<IEditorCompilationInterfaceProxy>();
        editorCompilationInterfaceProxyMock.Setup(x => x.GetAllEditorScriptAssemblies())
            .Returns(m_AllAssemblies.Where(x => !x.IsPrecompiled).Select(x => new ScriptAssembly() { Filename = new FileInfo(x.Path).Name, Flags = x.Flags }).ToArray());

        editorCompilationInterfaceProxyMock.Setup(x => x.GetAllPrecompiledAssemblies())
            .Returns(m_AllAssemblies.Where(x => x.IsPrecompiled).Select(x => new PrecompiledAssembly() { Path = x.Path, Flags = x.Flags }).ToArray());

        m_EditorLoadedTestAssemblyProvider = new EditorLoadedTestAssemblyProvider(editorCompilationInterfaceProxyMock.Object, editorAssembliesProxMock.Object);
    }

    private Mock<IAssemblyWrapper> CreateMockedAssembly(string path, bool referencesTest)
    {
        var assembly = new Mock<IAssemblyWrapper>();
        assembly.Setup(x => x.Location).Returns(path);

        AssemblyName[] assemblyReferences;
        if (referencesTest)
        {
            assemblyReferences = new[]
            {
                new AssemblyName(k_NunitAssemblyName),
                new AssemblyName(k_TestRunnerAssemblyName),
            };
        }
        else
        {
            assemblyReferences = new[]
            {
                new AssemblyName("RandomReference"),
            };
        }

        assembly.Setup(x => x.GetReferencedAssemblies()).Returns(assemblyReferences);

        return assembly;
    }

    [Test]
    public void GetAssembliesGroupedByType_WhenGettingEditModeAssemblies_TheCorrectListAreReturned()
    {
        var assembliesGroupedByType = m_EditorLoadedTestAssemblyProvider.GetAssembliesGroupedByType(TestPlatform.EditMode);

        Assert.AreEqual("SomeTestEditorOnlyPrecompiledUserAssembly.dll", new FileInfo(assembliesGroupedByType[0].Location).Name);
        Assert.AreEqual("SomeTestEditorOnlyScriptAssembly.dll", new FileInfo(assembliesGroupedByType[1].Location).Name);
        Assert.AreEqual(2, assembliesGroupedByType.Count);
    }

    [Test]
    public void GetAssembliesGroupedByType_WhenGettingPlayModeAssemblies_TheCorrectListAreReturned()
    {
        var assembliesGroupedByType = m_EditorLoadedTestAssemblyProvider.GetAssembliesGroupedByType(TestPlatform.PlayMode);

        Assert.AreEqual("SomeTestPrecompiledUserAssembly.dll", new FileInfo(assembliesGroupedByType[0].Location).Name);
        Assert.AreEqual("SomeTestScriptAssembly.dll", new FileInfo(assembliesGroupedByType[1].Location).Name);
        Assert.AreEqual(2, assembliesGroupedByType.Count);
    }

    private class TestAssembly
    {
        public string Path { get; set; }
        public bool ReferencesTestAssemblies { get; set; }
        public bool IsPrecompiled { get; set; }
        public AssemblyFlags Flags { get; set; }
    }
}
