namespace Spectre.Console.Analyzer.Fixes.FixProviders;

/// <inheritdoc />
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AotRequiresExplicitTypeCodeFixProvider))]
[Shared]
public class AotRequiresExplicitTypeCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Descriptors.S1050_AotRequiresType.Id, Descriptors.S1051_AotRequiresMatchTypes.Id);

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the property declaration identified by the diagnostic.
        var propertyDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
        if (propertyDeclaration == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add or fix type argument for explicit AOT usage",
                createChangedDocument: c => AddOrUpdateTypeArgumentAsync(context.Document, propertyDeclaration, c),
                equivalenceKey: "AddOrUpdateTypeArgument"),
            diagnostic);
    }

    private async Task<Document> AddOrUpdateTypeArgumentAsync(Document document, PropertyDeclarationSyntax propertyDeclaration, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var propertyType = propertyDeclaration.Type;
        var attributeList = propertyDeclaration.AttributeLists;

        // Find the CommandArgument or CommandOption attribute
        foreach (var attribute in attributeList.SelectMany(al => al.Attributes))
        {
            var name = attribute.Name.ToString();
            if (name != "CommandArgument" && name != "CommandOption")
            {
                continue;
            }

            // Check if the attribute has an argument list
            var argumentList = attribute.ArgumentList;
            if (argumentList == null)
            {
                // No arguments, add a new one with the correct type
                var newArgument = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.TypeOfExpression(propertyType));
                var newArgumentList = SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SeparatedList([newArgument]));

                var newAttribute = attribute.WithArgumentList(newArgumentList);
                root = root.ReplaceNode(attribute, newAttribute);
            }
            else
            {
                // Arguments exist, we need to either update or add the type argument
                var arguments = argumentList.Arguments;
                var typeArgumentFound = false;

                for (var i = 0; i < arguments.Count; i++)
                {
                    var argument = arguments[i];
                    if (argument.Expression is TypeOfExpressionSyntax)
                    {
                        // Update the existing type argument
                        var newArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(propertyType));
                        arguments = arguments.Replace(argument, newArgument);
                        typeArgumentFound = true;
                        break;
                    }
                }

                if (!typeArgumentFound)
                {
                    // Add a new type argument
                    var newArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(propertyType));
                    arguments = arguments.Add(newArgument);
                }

                var newArgumentList = argumentList.WithArguments(arguments);
                var newAttribute = attribute.WithArgumentList(newArgumentList);
                root = root.ReplaceNode(attribute, newAttribute);
            }
        }

        return document.WithSyntaxRoot(root);
    }
}