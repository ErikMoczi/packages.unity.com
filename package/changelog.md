# Changelog

## 0.2.4-preview.37

- Fix a crash on Linux and MacOS in the editor with dlopen crashing when trying to load burst-llvm (linux)

## 0.2.4-preview.36

- Fix a crash on Linux and MacOS in the editor with dlopen crashing when trying to load burst-llvm (mac)

## 0.2.4-preview.35

- Try to fix a crash on macosx in the editor when a job is being compiled by burst at startup time
- Fix burst accidentally resolving reference assemblies
- Add support for burst for ARM64 when building UWP player

## 0.2.4-preview.34

- Fix compiler exception with an invalid cast that could occur when using pinned variables (e.g `int32&` resolved to `int32**` instead of `int32*`)

## 0.2.4-preview.33

- Fix a compiler crash with methods incorrectly being marked as external and throwing an exception related to ABI

## 0.2.4-preview.32

- Fix codegen and linking errors for ARM when using mathematical functions on plain floats
- Add support for vector types GetHashCode
- Add support for DllImport (only compatible with Unity `2018.2.12f1`+ and ` 2018.3.0b5`+)
- Fix codegen when converting uint to int when used in a binary operation

## 0.2.4-preview.31

- Fix codegen for fmodf to use inline functions instead
- Add extended disassembly output to the burst inspector
- Fix generic resolution through de-virtualize methods
- Fix bug when accessing float3.zero. Prevents static constructors being considered intrinsics.
- Fix NoAlias attribute checking when generics are used

## 0.2.4-preview.30

- Fix IsValueType throwing a NullReferenceException in case of using generics
- Fix discovery for burst inspector/AOT methods inheriting from IJobProcessComponentData or interfaces with generics
- Add NoAliasAttribute
- Improved codegen for csum
- Improved codegen for abs(int)
- Improved codegen for abs on floatN/doubleN

## 0.2.4-preview.29

- Fix issue when calling an explicit interface method not being matched through a generic constraint
- Fix issue with or/and binary operation on a bool returned by a function

## 0.2.4-preview.28

- Fix a compilation issue when storing a bool returned from a function to a component of a bool vector
- Fix AOT compilation issue with a duplicated dictionary key 
- Fix settings of ANDROID_NDK_ROOT if it is not setup in Unity Editor

## 0.2.4-preview.27

- Improve detection of jobs within nested generics for AOT/burst inspector
- Fix compiler bug of comparison of a pointer to null pointer
- Fix crash compilation of sincos on ARM (neon/AARCH64) 
- Fix issue when using a pointer to a VectorType resulting in an incorrect access of a vector type
- Add support for doubles (preview)
- Improve AOT compiler error message/details if the compiler is failing before the linker

## 0.2.4-preview.26

- Added support for cosh, sinh and tanh

## 0.2.4-preview.25

- Fix warning in unity editor

## 0.2.4-preview.24

- Improve codegen of math.compress
- Improve codegen of math.asfloat/asint/asuint
- Improve codegen of math.csum for int4
- Improve codegen of math.count_bits
- Support for lzcnt and tzcnt intrinsics
- Fix AOT compilation errors for PS4 and XboxOne
- Fix an issue that could cause wrong code generation for some unsafe ptr operations

## 0.2.4-preview.23

- Fix bug with switch case to support not only int32

## 0.2.4-preview.22

- Fix issue with pointers comparison not supported
- Fix a StackOverflow exception when calling an interface method through a generic constraint on a nested type where the declaring type is a generic
- Fix an issue with EntityCommandBuffer.CreateEntity/AddComponent that could lead to ArgumentException/IndexOutOfRangeException

## 0.2.4-preview.21

- Correct issue with Android AOT compilation being unable to find the NDK.

## 0.2.4-preview.20

- Prepare the user documentation for a public release

## 0.2.4-preview.19

- Fix compilation error with generics when types are coming from different assemblies

## 0.2.4-preview.18

- Add support for subtracting pointers

## 0.2.4-preview.17

- Bump only to force a new version pushed


## 0.2.4-preview.16

- Fix AOT compilation errors

## 0.2.4-preview.15

- Fix crash for certain access to readonly static variable 
- Fix StackOverflowException when using a generic parameter type into an interface method

## 0.2.4-preview.14

- Fix an issue with package structure that was preventing burst to work in Unity

## 0.2.4-preview.13

- Add support for burst timings menu
- Improve codegen for sin/cos
- Improve codegen when using swizzles on vector types
- Add support for sincos intrinsic
- Fix AOT deployment

## 0.2.4-preview.12

- Fix a bug in codegen that was collapsing methods overload of System.Threading.Interlocked to the same method

## 0.2.4-preview.11

- Fix exception in codegen when accessing readonly static fields from different control flow paths

## 0.2.4-preview.10

- Fix a potential stack overflow issue when a generic parameter constraint on a type is also referencing another generic parameter through a generic interface constraint
- Update to latest Unity.Mathematics:
  - Fix order of parameters and codegen for step functions

## 0.2.4-preview.9

- Fix bug when casting an IntPtr to an enum pointer that was causing an invalid codegen exception

## 0.2.4-preview.8

- Breaking change: Move Unity.Jobs.Accuracy/Support to Unity.Burst
- Deprecate ComputeJobOptimizationAttribute in favor of BurstCompileAttribute
- Fix bug when using enum with a different type than int
- Fix bug with IL stind that could lead to a memory corruption.

## 0.2.4-preview.7

- Add support for nested structs in SOA native arrays
- Add support for arbitrary sized elements in full SOA native arrays
- Fix bug with conversion from signed/unsigned integers to signed numbers (integers & floats)
- Add support for substracting pointers at IL level
- Improve codegen with pointers arithmetic to avoid checking for overflows

## 0.2.4-preview.6

- Remove `bool1` from mathematics and add proper support in burst
- Add support for ARM platforms in the burst inspector UI

## 0.2.4-preview.5

- Add support for readonly static fields
- Add support for stackalloc
- Fix potential crash on MacOSX when using memset is used indirectly
- Fix crash when trying to write to a bool1*
- Fix bug with EnableBurstCompilation checkbox not working in Unity Editor

## 0.2.4-preview.4

- Fix an issue on Windows with `DllNotFoundException` occurring when trying to load `burst-llvm.dll` from a user profile containing unicode characters in the folder path
- Fix an internal compiler error occurring with IL dup instruction

## 0.2.4-preview.3

- Add support for struct with an explicit layout
- Fix noalias regression (that was preventing the auto-vectorizer to work correctly on basic loops)

## 0.2.3 (21 March 2018)

- Improve error messages for static field access
- Improve collecting of compilable job by trying to collect concrete job type instances (issue #23)

## 0.2.2 (19 March 2018)

- Improve error messages in case using `is` or `as` cast in C#
- Improve error messages if a static delegate instance is used
- Fix codegen error when converting a byte/ushort to a float
