using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace FUR10N.NullContracts.FlowAnalysis
{
    public class Branch
    {
        public List<Branch> Children { get; }

        public Branch Parent { get; }

        public Condition Condition { get; }

        public List<SyntaxNode> Body { get; }

        public Branch(Branch parent, Condition condition)
        {
            Parent = parent;
            Condition = condition ?? new Condition(ConditionType.None);
            Children = new List<Branch>();
            Body = new List<SyntaxNode>();
        }

        public IEnumerable<Branch> GetParents()
        {
            var parent = Parent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }

        /// <summary>
        /// Checks if the expression is in the body (does not look in child branches)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public bool ExpressionIsInBody(SyntaxNode expression)
        {
            return Body.Any(i => i == expression || i.Contains(expression));
        }

        /// <summary>
        /// Checks if this item is a parent of <paramref name="branch"/>
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public bool IsParentOf(Branch branch)
        {
            foreach (var parent in branch.GetParents())
            {
                if (parent == this)
                {
                    return true;
                }
            }
            return false;
        }

        public Branch FindBranch(SyntaxNode expression, out Condition inlineCondition)
        {
            using (new OperationTimer(i => Timings.Update(TimingOperation.FindBranch, i)))
            {
                return InnerFindBranch(this, expression, out inlineCondition);
            }
        }

        private Branch InnerFindBranch(Branch branch, SyntaxNode expression, out Condition inlineCondition)
        {
            if (branch.Condition.TryGetConditionBefore(expression, out inlineCondition))
            {
                return branch.Parent;
            }
            if (branch.ExpressionIsInBody(expression))
            {
                inlineCondition = null;
                return branch;
            }
            foreach (var child in branch.Children)
            {
                var result = InnerFindBranch(child, expression, out inlineCondition);
                if (result != null)
                {
                    return result;
                }
            }
            inlineCondition = null;
            return null;
        }

        public bool Contains(Branch branch)
        {
            if (this == branch)
            {
                return true;
            }
            foreach (var child in Children)
            {
                if (child.Contains(branch))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{string.Join(", ", Condition)} {{Children: {Children.Count}}}";
        }
    }
}
