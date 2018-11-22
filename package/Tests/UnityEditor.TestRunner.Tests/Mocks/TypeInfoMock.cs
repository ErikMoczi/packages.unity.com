using System;
using System.Reflection;
using NUnit.Framework.Interfaces;

namespace FrameworkTests
{
    public class TypeInfoMock : ITypeInfo
    {
        public T[] GetCustomAttributes<T>(bool inherit) where T : class
        {
            return new T[] {};
        }

        public bool IsDefined<T>(bool inherit)
        {
            throw new NotImplementedException();
        }

        public bool IsType(Type type)
        {
            throw new NotImplementedException();
        }

        public string GetDisplayName()
        {
            throw new NotImplementedException();
        }

        public string GetDisplayName(object[] args)
        {
            throw new NotImplementedException();
        }

        public Type GetGenericTypeDefinition()
        {
            throw new NotImplementedException();
        }

        public ITypeInfo MakeGenericType(Type[] typeArgs)
        {
            throw new NotImplementedException();
        }

        public bool HasMethodWithAttribute(Type attrType)
        {
            throw new NotImplementedException();
        }

        public IMethodInfo[] GetMethods(BindingFlags flags)
        {
            throw new NotImplementedException();
        }

        public ConstructorInfo GetConstructor(Type[] argTypes)
        {
            throw new NotImplementedException();
        }

        public bool HasConstructor(Type[] argTypes)
        {
            throw new NotImplementedException();
        }

        public object Construct(object[] args)
        {
            throw new NotImplementedException();
        }

        public Type Type { get; private set; }
        public ITypeInfo BaseType { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public Assembly Assembly { get; private set; }
        public string Namespace { get; private set; }
        public bool IsAbstract { get; private set; }
        public bool IsGenericType { get; private set; }
        public bool ContainsGenericParameters { get; private set; }
        public bool IsGenericTypeDefinition { get; private set; }
        public bool IsSealed { get; private set; }
        public bool IsStaticClass { get; private set; }
    }
}
