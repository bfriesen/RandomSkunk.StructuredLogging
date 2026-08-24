using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Mutable state shared by a root <see cref="RootOperationLog"/> and every <see cref="ChildOperationLog"/>
/// nested under it. A single instance backs the whole operation tree so the root can flush everything
/// accumulated anywhere in the tree as exactly one log entry. This type applies no synchronization of its
/// own - concurrent use (e.g. sub-operations run via <c>Task.WhenAll</c>) is only safe when the operation
/// was begun with <c>threadSafe: true</c> (see <see cref="LoggerOperationExtensions"/>), which wraps every
/// <see cref="IOperationLog"/> (root or sub-operation) in a locking decorator instead.
/// <see cref="_journal"/> is rented from <see cref="OperationLogPools"/> and returned there by
/// <see cref="Dispose"/>, called from <see cref="RootOperationLog.DisposeCore"/> - neither must be
/// touched by any <see cref="ChildOperationLog"/> still in scope after the root operation has been
/// disposed, since by then they may have already been handed out to a different, unrelated operation.
/// </summary>
internal sealed class OperationLogState : IDisposable
{
    public readonly ILogger Logger;
    public readonly LogLevel Level;
    public readonly EventId EventId;
    public readonly DateTimeOffset StartTime;
    public readonly Stopwatch Stopwatch;
    public List<KeyValuePair<string, object?>>? Properties;
    private readonly StringBuilder _journal = OperationLogPools.Journals.Rent();

    public object? Result;
    public bool HasResult;
    public Exception? Exception;

    /// <summary>
    /// Set by <see cref="Dispose"/>, once the root operation has finished writing its log entry
    /// and returned <see cref="_journal"/> to <see cref="OperationLogPools"/>. From that point on, the
    /// <see cref="StringBuilder"/> instance may already be in use by a different, unrelated operation, so any
    /// further attempt to use this state (e.g. via a <see cref="ChildOperationLog"/> that leaked out of scope)
    /// must throw instead of touching it.
    /// </summary>
    public bool IsDisposed { get; private set; }

    public OperationLogState(ILogger logger, LogLevel level, EventId eventId, string operationName)
    {
        Logger = logger;
        Level = level;
        EventId = eventId;

        StartTime = DateTimeOffset.Now;

        string operationLine = "Operation: " + operationName;
        string? eventIdLine = eventId != default ? "EventId: " + eventId : null;
        string startTimeLine = string.Create(
            CultureInfo.InvariantCulture,
            $"Start Time: {StartTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        int dashCount = Math.Max(operationLine.Length, Math.Max(eventIdLine?.Length ?? 0, startTimeLine.Length));

        _journal.Append(operationLine).Append('\n');
        if (eventIdLine is not null)
            _journal.Append(eventIdLine).Append('\n');
        _journal.Append(startTimeLine).Append('\n');
        _journal.Append('-', dashCount);
        Stopwatch = Stopwatch.StartNew();
    }

    public void AddProperty(string propertyName, object? value) =>
        (Properties ??= new(capacity: 8)).Add(new(propertyName, value));

    /// <summary>
    /// Appends a newline followed by the "[elapsed] " timestamp prefix that starts every journal line,
    /// without allocating an intermediate string for either the timestamp or the line itself - callers
    /// append the rest of the journal entry content directly to the returned <see cref="StringBuilder"/>
    /// afterward.
    /// </summary>
    /// <returns>The <see cref="StringBuilder"/> to append the rest of the journal entry to.</returns>
    public StringBuilder BeginJournalEntry()
    {
        _journal.Append('\n');
        return AppendTimestamp(Stopwatch.Elapsed);
    }

    public void Dispose()
    {
        OperationLogPools.Journals.Return(_journal);
        IsDisposed = true;
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if the root operation has already been disposed - see
    /// <see cref="IsDisposed"/>. Called by every <see cref="OperationLog{TSelf}"/> member that would
    /// otherwise read or write this shared state.
    /// </summary>
    public void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(IOperationLog), "Cannot use an operation log after the root operation has been disposed.");
    }

    private StringBuilder AppendTimestamp(TimeSpan elapsed) =>
        _journal.Append(CultureInfo.InvariantCulture, $"[{elapsed.TotalSeconds:F3}] ");
}
