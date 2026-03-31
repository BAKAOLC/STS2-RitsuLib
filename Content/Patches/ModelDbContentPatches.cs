using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     Appends RitsuLib-registered characters to <see cref="ModelDb.AllCharacters" />.
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
            return [new(typeof(ModelDb), "get_AllCharacters")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod-registered characters onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<CharacterModel> __result)
        {
            __result = ModContentRegistry.AppendCharacters(__result);
        }
    }

    /// <summary>
    ///     Merges RitsuLib-registered monster types into <see cref="ModelDb.Monsters" /> by <see cref="AbstractModel.Id" />.
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
            return [new(typeof(ModelDb), "get_Monsters")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Ensures standalone-registered monsters appear in global monster enumeration even before every act lists them.
        /// </summary>
        public static void Postfix(ref IEnumerable<MonsterModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModContentRegistry.AppendRegisteredMonsters(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered acts to <see cref="ModelDb.Acts" />.
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
            return [new(typeof(ModelDb), "get_Acts")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod-registered acts onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<ActModel> __result)
        {
            __result = ModContentRegistry.AppendActs(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared events to <see cref="ModelDb.AllSharedEvents" />.
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
            return [new(typeof(ModelDb), "get_AllSharedEvents")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod shared events onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<EventModel> __result)
        {
            __result = ModContentRegistry.AppendSharedEvents(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered powers to <see cref="ModelDb.AllPowers" />.
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
            return [new(typeof(ModelDb), "get_AllPowers")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod powers onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<PowerModel> __result)
        {
            __result = ModContentRegistry.AppendPowers(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered orbs to <see cref="ModelDb.Orbs" />.
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
            return [new(typeof(ModelDb), "get_Orbs")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod orbs onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<OrbModel> __result)
        {
            __result = ModContentRegistry.AppendOrbs(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared card pools to <see cref="ModelDb.AllSharedCardPools" />.
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
            return [new(typeof(ModelDb), "get_AllSharedCardPools")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod shared card pools onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<CardPoolModel> __result)
        {
            __result = ModContentRegistry.AppendSharedCardPools(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared events to <see cref="ModelDb.AllEvents" />.
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
            return [new(typeof(ModelDb), "get_AllEvents")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod shared events onto the <see cref="ModelDb.AllEvents" /> sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<EventModel> __result)
        {
            __result = ModContentRegistry.AppendSharedEvents(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared ancients to <see cref="ModelDb.AllSharedAncients" />.
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
            return [new(typeof(ModelDb), "get_AllSharedAncients")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod shared ancients onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            __result = ModContentRegistry.AppendSharedAncients(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared ancients to <see cref="ModelDb.AllAncients" />.
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
            return [new(typeof(ModelDb), "get_AllAncients")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod shared ancients onto the <see cref="ModelDb.AllAncients" /> sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            __result = ModContentRegistry.AppendSharedAncients(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered enchantments to <see cref="ModelDb.DebugEnchantments" /> (covers dynamic types not in
    ///     subtype scan).
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
            return [new(typeof(ModelDb), "get_DebugEnchantments")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod enchantments onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<EnchantmentModel> __result)
        {
            __result = ModContentRegistry.AppendEnchantments(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered afflictions to <see cref="ModelDb.DebugAfflictions" />.
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
            return [new(typeof(ModelDb), "get_DebugAfflictions")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod afflictions onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<AfflictionModel> __result)
        {
            __result = ModContentRegistry.AppendAfflictions(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered achievements to <see cref="ModelDb.Achievements" />.
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
            return [new(typeof(ModelDb), "get_Achievements")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Merges mod achievements into the vanilla list by <see cref="AbstractModel.Id" />.
        /// </summary>
        public static void Postfix(ref IReadOnlyList<AchievementModel> __result)
        {
            __result = ModContentRegistry.AppendAchievements(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered modifiers to <see cref="ModelDb.GoodModifiers" />.
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
            return [new(typeof(ModelDb), "get_GoodModifiers")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Merges mod good modifiers into the vanilla list by <see cref="AbstractModel.Id" />.
        /// </summary>
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            __result = ModContentRegistry.AppendGoodModifiers(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered modifiers to <see cref="ModelDb.BadModifiers" />.
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
            return [new(typeof(ModelDb), "get_BadModifiers")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Merges mod bad modifiers into the vanilla list by <see cref="AbstractModel.Id" />.
        /// </summary>
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            __result = ModContentRegistry.AppendBadModifiers(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared relic pools to <see cref="ModelDb.AllRelicPools" />.
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
            return [new(typeof(ModelDb), "get_AllRelicPools")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod shared relic pools onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<RelicPoolModel> __result)
        {
            __result = ModContentRegistry.AppendSharedRelicPools(__result);
        }
    }

    /// <summary>
    ///     Appends RitsuLib-registered shared potion pools to <see cref="ModelDb.AllPotionPools" />.
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
            return [new(typeof(ModelDb), "get_AllPotionPools")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Concatenates mod shared potion pools onto the vanilla sequence.
        /// </summary>
        public static void Postfix(ref IEnumerable<PotionPoolModel> __result)
        {
            __result = ModContentRegistry.AppendSharedPotionPools(__result);
        }
    }
}
