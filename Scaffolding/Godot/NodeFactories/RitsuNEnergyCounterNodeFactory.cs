using Godot;
using Godot.Collections;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Converts mod <see cref="NEnergyCounter" /> scenes (or procedural layers) into game-ready energy orbs.
    /// </summary>
    internal sealed class RitsuNEnergyCounterNodeFactory() : RitsuGodotNodeFactory<NEnergyCounter>([
        new RitsuGodotNodeSlot<NParticlesContainer>("%EnergyVfxBack"),
        new RitsuGodotNodeSlot<Control>("%Layers"),
        new RitsuGodotNodeSlot<Control>("%RotationLayers"),
        new RitsuGodotNodeSlot<NParticlesContainer>("%EnergyVfxFront"),
        new RitsuGodotNodeSlot<MegaLabel>("Label"),
        new RitsuGodotNodeSlot<NParticlesContainer>("%StarAnchor"),
    ])
    {
        private const string DefaultLabelFontPath = "res://themes/kreon_bold_shared.tres";

        private static readonly StringName ShadowOffsetX = "shadow_offset_x";
        private static readonly StringName ShadowOffsetY = "shadow_offset_y";
        private static readonly StringName ShadowOutlineSize = "shadow_outline_size";
        private static readonly StringName LabelThemeType = "Label";

        protected override NEnergyCounter CreateBareFromResourceImpl(object resource)
        {
            throw new NotSupportedException(
                "RitsuNEnergyCounterNodeFactory only supports scene conversion via RitsuGodotNodeFactories.CreateFromScene / CreateFromScenePath<NEnergyCounter>(...).");
        }

        protected override void ConvertScene(NEnergyCounter target, Node? source)
        {
            if (source != null)
            {
                target.Name = source.Name;
                switch (target)
                {
                    case Control targetControl when source is Control sourceControl:
                        CopyControlProperties(targetControl, sourceControl);
                        break;
                    case CanvasItem targetItem when source is CanvasItem sourceItem:
                        CopyCanvasItemProperties(targetItem, sourceItem);
                        break;
                    case Control energyCounter:
                        energyCounter.Size = new(128f, 128f);
                        energyCounter.PivotOffset = energyCounter.Size * 0.5f;
                        break;
                }
            }

            TransferAndCreateNodes(target, source);
        }

        protected override void GenerateNode(NEnergyCounter target, IRitsuGodotNodeSlot required)
        {
            switch (required.Path)
            {
                case "Label":
                    target.AddChild(CreateDefaultLabel());
                    break;
                case "%RotationLayers":
                {
                    var control = CreateFullRectControl(null);
                    target.AddUniqueChild(control, "RotationLayers");
                    break;
                }
                case "%EnergyVfxBack":
                    target.AddUniqueChild(CreateParticlesContainer(null, "EnergyVfxBack"));
                    break;
                case "%EnergyVfxFront":
                    target.AddUniqueChild(CreateParticlesContainer(null, "EnergyVfxFront"));
                    break;
                case "%StarAnchor":
                    target.AddUniqueChild(CreateParticlesContainer(null, "StarAnchor"));
                    break;
            }
        }

        protected override Node ConvertNodeType(Node node, Type targetType)
        {
            if (targetType == typeof(NParticlesContainer))
                return CreateParticlesContainer(node, node.Name);

            if (targetType == typeof(Control))
                return CreateFullRectControl(node);

            if (targetType == typeof(MegaLabel))
                return CreateLabel(node) ?? base.ConvertNodeType(node, targetType);

            return base.ConvertNodeType(node, targetType);
        }

        private static Control CreateFullRectControl(Node? n)
        {
            var rectControl = new Control
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = Control.GrowDirection.Both,
                GrowVertical = Control.GrowDirection.Both,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            if (n == null)
                return rectControl;

            rectControl.Name = n.Name;
            n.GetParent()?.RemoveChild(n);
            n.Name = "_" + n.Name;
            rectControl.AddChild(n);
            return rectControl;
        }

        private static NParticlesContainer CreateParticlesContainer(Node? source, StringName name)
        {
            var container = new NParticlesContainer
            {
                Name = name,
                UniqueNameInOwner = true,
            };

            if (source != null)
                source.Name = "_" + source.Name;

            if (source is CanvasItem sourceCanvas)
                CopyCanvasItemProperties(container, sourceCanvas);

            if (source is GpuParticles2D singleParticle)
            {
                container.AddChild(singleParticle);
                singleParticle.Owner = container;
                SetParticles(container);
                return container;
            }

            if (source != null)
            {
                source.GetParent()?.RemoveChild(source);
                container.AddChild(source);
            }

            SetParticles(container);
            return container;
        }

        private static void SetParticles(NParticlesContainer container)
        {
            var particles = new Array<GpuParticles2D>();
            CollectParticles(container, particles);
            container._particles = particles;
        }

        private static void CollectParticles(Node node, Array<GpuParticles2D> particles)
        {
            foreach (var child in node.GetChildren())
            {
                if (child is GpuParticles2D particle)
                    particles.Add(particle);
                CollectParticles(child, particles);
            }
        }

        private static MegaLabel? CreateLabel(Node? source)
        {
            if (source is not Label sourceLabel)
                return null;

            var label = new MegaLabel { Name = source.Name };
            CopyControlProperties(label, sourceLabel);
            label.Text = sourceLabel.Text;
            label.HorizontalAlignment = sourceLabel.HorizontalAlignment;
            label.VerticalAlignment = sourceLabel.VerticalAlignment;
            label.AutowrapMode = sourceLabel.AutowrapMode;
            label.ClipText = sourceLabel.ClipText;
            label.Uppercase = sourceLabel.Uppercase;
            label.VisibleCharactersBehavior = sourceLabel.VisibleCharactersBehavior;

            EnsureLabelFont(label, sourceLabel);
            CopyLabelThemeOverrides(label, sourceLabel);

            if (sourceLabel is MegaLabel sourceMegaLabel)
            {
                label.AutoSizeEnabled = sourceMegaLabel.AutoSizeEnabled;
                label.MinFontSize = sourceMegaLabel.MinFontSize;
                label.MaxFontSize = sourceMegaLabel.MaxFontSize;
            }
            else
            {
                label.AutoSizeEnabled = true;
                label.MinFontSize = 32;
                label.MaxFontSize = Math.Max(36,
                    sourceLabel.GetThemeFontSize(ThemeConstants.Label.FontSize, LabelThemeType));
            }

            source.Free();
            return label;
        }

        private static MegaLabel CreateDefaultLabel()
        {
            var label = new MegaLabel
            {
                Name = "Label",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                OffsetLeft = 16f,
                OffsetTop = -29f,
                OffsetRight = -16f,
                OffsetBottom = 29f,
                GrowHorizontal = Control.GrowDirection.Both,
                GrowVertical = Control.GrowDirection.Both,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Text = "3/3",
                AutoSizeEnabled = true,
                MinFontSize = 32,
                MaxFontSize = 36,
            };
            EnsureLabelFont(label, null);
            label.AddThemeColorOverride(ThemeConstants.Label.FontColor, new(1f, 0.964706f, 0.886275f));
            label.AddThemeColorOverride(ThemeConstants.Label.FontShadowColor, new(0f, 0f, 0f, 0.188235f));
            label.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, new(0.3f, 0.0759f, 0.051f));
            label.AddThemeConstantOverride(ShadowOffsetX, 3);
            label.AddThemeConstantOverride(ShadowOffsetY, 2);
            label.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, 16);
            label.AddThemeConstantOverride(ShadowOutlineSize, 16);
            label.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 36);
            return label;
        }

        private static void EnsureLabelFont(MegaLabel target, Label? source)
        {
            var font = source != null
                ? source.GetThemeFont(ThemeConstants.Label.Font, LabelThemeType)
                : null;
            if (source != null && ReferenceEquals(font, source.GetThemeDefaultFont()))
                font = PreloadManager.Cache.GetAsset<Font>(DefaultLabelFontPath);
            font ??= PreloadManager.Cache.GetAsset<Font>(DefaultLabelFontPath);
            target.AddThemeFontOverride(ThemeConstants.Label.Font, font);
        }

        private static void CopyLabelThemeOverrides(MegaLabel target, Label source)
        {
            target.AddThemeColorOverride(ThemeConstants.Label.FontColor,
                source.GetThemeColor(ThemeConstants.Label.FontColor, LabelThemeType));
            target.AddThemeColorOverride(ThemeConstants.Label.FontShadowColor,
                source.GetThemeColor(ThemeConstants.Label.FontShadowColor, LabelThemeType));
            target.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor,
                source.GetThemeColor(ThemeConstants.Label.FontOutlineColor, LabelThemeType));
            target.AddThemeConstantOverride(ShadowOffsetX,
                source.GetThemeConstant(ShadowOffsetX, LabelThemeType));
            target.AddThemeConstantOverride(ShadowOffsetY,
                source.GetThemeConstant(ShadowOffsetY, LabelThemeType));
            target.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize,
                source.GetThemeConstant(ThemeConstants.Label.OutlineSize, LabelThemeType));
            target.AddThemeConstantOverride(ShadowOutlineSize,
                source.GetThemeConstant(ShadowOutlineSize, LabelThemeType));
            target.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize,
                source.GetThemeFontSize(ThemeConstants.Label.FontSize, LabelThemeType));
        }
    }
}
