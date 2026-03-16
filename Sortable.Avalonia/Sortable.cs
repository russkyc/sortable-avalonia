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

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media.Imaging;

namespace Sortable.Avalonia;

/// <summary>
/// Provides attached properties and drag-and-drop sorting behavior for <see cref="ItemsControl"/>.
/// </summary>
public partial class Sortable
{
    /// <summary>
    /// Enables sortable behavior on an <see cref="ItemsControl"/>.
    /// </summary>
    public static readonly AttachedProperty<bool> SortableProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, bool>("Sortable");

    /// <summary>
    /// Gets whether sortable behavior is enabled for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static bool GetSortable(ItemsControl element) => element.GetValue(SortableProperty);

    /// <summary>
    /// Sets whether sortable behavior is enabled for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetSortable(ItemsControl element, bool value) => element.SetValue(SortableProperty, value);

    /// <summary>
    /// Group name used to restrict cross-collection drag/drop targets.
    /// </summary>
    public static readonly AttachedProperty<string?> GroupProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, string?>("Group");

    /// <summary>
    /// Gets the drag/drop group for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static string? GetGroup(ItemsControl element) => element.GetValue(GroupProperty);

    /// <summary>
    /// Sets the drag/drop group for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetGroup(ItemsControl element, string? value) => element.SetValue(GroupProperty, value);

    /// <summary>
    /// Command invoked for same-collection reorder updates.
    /// </summary>
    public static readonly AttachedProperty<ICommand?> UpdateCommandProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, ICommand?>("UpdateCommand");

    /// <summary>
    /// Gets the update command for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static ICommand? GetUpdateCommand(ItemsControl element) => element.GetValue(UpdateCommandProperty);

    /// <summary>
    /// Sets the update command for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetUpdateCommand(ItemsControl element, ICommand? value) =>
        element.SetValue(UpdateCommandProperty, value);

    /// <summary>
    /// Legacy command invoked for cross-collection transfers.
    /// </summary>
    public static readonly AttachedProperty<ICommand?> TransferCommandProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, ICommand?>("TransferCommand");

    /// <summary>
    /// Gets the transfer command for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static ICommand? GetTransferCommand(ItemsControl element) => element.GetValue(TransferCommandProperty);

    /// <summary>
    /// Sets the transfer command for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetTransferCommand(ItemsControl element, ICommand? value) =>
        element.SetValue(TransferCommandProperty, value);
    
    /// <summary>
    /// Enables binding to a command that fires when an item is released outside any valid ItemsControl.
    /// </summary>
    public static readonly AttachedProperty<ICommand?> ReleaseCommandProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, ICommand?>("ReleaseCommand");

    /// <summary>
    /// Gets the release command for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static ICommand? GetReleaseCommand(ItemsControl element) => element.GetValue(ReleaseCommandProperty);

    /// <summary>
    /// Sets the release command for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetReleaseCommand(ItemsControl element, ICommand? value) =>
        element.SetValue(ReleaseCommandProperty, value);

    /// <summary>
    /// Enables an <see cref="ItemsControl"/> as a valid cross-collection drop target.
    /// </summary>
    public static readonly AttachedProperty<bool> DroppableProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, bool>("Droppable");

    /// <summary>
    /// Gets whether cross-collection dropping is enabled for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static bool GetDroppable(ItemsControl element) => element.GetValue(DroppableProperty);

    /// <summary>
    /// Sets whether cross-collection dropping is enabled for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetDroppable(ItemsControl element, bool value) => element.SetValue(DroppableProperty, value);

    /// <summary>
    /// Command invoked for reversible drop operations.
    /// </summary>
    public static readonly AttachedProperty<ICommand?> DropCommandProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, ICommand?>("DropCommand");

    /// <summary>
    /// Gets the drop command for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static ICommand? GetDropCommand(ItemsControl element) => element.GetValue(DropCommandProperty);

    /// <summary>
    /// Sets the drop command for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetDropCommand(ItemsControl element, ICommand? value) =>
        element.SetValue(DropCommandProperty, value);

    /// <summary>
    /// Controls reordering behavior when dragging within the same collection.
    /// </summary>
    public static readonly AttachedProperty<SortableMode> ModeProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, SortableMode>("Mode", defaultValue: SortableMode.Sort);

    /// <summary>
    /// Gets the sortable mode for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static SortableMode GetMode(ItemsControl element) => element.GetValue(ModeProperty);

    /// <summary>
    /// Sets the sortable mode for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetMode(ItemsControl element, SortableMode value) => element.SetValue(ModeProperty, value);

    /// <summary>
    /// Specifies the default transfer mode for cross-collection drops.
    /// Used to enable swap preview animation during drag.
    /// </summary>
    public static readonly AttachedProperty<SortableTransferMode> CrossCollectionTransferModeProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, SortableTransferMode>("CrossCollectionTransferMode", defaultValue: SortableTransferMode.Move);

    /// <summary>
    /// Gets the cross-collection transfer mode for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static SortableTransferMode GetCrossCollectionTransferMode(ItemsControl element) => element.GetValue(CrossCollectionTransferModeProperty);

    /// <summary>
    /// Sets the cross-collection transfer mode for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetCrossCollectionTransferMode(ItemsControl element, SortableTransferMode value) => element.SetValue(CrossCollectionTransferModeProperty, value);

    /// <summary>
    /// Enables same-collection sorting from an item interaction source.
    /// </summary>
    public static readonly AttachedProperty<bool> IsSortableProperty =
        AvaloniaProperty.RegisterAttached<Sortable, Control, bool>("IsSortable");

    /// <summary>
    /// Gets whether item-level sorting is enabled on the specified control.
    /// </summary>
    public static bool GetIsSortable(Control element) => element.GetValue(IsSortableProperty);

