using System;
using System.Collections.Generic;

namespace Scrutor;

internal struct TypeFactoryMap
{
    public TypeFactoryMap(Func<IServiceProvider, object> implementationFactory, IEnumerable<Type> serviceTypes)
    {
        ImplementationFactory = implementationFactory;
        ServiceTypes = serviceTypes;
    }

    public Func<IServiceProvider, object> ImplementationFactory { get; }

    public IEnumerable<Type> ServiceTypes { get; }
}
