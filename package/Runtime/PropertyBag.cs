#if NET_4_6
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Properties
{
    // TODO: Add generic implementation to store typed container properties
    public class PropertyBag : IPropertyBag
    {
        private readonly List<IProperty> m_Properties;
        private readonly Dictionary<string, IProperty> m_Map;

        public int PropertyCount => m_Properties.Count;
        
        public IEnumerable<IProperty> Properties => m_Properties;

        public PropertyBag()
        {
            m_Properties = new List<IProperty>();
            m_Map = new Dictionary<string, IProperty>();
        }

        public PropertyBag(params IProperty[] properties)
        : this((IEnumerable<IProperty>)properties)
        {
        }

        public PropertyBag(IEnumerable<IProperty> properties)
        {
            m_Properties = new List<IProperty>(properties);
            m_Map = new Dictionary<string, IProperty>(m_Properties.Count);
            foreach (var n in m_Properties)
            {
                Assert.IsFalse(m_Map.ContainsKey(n.Name), $"PropertyBag already contains a property named {n.Name}");
                m_Map[n.Name] = n;
            }
        }

        public void AddProperty(IProperty property)
        {
            Assert.IsNotNull(property);
            Assert.IsFalse(m_Map.ContainsKey(property.Name));
            
            m_Properties.Add(property);
            m_Map[property.Name] = property;
        }

        public void RemoveProperty(IProperty property)
        {
            Assert.IsNotNull(property);
            m_Properties.Remove(property);
            m_Map.Remove(property.Name);
        }

        public void Clear()
        {
            m_Properties.Clear();
            m_Map.Clear();
        }

        public IProperty FindProperty(string name)
        {
            IProperty prop;
            return m_Map.TryGetValue(name, out prop) ? prop : null;
        }
        
        public void Visit<TContainer>(TContainer container, IPropertyVisitor visitor) 
            where TContainer : class, IPropertyContainer
        {
            foreach (var t in m_Properties)
            {
                var typed = t as ITypedContainerProperty<TContainer>;
                if (typed == null)
                {
                    // valid scenario when IPropertyContainer is used as TContainer
                    t.Accept(container, visitor);
                }
                else
                {
                    typed.Accept(container, visitor);
                }
            }
        }
        
        public void Visit<TContainer>(ref TContainer container, IPropertyVisitor visitor) 
            where TContainer : struct, IPropertyContainer
        {
            for (var i = 0; i < m_Properties.Count; i++)
            {
                var typed = (IStructTypedContainerProperty<TContainer>)m_Properties[i];
                typed.Accept(ref container, visitor);
            }
        }
    }
}
#endif // NET_4_6
