using System.Runtime.InteropServices;

namespace STS2RitsuLib
{
    internal static class RitsuLibExceptionPolicy
    {
        internal static bool IsRecoverable(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (exception is OutOfMemoryException
                or StackOverflowException
                or AccessViolationException
                or SEHException
                or OperationCanceledException
                or ThreadInterruptedException)
                return false;

            if (exception is AggregateException aggregate)
                return aggregate.InnerExceptions.All(IsRecoverable);

            return exception.InnerException == null || IsRecoverable(exception.InnerException);
        }
    }
}
