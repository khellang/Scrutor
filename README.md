# Scrutor

> Scrutor - I search or examine thoroughly; I probe, investigate or scrutinize  
> From scrūta, as the original sense of the verb was to search through trash. - https://en.wiktionary.org/wiki/scrutor

Assembly scanning extensions for Microsoft.Extensions.DependencyInjection

## Installation

Install the [Scrutor NuGet Package](https://www.nuget.org/packages/Scrutor).

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
