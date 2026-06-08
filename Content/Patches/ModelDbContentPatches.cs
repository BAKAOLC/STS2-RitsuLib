using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered characters to <see cref="ModelDb.AllCharacters" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的角色追加到 <see cref="ModelDb.AllCharacters" />。</para>
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
        /// <para xml:lang="en">Concatenates mod-registered characters onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 注册的角色拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CharacterModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendCharacters);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Merges RitsuLib-registered monster types into <see cref="ModelDb.Monsters" /> by <see cref="AbstractModel.Id" />.</para>
    /// <para xml:lang="zh-CN">按 <see cref="AbstractModel.Id" /> 将 RitsuLib 注册的怪物类型合并到 <see cref="ModelDb.Monsters" />。</para>
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
        /// <para xml:lang="en">Ensures standalone-registered monsters appear in global monster enumeration even before every act lists them.</para>
        /// <para xml:lang="zh-CN">确保独立注册的怪物即使在每个章节列出它们之前，也会出现在全局怪物枚举中。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<MonsterModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendRegisteredMonsters);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered acts to <see cref="ModelDb.Acts" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的章节追加到 <see cref="ModelDb.Acts" />。</para>
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
        /// <para xml:lang="en">Concatenates mod-registered acts onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 注册的章节拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<ActModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendActs);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered shared events to <see cref="ModelDb.AllSharedEvents" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的共享事件追加到 <see cref="ModelDb.AllSharedEvents" />。</para>
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
        /// <para xml:lang="en">Concatenates mod shared events onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 共享事件拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<EventModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedEvents);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered powers to <see cref="ModelDb.AllPowers" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的能力追加到 <see cref="ModelDb.AllPowers" />。</para>
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
        /// <para xml:lang="en">Concatenates mod powers onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 能力拼接到原版序列之后。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<PowerModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendPowers);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered orbs to <see cref="ModelDb.Orbs" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的充能球追加到 <see cref="ModelDb.Orbs" />。</para>
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
        /// <para xml:lang="en">Concatenates mod orbs onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 充能球拼接到原版序列之后。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<OrbModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendOrbs);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered shared card pools to <see cref="ModelDb.AllSharedCardPools" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的共享卡牌池追加到 <see cref="ModelDb.AllSharedCardPools" />。</para>
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
        /// <para xml:lang="en">Concatenates mod shared card pools onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 共享卡牌池拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CardPoolModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedCardPools);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered shared events to <see cref="ModelDb.AllEvents" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的共享事件追加到 <see cref="ModelDb.AllEvents" />。</para>
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
        /// <para xml:lang="en">Concatenates mod shared events onto the <see cref="ModelDb.AllEvents" /> sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 共享事件拼接到 <see cref="ModelDb.AllEvents" /> 序列之后。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<EventModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedEvents);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered shared ancients to <see cref="ModelDb.AllSharedAncients" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的共享ancient追加到 <see cref="ModelDb.AllSharedAncients" />。</para>
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
        /// <para xml:lang="en">Concatenates mod shared ancients onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 共享ancient拼接到原版序列之后。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedAncients);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered shared ancients to <see cref="ModelDb.AllAncients" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的共享ancient追加到 <see cref="ModelDb.AllAncients" />。</para>
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
        /// <para xml:lang="en">Concatenates mod shared ancients onto the <see cref="ModelDb.AllAncients" /> sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 共享远古事件拼接到 <see cref="ModelDb.AllAncients" /> 序列之后。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedAncients);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered enchantments to <see cref="ModelDb.DebugEnchantments" /> (covers dynamic types not in subtype scan).</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的附魔追加到 <see cref="ModelDb.DebugEnchantments" />（覆盖子类型扫描之外的动态类型）。</para>
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
        /// <para xml:lang="en">Concatenates mod enchantments onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 附魔拼接到原版序列之后。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<EnchantmentModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendEnchantments);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered afflictions to <see cref="ModelDb.DebugAfflictions" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的苦痛追加到 <see cref="ModelDb.DebugAfflictions" />。</para>
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
        /// <para xml:lang="en">Concatenates mod afflictions onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 苦痛拼接到原版序列之后。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<AfflictionModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendAfflictions);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered achievements to <see cref="ModelDb.Achievements" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的成就追加到 <see cref="ModelDb.Achievements" />。</para>
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
        /// <para xml:lang="en">Merges mod achievements into the vanilla list by <see cref="AbstractModel.Id" />.</para>
        /// <para xml:lang="zh-CN">按 <see cref="AbstractModel.Id" /> 将 mod 成就合并到原版列表中。</para>
        /// </summary>
        public static void Postfix(ref IReadOnlyList<AchievementModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendAchievements);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered modifiers to <see cref="ModelDb.GoodModifiers" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的修饰符追加到 <see cref="ModelDb.GoodModifiers" />。</para>
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
        /// <para xml:lang="en">Merges mod good modifiers into the current list using <see cref="ModifierRegistration.ModifierListSortOrder" />.</para>
        /// <para xml:lang="zh-CN">按 <see cref="ModifierRegistration.ModifierListSortOrder" /> 将 mod 正面修饰符合并到当前列表中。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendGoodModifiers);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered modifiers to <see cref="ModelDb.BadModifiers" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的修饰符追加到 <see cref="ModelDb.BadModifiers" />。</para>
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
        /// <para xml:lang="en">Merges mod bad modifiers into the current list using <see cref="ModifierRegistration.ModifierListSortOrder" />.</para>
        /// <para xml:lang="zh-CN">按 <see cref="ModifierRegistration.ModifierListSortOrder" /> 将 mod 负面修饰符合并到当前列表中。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendBadModifiers);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Merges RitsuLib-registered modifier exclusivity groups into <see cref="ModelDb.MutuallyExclusiveModifiers" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的修饰符互斥组合并到 <see cref="ModelDb.MutuallyExclusiveModifiers" />。</para>
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
        /// <para xml:lang="en">Merges mod exclusivity groups into the current list, including overlapping vanilla sets.</para>
        /// <para xml:lang="zh-CN">将 mod 互斥组合并到当前列表，并与存在交集的原版集合合并。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IReadOnlyList<IReadOnlySet<ModifierModel>> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendMutuallyExclusiveModifiers);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered shared relic pools to <see cref="ModelDb.AllRelicPools" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的共享遗物池追加到 <see cref="ModelDb.AllRelicPools" />。</para>
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
        /// <para xml:lang="en">Concatenates mod shared relic pools onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 共享遗物池拼接到原版序列之后。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<RelicPoolModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedRelicPools);
        }
    }

    /// <summary>
    /// <para xml:lang="en">Appends RitsuLib-registered shared potion pools to <see cref="ModelDb.AllPotionPools" />.</para>
    /// <para xml:lang="zh-CN">将 RitsuLib 注册的共享药水池追加到 <see cref="ModelDb.AllPotionPools" />。</para>
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
        /// <para xml:lang="en">Concatenates mod shared potion pools onto the vanilla sequence.</para>
        /// <para xml:lang="zh-CN">将 mod 共享药水池拼接到原版序列之后。</para>
        /// </summary>
        public static void Postfix(ref IEnumerable<PotionPoolModel> __result)
        {
            ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedPotionPools);
        }
    }
}
