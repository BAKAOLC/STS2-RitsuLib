using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace STS2RitsuLib.Analyzers
{
    [Generator]
    public sealed class ModuleTypeForwarderGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, static (output, compilation) =>
            {
                if (!string.Equals(compilation.AssemblyName, "STS2-RitsuLib", StringComparison.Ordinal))
                    return;

                var names = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
                {
                    if (assembly.Name is not ("STS2-RitsuLib.Shared" or "STS2-RitsuLib.Ui" or
                        "STS2-RitsuLib.Settings" or "STS2-RitsuLib.Runtime"))
                        continue;
                    Collect(assembly.GlobalNamespace, names);
                }

                var source = new StringBuilder();
                foreach (var name in names)
                    source.Append("[assembly: global::System.Runtime.CompilerServices.TypeForwardedTo(typeof(")
                        .Append(name).AppendLine("))]");
                output.AddSource("ModuleTypeForwarders.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            });
        }

        private static void Collect(INamespaceSymbol space, ISet<string> names)
        {
            foreach (var type in space.GetTypeMembers()
                         .Where(type => type.DeclaredAccessibility == Accessibility.Public))
            {
                if (space.ToDisplayString() != "STS2RitsuLib" &&
                    !space.ToDisplayString().StartsWith("STS2RitsuLib.", StringComparison.Ordinal))
                    continue;
                var target = type.IsGenericType ? type.ConstructUnboundGenericType() : type;
                names.Add(target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            foreach (var child in space.GetNamespaceMembers())
                Collect(child, names);
        }
    }
}
