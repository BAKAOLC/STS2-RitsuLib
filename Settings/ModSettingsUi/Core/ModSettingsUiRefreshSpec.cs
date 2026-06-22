using System.Collections.Immutable;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     When <see cref="RitsuModSettingsSubmenu.MarkDirty" /> is called on this binding, these bindings are also
    ///     marked dirty so selective refresh specs and autosave see the same invalidation (e.g. projected field → list
    ///     root, decorator → inner).
    ///     当对此 binding 调用 <see cref="RitsuModSettingsSubmenu.MarkDirty" /> 时，这些 binding 也会
    ///     被标记为脏，使选择性刷新规范和自动保存看到相同的失效（例如投影字段 → 列表
    ///     根、装饰器 → 内部）。
    /// </summary>
    internal interface IModSettingsUiRefreshPropagation
    {
        IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi { get; }
    }

    /// <summary>
    ///     Bindings that participate in UI refresh invalidation as a group (e.g. decorator + inner store).
    ///     作为一组参与 UI 刷新失效的 binding（例如装饰器 + 内部存储）。
    /// </summary>
    internal interface IModSettingsUiRefreshEquivalence
    {
        /// <summary>
        ///     Other binding instances that should count as the same target for selective refresh (typically the inner
        ///     binding when <see cref="ModSettingsDebugShowcaseBinding{TValue}" /> wraps an in-memory binding).
        ///     在选择性刷新中应视为同一目标的其他 binding 实例（通常是内部
        ///     binding，当 <see cref="ModSettingsDebugShowcaseBinding{TValue}" /> 包装内存 binding 时）。
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
    ///     声明已注册的设置 UI 刷新回调应何时运行，依据是上次 flush 后被标记为
    ///     脏的 binding。
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

        internal bool IsStaticDisplay =>
            Kind == ModSettingsRefreshRegistrationKind.SpecificBindings && Bindings.IsDefaultOrEmpty;

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
                        : treatAsFullPass || Overlaps(dirtyBindings, spec.Bindings),
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
                if (b is not IModSettingsUiRefreshEquivalence eq)
                    continue;
                if (eq.UiRefreshAlsoTreatAsDirty.Any(dirty.Contains))
                    return true;
            }

            foreach (var d in dirty)
            {
                if (d is not IModSettingsUiRefreshEquivalence eq2)
                    continue;
                if ((from alias in eq2.UiRefreshAlsoTreatAsDirty
                        from reg in bindings
                        where ReferenceEquals(reg, alias)
                        select alias).Any())
                    return true;
            }

            var dirtyExpanded = ModSettingsBindingInvalidationTopology.ExpandUnion(dirty);
            return Enumerable.Any(bindings,
                registered => ModSettingsBindingInvalidationTopology.ExpandClosure(registered)
                    .Any(dirtyExpanded.Contains));
        }
    }

    internal readonly record struct ModSettingsRefreshRegistration(
        Action Action,
        ModSettingsUiRefreshSpec Spec);
}
