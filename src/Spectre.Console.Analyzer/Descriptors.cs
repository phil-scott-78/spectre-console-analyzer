using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Spectre.Console.Analyzer.Descriptors.Category;

namespace Spectre.Console.Analyzer;

/// <summary>
/// Code analysis descriptors.
/// </summary>
public static class Descriptors
{
    internal enum Category
    {
        Usage, // 1xxx
    }

    private static readonly ConcurrentDictionary<Category, string> _categoryMapping = new();

    private static DiagnosticDescriptor Rule(string id, string title, Category category, DiagnosticSeverity defaultSeverity, string messageFormat, string? description = null)
    {
        var helpLink = $"https://spectreconsole.net/analyzer/rules/{id.ToLowerInvariant()}";
        const bool IsEnabledByDefault = true;
        return new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            _categoryMapping.GetOrAdd(category, c => c.ToString()),
            defaultSeverity,
            IsEnabledByDefault,
            description,
            helpLink);
    }

    /// <summary>
    /// Gets definitions of diagnostics Spectre1000.
    /// </summary>
    public static DiagnosticDescriptor S1000_UseAnsiConsoleOverSystemConsole { get; } =
        Rule(
            "Spectre1000",
            "Use AnsiConsole instead of System.Console",
            Usage,
            Warning,
            "Use AnsiConsole instead of System.Console");

    /// <summary>
    /// Gets definitions of diagnostics Spectre1010.
    /// </summary>
    public static DiagnosticDescriptor S1010_FavorInstanceAnsiConsoleOverStatic { get; } =
        Rule(
            "Spectre1010",
            "Favor the use of the instance of AnsiConsole over the static helper.",
            Usage,
            Info,
            "Favor the use of the instance of AnsiConsole over the static helper.");

    /// <summary>
    /// Gets definitions of diagnostics Spectre1020.
    /// </summary>
    public static DiagnosticDescriptor S1020_AvoidConcurrentCallsToMultipleLiveRenderables { get; } =
        Rule(
            "Spectre1020",
            "Avoid calling other live renderables while a current renderable is running.",
            Usage,
            Warning,
            "Avoid calling other live renderables while a current renderable is running.");

    /// <summary>
    /// Gets definitions of diagnostics Spectre1020.
    /// </summary>
    public static DiagnosticDescriptor S1021_AvoidPromptCallsDuringLiveRenderables { get; } =
        Rule(
            "Spectre1021",
            "Avoid prompting for input while a current renderable is running.",
            Usage,
            Warning,
            "Avoid prompting for input while a current renderable is running.");

    /// <summary>
    /// Gets definitions of diagnostics Spectre1050.
    /// </summary>
    public static DiagnosticDescriptor S1050_AotRequiresType { get; } =
        Rule(
            "Spectre1050",
            "Types must be explicitly defined when using non-intrinsic properties while publishing in AOT.",
            Usage,
            Warning,
            "Types must be explicitly defined on CommandArgument or CommandOption when using non-intrinsic properties while publish in AOT. ");

    /// <summary>
    /// Gets definitions of diagnostics Spectre1051.
    /// </summary>
    public static DiagnosticDescriptor S1051_AotRequiresMatchTypes { get; } =
        Rule(
            "Spectre1051",
            "Type must match the property type.",
            Usage,
            Warning,
            "Type must match the property type. {0} expected {1}, found {2}");

    /// <summary>
    /// Gets definitions of diagnostics Spectre1052.
    /// </summary>
    public static DiagnosticDescriptor S1052_InvalidCommandSettingPropertyType { get; } =
        Rule(
            id: "S1052",
            title: "Invalid property type for CommandArgument or CommandOption",
            Usage,
            Error,
            messageFormat: "The property '{0}' of type '{1}' is not a valid option type for Spectre.Console commands. {2}");
}