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
}
