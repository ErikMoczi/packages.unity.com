# Editor Coroutines code examples

Below are come common code examples, using the Editor Coroutines package:

## Yielding a specific number of frames within the Editor

```c#

   class MyEditorWindow : UnityEditor.EditorWindow
    {
        IEnumerator ExecuteEachFrame(int frameCount)
        {
            while(frameCount > 0)
            {
                yield return null;
                Debug.Log("Waiting");
            }
        }
        void OnEnable()
        {
            this.StartCoroutine(ExecuteEachFrame(10));
        }
    }
```

## Yielding a specific amount of time within the Editor

```c#

   class MyEditorWindow : UnityEditor.EditorWindow
    {
        IEnumerator ExecuteAfterWait(float timeInSeconds)
        {
            yield return new EditorWaitForSeconds(timeInSeconds);
            Debug.Log("Waited for: " + timeInSeconds + " s");
        }
        void OnEnable()
        {
           this.StartCoroutine(ExecuteAfterWait(2.3f));
        }
    }
```

## Creating an ownerless EditorCoroutine, using the InitializeOnLoad attribute

```c#
    [InitializeOnLoad]
    public class Startup
    {
        static IEnumerator MyCoroutine()
        {
            EditorWaitForSeconds wait = new EditorWaitForSeconds(2.0f);
            while (true)
            {
                yield return wait;
                Debug.Log("Triggered every " + wait.WaitTime + " seconds");
            }
        }

        static Startup()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(MyCoroutine());
        }
    }

```



[Back to manual](manual.md)