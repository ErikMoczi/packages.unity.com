# Memory Profiler Public API

Here is the available public API for Memory Profiler.

## Interface IMetadataCollect

Interface for creating a metadata collector type to populate the `PackedMemorySnapshot.Metadata` member. You can add multiple collectors, but it is recommended to add only one. 

> **Note**: Adding a collector will override the default metadata collection functionality. If you want to keep the default metadata, go to the `DefaultCollect` method in the file _com.unity.memoryprofiler\Runtime\MetadataInjector.cs_ and copy that code into your collector method.

|   **Type**   | **Signature** | **Description** |
| :----------: | :------------ | :-------------- |
|Method|`void CollectMetadata(MetaData data)`|The Memory Profiler will invoke this method during the capture process, to populate the metadata of the capture.|



[Back to manual](manual.md)