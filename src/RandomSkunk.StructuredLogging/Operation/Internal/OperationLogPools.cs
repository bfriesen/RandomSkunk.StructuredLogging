using System.Text;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// The pools <see cref="OperationLogState"/> rents its journal <see cref="StringBuilder"/> and property
/// list from (returned by <see cref="OperationLogState.ReturnJournalToPool"/> once an operation's single log entry has
/// been flushed), and <see cref="ValueFormatting.AppendJson{T}"/> rents its scratch
/// <see cref="PooledJsonWriter"/> from.
/// </summary>
internal static class OperationLogPools
{
    // A journal that grew past 4K chars came from an unusually long-running or chatty operation -
    // retaining its backing array would waste memory on every future rental for the (common) case of a
    // much shorter journal, so let it be collected instead of pooled.
    public static readonly ObjectPool<StringBuilder> Journals = new(
        () => new StringBuilder(),
        sb => sb.Clear(),
        sb => sb.Capacity <= 4096);

    // Same reasoning as Journals: don't retain the backing byte buffer from one unusually large
    // AppendJson call.
    public static readonly ObjectPool<PooledJsonWriter> JsonWriters = new(
        () => new PooledJsonWriter(),
        writer => writer.Reset(),
        writer => writer.Capacity <= 16384);
}
