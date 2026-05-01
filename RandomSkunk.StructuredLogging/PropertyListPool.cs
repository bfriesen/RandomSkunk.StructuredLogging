using Microsoft.Extensions.ObjectPool;

namespace RandomSkunk.StructuredLogging;

internal static class PropertyListPool
{
    internal static readonly ObjectPool<List<KeyValuePair<string, object?>>> Instance = ObjectPool.Create(new PropertyListPoolPolicy());

    private class PropertyListPoolPolicy : IPooledObjectPolicy<List<KeyValuePair<string, object?>>>
    {
        private const int _initialCapacity = 4;
        private const int _maximumRetainedCapacity = 16;

        public List<KeyValuePair<string, object?>> Create() => new(_initialCapacity);


        public bool Return(List<KeyValuePair<string, object?>> obj)
        {
            if (obj.Capacity > _maximumRetainedCapacity)
            {
                return false;
            }

            obj.Clear();
            return true;
        }
    }
}
