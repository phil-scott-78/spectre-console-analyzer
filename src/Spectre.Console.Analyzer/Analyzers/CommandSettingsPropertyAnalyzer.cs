namespace Spectre.Console.Analyzer;

/// <summary>
/// Analyzer for validating properties of CommandSettings classes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandSettingsPropertyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor _invalidPropertyTypeDescriptor =
        Descriptors.S1052_InvalidCommandSettingPropertyType;

    private static readonly string[] _dictionaryTypes =
    [
        "System.Collections.Generic.IDictionary<TKey, TValue>",
        "System.Linq.ILookup<TKey, TElement>",
        "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>"
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(_invalidPropertyTypeDescriptor);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol classSymbol)
        {
            return;
        }

        // Step 1: Identify classes deriving from Spectre.Console.Cli.CommandSettings
        if (!IsDerivedFromCommandSettings(classSymbol))
        {
            return;
        }

        // Step 2: Identify properties with CommandArgumentAttribute or CommandOptionAttribute
        foreach (var propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!HasCommandAttribute(propertySymbol))
            {
                continue;
            }

            // Step 3: Skip properties with TypeConverterAttribute
            if (HasTypeConverterAttribute(propertySymbol))
            {
                continue;
            }

            // Step 4: Validate property type
            if (IsInvalidPropertyType(propertySymbol.Type, out var reason))
            {
                var diagnostic = Diagnostic.Create(_invalidPropertyTypeDescriptor, propertySymbol.Locations[0], propertySymbol.Name, propertySymbol.Type, reason);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsDerivedFromCommandSettings(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == "Spectre.Console.Cli.CommandSettings")
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private bool HasCommandAttribute(IPropertySymbol propertySymbol)
    {
        return propertySymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "Spectre.Console.Cli.CommandArgumentAttribute" ||
            attribute.AttributeClass?.ToDisplayString() == "Spectre.Console.Cli.CommandOptionAttribute");
    }

    private static bool HasTypeConverterAttribute(IPropertySymbol propertySymbol)
    {
        return propertySymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "System.ComponentModel.TypeConverterAttribute");
    }

    private static bool IsInvalidPropertyType(ITypeSymbol typeSymbol, out string? reason)
    {
        reason = null;

        // Check for intrinsic types
        if (typeSymbol.SpecialType != SpecialType.None)
        {
            return false;
        }

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            // Check if the type has a single parameter constructor and the parameter is a string
            if (namedTypeSymbol.InstanceConstructors.Any(c => c.Parameters.Length == 1 && c.Parameters[0].Type.SpecialType == SpecialType.System_String))
            {
                return false;
            }

            // Check if type is an invalid interface or implements any invalid interfaces and TKey is not a string
            var namedTypeConstructedFromDisplayString = namedTypeSymbol.ConstructedFrom.ToDisplayString();
            if (_dictionaryTypes.Contains(namedTypeConstructedFromDisplayString))
            {
                if (namedTypeSymbol.TypeArguments[0].SpecialType != SpecialType.System_String)
                {
                    reason = "Dictionary types must have a string type for their key";
                    return true;
                }

                return false;
            }

            var dictionaryType = namedTypeSymbol.AllInterfaces.FirstOrDefault(i => _dictionaryTypes.Contains(i.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            if (dictionaryType != null)
            {
                if (dictionaryType.TypeArguments[0].SpecialType != SpecialType.System_String)
                {
                    reason = "Dictionary types must have a string type for their key";
                    return true;
                }

                return false;
            }
        }

        // Check if type is an array of struct
        if (typeSymbol is IArrayTypeSymbol { ElementType: { IsValueType: true, IsReferenceType: false } })
        {
            reason = "Arrays of structs are not supported";
            return true;
        }

        reason = "Non-intrinsic types are not supported";
        return true;
    }
}
