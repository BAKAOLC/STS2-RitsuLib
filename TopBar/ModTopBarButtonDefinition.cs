using System.Numerics;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Immutable registry entry for a mod-owned top-bar button.
    /// </summary>
    public sealed record ModTopBarButtonDefinition
    {
        internal ModTopBarButtonDefinition(
            string modId,
            string id,
            string? iconPath,
            string? locStem,
            int order,
            Vector2 offset,
            Action<ModTopBarButtonContext>? onClick,
            Func<ModTopBarButtonContext, bool>? visibleWhen,
            Func<ModTopBarButtonContext, bool>? isOpenWhen,
            Func<ModTopBarButtonContext, int>? countProvider)
        {
            ModId = modId;
            Id = id;
            IconPath = iconPath;
            LocStem = string.IsNullOrWhiteSpace(locStem) ? id : locStem;
            Order = order;
            Offset = offset;
            OnClick = onClick;
            VisibleWhen = visibleWhen;
            IsOpenWhen = isOpenWhen;
            CountProvider = countProvider;
        }

        /// <summary>Owning mod id.</summary>
        public string ModId { get; }

        /// <summary>Normalized global id (e.g. <c>MYMOD_TOPBARBUTTON_RECIPES</c>).</summary>
        public string Id { get; }

        /// <summary>Godot resource path for the icon, or null.</summary>
        public string? IconPath { get; }

        /// <summary>Effective loc stem (defaults to <see cref="Id" />).</summary>
        public string LocStem { get; }

        /// <summary>Sort order within this mod's top-bar buttons.</summary>
        public int Order { get; }

        /// <summary>Extra pixel offset on top of the auto-stacked slot.</summary>
        public Vector2 Offset { get; }

        /// <summary>Click handler; see <see cref="ModTopBarButtonSpec.OnClick" />.</summary>
        public Action<ModTopBarButtonContext>? OnClick { get; }

        /// <summary>Optional visibility predicate; see <see cref="ModTopBarButtonSpec.VisibleWhen" />.</summary>
        public Func<ModTopBarButtonContext, bool>? VisibleWhen { get; }

        /// <summary>Optional "screen open" predicate; see <see cref="ModTopBarButtonSpec.IsOpenWhen" />.</summary>
        public Func<ModTopBarButtonContext, bool>? IsOpenWhen { get; }

        /// <summary>Optional count provider for the badge; see <see cref="ModTopBarButtonSpec.CountProvider" />.</summary>
        public Func<ModTopBarButtonContext, int>? CountProvider { get; }

        /// <summary>Hover-tip title resolved against <c>static_hover_tips</c> with key <c>{LocStem}.title</c>.</summary>
        public LocString Title => new(ModTopBarButtonSpec.HoverTipLocTable, $"{LocStem}.title");

        /// <summary>Hover-tip description resolved against <c>static_hover_tips</c> with key <c>{LocStem}.description</c>.</summary>
        public LocString Description => new(ModTopBarButtonSpec.HoverTipLocTable, $"{LocStem}.description");
    }
}
