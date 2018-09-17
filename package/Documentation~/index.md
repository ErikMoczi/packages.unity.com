# Performance Testing Extension for Unity Test Runner

The Unity Performance Testing Extension is a Unity Editor package that, when installed, provides an API and test case decorators to make it easier to take measurements/samples of Unity profiler markers, and other custom metrics outside of the profiler, within the Unity Editor and built players. It also collects configuration metadata, such as build and player settings, which is useful when comparing data against different hardware and configurations.

The Performance Testing Extension is intended to be used with, and complement, the Unity Test Runner framework.

**Important Note:** When tests are run with the Unity Test Runner, a development player is always built to support communication between the editor and player, effectively overriding the development build setting from the build settings UI or scripting API.

## Installing

To install the Performance Testing Extension package
1. Open the manifest.json file for your Unity project (located in the YourProject/Packages directory) in a text editor
2. Add com.unity.test-framework.performance to the dependencies as seen below
3. Add com.unity.test-framework.performance to the testables section. If there is not a testables section in your manifest.json file, go ahead and add it.
4. Save the manifest.json file
5. Verify the Performance Testing Extension is now installed opening the Unity Package Manager window
6. Ensure you have created an Assembly Definition file in the same folder where your tests or scripts are that you’ll reference the Performance Testing Extension with. This Assembly Definition file needs to reference Unity.PerformanceTesting in order to use the Performance Testing Extension. Example of how to do this:
    * Create a new folder for storing tests in ("Tests", for example)
    * Create a new assembly definition file in the new folder using the context menu (right click/Create/Assembly definition) and name it "Tests" (or whatever you named the folder from step a. above)
    * In inspector for the assembly definition file check "Test Assemblies", and then Apply.
    * Open the assembly definition file in a text editor and add Unity.PerformanceTesting. To the references section. Save the file when you’re done doing this.

> Example: manifest.json file

``` json
{
    "dependencies": {
        "com.unity.test-framework.performance": "0.1.39-preview",
        "com.unity.modules.jsonserialize": "1.0.0",
        "com.unity.modules.unitywebrequest": "1.0.0",
        "com.unity.modules.unityanalytics": "1.0.0",
        "com.unity.modules.vr": "1.0.0",
        "com.unity.modules.physics": "1.0.0",
        "com.unity.modules.xr": "1.0.0"
      },
      "testables": [
        "com.unity.test-framework.performance"
      ]
}
```


> Example: assembly definition file

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

More information on how to create and run tests please refer to [Unity Test Runner docs](https://docs.unity3d.com/Manual/testing-editortestsrunner.html).


## Test Attributes
**[PerformanceTest]** - Non yeilding performance test.

**[PerformanceUnityTest]** - Yeilding performance test.

**[Version(string version)]** - Performance tests should be versioned with every change. If not specified it will be assumed to be 1


## SampleGroupDefinition

**struct SampleGroupDefinition** - used to define how a measurement is used in reporting and in regression detection.

Required parameters
- **name** : Name of the measurement. Should be kept short and simple.

Optional parameters
- **sampleUnit** : Unit of the measurement to report samples in. Possible values are:
    - Nanosecond, Microsecond, Millisecond, Second, Byte, Kilobyte, Megabyte, Gigabyte
- **aggregationType** : Preferred aggregation (default is median). Possible values are:
    - Median, Average, Min, Max, Percentile
- **percentile** : If aggregationType is Percentile, the percentile value used for the aggregation. e.g. 0.95.
- **increaseIsBetter** : Determines whether or not an increase in the measurement value should be considered a progression (performance improved) or a performance regression. Default is false. **NOTE:** This value is not used directly in the Performance Testing Extension, but recorded for later use in a reporting tool (such as the [Unity Performance Benchmark Reporter](https://github.com/Unity-Technologies/PerformanceBenchmarkReporter/wiki)) to determine whether or not a performance regression has occurred when used with a baseline result set.
- **threshold** : The threshold, as a percentage of the aggregated sample group value, to use for regression detection. Default value is 0.15f. **NOTE:** This value is not used directly in the Performance Testing Extension, but recorded for later use in a reporting tool (such as the [Unity Performance Benchmark Reporter](https://github.com/Unity-Technologies/PerformanceBenchmarkReporter/wiki)) to determine whether or not a performance regression has occurred when used with a baseline result set.

If unspecified a default SampleGroupDefinition will be used with the name of "Time", it is recommended to specify a name that is descriptive of what it is measuring.

## Taking measurements

The Performance Testing Extension provides several API methods you can use to take measurements in your performance test, depending on what you need to measure and how you want to do it. They are:
* Measure.Method
* Measure.Frames
* Measure.Scope(SampleGroupdDefinition sampleGroupDefinition)
* Measure.FrameTimes(SampleGroupdDefinition sampleGroupDefinition)
* Measure.ProfilerMarkers(SampleGroupDefinition[] sampleGroupDefinitions)
* Measure.Custom(SampleGroupDefinition sampleGroupDefinition, double value)

The sections below detail the specifics of each measurement method with examples.

Preferred way is to use Measure.Method or Measure.Frames. They both do a couple of warmup iterations which are then used to decide how many iterations per measurement should be used.


**MethodMeasurement Method()**

This will execute the provided method, sampling performance using the following additional properties/methods to control how the measurements are taken:
* **WarmupCount(int n)** - number of times to to execute before measurements are collected. Default is 3 if not specified.
* **MeasurementCount(int n)** - number of measurements to capture. Default is 7 if not specified.
* **IterationsPerMeasurement(int n)** - number of iterations per measurement to use
* **GC()** - if specified, will measure the Gargage Collection allocation value.

> Example 1: Simple method measurement using default values

``` csharp
[PerformanceTest]
public void Test()
{
    Measure.Method(() => { ... }).Run();
}
```

> Example 2: Customize Measure.Method properties

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

This will sample perf frame, records time per frame by default and provides additional properties/methods to control how the measurements are taken:
* **WarmupCount(int n)** - number of times to to execute before measurements are collected. Default is 3 if not specified.
* **MeasurementCount(int n)** - number of measurements to capture. Default is 7 if not specified.
* **DontRecordFrametime()** - disables frametime measurement
* **ProfilerMarkers(...)** - sample profile markers per frame

It will automatically select the number of warmup and runtime frames.

> Example 1: Simple frame time measurement

``` csharp
[PerformanceUnityTest]
public IEnumerator Test()
{
    ...

    yield return Measure.Frames().Run();
}
```

In cases where you are measuring a system over frametime it is advised to disable frametime measurements and instead measure profiler markers for your system.
> Example 2: Sample profile markers per frame, disable frametime measurement

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
> Example 3: Specify custom WarmupCount and MeasurementCount per frame

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

**IDisposable Measure.Scope(SampleGroupdDefinition sampleGroupDefinition)**

When method or frame measurements are not enough you can use the following to measure. It will measure Scope, Frames, Markers or Custom.

> Example 1: Measuring a scope

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

> Example 1: Sample frame times for a scope

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

When you want to record samples outside of frame time, method time, or profiler markers, use a custom measurement. It can be any double value. A sample group definition is required.

> Example 1: Use a custom measurement to capture total allocated memory

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
                    UnityEngine.Object.Instantiate(cube);
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