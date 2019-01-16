using NUnit.Framework;
using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using Unity.MemoryProfiler.Editor.Database.Operation;
using Unity.MemoryProfiler.Editor.Database;

namespace Unity.MemoryProfiler.Editor.Tests
{
    [TestFixture]
    internal class ColumnFactoryTests
    {
        private MethodInfo factoryMethod = typeof(ColumnCreator).GetMethod("GetFactory", BindingFlags.Static | BindingFlags.NonPublic);

        private List<string> validNamespaces = new List<string>()
        {
            "Database.Operation",
            "Database.View"
        };

        private List<string> excludedClasses = new List<string>()
        {
            "ViewColumnConst`1",
            "ExpConst`1"
        };

        private List<Type> supportedDataTypes = new List<Type>()
        {
            typeof(bool),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(long),
            typeof(short),
            typeof(uint),
            typeof(ulong),
            typeof(ushort),
            typeof(DiffTable.DiffResult),
            typeof(string) //Must always be the last type in the array for 'excludeStringType' to work
        };

        private Type[] GetTypesInheritingFrom(Type type)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            var enumerator = assembly.GetTypes().
                Where(t => t != type
                    && t.ContainsGenericParameters
                    && !t.IsAbstract
                    && t.GetGenericArguments().Length == 1
                    && validNamespaces.Contains(t.Namespace)
                    && !excludedClasses.Contains(t.Name)
                    && type.IsAssignableFrom(t));


            return enumerator.ToArray();
        }

        private void VerifyTypeCombinations(Type[] types, bool excludeStringType)
        {
            foreach (Type exprType in types)
                for (int i = 0; i < supportedDataTypes.Count - (excludeStringType ? 1 : 0); ++i)
                {
                    Assert.DoesNotThrow(() => factoryMethod.Invoke(null, new object[] { exprType, supportedDataTypes[i] }));
                }
        }

        [Test]
        public void FactoryIsAbleToCreateAllExpectedTypedColumnsInAssembly()
        {
            Type[] types = GetTypesInheritingFrom(typeof(Column));
            VerifyTypeCombinations(types, false);
        }

        [Test]
        public void FactoryIsAbleToCreateAllExpectedTypedExpressionsInAssembly()
        {
            Type[] types = GetTypesInheritingFrom(typeof(Expression));
            VerifyTypeCombinations(types, false);
        }

        [Test]
        public void FactoryIsAbleToCreateAllExpectedTypedMatchersInAssembly()
        {
            Type[] types = GetTypesInheritingFrom(typeof(Matcher));
            VerifyTypeCombinations(types, true);
        }
    }
}
