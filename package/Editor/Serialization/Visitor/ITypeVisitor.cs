#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Properties.Editor.Serialization
{
    public interface ITypeVisitor
    {
        bool VisitField(FieldInfo field);
        bool VisitProperty(PropertyInfo pinfo);
    }
}
#endif // NET_4_6
