using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codefixes.Helpers
{
    public static class RoslynExtensions
    {
        public static ConstructorDeclarationSyntax CtorInjection(ConstructorDeclarationSyntax ctor, IEnumerable<KeyValuePair<string, TypeSyntax>> values)
        {
            var old = ctor;
            var newCtor = old;

            foreach (var pair in values)
            {

                //TODO: check it doesn't contain expression already
                //newCtor.AddBodyStatements(CreateCtorBody())
            }

            return ctor;
        }

        public static ExpressionStatementSyntax CreateCtorBody(string parameterName, string fieldName)
        {
            return SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(fieldName),
                            SyntaxFactory.IdentifierName(parameterName)
                        )
                    );
        }

        public static string GetFieldVariableName(TypeSyntax type)
        {
            return GetFieldVariableName(type, false);
        }
        public static string GetFieldVariableName(TypeSyntax type, bool underscore)
        {
            var name = type.ToString();
            if (name.StartsWith("I"))
                name = name.Substring(1);

            var firstChara = name[0];
            if (Char.IsUpper(firstChara))
            {
                name = firstChara.ToString().ToLower() + name.Substring(1);
            }
            else
            {
                name += 1;
            }

            return name;
        }

        public static string GetFieldName(FieldDeclarationSyntax field)
        {
            var variable = field.Declaration.Variables.FirstOrDefault(); //.DescendantNodes().Where(e => e.IsKind(SyntaxKind.IdentifierToken)).Last().Span.ToString();
            if (variable == null)
                return "";
            return variable.Identifier.ToString();
        }

        public static ParameterListSyntax GenerateParameters(IEnumerable<TypeSyntax> types)
        {
            var list = new List<ParameterSyntax>();
            foreach (var type in types)
            {
                var p = SyntaxFactory.Parameter(
                    new SyntaxList<AttributeListSyntax>(),
                    new SyntaxTokenList(),
                    type,
                    SyntaxFactory.Identifier(GetFieldVariableName(type)),
                    null);

                list.Add(p);
            }

            return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(list));
        }

        public static StatementSyntax GenerateCtorStatement(string field, string param)
        {
            return SyntaxFactory.ExpressionStatement(
                          SyntaxFactory.AssignmentExpression(
                          SyntaxKind.SimpleAssignmentExpression,
                          SyntaxFactory.IdentifierName(field),
                          SyntaxFactory.IdentifierName(param)));
        }
    }
}
