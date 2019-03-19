using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using Unity.Properties.Codegen.CSharp;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.AI.Planner.DomainLanguage.TraitBased
{
    [Serializable]
    class Operation
    {
        public IEnumerable<string> OperandA
        {
            get => m_OperandA;
            set => m_OperandA = value.ToList();
        }

        public string Operator
        {
            get => m_Operator;
            set => m_Operator = value;
        }

        public IEnumerable<string> OperandB
        {
            get => m_OperandB;
            set => m_OperandB = value.ToList();
        }

        [SerializeField]
        List<string> m_OperandA;

        [SerializeField]
        string m_Operator;

        [SerializeField]
        List<string> m_OperandB;
    }

    [Serializable]
    class ParameterDefinition
    {
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public IEnumerable<string> IncludeTraitTypes
        {
            get => m_IncludeTraitTypes;
            set => m_IncludeTraitTypes = value.ToList();
        }

        public IEnumerable<string> ExcludeTraitTypes
        {
            get => m_ExcludeTraitTypes;
            set => m_ExcludeTraitTypes = value.ToList();
        }

        [SerializeField]
        string m_Name = "obj";

        [UsesTraitDefinition]
        [SerializeField]
        List<string> m_IncludeTraitTypes = new List<string>();

        [UsesTraitDefinition]
        [SerializeField]
        List<string> m_ExcludeTraitTypes = new List<string>();
    }

    [Serializable]
    class ActionDefinition : INamedData
    {
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public IEnumerable<ParameterDefinition> Parameters
        {
            get => m_Parameters;
            set => m_Parameters = value.ToList();
        }

        public IEnumerable<Operation> Preconditions
        {
            get => m_Preconditions;
            set => m_Preconditions = value.ToList();
        }

        public IEnumerable<ParameterDefinition> CreatedObjects
        {
            get => m_CreatedObjects;
            set => m_CreatedObjects = value.ToList();
        }

        public IEnumerable<Operation> Effects
        {
            get => m_Effects;
            set => m_Effects = value.ToList();
        }

        public IEnumerable<string> RemovedObjects
        {
            get => m_RemovedObjects;
            set => m_RemovedObjects = value.ToList();
        }

        public float Reward
        {
            get => m_Reward;
            set => m_Reward = value;
        }

        public string OperationalActionType
        {
            get => m_OperationalActionType;
            set => m_OperationalActionType = value;
        }

        [SerializeField]
        string m_Name;

        [SerializeField]
        List<ParameterDefinition> m_Parameters = new List<ParameterDefinition>();

        [SerializeField]
        List<Operation> m_Preconditions = new List<Operation>();

        [SerializeField]
        List<ParameterDefinition> m_CreatedObjects = new List<ParameterDefinition>();

        [SerializeField]
        List<string> m_RemovedObjects = new List<string>();

        [SerializeField]
        List<Operation> m_Effects = new List<Operation>();

        [SerializeField]
        float m_Reward;

        [FormerlySerializedAs("m_GameLogicType")]
        [SerializeField]
        string m_OperationalActionType;
    }

    [Serializable]
    class StateTerminationDefinition : INamedData
    {
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public ParameterDefinition ObjectParameters
        {
            get => m_ObjectParameters;
            set => m_ObjectParameters = value;
        }

        public IEnumerable<Operation> Criteria
        {
            get => m_Criteria;
            set => m_Criteria = value.ToList();
        }

        [SerializeField]
        string m_Name;

        [SerializeField]
        ParameterDefinition m_ObjectParameters;

        [SerializeField]
        List<Operation> m_Criteria = new List<Operation>();
    }

    [CreateAssetMenu(fileName = "New Plan", menuName = "AI/Plan Definition")]
    [Serializable]
    class PlanDefinition : ScriptableObject
    {
        public new string name => base.name.Replace(" ", string.Empty);

        public event Action definitionChanged;

        public string GeneratedClassDirectory => $"{m_DomainDefinition.GeneratedClassDirectory}{name}/";

        public DomainDefinition DomainDefinition
        {
            get => m_DomainDefinition;
            set => m_DomainDefinition = value;
        }

        public IEnumerable<ActionDefinition> ActionDefinitions
        {
            get => m_ActionDefinitions;
            set => m_ActionDefinitions = value.ToList();
        }

        public IEnumerable<StateTerminationDefinition> StateTerminationDefinitions
        {
            get => m_StateTerminationDefinitions;
            set => m_StateTerminationDefinitions = value.ToList();
        }

        [SerializeField]
        DomainDefinition m_DomainDefinition;

        [SerializeField]
        List<ActionDefinition> m_ActionDefinitions = new List<ActionDefinition>();

        [SerializeField]
        List<StateTerminationDefinition> m_StateTerminationDefinitions = new List<StateTerminationDefinition>();

        void OnValidate()
        {
            definitionChanged?.Invoke();
        }

        public Type GetType(string typeName)
        {
            var type = Type.GetType($"{m_DomainDefinition.name}.{name}.{typeName},{DomainDefinition.AssemblyName}");
            if (type == null)
                type = m_DomainDefinition.GetType(typeName);

            return type;
        }

#if UNITY_EDITOR
        public void GenerateClasses()
        {
            AssetDatabase.StartAssetEditing();
            foreach (var a in ActionDefinitions)
            {
                GenerateActionECSSystem(a);
            }
            foreach (var t in StateTerminationDefinitions)
            {
                GenerateTermination(t);
            }

            m_DomainDefinition.GenerateDefines();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        string GetTraitString(string trait)
        {
            var type = m_DomainDefinition.GetType(trait);
            return type != null ? $"{type.Namespace}.{trait}" : $"{m_DomainDefinition.name}.{trait}";
        }

        (string operandString, string paramString, string writeOperation) GetOperandStringECS(IEnumerable<string> operand, List<string> parameterNames)
        {
            var writeOperation = string.Empty;

            var paramString = string.Empty;
            var operandString = string.Empty;
            var i = 0;
            foreach (var e in operand)
            {
                if (i == 0)
                {
                    operandString = e;
                }
                else
                {
                    var split = e.Split('.');
                    var traitType = split[0];

                    if (i == 1)
                    {
                        // Create different variants for the operand (e.g. precondition use / write-back use)
                        var parameterName = operandString;
                        paramString = $"var @{parameterName} = {traitType}s[{operandString}Entity];\n";
                        paramString += $"@{parameterName}.{split[1]}";

                        operandString = $"{traitType}s[{parameterName}Entity].{split[1]}";

                        writeOperation = $"{traitType}s[{parameterName}Entity] = @{parameterName};\n";
                    }
                    else
                    {
                        var additionalProperties = $".{split[1]}";
                        paramString += additionalProperties;
                        operandString += additionalProperties;
                    }
                }

                i++;
            }

            if (parameterNames.Contains(operandString))
                operandString = $"DomainObjects[{operandString}Entity].ID";

            return (operandString, paramString, writeOperation);
        }

        (string operandString, string paramString, string writeOperation) GetEffectsOperandStringECS(IEnumerable<string> operand, List<string> parameterNames)
        {
            var writeOperation = string.Empty;

            var paramString = string.Empty;
            var operandString = string.Empty;
            var i = 0;
            foreach (var e in operand)
            {
                if (i == 0)
                {
                    operandString = e;
                }
                else
                {
                    var split = e.Split('.');
                    var traitType = split[0];

                    if (i == 1)
                    {
                        // Create different variants for the operand (e.g. precondition use / write-back use)
                        var parameterName = operandString;
                        paramString = $"var @{parameterName} = {traitType}s[{operandString}Entity];\n";
                        { // new
                            paramString += $"var hash = objectHashes[{parameterName}Entity];\n";
                            paramString += $"hash.Value -= @{parameterName}.GetHashCode();\n";
                        }
                        paramString += $"@{parameterName}.{split[1]}";

                        operandString = $"{traitType}s[{parameterName}Entity].{split[1]}";


                        writeOperation = $"{traitType}s[{parameterName}Entity] = @{parameterName};\n";
                        { // new
                            writeOperation += $"hash.Value += @{parameterName}.GetHashCode();\n";
                            writeOperation += $"objectHashes[{parameterName}Entity] = hash;";
                        }
                    }
                    else
                    {
                        var additionalProperties = $".{split[1]}";
                        paramString += additionalProperties;
                        operandString += additionalProperties;
                    }
                }

                i++;
            }

            if (parameterNames.Contains(operandString))
                operandString = $"DomainObjects[{operandString}Entity].ID";

            return (operandString, paramString, writeOperation);
        }

        void GenerateActionECSSystem(ActionDefinition actionDefinition)
        {
            if (actionDefinition == null)
                return;

            var cw = new CodeWriter();
            cw.Line("using System.Collections.Generic;");
            cw.Line("using Unity.AI.Planner;");
            cw.Line("using Unity.AI.Planner.DomainLanguage.TraitBased;");
            cw.Line("using Unity.Collections;");
            cw.Line("using Unity.Entities;");
            cw.Line();

            var parameters = actionDefinition.Parameters.ToList();
            var parameterNames = parameters.Select(p => p.Name).ToList();
            using (cw.Scope($"namespace {m_DomainDefinition.name}.{name}"))
            {
                using (cw.Scope($"public partial class {actionDefinition.Name} : BaseAction<{actionDefinition.Name}.Permutation>"))
                {
                    foreach (var parameter in parameters)
                    {
                        cw.Line($"public NativeArray<ComponentType> {parameter.Name}Types {{ get; private set; }}");
                    }
                    cw.Line();

                    foreach (var parameter in parameters)
                    {
                        cw.Line($"List<(Entity, int)> m_{parameter.Name}Entities = new List<(Entity, int)>();");
                    }
                    cw.Line();

                    using (cw.Scope("public struct Permutation"))
                    {
                        var parameterCount = 0;
                        foreach (var parameter in parameters)
                        {
                            cw.Line($"public int {parameter.Name}Index;");
                            parameterCount++;
                        }
                        cw.Line($"public int Length => {parameterCount};");
                        cw.Line();

                        // This allows accessing the arguments the way the non-ECS actions work
                        using (cw.Scope("public int this[int i]"))
                        {
                            using (cw.Scope("get"))
                            {
                                using (cw.Scope("switch(i)"))
                                {
                                    var i = 0;
                                    foreach (var parameter in parameters)
                                    {
                                        cw.Line($"case {i}:");
                                        cw.IncrementIndent();
                                        cw.Line($"return {parameter.Name}Index;");
                                        cw.DecrementIndent();
                                        cw.Line();
                                        i++;
                                    }
                                }
                                cw.Line();

                                cw.Line("return -1;");
                            }
                        }
                    }
                    cw.Line();

                    using (cw.Scope("protected override void OnCreateManager()"))
                    {
                        cw.Line("base.OnCreateManager();");
                        cw.Line();

                        foreach (var parameter in parameters)
                        {
                            using (cw.Scope($"{parameter.Name}Types = new NativeArray<ComponentType>(new []", false))
                            {
                                foreach (var included in parameter.IncludeTraitTypes)
                                {
                                    cw.Line($"ComponentType.ReadOnly<{included}>(),");
                                }

                                foreach (var excluded in parameter.ExcludeTraitTypes)
                                {
                                    cw.Line($"ComponentType.Subtractive<{excluded}>(),");
                                }
                            }
                            cw.Write(", Allocator.Persistent);").Line();
                            cw.Line();
                        }
                        cw.Line();

                        var tuples = parameters.Select(p => $"({p.Name}Types, m_{p.Name}Entities)");
                        using (cw.Scope("m_FilterTuples = new[]", false))
                        {
                            foreach (var tuple in tuples)
                            {
                                cw.Line($"{tuple},");
                            }
                        }
                        cw.Write(";").Line();
                    }
                    cw.Line();

                    using (cw.Scope("protected override void OnDestroyManager()"))
                    {
                        cw.Line("base.OnDestroyManager();");
                        cw.Line();

                        foreach (var parameter in actionDefinition.Parameters)
                        {
                            var parameterName = parameter.Name;
                            using (cw.Scope($"if ({parameterName}Types.IsCreated)"))
                            {
                                cw.Line($"{parameterName}Types.Dispose();");
                                cw.Line($"{parameterName}Types = default;");
                            }
                        }
                    }
                    cw.Line();

                    var traitTypes = new HashSet<string>();
                    foreach (var parameter in parameters)
                    {
                        foreach (var included in parameter.IncludeTraitTypes)
                        {
                            traitTypes.Add(included);
                        }
                    }

                    using (cw.Scope("protected override void GenerateArgumentPermutations(Entity stateEntity)"))
                    {
                        cw.Line("var DomainObjects = GetComponentDataFromEntity<DomainObjectTrait>(true);");

                        foreach (var traitType in traitTypes)
                        {
                            if (m_DomainDefinition.TraitDefinitions.First(td => td.Name == traitType).Fields.Any())
                                cw.Line($"var {traitType}s = GetComponentDataFromEntity<{traitType}>(true);");
                        }
                        cw.Line();


                        cw.Line("FilterObjects(stateEntity);");

                        var parameterCount = 0;
                        foreach (var parameter in parameters)
                        {
                            var parameterName = parameter.Name;
                            cw.Line($"foreach (var ({parameterName}Entity, {parameterName}Index) in m_{parameterName}Entities)");
                            cw.Line("{");
                            cw.IncrementIndent();
                            parameterCount++;
                        }

                        // preconditions
                        foreach (var precondition in actionDefinition.Preconditions)
                        {
                            var (operation, _, _) = GetOperandStringECS(precondition.OperandA, parameterNames);
                            operation += $" {precondition.Operator} ";
                            var operandB = GetOperandStringECS(precondition.OperandB, parameterNames);
                            operation += operandB.operandString;

                            cw.Line($"if (!({operation}))");
                            cw.IncrementIndent();
                            cw.Line("continue;");
                            cw.DecrementIndent();
                        }

                        using (cw.Scope("m_ArgumentPermutations.Add(new Permutation()", false))
                        {
                            foreach (var parameter in parameters)
                            {
                                var parameterName = parameter.Name;
                                cw.Line($"{parameterName}Index = {parameterName}Index,");
                            }
                        }
                        cw.Write(");").Line();

                        while (parameterCount > 0)
                        {
                            cw.DecrementIndent();
                            cw.Line("}");
                            parameterCount--;
                        }
                    }

                    cw.Line();

                    using (cw.Scope("protected override void ApplyEffects(Permutation permutation, Entity parentPolicyGraphNodeEntity, Entity originalStateEntity, int horizon)"))
                    {
                        cw.Line("var actionNodeEntity = CreateActionNode(parentPolicyGraphNodeEntity);");
                        cw.Line("var stateCopyEntity = CopyState(originalStateEntity);");
                        cw.Line();

                        cw.Line("var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateCopyEntity);");

                        cw.Line("var DomainObjects = GetComponentDataFromEntity<DomainObjectTrait>();");
                        foreach (var traitType in traitTypes)
                        {
                            if (m_DomainDefinition.TraitDefinitions.First(td => td.Name == traitType).Fields.Any())
                                cw.Line($"var {traitType}s = GetComponentDataFromEntity<{traitType}>();");
                        }
                        cw.Line("var objectHashes = GetComponentDataFromEntity<HashCode>();");
                        cw.Line();

                        foreach (var parameter in parameters)
                        {
                            var parameterName = parameter.Name;
                            cw.Line($"var {parameterName}Entity = GetEntity(domainObjectBuffer, permutation.{parameterName}Index, stateCopyEntity);");
                        }

                        foreach (var effect in actionDefinition.Effects)
                        {
                            using (cw.Scope(string.Empty))
                            {
                                var (_, operation, writeOperation) = GetEffectsOperandStringECS(effect.OperandA, parameterNames);
                                operation += $" {effect.Operator} ";
                                var operandB = GetEffectsOperandStringECS(effect.OperandB, parameterNames);
                                operation += $"{operandB.operandString};";

                                var operationLines = operation.Split('\n');
                                foreach (var line in operationLines)
                                {
                                    cw.Line(line);
                                }

                                var writeLines = writeOperation.Split('\n');
                                foreach (var line in writeLines)
                                {
                                    cw.Line(line);
                                }
                            }
                        }
                        cw.Line();

                        cw.Line("var argumentLength = permutation.Length;");
                        cw.Line("var argumentBuffer = EntityManager.GetBuffer<ActionNodeArgument>(actionNodeEntity);");
                        cw.Line("var arguments = new NativeArray<int>(argumentLength, Allocator.Temp);");
                        using (cw.Scope("for (var i = 0; i < argumentLength; i++)"))
                        {
                            cw.Line("var argumentIndex = permutation[i];");
                            cw.Line("argumentBuffer.Add(new ActionNodeArgument() { DomainObjectReferenceIndex = argumentIndex });");
                            cw.Line("arguments[i] = argumentIndex;");
                        }
                        cw.Line();

                        cw.Line("ApplyCustomActionEffectsToState(stateCopyEntity, originalStateEntity, arguments);");
                        cw.Line();

                        cw.Line("SetActionData(stateCopyEntity, originalStateEntity, parentPolicyGraphNodeEntity, horizon + 1, actionNodeEntity,");
                        cw.IncrementIndent();
                        cw.Line("Reward(originalStateEntity, stateCopyEntity, arguments));");
                        cw.DecrementIndent();
                        cw.Line();

                        cw.Line("arguments.Dispose();");
                    }

                    cw.Line();
                    cw.Line("partial void ApplyCustomActionEffectsToState(Entity stateEntity, Entity originalStateEntity, NativeArray<int> arguments); // Implement this method in another file to extend the action's effects");
                    cw.Line();

                    using (cw.Scope("float Reward(Entity startStateEntity, Entity endStateEntity, NativeArray<int> arguments)"))
                    {
                        cw.Line($"var reward = {actionDefinition.Reward}f;");
                        cw.Line("SetCustomReward(ref reward, startStateEntity, endStateEntity, arguments);");
                        cw.Line();
                        cw.Line("return reward;");
                    }
                    cw.Line();

                    cw.Line("partial void SetCustomReward(ref float reward, Entity startStateEntity, Entity endStateEntity, NativeArray<int> arguments); // Implement this method in another file to modify the reward");
                }
            }

            var path = $"{GeneratedClassDirectory}{actionDefinition.Name}.cs";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, cw.ToString());
        }

        string GetTerminationOperandStringECS(IEnumerable<string> operand)
        {
            var operandString = string.Empty;
            var i = 0;
            foreach (var e in operand)
            {
                if (i == 0)
                {
                    operandString = e;
                }
                else
                {
                    var split = e.Split('.');
                    var traitType = split[0];

                    if (i == 1)
                    {
                        operandString = $"entityManager.GetComponentData<{traitType}>(domainObjectEntity).{split[1]}";
                    }
                    else
                    {
                        var additionalProperties = $".{split[1]}";
                        operandString += additionalProperties;
                    }
                }

                i++;
            }

            return operandString;
        }

        void GenerateTermination(StateTerminationDefinition stateTerminationDefinition)
        {
            if (stateTerminationDefinition == null)
                return;

            var path = $"{GeneratedClassDirectory}{stateTerminationDefinition.Name}.cs";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var cw = new CodeWriter();
            cw.Line("using System;");
            cw.Line("using Unity.AI.Planner.DomainLanguage.TraitBased;");
            cw.Line("using Unity.Collections;");
            cw.Line("using Unity.Entities;");
            cw.Line();

            var terminationName = stateTerminationDefinition.Name;
            using (cw.Scope($"namespace {m_DomainDefinition.name}.{name}"))
            {
                using (cw.Scope($"public class {terminationName} : IStateTermination"))
                {
                    using (cw.Scope("public string Name"))
                    {
                        cw.Line("get => s_Name;");
                        cw.Line("set => s_Name = value;");
                    }
                    cw.Line();

                    cw.Line("public NativeArray<ComponentType> ComponentTypes => m_ComponentTypes;");
                    cw.Line();

                    // Property backing fields - Name, ArgumentFilters, Preconditions
                    cw.Line($"protected static string s_Name = \"{terminationName}\";");

                    var objectParameters = stateTerminationDefinition.ObjectParameters;

                    cw.Line("NativeArray<ComponentType> m_ComponentTypes;");
                    cw.Line();

                    using (cw.Scope($"public {terminationName}()"))
                    {
                        using (cw.Scope("m_ComponentTypes = new NativeArray<ComponentType>(new []", false))
                        {
                            foreach (var included in objectParameters.IncludeTraitTypes)
                            {
                                cw.Line($"ComponentType.ReadOnly<{included}>(),");
                            }

                            foreach (var excluded in objectParameters.ExcludeTraitTypes)
                            {
                                cw.Line($"ComponentType.Subtractive<{excluded}>(),");
                            }
                        }
                        cw.Write(", Allocator.Persistent);").Line();
                    }
                    cw.Line();

                    using (cw.Scope("public void Dispose()"))
                    {
                        using (cw.Scope("if (m_ComponentTypes.IsCreated)"))
                        {
                            cw.Line("m_ComponentTypes.Dispose();");
                            cw.Line("m_ComponentTypes = default;");
                        }
                    }
                    cw.Line();

                    using (cw.Scope("public bool ShouldTerminate(EntityManager entityManager, Entity domainObjectEntity)"))
                    {
                        cw.WriteIndent();
                        cw.Write("return ");

                        var first = true;
                        foreach (var e in stateTerminationDefinition.Criteria)
                        {
                            var opA = GetTerminationOperandStringECS(e.OperandA);
                            var opB = GetTerminationOperandStringECS(e.OperandB);
                            var op = $"{opA} {e.Operator} {opB}";

                            if (first)
                            {
                                cw.Write(op);
                                first = false;
                            }
                            else
                            {
                                cw.IncrementIndent();
                                cw.Line().WriteIndent();
                                cw.Write($"&& {op}");
                                cw.DecrementIndent();
                            }
                        }

                        cw.Write(";").Line();
                    }
                }
            }

            File.WriteAllText(path, cw.ToString());
        }
#endif
    }
}
