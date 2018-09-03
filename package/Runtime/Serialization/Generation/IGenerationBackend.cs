using System;
using System.Collections.Generic;

namespace Unity.Properties.Serialization
{
    public interface IGenerationBackend
    {
        StringBuffer Generate(List<PropertyContainerType> root);
    }
}
