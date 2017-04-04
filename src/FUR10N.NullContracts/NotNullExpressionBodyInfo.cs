using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FUR10N.NullContracts
{
    internal class NotNullExpressionBodyInfo
    {
        public readonly Location Location;

        public readonly ISymbol Symbol;

        public readonly ExpressionSyntax Expression;

        public NotNullExpressionBodyInfo(Location location, ISymbol symbol, ExpressionSyntax expression)
        {
            this.Location = location;
            this.Symbol = symbol;
            this.Expression = expression;
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }
}
