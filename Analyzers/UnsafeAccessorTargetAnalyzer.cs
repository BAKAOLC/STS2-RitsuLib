using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace STS2.RitsuLib.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnsafeAccessorTargetAnalyzer : DiagnosticAnalyzer
    {
        private const string UnsafeAccessorAttributeName =
            "System.Runtime.CompilerServices.UnsafeAccessorAttribute";

        private const int Constructor = 0;
        private const int Method = 1;
        private const int StaticMethod = 2;
        private const int Field = 3;
        private const int StaticField = 4;

        private static readonly DiagnosticDescriptor InvalidShape = new(
            "RLUA001",
            "Invalid UnsafeAccessor declaration",
            "{0}",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor TargetMismatch = new(
            "RLUA002",
            "UnsafeAccessor target does not match",
            "{0}",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            [InvalidShape, TargetMismatch];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(static compilationContext =>
            {
                var unsafeAccessorAttribute =
                    compilationContext.Compilation.GetTypeByMetadataName(UnsafeAccessorAttributeName);
                if (unsafeAccessorAttribute == null)
                    return;

                var metadataResolver = new MetadataResolver(compilationContext.Compilation);
                compilationContext.RegisterSymbolAction(
                    symbolContext => AnalyzeMethod(symbolContext, unsafeAccessorAttribute, metadataResolver),
                    SymbolKind.Method);
            });
        }

        private static void AnalyzeMethod(
            SymbolAnalysisContext context,
            INamedTypeSymbol unsafeAccessorAttribute,
            MetadataResolver metadataResolver)
        {
            var method = (IMethodSymbol)context.Symbol;
            var attribute = method.GetAttributes()
                .FirstOrDefault(candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.AttributeClass,
                    unsafeAccessorAttribute));
            if (attribute == null)
                return;

            var location = method.Locations.FirstOrDefault();
            if (!method.IsStatic || !method.IsExtern)
            {
                Report(context, InvalidShape, location, "UnsafeAccessor methods must be static extern methods.");
                return;
            }

            if (!TryGetAccessorKind(attribute, out var kind))
            {
                Report(context, InvalidShape, location, "UnsafeAccessor kind could not be read.");
                return;
            }

            var name = GetAccessorName(attribute) ?? method.Name;
            switch (kind)
            {
                case Field:
                case StaticField:
                    AnalyzeFieldAccessor(context, metadataResolver, method, kind == StaticField, name, location);
                    break;

                case Method:
                case StaticMethod:
                    AnalyzeMethodAccessor(context, metadataResolver, method, kind == StaticMethod, name, location);
                    break;

                case Constructor:
                    AnalyzeConstructorAccessor(context, metadataResolver, method, location);
                    break;

                default:
                    Report(context, InvalidShape, location, $"Unsupported UnsafeAccessor kind value '{kind}'.");
                    break;
            }
        }

        private static void AnalyzeFieldAccessor(
            SymbolAnalysisContext context,
            MetadataResolver metadataResolver,
            IMethodSymbol accessor,
            bool isStatic,
            string name,
            Location? location)
        {
            if (!TryGetOwnerType(context, accessor, true, isStatic, location, out var owner))
                return;

            if (!accessor.ReturnsByRef && !accessor.ReturnsByRefReadonly)
            {
                Report(context, InvalidShape, location, "UnsafeAccessor field accessors must return by ref.");
                return;
            }

            var expectedFieldType = SignatureKey.FromType(accessor.ReturnType);
            if (!metadataResolver.TryFindField(owner, name, isStatic, expectedFieldType, out var result))
                Report(context, TargetMismatch, location, result);
        }

        private static void AnalyzeMethodAccessor(
            SymbolAnalysisContext context,
            MetadataResolver metadataResolver,
            IMethodSymbol accessor,
            bool isStatic,
            string name,
            Location? location)
        {
            if (!TryGetOwnerType(context, accessor, true, isStatic, location, out var owner))
                return;

            var signature = AccessorMethodSignature.FromMethod(accessor, true);
            if (!metadataResolver.TryFindMethod(owner, name, isStatic, signature, out var result))
                Report(context, TargetMismatch, location, result);
        }

        private static void AnalyzeConstructorAccessor(
            SymbolAnalysisContext context,
            MetadataResolver metadataResolver,
            IMethodSymbol accessor,
            Location? location)
        {
            if (accessor.ReturnsVoid)
            {
                Report(context, InvalidShape, location,
                    "UnsafeAccessor constructor accessors must return the constructed type.");
                return;
            }

            var owner = accessor.ReturnType;
            var signature = AccessorMethodSignature.FromMethod(accessor, false);
            if (!metadataResolver.TryFindConstructor(owner, signature, out var result))
                Report(context, TargetMismatch, location, result);
        }

        private static bool TryGetOwnerType(
            SymbolAnalysisContext context,
            IMethodSymbol accessor,
            bool requiresReceiver,
            bool isStatic,
            Location? location,
            out ITypeSymbol owner)
        {
            owner = accessor.Parameters.FirstOrDefault()?.Type!;
            if (!requiresReceiver || accessor.Parameters.Length > 0)
            {
                if (isStatic
                    || owner.TypeKind != TypeKind.Struct
                    || accessor.Parameters[0].RefKind == RefKind.Ref) return true;
                Report(context, InvalidShape, location,
                    "UnsafeAccessor instance accessors for struct owners must pass the receiver by ref.");
                return false;
            }

            Report(context, InvalidShape, location,
                "UnsafeAccessor method and field accessors must have a first parameter that identifies the owning type.");
            return false;
        }

        private static bool TryGetAccessorKind(AttributeData attribute, out int kind)
        {
            kind = 0;
            if (attribute.ConstructorArguments.Length != 1)
                return false;

            if (attribute.ConstructorArguments[0].Value is not int value) return false;
            kind = value;
            return true;
        }

        private static string? GetAccessorName(AttributeData attribute)
        {
            return (from argument in attribute.NamedArguments
                where argument.Key == "Name"
                select argument.Value.Value as string).FirstOrDefault();
        }

        private static void Report(
            SymbolAnalysisContext context,
            DiagnosticDescriptor descriptor,
            Location? location,
            string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, message));
        }

        private sealed class MetadataResolver
        {
            private readonly ImmutableDictionary<string, MetadataAssemblyIndex> _assemblies;
            private readonly Compilation _compilation;

            public MetadataResolver(Compilation compilation)
            {
                _compilation = compilation;
                var builder = ImmutableDictionary.CreateBuilder<string, MetadataAssemblyIndex>(StringComparer.Ordinal);
                foreach (var reference in compilation.References.OfType<PortableExecutableReference>())
                {
                    var filePath = reference.FilePath;
                    if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly
                        || string.IsNullOrEmpty(filePath)
                        || !File.Exists(filePath))
                        continue;

                    var assemblyName = assembly.Identity.Name;
                    builder[assemblyName] = new(filePath!, assemblyName);
                }

                _assemblies = builder.ToImmutable();
            }

            public bool TryFindField(
                ITypeSymbol owner,
                string name,
                bool isStatic,
                string expectedType,
                out string result)
            {
                if (IsSourceAssembly(owner))
                    return TryFindSourceField(owner, name, isStatic, expectedType, out result);

                return TryGetReferencedType(owner, out var assembly, out var type, out result) &&
                       assembly.TryFindField(type, name, isStatic, expectedType, out result);
            }

            public bool TryFindMethod(
                ITypeSymbol owner,
                string name,
                bool isStatic,
                AccessorMethodSignature signature,
                out string result)
            {
                if (IsSourceAssembly(owner))
                    return TryFindSourceMethod(owner, name, isStatic, signature, out result);

                return TryGetReferencedType(owner, out var assembly, out var type, out result) &&
                       assembly.TryFindMethod(type, name, isStatic, signature, out result);
            }

            public bool TryFindConstructor(
                ITypeSymbol owner,
                AccessorMethodSignature signature,
                out string result)
            {
                if (IsSourceAssembly(owner))
                    return TryFindSourceConstructor(owner, signature, out result);

                return TryGetReferencedType(owner, out var assembly, out var type, out result) &&
                       assembly.TryFindMethod(type, ".ctor", false, signature, out result);
            }

            private bool TryGetReferencedType(
                ITypeSymbol owner,
                out MetadataAssemblyIndex assembly,
                out TypeDefinitionHandle type,
                out string result)
            {
                assembly = null!;
                type = default;
                var assemblyName = owner.ContainingAssembly.Identity.Name ?? string.Empty;
                if (!_assemblies.TryGetValue(assemblyName, out var metadataAssembly))
                {
                    result = $"Could not inspect metadata for assembly '{assemblyName}'.";
                    return false;
                }

                assembly = metadataAssembly;
                var ownerKey = SignatureKey.FromDefinition(owner);
                if (!assembly.TryFindType(ownerKey, out type))
                {
                    result = $"UnsafeAccessor owner type '{ownerKey}' was not found in '{assembly.FilePath}'.";
                    return false;
                }

                result = string.Empty;
                return true;
            }

            private bool IsSourceAssembly(ITypeSymbol owner)
            {
                return SymbolEqualityComparer.Default.Equals(owner.ContainingAssembly, _compilation.Assembly);
            }

            private static bool TryFindSourceField(
                ITypeSymbol owner,
                string name,
                bool isStatic,
                string expectedType,
                out string result)
            {
                var matches = owner.GetMembers(name)
                    .OfType<IFieldSymbol>()
                    .Where(field => field.IsStatic == isStatic)
                    .Where(field => SignatureKey.FromType(field.Type) == expectedType)
                    .Take(2)
                    .ToArray();

                return MatchCount(matches.Length, owner, name, out result);
            }

            private static bool TryFindSourceMethod(
                ITypeSymbol owner,
                string name,
                bool isStatic,
                AccessorMethodSignature signature,
                out string result)
            {
                var matches = owner.GetMembers(name)
                    .OfType<IMethodSymbol>()
                    .Where(method => method.MethodKind == MethodKind.Ordinary)
                    .Where(method => method.IsStatic == isStatic)
                    .Where(method => AccessorMethodSignature.FromTargetMethod(method).Equals(signature))
                    .Take(2)
                    .ToArray();

                return MatchCount(matches.Length, owner, name, out result);
            }

            private static bool TryFindSourceConstructor(
                ITypeSymbol owner,
                AccessorMethodSignature signature,
                out string result)
            {
                var matches = owner.GetMembers(".ctor")
                    .OfType<IMethodSymbol>()
                    .Concat(owner is INamedTypeSymbol namedType ? namedType.InstanceConstructors : [])
                    .Where(method => AccessorMethodSignature.FromTargetMethod(method).Equals(signature))
                    .Take(2)
                    .ToArray();

                return MatchCount(matches.Length, owner, ".ctor", out result);
            }

            private static bool MatchCount(int count, ITypeSymbol owner, string name, out string result)
            {
                if (count == 1)
                {
                    result = string.Empty;
                    return true;
                }

                result = count == 0
                    ? $"UnsafeAccessor target '{name}' was not found on exact owner type '{owner.ToDisplayString()}'."
                    : $"UnsafeAccessor target '{name}' is ambiguous on exact owner type '{owner.ToDisplayString()}'.";
                return false;
            }
        }

        private sealed class MetadataAssemblyIndex(string filePath, string assemblyName)
        {
            private readonly Lazy<IndexData> _index = new(() => BuildIndex(filePath, assemblyName));

            public string FilePath { get; } = filePath;

            public bool TryFindType(string typeKey, out TypeDefinitionHandle type)
            {
                return _index.Value.Types.TryGetValue(typeKey, out type);
            }

            public bool TryFindField(
                TypeDefinitionHandle owner,
                string name,
                bool isStatic,
                string expectedType,
                out string result)
            {
                var data = _index.Value;
                var reader = data.Reader;
                var typeDefinition = reader.GetTypeDefinition(owner);
                var matches = 0;
                foreach (var unused in from fieldHandle in typeDefinition.GetFields()
                         select reader.GetFieldDefinition(fieldHandle)
                         into field
                         where StringEquals(reader, field.Name, name)
                               && field.Attributes.HasFlag(FieldAttributes.Static) == isStatic
                         select field.DecodeSignature(data.SignatureProvider, null)
                         into actualType
                         where actualType == expectedType
                         select actualType)
                {
                    matches++;
                    if (matches > 1)
                        break;
                }

                return MatchCount(matches, data.GetTypeName(owner), name, out result);
            }

            public bool TryFindMethod(
                TypeDefinitionHandle owner,
                string name,
                bool isStatic,
                AccessorMethodSignature expectedSignature,
                out string result)
            {
                var data = _index.Value;
                var reader = data.Reader;
                var typeDefinition = reader.GetTypeDefinition(owner);
                var matches = 0;
                foreach (var unused in from methodHandle in typeDefinition.GetMethods()
                         select reader.GetMethodDefinition(methodHandle)
                         into method
                         where StringEquals(reader, method.Name, name)
                               && method.Attributes.HasFlag(MethodAttributes.Static) == isStatic
                         select AccessorMethodSignature.FromMetadata(method, data.SignatureProvider)
                         into actualSignature
                         where actualSignature.Equals(expectedSignature)
                         select actualSignature)
                {
                    matches++;
                    if (matches > 1)
                        break;
                }

                return MatchCount(matches, data.GetTypeName(owner), name, out result);
            }

            private static bool MatchCount(int count, string ownerName, string name, out string result)
            {
                if (count == 1)
                {
                    result = string.Empty;
                    return true;
                }

                result = count == 0
                    ? $"UnsafeAccessor target '{name}' was not found on exact owner type '{ownerName}'."
                    : $"UnsafeAccessor target '{name}' is ambiguous on exact owner type '{ownerName}'.";
                return false;
            }

            private static IndexData BuildIndex(string filePath, string assemblyName)
            {
                var stream = File.OpenRead(filePath);
                var peReader = new PEReader(stream);
                var reader = peReader.GetMetadataReader();
                var provider = new MetadataSignatureProvider(assemblyName);
                var types = ImmutableDictionary.CreateBuilder<string, TypeDefinitionHandle>(StringComparer.Ordinal);
                var names = ImmutableDictionary.CreateBuilder<TypeDefinitionHandle, string>();
                foreach (var typeHandle in reader.TypeDefinitions)
                {
                    var key = provider.GetTypeDefinitionKey(reader, typeHandle);
                    types[key] = typeHandle;
                    names.Add(typeHandle, key);
                }

                return new(stream, peReader, reader, provider, types.ToImmutable(), names.ToImmutable());
            }

            private static bool StringEquals(MetadataReader reader, StringHandle handle, string value)
            {
                return string.Equals(reader.GetString(handle), value, StringComparison.Ordinal);
            }
        }

        private sealed class IndexData(
            Stream stream,
            PEReader peReader,
            MetadataReader reader,
            MetadataSignatureProvider signatureProvider,
            ImmutableDictionary<string, TypeDefinitionHandle> types,
            ImmutableDictionary<TypeDefinitionHandle, string> names)
        {
            private readonly PEReader _peReader = peReader;
            private readonly Stream _stream = stream;

            public MetadataReader Reader { get; } = reader;

            public MetadataSignatureProvider SignatureProvider { get; } = signatureProvider;

            public ImmutableDictionary<string, TypeDefinitionHandle> Types { get; } = types;

            public string GetTypeName(TypeDefinitionHandle handle)
            {
                return names.TryGetValue(handle, out var name) ? name : "<unknown>";
            }
        }

        private sealed class MetadataSignatureProvider(string assemblyName) : ISignatureTypeProvider<string, object?>
        {
            public string GetArrayType(string elementType, ArrayShape shape)
            {
                return elementType + "[" + new string(',', shape.Rank - 1) + "]";
            }

            public string GetByReferenceType(string elementType)
            {
                return elementType + "&";
            }

            public string GetFunctionPointerType(MethodSignature<string> signature)
            {
                return "fnptr(" + signature.ReturnType + "(" + string.Join(",", signature.ParameterTypes) + "))";
            }

            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
            {
                return genericType + "<" + string.Join(",", typeArguments) + ">";
            }

            public string GetGenericMethodParameter(object? genericContext, int index)
            {
                return "!!" + index;
            }

            public string GetGenericTypeParameter(object? genericContext, int index)
            {
                return "!" + index;
            }

            public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
            {
                return unmodifiedType;
            }

            public string GetPinnedType(string elementType)
            {
                return elementType;
            }

            public string GetPointerType(string elementType)
            {
                return elementType + "*";
            }

            public string GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                return typeCode switch
                {
                    PrimitiveTypeCode.Void => "void",
                    PrimitiveTypeCode.Boolean => "bool",
                    PrimitiveTypeCode.Char => "char",
                    PrimitiveTypeCode.SByte => "sbyte",
                    PrimitiveTypeCode.Byte => "byte",
                    PrimitiveTypeCode.Int16 => "short",
                    PrimitiveTypeCode.UInt16 => "ushort",
                    PrimitiveTypeCode.Int32 => "int",
                    PrimitiveTypeCode.UInt32 => "uint",
                    PrimitiveTypeCode.Int64 => "long",
                    PrimitiveTypeCode.UInt64 => "ulong",
                    PrimitiveTypeCode.Single => "float",
                    PrimitiveTypeCode.Double => "double",
                    PrimitiveTypeCode.String => "System.Private.CoreLib:System.String",
                    PrimitiveTypeCode.IntPtr => "System.Private.CoreLib:System.IntPtr",
                    PrimitiveTypeCode.UIntPtr => "System.Private.CoreLib:System.UIntPtr",
                    PrimitiveTypeCode.Object => "System.Private.CoreLib:System.Object",
                    PrimitiveTypeCode.TypedReference => "System.Private.CoreLib:System.TypedReference",
                    _ => typeCode.ToString(),
                };
            }

            public string GetSZArrayType(string elementType)
            {
                return elementType + "[]";
            }

            public string GetTypeFromDefinition(
                MetadataReader reader,
                TypeDefinitionHandle handle,
                byte rawTypeKind)
            {
                return GetTypeDefinitionKey(reader, handle);
            }

            public string GetTypeFromReference(
                MetadataReader reader,
                TypeReferenceHandle handle,
                byte rawTypeKind)
            {
                var type = reader.GetTypeReference(handle);
                var name = reader.GetString(type.Name);
                var @namespace = reader.GetString(type.Namespace);
                var scope = GetResolutionScopeKey(reader, type.ResolutionScope);
                return scope + ":" + (string.IsNullOrEmpty(@namespace) ? name : @namespace + "." + name);
            }

            public string GetTypeFromSpecification(
                MetadataReader reader,
                object? genericContext,
                TypeSpecificationHandle handle,
                byte rawTypeKind)
            {
                return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
            }

            public string GetTypeDefinitionKey(MetadataReader reader, TypeDefinitionHandle handle)
            {
                var type = reader.GetTypeDefinition(handle);
                var name = reader.GetString(type.Name);
                if (!type.GetDeclaringType().IsNil)
                    return GetTypeDefinitionKey(reader, type.GetDeclaringType()) + "+" + name;
                var @namespace = reader.GetString(type.Namespace);
                return assemblyName + ":" + (string.IsNullOrEmpty(@namespace) ? name : @namespace + "." + name);
            }

            private string GetResolutionScopeKey(MetadataReader reader, EntityHandle scope)
            {
                return scope.Kind switch
                {
                    HandleKind.AssemblyReference => reader.GetString(reader
                        .GetAssemblyReference((AssemblyReferenceHandle)scope).Name),
                    HandleKind.TypeReference => GetTypeFromReference(reader, (TypeReferenceHandle)scope, 0),
                    _ => assemblyName,
                };
            }
        }

        private readonly struct AccessorMethodSignature : IEquatable<AccessorMethodSignature>
        {
            private readonly string _returnType;
            private readonly ImmutableArray<string> _parameterTypes;

            private AccessorMethodSignature(string returnType, ImmutableArray<string> parameterTypes)
            {
                _returnType = returnType;
                _parameterTypes = parameterTypes;
            }

            public static AccessorMethodSignature FromMethod(IMethodSymbol method, bool skipReceiver)
            {
                var parameterTypes = method.Parameters
                    .Skip(skipReceiver ? 1 : 0)
                    .Select(SignatureKey.FromParameter)
                    .ToImmutableArray();

                return new(GetReturnType(method), parameterTypes);
            }

            public static AccessorMethodSignature FromTargetMethod(IMethodSymbol method)
            {
                return new(
                    GetReturnType(method),
                    [..method.Parameters.Select(SignatureKey.FromParameter)]);
            }

            public static AccessorMethodSignature FromMetadata(
                MethodDefinition method,
                MetadataSignatureProvider provider)
            {
                var signature = method.DecodeSignature(provider, null);
                return new(signature.ReturnType, signature.ParameterTypes);
            }

            public bool Equals(AccessorMethodSignature other)
            {
                return _returnType == other._returnType && _parameterTypes.SequenceEqual(other._parameterTypes);
            }

            public override bool Equals(object? obj)
            {
                return obj is AccessorMethodSignature other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = StringComparer.Ordinal.GetHashCode(_returnType);

                return Enumerable.Aggregate(_parameterTypes, hash,
                    (current, parameterType) =>
                        unchecked((current * 397) ^ StringComparer.Ordinal.GetHashCode(parameterType)));
            }

            private static string GetReturnType(IMethodSymbol method)
            {
                if (method.ReturnsVoid)
                    return "void";

                var key = SignatureKey.FromType(method.ReturnType);
                return method.ReturnsByRef || method.ReturnsByRefReadonly ? key + "&" : key;
            }
        }

        private static class SignatureKey
        {
            public static string FromParameter(IParameterSymbol parameter)
            {
                var key = FromType(parameter.Type);
                return parameter.RefKind is RefKind.Ref or RefKind.Out or RefKind.In ? key + "&" : key;
            }

            public static string FromType(ITypeSymbol type)
            {
                switch (type)
                {
                    case IArrayTypeSymbol array:
                        return FromType(array.ElementType) + ArraySuffix(array.Rank);
                    case IPointerTypeSymbol pointer:
                        return FromType(pointer.PointedAtType) + "*";
                    case ITypeParameterSymbol typeParameter:
                        return typeParameter.TypeParameterKind == TypeParameterKind.Method
                            ? "!!" + typeParameter.Ordinal
                            : "!" + typeParameter.Ordinal;
                }

                if (type is not INamedTypeSymbol namedType)
                    return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var definition = FromDefinition(namedType);
                if (!namedType.IsGenericType || namedType.TypeArguments.Length == 0)
                    return definition;

                return definition + "<" + string.Join(",", namedType.TypeArguments.Select(FromType)) + ">";
            }

            public static string FromDefinition(ITypeSymbol type)
            {
                if (type.SpecialType != SpecialType.None)
                    return SpecialTypeKey(type.SpecialType);

                var assemblyName = type.ContainingAssembly.Identity.Name;
                return assemblyName + ":" + MetadataTypeName(type);
            }

            private static string MetadataTypeName(ITypeSymbol type)
            {
                if (type.ContainingType != null)
                    return MetadataTypeName(type.ContainingType) + "+" + type.MetadataName;

                var namespaceName = type.ContainingNamespace?.IsGlobalNamespace == false
                    ? type.ContainingNamespace.ToDisplayString()
                    : string.Empty;
                return string.IsNullOrEmpty(namespaceName)
                    ? type.MetadataName
                    : namespaceName + "." + type.MetadataName;
            }

            private static string SpecialTypeKey(SpecialType specialType)
            {
                return specialType switch
                {
                    SpecialType.System_Void => "void",
                    SpecialType.System_Boolean => "bool",
                    SpecialType.System_Char => "char",
                    SpecialType.System_SByte => "sbyte",
                    SpecialType.System_Byte => "byte",
                    SpecialType.System_Int16 => "short",
                    SpecialType.System_UInt16 => "ushort",
                    SpecialType.System_Int32 => "int",
                    SpecialType.System_UInt32 => "uint",
                    SpecialType.System_Int64 => "long",
                    SpecialType.System_UInt64 => "ulong",
                    SpecialType.System_Single => "float",
                    SpecialType.System_Double => "double",
                    SpecialType.System_String => "System.Private.CoreLib:System.String",
                    SpecialType.System_IntPtr => "System.Private.CoreLib:System.IntPtr",
                    SpecialType.System_UIntPtr => "System.Private.CoreLib:System.UIntPtr",
                    SpecialType.System_Object => "System.Private.CoreLib:System.Object",
                    _ => specialType.ToString(),
                };
            }

            private static string ArraySuffix(int rank)
            {
                return rank == 1 ? "[]" : "[" + new string(',', rank - 1) + "]";
            }
        }
    }
}
