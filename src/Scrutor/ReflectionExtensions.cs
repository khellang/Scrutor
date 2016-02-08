using System;
using System.Collections.Generic;
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
        
        /// <summary>
        /// Find matching interface by name C# interface name convention.  Optionally use a filter.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        internal static IEnumerable<Type> FindMatchingInterface(this TypeInfo t, Action<TypeInfo, IImplementationTypeFilter> action)
        {
            string matchingInterfaceName = "I" + t.Name;
            var matchedInterfaces = GetImplementedInterfacesToMap(t).Where(x => string.Equals(x.Name, matchingInterfaceName, StringComparison.Ordinal));
            Type type;
            if (action != null)
            {
                var filter = new ImplementationTypeFilter(matchedInterfaces);
                action(t, filter);
                type = filter.Types.FirstOrDefault();
            }
            else
            {
                type = matchedInterfaces.FirstOrDefault();
            }
            if (type != null)
            {
                yield return type;
            }
        }

        private static IEnumerable<Type> GetImplementedInterfacesToMap(TypeInfo typeInfo)
        {
            if (!typeInfo.IsGenericType)
            {
                return typeInfo.ImplementedInterfaces;
            }
            if (!typeInfo.IsGenericTypeDefinition)
            {
                return typeInfo.ImplementedInterfaces;
            }
            return FilterMatchingGenericInterfaces(typeInfo);
        }

        private static IEnumerable<Type> FilterMatchingGenericInterfaces(TypeInfo typeInfo)
        {
            var genericTypeParameters = typeInfo.GenericTypeParameters;
            foreach (Type current in typeInfo.ImplementedInterfaces)
            {
                var currentTypeInfo = current.GetTypeInfo();
                if (currentTypeInfo.IsGenericType && currentTypeInfo.ContainsGenericParameters && GenericParametersMatch(genericTypeParameters, currentTypeInfo.GenericTypeArguments))
                {
                    yield return currentTypeInfo.GetGenericTypeDefinition();
                }
            }
        }

        private static bool GenericParametersMatch(Type[] parameters, Type[] interfaceArguments)
        {
            if (parameters.Length != interfaceArguments.Length)
            {
                return false;
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] != interfaceArguments[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}