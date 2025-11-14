using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection DecorateKeyed<TService, TDecorator>(this IServiceCollection services, object serviceKey) where TDecorator : TService =>
        services.DecorateKeyed(typeof(TService), serviceKey, typeof(TDecorator));

    public static IServiceCollection DecorateKeyed(this IServiceCollection services, Type serviceType, object serviceKey, Type decoratorType) =>
        services.DecoratedKeyedInternal(
            serviceType,
            serviceKey,
            decoratorType: decoratorType,
            decorator: null);


    public static IServiceCollection DecorateKeyed<TService>(this IServiceCollection services, object serviceKey, Func<TService, TService> decorator) where TService : notnull =>
        services.DecorateKeyed<TService>(serviceKey, (service, _) => decorator(service));

    public static IServiceCollection DecorateKeyed<TService>(this IServiceCollection services, object serviceKey, Func<TService, IServiceProvider, TService> decorator) where TService : notnull =>
        services.DecoratedKeyedInternal(
            serviceType: typeof(TService),
            serviceKey,
            decoratorType: null,
            decorator: (service, provider) => decorator((TService)service, provider));


    public static IServiceCollection DecorateKeyed(this IServiceCollection services, Type serviceType, object serviceKey, Func<object, object> decorator) =>
        services.DecorateKeyed(serviceType, serviceKey, (decorated, _) => decorator(decorated));

    public static IServiceCollection DecorateKeyed(this IServiceCollection services, Type serviceType, object serviceKey, Func<object, IServiceProvider, object> decorator) =>
        services.DecoratedKeyedInternal(
            serviceType,
            serviceKey,
            decoratorType: null,
            decorator);


    private static IServiceCollection DecoratedKeyedInternal(this IServiceCollection services,
        Type serviceType,
        object serviceKey,
        Type? decoratorType,
        Func<object, IServiceProvider, object>? decorator)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (serviceKey == null)
            throw new ArgumentNullException(nameof(serviceKey));
        if (decoratorType == null && decorator == null)
            throw new ArgumentException($"Either {nameof(decoratorType)} or {nameof(decorator)} must be specified");

        //we get the only added service descriptor for the specified key.
        ServiceDescriptor existingDescriptor = services.Single(descriptor =>
            descriptor.ServiceType == serviceType &&
            descriptor.ServiceKey != null &&
            descriptor.ServiceKey.Equals(serviceKey));
        if (!existingDescriptor.IsKeyedService)
            throw new InvalidOperationException("Existing descriptor is not a keyed service descriptor.");

        //creating a new collection for decorating.
        IServiceCollection decoratingServices = new ServiceCollection();

        //adding an existing handle without a key for the possibility of decorating it.
        if (existingDescriptor.KeyedImplementationType != null)
            decoratingServices.Add(new ServiceDescriptor(serviceType,
                implementationType: existingDescriptor.KeyedImplementationType,
                lifetime: existingDescriptor.Lifetime));
        else if (existingDescriptor.KeyedImplementationInstance != null)
            decoratingServices.Add(new ServiceDescriptor(serviceType,
                instance: existingDescriptor.KeyedImplementationInstance));
        else if (existingDescriptor.KeyedImplementationFactory != null)
        {
            object serviceKeyLocal = serviceKey;
            decoratingServices.Add(new ServiceDescriptor(serviceType,
                factory: (serviceProvider) => existingDescriptor.KeyedImplementationFactory(serviceProvider, serviceKeyLocal),
                lifetime: existingDescriptor.Lifetime));
        }
        else
            throw new InvalidOperationException("No implementation found in the existing service descriptor.");

        //we are decorating the service.
        if (decoratorType != null)
            decoratingServices.Decorate(serviceType, decoratorType);
        else if (decorator != null)
            decoratingServices.Decorate(serviceType, decorator);

        //deleting the existing handle.
        int existingDescriptorIndex = services.IndexOf(existingDescriptor);
        services.Remove(existingDescriptor);

        //getting a decorated service descriptors.
        ServiceDescriptor[] decoratedDescriptors = decoratingServices.Where(descriptor =>
            descriptor.ServiceType == serviceType).ToArray();

        //we process the decorated service and the decorator wrappers by key.
        bool targetServiceAdded = false;
        foreach (ServiceDescriptor decoratedDescriptor in decoratedDescriptors)
        {
            if (!decoratedDescriptor.IsKeyedService)
            {
                if (targetServiceAdded)
                    throw new InvalidOperationException("Decorated service has already been added.");

                //adding a decorated service using a key.
                if (decoratedDescriptor.ImplementationType != null)
                    services.Insert(existingDescriptorIndex, new ServiceDescriptor(serviceType,
                        serviceKey: serviceKey,
                        implementationType: decoratedDescriptor.ImplementationType,
                        lifetime: decoratedDescriptor.Lifetime));
                else if (decoratedDescriptor.ImplementationFactory != null)
                    services.Insert(existingDescriptorIndex, new ServiceDescriptor(serviceType,
                        serviceKey: serviceKey,
                        factory: (serviceProvider, _) => decoratedDescriptor.ImplementationFactory(serviceProvider),
                        lifetime: decoratedDescriptor.Lifetime));
                else
                    throw new InvalidOperationException("No implementations in the target service descriptor decorator.");

                targetServiceAdded = true;
            }
            else
            {
                //adding substituted keyed descriptors for the source services.
                if (decoratedDescriptor.KeyedImplementationType != null)
                    services.Insert(existingDescriptorIndex, new ServiceDescriptor(serviceType,
                        serviceKey: decoratedDescriptor.ServiceKey,
                        implementationType: decoratedDescriptor.KeyedImplementationType,
                        lifetime: decoratedDescriptor.Lifetime));
                else if (decoratedDescriptor.KeyedImplementationInstance != null)
                    services.Insert(existingDescriptorIndex, new ServiceDescriptor(serviceType,
                        serviceKey: decoratedDescriptor.ServiceKey,
                        instance: decoratedDescriptor.KeyedImplementationInstance));
                else if (decoratedDescriptor.KeyedImplementationFactory != null)
                    services.Insert(existingDescriptorIndex, new ServiceDescriptor(serviceType,
                        serviceKey: decoratedDescriptor.ServiceKey,
                        factory: decoratedDescriptor.KeyedImplementationFactory,
                        lifetime: decoratedDescriptor.Lifetime));
                else
                    throw new InvalidOperationException("No implementations in the intermediate service descriptor.");
            }

            existingDescriptorIndex++;
        }

        return services;
    }
}
