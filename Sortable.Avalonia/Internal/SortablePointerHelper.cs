using Avalonia.Controls;
using Avalonia.Input;

namespace Sortable.Avalonia.Internal
{
    /// <summary>
    /// Helper for pointer and event handler logic.
    /// </summary>
    internal static class SortablePointerHelper
    {
        public static void CapturePointer(Control control, PointerEventArgs e)
        {
            e.Pointer.Capture(control);
            e.Handled = true;
        }
    }
}

