using System;

namespace Scrutor
{
    internal class InjectionContext : IInjectionContext
    {
        public Type CreatingServiceType { get; }

        public InjectionContext(Type creatingServiceType)
        {
            CreatingServiceType = creatingServiceType;
        }
    }
}