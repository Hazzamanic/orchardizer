using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Codefixes.Helpers;

namespace Codefixes
{
    [ExportCodeFixProvider(CtorInjectionAnalyzer.InjectDiagnosticId, LanguageNames.CSharp)]
    internal class CtorInectionCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CtorInjectionAnalyzer.InjectDiagnosticId); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;


            // Find the local declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

            context.RegisterCodeFix(CodeAction.Create("Initialize field", (c) => Initialize(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> Initialize(Document document, FieldDeclarationSyntax localDeclaration,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            var root = tree.GetRoot(cancellationToken);
            var constructors = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToList();
            var csor = constructors.FirstOrDefault();



            SyntaxNode visitingRoot = root;

            if (csor == null)
            {
                var oldClass = localDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                var className = oldClass.Identifier.ToString();

                var paramList = RoslynExtensions.GenerateParameters(new[] { localDeclaration.Declaration.Type });

                var newCtor = SyntaxFactory.ConstructorDeclaration(
                   attributeLists: SyntaxFactory.List(new AttributeListSyntax[] { }),
                   modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                   identifier: oldClass.Identifier,
                   parameterList: paramList,
                   initializer: null,
                   body: SyntaxFactory.Block(new[] { RoslynExtensions.GenerateCtorStatement(RoslynExtensions.GetFieldName(localDeclaration), RoslynExtensions.GetFieldVariableName(localDeclaration.Declaration.Type)) }),
                   semicolonToken: default(SyntaxToken)
                );
                
                csor = newCtor;

                visitingRoot = root.InsertNodesAfter(localDeclaration, new[] { newCtor });
            }

            var cr = new ConstructorRewriter(csor, localDeclaration);
            var newRoot = cr.Visit(visitingRoot).WithAdditionalAnnotations(Formatter.Annotation);

            //var workspace = MSBuildWorkspace.Create();
            //var formatted = Formatter.Format(newRoot, workspace);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
