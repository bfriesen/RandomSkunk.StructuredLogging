using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

/// <summary>
/// The metadata references needed to compile a test source snippet that uses
/// Microsoft.Extensions.Logging.Abstractions, shared by <see cref="AnalyzerVerifier"/> and
/// <see cref="CodeFixVerifier"/>.
/// </summary>
internal static class TestReferences
{
    public static readonly ImmutableArray<MetadataReference> All = Build();

    private static ImmutableArray<MetadataReference> Build()
    {
        var trustedPlatformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);

        var paths = trustedPlatformAssemblies
            .Append(typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location)
            .Distinct();

        return [.. paths.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }
}
