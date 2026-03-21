using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace Sortable.Avalonia.Internal
{
    /// <summary>
    /// Extension methods for visual tree traversal and control utilities.
    /// </summary>
    internal static class SortableVisualTreeExtensions
    {
        public static T? FindAncestorOfType<T>(this Control visual) where T : class
        {
            var current = visual.Parent as Control;
            while (current != null)
            {
                if (current is T t)
                    return t;
                current = current.Parent as Control;
            }
            return null;
        }

        public static bool IsInteractiveOrigin(this Control sourceVisual, Control dragRoot)
        {
            var current = sourceVisual;
            while (current != null && !ReferenceEquals(current, dragRoot))
            {
                if (current is Button ||
                    current is ToggleButton ||
                    current is TextBox ||
                    current is SelectingItemsControl ||
                    current is Slider ||
                    current is ScrollBar)
                {
                    return true;
                }
                current = current.Parent as Control;
            }
            return false;
        }

        public static bool IsPointerFromDragHandle(this Control sourceVisual, Control dragRoot, Func<Control, bool> isDragHandle)
        {
            var current = sourceVisual;
            while (current != null)
            {
                if (isDragHandle(current))
                {
                    return true;
                }
                if (ReferenceEquals(current, dragRoot))
                {
                    break;
                }
                current = current.Parent as Control;
            }
            return false;
        }
    }
}
