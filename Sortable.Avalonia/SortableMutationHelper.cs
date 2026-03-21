// MIT License
// 
// Copyright (c) 2026 Russell Camo (russkyc)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using Sortable.Avalonia.Internal;

namespace Sortable.Avalonia;

/// <summary>
/// Applies sortable mutation intents from command event args.
/// Use this helper from view model command handlers to keep mutations explicit and MVVM-friendly.
/// </summary>
public static class SortableMutationHelper
{
    /// <summary>
    /// Applies a same-collection update intent.
    /// </summary>
    /// <returns>True when a mutation is applied; otherwise false.</returns>
    public static bool ApplyUpdateMutation(this SortableUpdateEventArgs e)
    {
        if (e.SourceCollection == null || e.SourceCollection.Count == 0)
        {
            return false;
        }

        var oldIndex = e.OldIndex;
        if (oldIndex < 0 || oldIndex >= e.SourceCollection.Count)
        {
            return false;
        }

        var newIndex = Math.Max(0, Math.Min(e.NewIndex, e.SourceCollection.Count - 1));
        if (newIndex == oldIndex)
        {
            return false;
        }

        if (e.Mode == SortableMode.Swap)
        {
            return SortableCollectionHelper.SwapItemsInCollection(e.SourceCollection, oldIndex, newIndex);
        }

        var movedItem = e.SourceCollection[oldIndex];
        e.SourceCollection.RemoveAt(oldIndex);

        if (newIndex > e.SourceCollection.Count)
        {
            newIndex = e.SourceCollection.Count;
        }

        e.SourceCollection.Insert(newIndex, movedItem);
        return true;
    }

    /// <summary>
    /// Applies a cross-collection drop intent.
    /// </summary>
    /// <returns>True when a mutation is applied; otherwise false.</returns>
    public static bool ApplyDropMutation(this SortableDropEventArgs e)
    {
        if (!e.IsAccepted || e.SourceCollection == null || e.TargetCollection == null)
        {
            Console.WriteLine($"[MUTATION DEBUG] Drop rejected or missing collections. IsAccepted: {e.IsAccepted}, Source: {e.SourceCollection}, Target: {e.TargetCollection}");
            return false;
        }

        var itemToInsert = e.GetItemToInsert();
        if (itemToInsert == null)
        {
            Console.WriteLine("[MUTATION DEBUG] Item to insert is null.");
            return false;
        }

        Console.WriteLine($"[MUTATION DEBUG] TransferMode: {e.TransferMode}, Source.Count: {e.SourceCollection.Count}, Target.Count: {e.TargetCollection.Count}, OldIndex: {e.OldIndex}, NewIndex: {e.NewIndex}, Item: {itemToInsert}");

        if (e.TransferMode == SortableTransferMode.Swap)
        {
            return ApplyCrossCollectionSwap(e.SourceCollection, e.TargetCollection, e.OldIndex, e.NewIndex, itemToInsert);
        }

        var inserted = SortableCollectionHelper.InsertAtIndexOrAdd(e.TargetCollection, e.NewIndex, itemToInsert);

        if (e.TransferMode == SortableTransferMode.Move)
        {
            var removed = SortableCollectionHelper.RemoveAtIfInRange(e.SourceCollection, e.OldIndex);
            return inserted && removed;
        }

        return inserted;
    }

    private static bool ApplyCrossCollectionSwap(IList source, IList target, int sourceIndex, int targetIndex, object sourceItem)
    {
        if (sourceIndex < 0 || sourceIndex >= source.Count)
        {
            return false;
        }

        if (target.Count == 0)
        {
            var inserted = SortableCollectionHelper.InsertAtIndexOrAdd(target, targetIndex, sourceItem);
            var removed = SortableCollectionHelper.RemoveAtIfInRange(source, sourceIndex);
            return inserted && removed;
        }

        var boundedTargetIndex = Math.Max(0, Math.Min(targetIndex, target.Count - 1));
        var targetItem = target[boundedTargetIndex];

        target.RemoveAt(boundedTargetIndex);
        source.RemoveAt(sourceIndex);

        target.Insert(boundedTargetIndex, sourceItem);
        source.Insert(sourceIndex, targetItem);
        return true;
    }
}
