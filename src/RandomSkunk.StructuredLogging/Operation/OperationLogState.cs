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
/// <see cref="ISubOperationLog"/> (root or sub-operation) in a locking decorator instead.
/// <see cref="_journal"/> is rented from <see cref="OperationLogPools"/> and returned there by
/// <see cref="Dispose"/>, called from <see cref="RootOperationLog.DisposeCore"/> - neither must be
/// touched by any <see cref="ChildOperationLog"/> still in scope after the root operation has been
/// disposed, since by then they may have already been handed out to a different, unrelated operation.
/// </summary>
internal sealed class OperationLogState : IDisposable
{
    public readonly ILogger Logger;
    public LogLevel Level { get; private set; }
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

        // Each header line is appended straight to the journal and measured in place, rather than being
        // built as its own string first just so the dashed rule can be sized to the longest of them - the
        // line's length is simply how far the journal grew while writing it.
        int dashCount = 0;

        _journal.Append("Operation: ").Append(operationName);
        dashCount = EndHeaderLine(_journal, dashCount, lineStart: 0);

        if (eventId != default)
        {
            int lineStart = _journal.Length;
            _journal.Append("EventId: ");

            // EventId.ToString() is `Name ?? Id.ToString(InvariantCulture)`; spelling that out here keeps
            // the same text without allocating it as a string on the way in.
            if (eventId.Name is string eventIdName)
                _journal.Append(eventIdName);
            else
                _journal.Append(CultureInfo.InvariantCulture, $"{eventId.Id}");

            dashCount = EndHeaderLine(_journal, dashCount, lineStart);
        }

        int startTimeLineStart = _journal.Length;
        _journal.Append(CultureInfo.InvariantCulture, $"Start Time: {StartTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        dashCount = EndHeaderLine(_journal, dashCount, startTimeLineStart);

        _journal.Append('-', dashCount);
        Stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Terminates a header line just written to <paramref name="journal"/> starting at
    /// <paramref name="lineStart"/>, returning the running longest-line length the dashed rule under the
    /// header is sized from.
    /// </summary>
    private static int EndHeaderLine(StringBuilder journal, int dashCount, int lineStart)
    {
        int lineLength = journal.Length - lineStart;
        journal.Append('\n');
        return Math.Max(dashCount, lineLength);
    }

    public void AddProperty(string propertyName, object? value) =>
        (Properties ??= new(capacity: 8)).Add(new(propertyName, value));

    /// <summary>
    /// Raises <see cref="Level"/> to <paramref name="level"/> if it's more severe than the operation's
    /// current level, leaving it unchanged otherwise - see <see cref="ISubOperationLog.Escalate"/>.
    /// </summary>
    /// <returns>
    /// The level <see cref="Level"/> was set to just before this call - the same as the new
    /// <see cref="Level"/> (i.e. nothing changed) when this call was a no-op, or the previous, less
    /// severe level when it actually escalated. Callers can compare the return value against the new
    /// <see cref="Level"/> to tell which happened.
    /// </returns>
    public LogLevel Escalate(LogLevel level)
    {
        LogLevel previousLevel = Level;
        if (level > previousLevel)
            Level = level;
        return previousLevel;
    }

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
    /// <see cref="IsDisposed"/>. Called by every <see cref="OperationLog{TOperationLog}"/> member that would
    /// otherwise read or write this shared state.
    /// </summary>
    public void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(ISubOperationLog), "Cannot use an operation log after the root operation has been disposed.");
    }

    private StringBuilder AppendTimestamp(TimeSpan elapsed) =>
        _journal.Append(CultureInfo.InvariantCulture, $"[{elapsed.TotalSeconds:F3}] ");
}
