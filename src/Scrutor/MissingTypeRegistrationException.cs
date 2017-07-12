using System;
using System.Linq;
using System.Reflection;

namespace Scrutor
{
    public class MissingTypeRegistrationException : InvalidOperationException
    {
        public MissingTypeRegistrationException(Type serviceType)
            : base($"Could not find any registered services for type '{GetFriendlyName(serviceType)}'.")
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; }

        private static string GetFriendlyName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(char)) return "char";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(string)) return "string";
            if (type == typeof(object)) return "object";

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType) return GetGenericFriendlyName(typeInfo);

            return type.Name;
        }

        private static string GetGenericFriendlyName(TypeInfo typeInfo)
        {
            var argumentNames = typeInfo.GenericTypeArguments.Select(GetFriendlyName).ToArray();

            var baseName = typeInfo.Name.Split('`').First();

            return $"{baseName}<{string.Join(", ", argumentNames)}>";
        }
    }
}