using System.Collections;
using Avalonia.Controls;

namespace Sortable.Avalonia;

/// <summary>
/// Event args for out-of-bounds release operations.
/// Fired when an item is released outside any valid ItemsControl.
/// </summary>
public class SortableReleaseEventArgs
{
    /// <summary>
    /// The item being dragged.
    /// </summary>
    public object? Item { get; set; }

    /// <summary>
    /// The original collection from which the item was dragged.
    /// </summary>
    public IList? SourceCollection { get; set; }

    /// <summary>
    /// The original index of the item in the source collection.
    /// </summary>
    public int OldIndex { get; set; }

    /// <summary>
    /// Optional: The ItemsControl from which the item originated.
    /// </summary>
    public ItemsControl? SourceItemsControl { get; set; }
}


