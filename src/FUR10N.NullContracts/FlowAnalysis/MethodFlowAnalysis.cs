using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FUR10N.NullContracts.FlowAnalysis
{
    public class MethodFlowAnalysis
    {
        private readonly SemanticModel model;

        private List<Assignment> assignments { get; }

        private ImmutableArray<ISymbol> AlwaysAssignedToNotNull { get; }

        private Branch BranchTree { get; }

        private List<Branch> Lambdas { get; }

        public bool HasConstraints { get; }

        public List<ReturnStatementSyntax> ReturnStatements { get; }

        public MethodFlowAnalysis(
            SemanticModel model,
            List<Assignment> assignments,
            ImmutableArray<ISymbol> alwaysAssignedToNotNull,
            Branch branchTree,
            List<Branch> lambdas,
            bool hasConstraints,
            List<ReturnStatementSyntax> returnStatements)
        {
            this.model = model;
            this.assignments = assignments;
            this.AlwaysAssignedToNotNull = alwaysAssignedToNotNull;
            this.BranchTree = branchTree;
            this.Lambdas = lambdas;
            this.HasConstraints = hasConstraints;
            this.ReturnStatements = returnStatements;
        }

        public IEnumerable<Assignment> GetAssignmentsAfterConstraints()
        {
            foreach (var assignment in assignments)
            {
                foreach (var branch in GetListOfParents(assignment.Expression, out var inlineCondition))
                {
                    if (branch.Condition.IsConstraintFor(assignment.Symbol, model))
                    {
                        yield return assignment;
                        break;
                    }
                }
            }
            yield break;
        }

        public ExpressionStatus IsAlwaysAssigned(ExpressionSyntax expression, SyntaxNode parent)
        {
            using (new OperationTimer(i => Timings.Update(TimingOperation.IsAlwaysAssigned, i)))
            {
                var argSymbol = model.GetSymbolInfo(expression).Symbol;
                if (AlwaysAssignedToNotNull.Contains(argSymbol))
                {
                    // Agument might have been null, but it was assigned to a NotNull in all execution paths
                    return ExpressionStatus.Assigned;
                }

                var assignmentsForExpression = assignments.Where(i => i.Symbol.Equals(argSymbol)).ToList();
                // Fields/properties might be unassigned. We can really only look at locals
                if (argSymbol is ILocalSymbol)
                {
                    if (assignmentsForExpression.Count > 0)
                    {
                        if (assignmentsForExpression.All(i => i.Value == ValueType.NotNull))
                        {
                            return ExpressionStatus.Assigned;
                        }
                    }
                }

                // TODO: cache this?
                var path = GetListOfParents(parent, out var inlineCondition);
                var key = new ExpressionKey(expression, model);
                if (inlineCondition != null && inlineCondition.IsNotNullShortCircuit(key))
                {
                    // Short circuiting expression
                    return ExpressionStatus.Assigned;
                }

                // Find the condition that checks for NotNull
                Branch branchWithNullCheck = GetBranchWithNullCheck(path, key, out var unneededConstraint);

                if (branchWithNullCheck == null)
                {
                    return ExpressionStatus.NotAssigned;
                }

                if (NullAssignmentsAreInPath(expression, assignmentsForExpression, branchWithNullCheck))
                {
                    return ExpressionStatus.ReassignedAfterCondition;
                }

                return unneededConstraint ? ExpressionStatus.AssignedWithUnneededConstraint : ExpressionStatus.Assigned;
            }
        }

        /// <summary>
        /// Checks if there were any assignments to null on the symbol that is being checked in <paramref name="expression"/>
        /// </summary>
        /// <param name="expression">The expression containing the symbol being validated</param>
        /// <param name="assignmentsForExpression">All assignments to the symbol being validated</param>
        /// <param name="branchWithMatchingCondition">The branch with the condition that proves the expression is valid</param>
        /// <returns></returns>
        private bool NullAssignmentsAreInPath(
            ExpressionSyntax expression,
            List<Assignment> assignmentsForExpression,
            Branch branchWithMatchingCondition)
        {
            foreach (var assignment in assignmentsForExpression.Where(i => i.Value != ValueType.NotNull))
            {
                var assignmentBranch = BranchTree.FindBranch(assignment.Expression, out var trash);
                if (assignmentBranch != null)
                {
                    if (branchWithMatchingCondition == assignmentBranch
                        || branchWithMatchingCondition.IsParentOf(assignmentBranch))
                    {
                        if (branchWithMatchingCondition.Condition.IsWhile(assignment.Symbol, model))
                        {
                            return assignment.Expression.SpanStart < expression.SpanStart;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private Branch GetBranchWithNullCheck(List<Branch> parents, ExpressionKey key, out bool unneededConstraint)
        {
            Branch constraintBranch = null;
            Branch branchWithMatchingCondition = null;
            foreach (var parent in parents)
            {
                if (parent.Condition.IsNotNull(key))
                {
                    branchWithMatchingCondition = parent;
                    if (parent.Condition.Kind != ConditionType.Constraint)
                    {
                        break;
                    }
                    else
                    {
                        constraintBranch = parent;
                    }
                }
            }
            unneededConstraint = constraintBranch != null && constraintBranch != branchWithMatchingCondition;
            return branchWithMatchingCondition;
        }

        private List<Branch> GetListOfParents(SyntaxNode expression, out Condition inlineCondition)
        {
            var results = new List<Branch>();
            var containingBranch = FindBranch(expression, out inlineCondition);
            var branch = containingBranch;
            while (branch != null)
            {
                results.Add(branch);
                branch = branch.Parent;
            }
            return results;
        }

        private Branch FindBranch(SyntaxNode expression, out Condition inlineCondition)
        {
            // First search any lambdas. Those are kept separate since they could run at any time.
            foreach (var lambda in Lambdas)
            {
                Branch branch = lambda.FindBranch(expression, out inlineCondition);
                if (branch != null)
                {
                    return branch;
                }
            }
            // If not in a lambda, then search the full method body.
            return BranchTree.FindBranch(expression, out inlineCondition);
        }
    }
}
