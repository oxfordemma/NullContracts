using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using FUR10N.NullContracts.FlowAnalysis;
using System;

namespace FUR10N.NullContracts
{
    internal class ClassAnalyzer
    {
        private readonly SemanticModelAnalysisContext context;

        public ClassAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
        }

        public void Analyze(SemanticModel model)
        {
            var constructorFlowAnalyzer = new CtorFlowAnalyzer(model);
            var root = model.SyntaxTree.GetRoot();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var @class in classes)
            {
                var classSymbol = model.GetDeclaredSymbol(@class);
                if (classSymbol != null && classSymbol.HasNotNull())
                {
                    context.ReportDiagnostic(MainAnalyzer.CreateBadAttributeUsageError(@class.GetLocation(), false));
                }
                var members = GetTrackedMembers(model, @class);
                if (members.Item1.Count > 0)
                {
                    FlagUninitializedFields(@class, constructorFlowAnalyzer, members.Item1);
                }
                if (members.Item2.Count > 0)
                {
                    VerifyMethods(members.Item2);
                }
                if (members.Item3.Count > 0)
                {
                    VerifyExpressionBodies(members.Item3);
                }
            }
        }

        private void VerifyExpressionBodies(Dictionary<ISymbol, NotNullExpressionBodyInfo> expressions)
        {
            foreach (var expression in expressions.Values)
            {
                CheckExpressionForNull(expression.Symbol, expression.Expression);
            }
        }

        private void VerifyMethods(Dictionary<IMethodSymbol, NotNullMethodInfo> methods)
        {
            foreach (var method in methods.Values)
            {
                var analysis = Cache.Get(context.SemanticModel).GetMethodAnalysis(method.Symbol, method.Body, method.Body.ChildNodes(), AnalysisMode.Normal);
                foreach (var returnStatement in analysis.ReturnStatements)
                {
                    CheckExpressionForNull(method.Symbol, returnStatement.Expression, analysis);
                }
            }
        }

        // This is kinda duplicated in CodeBlockAnalyzer.GetAssignmentStatus
        private void CheckExpressionForNull(ISymbol symbol, ExpressionSyntax expression, MethodFlowAnalysis analysis = null)
        {
            var expressionValueType = expression.GetTypeOfValue(context.SemanticModel);
            if (expressionValueType == ValueType.NotNull)
            {
                // Argument cannot be null, so move to the next
                return;
            }
            if (expressionValueType == ValueType.Null)
            {
                context.ReportDiagnostic(MainAnalyzer.CreateReturnNull(expression.GetLocation(), symbol.ToString()));
                return;
            }
            if (analysis == null)
            {
                context.ReportDiagnostic(MainAnalyzer.CreateReturnNull(expression.GetLocation(), symbol.ToString()));
                return;
            }

            ExpressionStatus status = analysis.IsAlwaysAssigned(expression, expression);
            if (!status.IsAssigned())
            {
                context.ReportDiagnostic(MainAnalyzer.CreateReturnNull(expression.GetLocation(), symbol.ToString()));
            }
            if (status == ExpressionStatus.AssignedWithUnneededConstraint)
            {
                context.ReportDiagnostic(MainAnalyzer.CreateUnneededConstraint(expression.GetLocation(), symbol.ToString()));
            }
        }

        private void FlagUninitializedFields(
            ClassDeclarationSyntax @class,
            CtorFlowAnalyzer constructorFlowAnalyzer,
            Dictionary<ISymbol, NotNullFieldInfo> fields)
        {
            var flowAnalysis = constructorFlowAnalyzer.AnalyzeDataFlow(@class, fields);

            foreach (var member in fields.Values)
            {
                var isInitialized = member.Initializer != null &&
                    member.Initializer.Value.GetTypeOfValue(context.SemanticModel) == ValueType.NotNull;
                if (!isInitialized)
                {
                    if (flowAnalysis.Count == 0)
                    {
                        context.ReportDiagnostic(MainAnalyzer.CreateMemberNotInitialized(member.Location, member.Symbol));
                        continue;
                    }
                    foreach (var flow in flowAnalysis)
                    {
                        if (flow.UnassignedMembers.Contains(member))
                        {
                            context.ReportDiagnostic(MainAnalyzer.CreateMemberNotInitialized(flow.Constructor.GetLocation(), member.Symbol));
                        }
                    }
                }
            }
        }

