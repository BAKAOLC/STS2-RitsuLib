using System.Reflection;
using System.Reflection.Emit;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Content
{
    internal static class PlaceholderModelTypeEmitter
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModuleBuilder>
            ModulesByModId = new(StringComparer.OrdinalIgnoreCase);

        private static int _typeSerial;

        internal static Type EmitCardType(string modId, in PlaceholderCardDescriptor d)
        {
            var tb = DefineType(modId, "Card", typeof(ModPlaceholderCardTemplate));
            var baseCtor = RequireCtor(typeof(ModPlaceholderCardTemplate),
                typeof(int), typeof(CardType), typeof(CardRarity), typeof(TargetType), typeof(bool));
            var il = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes)
                .GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, d.BaseCost);
            il.Emit(OpCodes.Ldc_I4, (int)d.Type);
            il.Emit(OpCodes.Ldc_I4, (int)d.Rarity);
            il.Emit(OpCodes.Ldc_I4, (int)d.Target);
            il.Emit(d.ShowInCardLibrary ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, baseCtor);
            il.Emit(OpCodes.Ret);
            return tb.CreateType();
        }

        internal static Type EmitRelicType(string modId, in PlaceholderRelicDescriptor d)
        {
            var tb = DefineType(modId, "Relic", typeof(ModPlaceholderRelicTemplate));
            var baseCtor = RequireCtor(typeof(ModPlaceholderRelicTemplate),
                typeof(RelicRarity),
                typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                typeof(int), typeof(bool), typeof(int), typeof(bool), typeof(string), typeof(bool));
            var il = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes)
                .GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, (int)d.Rarity);
            EmitBool(il, d.IsUsedUp);
            EmitBool(il, d.HasUponPickupEffect);
            EmitBool(il, d.SpawnsPets);
            EmitBool(il, d.IsStackable);
            EmitBool(il, d.AddsPet);
            EmitBool(il, d.ShowCounter);
            il.Emit(OpCodes.Ldc_I4, d.DisplayAmount);
            EmitBool(il, d.IncludeEnergyHoverTip);
            il.Emit(OpCodes.Ldc_I4, d.MerchantCostOverride);
            EmitBool(il, d.AlwaysAllowedInRun);
            il.Emit(OpCodes.Ldstr, d.FlashSfx);
            EmitBool(il, d.ShouldFlashOnPlayer);
            il.Emit(OpCodes.Call, baseCtor);
            il.Emit(OpCodes.Ret);
            return tb.CreateType();
        }

        internal static Type EmitPotionType(string modId, in PlaceholderPotionDescriptor d)
        {
            var tb = DefineType(modId, "Potion", typeof(ModPlaceholderPotionTemplate));
            var baseCtor = RequireCtor(typeof(ModPlaceholderPotionTemplate),
                typeof(PotionRarity), typeof(PotionUsage), typeof(TargetType), typeof(bool), typeof(bool));
            var il = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes)
                .GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, (int)d.Rarity);
            il.Emit(OpCodes.Ldc_I4, (int)d.Usage);
            il.Emit(OpCodes.Ldc_I4, (int)d.Target);
            EmitBool(il, d.CanBeGeneratedInCombat);
            EmitBool(il, d.PassesCustomUsabilityCheck);
            il.Emit(OpCodes.Call, baseCtor);
            il.Emit(OpCodes.Ret);
            return tb.CreateType();
        }

        private static TypeBuilder DefineType(string modId, string kind, Type parent)
        {
            var module = GetOrCreateModule(modId);
            var id = Interlocked.Increment(ref _typeSerial);
            var safeMod = SanitizeIdentifier(modId);
            return module.DefineType(
                $"STS2RitsuLib.Emit.{safeMod}.{kind}_{id}",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
                parent);
        }

        private static ModuleBuilder GetOrCreateModule(string modId)
        {
            lock (SyncRoot)
            {
                if (ModulesByModId.TryGetValue(modId, out var existing))
                    return existing;

                var name = new AssemblyName(
                    $"STS2RitsuLib.Placeholders.{SanitizeIdentifier(modId)}_{Guid.NewGuid():N}");
                var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
                var module = assembly.DefineDynamicModule("MainModule");
                ModulesByModId[modId] = module;
                return module;
            }
        }

        private static string SanitizeIdentifier(string modId)
        {
            Span<char> buffer = stackalloc char[Math.Min(modId.Length, 48)];
            var n = 0;
            foreach (var c in modId)
            {
                if (n >= buffer.Length)
                    break;
                buffer[n++] = char.IsLetterOrDigit(c) ? c : '_';
            }

            return n == 0 ? "Mod" : new(buffer[..n]);
        }

        private static ConstructorInfo RequireCtor(Type declaring, params Type[] signature)
        {
            var ctor = declaring.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                signature,
                null);
            return ctor ?? throw new InvalidOperationException(
                $"Could not resolve constructor on '{declaring.FullName}' for emit signature.");
        }

        private static void EmitBool(ILGenerator il, bool value)
        {
            il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        }
    }
}
