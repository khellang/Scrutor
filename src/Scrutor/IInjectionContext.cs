using System;

namespace Scrutor
{
    public interface IInjectionContext
    {
        Type CreatingServiceType { get; }
    }
}