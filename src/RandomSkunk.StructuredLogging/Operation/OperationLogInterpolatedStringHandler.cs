using System.Runtime.CompilerServices;
using System.Text;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Interpolated string handler for the <c>text</c>/<c>operationName</c> parameter of
/// <see cref="IOperationLog.Append(ref OperationLogInterpolatedStringHandler)"/> and
/// <see cref="IOperationLog.BeginSubOperation(ref OperationLogInterpolatedStringHandler)"/>. Building the
/// interpolated content is skipped entirely when <see cref="IOperationLog.IsEnabled"/> is
/// <see langword="false"/>, the same short-circuit the structured-logging message handlers apply for a
/// disabled <see cref="Microsoft.Extensions.Logging.LogLevel"/>.
/// <para>
/// When the operation owns a real journal (<see cref="IJournalOwner"/> - in practice
/// <see cref="RootOperationLog"/>/<see cref="ChildOperationLog"/>), this handler writes directly into it,
/// skipping the throwaway intermediate <see cref="string"/> the non-handler overloads build. Otherwise (in
/// practice a <see cref="SynchronizedOperationLog"/>) it writes into a private buffer rented from
/// <see cref="OperationLogPools.Journals"/> instead - deliberately never the shared journal directly,
/// because the lock that makes concurrent access to it safe can't be acquired here: this handler's
/// constructor and its <c>AppendFormatted</c> calls all run as part of *argument evaluation*, before
/// <see cref="IOperationLog.Append(ref OperationLogInterpolatedStringHandler)"/>'s method body (and
/// therefore any lock taken inside it) ever runs. If a hole's expression were to throw partway through
/// with the lock already held, the method body - and any code that would release it - would never run,
/// deadlocking the operation's shared gate permanently. Writing into a private, thread-local buffer instead
/// means a throw there only discards that buffer, which is never returned to the pool - a bounded,
/// self-healing loss, not a deadlock.
/// </para>
/// </summary>
[InterpolatedStringHandler]
public ref struct OperationLogInterpolatedStringHandler
{
    private StringBuilder.AppendInterpolatedStringHandler _handler;
    private readonly StringBuilder? _directTarget;
    private readonly int _directStartIndex;
    private readonly StringBuilder? _rentedBuilder;

    /// <summary>
    /// Whether the operation was enabled when this handler was constructed - <see langword="false"/> means
    /// none of this handler's interpolated arguments were evaluated.
    /// </summary>
    internal readonly bool IsEnabled;

    /// <summary>
    /// Initializes the handler and checks whether <paramref name="log"/> is enabled.
    /// </summary>
    /// <param name="literalLength">The total number of characters in the interpolated string's literal text.</param>
    /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
    /// <param name="log">The operation log the text is being built for.</param>
    /// <param name="handlerIsValid">
    /// Set to <see langword="false"/> when <paramref name="log"/> is not enabled, so the compiler skips
    /// evaluating and appending the interpolated string's arguments.
    /// </param>
    public OperationLogInterpolatedStringHandler(int literalLength, int formattedCount, IOperationLog log, out bool handlerIsValid)
    {
        IsEnabled = handlerIsValid = log.IsEnabled;

        if (!IsEnabled)
        {
            _handler = default;
            _directTarget = null;
            _directStartIndex = 0;
            _rentedBuilder = null;
            return;
        }

        StringBuilder target;
        if (log is IJournalOwner owner)
        {
            target = owner.BeginJournalEntry();
            _directTarget = target;
            _directStartIndex = target.Length;
            _rentedBuilder = null;
        }
        else
        {
            target = OperationLogPools.Journals.Rent();
            _rentedBuilder = target;
            _directTarget = null;
            _directStartIndex = 0;
        }

        _handler = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount, target);
    }

    /// <summary>
    /// Appends a literal text segment of the interpolated string.
    /// </summary>
    /// <param name="value">The literal text to append.</param>
    public void AppendLiteral(string value) => _handler.AppendLiteral(value);

    /// <summary>
    /// Appends the formatted value of an interpolation expression.
    /// </summary>
    /// <typeparam name="T">The type of the value to append.</typeparam>
    /// <param name="value">The value to format and append.</param>
    public void AppendFormatted<T>(T value) => _handler.AppendFormatted(value);

    /// <summary>
    /// Appends the formatted value of an interpolation expression.
    /// </summary>
    /// <typeparam name="T">The type of the value to append.</typeparam>
    /// <param name="value">The value to format and append.</param>
    /// <param name="format">A standard or custom format string supported by <paramref name="value"/>'s type.</param>
    public void AppendFormatted<T>(T value, string? format) => _handler.AppendFormatted(value, format);

    /// <summary>
    /// Appends the formatted value of an interpolation expression.
    /// </summary>
    /// <typeparam name="T">The type of the value to append.</typeparam>
    /// <param name="value">The value to format and append.</param>
    /// <param name="alignment">The minimum number of characters the formatted value should occupy; positive values right-align with padding, negative values left-align with padding.</param>
    public void AppendFormatted<T>(T value, int alignment) => _handler.AppendFormatted(value, alignment);

    /// <summary>
    /// Appends the formatted value of an interpolation expression.
    /// </summary>
    /// <typeparam name="T">The type of the value to append.</typeparam>
    /// <param name="value">The value to format and append.</param>
    /// <param name="alignment">The minimum number of characters the formatted value should occupy; positive values right-align with padding, negative values left-align with padding.</param>
    /// <param name="format">A standard or custom format string supported by <paramref name="value"/>'s type.</param>
    public void AppendFormatted<T>(T value, int alignment, string? format) => _handler.AppendFormatted(value, alignment, format);

    /// <summary>
    /// Appends a string interpolation value.
    /// </summary>
    /// <param name="value">The string to append.</param>
    public void AppendFormatted(string? value) => _handler.AppendFormatted(value);

    /// <summary>
    /// Appends a string interpolation value.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <param name="alignment">The minimum number of characters the formatted value should occupy; positive values right-align with padding, negative values left-align with padding.</param>
    public void AppendFormatted(string? value, int alignment) => _handler.AppendFormatted(value, alignment);

    /// <summary>
    /// The real journal this handler wrote directly into, or <see langword="null"/> if it wrote into
    /// <see cref="RentedBuilder"/> instead.
    /// </summary>
    internal readonly StringBuilder? DirectTarget => _directTarget;

    /// <summary>
    /// The index within <see cref="DirectTarget"/> where this handler's content starts - only meaningful
    /// when <see cref="DirectTarget"/> is not <see langword="null"/>.
    /// </summary>
    internal readonly int DirectStartIndex => _directStartIndex;

    /// <summary>
    /// The pooled buffer this handler wrote into because the operation didn't own a real journal to write
    /// into directly, or <see langword="null"/> if it used <see cref="DirectTarget"/> instead. The caller is
    /// responsible for returning it to <see cref="OperationLogPools.Journals"/> once its content has been
    /// used.
    /// </summary>
    internal readonly StringBuilder? RentedBuilder => _rentedBuilder;
}
