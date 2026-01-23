using Microsoft.CodeAnalysis;
using Scrutor.Generators.Utilities;
using Xunit;

namespace Scrutor.Generators.Tests;

/// <summary>
/// Tests for diagnostic reporting.
/// </summary>
public class DiagnosticTests
{
    [Fact]
    public void DiagnosticDescriptors_HaveCorrectIds()
    {
        Assert.Equal("SCRUT001", DiagnosticDescriptors.UnsupportedPattern.Id);
        Assert.Equal("SCRUT002", DiagnosticDescriptors.InvalidServiceDescriptor.Id);
        Assert.Equal("SCRUT003", DiagnosticDescriptors.TypeMismatch.Id);
        Assert.Equal("SCRUT004", DiagnosticDescriptors.DuplicateRegistration.Id);
        Assert.Equal("SCRUT005", DiagnosticDescriptors.GenerationInfo.Id);
    }

    [Fact]
    public void DiagnosticDescriptors_HaveCorrectSeverities()
    {
        Assert.Equal(DiagnosticSeverity.Warning, DiagnosticDescriptors.UnsupportedPattern.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Error, DiagnosticDescriptors.InvalidServiceDescriptor.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Error, DiagnosticDescriptors.TypeMismatch.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Warning, DiagnosticDescriptors.DuplicateRegistration.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Info, DiagnosticDescriptors.GenerationInfo.DefaultSeverity);
    }

    [Fact]
    public void DiagnosticDescriptors_AreEnabled()
    {
        Assert.True(DiagnosticDescriptors.UnsupportedPattern.IsEnabledByDefault);
        Assert.True(DiagnosticDescriptors.InvalidServiceDescriptor.IsEnabledByDefault);
        Assert.True(DiagnosticDescriptors.TypeMismatch.IsEnabledByDefault);
        Assert.True(DiagnosticDescriptors.DuplicateRegistration.IsEnabledByDefault);
        Assert.True(DiagnosticDescriptors.GenerationInfo.IsEnabledByDefault);
    }
}
