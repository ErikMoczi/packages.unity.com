using System;
using System.Reflection;
using System.Text;

namespace Unity.Burst.Editor
{
    internal class BurstCompileTarget
    {
        public BurstCompileTarget(MethodInfo method, Type jobType, bool isStaticMethod)
        {
            Method = method;
            JobType = jobType;
            SupportsBurst = BurstReflection.ExtractBurstCompilerOptions(JobType, out DefaultOptions);
            IsStaticMethod = isStaticMethod;
        }

        /// <summary>
        /// <c>true</c> if the <see cref="Method"/> is directly tagged with a [BurstCompile] attribute
        /// </summary>
        public readonly bool IsStaticMethod;

        /// <summary>
        /// The Execute method of the target's producer type.
        /// </summary>
        public readonly MethodInfo Method;

        /// <summary>
        /// The type of the actual job (i.e. BoidsSimulationJob).
        /// </summary>
        public readonly Type JobType;

        /// <summary>
        /// The default compiler options
        /// </summary>
        public readonly string DefaultOptions;

        /// <summary>
        /// Set to true if burst compilation is possible.
        /// </summary>
        public readonly bool SupportsBurst;

        /// <summary>
        /// Generated disassembly, or null if disassembly failed
        /// </summary>
        public string[] Disassembly;

        public string GetDisplayName()
        {
            return IsStaticMethod ? Pretty(Method) : Pretty(JobType);
        }
        
        private static string Pretty(MethodInfo method)
        {
            var builder = new StringBuilder();
            builder.Append(Pretty(method.DeclaringType));
            builder.Append(".");
            builder.Append(method.Name);
            builder.Append("(");
            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (i > 0) builder.Append(", ");
                builder.Append(Pretty(param.ParameterType));
            }

            builder.Append(")");
            return builder.ToString();
        }

        private static string Pretty(Type type)
        {
            if (type == typeof(bool))
            {
                return "bool";
            }
            if (type == typeof(int))
            {
                return "int";
            }
            if (type == typeof(long))
            {
                return "long";
            }
            if (type == typeof(uint))
            {
                return "uint";
            }
            if (type == typeof(ulong))
            {
                return "ulong";
            }
            if (type == typeof(short))
            {
                return "short";
            }
            if (type == typeof(ushort))
            {
                return "ushort";
            }
            if (type == typeof(byte))
            {
                return "byte";
            }
            if (type == typeof(sbyte))
            {
                return "sbyte";
            }
            if (type == typeof(float))
            {
                return "float";
            }
            if (type == typeof(double))
            {
                return "double";
            }
            if (type == typeof(string))
            {
                return "string";
            }
            if (type == typeof(object))
            {
                return "object";
            }
            if (type == typeof(char))
            {
                return "char";
            }

            return type.ToString().Replace("+", ".");
        }
    }
}
