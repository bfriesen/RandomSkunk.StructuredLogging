namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Wraps a sub-operation <see cref="ISubOperationLog"/> (normally a <see cref="ChildOperationLog"/>, though
/// this decorator doesn't depend on that) so every member is synchronized on a shared <c>gate</c> - the same
/// one the root <see cref="SynchronizedOperationLog"/> it descends from uses. See
/// <see cref="SynchronizedOperationLogBase{TOperationLog}"/> for the shared implementation; this class adds
/// nothing beyond it, since a sub-operation has no members beyond what <see cref="ISubOperationLog"/>
/// already declares.
/// </summary>
internal sealed class SynchronizedSubOperationLog(ISubOperationLog inner, object gate)
    : SynchronizedOperationLogBase<ISubOperationLog>(inner, gate);
