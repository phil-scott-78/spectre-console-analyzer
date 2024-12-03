using Spectre.Console.Cli;

namespace Spectre.Console.Analyzer.Tests;

internal static class CodeAnalyzerHelper
{
    internal static ReferenceAssemblies CurrentSpectre { get; }

    static CodeAnalyzerHelper()
    {
        CurrentSpectre = ReferenceAssemblies.Net.Net90.AddAssemblies(
            ImmutableArray.Create(
                [
                    typeof(AnsiConsole).Assembly.Location.Replace(".dll", string.Empty),
                    typeof(CommandApp).Assembly.Location.Replace(".dll", string.Empty)
                ]));
    }
}
