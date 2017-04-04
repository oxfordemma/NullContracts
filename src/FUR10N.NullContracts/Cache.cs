using FUR10N.NullContracts.FlowAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FUR10N.NullContracts
{
    public class Cache
    {
        private static readonly object objectLock = new object();

        private static readonly Dictionary<SemanticModel, CompilationInfo> Compilations = new Dictionary<SemanticModel, CompilationInfo>();

        private static void AddEntry(SemanticModel model, CompilationInfo info)
        {
            if (Compilations.Count >= 5)
            {
                var first = Compilations.First();
                first.Value.Dispose();
                Compilations.Remove(first.Key);
            }
            Compilations[model] = info;
        }

        public static CompilationInfo Get(SemanticModel model)
        {
            lock (objectLock)
            {
                if (Compilations.TryGetValue(model, out var info))
                {
                    return info;
                }
                var newInfo = new CompilationInfo(model);
                AddEntry(model, newInfo);
                return newInfo;
            }
        }
    }

    public class CompilationInfo : IDisposable
    {
        public SemanticModel Model { get; }

        public SystemTypeSymbols Symbols { get; }

        private readonly CacheProvider methodCache = new CacheProvider(TimeSpan.FromSeconds(20));

        public CompilationInfo(SemanticModel model)
        {
            this.Model = model;
            this.Symbols = new SystemTypeSymbols(model.Compilation);
        }

        public MethodFlowAnalysis GetMethodAnalysis(ISymbol methodSymbol, BlockSyntax body, IEnumerable<SyntaxNode> statements)
        {
            var key = methodSymbol.ContainingNamespace.Name + "." + methodSymbol.ToDisplayString();
            var existing = methodCache.Get<string, MethodFlowAnalysis>(key);
            if (existing != null)
            {
                return existing;
            }
            lock (methodCache)
            {
                existing = methodCache.Get<string, MethodFlowAnalysis>(key);
                if (existing == null)
                {
                    existing = new MethodFlowAnalyzer(Model).Analyze(body, statements);
                    methodCache.Add<string, MethodFlowAnalysis>(key, existing, TimeSpan.FromSeconds(20), CacheItemPriority.Normal);
                }
                return existing;
            }
        }

        public void Dispose()
        {
            methodCache.Dispose();
        }
    }
}
