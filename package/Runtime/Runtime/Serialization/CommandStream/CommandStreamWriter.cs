

using System.IO;
using System.Text;
using Unity.Tiny.Serialization.Binary;

namespace Unity.Tiny.Serialization.CommandStream
{
    internal class CommandStreamWriter : BinaryWriter
    {
        private static readonly Encoding s_Encoding = new UTF8Encoding(false, true);
        
        public CommandStreamWriter(Stream stream, bool leaveOpen = false) : base(stream, s_Encoding, leaveOpen)
        {
            
        }

        public void PushSourceIdentiferScope(string sourceIdentifier)
        {
            Write(CommandType.PushSourceIdentifierScope);
            var start = BaseStream.Position;
            
            // Write a placeholder length
            Write((uint) 0);
            Write(sourceIdentifier);
            
            // Compute the payload length
            var end = BaseStream.Position;
            var length = end - start - sizeof(uint);
            
            // Seek back a re-write the proper length
            BaseStream.Seek(start, SeekOrigin.Begin);
            Write((uint) length);
            BaseStream.Seek(end, SeekOrigin.Begin);
        }
        
        public void PopSourceIdentiferScope()
        {
            Write(CommandType.PopSourceIdentifierScope);
            Write((uint) 0); // length
        }
    }
}

