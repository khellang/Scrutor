using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    public interface IAssemblySelector : IFluentInterface
    {
        IImplementationTypeSelector FromAssemblyOf<T>();

        IImplementationTypeSelector FromAssembliesOf(params Type[] types);

        IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types);

        IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies);

        IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies);
    }
}