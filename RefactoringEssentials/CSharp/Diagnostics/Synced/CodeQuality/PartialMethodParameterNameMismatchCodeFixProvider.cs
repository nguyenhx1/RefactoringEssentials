using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace RefactoringEssentials.CSharp.Diagnostics
{
    [ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
    public class PartialMethodParameterNameMismatchCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(CSharpDiagnosticIDs.PartialMethodParameterNameMismatchAnalyzerID);
            }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnostic = diagnostics.First();
            var node = root.FindNode(context.Span);
            if (node == null)
                return;
            var parameter = node.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
            var method = parameter != null ? parameter.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault() : null;
            if (method == null)
                return;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;

            var idx = method.ParameterList.Parameters.IndexOf(parameter);

            var newName = symbol.PartialDefinitionPart.Parameters[idx].Name;
            var newRoot = root.ReplaceNode(parameter, parameter.WithIdentifier(SyntaxFactory.Identifier(newName)));
            context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, string.Format(GettextCatalog.GetString("Rename to '{0}'"), newName), document.WithSyntaxRoot(newRoot)), diagnostic);
        }
    }
}