using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered characters to <see cref="ModelDb.AllCharacters" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的角色追加到 <see cref="ModelDb.AllCharacters" />。</para>
    /// </summary>
    internal class AllCharactersPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_characters";
        public static string Description => "Append registered characters to ModelDb.AllCharacters";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllCharacters", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">Concatenates mod-registered characters onto the vanilla sequence.</para>
        ///     <para xml:lang="zh-CN">将 mod 注册的角色拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CharacterModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendCharacters);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Merges RitsuLib-registered monster types into <see cref="ModelDb.Monsters" /> by
    ///         <see cref="AbstractModel.Id" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">按 <see cref="AbstractModel.Id" /> 将 RitsuLib 注册的怪物类型合并到 <see cref="ModelDb.Monsters" />。</para>
    /// </summary>
    internal class AllMonstersPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_monsters";
        public static string Description => "Merge registered monsters into ModelDb.Monsters";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "Monsters", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<MonsterModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendRegisteredMonsters);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered acts to <see cref="ModelDb.Acts" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的章节追加到 <see cref="ModelDb.Acts" />。</para>
    /// </summary>
    internal class ActsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_acts";
        public static string Description => "Append registered acts to ModelDb.Acts";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "Acts", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">Concatenates mod-registered acts onto the vanilla sequence.</para>
        ///     <para xml:lang="zh-CN">将 mod 注册的章节拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<ActModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendActs);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered shared events to <see cref="ModelDb.AllSharedEvents" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的共享事件追加到 <see cref="ModelDb.AllSharedEvents" />。</para>
    /// </summary>
    internal class AllSharedEventsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_shared_events";
        public static string Description => "Append registered shared events to ModelDb.AllSharedEvents";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllSharedEvents", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">Concatenates mod shared events onto the vanilla sequence.</para>
        ///     <para xml:lang="zh-CN">将 mod 共享事件拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<EventModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedEvents);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered powers to <see cref="ModelDb.AllPowers" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的能力追加到 <see cref="ModelDb.AllPowers" />。</para>
    /// </summary>
    internal class AllPowersPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_powers";
        public static string Description => "Append registered powers to ModelDb.AllPowers";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllPowers", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<PowerModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendPowers);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered orbs to <see cref="ModelDb.Orbs" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的充能球追加到 <see cref="ModelDb.Orbs" />。</para>
    /// </summary>
    internal class AllOrbsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_orbs";
        public static string Description => "Append registered orbs to ModelDb.Orbs";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "Orbs", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<OrbModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendOrbs);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered shared card pools to <see cref="ModelDb.AllSharedCardPools" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的共享卡牌池追加到 <see cref="ModelDb.AllSharedCardPools" />。</para>
    /// </summary>
    internal class AllSharedCardPoolsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_shared_card_pools";
        public static string Description => "Append registered shared card pools to ModelDb.AllSharedCardPools";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllSharedCardPools", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">Concatenates mod shared card pools onto the vanilla sequence.</para>
        ///     <para xml:lang="zh-CN">将 mod 共享卡牌池拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CardPoolModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedCardPools);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered shared events to <see cref="ModelDb.AllEvents" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的共享事件追加到 <see cref="ModelDb.AllEvents" />。</para>
    /// </summary>
    internal class AllEventsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_events";
        public static string Description => "Append registered shared events to ModelDb.AllEvents";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllEvents", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<EventModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedEvents);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered shared ancients to <see cref="ModelDb.AllSharedAncients" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的共享ancient追加到 <see cref="ModelDb.AllSharedAncients" />。</para>
    /// </summary>
    internal class AllSharedAncientsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_shared_ancients";
        public static string Description => "Append registered shared ancients to ModelDb.AllSharedAncients";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllSharedAncients", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">Concatenates mod shared ancients onto the vanilla sequence.</para>
        ///     <para xml:lang="zh-CN">将 mod 共享ancient拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedAncients);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered shared ancients to <see cref="ModelDb.AllAncients" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的共享ancient追加到 <see cref="ModelDb.AllAncients" />。</para>
    /// </summary>
    internal class AllAncientsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_ancients";
        public static string Description => "Append registered shared ancients to ModelDb.AllAncients";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllAncients", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedAncients);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Appends RitsuLib-registered enchantments to <see cref="ModelDb.DebugEnchantments" /> (covers
    ///         dynamic types not in subtype scan).
    ///     </para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的附魔追加到 <see cref="ModelDb.DebugEnchantments" />（覆盖子类型扫描之外的动态类型）。</para>
    /// </summary>
    internal class DebugEnchantmentsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_debug_enchantments";
        public static string Description => "Append registered enchantments to ModelDb.DebugEnchantments";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "DebugEnchantments", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<EnchantmentModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendEnchantments);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered afflictions to <see cref="ModelDb.DebugAfflictions" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的苦痛追加到 <see cref="ModelDb.DebugAfflictions" />。</para>
    /// </summary>
    internal class DebugAfflictionsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_debug_afflictions";
        public static string Description => "Append registered afflictions to ModelDb.DebugAfflictions";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "DebugAfflictions", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<AfflictionModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendAfflictions);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered achievements to <see cref="ModelDb.Achievements" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的成就追加到 <see cref="ModelDb.Achievements" />。</para>
    /// </summary>
    internal class AchievementsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_achievements";
        public static string Description => "Append registered achievements to ModelDb.Achievements";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "Achievements", MethodType.Getter)];
        }

        public static void Postfix(ref IReadOnlyList<AchievementModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendAchievements);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered modifiers to <see cref="ModelDb.GoodModifiers" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的修饰符追加到 <see cref="ModelDb.GoodModifiers" />。</para>
    /// </summary>
    internal class GoodModifiersPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_good_modifiers";
        public static string Description => "Append registered good modifiers to ModelDb.GoodModifiers";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "GoodModifiers", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Merges mod good modifiers into the current list using
        ///         <see cref="ModifierRegistration.ModifierListSortOrder" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">按 <see cref="ModifierRegistration.ModifierListSortOrder" /> 将 mod 正面修饰符合并到当前列表中。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendGoodModifiers);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered modifiers to <see cref="ModelDb.BadModifiers" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的修饰符追加到 <see cref="ModelDb.BadModifiers" />。</para>
    /// </summary>
    internal class BadModifiersPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_bad_modifiers";
        public static string Description => "Append registered bad modifiers to ModelDb.BadModifiers";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "BadModifiers", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Merges mod bad modifiers into the current list using
        ///         <see cref="ModifierRegistration.ModifierListSortOrder" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">按 <see cref="ModifierRegistration.ModifierListSortOrder" /> 将 mod 负面修饰符合并到当前列表中。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendBadModifiers);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Merges RitsuLib-registered modifier exclusivity groups into
    ///         <see cref="ModelDb.MutuallyExclusiveModifiers" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的修饰符互斥组合并到 <see cref="ModelDb.MutuallyExclusiveModifiers" />。</para>
    /// </summary>
    internal class MutuallyExclusiveModifiersPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_mutually_exclusive_modifiers";

        public static string Description =>
            "Merge registered mutually exclusive modifier groups into ModelDb.MutuallyExclusiveModifiers";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "MutuallyExclusiveModifiers", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">Merges mod exclusivity groups into the current list, including overlapping vanilla sets.</para>
        ///     <para xml:lang="zh-CN">将 mod 互斥组合并到当前列表，并与存在交集的原版集合合并。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<IReadOnlySet<ModifierModel>> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendMutuallyExclusiveModifiers);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered shared relic pools to <see cref="ModelDb.AllRelicPools" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的共享遗物池追加到 <see cref="ModelDb.AllRelicPools" />。</para>
    /// </summary>
    internal class AllRelicPoolsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_relic_pools";
        public static string Description => "Append registered shared relic pools to ModelDb.AllRelicPools";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllRelicPools", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<RelicPoolModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedRelicPools);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Appends RitsuLib-registered shared potion pools to <see cref="ModelDb.AllPotionPools" />.</para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的共享药水池追加到 <see cref="ModelDb.AllPotionPools" />。</para>
    /// </summary>
    internal class AllPotionPoolsPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_all_potion_pools";
        public static string Description => "Append registered shared potion pools to ModelDb.AllPotionPools";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), "AllPotionPools", MethodType.Getter)];
        }

        public static void Postfix(ref IEnumerable<PotionPoolModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedPotionPools);
        }
    }
}
