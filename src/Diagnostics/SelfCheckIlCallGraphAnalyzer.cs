using System.Reflection;
using System.Reflection.Emit;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Diagnostics
{
    internal static class SelfCheckIlCallGraphAnalyzer
    {
        private static readonly OpCode[] SingleByteOpCodes = new OpCode[0x100];
        private static readonly OpCode[] MultiByteOpCodes = new OpCode[0x100];

        static SelfCheckIlCallGraphAnalyzer()
        {
            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null) is not OpCode opcode)
                    continue;
                var value = (ushort)opcode.Value;
                if (value < 0x100)
                    SingleByteOpCodes[value] = opcode;
                else if ((value & 0xff00) == 0xfe00)
                    MultiByteOpCodes[value & 0xff] = opcode;
            }
        }

        internal static IReadOnlyList<string> BuildReportLines()
        {
            var lines = new List<string>();
            var targets = GetTargets();
            var callerMap = BuildCallerMap(targets);

            lines.Add("IL Static Call Graph (self-check points):");
            foreach (var target in targets)
            {
                if (target.Method == null)
                {
                    lines.Add($"- {target.DisplayName}: target_method_not_found");
                    continue;
                }

                if (!callerMap.TryGetValue(target.DisplayName, out var callers))
                {
                    lines.Add($"- {target.DisplayName}: callers=0");
                    continue;
                }

                lines.Add($"- {target.DisplayName}: callers={callers.Count}");
                lines.AddRange(callers.Select(caller => $"  - {caller}"));
            }

            return lines;
        }

        private static Dictionary<string, List<string>> BuildCallerMap(IReadOnlyList<IlTarget> targets)
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var availableTargets = targets.Where(t => t.Method != null).ToArray();

            foreach (var caller in EnumerateCandidateMethods())
            foreach (var calledMethod in EnumerateCalledMethods(caller))
            foreach (var target in availableTargets)
            {
                if (target.Method == null || !MethodMatches(calledMethod, target.Method))
                    continue;
                var callerName = FormatMethod(caller);
                if (!map.TryGetValue(target.DisplayName, out var set))
                {
                    set = new(StringComparer.Ordinal);
                    map[target.DisplayName] = set;
                }

                set.Add(callerName);
            }

            return map.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                StringComparer.Ordinal);
        }

        private static IReadOnlyList<IlTarget> GetTargets()
        {
            const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            return
            [
                new("CharacterModel.get_VisualsPath",
                    typeof(CharacterModel).GetMethod("get_VisualsPath", instanceFlags)),
                new("CharacterModel.get_EnergyCounterPath",
                    typeof(CharacterModel).GetMethod("get_EnergyCounterPath", instanceFlags)),
                new("CharacterModel.get_IconTexturePath",
                    typeof(CharacterModel).GetMethod("get_IconTexturePath", instanceFlags)),
                new("CharacterModel.get_IconOutlineTexturePath",
                    typeof(CharacterModel).GetMethod("get_IconOutlineTexturePath", instanceFlags)),
                new("CharacterModel.get_CharacterSelectBg",
                    typeof(CharacterModel).GetMethod("get_CharacterSelectBg", instanceFlags)),
                new("CharacterModel.get_CharacterSelectTransitionPath",
                    typeof(CharacterModel).GetMethod("get_CharacterSelectTransitionPath", instanceFlags)),
                new("CharacterModel.get_TrailPath", typeof(CharacterModel).GetMethod("get_TrailPath", instanceFlags)),
                new("CharacterModel.get_AttackSfx", typeof(CharacterModel).GetMethod("get_AttackSfx", instanceFlags)),
                new("CharacterModel.get_CastSfx", typeof(CharacterModel).GetMethod("get_CastSfx", instanceFlags)),
                new("CharacterModel.get_DeathSfx", typeof(CharacterModel).GetMethod("get_DeathSfx", instanceFlags)),
                new("ResourceLoader.Exists(string)",
                    typeof(ResourceLoader).GetMethod("Exists", staticFlags, [typeof(string)])),
                new("LocString.Exists(string,string)",
                    typeof(LocString).GetMethod("Exists", staticFlags, [typeof(string), typeof(string)])),
                new("ModelDb.GetById<T>(ModelId)", FindModelDbGetByIdGeneric()),
            ];
        }

        private static MethodInfo? FindModelDbGetByIdGeneric()
        {
            return typeof(ModelDb).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m is { Name: "GetById", IsGenericMethodDefinition: true } &&
                                     m.GetParameters() is { Length: 1 } p &&
                                     p[0].ParameterType == typeof(ModelId));
        }

        private static IEnumerable<MethodBase> EnumerateCandidateMethods()
        {
            const BindingFlags methodFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                             BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            const BindingFlags ctorFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                           BindingFlags.Instance | BindingFlags.Static;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || !IsCandidateAssembly(assembly))
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    MethodBase[] methods;
                    try
                    {
                        methods = type.GetMethods(methodFlags).Cast<MethodBase>()
                            .Concat(type.GetConstructors(ctorFlags))
                            .ToArray();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var method in methods)
                    {
                        MethodBody? body;
                        try
                        {
                            body = method.GetMethodBody();
                        }
                        catch
                        {
                            continue;
                        }

                        if (body?.GetILAsByteArray() is { Length: > 0 })
                            yield return method;
                    }
                }
            }
        }

        private static bool IsCandidateAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name ?? string.Empty;
            return !name.StartsWith("System", StringComparison.Ordinal) &&
                   !name.StartsWith("Microsoft", StringComparison.Ordinal) &&
                   !name.StartsWith("netstandard", StringComparison.Ordinal) &&
                   !name.StartsWith("mscorlib", StringComparison.Ordinal) &&
                   !name.StartsWith("Mono.", StringComparison.Ordinal) &&
                   !name.StartsWith("Godot", StringComparison.Ordinal) &&
                   !name.StartsWith("HarmonyLib", StringComparison.Ordinal);
        }

        private static IEnumerable<MethodBase> EnumerateCalledMethods(MethodBase caller)
        {
            MethodBody? body;
            try
            {
                body = caller.GetMethodBody();
            }
            catch
            {
                yield break;
            }

            var il = body?.GetILAsByteArray();
            if (il == null || il.Length == 0)
                yield break;

            var module = caller.Module;
            var typeArgs = caller.DeclaringType?.GetGenericArguments() ?? Type.EmptyTypes;
            var methodArgs = caller.IsGenericMethod ? caller.GetGenericArguments() : Type.EmptyTypes;
            var pos = 0;
            while (pos < il.Length)
            {
                var opcode = ReadOpCode(il, ref pos);
                if (opcode == null)
                    yield break;

                if (opcode.Value.OperandType == OperandType.InlineMethod)
                {
                    if (pos + 4 > il.Length)
                        yield break;
                    var token = BitConverter.ToInt32(il, pos);
                    MethodBase? resolved = null;
                    try
                    {
                        resolved = module.ResolveMethod(token, typeArgs, methodArgs);
                    }
                    catch
                    {
                        // ignored
                    }

                    if (resolved != null)
                        yield return resolved;

                    pos += 4;
                    continue;
                }

                pos += GetOperandLength(opcode.Value.OperandType, il, pos);
            }
        }

        private static OpCode? ReadOpCode(byte[] il, ref int pos)
        {
            if (pos >= il.Length)
                return null;
            var first = il[pos++];
            if (first != 0xfe)
                return SingleByteOpCodes[first];
            if (pos >= il.Length)
                return null;
            var second = il[pos++];
            return MultiByteOpCodes[second];
        }

        private static int GetOperandLength(OperandType operandType, byte[] il, int pos)
        {
            return operandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget => 1,
                OperandType.ShortInlineI => 1,
                OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI => 4,
                OperandType.InlineBrTarget => 4,
                OperandType.InlineField => 4,
                OperandType.InlineMethod => 4,
                OperandType.InlineSig => 4,
                OperandType.InlineString => 4,
                OperandType.InlineTok => 4,
                OperandType.InlineType => 4,
                OperandType.ShortInlineR => 4,
                OperandType.InlineI8 => 8,
                OperandType.InlineR => 8,
                OperandType.InlineSwitch => GetSwitchLength(il, pos),
                _ => 0,
            };
        }

        private static int GetSwitchLength(byte[] il, int pos)
        {
            if (pos + 4 > il.Length)
                return 0;
            var count = BitConverter.ToInt32(il, pos);
            if (count < 0)
                return 0;
            return 4 + count * 4;
        }

        private static bool MethodMatches(MethodBase candidate, MethodBase target)
        {
            if (ReferenceEquals(candidate, target))
                return true;
            return candidate.Module == target.Module && candidate.MetadataToken == target.MetadataToken;
        }

        private static string FormatMethod(MethodBase method)
        {
            var typeName = method.DeclaringType?.FullName ?? "<global>";
            return $"{typeName}.{method.Name}";
        }

        private readonly record struct IlTarget(string DisplayName, MethodBase? Method);
    }
}
