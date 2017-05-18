using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FUR10N.NullContracts.FlowAnalysis
{
    internal class CtorFlowAnalysis
    {
        private readonly SemanticModel model;

        public readonly ConstructorDeclarationSyntax Constructor;

        public ImmutableArray<NotNullFieldInfo> UnassignedMembers;

        private IMethodSymbol constructorSymbol;

        public IMethodSymbol ConstructorSymbol
        {
            get
            {
                return constructorSymbol ?? (constructorSymbol = model.GetDeclaredSymbol(Constructor));
            }
        }

        public CtorFlowAnalysis(SemanticModel model, ConstructorDeclarationSyntax constructor, IEnumerable<NotNullFieldInfo> membersNotAssigned)
        {
            this.model = model;
            Constructor = constructor;
            UnassignedMembers = ImmutableArray.Create(membersNotAssigned.ToArray());
        }

        /// <summary>
        /// Returns all constructors that this constructor invokes.
        /// </summary>
        /// <param name="flowAnalysis"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public IEnumerable<CtorFlowAnalysis> GetAllChainedCtors(IList<CtorFlowAnalysis> allCtors)
        {
            // TODO: guard against cyclical ctors
            var chainedCtor = this.Constructor.Initializer;
            if (chainedCtor == null)
            {
                yield break;
            }

            var targetSymbol = model.GetSymbolInfo(chainedCtor);
            if (targetSymbol.Symbol == null)
            {
                yield break;
            }

            foreach (var ctor in allCtors)
            {
                if (targetSymbol.Symbol.Equals(ctor.ConstructorSymbol))
                {
                    yield return ctor;

                    foreach (var nestedCtor in ctor.GetAllChainedCtors(allCtors))
                    {
                        yield return nestedCtor;
                    }
                }
            }
        }
    }
}
