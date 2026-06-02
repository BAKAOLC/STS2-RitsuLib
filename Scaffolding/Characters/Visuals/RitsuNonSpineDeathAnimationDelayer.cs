using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace STS2RitsuLib.Scaffolding.Characters.Visuals
{
    internal partial class RitsuNonSpineDeathAnimationDelayer : Node, IDeathDelayer
    {
        private const float MaxDelaySeconds = 30f;
        private const string NodeName = "RitsuNonSpineDeathAnimationDelayer";
        private static readonly ConditionalWeakTable<NCreature, DelaySlot> Delays = new();
        private readonly Task _delayTask;

        private RitsuNonSpineDeathAnimationDelayer(float seconds)
        {
            Name = NodeName;
            _delayTask = Cmd.Wait(Math.Clamp(seconds, 0f, MaxDelaySeconds), true);
        }

        public RitsuNonSpineDeathAnimationDelayer()
        {
            _delayTask = Task.CompletedTask;
        }

        public Task GetDelayTask()
        {
            return _delayTask;
        }

        internal static void Install(NCreature creature, float seconds)
        {
            if (seconds <= 0f || !float.IsFinite(seconds) || !IsInstanceValid(creature))
                return;

            seconds = Math.Clamp(seconds, 0f, MaxDelaySeconds);
            Delays.GetValue(creature, _ => new()).Seconds = seconds;

            if (creature.GetNodeOrNull(NodeName) is RitsuNonSpineDeathAnimationDelayer)
                return;

            creature.AddChild(new RitsuNonSpineDeathAnimationDelayer(seconds));
        }

        internal static bool TryGetDelaySeconds(NCreature creature, out float seconds)
        {
            seconds = 0f;
            if (!IsInstanceValid(creature) || !Delays.TryGetValue(creature, out var slot))
                return false;

            seconds = slot.Seconds;
            return seconds > 0f && float.IsFinite(seconds);
        }

        private sealed class DelaySlot
        {
            public float Seconds { get; set; }
        }
    }
}
