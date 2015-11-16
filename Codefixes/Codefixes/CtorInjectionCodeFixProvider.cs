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
            var constructors = tree.GetRoot(cancellationToken).DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToList();
            var csor = constructors.FirstOrDefault();

            if(csor == null)
            {
                tree.GetRoot().DescendantNodes();
            }

            var cr = new ConstructorRewriter(csor, localDeclaration);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = cr.Visit(root);

            //var workspace = MSBuildWorkspace.Create();
            //var formatted = Formatter.Format(newRoot, workspace);

            return document.WithSyntaxRoot(newRoot);
        }
    }
    }
