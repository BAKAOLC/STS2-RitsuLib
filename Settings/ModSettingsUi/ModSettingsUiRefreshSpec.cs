using System.Collections.Immutable;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Bindings that participate in UI refresh invalidation as a group (e.g. decorator + inner store).
    /// </summary>
    internal interface IModSettingsUiRefreshEquivalence
    {
        /// <summary>
        ///     Other binding instances that should count as the same target for selective refresh (typically the inner
        ///     binding when <see cref="ModSettingsDebugShowcaseBinding{TValue}" /> wraps an in-memory binding).
        /// </summary>
        IReadOnlyList<IModSettingsBinding> UiRefreshAlsoTreatAsDirty { get; }
    }

    internal enum ModSettingsRefreshRegistrationKind
    {
        Always,
        AnyBindingDirtyThisFlush,
        SpecificBindings,
    }

    /// <summary>
    ///     Declares when a registered settings UI refresh callback should run relative to bindings that were marked
    ///     dirty since the last flush.
    /// </summary>
    internal readonly record struct ModSettingsUiRefreshSpec(
        ModSettingsRefreshRegistrationKind Kind,
        ImmutableArray<IModSettingsBinding> Bindings)
    {
        public static ModSettingsUiRefreshSpec Always { get; } =
            new(ModSettingsRefreshRegistrationKind.Always, default);

        public static ModSettingsUiRefreshSpec AnyBindingDirty { get; } =
            new(ModSettingsRefreshRegistrationKind.AnyBindingDirtyThisFlush, default);

        public static ModSettingsUiRefreshSpec StaticDisplay { get; } =
            new(ModSettingsRefreshRegistrationKind.SpecificBindings, ImmutableArray<IModSettingsBinding>.Empty);

        public static ModSettingsUiRefreshSpec ForBinding(IModSettingsBinding binding)
        {
            return new(ModSettingsRefreshRegistrationKind.SpecificBindings, [binding]);
        }

        public static ModSettingsUiRefreshSpec ForBindings(params IModSettingsBinding[] bindings)
        {
            return new(ModSettingsRefreshRegistrationKind.SpecificBindings, [..bindings]);
        }

        internal static bool ShouldRun(
            ModSettingsUiRefreshSpec spec,
            bool treatAsFullPass,
            HashSet<IModSettingsBinding> dirtyBindings)
        {
            return spec.Kind switch
            {
                ModSettingsRefreshRegistrationKind.Always => true,
                ModSettingsRefreshRegistrationKind.AnyBindingDirtyThisFlush =>
                    treatAsFullPass || dirtyBindings.Count > 0,
                ModSettingsRefreshRegistrationKind.SpecificBindings =>
                    spec.Bindings.IsDefaultOrEmpty
                        ? treatAsFullPass
                        : Overlaps(dirtyBindings, spec.Bindings),
                _ => true,
            };
        }

        private static bool Overlaps(HashSet<IModSettingsBinding> dirty, ImmutableArray<IModSettingsBinding> bindings)
        {
            if (bindings.IsDefaultOrEmpty || dirty.Count == 0)
                return false;

            foreach (var b in bindings)
            {
                if (dirty.Contains(b))
                    return true;
                if (b is not IModSettingsUiRefreshEquivalence eq) continue;
                if (eq.UiRefreshAlsoTreatAsDirty.Any(dirty.Contains)) return true;
            }

            foreach (var d in dirty)
            {
                if (d is not IModSettingsUiRefreshEquivalence eq2)
                    continue;
                if ((from alias in eq2.UiRefreshAlsoTreatAsDirty
                        from b in bindings
                        where ReferenceEquals(b, alias)
                        select alias).Any()) return true;
            }

            return false;
        }
    }

    internal readonly record struct ModSettingsRefreshRegistration(
        Action Action,
        ModSettingsUiRefreshSpec Spec);
}
