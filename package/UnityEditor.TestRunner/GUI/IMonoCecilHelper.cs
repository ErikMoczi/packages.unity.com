using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnityEditor.TestTools.TestRunner.GUI
{
    internal interface IMonoCecilHelper
    {
        MethodDefinition GetMethodByName(TypeDefinition typeDefinition, string methodName);
        TypeDefinition FindTypeByFullName(AssemblyDefinition assemblyDefinition, string typeFullName);
        SequencePoint GetMethodFirstSequencePoint(MethodDefinition methodDefinition);
        SequencePoint GetSequencePointForMethod(string assemblyPath, string typeFullName, string methodName);
        AssemblyDefinition ReadAssembly(string assemblyPath);
        IFileOpenInfo TryGetCecilFileOpenInfo(Type type, MethodInfo methodInfo);
    }
}
