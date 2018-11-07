# Scrutor [![Build status](https://ci.appveyor.com/api/projects/status/j00uyvqnm54rdlkb?svg=true)](https://ci.appveyor.com/project/khellang/scrutor) [![NuGet Package](https://img.shields.io/nuget/v/Scrutor.svg)](https://www.nuget.org/packages/Scrutor)

> Scrutor - I search or examine thoroughly; I probe, investigate or scrutinize  
> From scrūta, as the original sense of the verb was to search through trash. - https://en.wiktionary.org/wiki/scrutor

Assembly scanning and decoration extensions for Microsoft.Extensions.DependencyInjection

## Installation

Install the [Scrutor NuGet Package](https://www.nuget.org/packages/Scrutor).

### Package Manager Console

```
Install-Package Scrutor
```

### .NET Core CLI

```
dotnet add package Scrutor
```

## Usage

The library adds two extension methods to `IServiceCollection`:

* `Scan` - This is the entry point to set up your assembly scanning.
* `Decorate` - This method is used to decorate already registered services.

See **Examples** below for usage examples.

## Examples

### Scanning

```csharp
var collection = new ServiceCollection();

collection.Scan(scan => scan
     // We start out with all types in the assembly of ITransientService
    .FromAssemblyOf<ITransientService>()
        // AddClasses starts out with all public, non-abstract types in this assembly.
        // These types are then filtered by the delegate passed to the method.
        // In this case, we filter out only the classes that are assignable to ITransientService.
        .AddClasses(classes => classes.AssignableTo<ITransientService>())
            // We then specify what type we want to register these classes as.
            // In this case, we want to register the types as all of its implemented interfaces.
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
            .WithScopedLifetime()
        // Generic interfaces are also supported too, e.g. public interface IOpenGeneric<T> 
        .AddClasses(classes => classes.AssignableTo(typeof(IOpenGeneric<>)))
            .AsImplementedInterfaces()
        // And you scan generics with multiple type parameters too
        // e.g. public interface IQueryHandler<TQuery, TResult>
        .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces());
```

### Decoration

```csharp
var collection = new ServiceCollection();

// First, add our service to the collection.
collection.AddSingleton<IDecoratedService, Decorated>();

// Then, decorate Decorated with the Decorator type.
collection.Decorate<IDecoratedService, Decorator>();

// Finally, decorate Decorator with the OtherDecorator type.
// As you can see, OtherDecorator requires a separate service, IService. We can get that from the provider argument.
collection.Decorate<IDecoratedService>((inner, provider) => new OtherDecorator(inner, provider.GetRequiredService<IService>()));

var serviceProvider = collection.BuildServiceProvider();

// When we resolve the IDecoratedService service, we'll get the following structure:
// OtherDecorator -> Decorator -> Decorated
var instance = serviceProvider.GetRequiredService<IDecoratedService>();
```
