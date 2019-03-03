using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.Networking
{
    internal class WeaverRunner
    {
        [InitializeOnLoadMethod]
        static void OnInitializeOnLoad()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        static void OnCompilationFinished(string targetAssembly, CompilerMessage[] messages)
        {
            const string k_HlapiRuntimeAssemblyName = "com.unity.multiplayer-hlapi.Runtime";

            // Do nothing if there were compile errors on the target
            if (messages.Length > 0)
            {
                foreach (var msg in messages)
                {
                    if (msg.type == CompilerMessageType.Error)
                    {
                        Console.WriteLine("Compile error, aborting: " + msg.message);
                        return;
                    }
                }
            }

            // Should not run on the editor only assemblies
            if (targetAssembly.Contains("-Editor") || targetAssembly.Contains(".Editor"))
            {
                return;
            }
            
            // Should not run on own assembly
            if (targetAssembly.Contains(k_HlapiRuntimeAssemblyName))
            {
                return;
            }

            var scriptAssembliesPath = Application.dataPath + "/../" + Path.GetDirectoryName(targetAssembly);

            string unityEngine = "";
            string unetAssemblyPath = "";
            var outputDirectory = scriptAssembliesPath;
            var assemblyPath = targetAssembly;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool usesUnet = false;
            bool foundThisAssembly = false;
            foreach (var assembly in assemblies)
            {
                // Find the assembly currently being compiled from domain assembly list and check if it's using unet
                if (assembly.GetName().Name == Path.GetFileNameWithoutExtension(targetAssembly))
                {
                    foundThisAssembly = true;
                    foreach (var dependency in assembly.GetReferencedAssemblies())
                    {
                        if (dependency.Name.Contains(k_HlapiRuntimeAssemblyName))
                        {
                            usesUnet = true;
                        }
                    }
                }
                try
                {
                    if (assembly.Location.Contains("UnityEngine.CoreModule"))
                    {
                        unityEngine = assembly.Location;
                    }
                    if (assembly.Location.Contains(k_HlapiRuntimeAssemblyName))
                    {
                        unetAssemblyPath = assembly.Location;
                    }
                }
                catch (NotSupportedException)
                {
                    // in memory assembly, can't get location
                }
            }

            if (!foundThisAssembly)
            {
                // Target assembly not found in current domain, trying to load it to check references 
                // will lead to trouble in the build pipeline, so lets assume it should go to weaver
                // (only saw this happen in runtime test framework on editor platform)
                usesUnet = true;
            }

            if (!usesUnet)
            {
                return;
            }

            if (string.IsNullOrEmpty(unityEngine))
            {
                Debug.LogError("Failed to find UnityEngine assembly");
                return;
            }

            if (string.IsNullOrEmpty(unetAssemblyPath))
            {
                Debug.LogError("Failed to find hlapi runtime assembly");
                return;
            }

            //Debug.Log("Package invoking weaver with " + unityEngine + " " + unetAssemblyPath + " " + outputDirectory + " " + assemblyPath + " " + assemblyResolver);

            Unity.UNetWeaver.Program.Process(unityEngine, unetAssemblyPath, outputDirectory, new[] { assemblyPath }, new string[] { }, null, (value) => { Debug.LogWarning(value); }, (value) => { Debug.LogError(value); });
        }
    }
}
