using System;
using System.Linq;
using System.Reflection;

namespace Scrutor
{
    internal static class ReflectionExtensions
    {
        internal static bool IsNonAbstractClass(this Type type)
        {
            var typeInfo = type.GetTypeInfo();

            return typeInfo.IsClass && !typeInfo.IsAbstract;
        }

        internal static bool IsInNamespace(this Type type, string @namespace)
        {
            var typeNamespace = type.Namespace ?? string.Empty;

            if (@namespace.Length > typeNamespace.Length)
            {
                return false;
            }

            var typeSubNamespace = typeNamespace.Substring(0, @namespace.Length);

            if (typeSubNamespace.Equals(@namespace, StringComparison.Ordinal))
            {
                if (typeNamespace.Length == @namespace.Length)
                {
                    //exactly the same
                    return true;
                }
                //is a subnamespace?
                var inSameSubNamespace = typeNamespace[@namespace.Length] == '.';
                return inSameSubNamespace;
            }

            return false;
        }

        internal static bool HasAttribute(this Type type, Type attributeType)
        {
            return type.GetTypeInfo().IsDefined(attributeType, inherit: true);
        }

        internal static bool HasAttribute<T>(this Type type, Func<T, bool> predicate) where T : Attribute
        {
            return type.GetTypeInfo().GetCustomAttributes<T>(inherit: true).Any(predicate);
        }

        internal static bool IsAssignableTo(this Type type, Type otherType)
        {
            var typeInfo = type.GetTypeInfo();
            var otherTypeInfo = otherType.GetTypeInfo();

            return otherTypeInfo.IsGenericTypeDefinition
                ? typeInfo.IsAssignableToGenericTypeDefinition(otherTypeInfo)
                : otherTypeInfo.IsAssignableFrom(typeInfo);
        }

        private static bool IsAssignableToGenericTypeDefinition(this TypeInfo typeInfo, TypeInfo genericTypeInfo)
        {
            var interfaceTypes = typeInfo.ImplementedInterfaces.Select(t => t.GetTypeInfo());

            foreach (var interfaceType in interfaceTypes)
            {
                if (interfaceType.IsGenericType)
                {
                    var typeDefinitionTypeInfo = interfaceType
                        .GetGenericTypeDefinition()
                        .GetTypeInfo();

                    if (typeDefinitionTypeInfo == genericTypeInfo)
                    {
                        return true;
                    }
                }
            }

            if (typeInfo.IsGenericType)
            {
                var typeDefinitionTypeInfo = typeInfo
                    .GetGenericTypeDefinition()
                    .GetTypeInfo();

                if (typeDefinitionTypeInfo == genericTypeInfo)
                {
                    return true;
                }
            }

            var baseTypeInfo = typeInfo.BaseType?.GetTypeInfo();

            if (baseTypeInfo == null)
            {
                return false;
            }

            return baseTypeInfo.IsAssignableToGenericTypeDefinition(genericTypeInfo);
        }
    }
}