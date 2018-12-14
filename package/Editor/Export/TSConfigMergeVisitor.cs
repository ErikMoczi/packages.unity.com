
using UnityEngine.Assertions;
using Unity.Properties;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TSConfigMergeVisitor : PropertyVisitor
    {
        private static MethodInfo ms_SetTypedPropertyValue = typeof(TSConfigMergeVisitor).GetMethod("SetTypedPropertyValue", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Dictionary<Type, MethodInfo> ms_GenericSetTypedPropertyValueMap = new Dictionary<Type, MethodInfo>();
        private readonly Stack<IPropertyContainer> m_PropertyContainers = new Stack<IPropertyContainer>();
        private readonly string m_ParentPath;

        public TSConfigMergeVisitor(IPropertyContainer container, string parentPath)
        {
            m_PropertyContainers.Push(container);
            m_ParentPath = parentPath;
        }

        protected override void Visit<TValue>(TValue value)
        {
            SetPropertyValue(Property.Name, value, ListIndex);
        }

        protected override bool BeginContainer()
        {
            return PushContainer(Property.Name, ListIndex);
        }

        protected override void EndContainer()
        {
            PopContainer(Property.Name, ListIndex);
        }

        private bool PushContainer(string name, int index)
        {
            var target = m_PropertyContainers.Peek();
            var property = target?.PropertyBag.FindProperty(name);

            var migration = target as MigrationContainer;

            Assert.IsNotNull(migration);

            if (null == property)
            {
                m_PropertyContainers.Push(migration.CreateContainer(name));
                return false;
            }

            IPropertyContainer value = index < 0 ? migration.GetContainer(name) : migration.GetContainerList(name)[index];

            m_PropertyContainers.Push(value);

            return true;
        }

        private void PopContainer(string name, int index)
        {
            // Fetch the dest container from the stack
            var container = m_PropertyContainers.Pop();

            Assert.IsNotNull(container);
            SetPropertyValue(name, container, index);
        }
        
        private void SetPropertyValue<TValue>(string name, TValue value, int index)
        {
            //HACK : this is to remap the relative paths to new relative paths
            if (value is string)
            {
                var retargettedString = Convert.ToString(value);

                if (retargettedString != "")
                {
                    var directory = new DirectoryInfo(Path.Combine(m_ParentPath, retargettedString));

                    bool directoryExists;

                    if (Path.HasExtension(retargettedString))
                    {
                        directoryExists = directory.Parent != null
                            && directory.Parent.Exists
                            && (Path.GetExtension(retargettedString).ToLowerInvariant() == TinyScriptUtility.TypeScriptExtension
                            || Path.GetExtension(retargettedString).ToLowerInvariant() == TinyScriptUtility.JavaScriptExtensionWithoutTXT);
                    }
                    else
                    {
                        directoryExists = directory.Exists;
                    }

                    if (directoryExists)
                    {
                        retargettedString = directory.FullName.ToForwardSlash().Substring(Application.dataPath.Length - "/Assets".Length + 1);
                    }
                }

                SetTypedPropertyValue(name, retargettedString, index);
            }
            else
            {
                //HACK : this is to strongly type TValue, otherwise it will serialize as System.Object
                if(!ms_GenericSetTypedPropertyValueMap.ContainsKey(value.GetType()))
                {
                    ms_GenericSetTypedPropertyValueMap.Add(value.GetType(), ms_SetTypedPropertyValue.MakeGenericMethod(value.GetType()));
                }
                ms_GenericSetTypedPropertyValueMap[value.GetType()].Invoke(this,new object[]{ name, value, index});
            }
        }

        private void SetTypedPropertyValue<TValue>(string name, TValue value, int index)
        {
            var target = m_PropertyContainers.Peek();

            Assert.IsNotNull(target);

            var property = target.PropertyBag.FindProperty(name);

            var migration = target as MigrationContainer;

            Assert.IsNotNull(migration);

            if (null == property)
            {
                if (!(value is IPropertyContainer))
                {
                    if (index < 0)
                    {
                        migration.CreateValue(name, value);
                    }
                    else
                    {
                        var list = migration.CreateValueList<TValue>(name);
                        list.Add(value);
                    }
                }
                return;
            }

            // Handle class properties
            if (property is IClassProperty)
            {
                var listTypedItemProperty = property as IListTypedItemClassProperty<TValue>;
                if (listTypedItemProperty != null)
                {
                    for (var i = 0; i < listTypedItemProperty.Count(target); ++i)
                    {
                        if (listTypedItemProperty.GetAt(target, i).ToString() == value.ToString())
                            return;
                    }

                    listTypedItemProperty.Add(target, value);
                    return;
                }

                var listClassProperty = property as IListClassProperty;
                if (listClassProperty != null)
                {
                    for (var i = 0; i < listClassProperty.Count(target); ++i)
                    {
                        if (listClassProperty.GetObjectAt(target, i).ToString() == value.ToString())
                            return;
                    }

                    listClassProperty.AddObject(target, value);
                    return;
                }

                var valueClassProperty = property as IValueClassProperty;
                if (valueClassProperty != null)
                {
                    valueClassProperty.SetObjectValue(target, value);
                    return;
                }
            }
        }
    }
}
