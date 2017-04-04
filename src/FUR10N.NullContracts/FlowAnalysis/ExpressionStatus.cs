using System;

namespace FUR10N.NullContracts.FlowAnalysis
{
    public enum ExpressionStatus
    {
        Assigned,
        ReassignedAfterCondition,
        NotAssigned,
        AssignedWithUnneededConstraint
    }

    public static class ExpressionStatusExtensions
    {
        public static bool IsAssigned(this ExpressionStatus status)
        {
            return status == ExpressionStatus.Assigned || status == ExpressionStatus.AssignedWithUnneededConstraint;
        }
    }
}
