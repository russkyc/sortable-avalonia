using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using System.Collections.Generic;

namespace Sortable.Avalonia.Internal
{
    /// <summary>
    /// Helper for layout and slot calculations for sortable controls.
    /// </summary>
    internal static class SortableLayoutHelper
    {
        public static void CacheLayoutSlots(Panel panel, Control container, ItemsControl itemsControl, List<Rect> slotBounds, List<ContentPresenter> logicalChildren)
        {
            slotBounds.Clear();
            logicalChildren.Clear();
            foreach (var item in itemsControl.Items)
            {
                var cp = itemsControl.ContainerFromIndex(itemsControl.Items.IndexOf(item)) as ContentPresenter;
                if (cp != null)
                {
                    logicalChildren.Add(cp);
                    var bounds = cp.Bounds;
                    slotBounds.Add(bounds);
                }
            }
        }
    }
}
