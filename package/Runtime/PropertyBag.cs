using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Properties
{
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
            foreach (var n in properties)
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
        
        public bool Visit<TContainer>(TContainer container, IPropertyVisitor visitor) 
            where TContainer : class, IPropertyContainer
        {
            for (var i = 0; i < m_Properties.Count; i++)
            {
                var typed = m_Properties[i] as ITypedContainerProperty<TContainer>;
                Assert.IsNotNull(typed);
                
                typed.Accept(container, visitor);
            }

            return true;
        }
        
        public bool VisitStruct<TContainer>(ref TContainer container, IPropertyVisitor visitor) 
            where TContainer : struct, IPropertyContainer
        {
            for (var i = 0; i < m_Properties.Count; i++)
            {
                var typed = m_Properties[i] as IStructTypedContainerProperty<TContainer>;
                Assert.IsNotNull(typed);
                
                typed.Accept(ref container, visitor);
            }

            return true;
        }
    }
}