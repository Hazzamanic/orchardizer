using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

namespace Codefixes
{
    [DiagnosticAnalyzer(InjectDiagnosticId, LanguageNames.CSharp)]
    public class CtorInjectionAnalyzer : DiagnosticAnalyzer
    {
        public const string InjectDiagnosticId = "CtorInjection";
        public static readonly DiagnosticDescriptor InjectRule = new DiagnosticDescriptor(InjectDiagnosticId,
                                                                                             "Initialize",
                                                                                             "Can be implemented",
                                                                                             "Usage",
                                                                                             DiagnosticSeverity.Warning,
                                                                                             isEnabledByDefault: true, 
                                                                                             description: "Does some stuff");

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get { return ImmutableArray.Create(SyntaxKind.FieldDeclaration); } }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(InjectRule); } }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
            var mods = fieldDeclaration.Modifiers;

            // not readonly or private
            if (!mods.Any(SyntaxKind.ReadOnlyKeyword) || !mods.Any(SyntaxKind.PrivateKeyword))
                return;

            // not an interface
            var type = fieldDeclaration.Declaration.Type;
            var variableType = ModelExtensions.GetTypeInfo(context.SemanticModel, type).ConvertedType;
            if (variableType.TypeKind != TypeKind.Interface)
                return;

            //    return false;

            var tree = fieldDeclaration.SyntaxTree;
            var constructors = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToList();
            var csor = constructors.FirstOrDefault();

            if (csor != null)
            {
                if (csor.ParameterList.Parameters.ToString().Contains(type.ToString()))
                    return;
            }

            var diagnostic = Diagnostic.Create(InjectRule, fieldDeclaration.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        //public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        //{
        //    if (CanBeInjected((FieldDeclarationSyntax)node, semanticModel))
        //    {
        //        addDiagnostic(Diagnostic.Create(InjectRule, node.GetLocation()));
        //    }
        //}

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
        AnalyzeNode, SyntaxKind.FieldDeclaration);
        }
    }
}