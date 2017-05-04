using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FUR10N.NullContracts.FlowAnalysis
{
    /// <summary>
    /// Analyzes flow information about methods (and constructors)
    /// </summary>
    internal class MethodFlowAnalyzer
    {
        private readonly List<Assignment> assignments = new List<Assignment>();

        private readonly Queue<Tuple<Branch, LambdaExpressionSyntax>> lambdas = new Queue<Tuple<Branch, LambdaExpressionSyntax>>();

        private readonly List<ReturnStatementSyntax> returnStatements = new List<ReturnStatementSyntax>();

        private readonly SemanticModel model;

        private readonly ExpressionToCondition expressionParser;

        private bool hasConstraints;

        public MethodFlowAnalyzer(SemanticModel model)
        {
            this.model = model;
            expressionParser = new ExpressionToCondition(model);
        }

        public MethodFlowAnalysis Analyze(BlockSyntax body, IEnumerable<SyntaxNode> statements)
        {
            hasConstraints = false;
            assignments.Clear();
            lambdas.Clear();
            using (new OperationTimer(i => Timings.Update(TimingOperation.MethodAnlyzer, i)))
            {
                var tree = BuildTree(null, null, statements, false);
                var lambdaTrees = new List<Branch>();
                while (lambdas.Count > 0)
                {
                    var lambda = lambdas.Dequeue();
                    if (lambda.Item2.Body is BlockSyntax block)
                    {
                        lambdaTrees.Add(BuildTree(lambda.Item1, null, lambda.Item2.Body.ChildNodes(), true));
                    }
                    else
                    {
                        lambdaTrees.Add(BuildTree(lambda.Item1, null, new[] { lambda.Item2.Body }, true));
                    }
                }
                // This is kinda a hack. The idea is that we probably wanna search the inner most lambda first.
                // But since each lambda contains all the statements of the inner lambdas, we'd match on the outer one first.
                // So reverse the list and start at the bottom.
                lambdaTrees.Reverse();

                var nullAssignments = assignments
                    .Where(i => i.Value != ValueType.NotNull)
                    .Select(i => i.Symbol)
                    .ToList();

                if (body != null)
                {
                    var data = model.AnalyzeDataFlow(body);

                    var variablesThatAreAlwaysNotNull = data.AlwaysAssigned.RemoveAll(i => nullAssignments.Any(j => j.Equals(i)));

                    var safeParameters = data.WrittenOutside.Where(i => i.HasNotNullOrCheckNull());

                    return new MethodFlowAnalysis(
                        model,
                        assignments,
                        variablesThatAreAlwaysNotNull.AddRange(safeParameters),
                        tree,
                        lambdaTrees,
                        hasConstraints,
                        returnStatements);
                }
                return new MethodFlowAnalysis(model,
                    assignments,
                    ImmutableArray.Create<ISymbol>(),
                    tree,
                    lambdaTrees,
                    hasConstraints,
                    returnStatements);
            }
        }

        private Branch BuildTree(Branch parent, Condition condition, IEnumerable<SyntaxNode> nodes, bool inLambda)
        {
            var branch = new Branch(parent, condition);
            foreach (var node in nodes)
            {
                if (node is IfStatementSyntax ifStatement)
                {
                    var temp = expressionParser.Parse(ConditionType.If, ifStatement.Condition);

                    VisitNode(branch, ifStatement.Condition);
                    var ifCondition = temp;
                    branch.Children.Add(BuildTree(branch, ifCondition, ifStatement.Statement.ChildNodes(), inLambda));
                    if (BlockExitsInAllPaths(ifStatement.Statement))
                    {
                        var restOfIfStatement = ifStatement.Else?.Statement == null ? 
                            Enumerable.Empty<SyntaxNode>() : new[] { ifStatement.Else.Statement };
                        branch.Children.Add(BuildTree(branch, ifCondition.Negate(), restOfIfStatement.Concat(GetNodesAfter(nodes, node)), inLambda));
                        break;
                    }
                    if (ifStatement.Else != null)
                    {
                        branch.Children.Add(BuildTree(branch, ifCondition.Negate(), new[] { ifStatement.Else.Statement }, inLambda));
                        // TODO: handle case where and else clause always returns.
                    }
                    continue;
                }
                else if (node is ReturnStatementSyntax returnStatement)
                {
                    if (!inLambda)
                    {
                        // We just want to track return statements in the method we are analyzing
                        returnStatements.Add(returnStatement);
                    }
                    VisitNode(branch, returnStatement);
                    if (returnStatement.Expression != null)
                    {
                        if (returnStatement.Expression is ConditionalExpressionSyntax conditional)
                        {
                            var conditionalCondition = expressionParser.Parse(ConditionType.Return, conditional.Condition);
                            branch.Children.Add(BuildTree(branch, conditionalCondition, new[] { conditional.WhenTrue }, inLambda));
                            branch.Children.Add(BuildTree(branch, conditionalCondition.Negate(), new[] { conditional.WhenFalse }, inLambda));
                        }
                        else
                        {
                            var returnCondition = expressionParser.Parse(ConditionType.Return, returnStatement.Expression);
                            branch.Children.Add(new Branch(branch, returnCondition));
                        }
                    }
                    continue;
                }
                else if (node is BinaryExpressionSyntax binaryExpression)
                {
                    VisitNode(branch, binaryExpression);
                    var returnCondition = expressionParser.Parse(ConditionType.Return, binaryExpression);
                    branch.Children.Add(new Branch(branch, returnCondition));
                    continue;
                }
                else if (node is WhileStatementSyntax whileStatement)
                {
                    VisitNode(branch, whileStatement.Condition);
                    var whileCondtion = expressionParser.Parse(ConditionType.While, whileStatement.Condition);
                    branch.Children.Add(BuildTree(branch, whileCondtion, whileStatement.Statement.ChildNodes(), inLambda));
                    continue;
                }
                else if (node is UsingStatementSyntax usingStatement)
                {
                    branch.Children.Add(BuildTree(branch, new Condition(ConditionType.None), usingStatement.Statement.ChildNodes(), inLambda));
                    continue;
                }
                else if (node is DoStatementSyntax doStatement)
                {
                    branch.Children.Add(BuildTree(branch, new Condition(ConditionType.While), doStatement.Statement.ChildNodes(), inLambda));
                    continue;
                }
                else if (node is ForEachStatementSyntax forEach)
                {
                    branch.Children.Add(BuildTree(branch, new Condition(ConditionType.ForEach), forEach.Statement.ChildNodes(), inLambda));
                    continue;
                }
                else if (node is ForStatementSyntax forStatement)
                {
                    branch.Children.Add(BuildTree(branch, new Condition(ConditionType.ForEach), forStatement.Statement.ChildNodes(), inLambda));
                    continue;
                }
                else if (node is BlockSyntax block)
                {
                    branch.Children.Add(BuildTree(branch, new Condition(ConditionType.None), block.ChildNodes(), inLambda));
                    continue;
                }
                else if (node is LockStatementSyntax lockStatement)
                {
                    branch.Children.Add(BuildTree(branch, new Condition(ConditionType.None), lockStatement.Statement.ChildNodes(), inLambda));
                    continue;
                }
                else if (node is TryStatementSyntax tryCatch)
                {
                    branch.Children.Add(BuildTree(branch, new Condition(ConditionType.None), tryCatch.Block.ChildNodes(), inLambda));
                    foreach (var catchBlock in tryCatch.Catches)
                    {
                        branch.Children.Add(BuildTree(branch, new Condition(ConditionType.None), catchBlock.ChildNodes(), inLambda));
                    }
                    if (tryCatch.Finally != null)
                    {
                        branch.Children.Add(BuildTree(branch, new Condition(ConditionType.None), tryCatch.Finally.ChildNodes(), inLambda));
                    }
                    continue;
                }
                else if (node is SwitchStatementSyntax switchStatment)
                {
                    foreach (var section in switchStatment.Sections)
                    {
                        foreach (var label in section.Labels)
                        {
                            VisitNode(branch, label);
                        }
                        branch.Children.Add(BuildTree(branch, new Condition(ConditionType.None), section.Statements, inLambda));
                    }
                    continue;
                }
                else if (node is ExpressionStatementSyntax exp && exp.Expression is AssignmentExpressionSyntax assignment)
                {
                    if (assignment.Right is ConditionalExpressionSyntax conditional)
                    {
                        var conditionalCondition = expressionParser.Parse(ConditionType.Return, conditional.Condition);
                        branch.Children.Add(BuildTree(branch, conditionalCondition, new[] { conditional.WhenTrue }, inLambda));
                        branch.Children.Add(BuildTree(branch, conditionalCondition.Negate(), new[] { conditional.WhenFalse }, inLambda));
                        continue;
                    }
                }
                else if (node.IsConstraint(model, out var constraint))
                {
                    hasConstraints = true;
                    var constraintCondition = new Condition(ConditionType.Constraint, new ExpressionKey(constraint, model), constraint, ValueType.NotNull);
                    branch.Children.Add(BuildTree(branch, constraintCondition, GetNodesAfter(nodes, node), inLambda));
                    break;
                }

                VisitNode(branch, node);
                branch.Body.Add(node);
            }
            return branch;
        }

        private static IEnumerable<SyntaxNode> GetNodesAfter(IEnumerable<SyntaxNode> list, SyntaxNode node)
        {
            bool after = false;
            foreach (var item in list)
            {
                if (after)
                {
                    yield return item;
                }
                else if (item == node)
                {
                    after = true;
                }
            }
        }

        private void VisitNode(Branch currentBranch, SyntaxNode node)
        {
            var walker = new MethodBodyWalker(model);
            walker.Visit(node);

            assignments.AddRange(walker.Assignments);
            if (walker.Lambdas.Count > 0)
            {
                foreach (var lambda in walker.Lambdas)
                {
                    lambdas.Enqueue(new Tuple<Branch, LambdaExpressionSyntax>(currentBranch, lambda));
                }
            }
        }

        private bool BlockExitsInAllPaths(StatementSyntax block)
        {
            foreach (var directChild in block.ChildNodes())
            {
                var kind = directChild.Kind();
                if (kind == SyntaxKind.ContinueStatement || kind == SyntaxKind.ReturnStatement || kind == SyntaxKind.ThrowStatement)
                {
                    return true;
                }
            }

            // TODO: this is too expensive and isn't that common
            // Add an extra return statement to the end of the body.
            // If there's unreachable code, then this block always exists.
            //var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "TestMethod")
            //    .AddBodyStatements(block)
            //    .AddBodyStatements(SyntaxFactory.ReturnStatement());
            //var @class = SyntaxFactory.ClassDeclaration("TestClass").AddMembers(method);
            //using (new OperationTimer(i => Timings.Update(TimingOperation.MethodAnalyzerVisit, i)))
            //{
            //    var tempModel = CtorFlowAnalyzer.CreateNewModelForClass(model, @class);
            //    var diagnostics = tempModel.GetDiagnostics();
            //    return diagnostics.Any(i => i.Id == "CS0162");
            //}
            return false;
        }

        private class MethodBodyWalker : CSharpSyntaxWalker
        {
            public readonly List<Assignment> Assignments = new List<Assignment>();

            public List<LambdaExpressionSyntax> Lambdas = new List<LambdaExpressionSyntax>();

            private readonly SemanticModel model;

            public MethodBodyWalker(SemanticModel model)
            {
                this.model = model;
            }

#if !PORTABLE
            public override void VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
            {
                base.VisitCasePatternSwitchLabel(node);

                var declaration = node.Pattern as DeclarationPatternSyntax;
                if (declaration == null)
                {
                    return;
                }

                if (declaration.Designation is SingleVariableDesignationSyntax variableDesignation)
                {
                    var symbol = model.GetDeclaredSymbol(variableDesignation);
                    if (symbol != null)
                    {
                        // variables declared in patterns are never null
                        var item = new Assignment(symbol, node, ValueType.NotNull);
                        Assignments.Add(item);
                    }
                }
            }

            public override void VisitIsPatternExpression(IsPatternExpressionSyntax node)
            {
                base.VisitIsPatternExpression(node);

                var declaration = node.Pattern as DeclarationPatternSyntax;
                if (declaration == null)
                {
                    return;
                }

                if (declaration.Designation is SingleVariableDesignationSyntax variableDesignation)
                {
                    var symbol = model.GetDeclaredSymbol(variableDesignation);
                    if (symbol != null)
                    {
                        // variables declared in patterns are never null
                        var item = new Assignment(symbol, node.Expression, ValueType.NotNull);
                        Assignments.Add(item);
                    }
                }
            }
#endif

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                base.VisitAssignmentExpression(node);

                var symbol = model.GetSymbolInfo(node.Left).Symbol;
                if (symbol != null)
                {
                    var item = new Assignment(symbol, node, node.Right.GetTypeOfValue(model));
                    Assignments.Add(item);
                }
            }

            public override void VisitArgument(ArgumentSyntax node)
            {
                base.VisitArgument(node);

                if (node.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword)
                {
                    var symbol = model.GetSymbolInfo(node.Expression).Symbol;
                    if (symbol != null)
                    {
                        Assignments.Add(new Assignment(symbol, node.Expression, ValueType.MaybeNull));
                    }
                }
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                base.VisitVariableDeclarator(node);

                var symbol = model.GetDeclaredSymbol(node);
                if (symbol != null && node.Initializer != null)
                {
                    var item = new Tuple<ISymbol, ExpressionSyntax>(symbol, node.Initializer.Value);
                    Assignments.Add(new Assignment(symbol, node.Initializer.Value, node.Initializer.Value.GetTypeOfValue(model)));
                }
            }

            public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                Lambdas.Add(node);
            }

            public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                Lambdas.Add(node);
            }
        }
    }

    public class Assignment
    {
        public readonly ISymbol Symbol;

        public readonly SyntaxNode Expression;

        public readonly ValueType Value;

        public Assignment(ISymbol symbol, SyntaxNode expression, ValueType value)
        {
            this.Symbol = symbol;
            this.Expression = expression;
            this.Value = value;
        }
    }
}
