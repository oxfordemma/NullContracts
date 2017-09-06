using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;

namespace FUR10N.NullContracts.FlowAnalysis
{
    public class Condition
    {
        private readonly LogicalType? type;

        private readonly ImmutableArray<IPart> parts;

        public readonly ConditionType Kind;

        public Condition(ConditionType kind)
        {
            this.Kind = kind;
            parts = ImmutableArray.Create<IPart>();
        }

        public Condition(ConditionType kind, ExpressionKey key, ExpressionSyntax expression, ValueType value)
        {
            this.Kind = kind;
            parts = ImmutableArray.Create<IPart>(new Part(key, expression, value));
        }

        private Condition(ConditionType kind, LogicalType? type, ImmutableArray<IPart> parts, bool isPartial = false)
        {
            this.Kind = kind;
            if (parts.Length > 1)
            {
                // This only matters when there are multiple parts
                this.type = type ?? throw new InvalidOperationException($"If there are multiple parts to a condition, then it must have a {nameof(LogicalType)}");
            }
            // Only partial commands should retain the type even if there is only one part
            // (since the full condition has multiple parts)
            if (isPartial)
            {
                this.type = type;
            }
            this.parts = parts;
        }

        private static LogicalType CombineType(LogicalType? existing, LogicalType newType)
        {
            if (newType == LogicalType.And)
            {
                if (existing.GetValueOrDefault(LogicalType.And) == LogicalType.And)
                {
                    return LogicalType.And;
                }
                else
                {
                    return LogicalType.Mixed;
                }
            }
            if (newType == LogicalType.Or)
            {
                if (existing.GetValueOrDefault(LogicalType.Or) == LogicalType.Or)
                {
                    return LogicalType.Or;
                }
                else
                {
                    return LogicalType.Mixed;
                }
            }
            return LogicalType.Mixed;
        }

        [Pure]
        public Condition WithIs(LogicalType op, ExpressionKey key, ExpressionSyntax expression, bool hasNotPrefix)
        {
            var newType = CombineType(type, op);
            return new Condition(Kind, newType, parts.Add(new IsPart(key, expression, hasNotPrefix)));
        }

        [Pure]
        public Condition With(LogicalType op, ExpressionKey key, ExpressionSyntax expression, ValueType value)
        {
            var newType = CombineType(type, op);
            return new Condition(Kind, newType, parts.Add(new Part(key, expression, value)));
        }

        [Pure]
        public Condition With(LogicalType op, Condition condition)
        {
            var newType = CombineType(type, op);
            return new Condition(Kind, newType, parts.AddRange(condition.parts));
        }

        [Pure]
        public bool IsConstraintFor(SyntaxNode node, SemanticModel model)
        {
            ExpressionKey key;
            switch (node)
            {
                case AssignmentExpressionSyntax assignment:
                    key = new ExpressionKey(assignment.Left, model);
                    break;
                default:
                    key = new ExpressionKey(node, model);
                    break;
            }

            return Kind == ConditionType.Constraint && parts.Any(i => i.Key == key);
        }

        [Pure]
        public bool IsWhile(ISymbol symbol, SemanticModel model)
        {
            return Kind == ConditionType.While && parts.Any(i => symbol.Equals(model.GetSymbolInfo(i.Expression).Symbol));
        }

        [Pure]
        public Condition Negate()
        {
            LogicalType? negative;
            switch (type)
            {
                case LogicalType.Or:
                    negative = LogicalType.And;
                    break;
                case LogicalType.And:
                    negative = LogicalType.Or;
                    break;
                case null:
                    negative = null;
                    break;
                default:
                    negative = LogicalType.Mixed;
                    break;
            }
            return new Condition(Kind, negative, parts.Select(i => i.Negate()).ToImmutableArray());
        }

        [Pure]
        public bool IsNotNull(ExpressionKey key)
        {
            if (type.GetValueOrDefault(LogicalType.And) == LogicalType.And)
            {
                return parts.Any(i => i.IsNotNull(key));
            }
            return false;
        }

