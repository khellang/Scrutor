using System;
using System.Collections.Generic;

namespace Scrutor;

internal struct TypeFactoryMap
{
    public TypeFactoryMap(Type implementationType, Func<IServiceProvider, object> implementationFactory, IEnumerable<Type> serviceTypes)
    {
        ImplementationType = implementationType;
        ImplementationFactory = implementationFactory;
        ServiceTypes = serviceTypes;
    }

    public Type ImplementationType { get; }

    public Func<IServiceProvider, object> ImplementationFactory { get; }

    public IEnumerable<Type> ServiceTypes { get; }
}
