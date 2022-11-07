using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Liminal.Cecil.Mono.Cecil;
using Liminal.Cecil.Mono.Cecil.Cil;
using Liminal.SDK.Serialization;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace Liminal.SDK.Build
{
    public class LimappExplorer : BaseWindowDrawer
    {
        public static string OutputDirectory;
        public static string InputDirectory;
        public static HashSet<int> ProcessedFile = new HashSet<int>();

        public override void Draw(BuildWindowConfig config)
        {
            DrawDirectorySelection(ref OutputDirectory, "Output Directory");
            DrawDirectorySelection(ref InputDirectory, "Input Directory");

            var input = @"C:\Work\Liminal\Platform\Liminal-SDK - 2022\Liminal-SDK-Unity-Package\Assets\TestApp\DLLs\App000000000017.dll";
            var output = @"C:\Work\Liminal\Platform\Liminal-SDK - 2022\Liminal-SDK-Unity-Package\DLLFixes\App000000000017.dll";

            if (GUILayout.Button("Read from Input"))
            {
                var asmDef = AssemblyDefinition.ReadAssembly(input);
                foreach (var item in asmDef.MainModule.AssemblyReferences)
                    Debug.Log(item.FullName);
            }

            // Take a dll and rebuild it with Cecil.
            if (GUILayout.Button("Update DLL"))
            {
                var asmDef = AssemblyDefinition.ReadAssembly(input);
                AddAssemblyReference(asmDef);
            }

            // Take a dll and rebuild it with Cecil.
            if (GUILayout.Button("Read from Output"))
            {
                var asmDef = AssemblyDefinition.ReadAssembly(output);
                foreach (var item in asmDef.MainModule.AssemblyReferences)
                    Debug.Log(item.FullName);

                foreach (var item in asmDef.MainModule.Types)
                    Debug.Log(item.FullName);
            }

            void AddAssemblyReference(AssemblyDefinition asmDef)
            {
                //asmDef.Name.Name = asmDef.Name.Name.Replace(ipp, version);
                var reference = AssemblyNameReference.Parse("UnityEngine.InputLegacyModule, Version=0.0.0.0");
                asmDef.MainModule.AssemblyReferences.Add(reference);
                var newInputType = asmDef.MainModule.ImportReference(typeof(UnityEngine.Input));

                var methods = GetAllMethodDefinitions(asmDef);
                foreach (var methodDef in methods)
                {
                    //ReplaceInstantiateCallsInMethod(methodDef, unityObjectTypeRef, liminalObjectTypeRef);
                    Replace(methodDef, newInputType, typeof(UnityEngine.Input));
                }

                //Debug.Log(inputTypeRef.FullName);

                asmDef.Write(output);
            }

            void Replace(MethodDefinition targetMethod, TypeReference replacementTypeRef, Type type)
            {
                if (!targetMethod.HasBody)
                    return;

                var methodCalls = targetMethod.Body.Instructions
                    .Where(x => x.OpCode == OpCodes.Call)
                    .ToArray();

                foreach (var instruction in methodCalls)
                {
                    if (instruction.Operand is not MethodReference mRef)
                        continue;

                    if (!mRef.DeclaringType.Name.Equals(type.Name)) 
                        continue;

                    Debug.Log($"{mRef.Name}, Declare Type {mRef.DeclaringType}");
                    instruction.Operand = CloneMethodWithDeclaringType(mRef, replacementTypeRef);
                }
            }

            MethodReference CloneMethodWithDeclaringType(MethodReference methodRef, TypeReference declaringTypeRef)
            {
                // If the input method reference is generic, it will be wrapped in a GenericInstanceMethod object
                var genericRef = methodRef as GenericInstanceMethod;
                if (genericRef != null)
                {
                    // The actual method data we need to replicate is the ElementMethod
                    // Replace the method reference with the element method from the generic wrapper
                    methodRef = methodRef.GetElementMethod();
                }

                // Build a new method reference that matches the original exactly, but with a different declaring type
                var newRef = new MethodReference(methodRef.Name, methodRef.ReturnType, declaringTypeRef)
                {
                    CallingConvention = methodRef.CallingConvention,
                    HasThis = methodRef.HasThis,
                    ExplicitThis = methodRef.ExplicitThis,
                    MethodReturnType = methodRef.MethodReturnType,
                };

                // Clone method input parameters
                foreach (var p in methodRef.Parameters)
                {
                    newRef.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
                }

                // Clone method generic parameters
                foreach (var p in methodRef.GenericParameters)
                {
                    newRef.GenericParameters.Add(new GenericParameter(p.Name, newRef));
                }

                if (genericRef == null)
                {
                    // For non-generic methods, we can simply return the new method reference
                    return newRef;
                }
                else
                {
                    // For generic methods, copy the generic arguments into the new method refernce
                    var newGenericRef = new GenericInstanceMethod(newRef);
                    foreach (var typeDef in genericRef.GenericArguments)
                    {
                        newGenericRef.GenericArguments.Add(typeDef);
                    }

                    // Done
                    return newGenericRef;
                }
            }

            IEnumerable<MethodDefinition> GetAllMethodDefinitions(AssemblyDefinition asmDef)
            {
                return asmDef.Modules
                    .SelectMany(m => m.Types
                        .Concat(m.Types.SelectMany(x => x.NestedTypes))
                        .SelectMany(x => x.Methods)
                    );
            }

            if (GUILayout.Button("Extract"))
            {
                ProcessedFile.Clear();

                var limapps = Directory.GetFiles(InputDirectory);
                EditorCoroutineUtility.StartCoroutineOwnerless(ExtractAll(limapps));
            }

            IEnumerator ExtractAll(string[] paths)
            {
                var limappPaths = paths.Where(x => Path.GetExtension(x) == ".limapp").ToArray();
                for (var i = 0; i < limappPaths.Length; i++)
                {
                    var limappPath = limappPaths[i];

                    if (Path.GetExtension(limappPath) != ".limapp")
                        continue;

                    EditorUtility.DisplayProgressBar("Extracting...", limappPath, i / (float)limappPath.Length);

                    Debug.Log($"Processing: {limappPath}");
                    var bytes = File.ReadAllBytes(limappPath);
                    yield return EditorCoroutineUtility.StartCoroutineOwnerless(ExtractPack(bytes, limappPath));
                }

                EditorUtility.ClearProgressBar();
            }


            IEnumerator ExtractPack(byte[] appBytes, string limappPath)
            {
                Debug.Log("Unpacking...");
                var unpacker = new AppUnpacker();
                unpacker.UnpackAsync(appBytes);

                yield return new WaitUntil(() => unpacker.IsDone);

                var fileName = Path.GetFileNameWithoutExtension(limappPath);

                // write all assemblies on disk
                var assmeblies = unpacker.Data.Assemblies;
                //Application.persistentDataPath
                var appFolder = $"{OutputDirectory}/{unpacker.Data.ApplicationId}/{unpacker.Data.TargetPlatform}";

                if (ProcessedFile.Contains(unpacker.Data.ApplicationId))
                    appFolder = $"{OutputDirectory}/{unpacker.Data.ApplicationId}-{fileName}/{unpacker.Data.TargetPlatform}";

                var assemblyFolder = $"{appFolder}/assemblyFolder";
                
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                if (!Directory.Exists(assemblyFolder))
                    Directory.CreateDirectory(assemblyFolder);

                // Wait, in theory, I can rewrite the assembly to match ah, but that's not it.

                for (var i = 0; i < assmeblies.Count; i++)
                {
                    var asmBytes = assmeblies[i];
                    var asm = Assembly.Load(asmBytes);
                    File.WriteAllBytes($"{assemblyFolder}/{asm.GetName()}", asmBytes);
                }

                File.WriteAllBytes($"{appFolder}/appBundle", unpacker.Data.SceneBundle);
                File.WriteAllText($"{appFolder}/manifest.txt", $"Filename: {Path.GetFileName(limappPath)}");

                ProcessedFile.Add(unpacker.Data.ApplicationId);
                Debug.Log("Done!");

                yield break;
            }

        }

        public void DrawDirectorySelection(ref string directoryPath, string title)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.Width(Size.x * 0.15F));
                directoryPath = GUILayout.TextField(directoryPath, GUILayout.Width(Size.x * 0.7F));

                if (GUILayout.Button("...", GUILayout.Width(Size.x * 0.1F)))
                {
                    directoryPath = EditorUtility.OpenFolderPanel("Select Limapp Folder", "", "");
                    directoryPath = DirectoryUtils.ReplaceBackWithForwardSlashes(directoryPath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }




    }
}

public static class TempInput
{
    public static void GetKey(KeyCode keycode)
    {
    }
}