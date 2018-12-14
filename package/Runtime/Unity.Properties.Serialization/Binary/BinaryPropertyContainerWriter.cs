

using System.IO;

namespace Unity.Properties.Serialization
{
    internal static class BinaryPropertyContainerWriter
    {
        private static readonly MemoryStream s_MemoryStream = new MemoryStream();
        private static readonly BinaryWriter s_BinaryWriter = new BinaryWriter(s_MemoryStream);
        
        public static void Write<TContainer>(Stream stream, TContainer container, BinaryPropertyVisitor visitor) 
            where TContainer : class, IPropertyContainer
        {
            s_MemoryStream.Position = 0;
            
            visitor.Writer = s_BinaryWriter;
            visitor.WriteContainer(container);
            
            stream.Write(s_MemoryStream.GetBuffer(), 0, (int) s_MemoryStream.Position);
        }
    }
}

