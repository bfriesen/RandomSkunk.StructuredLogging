using System.Text;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// The pools <see cref="OperationLogState"/> rents its journal <see cref="StringBuilder"/> and property
/// list from, and <see cref="RootOperationLog.Dispose"/> returns them to once an operation's single log
/// entry has been flushed.
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

    // Most operations set only a handful of properties, so cap what's retained to avoid keeping an
    // oversized backing array around from one property-heavy operation.
    public static readonly ObjectPool<List<(string Name, object? Value)>> PropertyLists = new(
        () => new(),
        list => list.Clear(),
        list => list.Capacity <= 32);
}
