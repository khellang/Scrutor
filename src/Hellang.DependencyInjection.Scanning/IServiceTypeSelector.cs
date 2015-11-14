using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    public interface IServiceTypeSelector : IFluentInterface
    {
        ILifetimeSelector AsSelf();

        ILifetimeSelector As<TService>();

        ILifetimeSelector As(params Type[] types);

        ILifetimeSelector As(IEnumerable<Type> types);

        ILifetimeSelector AsImplementedInterfaces();

        ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector);
    }
}