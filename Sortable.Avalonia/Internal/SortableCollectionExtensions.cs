using System.Collections.Generic;

namespace Sortable.Avalonia.Internal
{
    /// <summary>
    /// Generic helper methods for safe collection mutations during drag/drop operations.
    /// </summary>
    internal static class SortableCollectionExtensions
    {
        public static bool InsertAtIndexOrAdd<T>(this IList<T> collection, int index, T item)
        {
            if (index < 0 || index > collection.Count)
                collection.Add(item);
            else
                collection.Insert(index, item);
            return true;
        }

        public static bool RemoveAtIfInRange<T>(this IList<T> collection, int index)
        {
            if (index >= 0 && index < collection.Count)
            {
                collection.RemoveAt(index);
                return true;
            }
            return false;
        }

        public static bool SwapItems<T>(this IList<T> collection, int index1, int index2)
        {
            if (index1 < 0 || index2 < 0 || index1 >= collection.Count || index2 >= collection.Count)
                return false;
            if (index1 == index2)
                return true;
            (collection[index1], collection[index2]) = (collection[index2], collection[index1]);
            return true;
        }
    }
}
