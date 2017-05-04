using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace FUR10N.NullContracts.FlowAnalysis
{
    /// <summary>
    /// Converts an expression to a string that can be compared with other expressions
    /// </summary>
    public class ExpressionKey
    {
        public readonly string Key;

        public readonly ITypeSymbol Type;

        public ExpressionKey(SyntaxNode expression, SemanticModel model)
        {
            expression = GetBasicExpression(expression);
            if (expression is BinaryExpressionSyntax binaryExpression)
            {
                if (binaryExpression.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.AsExpression)
                {
                    Type = model.GetTypeInfo(binaryExpression.Right).Type;
                    expression = binaryExpression.Left;
                }
            }
            Key = FixKey(expression.ToString());
        }

        public ExpressionKey(SyntaxNode expression, ITypeSymbol type)
        {
            Type = type;
            Key = FixKey(GetBasicExpression(expression).ToString());
        }

        private SyntaxNode GetBasicExpression(SyntaxNode expression)
        {
            if (expression is CastExpressionSyntax cast)
            {
                return GetBasicExpression(cast.Expression);
            }
            else if (expression is ParenthesizedExpressionSyntax paren)
            {
                return GetBasicExpression(paren.Expression);
            }
            else if (expression is AssignmentExpressionSyntax assignment)
            {
                return GetBasicExpression(assignment.Right);
            }
            return expression;
        }

        public bool Contains(ExpressionKey e)
        {
            if (e.Type == null)
            {
                return Key == e.Key;
            }
            return Key == e.Key && e.Type.Equals(Type);
        }

        [Pure]
        private static string FixKey(string key)
        {
            return key.ToString().Replace("?.", ".").RemoveWhitespace();
        }

        public static bool operator ==(ExpressionKey e1, ExpressionKey e2)
        {
            return e1.Key == e2.Key && e1.Type.Equals(e2.Type);
        }

        public static bool operator !=(ExpressionKey e1, ExpressionKey e2)
        {
            return e1 != e2;
        }

        public override bool Equals(object obj)
        {
            var exp = obj as ExpressionKey;
            if (exp != null)
            {
                return exp == this;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override string ToString()
        {
            return Key;
        }
    }

    internal static class ExpressionKeyExtensions
    {
        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
        }
    }
}
