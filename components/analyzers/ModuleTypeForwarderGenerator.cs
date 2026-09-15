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
        private static readonly DiagnosticDescriptor InvalidOrigin = new(
            "RLTF001",
            "Invalid forwarded-from assembly identity",
            "Type '{0}' declares an invalid TypeForwardedFrom assembly identity",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, static (output, compilation) =>
            {
                var isFacade = compilation.AssemblyName == "STS2-RitsuLib";
                var originAttribute = compilation.GetTypeByMetadataName(
                    "System.Runtime.CompilerServices.TypeForwardedFromAttribute");
                var names = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
                {
                    if (assembly.Name is not ("STS2-RitsuLib.Shared" or "STS2-RitsuLib.Ui" or
                        "STS2-RitsuLib.Settings" or "STS2-RitsuLib.Runtime"))
                        continue;
                    Collect(assembly.GlobalNamespace, names, ShouldForward);
                }

                if (names.Count == 0)
                    return;

                var source = new StringBuilder();
                foreach (var name in names)
                    source.Append("[assembly: global::System.Runtime.CompilerServices.TypeForwardedTo(typeof(")
                        .Append(name).AppendLine("))]");
                output.AddSource("ModuleTypeForwarders.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));

                return;

                bool ShouldForward(INamedTypeSymbol type)
                {
                    var origin = type.GetAttributes().FirstOrDefault(attribute =>
                        SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, originAttribute));
                    if (origin == null)
                        return isFacade;

                    if (origin.ConstructorArguments.Length != 1 ||
                        origin.ConstructorArguments[0].Value is not string assemblyName ||
                        !AssemblyIdentity.TryParseDisplayName(assemblyName, out var identity))
                    {
                        output.ReportDiagnostic(Diagnostic.Create(InvalidOrigin, Location.None,
                            type.ToDisplayString()));
                        return false;
                    }

                    return isFacade || string.Equals(identity.Name, compilation.AssemblyName,
                        StringComparison.OrdinalIgnoreCase);
                }
            });
        }

        private static void Collect(INamespaceSymbol space, ISet<string> names,
            Func<INamedTypeSymbol, bool> shouldForward)
        {
            foreach (var type in space.GetTypeMembers()
                         .Where(type => type.DeclaredAccessibility == Accessibility.Public))
            {
                if (space.ToDisplayString() != "STS2RitsuLib" &&
                    !space.ToDisplayString().StartsWith("STS2RitsuLib.", StringComparison.Ordinal))
                    continue;
                if (!shouldForward(type))
                    continue;
                var target = type.IsGenericType ? type.ConstructUnboundGenericType() : type;
                names.Add(target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            foreach (var child in space.GetNamespaceMembers())
                Collect(child, names, shouldForward);
        }
    }
}
