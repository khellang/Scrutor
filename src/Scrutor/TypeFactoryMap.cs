using System;
using System.Collections.Generic;

namespace Scrutor;

internal struct TypeFactoryMap
{
    public TypeFactoryMap(Func<IServiceProvider, object> implementationFactory, IEnumerable<Type> serviceTypes, Type implementationType)
    {
        ImplementationFactory = implementationFactory;
        ServiceTypes = serviceTypes;
        ImplementationType = implementationType;
    }

    public Func<IServiceProvider, object> ImplementationFactory { get; }

    public IEnumerable<Type> ServiceTypes { get; }

    public Type ImplementationType { get; }

}
