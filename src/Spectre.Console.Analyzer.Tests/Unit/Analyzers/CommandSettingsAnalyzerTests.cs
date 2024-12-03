namespace Spectre.Console.Analyzer.Tests.Unit.Analyzers;

public class CommandSettingsAnalyzerTests
{
    [Fact]
    public async Task Should__Warn_When_Invalid_Setting_Types()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.IO;
using System.Linq;

sealed class Settings : CommandSettings
{
    [CommandArgument(1, ""<PROGRAM>"")]
    public ILookup<int, int> {|S1052:Ba4r|} { get; set; }

    [CommandArgument(2, ""<PROGRAM>"")]
    public MemoryStream {|S1052:Stream|} { get; set; }

    [CommandArgument(3, ""<PROGRAM>"")]
    public int Ok { get; set; }

    [CommandArgument(4, ""<PROGRAM>"")]
    public string AlsoOk { get; set; }

    [CommandArgument(5, ""<PROGRAM>"")]
    public string StillOk { get; set; }
}
";

        await SpectreAnalyzerVerifier<CommandSettingsPropertyAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string>
            {
                { "build_property.enableaotanalyzer", "true" },
            });
    }

    [Fact]
    public async Task Should__Not_Warn_When_Valid_Property_Type()
    {
        const string Source = @"
using Spectre.Console.Cli;

sealed class Settings : CommandSettings
{
    [CommandArgument(1, ""<PROGRAM>"")]
    public string ValidProperty { get; set; }

    [CommandArgument(2, ""<PROGRAM>"")]
    public int AnotherValidProperty { get; set; }
}
";

        await SpectreAnalyzerVerifier<CommandSettingsPropertyAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string>
            {
                { "build_property.enableaotanalyzer", "true" },
            });
    }

    [Fact]
    public async Task Should__Warn_When_Property_Is_Dictionary_With_Invalid_Key_Type()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.Collections.Generic;

sealed class Settings : CommandSettings
{
    [CommandArgument(1, ""<PROGRAM>"")]
    public IDictionary<int, string> {|S1052:InvalidDictionary|} { get; set; }
}
";

        await SpectreAnalyzerVerifier<CommandSettingsPropertyAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string>
            {
                { "build_property.enableaotanalyzer", "true" },
            });
    }

    [Fact]
    public async Task Should__Not_Warn_When_Property_Is_Dictionary_With_Valid_Key_Type()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.Collections.Generic;

sealed class Settings : CommandSettings
{
    [CommandArgument(1, ""<PROGRAM>"")]
    public IDictionary<string, string> ValidDictionary { get; set; }
}
";

        await SpectreAnalyzerVerifier<CommandSettingsPropertyAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string>
            {
                { "build_property.enableaotanalyzer", "true" },
            });
    }

    [Fact]
    public async Task Should__Warn_When_Property_Is_Array_Of_Struct()
    {
        const string Source = @"
using Spectre.Console.Cli;

sealed class Settings : CommandSettings
{
    [CommandArgument(1, ""<PROGRAM>"")]
    public MyStruct[] {|S1052:StructArray|} { get; set; }
}

struct MyStruct
{
}
";

        await SpectreAnalyzerVerifier<CommandSettingsPropertyAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string>
            {
                { "build_property.enableaotanalyzer", "true" },
            });
    }

    [Fact]
    public async Task Should__Not_Warn_When_Property_Has_TypeConverter()
    {
        const string Source = @"
using Spectre.Console.Cli;
using System.ComponentModel;

sealed class Settings : CommandSettings
{
    [CommandArgument(1, ""<PROGRAM>"")]
    [TypeConverter(typeof(MyCustomConverter))]
    public string PropertyWithConverter { get; set; }
}

class MyCustomConverter : TypeConverter { }
";

        await SpectreAnalyzerVerifier<CommandSettingsPropertyAnalyzer>
            .VerifyAnalyzerAsync(Source, new Dictionary<string, string>
            {
                { "build_property.enableaotanalyzer", "true" },
            });
    }
}