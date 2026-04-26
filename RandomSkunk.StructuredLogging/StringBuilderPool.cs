using Microsoft.Extensions.ObjectPool;
using System.Text;

namespace RandomSkunk.StructuredLogging;

internal static class StringBuilderPool
{
    internal static readonly ObjectPool<StringBuilder> Instance = ObjectPool.Create(new StringBuilderPooledObjectPolicy());
}
