namespace Unity.MemoryProfiler.Editor
{
    public interface ITypeRenderer
    {
        string GetTypeName();
        string Render(CachedSnapshot snapshot, ObjectData data);
        bool Expandable();
    }
    public class Int16TypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.Int16"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadInt16().ToString(); }
    }
    public class Int32TypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.Int32"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadInt32().ToString(); }
    }
    public class Int64TypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.Int64"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadInt64().ToString(); }
    }
    public class UInt16TypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.UInt16"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadUInt16().ToString(); }
    }
    public class UInt32TypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.UInt32"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadUInt32().ToString(); }
    }
    public class UInt64TypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.UInt64"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadUInt64().ToString(); }
    }
    public class BooleanTypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.Boolean"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadBoolean().ToString(); }
    }
    public class CharTypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.Char"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadChar().ToString(); }
    }
    public class DoubleTypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.Double"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadDouble().ToString(); }
    }
    public class SingleTypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.Single"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadSingle().ToString(); }
    }
    public class StringTypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.String"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od)
        {
            if (od.dataIncludeObjectHeader)
            {
                //od.data.Add(managedData.virtualMachineInformation.objectHeaderSize)
                od = od.GetBoxedValue(snapshot, true);
            }
            return "\"" + od.managedObjectData.ReadString() + "\"";
        }
    }
    public class IntPtrTypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.IntPtr"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadPointer().ToString(); }
    }
    public class ByteTypeDisplay : ITypeRenderer
    {
        bool ITypeRenderer.Expandable() { return false; }
        string ITypeRenderer.GetTypeName() { return "System.Byte"; }
        string ITypeRenderer.Render(CachedSnapshot snapshot, ObjectData od) { return od.managedObjectData.ReadByte().ToString(); }
    }
}
