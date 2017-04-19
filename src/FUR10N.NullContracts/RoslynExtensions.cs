using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace FUR10N.NullContracts
{
    public static class RoslynExtensions
    {
        public static bool HasNotNull(this ISymbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }
            return GetAttributes(symbol).Any(i => i.AttributeClass.Name == "NotNullAttribute");
        }

        public static bool HasCheckNull(this ISymbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }
            return GetAttributes(symbol).Any(i => i.AttributeClass.Name == "CheckNullAttribute");
        }

        public static bool HasNotNullOrCheckNull(this ISymbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }
            return GetAttributes(symbol)
                .Any(i => i.AttributeClass.Name == "NotNullAttribute" || i.AttributeClass.Name == "CheckNullAttribute");
        }

        private static IEnumerable<AttributeData> GetAttributes(ISymbol symbol)
        {
            foreach (var directAttr in symbol.GetAttributes())
            {
                yield return directAttr;
            }
            var accessor = symbol as IMethodSymbol;
            if (accessor != null)
            {
                var propertySymbol = accessor.GetPropertySymbol();
                if (propertySymbol != null)
                {
                    foreach (var accessorAttr in propertySymbol.GetAttributes())
                    {
                        yield return accessorAttr;
                    }
                }
            }
        }

        public static bool IsAutoProperty(this PropertyDeclarationSyntax property)
        {
            // TODO: better way to calculate this?
            if (property.ExpressionBody != null)
            {
                return false;
            }
            return property.AccessorList.Accessors
                .Where(i => i.Body != null)
                .Select(i => i.Body.GetText().ToString())
                .All(i => i.Contains("get;") || i.Contains("set;"));
        }

        public static ExpressionSyntax FindUnderlyingMember(this ExpressionSyntax expression)
        {
            switch (expression)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    var name = memberAccess.Name;
                    return FindUnderlyingMember(name);
                case ConditionalAccessExpressionSyntax conditional:
                    return FindUnderlyingMember(conditional.WhenNotNull);
                case MemberBindingExpressionSyntax memberBinding:
                    return FindUnderlyingMember(memberBinding.Name);
                case ParenthesizedExpressionSyntax parenthesizedExpression:
                    return FindUnderlyingMember(parenthesizedExpression.Expression);
                case AssignmentExpressionSyntax assignmentExpression:
                    return FindUnderlyingMember(assignmentExpression.Right);
                case AwaitExpressionSyntax awaitExpression:
                    return FindUnderlyingMember(awaitExpression.Expression);
                case CastExpressionSyntax castExpression:
                    return FindUnderlyingMember(castExpression.Expression);
                case ConditionalExpressionSyntax conditional:
                    return conditional;
                case PrefixUnaryExpressionSyntax prefixUnary:
                    return FindUnderlyingMember(prefixUnary.Operand);
                case IdentifierNameSyntax identifier:
                case LiteralExpressionSyntax literal:
                case InvocationExpressionSyntax invocation:
                case ThisExpressionSyntax thisExpression:
                case ElementAccessExpressionSyntax elementAccess:
                case ObjectCreationExpressionSyntax objectCreation:
                case ArrayCreationExpressionSyntax arrayCreation:
                case ImplicitElementAccessSyntax indexer:
#if !PORTABLE
                case ThrowExpressionSyntax throwExpression:
                case TupleExpressionSyntax tupleExpression:
#endif
                    return expression;
            }

#if PORTABLE
            // Check for ThrowExpressionSyntax and TupleExpressionSyntax
            if (expression.Kind() == (SyntaxKind)9052 || expression.Kind() == (SyntaxKind)8926)
            {
                return expression;
            }
