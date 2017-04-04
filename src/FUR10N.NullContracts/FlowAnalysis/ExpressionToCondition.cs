using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;
using System.Linq;

namespace FUR10N.NullContracts.FlowAnalysis
{
    public class ExpressionToCondition
    {
        private readonly SemanticModel model;

        private readonly LiteralExpressionSyntax NullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        public ExpressionToCondition(SemanticModel model)
        {
            this.model = model;
        }

        public Condition Parse(ConditionType kind, ExpressionSyntax expression)
        {
            using (new OperationTimer(i => Timings.Update(TimingOperation.ExpressionToCondition, i)))
            {
                var list = new LinkedList<BooleanExpression>();
                list.AddFirst(new BooleanExpression(expression));
                Queue<LinkedListNode<BooleanExpression>> queue = new Queue<LinkedListNode<BooleanExpression>>();
                ExplodeExpression(list, list.First, queue);

                while (queue.Count > 0)
                {
                    ExplodeExpression(list, queue.Dequeue(), queue);
                }

                var condition = new Condition(kind);
                var lastOp = Operator.None;
                foreach (var item in list)
                {
                    LogicalType conditionType;
                    switch (lastOp)
                    {
                        case Operator.Or:
                            conditionType = LogicalType.Or;
                            break;
                        case Operator.And:
                            conditionType = LogicalType.And;
                            break;
                        default:
                            conditionType = LogicalType.Mixed;
                            break;
                    }
                    if (item.IsIsExpression)
                    {
                        condition = condition.WithIs(conditionType, new ExpressionKey(item.Expression, item.IsType), item.Expression, item.HasNotPrefix);
                    }
                    else
                    {
                        condition = condition.With(conditionType, new ExpressionKey(item.Expression, model), item.Expression, item.Value);
                    }
                    lastOp = item.Operator;
                }

                return condition;
            }
        }

