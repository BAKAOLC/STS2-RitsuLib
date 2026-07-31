using System.Runtime.InteropServices;

namespace STS2RitsuLib
{
    internal static class RitsuLibExceptionPolicy
    {
        internal static bool IsRecoverable(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception switch
            {
                OutOfMemoryException
                    or StackOverflowException
                    or AccessViolationException
                    or SEHException
                    or OperationCanceledException
                    or ThreadInterruptedException => false,
                AggregateException aggregate => aggregate.InnerExceptions.All(IsRecoverable),
                { InnerException: { } innerException } => IsRecoverable(innerException),
                _ => true,
            };
        }
    }
}
