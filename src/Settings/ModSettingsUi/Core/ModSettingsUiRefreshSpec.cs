using System.Collections.Immutable;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Allows a binding to propagate <see cref="RitsuModSettingsSubmenu.MarkDirty" /> to related bindings so
    ///         selective refresh and autosave observe the same invalidation. Examples include a projected field and its
    ///         list root, or a decorator and its inner binding.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         允许绑定将 <see cref="RitsuModSettingsSubmenu.MarkDirty" /> 传播到相关绑定，使选择性刷新和自动保存观察到
    ///         相同的失效关系，例如投影字段与列表根绑定，或装饰器与内部绑定。
    ///     </para>
    /// </summary>
    internal interface IModSettingsUiRefreshPropagation
    {
        IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Identifies bindings that participate in UI refresh invalidation as one equivalent group.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         标识在界面刷新失效判断中视为同一等价组的绑定。
    ///     </para>
    /// </summary>
    internal interface IModSettingsUiRefreshEquivalence
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets other binding instances treated as the same selective-refresh target, such as the inner binding
        ///         wrapped by <see cref="ModSettingsDebugShowcaseBinding{TValue}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取在选择性刷新中视为同一目标的其他绑定实例，例如
        ///         <see cref="ModSettingsDebugShowcaseBinding{TValue}" /> 所包装的内部绑定。
        ///     </para>
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
    ///     <para xml:lang="en">
    ///         Declares when a registered settings UI refresh callback runs, based on bindings marked dirty since the
    ///         previous refresh flush.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         根据上一次刷新队列执行后被标记为脏的绑定，声明已注册的设置界面刷新回调何时运行。
    ///     </para>
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
            new(ModSettingsRefreshRegistrationKind.SpecificBindings, []);

        internal bool IsStaticDisplay =>
            Kind == ModSettingsRefreshRegistrationKind.SpecificBindings && Bindings.IsDefaultOrEmpty;

        public static ModSettingsUiRefreshSpec ForBinding(IModSettingsBinding binding)
        {
            ArgumentNullException.ThrowIfNull(binding);
            return new(ModSettingsRefreshRegistrationKind.SpecificBindings, [binding]);
        }

        public static ModSettingsUiRefreshSpec ForBindings(params IModSettingsBinding[] bindings)
        {
            ArgumentNullException.ThrowIfNull(bindings);
            // Keep validation failure separate from successful specification construction.
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (bindings.Any(static binding => binding == null))
                throw new ArgumentException("Refresh bindings cannot contain null.", nameof(bindings));

            return new(ModSettingsRefreshRegistrationKind.SpecificBindings, [.. bindings]);
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
