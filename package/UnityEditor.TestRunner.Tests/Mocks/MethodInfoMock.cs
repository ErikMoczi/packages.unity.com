using System;
using System.Reflection;
using NUnit.Framework.Interfaces;

namespace FrameworkTests
{
    public class MethodInfoMock : IMethodInfo
    {
        readonly object m_CustomAttribute;
        public MethodInfoMock(object customAttribute = null)
        {
            this.m_CustomAttribute = customAttribute;
            this.TypeInfo = new TypeInfoMock();
        }

        public T[] GetCustomAttributes<T>(bool inherit) where T : class
        {
            var attribute = m_CustomAttribute as T;
            if (attribute != null)
            {
                return new[] { attribute};
            }

            return new T[] {};
        }

        public bool IsDefined<T>(bool inherit)
        {
            throw new NotImplementedException();
        }

        public IParameterInfo[] GetParameters()
        {
            throw new NotImplementedException();
        }

        public Type[] GetGenericArguments()
        {
            throw new NotImplementedException();
        }

        public IMethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            throw new NotImplementedException();
        }

        public object Invoke(object fixture, params object[] args)
        {
            throw new NotImplementedException();
        }

        public ITypeInfo TypeInfo { get; private set; }
        public MethodInfo MethodInfo { get; private set; }
        public string Name { get; private set; }
        public bool IsAbstract { get; private set; }
        public bool IsPublic { get; private set; }
        public bool ContainsGenericParameters { get; private set; }
        public bool IsGenericMethod { get; private set; }
        public bool IsGenericMethodDefinition { get; private set; }
        public ITypeInfo ReturnType { get; private set; }
    }
}
