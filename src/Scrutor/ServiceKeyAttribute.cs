using JetBrains.Annotations;
using System;

namespace Scrutor;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ServiceKeyAttribute(string name): Attribute
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}