#endif
            
            var binaryExpression = expression as BinaryExpressionSyntax;
            if (binaryExpression != null)
            {
                if (binaryExpression.OperatorToken.Kind() == SyntaxKind.AsKeyword)
                {
                    return binaryExpression.Left.FindUnderlyingMember();
                }
                if (binaryExpression.Kind() == SyntaxKind.CoalesceExpression)
                {
                    return binaryExpression.Right.FindUnderlyingMember();
                }
                return binaryExpression;
            }
            throw new ParseFailedException(expression.GetLocation(), $"Parse failed on: {expression.ToString()} - Token: {expression.Kind()}");
        }

        public static ValueType GetTypeOfValue(this ExpressionSyntax expression, SemanticModel model)
        {
            var kind = expression.Kind();
            switch (kind)
            {
                case SyntaxKind.NullLiteralExpression:
                    return ValueType.Null;
                case SyntaxKind.MemberBindingExpression:
                case SyntaxKind.IdentifierName:
                    var identifier = model.GetSymbolInfo(expression).Symbol;
                    if (identifier is ILocalSymbol)
                    {
                        return ((ILocalSymbol)identifier).IsForEach() ? ValueType.NotNull : ValueType.MaybeNull;
                    }
                    if (identifier is IParameterSymbol parameterSymbol)
                    {
                        if (identifier.IsImplicitlyDeclared)
                        {
                            // We are a setter parameter.
                            // First check attributes on the 'set' method. Then check attributes on the actual property.
                            var setterDefinition = parameterSymbol.ContainingSymbol;
                            if (parameterSymbol.ContainingSymbol.HasNotNull())
                            {
                                return ValueType.NotNull;
                            }
                            var propertyDefinition = parameterSymbol.ContainingSymbol.GetPropertySymbol();
                            if (propertyDefinition != null && propertyDefinition.HasNotNull())
                            {
                                return ValueType.NotNull;
                            }
                        }
                        else if (identifier.ContainingSymbol.IsLambdaMethod())
                        {
                            if (expression.IsInLinqExpression(model))
                            {
                                return ValueType.NotNull;
                            }
                            else
                            {
                                var lambda = expression.FindParent<LambdaExpressionSyntax>();
                                var delegateInvoke = TryGetLambdaDelegateConversion(lambda, model);
                                if (delegateInvoke != null)
                                {
                                    var paramIndex = ((IMethodSymbol)identifier.ContainingSymbol).Parameters.IndexOf(parameterSymbol);
                                    return delegateInvoke.Parameters[paramIndex].HasNotNullOrCheckNull() ? ValueType.NotNull : ValueType.Null;
                                }
                            }
                        }
                    }
                    if (identifier != null && Cache.Get(model).Symbols.IsPropertyThatIsNotNull(identifier))
                    {
                        return ValueType.NotNull;
                    }
                    return identifier.HasNotNullOrCheckNull() ? ValueType.NotNull : ValueType.MaybeNull;
                case SyntaxKind.InvocationExpression:
                    var method = model.GetSymbolInfo(expression).Symbol as IMethodSymbol;
                    if (method?.ReturnType != null)
                    {
                        if (method.HasNotNull())
                        {
                            return ValueType.NotNull;
                        }
                        if (Cache.Get(model).Symbols.IsMethodCallThatIsNotNull(method))
                        {
                            return ValueType.NotNull;
                        }
                        return method.ReturnType.IsValueType ? ValueType.NotNull : ValueType.Null;
                    }
                    return ValueType.MaybeNull;
                case SyntaxKind.SimpleAssignmentExpression:
                    return GetTypeOfValue(((AssignmentExpressionSyntax)expression).Right, model);
                case SyntaxKind.SimpleMemberAccessExpression:
                    return GetTypeOfValue(((MemberAccessExpressionSyntax)expression).Name, model);
                case SyntaxKind.CoalesceExpression:
                    var coalesceExp = ((BinaryExpressionSyntax)expression);
                    var member = coalesceExp.Right.FindUnderlyingMember();
                    if (member != null)
                    {
                        return GetTypeOfValue(member, model);
                    }
                    return GetTypeOfValue(coalesceExp.Right, model);
                case SyntaxKind.AddExpression:
                    var symbolInfo = model.GetSymbolInfo(expression).Symbol;
                    var typeInfo = model.GetTypeInfo(expression);
                    if (typeInfo.ConvertedType != null)
                    {
                        if (typeInfo.ConvertedType.IsValueType
                            || typeInfo.ConvertedType.Equals(Cache.Get(model).Symbols.StringType))
                        {
                            return ValueType.NotNull;
                        }
                    }
                    if (symbolInfo != null && symbolInfo.Name == "ToList" && symbolInfo.ContainingType.Name == "Enumerable")
                    {
                        return ValueType.NotNull;
                    }
                    break;
                case SyntaxKind.ConditionalExpression:
                    var conditional = (ConditionalExpressionSyntax)expression;
                    var whenTrue = GetTypeOfValue(conditional.WhenTrue, model);
                    if (whenTrue == ValueType.MaybeNull)
                    {
                        return ValueType.MaybeNull;
                    }
                    var whenFalse = GetTypeOfValue(conditional.WhenFalse, model);
                    if (whenFalse == ValueType.MaybeNull)
                    {
                        return ValueType.MaybeNull;
                    }
                    if (whenTrue != whenFalse)
                    {
                        return ValueType.MaybeNull;
                    }
                    return whenTrue;
                case SyntaxKind.CastExpression:
                    return GetTypeOfValue(((CastExpressionSyntax)expression).Expression, model);
                case SyntaxKind.ParenthesizedExpression:
                    return GetTypeOfValue(((ParenthesizedExpressionSyntax)expression).Expression, model);
                case SyntaxKind.AwaitExpression:
                    return GetTypeFromAwaitExpression((AwaitExpressionSyntax)expression, model);
                case SyntaxKind.ConditionalAccessExpression:
                    return GetTypeOfValue(((ConditionalAccessExpressionSyntax)expression).WhenNotNull, model);
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.ObjectCreationExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ElementAccessExpression:
                case SyntaxKind.ImplicitArrayCreationExpression:
                case SyntaxKind.ArrayCreationExpression:
                case SyntaxKind.ThisExpression:
#if PORTABLE
                case (SyntaxKind)9052:
#else
                case SyntaxKind.ThrowExpression:
#endif
                    return ValueType.NotNull;
            }
            return ValueType.MaybeNull;
        }

        private static IMethodSymbol TryGetLambdaDelegateConversion(LambdaExpressionSyntax lambda, SemanticModel model)
        {
            // Check if we are a lambda that is a method argument
            if (lambda == null || !(lambda.Parent is ArgumentSyntax))
            {
                return null;
            }
            var index = ((ArgumentListSyntax)lambda.Parent.Parent).Arguments.IndexOf((ArgumentSyntax)lambda.Parent);
            if (index == -1)
            {
                // Shouldn't happen
                return null;
            }
            var targetMethod = lambda.FindParent<InvocationExpressionSyntax>();
            if (targetMethod == null)
            {
                return null;
            }
            var methodSymbol = model.GetSymbolInfo(targetMethod).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return null;
            }
            var parameter = methodSymbol.Parameters[index];
            if (parameter.Type?.TypeKind != TypeKind.Delegate)
            {
                return null;
            }
            return (parameter.Type as INamedTypeSymbol)?.DelegateInvokeMethod;
        }

        private static ValueType GetTypeFromAwaitExpression(AwaitExpressionSyntax expression, SemanticModel model)
        {
            var invocation = expression.Expression as InvocationExpressionSyntax;
            if (invocation == null)
            {
                return GetTypeOfValue(expression.Expression, model);
            }
            var methodSymbol = model.GetSymbolInfo(invocation).Symbol;
            if (methodSymbol != null && methodSymbol.OriginalDefinition.Equals(Cache.Get(model).Symbols.ConfigureAwait))
            {
                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    return GetTypeOfValue(memberAccess.Expression, model);
                }
            }
            return GetTypeOfValue(invocation, model);
        }

        public static bool IsLambdaMethod(this ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
                return method.MethodKind == MethodKind.AnonymousFunction;
            }
            return false;
        }

        public static bool IsInLinqExpression(this ExpressionSyntax expression, SemanticModel model)
        {
            var lambda = expression.Parent;
            while (lambda != null && !(lambda is LambdaExpressionSyntax))
            {
                lambda = lambda.Parent;
            }
            if (!(lambda is LambdaExpressionSyntax))
            {
                return false;
            }

            var linqMethod = lambda.Parent.Parent.Parent as InvocationExpressionSyntax;
            if (linqMethod == null)
            {
                return false;
            }

            var methodSymbol = model.GetSymbolInfo(linqMethod).Symbol;

            if (methodSymbol?.ContainingType.OriginalDefinition != null)
            {
                return methodSymbol.ContainingType.OriginalDefinition.Equals(Cache.Get(model).Symbols.Enumerable);
            }
            return false;
        }

        public static IPropertySymbol GetPropertySymbol(this ISymbol accessor)
        {
            var temp = accessor as IMethodSymbol;
            return temp == null ? null : temp.AssociatedSymbol as IPropertySymbol;
        }

        public static string GetFullName(this ISymbol symbol)
        {
            if (symbol == null)
            {
                return null;
            }
            if (symbol.ContainingSymbol.Kind == SymbolKind.NamedType)
            {
                var className = symbol.ContainingSymbol.Name;
                var name = symbol.Name;
                return $"{className}.{name}";
            }
            return symbol.Name;
        }

        public static T FindParent<T>(this SyntaxNode node) where T : SyntaxNode
        {
            while (node.Parent != null)
            {
                if (node.Parent is T)
                {
                    return (T)node.Parent;
                }
                node = node.Parent;
            }
            return null;
        }

        public static bool IsCheckAgainstNull(this BinaryExpressionSyntax expression, out ExpressionSyntax target, out ValueType valueType)
        {
            var kind = expression.Kind();
            if (kind == SyntaxKind.NotEqualsExpression || kind == SyntaxKind.EqualsExpression)
            {
                var leftKind = expression.Left.Kind();
                var rightKind = expression.Right.Kind();
                if (leftKind == SyntaxKind.NullLiteralExpression && rightKind != SyntaxKind.NullLiteralExpression)
                {
                    target = expression.Right;
                    valueType = kind == SyntaxKind.NotEqualsExpression ? ValueType.NotNull : ValueType.Null;
                    return true;
                }
                if (rightKind == SyntaxKind.NullLiteralExpression)
                {
                    target = expression.Left;
                    valueType = kind == SyntaxKind.NotEqualsExpression ? ValueType.NotNull : ValueType.Null;
                    return true;
                }
            }
            target = null;
            valueType = ValueType.MaybeNull;
            return false;
        }

        public static bool IsCheckForNotNull(this BinaryExpressionSyntax expression, out ExpressionSyntax target)
        {
            if (expression.Kind() == SyntaxKind.NotEqualsExpression)
            {
                var leftKind = expression.Left.Kind();
                var rightKind = expression.Right.Kind();
                if (leftKind == SyntaxKind.NullLiteralExpression && rightKind != SyntaxKind.NullLiteralExpression)
                {
                    target = expression.Right;
                    return true;
                }
                if (rightKind == SyntaxKind.NullLiteralExpression)
                {
                    target = expression.Left;
                    return true;
                }
            }
            target = null;
            return false;
        }

        public static bool IsCheckForNull(this BinaryExpressionSyntax expression, out ExpressionSyntax target)
        {
            if (expression.Kind() == SyntaxKind.EqualsExpression)
            {
                var leftKind = expression.Left.Kind();
                var rightKind = expression.Right.Kind();
                if (leftKind == SyntaxKind.NullLiteralExpression && rightKind != SyntaxKind.NullLiteralExpression)
                {
                    target = expression.Right;
                    return true;
                }
                if (rightKind == SyntaxKind.NullLiteralExpression)
                {
                    target = expression.Left;
                    return true;
                }
            }
            target = null;
            return false;
        }

        public static bool IsDirectChildOfMethod(this SyntaxNode node)
        {
            if (node.Parent is BlockSyntax)
            {
                if (node.Parent.Parent is ConstructorDeclarationSyntax || node.Parent.Parent is MethodDeclarationSyntax)
                {
                    return true;
                }
            }
            return false;
        }

        private static PropertyInfo isForEachProperty;

        public static bool IsForEach(this ILocalSymbol symbol)
        {
            lock (typeof(RoslynExtensions))
            {
                if (isForEachProperty == null)
                {
#if PORTABLE
                    isForEachProperty = symbol.GetType().GetRuntimeProperty("IsForEach");
#else
                    isForEachProperty = symbol.GetType().GetProperty("IsForEach", BindingFlags.Instance | BindingFlags.Public);
#endif
                }
            }
            return (bool)isForEachProperty.GetValue(symbol);
        }

        public static bool IsLogicalOperation(this SyntaxNode node)
        {
            var kind = node.Kind();
            switch (kind)
            {
                case SyntaxKind.LogicalAndExpression:
                case SyntaxKind.LogicalNotExpression:
                case SyntaxKind.LogicalOrExpression:
                    return true;
            }
            return false;
        }
        
        public static bool IsConstraint(this SyntaxNode node, SemanticModel model, out ExpressionSyntax key)
        {
            if (node is ExpressionStatementSyntax expression)
            {
                node = expression.Expression;
            }

            if (node is InvocationExpressionSyntax invocation)
            {
                var symbol = model.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
                if (symbol != null)
                {
                    if (symbol.IsConstraintMethod())
                    {
                        var lambda = invocation.ArgumentList.Arguments.FirstOrDefault().Expression as LambdaExpressionSyntax;
                        if (lambda != null)
                        {
                            if (lambda.Body is MemberAccessExpressionSyntax || lambda.Body is IdentifierNameSyntax)
                            {
                                key = (ExpressionSyntax)lambda.Body;
                                return true;
                            }
                        }
                    }
                }
            }
            key = null;
            return false;
        }

        public static bool IsConstraintMethod(this IMethodSymbol method)
        {
            return method.ContainingType.Name == "Constraint" && method.Name == "NotNull";
        }

        public static bool IsReadOnlyOrConst(this IFieldSymbol symbol)
        {
            return symbol.IsReadOnly || symbol.IsConst;
        }

        // TODO: make this a help class instead
