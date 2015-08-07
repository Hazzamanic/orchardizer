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
                newCtor.AddBodyStatements(CreateCtorBody())
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
    }
}
