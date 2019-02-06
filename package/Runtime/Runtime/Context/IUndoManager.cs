using System;
using System.Collections.Generic;

namespace Unity.Tiny
{
    internal struct Change
    {
        public int Version { get; set; }
        public TinyId Id { get; set; }
        public IRegistryObject RegistryObject { get; set; }
        public IMemento NextVersion { get; set; }
        public IMemento PreviousVersion { get; set; }
    }
    
    internal delegate void UndoPerformed(HashSet<Change> changes);
    internal delegate void RedoPerformed(HashSet<Change> changes);
    
    internal interface IUndoManager : IContextManager
    {
        event Action OnBeginUndo;
        event Action OnEndUndo;
        event Action OnBeginRedo;
        event Action OnEndRedo;
        
        event UndoPerformed OnUndoPerformed;
        event RedoPerformed OnRedoPerformed;

        void Update();
        void SetAsBaseline(params IOriginator[] originators);
        void SetAsBaseline(IEnumerable<IOriginator> originators);
    }
}