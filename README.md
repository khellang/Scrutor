# DependencyInjection.Scanning

Assembly scanning extensions for Microsoft.Extensions.DependencyInjection

## Installation

Install the [Hellang.DependencyInjection.Scanning NuGet Package](https://www.nuget.org/packages/Hellang.DependencyInjection.Scanning).

## Usage

The library adds a single extension method, `Scan`, to `IServiceCollection`. This is the entry point to set up your assembly scanning.

### Example

```csharp
var collection = new ServiceCollection();

collection.Scan(scan => scan
    .FromAssemblyOf<ITransientService>()
        .AddClasses(classes => classes.AssignableTo<ITransientService>())
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        .AddClasses(classes => classes.AssignableTo<IScopedService>())
            .As<IScopedService>()
            .WithScopedLifetime());
```
