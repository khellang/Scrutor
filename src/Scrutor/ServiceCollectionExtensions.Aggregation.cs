using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Scrutor;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection Aggregate<TService, TAggregator>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Preconditions.NotNull(services, nameof(services));

            return services.AggregateDescriptors(
                typeof(TService),
                descriptors => descriptors.Aggregate(typeof(TService), typeof(TAggregator), lifetime));
        }

        public static bool TryAggregate<TService, TAggregator>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Preconditions.NotNull(services, nameof(services));

            return services.TryAggregateDescriptors(
                typeof(TService),
                descriptors => descriptors.Aggregate(typeof(TService), typeof(TAggregator), lifetime));
        }

        public static IServiceCollection Aggregate<TService>(this IServiceCollection services, Func<IEnumerable<TService>, TService> aggregator, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            return services.Aggregate<TService>((children, _) => aggregator(children), lifetime);
        }

        public static IServiceCollection Aggregate<TService>(this IServiceCollection services, Func<IEnumerable<TService>, IServiceProvider, TService> aggregator, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(aggregator, nameof(aggregator));

            return services.AggregateDescriptors(
                typeof(TService),
                descriptors => descriptors.Aggregate(
                    typeof(TService),
                    lifetime,
                    (children, provider) => aggregator(children.Cast<TService>(), provider)));
        }

        public static bool TryAggregate<TService>(this IServiceCollection services, Func<IEnumerable<TService>, TService> aggregator, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            return services.TryAggregate<TService>((children, _) => aggregator(children), lifetime);
        }

        public static bool TryAggregate<TService>(this IServiceCollection services, Func<IEnumerable<TService>, IServiceProvider, TService> aggregator, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(aggregator, nameof(aggregator));

            return services.TryAggregateDescriptors(
                typeof(TService),
                descriptors => descriptors.Aggregate(
                    typeof(TService),
                    lifetime,
                    (children, provider) => aggregator(children.Cast<TService>(), provider)));
        }

        public static IServiceCollection Aggregate(this IServiceCollection services, Type serviceType, Type aggregatorType, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(aggregatorType, nameof(aggregatorType));

            if (serviceType.IsOpenGeneric() && aggregatorType.IsOpenGeneric())
            {
                return services.AggregateOpenGeneric(serviceType, aggregatorType, lifetime);
            }

            return services.AggregateDescriptors(
                serviceType,
                descriptors => descriptors.Aggregate(serviceType, aggregatorType, lifetime));
        }

        public static bool TryAggregate(this IServiceCollection services, Type serviceType, Type aggregatorType, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(aggregatorType, nameof(aggregatorType));

            if (serviceType.IsOpenGeneric() && aggregatorType.IsOpenGeneric())
            {
                return services.TryAggregateOpenGeneric(serviceType, aggregatorType, lifetime);
            }

            return services.TryAggregateDescriptors(
                serviceType,
                descriptors => descriptors.Aggregate(serviceType, aggregatorType, lifetime));
        }

        public static IServiceCollection Aggregate(this IServiceCollection services, Type serviceType, Func<IEnumerable<object>, object> aggregator, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(aggregator, nameof(aggregator));

            return services.AggregateDescriptors(serviceType, x => x.Aggregate(serviceType, lifetime, aggregator));
        }

        public static bool TryAggregate(this IServiceCollection services, Type serviceType, Func<IEnumerable<object>, object> aggregator, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(aggregator, nameof(aggregator));

            return services.TryAggregateDescriptors(serviceType, x => x.Aggregate(serviceType, lifetime, aggregator));
        }

        private static IServiceCollection AggregateOpenGeneric(this IServiceCollection services, Type serviceType, Type aggregatorType, ServiceLifetime lifetime)
        {
            if (services.TryAggregateOpenGeneric(serviceType, aggregatorType, lifetime))
            {
                return services;
            }

            throw new MissingTypeRegistrationException(serviceType);
        }

        private static bool TryAggregateOpenGeneric(this IServiceCollection services, Type openGenericServiceType, Type openGenericAggregatorType, ServiceLifetime lifetime)
        {
            var descriptors = services
                .Where(descriptor => descriptor.ServiceType.IsAssignableTo(openGenericServiceType))
                .ToList();
            if (descriptors.Count == 0)
            {
                return false;
            }

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            var closedGenericArguments = descriptors
                .Where(descriptor => !descriptor.ServiceType.IsOpenGeneric())
                .Select(descriptor => descriptor.ServiceType.GenericTypeArguments)
                .Distinct()
                .ToList();
            foreach (var arguments in closedGenericArguments)
            {
                var closedGenericDescriptors = descriptors
                    .Where(descriptor => descriptor.ServiceType.IsOpenGeneric()
                                         || descriptor.ServiceType.GenericTypeArguments.SequenceEqual(arguments));
                var closedServiceType = openGenericServiceType.MakeGenericType(arguments);
                var closedAggregatorType = openGenericAggregatorType.MakeGenericType(arguments);
                var aggregatorDescriptor = closedGenericDescriptors.AggregateOpenGenerics(closedServiceType, closedAggregatorType, lifetime);
                services.Add(aggregatorDescriptor);
            }

            //var openGenericDescriptors = descriptors
            //    .Where(descriptor => descriptor.ServiceType.IsOpenGeneric())
            //    .ToList();
            //if (openGenericDescriptors.Count > 0)
            //{
            //    openGenericDescriptors.AggregateOpenGenerics(openGenericServiceType, openGenericAggregatorType, )
            //}

            return true;
        }

        private static IServiceCollection AggregateDescriptors(this IServiceCollection services, Type serviceType, Func<IEnumerable<ServiceDescriptor>, ServiceDescriptor> aggregator)
        {
            if (services.TryAggregateDescriptors(serviceType, aggregator))
            {
                return services;
            }

            throw new MissingTypeRegistrationException(serviceType);
        }

        private static bool TryAggregateDescriptors(this IServiceCollection services, Type serviceType, Func<IEnumerable<ServiceDescriptor>, ServiceDescriptor> aggregator)
        {
            if (!services.TryGetDescriptors(serviceType, out var descriptors))
            {
                return false;
            }

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.Add(aggregator(descriptors));

            return true;
        }

        private static ServiceDescriptor Aggregate<TService>(this IEnumerable<ServiceDescriptor> descriptors, ServiceLifetime lifetime, Func<IEnumerable<TService>, TService> aggregator)
        {
            return descriptors.Aggregate<TService>(lifetime, (children, _) => aggregator(children));
        }

        private static ServiceDescriptor Aggregate<TService>(this IEnumerable<ServiceDescriptor> descriptors, ServiceLifetime lifetime, Func<IEnumerable<TService>, IServiceProvider, TService> aggregator)
        {
            return descriptors.Aggregate(typeof(TService), lifetime, (children, provider) => aggregator(children.Cast<TService>(), provider));
        }

        private static ServiceDescriptor Aggregate(this IEnumerable<ServiceDescriptor> descriptors, Type serviceType, Type aggregatorType, ServiceLifetime lifetime)
        {
            return descriptors.Aggregate(serviceType, lifetime, (children, provider) => provider.CreateInstance(aggregatorType, children));
        }

        private static ServiceDescriptor Aggregate(this IEnumerable<ServiceDescriptor> descriptors, Type serviceType, ServiceLifetime lifetime, Func<IEnumerable<object>, object> aggregator)
        {
            return descriptors.Aggregate(serviceType, lifetime, (children, _) => aggregator(children));
        }

        private static ServiceDescriptor Aggregate(this IEnumerable<ServiceDescriptor> descriptors, Type serviceType, ServiceLifetime lifetime, Func<IEnumerable<object>, IServiceProvider, object> aggregator)
        {
            return ServiceDescriptor.Describe(
                serviceType,
                provider => aggregator(provider.GetInstances(serviceType, descriptors), provider),
                lifetime);
        }

        private static ServiceDescriptor AggregateOpenGenerics(this IEnumerable<ServiceDescriptor> descriptors, Type serviceType, Type aggregatorType, ServiceLifetime lifetime)
        {
            return descriptors.AggregateOpenGenerics(
                serviceType,
                lifetime,
                (children, provider) => provider.CreateInstance(aggregatorType, children));
        }

        private static ServiceDescriptor AggregateOpenGenerics(this IEnumerable<ServiceDescriptor> descriptors, Type serviceType, ServiceLifetime lifetime, Func<IEnumerable<object>, IServiceProvider, object> aggregator)
        {
            return descriptors.AggregateOpenGenerics(serviceType, serviceType.GenericTypeArguments, lifetime, aggregator);
        }

        private static ServiceDescriptor AggregateOpenGenerics(this IEnumerable<ServiceDescriptor> descriptors, Type serviceType, Type[] genericTypeArguments, ServiceLifetime lifetime, Func<IEnumerable<object>, IServiceProvider, object> aggregator)
        {
            return ServiceDescriptor.Describe(
                serviceType,
                provider => aggregator(provider.GetOpenGenericInstances(serviceType, genericTypeArguments, descriptors), provider),
                lifetime);
        }

        private static IEnumerable<object> GetInstances(this IServiceProvider provider, Type serviceType, IEnumerable<ServiceDescriptor> descriptors)
        {
            var instances = provider.GetInstances(descriptors);
            var resultType = typeof(CastingEnumerable<>).MakeGenericType(serviceType);
            return (IEnumerable<object>)Activator.CreateInstance(resultType, instances);
        }

        private static IEnumerable<object> GetOpenGenericInstances(this IServiceProvider provider, Type serviceType, Type[] genericTypeArguments, IEnumerable<ServiceDescriptor> descriptors)
        {
            var instances = provider.GetOpenGenericInstances(descriptors, genericTypeArguments);
            var resultType = typeof(CastingEnumerable<>).MakeGenericType(serviceType);
            return (IEnumerable<object>)Activator.CreateInstance(resultType, instances);
        }

        private static IEnumerable<object> GetInstances(this IServiceProvider provider, IEnumerable<ServiceDescriptor> descriptors)
        {
            return descriptors.Select(provider.GetInstance);
        }

        private static IEnumerable<object> GetOpenGenericInstances(this IServiceProvider provider, IEnumerable<ServiceDescriptor> descriptors, Type[] genericTypeArguments)
        {
            return descriptors.Select(descriptor => provider.GetOpenGenericInstance(descriptor, genericTypeArguments));
        }

        private static object GetOpenGenericInstance(this IServiceProvider provider, ServiceDescriptor descriptor, Type[] genericTypeArguments)
        {
            if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance;
            }

            if (descriptor.ImplementationType != null)
            {
                var implementationType = descriptor.ImplementationType.IsOpenGeneric()
                    ? descriptor.ImplementationType.MakeGenericType(genericTypeArguments)
                    : descriptor.ImplementationType;
                return provider.GetServiceOrCreateInstance(implementationType);
            }

            return descriptor.ImplementationFactory(provider);
        }

        private class CastingEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _delegate;

            public CastingEnumerable([NotNull] IEnumerable<object> @delegate)
            {
                _delegate = @delegate?.Cast<T>() ?? throw new ArgumentNullException(nameof(@delegate));
            }

            public IEnumerator<T> GetEnumerator() => _delegate.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
