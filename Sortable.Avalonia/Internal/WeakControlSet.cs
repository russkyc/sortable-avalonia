using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Sortable.Avalonia.Internal;

/// <summary>
/// A set of <see cref="ItemsControl"/> instances that does not keep them alive.
/// Used to track sortable controls without preventing detached views from being collected.
/// </summary>
internal sealed class WeakControlSet
{
    private static readonly object Sentinel = new();
    private readonly ConditionalWeakTable<ItemsControl, object> _table = new();

    /// <summary>
    /// Adds the control if it is not already tracked.
    /// </summary>
    /// <returns><c>true</c> when the control was newly added; otherwise <c>false</c>.</returns>
    public bool Add(ItemsControl control)
    {
        if (_table.TryGetValue(control, out _))
        {
            return false;
        }

        _table.Add(control, Sentinel);
        return true;
    }

    public bool Remove(ItemsControl control) => _table.Remove(control);

    public bool Contains(ItemsControl control) => _table.TryGetValue(control, out _);
}
