# Changelog

## 0.2.4-preview.5 (TODO)

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

