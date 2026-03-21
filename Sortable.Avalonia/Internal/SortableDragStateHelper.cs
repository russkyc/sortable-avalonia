using Avalonia.Controls;
using System.Collections;
using Avalonia;
using Avalonia.VisualTree;

namespace Sortable.Avalonia.Internal
{
    /// <summary>
    /// Helper for drag state management.
    /// </summary>
    internal static class SortableDragStateHelper
    {
        public static void ResetDragState(ref object? draggedData, ref ItemsControl? currentItemsControl, ref ItemsControl? originalItemsControl, ref IList? sourceCollection, ref IList? targetCollection, ref string? sourceGroup, ref Panel? currentPanel, ref bool endDragInProgress)
        {
            draggedData = null;
            currentItemsControl = null;
            originalItemsControl = null;
            sourceCollection = null;
            targetCollection = null;
            sourceGroup = null;
            currentPanel = null;
            endDragInProgress = false;
        }
    }
}
