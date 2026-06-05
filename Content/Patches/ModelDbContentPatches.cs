using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     Appends RitsuLib-registered characters to <see cref="ModelDb.AllCharacters" />.
    ///     将 RitsuLib 注册的角色追加到 <see cref="ModelDb.AllCharacters" />。
    /// </summary>
    public class AllCharactersPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_characters";

        /// <inheritdoc />
        public static string Description => "Append registered characters to ModelDb.AllCharacters";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllCharacters", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod-registered characters onto the vanilla sequence.
        ///     将 mod 注册的角色拼接到原版序列之后。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CharacterModel> __result)
        {
            __result = ModContentRegistry.AppendCharacters(__result);
        }
    }

    /// <summary>
    ///     Merges RitsuLib-registered monster types into <see cref="ModelDb.Monsters" /> by <see cref="AbstractModel.Id" />.
    ///     按 <see cref="AbstractModel.Id" /> 将 RitsuLib 注册的怪物类型合并到 <see cref="ModelDb.Monsters" />。
    /// </summary>
    public class AllMonstersPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_monsters";

        /// <inheritdoc />
        public static string Description => "Merge registered monsters into ModelDb.Monsters";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "Monsters", MethodType.Getter)];
        }

        /// <summary>
        ///     Ensures standalone-registered monsters appear in global monster enumeration even before every act lists them.
        ///     确保独立注册的怪物即使在每个章节列出它们之前，也会出现在全局怪物枚举中。
        /// </summary>
        public static void Postfix(ref IEnumerable<MonsterModel> __result)
        {
            __result = ModContentRegistry.AppendRegisteredMonsters(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered acts to <see cref="ModelDb.Acts" />.
    ///     将 RitsuLib 注册的章节追加到 <see cref="ModelDb.Acts" />。
    /// </summary>
    public class ActsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_acts";

        /// <inheritdoc />
        public static string Description => "Append registered acts to ModelDb.Acts";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "Acts", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod-registered acts onto the vanilla sequence.
        ///     将 mod 注册的章节拼接到原版序列之后。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<ActModel> __result)
        {
            __result = ModContentRegistry.AppendActs(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared events to <see cref="ModelDb.AllSharedEvents" />.
    ///     将 RitsuLib 注册的共享事件追加到 <see cref="ModelDb.AllSharedEvents" />。
    /// </summary>
    public class AllSharedEventsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_shared_events";

        /// <inheritdoc />
        public static string Description => "Append registered shared events to ModelDb.AllSharedEvents";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllSharedEvents", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod shared events onto the vanilla sequence.
        ///     将 mod 共享事件拼接到原版序列之后。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<EventModel> __result)
        {
            __result = ModContentRegistry.AppendSharedEvents(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered powers to <see cref="ModelDb.AllPowers" />.
    ///     将 RitsuLib 注册的能力追加到 <see cref="ModelDb.AllPowers" />。
    /// </summary>
    public class AllPowersPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_powers";

        /// <inheritdoc />
        public static string Description => "Append registered powers to ModelDb.AllPowers";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllPowers", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod powers onto the vanilla sequence.
        ///     将 mod 能力拼接到原版序列之后。
        /// </summary>
        public static void Postfix(ref IEnumerable<PowerModel> __result)
        {
            __result = ModContentRegistry.AppendPowers(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered orbs to <see cref="ModelDb.Orbs" />.
    ///     将 RitsuLib 注册的充能球追加到 <see cref="ModelDb.Orbs" />。
    /// </summary>
    public class AllOrbsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_orbs";

        /// <inheritdoc />
        public static string Description => "Append registered orbs to ModelDb.Orbs";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "Orbs", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod orbs onto the vanilla sequence.
        ///     将 mod 充能球拼接到原版序列之后。
        /// </summary>
        public static void Postfix(ref IEnumerable<OrbModel> __result)
        {
            __result = ModContentRegistry.AppendOrbs(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared card pools to <see cref="ModelDb.AllSharedCardPools" />.
    ///     将 RitsuLib 注册的共享卡牌池追加到 <see cref="ModelDb.AllSharedCardPools" />。
    /// </summary>
    public class AllSharedCardPoolsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_shared_card_pools";

        /// <inheritdoc />
        public static string Description => "Append registered shared card pools to ModelDb.AllSharedCardPools";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllSharedCardPools", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod shared card pools onto the vanilla sequence.
        ///     将 mod 共享卡牌池拼接到原版序列之后。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CardPoolModel> __result)
        {
            __result = ModContentRegistry.AppendSharedCardPools(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared events to <see cref="ModelDb.AllEvents" />.
    ///     将 RitsuLib 注册的共享事件追加到 <see cref="ModelDb.AllEvents" />。
    /// </summary>
    public class AllEventsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_events";

        /// <inheritdoc />
        public static string Description => "Append registered shared events to ModelDb.AllEvents";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllEvents", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod shared events onto the <see cref="ModelDb.AllEvents" /> sequence.
        ///     将 mod 共享事件拼接到 <see cref="ModelDb.AllEvents" /> 序列之后。
        /// </summary>
        public static void Postfix(ref IEnumerable<EventModel> __result)
        {
            __result = ModContentRegistry.AppendSharedEvents(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared ancients to <see cref="ModelDb.AllSharedAncients" />.
    ///     将 RitsuLib 注册的共享ancient追加到 <see cref="ModelDb.AllSharedAncients" />。
    /// </summary>
    public class AllSharedAncientsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_shared_ancients";

        /// <inheritdoc />
        public static string Description => "Append registered shared ancients to ModelDb.AllSharedAncients";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllSharedAncients", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod shared ancients onto the vanilla sequence.
        ///     将 mod 共享ancient拼接到原版序列之后。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            __result = ModContentRegistry.AppendSharedAncients(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared ancients to <see cref="ModelDb.AllAncients" />.
    ///     将 RitsuLib 注册的共享ancient追加到 <see cref="ModelDb.AllAncients" />。
    /// </summary>
    public class AllAncientsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_ancients";

        /// <inheritdoc />
        public static string Description => "Append registered shared ancients to ModelDb.AllAncients";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllAncients", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod shared ancients onto the <see cref="ModelDb.AllAncients" /> sequence.
        ///     将 mod 共享远古事件拼接到 <see cref="ModelDb.AllAncients" /> 序列之后。
        /// </summary>
        public static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            __result = ModContentRegistry.AppendSharedAncients(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered enchantments to <see cref="ModelDb.DebugEnchantments" /> (covers dynamic types not in
    ///     subtype scan).
    ///     将 RitsuLib 注册的附魔追加到 <see cref="ModelDb.DebugEnchantments" />（覆盖子类型扫描之外的动态类型）。
    /// </summary>
    public class DebugEnchantmentsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_debug_enchantments";

        /// <inheritdoc />
        public static string Description => "Append registered enchantments to ModelDb.DebugEnchantments";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "DebugEnchantments", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod enchantments onto the vanilla sequence.
        ///     将 mod 附魔拼接到原版序列之后。
        /// </summary>
        public static void Postfix(ref IEnumerable<EnchantmentModel> __result)
        {
            __result = ModContentRegistry.AppendEnchantments(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered afflictions to <see cref="ModelDb.DebugAfflictions" />.
    ///     将 RitsuLib 注册的苦痛追加到 <see cref="ModelDb.DebugAfflictions" />。
    /// </summary>
    public class DebugAfflictionsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_debug_afflictions";

        /// <inheritdoc />
        public static string Description => "Append registered afflictions to ModelDb.DebugAfflictions";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "DebugAfflictions", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod afflictions onto the vanilla sequence.
        ///     将 mod 苦痛拼接到原版序列之后。
        /// </summary>
        public static void Postfix(ref IEnumerable<AfflictionModel> __result)
        {
            __result = ModContentRegistry.AppendAfflictions(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered achievements to <see cref="ModelDb.Achievements" />.
    ///     将 RitsuLib 注册的成就追加到 <see cref="ModelDb.Achievements" />。
    /// </summary>
    public class AchievementsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_achievements";

        /// <inheritdoc />
        public static string Description => "Append registered achievements to ModelDb.Achievements";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "Achievements", MethodType.Getter)];
        }

        /// <summary>
        ///     Merges mod achievements into the vanilla list by <see cref="AbstractModel.Id" />.
        ///     按 <see cref="AbstractModel.Id" /> 将 mod 成就合并到原版列表中。
        /// </summary>
        public static void Postfix(ref IReadOnlyList<AchievementModel> __result)
        {
            __result = ModContentRegistry.AppendAchievements(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered modifiers to <see cref="ModelDb.GoodModifiers" />.
    ///     将 RitsuLib 注册的修饰符追加到 <see cref="ModelDb.GoodModifiers" />。
    /// </summary>
    public class GoodModifiersPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_good_modifiers";

        /// <inheritdoc />
        public static string Description => "Append registered good modifiers to ModelDb.GoodModifiers";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "GoodModifiers", MethodType.Getter)];
        }

        /// <summary>
        ///     Merges mod good modifiers into the current list using <see cref="ModifierRegistration.ModifierListSortOrder" />.
        ///     按 <see cref="ModifierRegistration.ModifierListSortOrder" /> 将 mod 正面修饰符合并到当前列表中。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            __result = ModContentRegistry.AppendGoodModifiers(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered modifiers to <see cref="ModelDb.BadModifiers" />.
    ///     将 RitsuLib 注册的修饰符追加到 <see cref="ModelDb.BadModifiers" />。
    /// </summary>
    public class BadModifiersPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_bad_modifiers";

        /// <inheritdoc />
        public static string Description => "Append registered bad modifiers to ModelDb.BadModifiers";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "BadModifiers", MethodType.Getter)];
        }

        /// <summary>
        ///     Merges mod bad modifiers into the current list using <see cref="ModifierRegistration.ModifierListSortOrder" />.
        ///     按 <see cref="ModifierRegistration.ModifierListSortOrder" /> 将 mod 负面修饰符合并到当前列表中。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            __result = ModContentRegistry.AppendBadModifiers(__result);
        }
    }

    /// <summary>
    ///     Merges RitsuLib-registered modifier exclusivity groups into <see cref="ModelDb.MutuallyExclusiveModifiers" />.
    ///     将 RitsuLib 注册的修饰符互斥组合并到 <see cref="ModelDb.MutuallyExclusiveModifiers" />。
    /// </summary>
    public class MutuallyExclusiveModifiersPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_mutually_exclusive_modifiers";

        /// <inheritdoc />
        public static string Description =>
            "Merge registered mutually exclusive modifier groups into ModelDb.MutuallyExclusiveModifiers";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "MutuallyExclusiveModifiers", MethodType.Getter)];
        }

        /// <summary>
        ///     Merges mod exclusivity groups into the current list, including overlapping vanilla sets.
        ///     将 mod 互斥组合并到当前列表，并与存在交集的原版集合合并。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<IReadOnlySet<ModifierModel>> __result)
        {
            __result = ModContentRegistry.AppendMutuallyExclusiveModifiers(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared relic pools to <see cref="ModelDb.AllRelicPools" />.
    ///     将 RitsuLib 注册的共享遗物池追加到 <see cref="ModelDb.AllRelicPools" />。
    /// </summary>
    public class AllRelicPoolsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_relic_pools";

        /// <inheritdoc />
        public static string Description => "Append registered shared relic pools to ModelDb.AllRelicPools";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllRelicPools", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod shared relic pools onto the vanilla sequence.
        ///     将 mod 共享遗物池拼接到原版序列之后。
        /// </summary>
        public static void Postfix(ref IEnumerable<RelicPoolModel> __result)
        {
            __result = ModContentRegistry.AppendSharedRelicPools(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared potion pools to <see cref="ModelDb.AllPotionPools" />.
    ///     将 RitsuLib 注册的共享药水池追加到 <see cref="ModelDb.AllPotionPools" />。
    /// </summary>
    public class AllPotionPoolsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "modeldb_all_potion_pools";

        /// <inheritdoc />
        public static string Description => "Append registered shared potion pools to ModelDb.AllPotionPools";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllPotionPools", MethodType.Getter)];
        }

        /// <summary>
        ///     Concatenates mod shared potion pools onto the vanilla sequence.
        ///     将 mod 共享药水池拼接到原版序列之后。
        /// </summary>
        public static void Postfix(ref IEnumerable<PotionPoolModel> __result)
        {
            __result = ModContentRegistry.AppendSharedPotionPools(__result);
        }
    }
}