#if PORTABLE
        public static Tuple<ISymbol, BlockSyntax, IEnumerable<SyntaxNode>> GetParentMethod(this SyntaxNode node, SemanticModel model)
#else
        public static (ISymbol symbol, BlockSyntax body, IEnumerable<SyntaxNode> expressions) GetParentMethod(this SyntaxNode node, SemanticModel model)
#endif
        {
            while (!(node is MethodDeclarationSyntax) 
                && !(node is ConstructorDeclarationSyntax) 
                && !(node is AccessorDeclarationSyntax)
                && !(node is PropertyDeclarationSyntax))
            {
                if (node == null)
                {
#if PORTABLE
                    return new Tuple<ISymbol, BlockSyntax, IEnumerable<SyntaxNode>>(null, null, null);
#else
                    return (null, null, null);
#endif
                }
                node = node.Parent;
            }

            ISymbol methodSymbol = null;
            BlockSyntax body = null;
            IEnumerable<SyntaxNode> expressions = null;
            switch (node)
            {
                case ConstructorDeclarationSyntax ctor:
                    if (ctor.Body != null)
                    {
                        methodSymbol = model.GetDeclaredSymbol(ctor);
                        body = ctor.Body;
                    }
                    break;
                case MethodDeclarationSyntax method:
                    if (method.Body != null)
                    {
                        methodSymbol = model.GetDeclaredSymbol(method);
                        body = method.Body;
                    }
                    break;
                case AccessorDeclarationSyntax accessor:
                    if (accessor.Body != null)
                    {
                        methodSymbol = model.GetDeclaredSymbol(accessor);
                        body = accessor.Body;
                    }
                    break;
                case PropertyDeclarationSyntax property:
                    if (property.ExpressionBody is ArrowExpressionClauseSyntax arrowExpression)
                    {
                        methodSymbol = model.GetDeclaredSymbol(property);
                        expressions = new[] { arrowExpression.Expression };
                    }
                    break;
            }
            if (body != null)
            {
                expressions = body.ChildNodes();
            }

            if (methodSymbol == null || expressions == null)
            {
                // Probably a compilation error
#if PORTABLE
                return new Tuple<ISymbol, BlockSyntax, IEnumerable<SyntaxNode>>(null, null, null);
#else
                return (null, null, null);
#endif
            }
#if PORTABLE
            return new Tuple<ISymbol, BlockSyntax, IEnumerable<SyntaxNode>>(methodSymbol, body, expressions);
#else
            return (methodSymbol, body, expressions);
#endif
        }
    }
}
