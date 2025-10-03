using System.Collections.Generic;

namespace Formula81.XrmToolBox.Libraries.Core.Extensions
{
    public static class QueueEx
    {
        public static IEnumerable<T> DequeueChunk<T>(this Queue<T> queue, int chunkSize)
        {
            for (var i = 0; i < chunkSize && queue.Count > 0; i++)
            {
                yield return queue.Dequeue();
            }
        }
    }
}
