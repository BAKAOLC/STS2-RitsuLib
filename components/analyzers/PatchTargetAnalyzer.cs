using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace STS2RitsuLib.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PatchTargetAnalyzer : DiagnosticAnalyzer
    {
        private const string PatchTargetTypeName = "STS2RitsuLib.Patching.Models.PatchTarget";
        private const string ModPatchTargetTypeName = "STS2RitsuLib.Patching.Models.ModPatchTarget";

        private const string AsyncStateMachineAttributeName =
            "System.Runtime.CompilerServices.AsyncStateMachineAttribute";

        private const string IteratorStateMachineAttributeName =
            "System.Runtime.CompilerServices.IteratorStateMachineAttribute";

        private static readonly DiagnosticDescriptor TargetMissing = new(
            "RLPT001",
            "Patch target does not match",
            "{0}",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor TargetAmbiguous = new(
            "RLPT002",
            "Patch target is ambiguous",
            "{0}",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            [TargetMissing, TargetAmbiguous];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(static compilationContext =>
            {
                var patchTargetType =
                    compilationContext.Compilation.GetTypeByMetadataName(PatchTargetTypeName);
                var modPatchTargetType =
                    compilationContext.Compilation.GetTypeByMetadataName(ModPatchTargetTypeName);
                if (patchTargetType == null || modPatchTargetType == null)
                    return;

                compilationContext.RegisterOperationAction(
                    operationContext => AnalyzeInvocation(operationContext, patchTargetType),
                    OperationKind.Invocation);
                compilationContext.RegisterOperationAction(
                    operationContext => AnalyzeObjectCreation(operationContext, modPatchTargetType),
                    OperationKind.ObjectCreation);
            });
        }

        private static void AnalyzeInvocation(
            OperationAnalysisContext context,
            INamedTypeSymbol patchTargetType)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;
            if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, patchTargetType)
                || !TryGetFactoryKind(method.Name, out var kind, out var ignoreIfMissing)
                || !TryGetTargetType(invocation, method, out var targetType)
                || !TryGetMethodName(invocation, method, kind, out var methodName, out var location)
                || !TryGetParameterTypes(invocation.Arguments, out var parameterTypes))
                return;

            AnalyzeTarget(
                context,
                new(targetType, methodName, parameterTypes, ignoreIfMissing, kind, location));
        }

        private static void AnalyzeObjectCreation(
            OperationAnalysisContext context,
            INamedTypeSymbol modPatchTargetType)
        {
            var creation = (IObjectCreationOperation)context.Operation;
            var constructor = creation.Constructor;
            if (constructor == null
                || !SymbolEqualityComparer.Default.Equals(constructor.ContainingType, modPatchTargetType)
                || !TryGetTypeArgument(creation.Arguments, "targetType", out var targetType)
                || !TryGetStringArgument(creation.Arguments, "methodName", out var methodName, out var location)
                || !TryGetParameterTypes(creation.Arguments, out var parameterTypes)
                || !TryGetBooleanArgument(creation.Arguments, "ignoreIfMissing", false, out var ignoreIfMissing)
                || !TryGetMethodType(creation.Arguments, out var kind))
                return;

            AnalyzeTarget(
                context,
                new(targetType, methodName, parameterTypes, ignoreIfMissing, kind, location));
        }

        private static void AnalyzeTarget(OperationAnalysisContext context, PatchTargetSpec target)
        {
            var match = FindTarget(target);
            if (match == TargetMatch.Single
                || match == TargetMatch.Missing && target.IgnoreIfMissing)
                return;

            var display = FormatTarget(target);
            var message = match == TargetMatch.Ambiguous
                ? $"Patch target '{display}' resolves to more than one method."
                : $"Patch target '{display}' was not found.";
            context.ReportDiagnostic(Diagnostic.Create(
                match == TargetMatch.Ambiguous ? TargetAmbiguous : TargetMissing,
                target.Location,
                message));
        }

        private static TargetMatch FindTarget(PatchTargetSpec target)
        {
            return target.Kind switch
            {
                PatchTargetKind.Getter => FindPropertyAccessor(target, true),
                PatchTargetKind.Setter => FindPropertyAccessor(target, false),
                PatchTargetKind.Constructor => FindConstructor(target),
                PatchTargetKind.Async => FindMethod(target, AsyncStateMachineAttributeName),
                PatchTargetKind.Enumerator => FindMethod(target, IteratorStateMachineAttributeName),
                _ => FindMethod(target, null),
            };
        }

        private static TargetMatch FindMethod(PatchTargetSpec target, string? stateMachineAttributeName)
        {
            var matches = EnumerateMethods(target.TargetType, target.MethodName)
                .Where(method => MatchesParameters(method, target.ParameterTypes))
                .Where(static method => !method.IsAbstract)
                .Where(method => stateMachineAttributeName == null
                                 || HasStateMachine(method, stateMachineAttributeName))
                .Take(2)
                .Count();
            return ToMatch(matches);
        }

        private static TargetMatch FindPropertyAccessor(PatchTargetSpec target, bool getter)
        {
            var matches = target.TargetType.GetMembers(target.MethodName)
                .OfType<IPropertySymbol>()
                .Select(property => getter ? property.GetMethod : property.SetMethod)
                .Where(static method => method is { IsAbstract: false })
                .Take(2)
                .Count();
            return ToMatch(matches);
        }

        private static TargetMatch FindConstructor(PatchTargetSpec target)
        {
            if (target.TargetType is not INamedTypeSymbol namedType)
                return TargetMatch.Missing;

            var matches = namedType.InstanceConstructors
                .Where(method => MatchesParameters(method, target.ParameterTypes))
                .Take(2)
                .Count();
            return ToMatch(matches);
        }

        private static IEnumerable<IMethodSymbol> EnumerateMethods(ITypeSymbol targetType, string methodName)
        {
            var selected = new List<IMethodSymbol>();
            for (var current = targetType as INamedTypeSymbol; current != null; current = current.BaseType)
            {
                foreach (var method in EnumerateDeclaredMethods(current, methodName))
                {
                    if (!SymbolEqualityComparer.Default.Equals(current, targetType)
                        && (method.IsStatic || method.DeclaredAccessibility == Accessibility.Private))
                        continue;
                    if (selected.Any(candidate => HasSameRuntimeSignature(candidate, method)))
                        continue;

                    selected.Add(method);
                    yield return method;
                }
            }
        }

        private static IEnumerable<IMethodSymbol> EnumerateDeclaredMethods(
            INamedTypeSymbol type,
            string methodName)
        {
            foreach (var member in type.GetMembers())
            {
                switch (member)
                {
                    case IMethodSymbol method when method.MetadataName == methodName:
                        yield return method;
                        break;

                    case IPropertySymbol property:
                        if (property.GetMethod?.MetadataName == methodName)
                            yield return property.GetMethod;
                        if (property.SetMethod?.MetadataName == methodName)
                            yield return property.SetMethod;
                        break;

                    case IEventSymbol eventSymbol:
                        if (eventSymbol.AddMethod?.MetadataName == methodName)
                            yield return eventSymbol.AddMethod;
                        if (eventSymbol.RemoveMethod?.MetadataName == methodName)
                            yield return eventSymbol.RemoveMethod;
                        break;
                }
            }
        }

        private static bool HasSameRuntimeSignature(IMethodSymbol left, IMethodSymbol right)
        {
            if (left.Arity != right.Arity
                || left.Parameters.Length != right.Parameters.Length)
                return false;

            for (var i = 0; i < left.Parameters.Length; i++)
            {
                var leftParameter = left.Parameters[i];
                var rightParameter = right.Parameters[i];
                if (leftParameter.RefKind != rightParameter.RefKind
                    || !SymbolEqualityComparer.Default.Equals(leftParameter.Type, rightParameter.Type))
                    return false;
            }

            return true;
        }

        private static bool MatchesParameters(
            IMethodSymbol method,
            ImmutableArray<ITypeSymbol>? parameterTypes)
        {
            if (parameterTypes == null)
                return true;
            if (method.Parameters.Length != parameterTypes.Value.Length)
                return false;

            for (var i = 0; i < method.Parameters.Length; i++)
            {
                if (method.Parameters[i].RefKind != RefKind.None
                    || !SymbolEqualityComparer.Default.Equals(method.Parameters[i].Type, parameterTypes.Value[i]))
                    return false;
            }

            return true;
        }

        private static bool HasStateMachine(IMethodSymbol method, string attributeName)
        {
            if (attributeName == AsyncStateMachineAttributeName && method.IsAsync)
                return true;

            var attribute = method.GetAttributes().FirstOrDefault(candidate =>
                candidate.AttributeClass?.ToDisplayString() == attributeName);
            if (attribute == null
                || attribute.ConstructorArguments.Length != 1
                || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol stateMachineType)
                return false;

            return stateMachineType.GetMembers("MoveNext")
                .OfType<IMethodSymbol>()
                .Any(static candidate =>
                    candidate.MethodKind == MethodKind.Ordinary
                    && candidate.Parameters.Length == 0
                    && !candidate.IsAbstract);
        }

        private static bool TryGetFactoryKind(
            string methodName,
            out PatchTargetKind kind,
            out bool ignoreIfMissing)
        {
            ignoreIfMissing = methodName.StartsWith("Optional", StringComparison.Ordinal);
            var normalizedName = ignoreIfMissing ? methodName.Substring("Optional".Length) : methodName;
            kind = normalizedName switch
            {
                "Method" => PatchTargetKind.Normal,
                "Getter" => PatchTargetKind.Getter,
                "Setter" => PatchTargetKind.Setter,
                "Constructor" => PatchTargetKind.Constructor,
                "AsyncMethod" => PatchTargetKind.Async,
                "EnumeratorMethod" => PatchTargetKind.Enumerator,
                _ => PatchTargetKind.Normal,
            };
            return normalizedName is "Method" or "Getter" or "Setter" or "Constructor" or "AsyncMethod"
                or "EnumeratorMethod";
        }

        private static bool TryGetTargetType(
            IInvocationOperation invocation,
            IMethodSymbol method,
            out ITypeSymbol targetType)
        {
            if (method is { IsGenericMethod: true, TypeArguments.Length: 1 })
            {
                targetType = method.TypeArguments[0];
                return true;
            }

            return TryGetTypeArgument(invocation.Arguments, "targetType", out targetType);
        }

        private static bool TryGetMethodName(
            IInvocationOperation invocation,
            IMethodSymbol method,
            PatchTargetKind kind,
            out string methodName,
            out Location location)
        {
            if (kind == PatchTargetKind.Constructor)
            {
                methodName = ".ctor";
                location = invocation.Syntax.GetLocation();
                return true;
            }

            return TryGetStringArgument(invocation.Arguments, "methodName", out methodName, out location);
        }

        private static bool TryGetTypeArgument(
            ImmutableArray<IArgumentOperation> arguments,
            string parameterName,
            out ITypeSymbol type)
        {
            var argument = FindArgument(arguments, parameterName);
            var operation = Unwrap(argument?.Value);
            if (operation is ITypeOfOperation typeOf)
            {
                type = typeOf.TypeOperand;
                return true;
            }

            type = null!;
            return false;
        }

        private static bool TryGetStringArgument(
            ImmutableArray<IArgumentOperation> arguments,
            string parameterName,
            out string value,
            out Location location)
        {
            var argument = FindArgument(arguments, parameterName);
            var operation = Unwrap(argument?.Value);
            if (operation?.ConstantValue is { HasValue: true, Value: string constant })
            {
                value = constant;
                location = operation.Syntax.GetLocation();
                return true;
            }

            value = string.Empty;
            location = argument?.Syntax.GetLocation() ?? Location.None;
            return false;
        }

        private static bool TryGetBooleanArgument(
            ImmutableArray<IArgumentOperation> arguments,
            string parameterName,
            bool defaultValue,
            out bool value)
        {
            var argument = FindArgument(arguments, parameterName);
            if (argument == null)
            {
                value = defaultValue;
                return true;
            }

            var operation = Unwrap(argument.Value);
            if (operation?.ConstantValue is { HasValue: true, Value: bool constant })
            {
                value = constant;
                return true;
            }

            value = false;
            return false;
        }

        private static bool TryGetParameterTypes(
            ImmutableArray<IArgumentOperation> arguments,
            out ImmutableArray<ITypeSymbol>? parameterTypes)
        {
            var argument = FindArgument(arguments, "parameterTypes");
            if (argument == null)
            {
                parameterTypes = null;
                return true;
            }

            var operation = Unwrap(argument.Value);
            if (operation?.ConstantValue is { HasValue: true, Value: null })
            {
                parameterTypes = null;
                return true;
            }

            ImmutableArray<IOperation> elements;
            switch (operation)
            {
                case IArrayCreationOperation { Initializer: not null } arrayCreation:
                    elements = arrayCreation.Initializer.ElementValues;
                    break;

                case ICollectionExpressionOperation collectionExpression:
                    elements = collectionExpression.Elements;
                    break;

                default:
                    parameterTypes = null;
                    return false;
            }

            var builder = ImmutableArray.CreateBuilder<ITypeSymbol>(elements.Length);
            foreach (var element in elements)
            {
                if (Unwrap(element) is not ITypeOfOperation typeOf)
                {
                    parameterTypes = null;
                    return false;
                }

                builder.Add(typeOf.TypeOperand);
            }

            parameterTypes = builder.MoveToImmutable();
            return true;
        }

        private static bool TryGetMethodType(
            ImmutableArray<IArgumentOperation> arguments,
            out PatchTargetKind kind)
        {
            var argument = FindArgument(arguments, "harmonyMethodType");
            if (argument == null)
            {
                kind = PatchTargetKind.Normal;
                return true;
            }

            var operation = Unwrap(argument.Value);
            if (operation?.ConstantValue is not { HasValue: true } constant
                || operation.Type is not INamedTypeSymbol enumType)
            {
                kind = PatchTargetKind.Normal;
                return false;
            }

            var memberName = enumType.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(field =>
                    field.HasConstantValue && Equals(field.ConstantValue, constant.Value))
                ?.Name;
            kind = memberName switch
            {
                "Getter" => PatchTargetKind.Getter,
                "Setter" => PatchTargetKind.Setter,
                "Constructor" => PatchTargetKind.Constructor,
                "Enumerator" => PatchTargetKind.Enumerator,
                "Async" => PatchTargetKind.Async,
                _ => PatchTargetKind.Normal,
            };
            return memberName != null;
        }

        private static IArgumentOperation? FindArgument(
            ImmutableArray<IArgumentOperation> arguments,
            string parameterName)
        {
            return arguments.FirstOrDefault(argument => argument.Parameter?.Name == parameterName);
        }

        private static IOperation? Unwrap(IOperation? operation)
        {
            while (operation is IConversionOperation conversion)
                operation = conversion.Operand;
            return operation;
        }

        private static TargetMatch ToMatch(int matches)
        {
            return matches switch
            {
                0 => TargetMatch.Missing,
                1 => TargetMatch.Single,
                _ => TargetMatch.Ambiguous,
            };
        }

        private static string FormatTarget(PatchTargetSpec target)
        {
            var owner = target.TargetType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            var parameters = target.ParameterTypes == null
                ? ""
                : $"({string.Join(", ", target.ParameterTypes.Value.Select(type =>
                    type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))})";
            var kind = target.Kind == PatchTargetKind.Normal ? "" : $" [{target.Kind}]";
            return $"{owner}.{target.MethodName}{parameters}{kind}";
        }

        private enum PatchTargetKind
        {
            Normal,
            Getter,
            Setter,
            Constructor,
            Async,
            Enumerator,
        }

        private enum TargetMatch
        {
            Missing,
            Single,
            Ambiguous,
        }

        private readonly struct PatchTargetSpec(
            ITypeSymbol targetType,
            string methodName,
            ImmutableArray<ITypeSymbol>? parameterTypes,
            bool ignoreIfMissing,
            PatchTargetKind kind,
            Location location)
        {
            public ITypeSymbol TargetType { get; } = targetType;

            public string MethodName { get; } = methodName;

            public ImmutableArray<ITypeSymbol>? ParameterTypes { get; } = parameterTypes;

            public bool IgnoreIfMissing { get; } = ignoreIfMissing;

            public PatchTargetKind Kind { get; } = kind;

            public Location Location { get; } = location;
        }
    }
}
