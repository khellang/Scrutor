using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

public abstract class DecoratorStrategy
{
    protected DecoratorStrategy(Type serviceType)
    {
        ServiceType = serviceType;
    }
    
    public Type ServiceType { get; }
    
    public abstract bool CanDecorate(Type serviceType);
    
    public abstract Func<IServiceProvider, object> CreateDecorator(Type serviceType);
    
    internal static DecoratorStrategy WithType(Type serviceType, Type decoratorType) => 
        Create(serviceType, decoratorType, decoratorFactory: null);

    internal static DecoratorStrategy WithFactory(Type serviceType, Func<object, IServiceProvider, object> decoratorFactory) => 
        Create(serviceType, decoratorType: null, decoratorFactory);
    
    protected static Func<IServiceProvider, object> TypeDecorator(Type serviceType, Type decoratorType) => serviceProvider =>
    {
        var instanceToDecorate = serviceProvider.GetRequiredService(serviceType);
        return ActivatorUtilities.CreateInstance(serviceProvider, decoratorType, instanceToDecorate);
    };

    protected static Func<IServiceProvider, object> FactoryDecorator(Type decorated, Func<object, IServiceProvider, object> creationFactory) => serviceProvider =>
    {
        var instanceToDecorate = serviceProvider.GetRequiredService(decorated);
        return creationFactory(instanceToDecorate, serviceProvider);
    };

    private static DecoratorStrategy Create(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
    {
        if (serviceType.IsOpenGeneric())
        {
            return new OpenGenericDecoratorStrategy(serviceType, decoratorType, decoratorFactory);
        }

        return new ClosedTypeDecoratorStrategy(serviceType, decoratorType, decoratorFactory);
    }
}
