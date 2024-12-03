using Spectre.Console.Analyzer.Analyzers;

namespace Spectre.Console.Analyzer.Tests.Unit.Analyzers;

public class AotRequiresExplicitTypeAnalyzerTests
{
    [Fact]
    public async Task Should_Warn_When_Type_Argument_Is_Missing()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.IO;

sealed class Settings : CommandSettings
{
    [CommandOption(""-p|--project <PROJECTPATH>"")]
    public DirectoryInfo {|Spectre1050:ProjectPath|} { get; set; }
}
";

        await SpectreAnalyzerVerifier<AotRequiresExplicitTypeAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string> { { "build_property.enableaotanalyzer", "true" }, });
    }

    [Fact]
    public async Task Should_Warn_When_Type_Argument_Mismatches_Property_Type()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.IO;

sealed class Settings : CommandSettings
{
    [CommandOption(""-p|--project <PROJECTPATH>"", typeof(FileInfo))]
    public DirectoryInfo {|Spectre1051:ProjectPath|} { get; set; }
}
";

        await SpectreAnalyzerVerifier<AotRequiresExplicitTypeAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string> { { "build_property.enableaotanalyzer", "true" }, });
    }

    [Fact]
    public async Task Should_Warn_When_Named_Type_Argument_Mismatches_Property_Type()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.IO;

sealed class Settings : CommandSettings
{
    [CommandOption(optionType: typeof(FileInfo), template: ""-p|--project <PROJECTPATH>"")]
    public DirectoryInfo {|Spectre1051:ProjectPath|} { get; set; }
}
";
        await SpectreAnalyzerVerifier<AotRequiresExplicitTypeAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string> { { "build_property.enableaotanalyzer", "true" }, });
    }

    [Fact]
    public async Task Should_Not_Warn_When_Type_Argument_Matches_Property_Type()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.IO;

sealed class Settings : CommandSettings
{
    [CommandOption(""-p|--project <PROJECTPATH>"", typeof(DirectoryInfo))]
    public DirectoryInfo ProjectPath { get; set; }
}
";
    }

    [Fact]
    public async Task Should_Not_Warn_With_Dictionaries()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.Collections.Generic;

sealed class Settings : CommandSettings
{
    [CommandOption(""-p|--project <PROJECTPATH>"")]
    public IDictionary<string, int> Data { get; set; }
}
";

        await SpectreAnalyzerVerifier<AotRequiresExplicitTypeAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string> { { "build_property.enableaotanalyzer", "true" }, });
    }

    [Fact]
    public async Task Should_Not_Warn_When_Named_Type_Argument_Matches_Property_Type()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.IO;

sealed class Settings : CommandSettings
{
    [CommandOption(optionType: typeof(DirectoryInfo), template: ""-p|--project <PROJECTPATH>"")]
    public DirectoryInfo ProjectPath { get; set; }
}
";

        await SpectreAnalyzerVerifier<AotRequiresExplicitTypeAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string> { { "build_property.enableaotanalyzer", "true" }, });
    }
}