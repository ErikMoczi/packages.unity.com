# Performance testing extension for Unity Test Runner

Extension provides a set of calls to make it easier to take measurements and record profiler markers. It also collects data about build and player settings which is useful when comparing data for separating different hardware and configurations.

## Installing
To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html).

And add `com.unity.test-framework.performance` your packages manifest.
YourProject/Packages/manifest.json

``` json
{
    "dependencies": {
        "com.unity.test-framework.performance": "0.1.36-preview",
        "com.unity.modules.jsonserialize": "1.0.0",
        "com.unity.modules.unitywebrequest": "1.0.0",
        "com.unity.modules.vr": "1.0.0"
      },
      "testables": [
        "com.unity.test-framework.performance"
      ],
      "registry": "https://staging-packages.unity.com"
}
```

If you are using 2018.1 or 2018.2 the module dependencies are unnecessary.

Assembly definitions should reference `Unity.PerformanceTesting` in order to use it. Create a new folder for storing tests in and then create a new asset from context menu called `right click/Create/Assembly definition`. In inspector for the assembly file check "Test Assemblies and apply. Then open the file in text editor and add `Unity.PerformanceTesting`.

``` json
{
    "name": "Tests.Editor",
    "references": [
        "Unity.PerformanceTesting"
    ],
    "optionalUnityReferences": [
        "TestAssemblies"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false
}
```

How to test internals can be found in the following link:
https://q.unity3d.com/questions/992/how-to-test-internal-variables-in-the-editor-tests.html

