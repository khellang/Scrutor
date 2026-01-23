using System.Runtime.CompilerServices;

namespace Scrutor.Generators.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();

        // Use Snapshots folder for verified files
        DerivePathInfo(
            (_, projectDirectory, type, method) =>
                new PathInfo(
                    directory: Path.Combine(projectDirectory, "Snapshots"),
                    typeName: type.Name,
                    methodName: method.Name));
    }
}
