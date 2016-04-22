# Scrutor [![Build status](https://ci.appveyor.com/api/projects/status/j00uyvqnm54rdlkb?svg=true)](https://ci.appveyor.com/project/khellang/scrutor)

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
     // We start out with all types in the assembly of ITransientService
    .FromAssemblyOf<ITransientService>()
        // AddClasses starts out with all public, non-abstract types in this assembly.
        // These types are then filtered by the delegate passed to the method.
        // In this case, we filter out only the classes that are assignable to ITransientService
        .AddClasses(classes => classes.AssignableTo<ITransientService>())
            // Whe then specify what type we want to register these classes as.
            // In this case, we wan to register the types as all of its implemented interfaces.
            // So if a type implements 3 interfaces; A, B, C, we'd end up with three separate registrations.
            .AsImplementedInterfaces()
            // And lastly, we specify the lifetime of these registrations.
            .WithTransientLifetime()
        // Here we start again, with a new full set of classes from the assembly above.
        // This time, filtering out only the classes assignable to IScopedService.
        .AddClasses(classes => classes.AssignableTo<IScopedService>())
            // Now, we just want to register these types as a single interface, IScopedService.
            .As<IScopedService>()
            // And again, just specify the lifetime.
            .WithScopedLifetime());
```
