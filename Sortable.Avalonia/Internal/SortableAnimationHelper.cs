using Avalonia.Controls;

namespace Sortable.Avalonia.Internal
{
    /// <summary>
    /// Helper for drag animation and visual reset logic.
    /// </summary>
    internal static class SortableAnimationHelper
    {
        public static void ResetDraggedElementVisuals(Control? dragProxy, Control? originalElement, double originalOpacity)
        {
            if (dragProxy != null)
            {
                dragProxy.IsVisible = false;
            }
            if (originalElement != null)
            {
                originalElement.Opacity = originalOpacity;
            }
        }
    }
}
