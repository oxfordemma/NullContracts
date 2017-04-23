using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

using System.Collections.Immutable;
using System.Linq;
using System.Collections.Generic;

namespace FUR10N.NullContracts
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MainAnalyzer : DiagnosticAnalyzer
    {
        public const string ParseFailedId = "NC0000";

        internal static DiagnosticDescriptor ParseFailed =
            new DiagnosticDescriptor(
                id: ParseFailedId,
                title: "Parser Failed",
                messageFormat: "{0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string BadAttributeUsageId = "NC1001";

        internal static DiagnosticDescriptor BadAttributeUsage =
            new DiagnosticDescriptor(
                id: BadAttributeUsageId,
                title: "[NotNull] attribute is not supported here.",
                messageFormat: "{0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string MissingAttributeId = "NC1002";

        internal static DiagnosticDescriptor MissingAttribute =
            new DiagnosticDescriptor(
                id: MissingAttributeId,
                title: "Missing attribute from inherited member",
                messageFormat: "A member that was overridden requries the [NotNull] attribute: {0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string InvalidConstraintId = "NC1003";

        internal static DiagnosticDescriptor InvalidConstraint =
            new DiagnosticDescriptor(
                id: InvalidConstraintId,
                title: "Unsupported Constraint",
                messageFormat: "Constraints can only be applied to locals or fields/properties",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string AssignmentAfterConstraintId = "NC1004";

        internal static DiagnosticDescriptor AssignmentAfterConstraint =
            new DiagnosticDescriptor(
                id: AssignmentAfterConstraintId,
                title: "Assignment After Constraint",
                messageFormat: "Cannot modify a field that has a constraint on it. Field: {0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string AssignmentAfterConditionId = "NC1005";

        internal static DiagnosticDescriptor AssignmentAfterCondition =
            new DiagnosticDescriptor(
                id: AssignmentAfterConditionId,
                title: "A member was reassigned after a pre-condition",
                messageFormat: "A member that was checked for null was reassigned after the check: {0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string UnneededNullCheckId = "NC2001";

        internal static DiagnosticDescriptor UnneededNullCheck =
            new DiagnosticDescriptor(
                id: UnneededNullCheckId,
                title: "Unneeded Null Check",
                messageFormat: "Unneeded null check on [NotNull]: {0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public const string UnneededConstraintId = "NC2002";

        internal static DiagnosticDescriptor UnneededConstraint =
            new DiagnosticDescriptor(
                id: UnneededConstraintId,
                title: "A constraint was not needed",
                messageFormat: "There was a constraint on a member that is already checked for null.",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public const string NullAssignmentId = "NC3001";

        internal static DiagnosticDescriptor NullAssignment =
            new DiagnosticDescriptor(
                id: NullAssignmentId,
                title: "Assigned null to [NotNull] member",
                messageFormat: "Tried to assign a null to a [NotNull] member: {0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string MemberNotInitializedId = "NC3002";

        internal static DiagnosticDescriptor MemberNotInitialized =
            new DiagnosticDescriptor(
                id: MemberNotInitializedId,
                title: "[NotNull] member not initalized",
                messageFormat: "The [NotNull] member '{0}' might not have been initialized during construction.",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string PropagateNotNullInCtorsId = "NC3003";

        internal static DiagnosticDescriptor PropagateNotNullInCtors =
            new DiagnosticDescriptor(
                id: PropagateNotNullInCtorsId,
                title: "Propagate [NotNull] in chained ctors",
                messageFormat: "The chained constructor expects a [NotNull] arg: {0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string ReturnNullId = "NC3004";

        internal static DiagnosticDescriptor ReturnNull =
            new DiagnosticDescriptor(
                id: ReturnNullId,
                title: "Returned a possibly null value",
                messageFormat: "Tried to return a value in a [NotNull] method that might be null: {0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public const string NotNullAsRefParameterId = "NC3005";

        internal static DiagnosticDescriptor NotNullAsRefParameter =
            new DiagnosticDescriptor(
                id: NotNullAsRefParameterId,
                title: "Used [NotNull] member as a ref parameter",
                messageFormat: "Used [NotNull] member as a ref parameter: {0}",
                category: "Nulls",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = 
            ImmutableArray.Create(new[] {
                ParseFailed,
                BadAttributeUsage,
                UnneededNullCheck,
                NullAssignment,
                MemberNotInitialized,
                PropagateNotNullInCtors,
                InvalidConstraint,
                AssignmentAfterConstraint,
                ReturnNull,
                MissingAttribute,
                AssignmentAfterCondition,
                UnneededConstraint,
                NotNullAsRefParameter });

        public static Diagnostic CreateBadAttributeUsageError(Location location, bool forValueType)
        {
            string message;
            if (forValueType)
            {
                message = "[NotNull] is only supported on reference types.";
            }
            else
            {
                message = "[NotNull] is currently only supported on read-only fields/properties and method arguments.";
            }
            return Diagnostic.Create(BadAttributeUsage, location, message);
        }

        public static Diagnostic CreateUnneededNullCheckError(Location location, ISymbol symbol)
        {
            return Diagnostic.Create(UnneededNullCheck, location, symbol.GetFullName());
        }

        public static Diagnostic CreateNullAssignmentError(Location location, ISymbol symbol)
        {
            return Diagnostic.Create(NullAssignment, location, symbol.GetFullName());
        }

        public static Diagnostic CreateNullAssignmentError(Location location, string errorContext)
        {
            return Diagnostic.Create(NullAssignment, location, errorContext);
        }

        public static Diagnostic CreateMemberNotInitialized(Location location, ISymbol symbol)
        {
            return Diagnostic.Create(MemberNotInitialized, location, symbol.GetFullName());
        }

        public static Diagnostic CreatePropagateNotNullInCtors(Location location, ISymbol symbol)
        {
            return Diagnostic.Create(PropagateNotNullInCtors, location, symbol.GetFullName());
        }

        public static Diagnostic CreatePropagateNotNullInCtors(Location location, string errorContext)
        {
            return Diagnostic.Create(PropagateNotNullInCtors, location, errorContext);
        }

        public static Diagnostic CreateInvalidConstraintError(Location location)
        {
            return Diagnostic.Create(InvalidConstraint, location);
        }

        public static Diagnostic CreateAssignmentAfterConstraint(Location location, string errorContext)
        {
            return Diagnostic.Create(AssignmentAfterConstraint, location, errorContext);
        }

        public static Diagnostic CreateReturnNull(Location location, string errorContext)
        {
            return Diagnostic.Create(ReturnNull, location, errorContext);
        }

        public static Diagnostic CreateMissingAttribute(Location location, string errorContext)
        {
            return Diagnostic.Create(MissingAttribute, location, errorContext);
        }

        public static Diagnostic CreateAssignmentAfterCondition(Location location, string errorContext)
        {
            return Diagnostic.Create(AssignmentAfterCondition, location, errorContext);
        }

        public static Diagnostic CreateUnneededConstraint(Location location, string errorContext)
        {
            return Diagnostic.Create(UnneededConstraint, location, errorContext);
        }

        public static Diagnostic CreateNotNullAsRefParameter(Location location, string errorContext)
        {
            return Diagnostic.Create(NotNullAsRefParameter, location, errorContext);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCodeBlockAction(AnalyzeCodeBlock);
            context.RegisterSemanticModelAction(AnalyzeModel);
        }

        private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
        {
            using (new OperationTimer(i => Timings.Update(TimingOperation.CodeBlockAnlyzer, i)))
            {
                try
                {
                    SetupCustomNotNullMethods(context.SemanticModel.Compilation, context.Options);
                    new ExpressionAnalyzer(context).Analyze(context.CodeBlock);
                }
                catch (ParseFailedException ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ParseFailed, ex.Location, ex.Message));
#if DEBUG
                    throw;
#endif
                }
            }
        }

        private static void AnalyzeModel(SemanticModelAnalysisContext context)
        {
            using (new OperationTimer(i => Timings.Update(TimingOperation.ClassAnalyzer, i)))
            {
                try
                {
                    SetupCustomNotNullMethods(context.SemanticModel.Compilation, context.Options);
                    new ClassAnalyzer(context).Analyze(context.SemanticModel);
                }
                catch (ParseFailedException ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ParseFailed, ex.Location, ex.Message));
#if DEBUG
                    throw;
#endif
                }
            }
        }

        private static void SetupCustomNotNullMethods(Compilation compilation, AnalyzerOptions options)
        {
            var file = options.AdditionalFiles.FirstOrDefault(i =>
                i.Path.EndsWith("NullContracts.NotNullMethods.txt", System.StringComparison.OrdinalIgnoreCase));
            if (file == null)
            {
                return;
            }
            SystemTypeSymbols.AddExternalNotNullMethods(compilation, file);
        }
    }
}
