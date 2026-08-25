using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Localization
{
    internal sealed class I18NLocTableRegistration : IDisposable
    {
        private LocTable _currentTable;
        private int _disposed;

        internal I18NLocTableRegistration(string tableId, I18N i18N)
        {
            TableId = tableId;
            I18N = i18N;
            _currentTable = I18NLocTable.Create(tableId, i18N);
            i18N.Changed += Refresh;
            i18N.Disposed += OnSourceDisposed;

            if (!i18N.IsDisposed)
                return;

            Dispose();
            throw new ObjectDisposedException(nameof(i18N));
        }

        internal I18N I18N { get; }

        internal LocTable CurrentTable => Volatile.Read(ref _currentTable);

        private string TableId { get; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            I18N.Changed -= Refresh;
            I18N.Disposed -= OnSourceDisposed;
        }

        private void Refresh()
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            I18NLocTable replacement;
            try
            {
                replacement = I18NLocTable.Create(TableId, I18N);
            }
            catch (ObjectDisposedException)
            {
                I18NLocTableBridge.RemoveIfCurrent(TableId, this);
                return;
            }

            if (Volatile.Read(ref _disposed) == 0)
                Volatile.Write(ref _currentTable, replacement);
        }

        private void OnSourceDisposed(I18N _)
        {
            I18NLocTableBridge.RemoveIfCurrent(TableId, this);
        }
    }
}
