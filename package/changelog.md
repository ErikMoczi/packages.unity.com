# Changelog

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
