using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using UnityEngine;

namespace UnityEditor.TestTools.TestRunner.GUI
{
    internal class MonoCecilHelper : IMonoCecilHelper
    {
        public SequencePoint GetMethodFirstSequencePoint(MethodDefinition methodDefinition)
        {
            if (methodDefinition == null)
            {
                Debug.Log("MethodDefinition cannot be null. Check if any method was found by name in its declaring type TypeDefinition.");
                return null;
            }

            if (!methodDefinition.HasBody || !methodDefinition.Body.Instructions.Any() || methodDefinition.DebugInformation == null)
            {
                Debug.Log(string.Concat("To get SequencePoints MethodDefinition for ", methodDefinition.Name, " must have MethodBody, DebugInformation and Instructions."));
                return null;
            }

            if (!methodDefinition.DebugInformation.HasSequencePoints)
            {
                Debug.Log(string.Concat("No SequencePoints for MethodDefinition for ", methodDefinition.Name));
                return null;
            }

            var firstInstruction = methodDefinition.Body.Instructions.First();
            return methodDefinition.DebugInformation.GetSequencePoint(firstInstruction);
        }

        public MethodDefinition GetMethodByName(TypeDefinition typeDefinition, string methodName)
        {
            if (typeDefinition == null)
            {
                Debug.Log("TypeDefinition cannot be null. Check whether the type exists in assembly.");
                return null;
            }

            if (!typeDefinition.HasMethods)
            {
                Debug.Log(string.Concat("TypeDefinition ", typeDefinition.Name, "has no method definitions."));
                return null;
            }

            return typeDefinition.Methods.FirstOrDefault(m => m.Name.Equals(methodName));
        }

        public TypeDefinition FindTypeByFullName(AssemblyDefinition assemblyDefinition, string typeFullName)
        {
            if (assemblyDefinition == null)
            {
                Debug.Log("AssemblyDefinition cannot be null. Check if it's read correctly.");
                return null;
            }

            if (!assemblyDefinition.MainModule.HasTypes)
            {
                Debug.Log(string.Concat("AssemblyDefinition ", assemblyDefinition.Name));
                return null;
            }

            var allTypes = AggregateAllTypeDefinitions(assemblyDefinition.MainModule.Types); // recursively checks for nested types and adds them to colleciton, if any
            return allTypes.FirstOrDefault(t => t.FullName == typeFullName);
        }

        public AssemblyDefinition ReadAssembly(string assemblyPath)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
            var readerParameters = new ReaderParameters
            {
                SymbolReaderProvider = new MdbReaderProvider(),
                AssemblyResolver = assemblyResolver,
                ReadingMode = ReadingMode.Deferred
            };

            return AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
        }

        IEnumerable<TypeDefinition> AggregateAllTypeDefinitions(IEnumerable<TypeDefinition> types)
        {
            var typeDefs = types.ToList();
            foreach (var typeDefinition in types)
            {
                if (typeDefinition.HasNestedTypes)
                    typeDefs.AddRange(AggregateAllTypeDefinitions(typeDefinition.NestedTypes));
            }
            return typeDefs;
        }

        public SequencePoint GetSequencePointForMethod(string assemblyPath, string typeFullName, string methodName)
        {
            var assemblyDefinition = ReadAssembly(assemblyPath);
            var typeDefinition = FindTypeByFullName(assemblyDefinition, typeFullName);
            var methodDefinition = GetMethodByName(typeDefinition, methodName);

            return GetMethodFirstSequencePoint(methodDefinition);
        }

        public IFileOpenInfo TryGetCecilFileOpenInfo(Type type, MethodInfo methodInfo)
        {
            var assemblyPath = type.Assembly.Location;
            var methodName = methodInfo.Name;

            // Nested types are appended to the type name in reflection, but not in Cecil
            var typeName = type.FullName ?? string.Empty;
            typeName = typeName.Contains("+") ? typeName.Split('+').First() : typeName;

            var sequencePoint = GetSequencePointForMethod(assemblyPath, typeName, methodName);

            var fileOpenInfo = new FileOpenInfo();
            if (sequencePoint != null) // Can be null in case of yield return in target method
            {
                fileOpenInfo.LineNumber = sequencePoint.StartLine;
                fileOpenInfo.FilePath = sequencePoint.Document.Url;
            }

            return fileOpenInfo;
        }
    }
}
