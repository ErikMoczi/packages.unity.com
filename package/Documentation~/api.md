# Editor Coroutines API

Here is the available public API for Editor Coroutines.

## Class EditorCoroutine

A handle to an `EditorCoroutine` can be passed to [EditorCoroutineUtility](#class-editorcoroutineutility) control methods to affect the coroutine.

## Class EditorWaitForSeconds

Suspends the `EditorCoroutine` execution for the given amount of seconds, using [unscaled time](https://docs.unity3d.com/ScriptReference/Time-unscaledTime.html). The coroutine execution continues after the specified time has elapsed.

|   **Type**   | **Signature** | **Description** |
| :----------: | :------------ | :-------------- |
|Constructor|`EditorWaitForSeconds(float time)`|The constructor runs when creating a new `EditorWaitForSeconds` object. The `time` parameter is the amount of time to wait in seconds.|
|Property|`double WaitTime`|Get the specified amount of seconds.|

## Class EditorCoroutineUtility

This class is a utility to manage the lifetime and ownership of an `EditorCoroutine`.

|   **Type**   | **Signature** | **Description** |
| :----------: | :------------ | :-------------- |
|Method|`static EditorCoroutine StartCoroutine(IEnumerator routine, object owner)`|Starts an `EditorCoroutine` with the specified `owner` object. If the garbage collector collects the `owner` object, while the resulting coroutine is still executing, the coroutine will stop running. **Note**: Only types that don't inherit from [UnityEngine.Object](https://docs.unity3d.com/ScriptReference/Object.html) will get collected.|
|Method|`static EditorCoroutine StartCoroutineOwnerless(IEnumerator routine)`|This method starts an `EditorCoroutine` without an owner object. The `EditorCoroutine` runs until it completes or is canceled using `StopCoroutine`.|
|Method|`static void StopCoroutine(EditorCoroutine coroutine)`|Immediately stop an `EditorCoroutine`. This method is safe to call on an already completed coroutine.|

> **Note**: For more information, see [IEnumerator](https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerator?view=netstandard-2.0).

## Class EditorWindowCoroutineExtension

This extension class for the [EditorWindow](https://docs.unity3d.com/ScriptReference/EditorWindow.html) has helper methods to start and stop `EditorCoroutines` within the scope of an `EditorWindow`.

|   **Type**   | **Signature** | **Description** |
| :----------: | :------------ | :-------------- |
|Method|`static EditorCoroutine StartCoroutine(this EditorWindow window, IEnumerator routine)`|Start an `EditorCoroutine`, owned by the calling `EditorWindow` object.|
|Method|`static void StopCoroutine(this EditorWindow window, EditorCoroutine coroutine)`|Immediately stop an `EditorCoroutine`. This method is safe to call on an already completed coroutine.|



[Back to manual](manual.md)