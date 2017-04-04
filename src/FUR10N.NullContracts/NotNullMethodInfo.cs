using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FUR10N.NullContracts
{
    internal class NotNullMethodInfo
    {
        public readonly Location Location;

        public readonly IMethodSymbol Symbol;

        public readonly BlockSyntax Body;

        public NotNullMethodInfo(Location location, IMethodSymbol symbol, BlockSyntax body)
        {
            this.Location = location;
            this.Symbol = symbol;
            this.Body = body;
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }
}
