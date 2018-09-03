# Changelog

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