        [Pure]
        public bool IsNotNullShortCircuit(ExpressionKey key)
        {
            if (type.GetValueOrDefault(LogicalType.And) == LogicalType.And)
            {
                return parts.Any(i => i.IsNotNull(key));
            }
            if (type.GetValueOrDefault(LogicalType.Or) == LogicalType.Or)
            {
                return parts.Any(i => i.IsNull(key));
            }
            return false;
        }

        [Pure]
        public bool TryGetConditionBefore(SyntaxNode expression, out Condition before)
        {
            var matches = parts.TakeWhile(i => i.Expression != expression && !i.Expression.Contains(expression)).ToList();
            if (matches.Count == parts.Length)
            {
                before = null;
                return false;
            }
            before = new Condition(Kind, type, matches.ToImmutableArray(), true);
            return true;
        }

        [Pure]
        public override string ToString()
        {
            if (parts.Length == 0)
            {
                return "if (true)";
            }
            string prefix;
            switch (Kind)
            {
                case ConditionType.While:
                    prefix = "while";
                    break;
                case ConditionType.Constraint:
                    prefix = "Constraint";
                    break;
                default:
                    prefix = "if";
                    break;
            }
            return $"{prefix} ({string.Join($" {type} ", parts)})";
        }

        private interface IPart
        {
            ExpressionKey Key { get; }
            ExpressionSyntax Expression { get; }
            bool IsNotNull(ExpressionKey key);
            bool IsNull(ExpressionKey key);
            IPart Negate();
        }

        private class IsPart : IPart
        {
            public ExpressionKey Key { get; }

            private readonly bool hasNotPrefix;

            public ExpressionSyntax Expression { get; }

            public IsPart(ExpressionKey key, ExpressionSyntax expression, bool hasNotPrefix)
            {
                this.Key = key;
                this.Expression = expression;
                this.hasNotPrefix = hasNotPrefix;

            }

            public bool IsNotNull(ExpressionKey key)
            {
                return !hasNotPrefix && this.Key.Contains(key);
            }

            public bool IsNull(ExpressionKey key)
            {
                return hasNotPrefix && this.Key.Contains(key);
            }

            public IPart Negate()
            {
                return new IsPart(Key, Expression, !hasNotPrefix);
            }

            public override string ToString()
            {
                if (hasNotPrefix)
                {
                    return $"!({Key} is {Key.Type})";
                }
                return $"{Key} is {Key.Type}";
            }
        }

        private class Part : IPart
        {
            public ExpressionKey Key { get; }

            private readonly ValueType value;

            public ExpressionSyntax Expression { get; }

            public Part(ExpressionKey key, ExpressionSyntax expression, ValueType value)
            {
                this.Key = key;
                this.Expression = expression;
                this.value = value;
            }

            public bool IsNotNull(ExpressionKey key)
            {
                return value == ValueType.NotNull && this.Key.Contains(key);
            }

            public bool IsNull(ExpressionKey key)
            {
                return value == ValueType.Null && this.Key.Contains(key);
            }

            [Pure]
            public IPart Negate()
            {
                ValueType negative;
                switch (value)
                {
                    case ValueType.NotNull:
                        negative = ValueType.Null;
                        break;
                    case ValueType.Null:
                        negative = ValueType.NotNull;
                        break;
                    case ValueType.OutOfRange:
                        negative = ValueType.OutOfRange;
                        break;
                    default:
                        negative = ValueType.MaybeNull;
                        break;
                }
                return new Part(Key, Expression, negative);
            }

            public override string ToString()
            {
                switch (value)
                {
                    case ValueType.NotNull:
                        return $"{Key} != null";
                    case ValueType.Null:
                        return $"{Key} == null";
                    case ValueType.MaybeNull:
                        return $"{Key} == MaybeNull";
                }
                return Key.ToString();
            }
        }
    }

    public enum ConditionType
    {
        None, If, While, ForEach, Constraint, Return
    }

    public enum LogicalType
    {
        Or, And, Mixed
    }
}
