namespace RandomSkunk.StructuredLogging;

/// <summary>
/// A no-op <see cref="ISubOperationLog"/> returned when the operation's level is disabled on the logger,
/// so a disabled operation does zero journal/property accumulation - mirroring how the interpolated
/// string handlers skip evaluating their holes entirely when disabled. Every member is a no-op, and
/// <see cref="BeginSubOperation"/> returns the same singleton, so the no-op short-circuits through any
/// depth of nesting.
/// </summary>
internal sealed class NullOperationLog : ISubOperationLog
{
    public static readonly NullOperationLog Instance = new();

    private NullOperationLog()
    {
    }

    public ISubOperationLog SetProperty<T>(string name, T value) => this;

    IOperationLog IOperationLog.SetProperty<T>(string name, T value) => this;

    public ISubOperationLog Append(string text) => this;

    IOperationLog IOperationLog.Append(string text) => this;

    public ISubOperationLog BeginSubOperation(string name) => this;

    public ISubOperationLog SetException(Exception exception) => this;

    IOperationLog IOperationLog.SetException(Exception exception) => this;

    public ISubOperationLog SetException(Exception exception, bool propagateToRoot) => this;

    public ISubOperationLog SetResult<T>(T value) => this;

    IOperationLog IOperationLog.SetResult<T>(T value) => this;

    public void Dispose()
    {
    }
}
