namespace STS2RitsuLib.Interop.AutoRegistration
{
    internal sealed class AutoRegistrationOperationComparer : IComparer<AutoRegistrationOperation>
    {
        public static AutoRegistrationOperationComparer Instance { get; } = new();

        public int Compare(AutoRegistrationOperation? x, AutoRegistrationOperation? y)
        {
            return CompareCore(x, y);
        }

        public static int CompareCore(AutoRegistrationOperation? x, AutoRegistrationOperation? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            var result = StringComparer.OrdinalIgnoreCase.Compare(x.OwnerModId, y.OwnerModId);
            if (result != 0)
                return result;

            result = StringComparer.Ordinal.Compare(
                x.SourceAssembly.FullName ?? x.SourceAssembly.GetName().Name ?? string.Empty,
                y.SourceAssembly.FullName ?? y.SourceAssembly.GetName().Name ?? string.Empty);
            if (result != 0)
                return result;

            result = x.Phase.CompareTo(y.Phase);
            if (result != 0)
                return result;

            result = x.Order.CompareTo(y.Order);
            if (result != 0)
                return result;

            result = StringComparer.Ordinal.Compare(x.SourceType.FullName ?? x.SourceType.Name,
                y.SourceType.FullName ?? y.SourceType.Name);
            return result != 0 ? result : StringComparer.Ordinal.Compare(x.Signature, y.Signature);
        }
    }
}
