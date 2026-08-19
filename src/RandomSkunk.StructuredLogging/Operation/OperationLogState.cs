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
/// <see cref="RootOperationLog.DisposeCore"/> - neither must be touched by any
/// <see cref="ChildOperationLog"/> still in scope after the root operation has been disposed, since by
/// then they may have already been handed out to a different, unrelated operation.
/// </summary>
internal sealed class OperationLogState
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

    public OperationLogState(ILogger logger, LogLevel level, EventId eventId, string name)
    {
        Logger = logger;
        Level = level;
        EventId = eventId;

        StartTime = DateTimeOffset.Now;
        _journal.Append(name).Append('\n').Append('-', name.Length).Append('\n');
        AppendTimestamp(TimeSpan.Zero).Append(
            CultureInfo.InvariantCulture,
            $"Operation started at {StartTime:yyyy-MM-dd HH:mm:ss.fff zzz}.");
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

    public void ReturnJournalToPool()
    {
        OperationLogPools.Journals.Return(_journal);
    }

    private StringBuilder AppendTimestamp(TimeSpan elapsed) =>
        _journal.Append(CultureInfo.InvariantCulture, $"[{elapsed.TotalSeconds:F3}] ");
}
