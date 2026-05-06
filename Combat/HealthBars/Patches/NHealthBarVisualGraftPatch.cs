using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.HealthBars.Patches
{
    internal static class NHealthBarGraftUiPatchHelper
    {
        private static readonly Color DefaultGraftColor = new("5C8F6E");

        private static readonly AttachedState<NHealthBar, GraftUiState> GraftStates = new(_ => new());

        public static void RefreshGraftOverlay(NHealthBar healthBar)
        {
            BaseLibVisualGraftBridge.TryRegisterSecondary();
            if (BaseLibVisualGraftBridge.ShouldRitsuGraftStandDown())
                return;

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.ShowsInfiniteHp)
            {
                ResetGraft(healthBar);
                return;
            }

            var metrics = HealthBarVisualGraftRegistry.Aggregate(creature);
            var graftHp = Math.Max(0, metrics.GraftHp);
            if (creature.MaxHp <= 0)
                return;

            if (graftHp <= 0)
            {
                ResetGraft(healthBar);
                return;
            }

            var visualDenom = Math.Max(creature.MaxHp, creature.CurrentHp + graftHp);
            var scale = visualDenom / (float)creature.MaxHp;
            if (scale <= 1.0001f)
            {
                ResetGraft(healthBar);
                return;
            }

            if (!EnsureGraftStrip(healthBar, out var state))
                return;

            SyncHpBarToHitbox(healthBar, scale);

            ApplyMainForegroundDenom(healthBar, creature, visualDenom);
            RecomputeVanillaPoisonAndDoomForVisualDenom(healthBar, creature, visualDenom);
            PositionGraftStrip(healthBar, creature, graftHp, visualDenom, metrics, state);
        }

        public static void AfterForecastTouchup(NHealthBar healthBar)
        {
            if (BaseLibVisualGraftBridge.ShouldRitsuGraftStandDown())
                return;

            var creature = healthBar._creature;
            if (creature.CurrentHp <= 0 || creature.ShowsInfiniteHp)
                return;

            var metrics = HealthBarVisualGraftRegistry.Aggregate(creature);
            var graftHp = Math.Max(0, metrics.GraftHp);
            if (graftHp <= 0 || creature.MaxHp <= 0)
                return;

            var visualDenom = Math.Max(creature.MaxHp, creature.CurrentHp + graftHp);
            var scale = visualDenom / (float)creature.MaxHp;
            if (scale <= 1.0001f)
                return;

            if (!GraftStates.TryGetValue(healthBar, out var state) || state.Strip == null)
                return;

            RecomputeVanillaPoisonAndDoomForVisualDenom(healthBar, creature, visualDenom);
            PositionGraftStrip(healthBar, creature, graftHp, visualDenom, metrics, state);
        }

        private static void ResetGraft(NHealthBar healthBar)
        {
            if (!GraftStates.TryGetValue(healthBar, out var state))
                return;

            if (state.Strip != null)
            {
                state.Strip.Visible = false;
                state.Strip.Material = null;
                state.Strip.SelfModulate = Colors.White;
            }

            SyncHpBarToHitbox(healthBar, 1f);
        }

        private static void SyncHpBarToHitbox(NHealthBar healthBar, float widthMultiplier)
        {
            if (healthBar.GetParent()?.GetParent() is not NCreature creatureNode)
                return;

            var bounds = creatureNode.Hitbox;
            var creature = healthBar._creature;
            var pad = (24f - creature.Monster?.HpBarSizeReduction).GetValueOrDefault();
            var vanillaX = bounds.Size.X + pad;
            var mult = widthMultiplier < 1f ? 1f : widthMultiplier;
            var targetX = vanillaX * mult;

            var hpBar = healthBar.HpBarContainer;
            var gp = hpBar.GlobalPosition;
            gp.X = bounds.GlobalPosition.X - pad * 0.5f;
            if (mult > 1.0001f)
                gp.X -= (targetX - vanillaX) * 0.5f;
            hpBar.GlobalPosition = gp;

            NHealthBarGraftCompat.TryResizeHpBarContainer(healthBar, new(targetX, hpBar.Size.Y));

            if (healthBar.GetNodeOrNull<Control>("%BlockContainer") is not { } block) return;
            var half = block.Size.X * 0.5f;
            var bgp = block.GlobalPosition;
            bgp.X = bounds.GlobalPosition.X - half;
            block.GlobalPosition = bgp;
        }

        private static void ApplyMainForegroundDenom(NHealthBar healthBar, Creature creature, int visualDenom)
        {
            if (!healthBar._hpForeground.Visible)
                return;

            var maxWidth = GetMaxFgWidth(healthBar);
            if (maxWidth <= 0f || visualDenom <= 0)
                return;

            var width = Math.Max((float)creature.CurrentHp / visualDenom * maxWidth, creature.CurrentHp > 0 ? 12f : 0f);
            healthBar._hpForeground.OffsetRight = width - maxWidth;
        }

        private static void RecomputeVanillaPoisonAndDoomForVisualDenom(
            NHealthBar healthBar,
            Creature creature,
            int visualDenom)
        {
            var maxWidth = GetMaxFgWidth(healthBar);
            if (maxWidth <= 0f || visualDenom <= 0)
                return;

            var hpForeground = healthBar._hpForeground;
            if (healthBar._poisonForeground is not NinePatchRect poisonForeground ||
                healthBar._doomForeground is not NinePatchRect doomForeground)
                return;
            var poisonDamage = creature.GetPower<PoisonPower>()?.CalculateTotalDamageNextTurn() ?? 0;
            var doomAmount = creature.GetPowerAmount<DoomPower>();
            var currentHpOffsetRight = GetFgWidthForDenom(healthBar, creature.CurrentHp, visualDenom) - maxWidth;

            if (creature.HasPower<PoisonPower>())
            {
                if (poisonDamage > 0)
                {
                    poisonForeground.Visible = true;
                    if (poisonDamage >= creature.CurrentHp)
                    {
                        poisonForeground.OffsetLeft = 0f;
                        poisonForeground.OffsetRight = currentHpOffsetRight;
                        hpForeground.Visible = false;
                    }
                    else
                    {
                        var hpAfterPoison = Math.Max(0, creature.CurrentHp - poisonDamage);
                        var fgWidthAfterPoison = GetFgWidthForDenom(healthBar, hpAfterPoison, visualDenom);
                        hpForeground.OffsetRight = fgWidthAfterPoison - maxWidth;
                        hpForeground.Visible = true;
                        poisonForeground.OffsetLeft =
                            Math.Max(0f, fgWidthAfterPoison - poisonForeground.PatchMarginLeft);
                        poisonForeground.OffsetRight = currentHpOffsetRight;
                    }
                }
                else
                {
                    poisonForeground.Visible = false;
                }
            }
            else
            {
                poisonForeground.Visible = false;
                poisonForeground.OffsetLeft = 0f;
            }

            if (creature.HasPower<DoomPower>())
            {
                if (doomAmount > 0)
                {
                    doomForeground.Visible = true;
                    var doomOffset = GetFgWidthForDenom(healthBar, doomAmount, visualDenom) - maxWidth;
                    var doomLethal = doomAmount >= creature.CurrentHp - poisonDamage;
                    var poisonLethal = poisonDamage >= creature.CurrentHp;
                    if (doomLethal)
                    {
                        if (!poisonLethal)
                        {
                            doomForeground.OffsetRight = hpForeground.OffsetRight;
                            hpForeground.Visible = false;
                        }
                        else
                        {
                            hpForeground.Visible = false;
                            doomForeground.Visible = false;
                        }
                    }
                    else
                    {
                        doomForeground.OffsetRight = Math.Min(0f, doomOffset + doomForeground.PatchMarginRight);
                        hpForeground.Visible = true;
                    }
                }
                else
                {
                    doomForeground.Visible = false;
                }
            }
            else
            {
                doomForeground.Visible = false;
            }
        }

        private static void PositionGraftStrip(
            NHealthBar healthBar,
            Creature creature,
            int graftHp,
            int visualDenom,
            HealthBarVisualGraftMetrics metrics,
            GraftUiState state)
        {
            var strip = state.Strip;
            if (strip == null)
                return;

            var maxWidth = GetMaxFgWidth(healthBar);
            if (maxWidth <= 0f || visualDenom <= 0)
            {
                strip.Visible = false;
                return;
            }

            var mainWidth = Math.Max(
                (float)creature.CurrentHp / visualDenom * maxWidth,
                creature.CurrentHp > 0 ? 12f : 0f);
            var graftWidth = (float)graftHp / visualDenom * maxWidth;
            if (graftWidth < 0.5f)
            {
                strip.Visible = false;
                return;
            }

            strip.Visible = true;
            strip.Material = metrics.GraftMaterial;
            strip.SelfModulate = metrics.GraftSelfModulate ?? DefaultGraftColor;
            strip.OffsetLeft = mainWidth > 0f ? Math.Max(0f, mainWidth - strip.PatchMarginLeft) : 0f;
            strip.OffsetRight = mainWidth + graftWidth - maxWidth;
        }

        private static float GetMaxFgWidth(NHealthBar healthBar)
        {
            var expectedMaxFgWidth = healthBar._expectedMaxFgWidth;
            return expectedMaxFgWidth > 0f
                ? expectedMaxFgWidth
                : healthBar._hpForegroundContainer.Size.X;
        }

        private static float GetFgWidthForDenom(NHealthBar healthBar, int amount, int visualDenom)
        {
            if (amount <= 0 || visualDenom <= 0)
                return 0f;

            var creature = healthBar._creature;
            var width = (float)amount / visualDenom * GetMaxFgWidth(healthBar);
            return Math.Max(width, creature.CurrentHp > 0 ? 12f : 0f);
        }

        private static bool EnsureGraftStrip(NHealthBar healthBar, out GraftUiState state)
        {
            state = GraftStates.GetOrCreate(healthBar);
            if (state.Strip != null)
                return true;

            if (healthBar._poisonForeground is not NinePatchRect poisonTemplate ||
                poisonTemplate.GetParent() is not Control mask ||
                healthBar._hpForeground is not { } hpForeground)
                return false;

            var strip = (NinePatchRect)poisonTemplate.Duplicate();
            strip.Name = "RitsuVisualGraftStrip";
            strip.Visible = false;
            strip.MouseFilter = Control.MouseFilterEnum.Ignore;
            mask.AddChild(strip);
            var insertAt = Math.Clamp(hpForeground.GetIndex() + 1, 0, mask.GetChildCount() - 1);
            mask.MoveChild(strip, insertAt);

            state.Strip = strip;
            return true;
        }

        private sealed class GraftUiState
        {
            public NinePatchRect? Strip { get; set; }
        }
    }
}