    /// <summary>
    /// Sets whether item-level sorting is enabled on the specified control.
    /// </summary>
    public static void SetIsSortable(Control element, bool value) => element.SetValue(IsSortableProperty, value);

    /// <summary>
    /// Enables item-level participation in cross-collection drop interactions.
    /// </summary>
    public static readonly AttachedProperty<bool> IsDroppableProperty =
        AvaloniaProperty.RegisterAttached<Sortable, Control, bool>("IsDroppable");

    /// <summary>
    /// Gets whether item-level dropping is enabled on the specified control.
    /// </summary>
    public static bool GetIsDroppable(Control element) => element.GetValue(IsDroppableProperty);

    /// <summary>
    /// Sets whether item-level dropping is enabled on the specified control.
    /// </summary>
    public static void SetIsDroppable(Control element, bool value) => element.SetValue(IsDroppableProperty, value);

    /// <summary>
    /// Marks a control inside an item template as a drag handle.
    /// When one or more handles are present in the item visual, drag can only start from those handles.
    /// </summary>
    public static readonly AttachedProperty<bool> IsDragHandleProperty =
        AvaloniaProperty.RegisterAttached<Sortable, Control, bool>("IsDragHandle");

    /// <summary>
    /// Gets whether the specified control is a drag handle.
    /// </summary>
    public static bool GetIsDragHandle(Control element) => element.GetValue(IsDragHandleProperty);

    /// <summary>
    /// Sets whether the specified control is a drag handle.
    /// </summary>
    public static void SetIsDragHandle(Control element, bool value) => element.SetValue(IsDragHandleProperty, value);

    /// <summary>
    /// Duration in milliseconds used for all sortable animations including:
    /// - Interactive drag preview animations (items shifting during drag)
    /// - Programmatic layout animations (collection changes)
    /// - Cross-collection travel animations
    /// </summary>
    public static readonly AttachedProperty<int> AnimationDurationProperty =
        AvaloniaProperty.RegisterAttached<Sortable, ItemsControl, int>(
            "AnimationDuration",
            defaultValue: DefaultAnimationDurationMs);

    /// <summary>
    /// Gets the animation duration in milliseconds for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static int GetAnimationDuration(ItemsControl element) =>
        NormalizeAnimationDuration(element.GetValue(AnimationDurationProperty), DefaultAnimationDurationMs);

    /// <summary>
    /// Sets the animation duration in milliseconds for the specified <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetAnimationDuration(ItemsControl element, int value) =>
        element.SetValue(
            AnimationDurationProperty,
            NormalizeAnimationDuration(value, DefaultAnimationDurationMs));

    private const double PlaceholderOpacity = 0.5;
    private const int DefaultAnimationDurationMs = 250;
    private const int MinimumAnimationDurationMs = 1;
    private const int ProgrammaticPairingWindowMs = 750;

    // Tracks Sortable-enabled ItemsControls so programmatic ItemsSource changes can be animated.
    private static readonly HashSet<ItemsControl> TrackedItemsControls = new();
    private static readonly Dictionary<ItemsControl, NotifyCollectionChangedEventHandler> CollectionHandlers = new();
    private static readonly Dictionary<ItemsControl, INotifyCollectionChanged> ObservedCollections = new();
    private static readonly Dictionary<ItemsControl, Dictionary<object, Rect>> LastStableBounds = new();
    private static readonly List<ProgrammaticRemovalSnapshot> PendingProgrammaticRemovals = new();

    // STATE
    private static bool _isDragging;
    private static bool _outsideDroppableAndSortableBounds;
    private static Control? _draggedElement;
    private static Point _dragStartPoint;
    private static Canvas? _overlayCanvas; // Canvas for drag preview
    private static Border? _dragProxy; // Content inside the overlay
    private static ContentPresenter? _crossCollectionPlaceholder; // Uses target ItemTemplate for cross-collection preview
    private static RenderTargetBitmap? _dragProxyBitmap; // Snapshot used by the drag proxy
    private static Point _dragProxyOffset; // Offset from pointer to proxy top-left

    // PROXY PREVIEW STATE
    private static int _originalIndex;
    private static int _originalSourceIndex; // Track the original index in source collection before cross-collection drag
    private static int _currentIndex;

    private static readonly List<Rect> SlotBounds = new();
    private static readonly List<ContentPresenter> LogicalChildren = new();

    private static object? _draggedData;
    private static ItemsControl? _currentItemsControl;
    private static ItemsControl? _originalItemsControl; // Track the original ItemsControl to detect returns
    private static bool _endDragInProgress;
    private static IList? _sourceCollection;
    private static IList? _targetCollection;
    private static string? _sourceGroup;
    private static Panel? _currentPanel;
    private static bool _isSortableOnly; // True if source is IsSortable-enabled (allows same-collection sort)

    private static int GetAnimationDurationMs(ItemsControl? itemsControl)
    {
        return itemsControl == null
            ? DefaultAnimationDurationMs
            : NormalizeAnimationDuration(
                itemsControl.GetValue(AnimationDurationProperty),
                DefaultAnimationDurationMs);
    }

    private static int NormalizeAnimationDuration(int value, int fallback)
    {
        return value >= MinimumAnimationDurationMs ? value : fallback;
    }

    static Sortable()
    {
        SortableProperty.Changed.AddClassHandler<ItemsControl>(HandleSortableChanged);
        DroppableProperty.Changed.AddClassHandler<ItemsControl>(HandleDroppableChanged);
        IsSortableProperty.Changed.AddClassHandler<Control>(HandleIsSortableChanged);
        IsDroppableProperty.Changed.AddClassHandler<Control>(HandleIsDroppableChanged);
    }
}
