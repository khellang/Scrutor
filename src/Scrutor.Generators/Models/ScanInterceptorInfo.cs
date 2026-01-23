using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Scrutor.Generators.Models;

/// <summary>
/// Information needed to generate an interceptor for a Scan() call.
/// </summary>
internal sealed class ScanInterceptorInfo
{
    /// <summary>
    /// The file path where the Scan() call is located.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The line number (1-based) of the Scan method name.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// The column number (1-based) of the Scan method name.
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// The parsed configuration from the Scan() method chain.
    /// </summary>
    public ScanConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Whether this Scan() call uses UsingAttributes().
    /// </summary>
    public bool UsesAttributes { get; set; }

    /// <summary>
    /// Whether this call has unsupported patterns.
    /// </summary>
    public bool HasUnsupportedPatterns { get; set; }

    /// <summary>
    /// Description of unsupported patterns if any.
    /// </summary>
    public string? UnsupportedPatternDescription { get; set; }

    /// <summary>
    /// The location for diagnostic reporting.
    /// </summary>
    public Location? Location { get; set; }

    /// <summary>
    /// Unique identifier for this interceptor.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The original Scan() call chain source text for documentation.
    /// </summary>
    public string? CallChainText { get; set; }
}
