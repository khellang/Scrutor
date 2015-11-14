using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    internal class ImplementationTypeFilter : IImplementationTypeFilter
    {
        public ImplementationTypeFilter(IEnumerable<Type> types)
        {
            Types = types;
        }

        internal IEnumerable<Type> Types { get; private set; }

        public IImplementationTypeFilter AssignableTo<T>()
        {
            return AssignableTo(typeof(T));
        }

        public IImplementationTypeFilter AssignableTo(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return AssignableToAny(type);
        }

        public IImplementationTypeFilter AssignableToAny(params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return AssignableToAny(types.AsEnumerable());
        }

        public IImplementationTypeFilter AssignableToAny(IEnumerable<Type> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return Where(t => types.Any(type => t.IsAssignableTo(type)));
        }

        public IImplementationTypeFilter WithAttribute<T>() where T : Attribute
        {
            return WithAttribute(typeof(T));
        }

        public IImplementationTypeFilter WithAttribute(Type attributeType)
        {
            if (attributeType == null)
            {
                throw new ArgumentNullException(nameof(attributeType));
            }

            return Where(t => t.HasAttribute(attributeType));
        }

        public IImplementationTypeFilter WithAttribute<T>(Func<T, bool> predicate) where T : Attribute
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return Where(t => t.HasAttribute(predicate));
        }

        public IImplementationTypeFilter WithoutAttribute<T>() where T : Attribute
        {
            return WithoutAttribute(typeof(T));
        }

        public IImplementationTypeFilter WithoutAttribute(Type attributeType)
        {
            if (attributeType == null)
            {
                throw new ArgumentNullException(nameof(attributeType));
            }

            return Where(t => !t.HasAttribute(attributeType));
        }

        public IImplementationTypeFilter WithoutAttribute<T>(Func<T, bool> predicate) where T : Attribute
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return Where(t => !t.HasAttribute(predicate));
        }

        public IImplementationTypeFilter InNamespaceOf<T>()
        {
            return InNamespaceOf(typeof(T));
        }

        public IImplementationTypeFilter InNamespaceOf(params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return InNamespaces(types.Select(t => t.Namespace));
        }

        public IImplementationTypeFilter InNamespaces(params string[] namespaces)
        {
            if (namespaces == null)
            {
                throw new ArgumentNullException(nameof(namespaces));
            }

            return InNamespaces(namespaces.AsEnumerable());
        }

        public IImplementationTypeFilter InNamespaces(IEnumerable<string> namespaces)
        {
            if (namespaces == null)
            {
                throw new ArgumentNullException(nameof(namespaces));
            }

            return Where(t => namespaces.Any(ns => t.IsInNamespace(ns)));
        }

        public IImplementationTypeFilter NotInNamespaceOf<T>()
        {
            return NotInNamespaceOf(typeof(T));
        }

        public IImplementationTypeFilter NotInNamespaceOf(params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return NotInNamespaces(types.Select(t => t.Namespace));
        }

        public IImplementationTypeFilter NotInNamespaces(params string[] namespaces)
        {
            if (namespaces == null)
            {
                throw new ArgumentNullException(nameof(namespaces));
            }

            return NotInNamespaces(namespaces.AsEnumerable());
        }

        public IImplementationTypeFilter NotInNamespaces(IEnumerable<string> namespaces)
        {
            if (namespaces == null)
            {
                throw new ArgumentNullException(nameof(namespaces));
            }

            return Where(t => namespaces.All(ns => !t.IsInNamespace(ns)));
        }

        public IImplementationTypeFilter Where(Func<Type, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            Types = Types.Where(predicate);
            return this;
        }
    }
}