using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Properties.Serialization
{
    public interface ITypeVisitor
    {
        bool VisitField(FieldInfo field);
        bool VisitProperty(PropertyInfo pinfo);
    }
}