#if PORTABLE
        private Tuple<Dictionary<ISymbol, NotNullFieldInfo>, Dictionary<IMethodSymbol, NotNullMethodInfo>,  Dictionary<ISymbol, NotNullExpressionBodyInfo>> 
#else
        private (Dictionary<ISymbol, NotNullFieldInfo> fields, Dictionary<IMethodSymbol, NotNullMethodInfo> methods, Dictionary<ISymbol, NotNullExpressionBodyInfo> expressions)
#endif
            GetTrackedMembers(SemanticModel model, ClassDeclarationSyntax type)
        {
            var trackedFields = new Dictionary<ISymbol, NotNullFieldInfo>();
            var trackedMethods = new Dictionary<IMethodSymbol, NotNullMethodInfo>();
            var trackedExpressionBodies = new Dictionary<ISymbol, NotNullExpressionBodyInfo>();
            foreach (var member in type.ChildNodes()
                .Where(i => i is PropertyDeclarationSyntax || i is FieldDeclarationSyntax || i is MethodDeclarationSyntax))
            {
                var property = member as PropertyDeclarationSyntax;
                if (property != null)
                {
                    var symbol = model.GetDeclaredSymbol(property);
                    if (symbol == null)
                    {
                        throw new ParseFailedException(member.GetLocation(), "Parse failed on: " + property);
                    }
                    if (!property.IsAutoProperty())
                    {
                        if (property.ExpressionBody?.Expression != null)
                        {
                            var info = VisitExpressionBody(model, symbol, property.ExpressionBody.Expression, property.GetLocation());
                            if (info != null)
                            {
                                trackedExpressionBodies.Add(info.Symbol, info);
                            }
                        }
                        else
                        {
                            var getter = property.AccessorList?.Accessors.FirstOrDefault(i => i.Kind() == SyntaxKind.GetAccessorDeclaration);
                            if (getter != null)
                            {
                                var info = VisitGetterMethod(model, symbol, getter);
                                if (info != null)
                                {
                                    trackedMethods.Add(info.Symbol, info);
                                }
                            }
                        }
                        // Is computed property, so we don't need to worry about the ctor setting it.
                        continue;
                    }
                    if (symbol.HasNotNull())
                    {
                        if (symbol.SetMethod != null || IsValueType(symbol.Type))
                        {
                            context.ReportDiagnostic(MainAnalyzer.CreateBadAttributeUsageError(member.GetLocation(), IsValueType(symbol.Type)));
                            continue;
                        }
                        trackedFields.Add(symbol, new NotNullFieldInfo(member.GetLocation(), symbol, property.Initializer));
                    }
                    else if (FindInheritedReference(model, symbol).HasNotNull())
                    {
                        context.ReportDiagnostic(MainAnalyzer.CreateMissingAttribute(member.GetLocation(), symbol.ToString()));
                    }
                }

                var field = member as FieldDeclarationSyntax;
                if (field != null)
                {
                    var declaration = field.Declaration.Variables.First();
                    var symbol = model.GetDeclaredSymbol(declaration) as IFieldSymbol;
                    if (symbol == null)
                    {
                        throw new ParseFailedException(member.GetLocation(), "Parse failed on: " + declaration);
                    }
                    if (symbol.HasNotNull())
                    {
                        if (!symbol.IsReadOnlyOrConst() || IsValueType(symbol.Type))
                        {
                            context.ReportDiagnostic(MainAnalyzer.CreateBadAttributeUsageError(member.GetLocation(), IsValueType(symbol.Type)));
                            continue;
                        }
                        trackedFields.Add(symbol, new NotNullFieldInfo(member.GetLocation(), symbol, declaration.Initializer));
                    }
                }

                if (member is MethodDeclarationSyntax method)
                {
                    var info = VisitMethod(model, method);
                    if (info != null)
                    {
                        trackedMethods.Add(info.Symbol, info);
                    }
                }
            }
#if PORTABLE
            return new Tuple<Dictionary<ISymbol, NotNullFieldInfo>, Dictionary<IMethodSymbol, NotNullMethodInfo>, Dictionary<ISymbol, NotNullExpressionBodyInfo>>(trackedFields, trackedMethods, trackedExpressionBodies);
#else
            return (trackedFields, trackedMethods, trackedExpressionBodies);
#endif
        }

        public NotNullMethodInfo VisitMethod(SemanticModel model, MethodDeclarationSyntax method)
        {
            var symbol = model.GetDeclaredSymbol(method);
            if (symbol != null)
            {
                if (symbol.HasNotNull())
                {
                    if (symbol.ReturnsVoid || IsValueType(symbol.ReturnType))
                    {
                        context.ReportDiagnostic(MainAnalyzer.CreateBadAttributeUsageError(method.GetLocation(), true));
                        return null;
                    }
                    if (method.Body != null)
                    {
                        return new NotNullMethodInfo(method.GetLocation(), symbol, method.Body);
                    }
                }
                else if (FindInheritedReference(model, symbol).HasNotNull())
                {
                    context.ReportDiagnostic(MainAnalyzer.CreateMissingAttribute(method.GetLocation(), symbol.ToString()));
                }
            }
            return null;
        }

        private bool IsValueType(ITypeSymbol type)
        {
            return !type.IsReferenceType && type.TypeKind != TypeKind.TypeParameter;
        }

        public NotNullMethodInfo VisitGetterMethod(SemanticModel model, IPropertySymbol propertySymbol, AccessorDeclarationSyntax getter)
        {
            var symbol = model.GetDeclaredSymbol(getter);
            if (symbol != null)
            {
                if (propertySymbol.HasNotNull() || symbol.HasNotNull())
                {
                    if (symbol.ReturnsVoid || IsValueType(symbol.ReturnType))
                    {
                        context.ReportDiagnostic(MainAnalyzer.CreateBadAttributeUsageError(getter.GetLocation(), true));
                        return null;
                    }
                    if (getter.Body != null)
                    {
                        return new NotNullMethodInfo(getter.GetLocation(), symbol, getter.Body);
                    }
                }
                else if (FindInheritedReference(model, symbol).HasNotNull())
                {
                    context.ReportDiagnostic(MainAnalyzer.CreateMissingAttribute(getter.GetLocation(), symbol.ToString()));
                }
            }
            return null;
        }

        public NotNullExpressionBodyInfo VisitExpressionBody(SemanticModel model, IPropertySymbol propertySymbol, ExpressionSyntax expression, Location location)
        {
            if (propertySymbol.HasNotNull())
            {
                if (!propertySymbol.Type.IsReferenceType)
                {
                    context.ReportDiagnostic(MainAnalyzer.CreateBadAttributeUsageError(location, true));
                    return null;
                }
                return new NotNullExpressionBodyInfo(location, propertySymbol, expression);
            }
            else if (FindInheritedReference(model, propertySymbol).HasNotNull())
            {
                context.ReportDiagnostic(MainAnalyzer.CreateMissingAttribute(location, propertySymbol.ToString()));
            }
            return null;
        }

        /// <summary>
        /// Checks any base classes to see if this symbol is inherited from something else.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        private ISymbol FindInheritedReference(SemanticModel model, ISymbol symbol)
        {
            foreach (var i in symbol.ContainingType.AllInterfaces)
            {
                foreach (var member in i.GetMembers(symbol.Name))
                {
                    var impl = symbol.ContainingType.FindImplementationForInterfaceMember(member);
                    if (impl != null && impl.Equals(symbol))
                    {
                        return member;
                    }
                }
            }

            if (symbol.IsOverride)
            {
                if (symbol is IMethodSymbol method)
                {
                    return method.OverriddenMethod;
                }
                else if (symbol is IPropertySymbol property)
                {
                    return property.OverriddenProperty;
                }
            }
            return null;
        }
    }
}
