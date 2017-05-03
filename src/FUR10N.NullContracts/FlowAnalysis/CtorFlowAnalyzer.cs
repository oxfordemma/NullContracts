using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace FUR10N.NullContracts.FlowAnalysis
{
    internal class CtorFlowAnalyzer
    {
        private readonly SemanticModel model;

        public CtorFlowAnalyzer(SemanticModel model)
        {
            this.model = model;
        }

        /// <summary>
        /// Make sure all ctors set all [NotNull] members.
        /// <para>
        /// Strategy:
        /// Roslyn provides analysis to ensure a local variable gets set in all code paths (like when using the 'out' keyword).
        /// This analysis does not work with variables declared outside of the method (class fields and properties).
        /// So, create a temp copy of each ctor, and replace all writes to the [NotNull] member with writes to a corresponding local variable.
        /// We'll then do analysis on those temp local variables to ensure everything is set.
        /// </para>
        /// </summary>
        /// <param name="class"></param>
        /// <param name="notNullMembers"></param>
        public List<CtorFlowAnalysis> AnalyzeDataFlow(ClassDeclarationSyntax @class, Dictionary<ISymbol, NotNullFieldInfo> notNullMembers)
        {
            var analysis = new List<CtorFlowAnalysis>();
            var ctors = @class.ChildNodes().OfType<ConstructorDeclarationSyntax>().ToList();
            if (ctors.Count == 0)
            {
                return analysis;
            }

            var localDeclarations = notNullMembers.Values.Select(i => i.TempDeclaration).ToArray();

            var newClass = SyntaxFactory.ClassDeclaration(@class.Identifier);
            foreach (var ctor in ctors)
            {
                if (ctor.Body == null)
                {
                    // Probably a compilation error
                    return analysis;
                }
                var newCtor = SyntaxFactory.ConstructorDeclaration(@class.Identifier)
                    .WithModifiers(ctor.Modifiers)
                    .WithParameterList(ctor.ParameterList)
                    .AddBodyStatements(localDeclarations)
                    .AddBodyStatements(ctor.Body.Statements.ToArray());

                var newCtorStatements = newCtor.Body.DescendantNodes().ToList();
                // We need to offset into the new ctor by the number of temp declaration we added.
                // Each temp declaration is 4 nodes.
                var newCtorIndex = 4 * notNullMembers.Count;
                foreach (var node in ctor.Body.DescendantNodes())
                {
                    AssignmentExpressionSyntax assignment = node as AssignmentExpressionSyntax;
                    if (assignment != null)
                    {
                        var assignmentSymbol = model.GetSymbolInfo(assignment.Left).Symbol;
                        if (assignmentSymbol == null)
                        {
                            return analysis;
                        }
                        NotNullFieldInfo notNullMember;
                        if (notNullMembers.TryGetValue(assignmentSymbol, out notNullMember))
                        {
                            var newAssignmentTarget = SyntaxFactory.IdentifierName(notNullMember.TempDeclarationName);
                            newCtor = newCtor.ReplaceNode(newCtorStatements[newCtorIndex], assignment.WithLeft(newAssignmentTarget));
                            newCtorStatements = newCtor.Body.DescendantNodes().ToList();
                        }
                    }
                    newCtorIndex++;
                }

                newClass = newClass.AddMembers(newCtor);
            }

            var newModel = CreateNewModelForClass(model, newClass);

            var ctorParamLists = ctors.ToDictionary(i => GetKeyForCtor(i));
            var instanceNotNullMembers = notNullMembers.Where(i => !i.Value.Symbol.IsStatic).ToDictionary(i => i.Key, i => i.Value);
            var staticNotNullMembers = notNullMembers.Where(i => i.Value.Symbol.IsStatic).ToDictionary(i => i.Key, i => i.Value);
            foreach (var tempCtor in newModel.SyntaxTree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>())
            {
                ConstructorDeclarationSyntax originalCtor;
                if (!ctorParamLists.TryGetValue(GetKeyForCtor(tempCtor), out originalCtor))
                {
                    throw new InvalidOperationException($"Could not find ctor in {@class.Identifier.Text} with params: " + tempCtor.ParameterList.GetText());
                }
                var ctorSymbol = model.GetDeclaredSymbol(originalCtor);
                if (ctorSymbol == null)
                {
                    continue;
                }
                var membersToCheck = ctorSymbol.IsStatic ? staticNotNullMembers : instanceNotNullMembers;
                var flow = newModel.AnalyzeDataFlow(tempCtor.Body);
                var alwaysAssignedMembers = membersToCheck.Values.Where(i => flow.AlwaysAssigned.Select(j => j.Name).Contains(i.TempDeclarationName));
                var notAlwaysAssignedMembers = membersToCheck.Values.Except(alwaysAssignedMembers);
                analysis.Add(new CtorFlowAnalysis(model, originalCtor, notAlwaysAssignedMembers));
            }

            return SecondPass(analysis);
        }

        private string GetKeyForCtor(ConstructorDeclarationSyntax ctor)
        {
            var paramList = ctor.ParameterList.GetText().ToString();
            if (ctor.Modifiers.Any(i =>i.Kind() == SyntaxKind.StaticKeyword))
            {
                return "static_" + paramList;
            }
            return paramList;
        }

        /// <summary>
        /// The second pass will throw away in unassigned members that were actually assigned in chained constructors
        /// </summary>
        /// <param name="analysis"></param>
        /// <returns></returns>
        private List<CtorFlowAnalysis> SecondPass(List<CtorFlowAnalysis> analysis)
        {
            foreach (var ctor in analysis)
            {
                var chainedCtors = ctor.GetAllChainedCtors(analysis).Take(analysis.Count); // Use Take to workaround cyclical ctors

                // Get the list of unassigned members for each ctor in this chain
                var unassignedMembers = chainedCtors.Select(i => i.UnassignedMembers).Concat(new[] { ctor.UnassignedMembers }).ToList();

                // Get the list of members are are not assigned in all ctors in the chain
                var intersection = unassignedMembers.Skip(1).Aggregate(new HashSet<NotNullFieldInfo>(unassignedMembers.First()),
                    (h, e) => { h.IntersectWith(e); return h; }).ToList();

                ctor.UnassignedMembers = ImmutableArray.Create(intersection.ToArray());
            }

            return analysis;
        }

        public static SemanticModel CreateNewModelForClass(SemanticModel model, ClassDeclarationSyntax @class)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(@class);
            var compilation = model.Compilation.AddSyntaxTrees(compilationUnit.SyntaxTree);
            return compilation.GetSemanticModel(compilationUnit.SyntaxTree);
        }
    }
}
