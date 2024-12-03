namespace Spectre.Console.Analyzer.Analyzers;

/// <summary>
/// Analyzer for proper usage of CommandOption and CommandArgument when in AOT publishing scenarios.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AotRequiresExplicitTypeAnalyzer : SpectreAnalyzer
{
    private const string EnableAotAnalyzerOption = "build_property.enableaotanalyzer";
    private const string CommandSettingsClass = "Spectre.Console.Cli.CommandSettings";
    private static readonly string[] AttributeTypesToAnalyze =
    {
        "Spectre.Console.Cli.CommandOptionAttribute",
        "Spectre.Console.Cli.CommandArgumentAttribute",
    };

    private static readonly string[] _dictionaryTypes =
    [
        "System.Collections.Generic.IDictionary<TKey, TValue>",
        "System.Linq.ILookup<TKey, TElement>",
        "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>"
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.S1050_AotRequiresType, Descriptors.S1051_AotRequiresMatchTypes);

    /// <inheritdoc />
    protected override void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext)
    {
        if (!IsAotAnalyzerEnabled(compilationStartContext.Options.AnalyzerConfigOptionsProvider))
        {
            return;
        }

        compilationStartContext.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static bool IsAotAnalyzerEnabled(AnalyzerConfigOptionsProvider optionsProvider)
    {
        return optionsProvider.GlobalOptions.TryGetValue(EnableAotAnalyzerOption, out var publishAotValue) && publishAotValue == "true";
    }

    private void AnalyzeNamedType(SymbolAnalysisContext symbolContext)
    {
        if (symbolContext.Symbol is not INamedTypeSymbol classSymbol)
        {
            return;
        }

        if (!IsDerivedFromCommandSettings(classSymbol))
        {
            return;
        }

        foreach (var propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!IsIntrinsicType(propertySymbol.Type) && !IsDictionaryOrImplementsDictionaryInterface(propertySymbol.Type))
            {
                AnalyzePropertyAttributes(propertySymbol, symbolContext);
            }
        }
    }

    private static bool IsDerivedFromCommandSettings(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == CommandSettingsClass)
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsIntrinsicType(ITypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType != SpecialType.None;
    }

    private static bool IsDictionaryOrImplementsDictionaryInterface(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            var namedTypeConstructedFromDisplayString = namedTypeSymbol.ConstructedFrom.ToDisplayString();

            if (_dictionaryTypes.Contains(namedTypeConstructedFromDisplayString))
            {
                return true;
            }
        }

        return typeSymbol.AllInterfaces.Any(i => _dictionaryTypes.Contains(i.ToDisplayString()));
    }

    private void AnalyzePropertyAttributes(IPropertySymbol propertySymbol, SymbolAnalysisContext symbolContext)
    {
        foreach (var attributeData in propertySymbol.GetAttributes())
        {
            if (attributeData.AttributeClass == null || !AttributeTypesToAnalyze.Contains(attributeData.AttributeClass.ToDisplayString()))
            {
                continue;
            }

            AnalyzeAttributeConstructor(propertySymbol, attributeData, symbolContext);
        }
    }

    private void AnalyzeAttributeConstructor(IPropertySymbol propertySymbol, AttributeData attributeData, SymbolAnalysisContext symbolContext)
    {
        if (attributeData.ConstructorArguments.Length > 0)
        {
            AnalyzeConstructorArguments(propertySymbol, attributeData, symbolContext);
        }
        else
        {
            AnalyzeNamedArguments(propertySymbol, attributeData, symbolContext);
        }
    }

    private static void AnalyzeConstructorArguments(IPropertySymbol propertySymbol, AttributeData attributeData, SymbolAnalysisContext symbolContext)
    {
        var lastArgument = attributeData.ConstructorArguments.Last();

        if (lastArgument is { Kind: TypedConstantKind.Type, Value: ITypeSymbol argumentType })
        {
            if (!SymbolEqualityComparer.Default.Equals(argumentType, propertySymbol.Type))
            {
                ReportTypeMismatchDiagnostic(propertySymbol, argumentType, symbolContext);
            }
        }
        else
        {
            ReportMissingTypeArgumentDiagnostic(propertySymbol, symbolContext);
        }
    }

    private static void AnalyzeNamedArguments(IPropertySymbol propertySymbol, AttributeData attributeData, SymbolAnalysisContext symbolContext)
    {
        var hasTypeArgument = false;
        foreach (var namedArgument in attributeData.NamedArguments.Where(arg => arg.Key is "optionType" or "argumentType"))
        {
            hasTypeArgument = true;
            if (namedArgument.Value.Kind == TypedConstantKind.Type && namedArgument.Value.Value is ITypeSymbol namedArgumentType &&
                !SymbolEqualityComparer.Default.Equals(namedArgumentType, propertySymbol.Type))
            {
                ReportTypeMismatchDiagnostic(propertySymbol, namedArgumentType, symbolContext);
            }
        }

        if (!hasTypeArgument)
        {
            ReportMissingTypeArgumentDiagnostic(propertySymbol, symbolContext);
        }
    }

    private static void ReportTypeMismatchDiagnostic(IPropertySymbol propertySymbol, ITypeSymbol argumentType, SymbolAnalysisContext symbolContext)
    {
        var diagnostic = Diagnostic.Create(Descriptors.S1051_AotRequiresMatchTypes, propertySymbol.Locations[0], propertySymbol.Name, propertySymbol.Type, argumentType);
        symbolContext.ReportDiagnostic(diagnostic);
    }

    private static void ReportMissingTypeArgumentDiagnostic(IPropertySymbol propertySymbol, SymbolAnalysisContext symbolContext)
    {
        var diagnostic = Diagnostic.Create(Descriptors.S1050_AotRequiresType, propertySymbol.Locations[0], propertySymbol.Name);
        symbolContext.ReportDiagnostic(diagnostic);
    }
}
