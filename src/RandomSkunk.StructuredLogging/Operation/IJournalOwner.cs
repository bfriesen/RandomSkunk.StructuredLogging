using System.Text;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Implemented by <see cref="IOperationLog"/> implementations that own a real journal
/// <see cref="StringBuilder"/> - <see cref="RootOperationLog"/> and <see cref="ChildOperationLog"/> (via
/// <see cref="OperationLog{TSelf}"/>) - so that <see cref="OperationLogInterpolatedStringHandler"/> can
/// write directly into it instead of building a separate, throwaway string first. Deliberately not
/// implemented by <see cref="SynchronizedOperationLog"/>: writing into a shared journal has to happen
/// under its lock, which can't be guaranteed during interpolated-string-handler argument evaluation (see
/// <see cref="OperationLogInterpolatedStringHandler"/>'s doc comment), so it always falls back to a
/// pooled, private buffer instead - the same fallback any future <see cref="IOperationLog"/>
/// implementation that isn't <see cref="IJournalOwner"/> gets automatically.
/// </summary>
internal interface IJournalOwner
{
    /// <summary>
    /// Appends a newline and the "[elapsed] " timestamp prefix that starts every journal line, then
    /// returns the journal <see cref="StringBuilder"/> so the caller can append the rest of the entry
    /// directly to it - see <see cref="OperationLogState.BeginJournalEntry"/>.
    /// </summary>
    StringBuilder BeginJournalEntry();
}
