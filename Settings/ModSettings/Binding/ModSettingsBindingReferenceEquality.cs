using System.Runtime.CompilerServices;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsBindingReferenceEquality
    {
        internal static readonly IEqualityComparer<IModSettingsBinding> Instance = new Comparer();

        private sealed class Comparer : IEqualityComparer<IModSettingsBinding>
        {
            public bool Equals(IModSettingsBinding? x, IModSettingsBinding? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IModSettingsBinding obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