More information on how to create and run tests please refer to [Unity Test Runner docs](https://docs.unity3d.com/Manual/testing-editortestsrunner.html).


## Test Attributes
**[PerformanceTest]** - Non yeilding performance test.

**[PerformanceUnityTest]** - Yeilding performance test.

**[Version(string version)]** - Performance tests should be versioned with every change. If not specified it will be assumed to be 1


## SampleGroupDefinition

**struct SampleGroupDefinition**
SampleGroupDefinition is used to define how a measurement is used in reporting and in regression detection.

Required parameters
- **name** : Name of the measurement. Should be kept short and simple.

Optional parameters
- **sampleUnit** : Unit of the measurement.
    - Nanosecond, Microsecond, Millisecond, Second, Byte, Kilobyte, Megabyte, Gigabyte
- **aggregationType** : Preferred aggregation (default is median)
- **percentile** : If aggregationType is Percentile, the percentile value used for the aggregation. i.e 0.95.
- **threshold** : Threshold used for regression detection. If current sample value is over the threshold different from the baseline results, the result is concidered as a regression or a progression. Default value is 0.15f.
- **increaseIsBetter** : Defines if an increase in the measurement value is concidered as a progression (better) or a regression. Default is false.

If unspecified a default SampleGroupDefinition will be used with the name of "Measure.Scope", it is recommended to specify a name that is descriptive of what it is measuring.

## Taking measurements

Preferred way is to use `Measure.Method` or `Measure.Frames`. They both do a couple of warmup iterations which are then used to decide how many iterations per measurement should be used.

**MethodMeasurement Method()**

It will execute provided method at least 3 times for warmup and 7 for measurements.

``` csharp
[PerformanceTest]
public void Test()
{
    Measure.Method(() => { ... }).Run();
}
```

In cases where you feel the default values are not ideal you can specify custom iterations.

WarmupCount - how many iterations to run without measuring for warmup
MeasurementCount - how many measurements to take
IterationsPerMeasurement - how many iterations per measurement to take
GC - measures the amount of GC allocations

```
[PerformanceTest]
public void Test()
{
    Measure.Method(() => { ... })
        .WarmupCount(10)
        .MeasurementCount(10)
        .IterationsPerMeasurement(5)
        .GC()
        .Run();
}
```

**FramesMeasurement Measure.Frames()**

Used to yield for frames. It will automatically select the number of warmup and runtime frames.

``` csharp
[PerformanceUnityTest]
public IEnumerator Test()
{
    ...

    yield return Measure.Frames().Run();
}
```

In cases where you are measuring a system over frametime it is advised to disable frametime measurements and instead measure profiler markers for your system.
``` csharp
[PerformanceUnityTest]
public IEnumerator Test()
{
    ...

    yield return Measure.Frames()
        .ProfilerMarkers(...)
        .DontRecordFrametime()
        .Run();
}
```

If you want more control, you can specify how many frames you want to measure.

``` csharp
[PerformanceUnityTest]
public IEnumerator Test()
{
    ...

    yield return Measure.Frames()
        .WarmupCount(5)
        .MeasurementCount(10)
        .Run();
}
```

When method or frame measurements are not enough you can use the following to measure. It will measure Scope, Frames, Markers or Cusom.

**IDisposable Measure.Scope(SampleGroupdDefinition sampleGroupDefinition)**

Used to measure a scope.

``` csharp
[PerformanceTest]
public void Test()
{
    using(Measure.Scope())
    {
        ...
    }
}
```

**IDisposable Measure.FrameTimes(SampleGroupdDefinition sampleGroupDefinition)**

Records frame times for a scope.

``` csharp
[PerformanceUnityTest]
public IEnumerator Test()
{
    using (Measure.Frames().Scope())
    {
        yield return ...;
    }
}
```


**IDisposable Measure.ProfilerMarkers(SampleGroupDefinition[] sampleGroupDefinitions)**

Records profiler samples for a scope. The name of sample group definition has to match profiler sample names.

``` csharp
[PerformanceTest]
public void Test()
{
    SampleGroupDefinition[] m_definitions =
    {
        new SampleGroupDefinition("Instantiate"),
        new SampleGroupDefinition("Instantiate.Copy"),
        new SampleGroupDefinition("Instantiate.Produce"),
        new SampleGroupDefinition("Instantiate.Awake")
    };

    using(Measure.ProfilerMarkers(m_definitions))
    {
        ...
    }
}
```


**void Custom(SampleGroupDefinition sampleGroupDefinition, double value)**

Records a custom sample. It can be any double value. A sample group definition is required.

``` csharp
[PerformanceTest]
public void Test()
{
    var definition = new SampleGroupDefinition("TotalAllocatedMemory", SampleUnit.Megabyte);
    Measure.Custom(definition, Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
}
```

## Output

Each performance test will have a performance test summary. Every sample group will have multiple aggregated samples such as median, min, max, average, standard deviation, sample count, count of zero samples and sum of all samples.

`Time Millisecond Median:53.59 Min:53.36 Max:62.10 Avg:54.07 Std:1.90 Zeroes:0 SampleCount: 19 Sum: 1027.34`

## Examples

``` csharp
    [PerformanceTest, Version("2")]
    public void Serialize_SimpleObject()
    {
        var obj = new SimpleObject();
        obj.Init();

        Measure.Method(() => JsonUtility.ToJson(obj))
            .Definition(sampleUnit: SampleUnit.Microsecond)
            .Run();
    }

    [Serializable]
    public class SimpleObject
    {
        public int IntField;
        public string StringField;
        public float FloatField;
        public bool BoolField;

        [Serializable]
        public struct NestedStruct
        {
            public int A, B;
        }

        public NestedStruct Str;

        public Vector3 Position;

        public void Init()
        {
            IntField = 1;
            StringField = "Test";
            FloatField = 2.0f;
            BoolField = false;
            Str.A = 15;
            Str.B = 20;
        }
    }
```



``` csharp
    // Records total and frame times for loading a scene async

    [PerformanceUnityTest]
    public IEnumerator LoadAsync_SampleScene()
    {
        using(Measure.Frames())
        {
            using (Measure.Scope())
            {
                yield return SceneManager.LoadSceneAsync("SampleScene");
            }
        }
    }
```

``` csharp
    SampleGroupDefinition[] m_definitions =
    {
        new SampleGroupDefinition("Instantiate"),
        new SampleGroupDefinition("Instantiate.Copy"),
        new SampleGroupDefinition("Instantiate.Produce"),
        new SampleGroupDefinition("Instantiate.Awake")
    };

    [PerformanceTest]
    public void Instantiate_CreateCubes()
    {
        using (Measure.ProfilerMarkers(m_definitions))
        {
            using(Measure.Scope())
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                for (var i = 0; i < 5000; i++)
                {
                    Object.Instantiate(cube);
                }
            }
        }
    }
```


``` csharp
    [PerformanceUnityTest]
    public IEnumerator Rendering_SampleScene()
    {
        using(Measure.Scope(new SampleGroupDefinition("Setup.LoadScene")))
        {
            SceneManager.LoadScene("SampleScene");
        }
        yield return null;

        yield return Measure.Frames().Run();
    }
```


``` csharp
    // Records allocated and reserved memory, specifies that the sample unit is in Megabytes.

    [PerformanceTest, Version("1")]
    public void Measure_Empty()
    {
        var allocated = new SampleGroupDefinition("TotalAllocatedMemory", SampleUnit.Megabyte);
        var reserved = new SampleGroupDefinition("TotalReservedMemory", SampleUnit.Megabyte);
        Measure.Custom(allocated, Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
        Measure.Custom(reserved, Profiler.GetTotalReservedMemoryLong() / 1048576f);
    }
```