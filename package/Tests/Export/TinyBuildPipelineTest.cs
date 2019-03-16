
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class TinyBuildPipelineTest
    {
        private DirectoryInfo _scriptDir;
        private FileInfo _configFile;
	    private TinyContext _context;
        private TinyRegistry _registry;
        private TinyProject _project;
        private TinyModule _module;

        private const string k_OldId = "this is a globally unique ID made with love";
        
        [SetUp]
        public void SetUp()
        {
            _scriptDir = Directory.CreateDirectory("TestTinyScript");
            
            File.WriteAllText(Path.Combine(_scriptDir.FullName, "fake-runtime.d.ts"), @"
declare namespace ut {

	class Entity {
		hasComponent<T>(): boolean;
	}
	class World {
		forEachEntity<T>(callback: (entity: Entity, c: T) => void): void;
		forEachEntity<T1, T2>(callback: (entity: Entity, c1: T1, c2: T2) => void): void;
		forEachEntity<T1, T2, T3>(callback: (entity: Entity, c1: T1, c2: T2, c3: T3) => void): void;
	}
	class Scheduler {}

	class System {}

	abstract class Component {}

	abstract class EntityFilter {}

	abstract class ComponentSystem {
		scheduler: Scheduler;
		world: World;

		abstract OnUpdate() : void;
	}

	abstract class ComponentBehaviour {}

	namespace Shared {
		class UserCodeStart extends System { }
		class UserCodeEnd extends System { }
		class SomeFence extends System { }
	}

	type Schedulable = typeof System | typeof ComponentSystem | typeof ComponentBehaviour;

	function usingFilter(...args: typeof EntityFilter[]);
	function executeAfter(...args: Schedulable[]);
	function executeBefore(...args: Schedulable[]);
	function requiredComponents(...args: typeof Component[]);
	function optionalComponents(...args: typeof Component[]);
}
");

            File.WriteAllText(Path.Combine(_scriptDir.FullName, "fake-generated.d.ts"), @"
declare namespace game {
	class UserComponent extends ut.Component {
		x(): number;
		setX(): number;
	}
	class OtherUserComponent extends ut.Component {
		x(): number;
		setX(): number;
	}

	class A extends ut.Component {}
	class B extends ut.Component {}
	class C extends ut.Component {}
	class D extends ut.Component {}
}
");
	        _configFile = new FileInfo(Path.Combine(_scriptDir.FullName, "fake-tsconfig.json"));
	        File.WriteAllText(_configFile.FullName, @"
{
    ""compilerOptions"": {
		""experimentalDecorators"": true,
		""target"": ""es5"",
		""outFile"": ""test-tsout.js""
	}
}
");
            _context = new TinyContext(ContextUsage.Tests);
	        _registry = _context.Registry;
            _project = _registry.CreateProject(TinyId.New(), "TestProject");
            _module = _registry.CreateModule(TinyId.New(), "Game");
            _module.Options |= TinyModuleOptions.ProjectModule;
            _module.Namespace = "game";
            _project.Module = (TinyModule.Reference)_module;
	        
	        var sharedModule = _registry.CreateModule(TinyId.New(), "UTiny.Shared");
	        sharedModule.Namespace = "ut.Shared";
	        
	        _module.AddExplicitModuleDependency((TinyModule.Reference)sharedModule);
        }

        [TearDown]
        public void TearDown()
        {
            TinyBuildUtilities.PurgeDirectory(_scriptDir);
            _registry.Clear();
            _registry = null;
	        _context = null;
        }

	    private ScriptMetadata RunTest()
	    {
		    var metaFile = new FileInfo("test-metadata.json");
		    var metadata = TinyBuildUtilities.CompileTypeScript(_configFile, metaFile);
		    Assert.NotNull(metadata);
		    metaFile.Delete();
		    
		    // inject standard fences before we resolve the metadata
		    metadata.Systems.Add(new ScriptComponentSystem()
		    {
				Name = "InputFence",
			    QualifiedName = "ut.Shared.InputFence",
			    IsFence = true,
			    IsRuntime = true,
			    Source = new ScriptSource()
			    {
				    File = "<injection>"
			    }
		    });
		    metadata.Systems.Add(new ScriptComponentSystem()
		    {
			    Name = "UserCodeStart",
			    QualifiedName = "ut.Shared.UserCodeStart",
			    IsFence = true,
			    IsRuntime = true,
			    Source = new ScriptSource()
			    {
				    File = "<injection>"
			    }
		    });
		    metadata.Systems.Add(new ScriptComponentSystem()
		    {
			    Name = "UserCodeEnd",
			    QualifiedName = "ut.Shared.UserCodeEnd",
			    IsFence = true,
			    IsRuntime = true,
			    Source = new ScriptSource()
			    {
				    File = "<injection>"
			    }
		    });
		    metadata.Systems.Add(new ScriptComponentSystem()
		    {
			    Name = "PlatformRenderingFence",
			    QualifiedName = "ut.Shared.PlatformRenderingFence",
			    IsFence = true,
			    IsRuntime = true,
			    Source = new ScriptSource()
			    {
				    File = "<injection>"
			    }
		    });

		    var scripting = _context.GetManager<IScriptingManager>();
		    Assert.IsTrue(scripting.Apply(metadata, _context, _module));

		    metadata = scripting.Metadata;

		    foreach (var decl in metadata.AllDeclarations)
		    {
			    Debug.Log($"Reflected '{decl.QualifiedName}' from {decl.Source}");
		    }
		    
		    return metadata;
	    }

        [Test]
        public void ScriptReflection_Basic()
        {
	        File.WriteAllText(Path.Combine(_scriptDir.FullName, "test.ts"), @"
namespace game {
/** This is a test system.*/
export class SomeSystem extends ut.ComponentSystem {
	OnUpdate(): void {}
}}
");
	        var meta = RunTest();
	        Assert.AreEqual(0, meta.ResolutionErrors.Count);

	        var sys = meta.Systems.FirstOrDefault(s => s.QualifiedName == "game.SomeSystem");
	        
	        Assert.NotNull(sys);
	        Assert.AreEqual("SomeSystem", sys.Name);
	        Assert.AreEqual("This is a test system.", sys.Description);
        }
	    
	    [Test]
	    public void ScriptReflection_ExecuteAfterBefore()
	    {
		    File.WriteAllText(Path.Combine(_scriptDir.FullName, "test.ts"), @"
namespace game {
/** This is a test system.*/
@ut.executeAfter(ut.Shared.UserCodeStart)
@ut.executeBefore(ut.Shared.UserCodeEnd)
export class SomeOtherSystem extends ut.ComponentSystem {
	OnUpdate(): void {}
}}
");
		    var meta = RunTest();
		    Assert.AreEqual(0, meta.ResolutionErrors.Count);
		    
		    var sys = meta.Systems.FirstOrDefault(s => s.QualifiedName == "game.SomeOtherSystem");
		    Assert.NotNull(sys);
		    Assert.AreEqual("SomeOtherSystem", sys.Name);
		    Assert.AreEqual(1, sys.ExecuteAfter.Count);
		    Assert.AreEqual(1, sys.ExecuteBefore.Count);
	    }
	    
	    [Test]
	    public void ScriptReflection_SimpleBehaviour()
	    {
		    File.WriteAllText(Path.Combine(_scriptDir.FullName, "test.ts"), @"
namespace game {
/** Filter comment.*/
export class SimpleFilter extends ut.EntityFilter {
	data:UserComponent;
	optionalData?:OtherUserComponent;
}
/** Behaviour comment.*/
export class SimpleBehaviour extends ut.ComponentBehaviour {
	filter:SimpleFilter;

	OnUpdate(): void {}
}}
");
		    var meta = RunTest();
		    Assert.AreEqual(0, meta.ResolutionErrors.Count);
		    
		    var filter = meta.Filters.FirstOrDefault(s => s.QualifiedName == "game.SimpleFilter");
		    var behaviour = meta.Behaviours.FirstOrDefault(s => s.QualifiedName == "game.SimpleBehaviour");
		    Assert.NotNull(filter);
		    Assert.NotNull(behaviour);
		    
		    Assert.AreEqual(behaviour.MainFilter, filter);
		    Assert.AreEqual(1, behaviour.Methods.Count);
		    Assert.AreEqual("OnUpdate", behaviour.Methods[0]);

		    var dataField = filter.Fields.FirstOrDefault(f => f.Name == "data");
		    Assert.NotNull(dataField);
		    Assert.AreEqual("game.UserComponent", dataField.QualifiedName);
		    Assert.IsFalse(dataField.IsOptional);
		    
		    var optField = filter.Fields.FirstOrDefault(f => f.Name == "optionalData");
		    Assert.NotNull(optField);
		    Assert.AreEqual("game.OtherUserComponent", optField.QualifiedName);
		    Assert.IsTrue(optField.IsOptional);
	    }
    }
}
