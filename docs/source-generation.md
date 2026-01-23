# Source Generation Support

Scrutor includes an optional source generator that performs assembly scanning at compile time instead of runtime. This enables AOT (Ahead-of-Time) compilation compatibility and improves startup performance by eliminating runtime reflection.

## How It Works

When enabled, the source generator analyzes your `Scan()` method chains and `[ServiceDescriptor]` attributes at compile time. It then generates **interceptor methods** that replace the runtime `Scan()` calls with pre-computed service registrations.

This means your existing code continues to work unchanged - the generator transparently intercepts the `Scan()` calls and substitutes optimized, statically-generated registrations.

## Enabling Source Generation

Add the following property to your project file:

```xml
<PropertyGroup>
  <ScrutorGenerateServices>true</ScrutorGenerateServices>
</PropertyGroup>
```

That's it. No code changes are required - your existing `Scan()` calls are automatically optimized at compile time.

## Usage Patterns

### Pattern 1: Scan() Method Chains

The most common pattern - your existing Scan() calls are intercepted automatically:

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<IMyService>()
    .AddClasses(c => c.AssignableTo<IMyService>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

At compile time, this is replaced with direct registration calls like:

```csharp
services.AddScoped<IMyService, MyServiceImpl>();
services.AddScoped<IMyService, AnotherServiceImpl>();
// ... all discovered types
```

### Pattern 2: ServiceDescriptor Attributes

Mark classes with `[ServiceDescriptor]` attributes and use `UsingAttributes()`:

```csharp
[ServiceDescriptor(typeof(IEmailService), ServiceLifetime.Scoped)]
public class EmailService : IEmailService { }

// Generic version
[ServiceDescriptor<INotificationService>(ServiceLifetime.Singleton)]
public class NotificationService : INotificationService { }

// With service key
[ServiceDescriptor<ICache>(ServiceLifetime.Singleton, "redis")]
public class RedisCache : ICache { }
```

Then scan with attribute-based registration:

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<EmailService>()
    .AddClasses()
    .UsingAttributes());
```

### Pattern 3: Keyed Services

Keyed service registration is fully supported:

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<IHandler>()
    .AddClasses(c => c.AssignableTo<IHandler>())
    .AsImplementedInterfaces()
    .WithSingletonLifetime()
    .WithServiceKey("handlers"));
```

Generates:

```csharp
services.AddKeyedSingleton<IHandler, MyHandler>("handlers");
```

## Supported Methods

### Assembly Sources

| Method | Support |
|--------|---------|
| `FromAssemblyOf<T>()` | Full |
| `FromAssemblies(...)` | Full |

### Type Filtering

| Method | Support |
|--------|---------|
| `AddClasses()` | Full |
| `AddClasses(publicOnly: false)` | Full |
| `AssignableTo<T>()` | Full |
| `AssignableTo(typeof(T))` | Full |
| `InNamespace(string)` | Full |
| `InNamespaces(params string[])` | Full |
| `InExactNamespace(string)` | Full |
| `InExactNamespaces(params string[])` | Full |
| `WithAttribute<T>()` | Full |
| `WithoutAttribute<T>()` | Full |

### Service Mapping

| Method | Support |
|--------|---------|
| `AsImplementedInterfaces()` | Full |
| `AsSelf()` | Full |
| `As<T>()` | Full |
| `AsMatchingInterface()` | Full |
| `AsSelfWithInterfaces()` | Full |

### Lifetime

| Method | Support |
|--------|---------|
| `WithSingletonLifetime()` | Full |
| `WithScopedLifetime()` | Full |
| `WithTransientLifetime()` | Full |
| `WithLifetime(ServiceLifetime)` | Full |

### Registration Strategy

| Method | Support |
|--------|---------|
| `UsingRegistrationStrategy(RegistrationStrategy.Skip)` | Full |
| `UsingRegistrationStrategy(RegistrationStrategy.Append)` | Full |
| `UsingRegistrationStrategy(RegistrationStrategy.Replace)` | Full |
| `UsingRegistrationStrategy(RegistrationStrategy.Throw)` | Full |

### Other

| Method | Support |
|--------|---------|
| `WithServiceKey(object)` | Full (literal values) |
| `UsingAttributes()` | Full |

## Unsupported Patterns

The following patterns require runtime evaluation and will **fall back to runtime scanning** (with a diagnostic warning):

| Pattern | Reason |
|---------|--------|
| `FromCallingAssembly()` | Dynamic assembly resolution |
| `FromExecutingAssembly()` | Dynamic assembly resolution |
| `FromEntryAssembly()` | Dynamic assembly resolution |
| `FromApplicationDependencies()` | Dynamic dependency scanning |
| `Where(predicate)` | Custom predicate evaluation |
| `AssignableToAny(...)` | Multiple type checks |
| `WithLifetime(Func<Type, ServiceLifetime>)` | Dynamic lifetime selection |
| `WithServiceKey(Func<Type, object?>)` | Dynamic key selection |

When an unsupported pattern is detected, the generator emits a `SCRUT001` warning and the original runtime `Scan()` behavior is preserved.

## Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| SCRUT001 | Warning | Unsupported pattern in Scan() configuration - falls back to runtime |
| SCRUT002 | Error | Invalid ServiceDescriptor configuration |
| SCRUT003 | Error | Type mismatch in service registration |
| SCRUT004 | Warning | Duplicate service registration detected |
| SCRUT005 | Info | Source generation information |

## Generated Code

The generator creates an interceptor file (visible in your IDE under Dependencies > Analyzers > Scrutor.Generators). Example output:

```csharp
// <auto-generated/>
#nullable enable

namespace Scrutor.Generated
{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Scrutor.Generators", "1.0.0")]
    file static class MyProjectScanInterceptors
    {
        /// <summary>
        /// Intercepts Scan() call at Program.cs:15
        /// </summary>
        [global::System.Runtime.CompilerServices.InterceptsLocation("Program.cs", 15, 10)]
        public static IServiceCollection Scan_0(
            this IServiceCollection services,
            Action<ITypeSourceSelector> action)
        {
            services.AddScoped<IMyService, MyServiceImpl>();
            services.AddScoped<IMyService, AnotherServiceImpl>();
            return services;
        }
    }
}
```

## Benefits

1. **AOT Compatibility**: Works with Native AOT compilation in .NET 8+
2. **Faster Startup**: No runtime reflection or assembly scanning
3. **Trimming Safe**: No dynamic type loading that could be trimmed away
4. **Zero Code Changes**: Existing Scan() calls work transparently
5. **Fallback Support**: Unsupported patterns gracefully fall back to runtime

## Requirements

- .NET 8.0 or later
- C# 12 or later (for interceptor support)
- `ScrutorGenerateServices` property set to `true`

## Troubleshooting

### Generated code not appearing

1. Ensure `<ScrutorGenerateServices>true</ScrutorGenerateServices>` is in your project file
2. Rebuild the project (source generators run on build)
3. Check for analyzer errors in the Error List

### Scan() not being intercepted

1. Verify the pattern is supported (see tables above)
2. Check for SCRUT001 warnings indicating fallback to runtime
3. Ensure the assembly source uses `FromAssemblyOf<T>()` with a concrete type

### Build errors after enabling

1. Check SCRUT002/SCRUT003 errors for invalid configurations
2. Verify service types are accessible from the generated code
3. Ensure all referenced types are in the compilation
