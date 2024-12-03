using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Options;

namespace Spectre.Console.Analyzer.Tests;

public static class SpectreAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, Dictionary<string, string> globalOptions)
        => VerifyCodeFixAsync(source, OutputKind.DynamicallyLinkedLibrary, new[] { expected }, fixedSource, globalOptions);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
        => VerifyCodeFixAsync(source, OutputKind.DynamicallyLinkedLibrary, new[] { expected }, fixedSource, new Dictionary<string, string>());

    public static Task VerifyCodeFixAsync(string source, OutputKind outputKind, DiagnosticResult expected,
        string fixedSource)
        => VerifyCodeFixAsync(source, outputKind, new[] { expected }, fixedSource, new Dictionary<string, string>());

    private static Task VerifyCodeFixAsync(string source, OutputKind outputKind, IEnumerable<DiagnosticResult> expected,
        string fixedSource, Dictionary<string, string> globalOptions)
    {
        var test = new Test
        {
            TestCode = source,
            TestState =
            {
                OutputKind = outputKind,
            },
            GlobalOptions = globalOptions,
            FixedCode = fixedSource,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAnalyzerAsync(source, new Dictionary<string, string>(), expected);
    }

    public static Task VerifyAnalyzerAsync(string source, Dictionary<string, string> globalOptions, params DiagnosticResult[] expected)
    {
        var test = new Test
        {
            TestCode = source,
            CompilerDiagnostics = CompilerDiagnostics.All,
            GlobalOptions = globalOptions,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    // Code fix tests support both analyzer and code fix testing. This test class is derived from the code fix test
    // to avoid the need to maintain duplicate copies of the customization work.
    private class Test : CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier>
    {
        public Dictionary<string, string> GlobalOptions { get; init; } = new();

        public Test()
        {
            ReferenceAssemblies = CodeAnalyzerHelper.CurrentSpectre;
            TestBehaviors |= TestBehaviors.SkipGeneratedCodeCheck;
        }

        protected override AnalyzerOptions GetAnalyzerOptions(Project project)
        {
            return new AnalyzerOptions([], new TestAnalyzerConfigOptionsProvider(GlobalOptions));
        }

        protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
        {
            var analyzer = new TAnalyzer();
            foreach (var provider in CodeFixProviderDiscovery.GetCodeFixProviders(Language))
            {
                if (analyzer.SupportedDiagnostics.Any(diagnostic =>
                        provider.FixableDiagnosticIds.Contains(diagnostic.Id)))
                {
                    yield return provider;
                }
            }
        }
    }
}

/// <summary>
/// Custom analyzer config options provider to allow us to test the analyzer when EnableAot is enabled or not.
/// </summary>
/// <param name="globalOptions">The global options to set.</param>
internal class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
    : AnalyzerConfigOptionsProvider
{
    private readonly TestAnalyzerConfigOptions _globalOptions = new(globalOptions);

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestAnalyzerConfigOptions();
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestAnalyzerConfigOptions();
}

internal class TestAnalyzerConfigOptions(Dictionary<string, string> options = null) : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _options = options ?? new Dictionary<string, string>();

    public override bool TryGetValue(string key, out string value) => _options.TryGetValue(key, out value);
    public override IEnumerable<string> Keys => _options.Keys;
}