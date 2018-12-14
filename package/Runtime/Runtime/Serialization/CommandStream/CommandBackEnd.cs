

using System.Collections.Generic;
using System.IO;
using Unity.Properties;

namespace Unity.Tiny.Serialization.CommandStream
{
    /// <summary>
    /// Writes objects as commands to a stream
    /// </summary>
    internal static class CommandBackEnd
    {
        public static void Persist(Stream output, params IPropertyContainer[] objects)
        {
            Persist(output, (IEnumerable<IPropertyContainer>) objects);
        }

        public static void Persist(Stream output, IEnumerable<IPropertyContainer> objects)
        {
            var commandStreamWriter = new BinaryWriter(output);
            
            using (var memory = new MemoryStream())
            {
                foreach (var obj in objects)
                {
                    (obj as IRegistryObject)?.Refresh();
                    
                    var container = obj;
                    
                    var typeId = (TinyTypeId) (obj.PropertyBag.FindProperty("$TypeId") as IValueProperty)?.GetObjectValue(container);
                    commandStreamWriter.Write(CommandType.GetCreateCommandType(typeId));

                    // Use binary serialization protocol
                    Binary.BinaryBackEnd.Persist(memory, obj);
                    
                    // Write the payload size
                    commandStreamWriter.Write((uint) memory.Position);
                    
                    // Write the payload
                    commandStreamWriter.Write(memory.GetBuffer(), 0, (int) memory.Position);
                    memory.Position = 0;
                }
            }
        }
    }
}

