using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FUR10N.NullContracts
{
    internal class NotNullFieldInfo
    {
        public readonly Location Location;

        public readonly ISymbol Symbol;

        public readonly EqualsValueClauseSyntax Initializer;

        public readonly LocalDeclarationStatementSyntax TempDeclaration;

        public readonly string TempDeclarationName;

        public NotNullFieldInfo(Location location, ISymbol symbol, EqualsValueClauseSyntax initializer)
        {
            this.Location = location;
            this.Symbol = symbol;
            this.Initializer = initializer;

            TempDeclarationName = $"nc_temp_{symbol.Name}";
            TempDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                .WithTrailingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(TempDeclarationName)))));
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }
}