        private void ExplodeExpression(
            LinkedList<BooleanExpression> list,
            LinkedListNode<BooleanExpression> item,
            Queue<LinkedListNode<BooleanExpression>> queue)
        {
            var expression = item.Value.Expression;
            switch (expression)
            {
                case BinaryExpressionSyntax binaryExpression:
                    {
                        var kind = expression.Kind();
                        if (binaryExpression.IsCheckAgainstNull(out var target, out var value))
                        {
                            item.Value.Expression = target;
                            item.Value.Value = value;
                            item.Value.IsNullCheck = true;
                            queue.Enqueue(item);
                        }
                        else if (kind == SyntaxKind.LogicalOrExpression || kind == SyntaxKind.LogicalAndExpression)
                        {
                            var toReplace = item.Value;
                            item.Value = new BooleanExpression(binaryExpression.Left, ValueType.MaybeNull, GetOp(kind));
                            queue.Enqueue(item);

                            toReplace.Expression = binaryExpression.Right;
                            toReplace.Value = ValueType.MaybeNull;
                            queue.Enqueue(list.AddAfter(item, toReplace));
                        }
                        else if (kind == SyntaxKind.IsExpression)
                        {
                            SyntaxKind newKind = !item.Value.HasNotPrefix ? SyntaxKind.NotEqualsExpression : SyntaxKind.EqualsExpression;
                            var newExpression = SyntaxFactory.BinaryExpression(newKind, binaryExpression.Left, NullLiteral);
                            item.Value.Expression = newExpression;
                            item.Value.IsType = model.GetTypeInfo(binaryExpression.Right).Type;
                            item.Value.OriginalExpression = binaryExpression.Left;
                            queue.Enqueue(item);
                        }
                        break;
                    }
                case PrefixUnaryExpressionSyntax unary:
                    {
                        item.Value.Expression = unary.Operand;
                        if (unary.Kind() == SyntaxKind.LogicalNotExpression)
                        {
                            item.Value.HasNotPrefix = !item.Value.HasNotPrefix;
                        }
                        queue.Enqueue(item);
                        break;
                    }
                case InvocationExpressionSyntax invocation:
                    {
                        ISymbol methodSymbol;
                        try
                        {
                            methodSymbol = model.GetSymbolInfo(item.Value.OriginalExpression ?? item.Value.Expression).Symbol;
                        }
                        catch (System.Exception)
                        {
                            // We create new expressions in some cases and cannot fetch the symbol
                            break;
                        }
                        if (methodSymbol == null)
                        {
                            break;
                        }
                        var systemSymbols = Cache.Get(model).Symbols;
                        if (methodSymbol.Equals(systemSymbols.StringIsNullOrEmpty) || methodSymbol.Equals(systemSymbols.StringIsNullOrWhiteSpace))
                        {
                            var arg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                            if (arg != null)
                            {
                                SyntaxKind kind = item.Value.HasNotPrefix ? SyntaxKind.NotEqualsExpression : SyntaxKind.EqualsExpression;
                                var newExpression = SyntaxFactory.BinaryExpression(
                                    kind,
                                    arg,
                                    NullLiteral);
                                item.Value = new BooleanExpression(newExpression, ValueType.OutOfRange, item.Value.Operator);
                                item.Value.OriginalExpression = arg;
                                queue.Enqueue(item);
                            }
                        }
                        else if (Cache.Get(model).Symbols.UriTryCreate.Any(i => i.Equals((IMethodSymbol)methodSymbol)))
                        {
                            var outParam = invocation.ArgumentList.Arguments.Skip(2).FirstOrDefault()?.Expression;
                            if (outParam != null)
                            {
                                SyntaxKind kind = item.Value.HasNotPrefix ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression;
                                var newExpression = SyntaxFactory.BinaryExpression(
                                    kind,
                                    outParam,
                                    NullLiteral);
                                item.Value = new BooleanExpression(newExpression, ValueType.OutOfRange, item.Value.Operator);
                                item.Value.OriginalExpression = outParam;
                                queue.Enqueue(item);
                            }
                        }
                        break;
                    }
                case ConditionalAccessExpressionSyntax conditionalAccess:
                    {
                        var oldItem = item.Value;
                        var kind = item.Value.Value == ValueType.Null ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression;
                        var op = kind == SyntaxKind.EqualsExpression ? Operator.Or : Operator.And;
                        var firstPart = SyntaxFactory.BinaryExpression(
                            kind,
                            conditionalAccess.Expression,
                            NullLiteral);
                        item.Value = new BooleanExpression(firstPart, item.Value.Value, op);
                        item.Value.OriginalExpression = conditionalAccess.Expression;
                        queue.Enqueue(item);

                        if (conditionalAccess.WhenNotNull is MemberBindingExpressionSyntax binding)
                        {
                            var memberAccess = SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                conditionalAccess.Expression, binding.Name);
                            var newExpression = SyntaxFactory.BinaryExpression(
                                kind,
                                memberAccess,
                                NullLiteral);
                            queue.Enqueue(list.AddAfter(item, new BooleanExpression(newExpression, item.Value.Value, oldItem.Operator)));
                        }
                        else if (conditionalAccess.WhenNotNull is ConditionalAccessExpressionSyntax sub)
                        {
                            if (sub.Expression is MemberBindingExpressionSyntax subsub)
                            {
                                var exp = SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    conditionalAccess.Expression,
                                    subsub.Name);
                                var ca = SyntaxFactory.ConditionalAccessExpression(exp, sub.WhenNotNull);
                                var bin = SyntaxFactory.BinaryExpression(kind, ca, NullLiteral);
                                queue.Enqueue(list.AddAfter(item, new BooleanExpression(bin, item.Value.Value, oldItem.Operator)));
                            }
                        }
                        break;
                    }
                case ParenthesizedExpressionSyntax paren:
                    {
                        item.Value.Expression = paren.Expression;
                        queue.Enqueue(item);
                        break;
                    }
                case AssignmentExpressionSyntax assignment:
                    {
                        if (item.Value.IsNullCheck)
                        {
                            // Only do more processing on things that are null checks
                            // Ex: while ((var item = Next()) != null) ;
                            item.Value.Expression = assignment.Left;
                        }
                        break;
                    }
            }
        }

        [System.Diagnostics.DebuggerStepThrough]
        private static Operator GetOp(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.LogicalAndExpression:
                    return Operator.And;
                case SyntaxKind.LogicalOrExpression:
                    return Operator.Or;
                default:
                    return Operator.None;
            }
        }

        private enum Operator { Or, And, None }

        private class BooleanExpression
        {
            public ExpressionSyntax Expression;

            public Operator Operator = Operator.None;

            public ValueType Value = ValueType.OutOfRange;

            public bool HasNotPrefix;

            public ITypeSymbol IsType;

            public bool IsNullCheck;

            public bool IsIsExpression => IsType != null;

            public ExpressionSyntax OriginalExpression;

            public BooleanExpression(ExpressionSyntax expression)
            {
                this.Expression = expression;
            }

            public BooleanExpression(ExpressionSyntax expression, ValueType value, Operator op)
                : this(expression, value, op, false, false)
            {
            }

            private BooleanExpression(ExpressionSyntax expression, ValueType value, Operator op, bool hasNotPrefix, bool isIsExpression)
            {
                this.Expression = expression;
                this.Value = value;
                this.Operator = op;
                this.HasNotPrefix = hasNotPrefix;
            }

            public BooleanExpression WithExpression(ExpressionSyntax expression, ValueType value)
            {
                return new BooleanExpression(expression, value, Operator, HasNotPrefix, IsIsExpression);
            }

            public override string ToString()
            {
                return $"{Expression} {Operator}";
            }
        }
    }
}
