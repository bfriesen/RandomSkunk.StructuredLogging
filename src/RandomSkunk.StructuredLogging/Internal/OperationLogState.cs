using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Mutable state shared by a root <see cref="RootOperationLog"/> and every <see cref="ChildOperationLog"/>
/// nested under it. A single instance backs the whole operation tree so that concurrent sub-operations
/// (e.g. run via <c>Task.WhenAll</c>) can safely interleave journal lines and properties under one lock,
/// and so the root can flush everything accumulated anywhere in the tree as exactly one log entry.
/// <see cref="Journal"/> and <see cref="Properties"/> are rented from <see cref="OperationLogPools"/> and
/// returned there by <see cref="RootOperationLog.Dispose"/> - neither must be touched by any
/// <see cref="ChildOperationLog"/> still in scope after the root operation has been disposed, since by
/// then they may have already been handed out to a different, unrelated operation.
/// </summary>
internal sealed class OperationLogState(ILogger logger, LogLevel level, EventId eventId)
{
    public readonly ILogger Logger = logger;
    public readonly LogLevel Level = level;
    public readonly EventId EventId = eventId;
    public readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;
    public readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    public readonly StringBuilder Journal = OperationLogPools.Journals.Rent();
    public readonly List<(string Name, object? Value)> Properties = OperationLogPools.PropertyLists.Rent();

    public object? Result;
    public bool HasResult;
    public Exception? Exception;

    /// <summary>
    /// Appends a timestamped line to the journal. Must be called while holding a lock on this instance.
    /// </summary>
    public void AppendLine(string text)
    {
        StartLine();
        Journal.Append(text);
    }

    /// <summary>
    /// Appends the "[elapsed] " timestamp prefix that starts every journal line, without allocating an
    /// intermediate string for either the timestamp or the line itself - callers append the rest of the
    /// line's content directly to <see cref="Journal"/> afterward. Must be called while holding a lock on
    /// this instance.
    /// </summary>
    public void StartLine()
    {
        if (Journal.Length > 0)
            Journal.Append('\n');

        Span<char> elapsedSeconds = stackalloc char[32];
        Stopwatch.Elapsed.TotalSeconds.TryFormat(elapsedSeconds, out int written, "F3", CultureInfo.InvariantCulture);

        Journal.Append('[').Append(elapsedSeconds[..written]).Append("] ");
    }
}
