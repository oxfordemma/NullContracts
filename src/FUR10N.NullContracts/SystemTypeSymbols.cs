using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace FUR10N.NullContracts
{
    public class SystemTypeSymbols
    {
        private static HashSet<ISymbol> ExternalNotNullMethods { get; } = new HashSet<ISymbol>();

        private static int lastConfigLength;

        public ITypeSymbol StringType { get; }

        public IMethodSymbol StringIsNullOrEmpty { get; }

        public IMethodSymbol StringIsNullOrWhiteSpace { get; }

        public ITypeSymbol UriType { get; }

        public ImmutableArray<IMethodSymbol> UriTryCreate { get; }

        public IPropertySymbol DictionaryValues { get; }

        public IPropertySymbol DictionaryKeys { get; }

        public ITypeSymbol Enumerable { get; }

        public IMethodSymbol ConfigureAwait { get; }

        private readonly HashSet<IMethodSymbol> NotNullFrameworkMethods = new HashSet<IMethodSymbol>();

        public static void AddExternalNotNullMethods(Compilation compilation, AdditionalText config)
        {
            lock (ExternalNotNullMethods)
            {
                var text = config.GetText();
                if (lastConfigLength == text.Length)
                {
                    return;
                }
                ExternalNotNullMethods.Clear();
                lastConfigLength = text.Length;
                // SourceText.Lines sometimes doesn't actually split the text into multiple lines
                foreach (var method in text.ToString().Split('\n').Select(i => i.Trim()))
                {
                    if (!method.Contains("."))
                    {
                        return;
                    }

                    var separatorIndex = method.LastIndexOf('.');
                    var className = method.Substring(0, separatorIndex);
                    var methodName = method.Substring(separatorIndex + 1);

                    var type = compilation.GetTypeByMetadataName(className);
                    if (type == null)
                    {
                        return;
                    }
                    foreach (var match in type.GetMembers(methodName))
                    {
                        ExternalNotNullMethods.Add(match);
                    }
                }
            }
        }

        public SystemTypeSymbols(Compilation compilation)
        {
            if (StringType != null)
            {
                return;
            }

            StringType = compilation.GetTypeByMetadataName(typeof(string).FullName);
            if (StringType != null)
            {
                StringIsNullOrEmpty = StringType.GetMembers("IsNullOrEmpty").OfType<IMethodSymbol>().First();
                StringIsNullOrWhiteSpace = StringType.GetMembers("IsNullOrWhiteSpace").OfType<IMethodSymbol>().First();
                AddRange(NotNullFrameworkMethods, StringType.GetMembers("Substring").OfType<IMethodSymbol>());
                AddRange(NotNullFrameworkMethods, StringType.GetMembers("Replace").OfType<IMethodSymbol>());
            }

            UriType = compilation.GetTypeByMetadataName(typeof(Uri).FullName);
            if (UriType != null)
            {
                UriTryCreate = UriType.GetMembers("TryCreate").OfType<IMethodSymbol>().ToImmutableArray();
                NotNullFrameworkMethods.Add(UriType.GetMembers("ToString").OfType<IMethodSymbol>().First());
            }

            var dictionary = compilation.GetTypeByMetadataName(typeof(Dictionary<,>).FullName);
            DictionaryValues = dictionary.GetMembers("Values").OfType<IPropertySymbol>().First();
            DictionaryKeys = dictionary.GetMembers("Keys").OfType<IPropertySymbol>().First();

            var guid = compilation.GetTypeByMetadataName(typeof(Guid).FullName);
            AddRange(NotNullFrameworkMethods, guid.GetMembers("ToString").OfType<IMethodSymbol>());

            Enumerable = compilation?.GetTypeByMetadataName(typeof(Enumerable).FullName);
            if (Enumerable != null)
            {
                AddRange(NotNullFrameworkMethods, Enumerable.GetMembers("ToList").OfType<IMethodSymbol>());
                AddRange(NotNullFrameworkMethods, Enumerable.GetMembers("ToArray").OfType<IMethodSymbol>());
                AddRange(NotNullFrameworkMethods, Enumerable.GetMembers("Where").OfType<IMethodSymbol>());
                AddRange(NotNullFrameworkMethods, Enumerable.GetMembers("Select").OfType<IMethodSymbol>());
            }

            var path = compilation.GetTypeByMetadataName(typeof(System.IO.Path).FullName);
            if (path != null)
            {
                var getTempPath = path.GetMembers("GetTempPath").OfType<IMethodSymbol>().FirstOrDefault();
                if (getTempPath != null)
                {
                    NotNullFrameworkMethods.Add(getTempPath);
                }
            }

            var marshal = compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.Marshal).FullName);
            if (marshal != null)
            {
                var getObject = marshal.GetMembers("GetObjectForIUnknown").OfType<IMethodSymbol>().FirstOrDefault();
                if (getObject != null)
                {
                    NotNullFrameworkMethods.Add(getObject);
                }
            }

            var task = compilation.GetTypeByMetadataName(typeof(Task).FullName);
            if (task != null)
            {
                var fromResult = task.GetMembers("FromResult").OfType<IMethodSymbol>().FirstOrDefault();
                if (fromResult != null)
                {
                    NotNullFrameworkMethods.Add(fromResult);
                }
            }

            var task1 = compilation.GetTypeByMetadataName(typeof(Task<>).FullName);
            if (task1 != null)
            {
                ConfigureAwait = task1.GetMembers("ConfigureAwait").OfType<IMethodSymbol>().FirstOrDefault();
            }

            var int64 = compilation.GetTypeByMetadataName(typeof(long).FullName);
            if (int64 != null)
            {
                AddRange(NotNullFrameworkMethods, int64.GetMembers("ToString").OfType<IMethodSymbol>());
            }
        }

        private void AddRange<T>(HashSet<T> set, IEnumerable<T> list)
        {
            foreach (var item in list)
            {
                set.Add(item);
            }
        }

        public bool IsMethodCallThatIsNotNull(IMethodSymbol method)
        {
            using (new OperationTimer(i => Timings.Update(TimingOperation.SymbolLookup, i)))
            {
                var reduced = method.OriginalDefinition.ReducedFrom ?? method.OriginalDefinition;

                if (NotNullFrameworkMethods.Contains(reduced) || ExternalNotNullMethods.Contains(reduced))
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsPropertyThatIsNotNull(ISymbol property)
        {
            if (DictionaryValues.Equals(property.OriginalDefinition))
            {
                return true;
            }
            if (DictionaryKeys.Equals(property.OriginalDefinition))
            {
                return true;
            }
            if (ExternalNotNullMethods.Contains(property))
            {
                return true;
            }
            return false;
        }
    }
}
