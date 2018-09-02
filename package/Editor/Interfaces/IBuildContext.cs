using System;

namespace UnityEditor.Build.Interfaces
{
    public interface IContextObject { }

    public interface IScriptsCallback : IContextObject
    {
        ReturnCodes PostScripts(IBuildParameters buildParameters, IBuildResults buildResults);
    }

    public interface IDependencyCallback : IContextObject
    {
        ReturnCodes PostDependency(IBuildParameters buildParameters, IDependencyData dependencyData);
    }

    public interface IPackingCallback : IContextObject
    {
        ReturnCodes PostPacking(IBuildParameters buildParameters, IDependencyData dependencyData, IWriteData writeData);
    }

    public interface IWritingCallback : IContextObject
    {
        ReturnCodes PostWriting(IBuildParameters buildParameters, IDependencyData dependencyData, IWriteData writeData, IBuildResults buildResults);
    }

    public interface IBuildContext
    {
        bool ContainsContextObject<T>() where T : IContextObject;
        bool ContainsContextObject(Type type);

        T GetContextObject<T>() where T : IContextObject;
        IContextObject GetContextObject(Type type);

        void SetContextObject<T>(IContextObject contextObject) where T : IContextObject;
        void SetContextObject(IContextObject contextObject);

        bool TryGetContextObject<T>(out T contextObject) where T : IContextObject;
    }
}
