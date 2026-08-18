using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A no-op <see cref="IOperationLog"/> returned when the operation's level is disabled on the logger,
/// so a disabled operation does zero journal/property accumulation - mirroring how the interpolated
/// string handlers skip evaluating their holes entirely when disabled. Every member is a no-op, and
/// <see cref="BeginSubOperation"/> returns the same singleton, so the no-op short-circuits through any
/// depth of nesting.
/// </summary>
internal sealed class NullOperationLog : IOperationLog
{
    public static readonly NullOperationLog Instance = new();

    private NullOperationLog()
    {
    }

    public IOperationLog SetException(Exception exception, bool recordEverywhere = false) => this;

    public IOperationLog SetResult<T>(T value) => this;

    public IOperationLog SetProperty<T>(string name, T value) => this;

    public IOperationLog Append(string text) => this;

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public IOperationLog BeginSubOperation(string name) => this;

    public void Dispose()
    {
    }
}
