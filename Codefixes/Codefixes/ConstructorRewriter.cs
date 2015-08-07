using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Codefixes
{
    class ConstructorRewriter : CSharpSyntaxRewriter
    {
        private readonly ConstructorDeclarationSyntax constructor;
        private readonly FieldDeclarationSyntax field;

        public ConstructorRewriter(ConstructorDeclarationSyntax constructor, FieldDeclarationSyntax field)
        {
            this.constructor = constructor;
            this.field = field;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax constructorDeclaration)
        {
            if (constructor == constructorDeclaration)
            {
                var type = field.Declaration.Type;
                var variable = field.DescendantNodes().Where(e => e.IsKind(SyntaxKind.IdentifierToken)).Last().Span;

                string name = variable.ToString();

                var typeString = type.ToString();
                if (typeString.StartsWith("I"))
                    name = typeString.Substring(1);

                name = Char.ToLowerInvariant(name[0]) + name.Substring(1);
                var p = SyntaxFactory.Parameter(
                    new SyntaxList<AttributeListSyntax>(),
                    new SyntaxTokenList(),
                    type,
                    SyntaxFactory.Identifier(name),
                    null);

                var parameters = constructorDeclaration.ParameterList.AddParameters(p);
                var body = constructorDeclaration.Body;
                var statement = SyntaxFactory.ParseStatement("this." + name + " = " + name + ";" + Environment.NewLine);//.WithLeadingTrivia(SyntaxFactory.Tab, SyntaxFactory.Tab);
                body = body.AddStatements(statement);

                return constructorDeclaration.WithParameterList(parameters).WithBody(body); //(BlockSyntax)formatted);
            }

            return base.VisitConstructorDeclaration(constructorDeclaration);
        }
    }
}
