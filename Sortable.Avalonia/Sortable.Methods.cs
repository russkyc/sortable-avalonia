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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Sortable.Avalonia.Internal;

namespace Sortable.Avalonia;

public partial class Sortable
{
    // Stores/restores target panel min-size while cross-collection preview transforms are active.
    private static Panel? _previewReservePanel;
    private static double _previewReserveOriginalMinWidth;
    private static double _previewReserveOriginalMinHeight;
    private static bool _previewReserveApplied;

    private static void HandleSortableChanged(ItemsControl itemsControl, AvaloniaPropertyChangedEventArgs e)
    {
        if (ShouldTrackProgrammaticAnimations(itemsControl))
        {
            AttachProgrammaticAnimationTracking(itemsControl);
            return;
        }

        DetachProgrammaticAnimationTracking(itemsControl);
    }

    private static void HandleDroppableChanged(ItemsControl itemsControl, AvaloniaPropertyChangedEventArgs e)
    {
        if (ShouldTrackProgrammaticAnimations(itemsControl))
        {
            AttachProgrammaticAnimationTracking(itemsControl);
            return;
        }

        DetachProgrammaticAnimationTracking(itemsControl);
    }

    private static bool ShouldTrackProgrammaticAnimations(ItemsControl itemsControl) =>
        GetSortable(itemsControl) || GetDroppable(itemsControl);

    private static void AttachProgrammaticAnimationTracking(ItemsControl itemsControl)
    {
        if (!TrackedItemsControls.Add(itemsControl))
        {
            RefreshCollectionSubscription(itemsControl);
            QueueStableBoundsWarmup(itemsControl);
            return;
        }

        itemsControl.PropertyChanged += OnTrackedItemsControlPropertyChanged;
        itemsControl.AttachedToVisualTree += OnTrackedItemsControlAttached;
        itemsControl.DetachedFromVisualTree += OnTrackedItemsControlDetached;
        itemsControl.LayoutUpdated += OnTrackedItemsControlLayoutUpdated;

        RefreshCollectionSubscription(itemsControl);
        QueueStableBoundsWarmup(itemsControl);
    }

    private static void DetachProgrammaticAnimationTracking(ItemsControl itemsControl)
    {
        if (!TrackedItemsControls.Remove(itemsControl))
        {
            return;
        }

        itemsControl.PropertyChanged -= OnTrackedItemsControlPropertyChanged;
        itemsControl.AttachedToVisualTree -= OnTrackedItemsControlAttached;
        itemsControl.DetachedFromVisualTree -= OnTrackedItemsControlDetached;
        itemsControl.LayoutUpdated -= OnTrackedItemsControlLayoutUpdated;

        if (ObservedCollections.TryGetValue(itemsControl, out var observedCollection) &&
            CollectionHandlers.TryGetValue(itemsControl, out var handler))
        {
            observedCollection.CollectionChanged -= handler;
        }

        ObservedCollections.Remove(itemsControl);
        CollectionHandlers.Remove(itemsControl);
        LastStableBounds.Remove(itemsControl);
        PendingProgrammaticRemovals.RemoveAll(snapshot => ReferenceEquals(snapshot.SourceItemsControl, itemsControl));
    }

    private static void OnTrackedItemsControlAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not ItemsControl itemsControl)
        {
            return;
        }

        if (!ShouldTrackProgrammaticAnimations(itemsControl))
        {
            return;
        }

        RefreshCollectionSubscription(itemsControl);
        QueueStableBoundsWarmup(itemsControl);
    }

    private static void OnTrackedItemsControlDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not ItemsControl itemsControl)
        {
            return;
        }

        if (!ShouldTrackProgrammaticAnimations(itemsControl))
        {
            DetachProgrammaticAnimationTracking(itemsControl);
            return;
        }

        // Keep tracking for tab content that gets detached/re-attached; just reset stale geometry.
        LastStableBounds.Remove(itemsControl);
    }

    private static void OnTrackedItemsControlLayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is not ItemsControl itemsControl)
        {
            return;
        }

        if (_isDragging || !TrackedItemsControls.Contains(itemsControl) || !itemsControl.IsAttachedToVisualTree())
        {
            return;
        }

        var currentBounds = CaptureItemBounds(itemsControl);
        if (currentBounds.Count > 0)
        {
            LastStableBounds[itemsControl] = currentBounds;
        }
    }

    private static void OnTrackedItemsControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not ItemsControl itemsControl)
        {
            return;
        }

        if (e.Property == ItemsControl.ItemsSourceProperty)
        {
            RefreshCollectionSubscription(itemsControl);
            QueueStableBoundsWarmup(itemsControl);
        }
    }

    private static void QueueStableBoundsWarmup(ItemsControl itemsControl)
    {
        Dispatcher.UIThread.Post(
            () => Dispatcher.UIThread.Post(
                () => RefreshStableBoundsSnapshot(itemsControl),
                DispatcherPriority.Render),
            DispatcherPriority.Loaded);
    }

    private static void RefreshCollectionSubscription(ItemsControl itemsControl)
    {
        if (ObservedCollections.TryGetValue(itemsControl, out var existingCollection) &&
            CollectionHandlers.TryGetValue(itemsControl, out var existingHandler))
        {
            existingCollection.CollectionChanged -= existingHandler;
        }

        ObservedCollections.Remove(itemsControl);
        CollectionHandlers.Remove(itemsControl);

        if (itemsControl.ItemsSource is not INotifyCollectionChanged observableCollection)
        {
            return;
        }

        NotifyCollectionChangedEventHandler handler = (_, args) =>
            OnTrackedCollectionChanged(itemsControl, args);

        observableCollection.CollectionChanged += handler;
        ObservedCollections[itemsControl] = observableCollection;
        CollectionHandlers[itemsControl] = handler;
    }

    private static void OnTrackedCollectionChanged(ItemsControl itemsControl, NotifyCollectionChangedEventArgs args)
    {
        // Drag previews already animate with dedicated logic; avoid mixing both systems.
        if (_isDragging)
        {
            return;
        }

        if (!ShouldTrackProgrammaticAnimations(itemsControl))
        {
            return;
        }

        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Remove:
                CaptureProgrammaticRemovalSnapshot(itemsControl, args);
                QueueProgrammaticLayoutAnimation(itemsControl);
                break;
            case NotifyCollectionChangedAction.Add:
                QueueProgrammaticLayoutAnimation(itemsControl);
                QueueProgrammaticCrossCollectionAnimations(itemsControl, args);
                break;
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
                QueueProgrammaticLayoutAnimation(itemsControl);
                break;
        }
    }

    private static void CaptureProgrammaticRemovalSnapshot(ItemsControl itemsControl, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems == null || args.OldItems.Count == 0)
        {
            return;
        }

        if (!LastStableBounds.TryGetValue(itemsControl, out var beforeBounds) || beforeBounds.Count == 0)
        {
            return;
        }

        var group = GetGroupFromItemsControl(itemsControl);

        foreach (var removed in args.OldItems)
        {
            if (removed == null || !beforeBounds.TryGetValue(removed, out var sourceBounds))
            {
                continue;
            }

            PendingProgrammaticRemovals.RemoveAll(snapshot => ReferenceEquals(snapshot.Item, removed));
            PendingProgrammaticRemovals.Add(new ProgrammaticRemovalSnapshot
            {
                Item = removed,
                SourceItemsControl = itemsControl,
                SourceBounds = sourceBounds,
                Group = group,
                CapturedAtUtc = DateTime.UtcNow
            });
        }

        CleanupStaleProgrammaticRemovals();
    }

    private static void QueueProgrammaticCrossCollectionAnimations(ItemsControl targetItemsControl, NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems == null || args.NewItems.Count == 0)
        {
            return;
        }

        CleanupStaleProgrammaticRemovals();

        foreach (var added in args.NewItems)
        {
            if (added == null)
            {
                continue;
            }

            var match = PendingProgrammaticRemovals.FirstOrDefault(
                snapshot =>
                    ReferenceEquals(snapshot.Item, added) &&
                    !ReferenceEquals(snapshot.SourceItemsControl, targetItemsControl) &&
                    IsMatchingGroup(snapshot.Group, GetGroupFromItemsControl(targetItemsControl)));

            if (match == null)
            {
                continue;
            }

            PendingProgrammaticRemovals.Remove(match);
            QueueCrossCollectionTravelAnimation(match, targetItemsControl, added);
        }
    }

    private static bool IsMatchingGroup(string? sourceGroup, string? targetGroup)
    {
        if (string.IsNullOrWhiteSpace(sourceGroup) || string.IsNullOrWhiteSpace(targetGroup))
        {
            return false;
        }

        return string.Equals(sourceGroup, targetGroup, StringComparison.Ordinal);
    }

    private static void CleanupStaleProgrammaticRemovals()
    {
        var now = DateTime.UtcNow;
        PendingProgrammaticRemovals.RemoveAll(
            snapshot => (now - snapshot.CapturedAtUtc).TotalMilliseconds > ProgrammaticPairingWindowMs);
    }

    private static void QueueCrossCollectionTravelAnimation(
        ProgrammaticRemovalSnapshot snapshot,
        ItemsControl targetItemsControl,
        object item)
    {
        Dispatcher.UIThread.Post(
            () => Dispatcher.UIThread.Post(
                () => AnimateProgrammaticCrossCollectionTravel(snapshot, targetItemsControl, item),
                DispatcherPriority.Render),
            DispatcherPriority.Loaded);
    }

    private static void AnimateProgrammaticCrossCollectionTravel(
        ProgrammaticRemovalSnapshot snapshot,
        ItemsControl targetItemsControl,
        object item)
    {
        if (_isDragging)
        {
            return;
        }

        var travelDurationMs = GetAnimationDurationMs(targetItemsControl) > 0 
            ? GetAnimationDurationMs(targetItemsControl) 
            : GetAnimationDurationMs(snapshot.SourceItemsControl);

        var topLevel = TopLevel.GetTopLevel(targetItemsControl) ?? TopLevel.GetTopLevel(snapshot.SourceItemsControl);
        if (topLevel == null)
        {
            return;
        }

        if (FindItemPresenter(targetItemsControl, item) is not { } targetPresenter)
        {
            return;
        }

        var sourceOrigin = snapshot.SourceItemsControl.TranslatePoint(new Point(0, 0), topLevel);
        var targetOrigin = targetItemsControl.TranslatePoint(new Point(0, 0), topLevel);
        if (!sourceOrigin.HasValue || !targetOrigin.HasValue)
        {
            return;
        }

        var sourceRect = new Rect(sourceOrigin.Value + snapshot.SourceBounds.Position, snapshot.SourceBounds.Size);
        var targetRect = new Rect(targetOrigin.Value + targetPresenter.Bounds.Position, targetPresenter.Bounds.Size);

        var overlayLayer = OverlayLayer.GetOverlayLayer(topLevel);
        if (overlayLayer == null)
        {
            return;
        }

        var overlay = new Canvas { IsHitTestVisible = false };
        overlayLayer.Children.Add(overlay);

        var ghost = new Border
        {
            Width = Math.Max(1d, targetRect.Width),
            Height = Math.Max(1d, targetRect.Height),
            Background = CreateProgrammaticGhostBrush(targetPresenter, topLevel, out var bitmap),
            Opacity = 0.95,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(ghost, SnapToDevicePixels(sourceRect.X, topLevel.RenderScaling));
        Canvas.SetTop(ghost, SnapToDevicePixels(sourceRect.Y, topLevel.RenderScaling));
        overlay.Children.Add(ghost);

        targetPresenter.Opacity = 0;
        targetPresenter.Transitions = null;

        ghost.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Canvas.LeftProperty,
                Duration = TimeSpan.FromMilliseconds(travelDurationMs),
                Easing = new CubicEaseOut()
            },
            new DoubleTransition
            {
                Property = Canvas.TopProperty,
                Duration = TimeSpan.FromMilliseconds(travelDurationMs),
                Easing = new CubicEaseOut()
            },
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(travelDurationMs),
                Easing = new CubicEaseOut()
            }
        };

        Canvas.SetLeft(ghost, SnapToDevicePixels(targetRect.X, topLevel.RenderScaling));
        Canvas.SetTop(ghost, SnapToDevicePixels(targetRect.Y, topLevel.RenderScaling));
        ghost.Opacity = 0.15;

        Dispatcher.UIThread.Post(
            () =>
            {
                targetPresenter.Transitions = new Transitions
                {
                    new DoubleTransition
                    {
                        Property = Visual.OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(travelDurationMs * 0.46),
                        Easing = new CubicEaseOut()
                    }
                };
                targetPresenter.Opacity = 1;
            },
            DispatcherPriority.Render);

        DispatcherTimer.RunOnce(
            () =>
            {
                overlayLayer.Children.Remove(overlay);
                bitmap?.Dispose();
            },
            TimeSpan.FromMilliseconds(travelDurationMs + 40));
    }

    private static IBrush CreateProgrammaticGhostBrush(Control previewVisual, TopLevel topLevel, out RenderTargetBitmap? bitmap)
    {
        bitmap = null;

        try
        {
            var scale = topLevel.RenderScaling;
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(previewVisual.Bounds.Width * scale));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(previewVisual.Bounds.Height * scale));
            var dpi = new Vector(96 * scale, 96 * scale);

            bitmap = new RenderTargetBitmap(new PixelSize(pixelWidth, pixelHeight), dpi);
            bitmap.Render(previewVisual);

            return new ImageBrush(bitmap)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
        }
        catch
        {
            return new VisualBrush(previewVisual);
        }
    }

    private static ContentPresenter? FindItemPresenter(ItemsControl itemsControl, object item)
    {
        return itemsControl.ItemsPanelRoot?.Children
            .OfType<ContentPresenter>()
            .FirstOrDefault(child => ReferenceEquals(child.DataContext, item));
    }

    private static void QueueProgrammaticLayoutAnimation(ItemsControl itemsControl)
    {
        if (!LastStableBounds.TryGetValue(itemsControl, out var beforeBounds) || beforeBounds.Count == 0)
        {
            Dispatcher.UIThread.Post(() => RefreshStableBoundsSnapshot(itemsControl), DispatcherPriority.Loaded);
            return;
        }

        var frozenBefore = new Dictionary<object, Rect>(beforeBounds);

        Dispatcher.UIThread.Post(
            () => Dispatcher.UIThread.Post(
                () => AnimateProgrammaticCollectionChange(itemsControl, frozenBefore),
                DispatcherPriority.Render),
            DispatcherPriority.Loaded);
    }

    private static void AnimateProgrammaticCollectionChange(ItemsControl itemsControl, IReadOnlyDictionary<object, Rect> beforeBounds)
    {
        if (_isDragging)
        {
            return;
        }

        var panel = itemsControl.ItemsPanelRoot;
        if (panel == null)
        {
            return;
        }

        var animationDurationMs = GetAnimationDurationMs(itemsControl);
        var nowBounds = CaptureItemBounds(itemsControl);

        foreach (var child in panel.Children.OfType<ContentPresenter>())
        {
            var item = child.DataContext;
            if (item == null)
            {
                continue;
            }

            if (!beforeBounds.TryGetValue(item, out var previous) || !nowBounds.TryGetValue(item, out var current))
            {
                continue;
            }

            var shiftX = previous.X - current.X;
            var shiftY = previous.Y - current.Y;
            if (Math.Abs(shiftX) < 0.5 && Math.Abs(shiftY) < 0.5)
            {
                continue;
            }

            // FLIP: paint the inverted transform first, then animate back to identity.
            child.Transitions = null;
            child.RenderTransform = TransformOperations.Parse($"translateX({shiftX}px) translateY({shiftY}px)");

            Dispatcher.UIThread.Post(
                () =>
                {
                    child.Transitions = new Transitions
                    {
                        new TransformOperationsTransition
                        {
                            Property = Visual.RenderTransformProperty,
                            Duration = TimeSpan.FromMilliseconds(animationDurationMs),
                            Easing = new CubicEaseOut()
                        }
                    };

                    child.RenderTransform = TransformOperations.Parse("none");
                },
                DispatcherPriority.Render);
        }

        LastStableBounds[itemsControl] = nowBounds;
    }

    private static void RefreshStableBoundsSnapshot(ItemsControl itemsControl)
    {
        if (!TrackedItemsControls.Contains(itemsControl))
        {
            return;
        }

        LastStableBounds[itemsControl] = CaptureItemBounds(itemsControl);
    }

    private static Dictionary<object, Rect> CaptureItemBounds(ItemsControl itemsControl)
    {
        var result = new Dictionary<object, Rect>();
        var panel = itemsControl.ItemsPanelRoot;
        if (panel == null)
        {
            return result;
        }

        foreach (var child in panel.Children.OfType<ContentPresenter>())
        {
            if (child.DataContext != null)
            {
                result[child.DataContext] = child.Bounds;
            }
        }

        return result;
    }

    private static void HandleIsSortableChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        // Attach handlers if either IsSortable or IsDroppable is true
        if (GetIsSortable(control) || GetIsDroppable(control))
        {
            AttachEventHandlers(control);
        }
        else if (!GetIsDroppable(control))
        {
            DetachEventHandlers(control);
        }
    }

    private static void HandleIsDroppableChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        // Attach handlers if either IsSortable or IsDroppable is true
        if (GetIsSortable(control) || GetIsDroppable(control))
        {
            AttachEventHandlers(control);
        }
        else if (!GetIsSortable(control))
        {
            DetachEventHandlers(control);
        }
    }

    private static void AttachEventHandlers(Control control)
    {
        control.PointerPressed += OnPointerPressed;
        control.PointerMoved += OnPointerMoved;
        control.PointerReleased += OnPointerReleased;
        control.PointerCaptureLost += OnPointerCaptureLost;
    }

    private static void DetachEventHandlers(Control control)
    {
        control.PointerPressed -= OnPointerPressed;
        control.PointerMoved -= OnPointerMoved;
        control.PointerReleased -= OnPointerReleased;
        control.PointerCaptureLost -= OnPointerCaptureLost;
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;

        // Only respond to primary button (left mouse button or touch)
        var properties = e.GetCurrentPoint(control).Properties;
        if (!properties.IsLeftButtonPressed && e.Pointer.Type != PointerType.Touch)
        {
            return;
        }

        var container = control.FindAncestorOfType<ContentPresenter>();
        var itemsControl = FindSortableItemsControl(control);
        var panel = container?.FindAncestorOfType<Panel>();
        if (panel == null || container == null || itemsControl == null) return;

        if (!CanStartDragFromPointerOrigin(control, e))
        {
            return;
        }

        // Check if this item is marked as sortable (same-collection sort enabled)
        _isSortableOnly = GetIsSortable(control);

        _isDragging = true;
        _draggedElement = control;
        _dragStartPoint = e.GetPosition(panel);
        _currentItemsControl = itemsControl;
        _originalItemsControl = itemsControl; // Store the original
        _sourceCollection = itemsControl.ItemsSource as IList;
        _targetCollection = _sourceCollection;
        _sourceGroup = GetGroup(itemsControl); // Get group from ItemsControl

        CacheLayoutSlots(panel, container, itemsControl);

        // Create drag overlay BEFORE hiding original element.
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel != null)
        {
            var pointerPosition = e.GetPosition(topLevel);
            CreateDragOverlay(control, topLevel, pointerPosition);
        }

        // Keep source item visible as the in-list placeholder during drag.
        control.Opacity = PlaceholderOpacity;

        e.Pointer.Capture(control);
        e.Handled = true;
    }

    private static bool CanStartDragFromPointerOrigin(Control dragRoot, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual sourceVisual)
        {
            return true;
        }

        // If a handle exists in this item, drag can start only from that handle subtree.
        if (HasDragHandle(dragRoot))
        {
            return IsPointerFromDragHandle(sourceVisual, dragRoot);
        }

        // Otherwise, keep embedded controls clickable and focusable.
        return !IsInteractiveOrigin(sourceVisual, dragRoot);
    }

    private static bool HasDragHandle(Control dragRoot)
    {
        if (GetIsDragHandle(dragRoot))
        {
            return true;
        }

        return dragRoot.GetVisualDescendants()
            .OfType<Control>()
            .Any(GetIsDragHandle);
    }

    private static bool IsPointerFromDragHandle(Visual sourceVisual, Control dragRoot)
    {
        var current = sourceVisual;
        while (current != null)
        {
            if (current is Control control && GetIsDragHandle(control))
            {
                return true;
            }

            if (ReferenceEquals(current, dragRoot))
            {
                break;
            }

            current = current.GetVisualParent();
        }

        return false;
    }

    private static bool IsInteractiveOrigin(Visual sourceVisual, Control dragRoot)
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

            current = current.GetVisualParent();
        }

        return false;
    }

    private static void CacheLayoutSlots(Panel panel, ContentPresenter container, ItemsControl itemsControl)
    {
        LogicalChildren.Clear();
        SlotBounds.Clear();

        foreach (var child in panel.Children)
        {
            if (child is ContentPresenter cp)
            {
                LogicalChildren.Add(cp);
                SlotBounds.Add(cp.Bounds);
            }
        }

        _originalIndex = LogicalChildren.IndexOf(container);
        _originalSourceIndex = _originalIndex;
        _currentIndex = _originalIndex;
        _currentPanel = panel;
        _draggedData = GetDraggedData(container, itemsControl);
    }

    private static object? GetDraggedData(ContentPresenter container, ItemsControl itemsControl)
    {
        if (_originalIndex >= 0 && itemsControl.ItemsSource is IList list && _originalIndex < list.Count)
        {
            return list[_originalIndex];
        }

        return container.DataContext;
    }

    private static void CreateDragOverlay(Control previewVisual, TopLevel topLevel, Point pointerPosition)
    {
        var previewBrush = CreateDragPreviewBrush(previewVisual, topLevel);

        _dragProxy = new Border
        {
            Width = previewVisual.Bounds.Width,
            Height = previewVisual.Bounds.Height,
            Background = previewBrush,
            Opacity = 1.0,
            IsHitTestVisible = false
        };

        RenderOptions.SetBitmapInterpolationMode(_dragProxy, BitmapInterpolationMode.HighQuality);

        _overlayCanvas = new Canvas
        {
            Background = null,
            IsHitTestVisible = false
        };
        _overlayCanvas.Children.Add(_dragProxy);

        var previewPositionInTopLevel = previewVisual.TranslatePoint(new Point(0, 0), topLevel);
        if (previewPositionInTopLevel.HasValue)
        {
            _dragProxyOffset = previewPositionInTopLevel.Value - pointerPosition;
        }
        else
        {
            _dragProxyOffset = new Point(0, 0);
        }

        var initialLeft = SnapToDevicePixels(pointerPosition.X + _dragProxyOffset.X, topLevel.RenderScaling);
        var initialTop = SnapToDevicePixels(pointerPosition.Y + _dragProxyOffset.Y, topLevel.RenderScaling);
        Canvas.SetLeft(_dragProxy, initialLeft);
        Canvas.SetTop(_dragProxy, initialTop);

        var overlayLayer = OverlayLayer.GetOverlayLayer(topLevel);
        overlayLayer?.Children.Add(_overlayCanvas);
    }

    private static IBrush CreateDragPreviewBrush(Control previewVisual, TopLevel topLevel)
    {
        try
        {
            var scale = topLevel.RenderScaling;
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(previewVisual.Bounds.Width * scale));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(previewVisual.Bounds.Height * scale));
            var dpi = new Vector(96 * scale, 96 * scale);

            _dragProxyBitmap = new RenderTargetBitmap(new PixelSize(pixelWidth, pixelHeight), dpi);
            _dragProxyBitmap.Render(previewVisual);

            return new ImageBrush(_dragProxyBitmap)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
        }
        catch
        {
            return new VisualBrush(previewVisual);
        }
    }

    private static double SnapToDevicePixels(double value, double renderScaling)
    {
        if (renderScaling <= 0)
        {
            return value;
        }

        return Math.Round(value * renderScaling) / renderScaling;
    }

    private static void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _draggedElement == null) return;

        var container = _draggedElement.FindAncestorOfType<ContentPresenter>();
        var panel = container?.FindAncestorOfType<Panel>();
        if (panel == null || container == null) return;

        // Update drag proxy position in TopLevel coordinates
        var topLevel = TopLevel.GetTopLevel(_draggedElement);
        if (topLevel != null && _dragProxy != null)
        {
            var currentPointerPosition = e.GetPosition(topLevel);
            var left = SnapToDevicePixels(currentPointerPosition.X + _dragProxyOffset.X, topLevel.RenderScaling);
            var top = SnapToDevicePixels(currentPointerPosition.Y + _dragProxyOffset.Y, topLevel.RenderScaling);
            Canvas.SetLeft(_dragProxy, left);
            Canvas.SetTop(_dragProxy, top);
        }

        // Check if hovering over a different sortable ItemsControl in the same group
        var hoveredItemsControl = FindHoveredItemsControl(e);
        if (hoveredItemsControl != null)
        {
            SwitchToNewItemsControl(hoveredItemsControl, e);
            return;
        }

        var activePanel = _currentItemsControl?.ItemsPanelRoot;
        if (activePanel == null)
        {
            return;
        }

        _currentPanel = activePanel;

        // Handle empty collections or dropping at the end
        if (LogicalChildren.Count == 0)
        {
            _currentIndex = 0;
            UpdateCrossCollectionPlaceholder();
            return;
        }

        int hoverIndex;
        bool isCrossCollection = !ReferenceEquals(_sourceCollection, _targetCollection);

        if (isCrossCollection)
        {
            // For cross-collection hovers, use pointer position in the active target panel.
            var pointerInActivePanel = e.GetPosition(activePanel);
            hoverIndex = FindClosestSlot(pointerInActivePanel, includeTerminalSlot: true);
        }
        else
        {
            // Same collection - only allow reordering if IsSortable is enabled
            if (!_isSortableOnly)
            {
                // Not sortable, don't show preview
                HideCrossCollectionPlaceholder();
                return;
            }

            // Continue with normal drag within original ItemsControl.
            var currentPosition = e.GetPosition(panel);
            var draggedVirtualBounds = container.Bounds.Translate(currentPosition - _dragStartPoint);
            var draggedCenter = GetBoundsCenter(draggedVirtualBounds);
            hoverIndex = FindClosestSlot(draggedCenter, includeTerminalSlot: false);
        }

        hoverIndex = Math.Max(0, Math.Min(hoverIndex, LogicalChildren.Count));

        if (hoverIndex != _currentIndex)
        {
            _currentIndex = hoverIndex;
            UpdatePreviewLayout();
        }

        if (isCrossCollection)
        {
            UpdateCrossCollectionPlaceholder();
        }
        else
        {
            HideCrossCollectionPlaceholder();
        }
    }

    private static ItemsControl? FindHoveredItemsControl(PointerEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(_draggedElement);
        if (topLevel == null) return null;

        var position = e.GetPosition(topLevel);
        var hoveredElements = topLevel.GetVisualsAt(position).ToList();

        // First pass: look for sortable ItemsControl directly
        foreach (var element in hoveredElements)
        {
            if (element is ItemsControl itemsControl && CanAcceptDragTarget(itemsControl))
            {
                return itemsControl;
            }
        }

        // Second pass: look for sortable ancestor ItemsControl
        foreach (var element in hoveredElements)
        {
            var ancestorItemsControl = FindSortableItemsControl(element);
            if (ancestorItemsControl != null && CanAcceptDragTarget(ancestorItemsControl))
            {
                return ancestorItemsControl;
            }
        }

        // Third pass: Check sortable ItemsControls in the tree for bounds intersection.
        // This helps with empty ItemsControls that might not be in hit test results.
        var allItemsControls = topLevel.GetVisualDescendants().OfType<ItemsControl>();
        foreach (var itemsControl in allItemsControls)
        {
            if (!CanAcceptDragTarget(itemsControl)) continue;

            var referenceVisual = (Visual)itemsControl;
            var boundsToCheck = itemsControl.Bounds;

            // For empty lists, use parent container bounds so drop target stays hittable.
            if ((boundsToCheck.Height == 0 || boundsToCheck.Width == 0) && itemsControl.Parent is Visual parent)
            {
                referenceVisual = parent;
                boundsToCheck = parent.Bounds;
            }

            var visualPosition = referenceVisual.TranslatePoint(new Point(0, 0), topLevel);
            if (!visualPosition.HasValue) continue;

            var bounds = new Rect(visualPosition.Value, new Size(boundsToCheck.Width, boundsToCheck.Height));
            if (bounds.Contains(position))
            {
                return itemsControl;
            }
        }

        return null;
    }

    private static string? GetGroupFromItemsControl(ItemsControl itemsControl)
    {
        var group = GetGroup(itemsControl);
        return string.IsNullOrWhiteSpace(group) ? null : group;
    }

    private static ItemsControl? FindSortableItemsControl(Visual? start)
    {
        var current = start;
        while (current != null)
        {
            if (current is ItemsControl itemsControl && (GetSortable(itemsControl) || GetDroppable(itemsControl)))
            {
                return itemsControl;
            }

            current = current.GetVisualParent();
        }

        return null;
    }

    private static bool CanAcceptDragTarget(ItemsControl itemsControl)
    {
        if (itemsControl == _currentItemsControl) return false;
        // For cross-collection drop, target must have Droppable enabled
        if (!GetDroppable(itemsControl)) return false;

        var targetGroup = GetGroupFromItemsControl(itemsControl);
        return !string.IsNullOrEmpty(_sourceGroup) && targetGroup == _sourceGroup;
    }

    private static void SwitchToNewItemsControl(ItemsControl newItemsControl, PointerEventArgs e)
    {
        if (!CanAcceptDragTarget(newItemsControl)) return;

        var newPanel = newItemsControl.ItemsPanelRoot;
        if (newPanel == null) return;

        ClearCrossCollectionPanelReserve();

        // Clear old transforms
        foreach (var child in LogicalChildren)
        {
            child.Transitions = null;
            child.RenderTransform = TransformOperations.Parse("none");
        }

        _currentItemsControl = newItemsControl;
        _targetCollection = newItemsControl.ItemsSource as IList;
        _currentPanel = newPanel;

        // Check if we're returning to the original ItemsControl
        bool returningToOriginal = newItemsControl == _originalItemsControl;

        // If returning to original, restore the original index (same-collection mode)
        if (returningToOriginal)
        {
            _originalIndex = _originalSourceIndex;
        }
        else
        {
            // Mark as cross-collection
            _originalIndex = -1;
        }

        // Rebuild layout cache for new target
        LogicalChildren.Clear();
        SlotBounds.Clear();

        foreach (var child in newPanel.Children)
        {
            if (child is ContentPresenter cp)
            {
                LogicalChildren.Add(cp);
                SlotBounds.Add(cp.Bounds);
            }
        }

        // Handle empty target collection
        if (LogicalChildren.Count == 0 || SlotBounds.Count == 0)
        {
            _currentIndex = 0;
            return;
        }

        // Find hover position in new control
        var position = e.GetPosition(newPanel);
        var isCrossCollectionTarget = !ReferenceEquals(_sourceCollection, _targetCollection);
        int hoverIndex = FindClosestSlot(position, includeTerminalSlot: isCrossCollectionTarget);

        _currentIndex = Math.Max(0, Math.Min(hoverIndex, LogicalChildren.Count));

        // Update preview layout
        UpdatePreviewLayout();

        if (!ReferenceEquals(_sourceCollection, _targetCollection))
        {
            UpdateCrossCollectionPlaceholder();
        }
        else
        {
            HideCrossCollectionPlaceholder();
        }
    }

    private static Point GetBoundsCenter(Rect bounds) =>
        new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

    private static int FindClosestSlot(Point center, bool includeTerminalSlot)
    {
        if (SlotBounds.Count == 0)
        {
            return 0;
        }

        if (!includeTerminalSlot)
        {
            return FindClosestExistingSlot(center);
        }

        var panelLayout = DetectPanelLayout();

        if (panelLayout == PanelLayoutType.VerticalStack)
        {
            return FindInsertionIndexByAxis(center, useVerticalAxis: true);
        }

        if (panelLayout == PanelLayoutType.HorizontalStack)
        {
            return FindInsertionIndexByAxis(center, useVerticalAxis: false);
        }

        return FindInsertionIndexByNearestPath(center);
    }

    private static int FindClosestExistingSlot(Point center)
    {
        int closestIndex = _currentIndex;
        double minDistance = double.MaxValue;

        for (int i = 0; i < SlotBounds.Count; i++)
        {
            var slotCenter = GetBoundsCenter(SlotBounds[i]);
            var dx = center.X - slotCenter.X;
            var dy = center.Y - slotCenter.Y;
            var distance = (dx * dx) + (dy * dy);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private static int FindInsertionIndexByAxis(Point position, bool useVerticalAxis)
    {
        for (int i = 0; i < SlotBounds.Count; i++)
        {
            var slot = SlotBounds[i];
            var midpoint = useVerticalAxis
                ? slot.Y + (slot.Height / 2)
                : slot.X + (slot.Width / 2);
            var coordinate = useVerticalAxis ? position.Y : position.X;

            if (coordinate < midpoint)
            {
                return i;
            }
        }

        // Pointer is past the last midpoint, so preview the append position.
        return SlotBounds.Count;
    }

    private static int FindInsertionIndexByNearestPath(Point position)
    {
        int closestIndex = 0;
        double minDistance = double.MaxValue;

        for (int i = 0; i < SlotBounds.Count; i++)
        {
            var slotCenter = GetBoundsCenter(SlotBounds[i]);
            var dx = position.X - slotCenter.X;
            var dy = position.Y - slotCenter.Y;
            var distance = (dx * dx) + (dy * dy);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        if (SlotBounds.Count == 1)
        {
            var onlyCenter = GetBoundsCenter(SlotBounds[0]);
            return position.Y >= onlyCenter.Y ? 1 : 0;
        }

        var currentCenter = GetBoundsCenter(SlotBounds[closestIndex]);
        Vector flowDirection;

        if (closestIndex == 0)
        {
            var nextCenter = GetBoundsCenter(SlotBounds[1]);
            flowDirection = nextCenter - currentCenter;
        }
        else if (closestIndex == SlotBounds.Count - 1)
        {
            var previousCenter = GetBoundsCenter(SlotBounds[closestIndex - 1]);
            flowDirection = currentCenter - previousCenter;
        }
        else
        {
            var previousCenter = GetBoundsCenter(SlotBounds[closestIndex - 1]);
            var nextCenter = GetBoundsCenter(SlotBounds[closestIndex + 1]);
            flowDirection = nextCenter - previousCenter;
        }

        var toPointer = position - currentCenter;
        var isAfterClosest = (toPointer.X * flowDirection.X) + (toPointer.Y * flowDirection.Y) >= 0;
        var insertionIndex = isAfterClosest ? closestIndex + 1 : closestIndex;

        return Math.Max(0, Math.Min(insertionIndex, SlotBounds.Count));
    }

    private static void UpdatePreviewLayout()
    {
        var animationDuration = GetAnimationDurationMs(_currentItemsControl);
        bool isCrossSwap = IsCrossCollectionSwapMode();

        for (int i = 0; i < LogicalChildren.Count; i++)
        {
            int previewIndex = MapItemIndexToPreviewSlot(i);
            ApplyPreviewTransform(LogicalChildren[i], i, previewIndex, animationDuration);
            
            // In cross-collection swap mode, dim the target item that will be swapped out
            if (isCrossSwap && i == _currentIndex && _currentIndex < LogicalChildren.Count)
            {
                LogicalChildren[i].Opacity = PlaceholderOpacity;
            }
            else if (Math.Abs(LogicalChildren[i].Opacity - 1.0) > 0.01)
            {
                // Restore full opacity for items not being swapped
                LogicalChildren[i].Opacity = 1.0;
            }
        }

        UpdateCrossCollectionPanelReserve();
    }

    private static int MapItemIndexToPreviewSlot(int index)
    {
        // Ensure we have valid bounds
        if (index < 0 || index >= LogicalChildren.Count) return index;

        // Cross-collection drag
        if (_originalIndex < 0)
        {
            // Check if we're in cross-collection swap mode
            if (IsCrossCollectionSwapMode())
            {
                // In cross-collection swap: only the target item at _currentIndex is affected
                // It should stay in place visually (will be hidden by reduced opacity)
                // No items shift - we're doing a direct exchange
                return index;
            }
            
            // Cross-collection move/copy: existing target items at/after insertion shift down
            return index >= _currentIndex ? index + 1 : index;
        }

        // Same-collection operations
        var mode = _currentItemsControl != null ? GetMode(_currentItemsControl) : SortableMode.Sort;

        if (mode == SortableMode.Swap)
        {
            // Swap mode: only the dragged item and target item exchange positions
            if (index == _originalIndex)
            {
                // The dragged item goes to the target position
                return _currentIndex;
            }
            else if (index == _currentIndex)
            {
                // The target item goes to the original position
                return _originalIndex;
            }

            // All other items stay in place
            return index;
        }

        // Sort mode (default): the dragged item's translucent placeholder follows insertion slot.
        if (index == _originalIndex)
        {
            return _currentIndex;
        }

        if (_originalIndex < _currentIndex)
        {
            // Dragging downward: items between original and insertion shift up.
            return (index > _originalIndex && index <= _currentIndex) ? index - 1 : index;
        }

        if (_originalIndex > _currentIndex)
        {
            // Dragging upward: items between insertion and original shift down.
            return (index >= _currentIndex && index < _originalIndex) ? index + 1 : index;
        }

        return index;
    }

    private static bool IsCrossCollectionSwapMode()
    {
        // Check if this is a cross-collection operation
        if (ReferenceEquals(_sourceCollection, _targetCollection))
        {
            return false;
        }

        if (_currentItemsControl == null)
        {
            return false;
        }

        // Check if the target ItemsControl has CrossCollectionTransferMode set to Swap
        var transferMode = GetCrossCollectionTransferMode(_currentItemsControl);
        return transferMode == SortableTransferMode.Swap;
    }

    private static void ApplyPreviewTransform(ContentPresenter child, int currentSlotIndex, int previewSlotIndex,
        int durationMs)
    {
        // Validate current index is within bounds
        if (currentSlotIndex < 0 || currentSlotIndex >= SlotBounds.Count)
        {
            return;
        }

        var currentSlot = SlotBounds[currentSlotIndex];
        Rect targetSlot;

        // For cross-collection drag, previewSlotIndex might be SlotBounds.Count (inserting at end)
        if (previewSlotIndex >= SlotBounds.Count)
        {
            // Calculate position for next slot based on panel layout
            targetSlot = CalculateNextSlotPosition();
        }
        else if (previewSlotIndex >= 0)
        {
            targetSlot = SlotBounds[previewSlotIndex];
        }
        else
        {
            return;
        }

        var shiftX = targetSlot.X - currentSlot.X;
        var shiftY = targetSlot.Y - currentSlot.Y;

        if (durationMs <= 0)
        {
            child.Transitions = null;
            child.RenderTransform = TransformOperations.Parse($"translateX({shiftX}px) translateY({shiftY}px)");
            return;
        }

        child.Transitions = new Transitions
        {
            new TransformOperationsTransition
            {
                Property = Visual.RenderTransformProperty,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                Easing = new CubicEaseOut()
            }
        };
        child.RenderTransform = TransformOperations.Parse($"translateX({shiftX}px) translateY({shiftY}px)");
    }

    private static Rect CalculateNextSlotPosition()
    {
        if (SlotBounds.Count == 0)
        {
            var width = _draggedElement?.Bounds.Width ?? 120;
            var height = _draggedElement?.Bounds.Height ?? 40;
            return new Rect(0, 0, Math.Max(1d, width), Math.Max(1d, height));
        }

        var lastSlot = SlotBounds[SlotBounds.Count - 1];

        // Detect panel layout type
        var panelLayout = DetectPanelLayout();

        switch (panelLayout)
        {
            case PanelLayoutType.VerticalStack:
                // Stack vertically - add below last item
                return new Rect(
                    lastSlot.X,
                    lastSlot.Y + lastSlot.Height + GetVerticalSpacing(),
                    lastSlot.Width,
                    lastSlot.Height
                );

            case PanelLayoutType.HorizontalStack:
                // Stack horizontally - add to right of last item
                return new Rect(
                    lastSlot.X + lastSlot.Width + GetHorizontalSpacing(),
                    lastSlot.Y,
                    lastSlot.Width,
                    lastSlot.Height
                );

            case PanelLayoutType.MultiColumn:
                // Multi-column grid layout
                return CalculateNextMultiColumnSlot(lastSlot);

            default:
                // Default to vertical stacking
                return new Rect(
                    lastSlot.X,
                    lastSlot.Y + lastSlot.Height + 8,
                    lastSlot.Width,
                    lastSlot.Height
                );
        }
    }

    private static Rect CalculateNextMultiColumnSlot(Rect lastSlot)
    {
        if (SlotBounds.Count < 2)
        {
            // Not enough data, assume vertical stacking
            return new Rect(
                lastSlot.X,
                lastSlot.Y + lastSlot.Height + GetVerticalSpacing(),
                lastSlot.Width,
                lastSlot.Height
            );
        }

        // Analyze the layout to determine column count and positioning
        var firstSlot = SlotBounds[0];
        var columnCount = 1;
        var horizontalSpacing = GetHorizontalSpacing();
        var verticalSpacing = GetVerticalSpacing();

        // Count items in the first row (same Y position, within tolerance)
        for (int i = 1; i < SlotBounds.Count; i++)
        {
            if (Math.Abs(SlotBounds[i].Y - firstSlot.Y) < 5)
            {
                columnCount++;
            }
            else
            {
                break;
            }
        }

        // Determine which column position for the next slot
        var currentColumn = SlotBounds.Count % columnCount;

        if (currentColumn == 0)
        {
            // First column of new row - position below the item directly above
            var itemAbove = SlotBounds[SlotBounds.Count - columnCount];
            return new Rect(
                itemAbove.X,
                lastSlot.Y + lastSlot.Height + verticalSpacing,
                lastSlot.Width,
                lastSlot.Height
            );
        }

        // Continue on the same row - position to the right
        return new Rect(
            lastSlot.X + lastSlot.Width + horizontalSpacing,
            lastSlot.Y,
            lastSlot.Width,
            lastSlot.Height
        );
    }

    private enum PanelLayoutType
    {
        VerticalStack,
        HorizontalStack,
        MultiColumn
    }

    private static PanelLayoutType DetectPanelLayout()
    {
        if (_currentPanel == null || SlotBounds.Count < 2)
        {
            return PanelLayoutType.VerticalStack;
        }

        // Check panel type first
        if (_currentPanel is StackPanel stackPanel)
        {
            return stackPanel.Orientation == global::Avalonia.Layout.Orientation.Horizontal
                ? PanelLayoutType.HorizontalStack
                : PanelLayoutType.VerticalStack;
        }

        // For other panel types, analyze the actual layout
        var firstSlot = SlotBounds[0];
        var secondSlot = SlotBounds[1];

        var sameRow = Math.Abs(firstSlot.Y - secondSlot.Y) < 5;
        var sameColumn = Math.Abs(firstSlot.X - secondSlot.X) < 5;

        if (sameRow && !sameColumn)
        {
            // Items are side-by-side
            // Check if there are multiple rows (multi-column grid)
            if (SlotBounds.Count > 2)
            {
                // Count items in first row
                var itemsInFirstRow = 1;
                for (int i = 1; i < SlotBounds.Count; i++)
                {
                    if (Math.Abs(SlotBounds[i].Y - firstSlot.Y) < 5)
                    {
                        itemsInFirstRow++;
                    }
                    else
                    {
                        break;
                    }
                }

                // If there are more items than fit in one row, it's a multi-column layout
                if (SlotBounds.Count > itemsInFirstRow)
                {
                    return PanelLayoutType.MultiColumn;
                }
            }

            return PanelLayoutType.HorizontalStack;
        }

        if (sameColumn && !sameRow)
        {
            // Items are stacked vertically
            return PanelLayoutType.VerticalStack;
        }

        // Default assumption
        return PanelLayoutType.VerticalStack;
    }

    private static double GetHorizontalSpacing()
    {
        if (_currentPanel == null || SlotBounds.Count < 2)
        {
            return 8; // Default spacing
        }

        // Try to detect horizontal spacing from actual layout
        var firstSlot = SlotBounds[0];

        // Find the next item in the same row
        for (int i = 1; i < SlotBounds.Count; i++)
        {
            var slot = SlotBounds[i];
            if (Math.Abs(slot.Y - firstSlot.Y) < 5) // Same row
            {
                var spacing = slot.X - (firstSlot.X + firstSlot.Width);
                return Math.Max(0d, spacing);
            }
        }

        return 8; // Default
    }

    private static double GetVerticalSpacing()
    {
        if (_currentPanel == null || SlotBounds.Count < 2)
        {
            return 8; // Default spacing
        }

        // Try to detect vertical spacing from actual layout
        var firstSlot = SlotBounds[0];

        // Find the next item in the same column
        for (int i = 1; i < SlotBounds.Count; i++)
        {
            var slot = SlotBounds[i];
            if (Math.Abs(slot.X - firstSlot.X) < 5) // Same column
            {
                var spacing = slot.Y - (firstSlot.Y + firstSlot.Height);
                return Math.Max(0d, spacing);
            }
        }

        return 8; // Default
    }

    private static void UpdateCrossCollectionPanelReserve()
    {
        if (ReferenceEquals(_sourceCollection, _targetCollection) || _currentPanel == null)
        {
            ClearCrossCollectionPanelReserve();
            return;
        }

        if (!_previewReserveApplied || !ReferenceEquals(_previewReservePanel, _currentPanel))
        {
            ClearCrossCollectionPanelReserve();
            _previewReservePanel = _currentPanel;
            _previewReserveOriginalMinWidth = _currentPanel.MinWidth;
            _previewReserveOriginalMinHeight = _currentPanel.MinHeight;
            _previewReserveApplied = true;
        }

        var requiredBounds = CalculateCrossCollectionPreviewBounds();
        var reservedMinWidth = Math.Max(_previewReserveOriginalMinWidth, Math.Ceiling(requiredBounds.Width));
        var reservedMinHeight = Math.Max(_previewReserveOriginalMinHeight, Math.Ceiling(requiredBounds.Height));

        if (Math.Abs(_previewReservePanel.MinWidth - reservedMinWidth) > 0.1 ||
            Math.Abs(_previewReservePanel.MinHeight - reservedMinHeight) > 0.1)
        {
            _previewReservePanel.MinWidth = reservedMinWidth;
            _previewReservePanel.MinHeight = reservedMinHeight;
            _previewReservePanel.InvalidateMeasure();
        }
    }

    private static Rect CalculateCrossCollectionPreviewBounds()
    {
        var maxRight = 0d;
        var maxBottom = 0d;

        for (int i = 0; i < SlotBounds.Count; i++)
        {
            var slot = SlotBounds[i];
            maxRight = Math.Max(maxRight, slot.Right);
            maxBottom = Math.Max(maxBottom, slot.Bottom);
        }

        for (int i = 0; i < LogicalChildren.Count; i++)
        {
            int previewIndex = MapItemIndexToPreviewSlot(i);
            var previewSlot = ResolvePreviewSlotRect(previewIndex, i);
            maxRight = Math.Max(maxRight, previewSlot.Right);
            maxBottom = Math.Max(maxBottom, previewSlot.Bottom);
        }

        var insertionSlot = GetInsertionPreviewRect();
        maxRight = Math.Max(maxRight, insertionSlot.Right);
        maxBottom = Math.Max(maxBottom, insertionSlot.Bottom);

        return new Rect(0, 0, Math.Max(1d, maxRight), Math.Max(1d, maxBottom));
    }

    private static Rect ResolvePreviewSlotRect(int previewSlotIndex, int fallbackSlotIndex)
    {
        if (previewSlotIndex >= SlotBounds.Count)
        {
            return CalculateNextSlotPosition();
        }

        if (previewSlotIndex >= 0)
        {
            return SlotBounds[previewSlotIndex];
        }

        if (fallbackSlotIndex >= 0 && fallbackSlotIndex < SlotBounds.Count)
        {
            return SlotBounds[fallbackSlotIndex];
        }

        return CalculateNextSlotPosition();
    }

    private static void ClearCrossCollectionPanelReserve()
    {
        if (!_previewReserveApplied || _previewReservePanel == null)
        {
            return;
        }

        _previewReservePanel.MinWidth = _previewReserveOriginalMinWidth;
        _previewReservePanel.MinHeight = _previewReserveOriginalMinHeight;
        _previewReservePanel.InvalidateMeasure();

        _previewReservePanel = null;
        _previewReserveOriginalMinWidth = 0;
        _previewReserveOriginalMinHeight = 0;
        _previewReserveApplied = false;
    }

    private static void UpdateCrossCollectionPlaceholder()
    {
        if (ReferenceEquals(_sourceCollection, _targetCollection))
        {
            HideCrossCollectionPlaceholder();
            return;
        }

        UpdateCrossCollectionPanelReserve();

        if (_overlayCanvas == null || _draggedElement == null || _currentPanel == null)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(_draggedElement);
        if (topLevel == null)
        {
            return;
        }

        var panelPosition = _currentPanel.TranslatePoint(new Point(0, 0), topLevel);
        if (!panelPosition.HasValue)
        {
            return;
        }

        // Create placeholder for incoming item
        if (_crossCollectionPlaceholder == null)
        {
            _crossCollectionPlaceholder = new ContentPresenter
            {
                Opacity = PlaceholderOpacity,
                IsHitTestVisible = false
            };

            _overlayCanvas.Children.Insert(0, _crossCollectionPlaceholder);
        }

        // Keep template/content synced with the currently hovered target ItemsControl.
        _crossCollectionPlaceholder.Content = _draggedData;
        _crossCollectionPlaceholder.ContentTemplate = _currentItemsControl?.ItemTemplate;

        var targetSlot = GetInsertionPreviewRect();
        var overlayRect = new Rect(
            panelPosition.Value.X + targetSlot.X,
            panelPosition.Value.Y + targetSlot.Y,
            targetSlot.Width,
            targetSlot.Height);

        // Overlay is rendered at top-level, so clamp to the visible host viewport (ScrollViewer-aware).
        if (TryGetCrossCollectionViewportRect(topLevel) is { } viewportRect)
        {
            overlayRect = ClampRectToViewport(overlayRect, viewportRect);
        }

        _crossCollectionPlaceholder.Width = overlayRect.Width;
        _crossCollectionPlaceholder.Height = overlayRect.Height;

        var left = SnapToDevicePixels(overlayRect.X, topLevel.RenderScaling);
        var top = SnapToDevicePixels(overlayRect.Y, topLevel.RenderScaling);
        Canvas.SetLeft(_crossCollectionPlaceholder, left);
        Canvas.SetTop(_crossCollectionPlaceholder, top);
    }

    private static Rect? TryGetCrossCollectionViewportRect(TopLevel topLevel)
    {
        if (_currentItemsControl is not Visual currentVisual)
        {
            return null;
        }

        var scrollViewer = currentVisual.FindAncestorOfType<ScrollViewer>();
        var viewportVisual = (Visual?)scrollViewer ?? currentVisual;

        var viewportOrigin = viewportVisual.TranslatePoint(new Point(0, 0), topLevel);
        if (!viewportOrigin.HasValue)
        {
            return null;
        }

        return new Rect(viewportOrigin.Value, viewportVisual.Bounds.Size);
    }

    private static Rect ClampRectToViewport(Rect rect, Rect viewport)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return rect;
        }

        var width = Math.Min(rect.Width, viewport.Width);
        var height = Math.Min(rect.Height, viewport.Height);

        var clampedLeft = Math.Max(viewport.X, Math.Min(rect.X, viewport.Right - width));
        var clampedTop = Math.Max(viewport.Y, Math.Min(rect.Y, viewport.Bottom - height));

        return new Rect(clampedLeft, clampedTop, width, height);
    }

    private static Rect GetInsertionPreviewRect()
    {
        if (SlotBounds.Count == 0)
        {
            var width = _draggedElement?.Bounds.Width ?? 120;
            var height = _draggedElement?.Bounds.Height ?? 40;
            return new Rect(0, 0, Math.Max(1d, width), Math.Max(1d, height));
        }

        if (_currentIndex >= SlotBounds.Count)
        {
            // Use the same calculation as ApplyPreviewTransform for consistency
            return CalculateNextSlotPosition();
        }

        if (_currentIndex < 0)
        {
            return SlotBounds[0];
        }

        return SlotBounds[_currentIndex];
    }

    private static void HideCrossCollectionPlaceholder()
    {
        ClearCrossCollectionPanelReserve();

        if (_crossCollectionPlaceholder == null || _overlayCanvas == null)
        {
            return;
        }

        _overlayCanvas.Children.Remove(_crossCollectionPlaceholder);
        _crossCollectionPlaceholder = null;
    }

    private static void CleanupProxyTransforms(ContentPresenter? container)
    {
        foreach (var child in LogicalChildren)
        {
            child.Transitions = null;
            child.RenderTransform = TransformOperations.Parse("none");
            child.Opacity = 1.0; // Restore full opacity
        }

        ClearCrossCollectionPanelReserve();

        if (container != null)
        {
            container.ZIndex = 0;
        }
    }

    private static void ResetDraggedElementVisuals()
    {
        if (_draggedElement != null)
        {
            _draggedElement.Transitions = null;
            _draggedElement.RenderTransform = TransformOperations.Parse("none");
            _draggedElement.Opacity = 1.0;
            _draggedElement.ZIndex = 0;
        }

        // Remove overlay canvas from whichever host currently contains it.
        if (_overlayCanvas != null)
        {
            HideCrossCollectionPlaceholder();

            if (_overlayCanvas.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(_overlayCanvas);
            }
            else if (_draggedElement != null)
            {
                var topLevel = TopLevel.GetTopLevel(_draggedElement);
                var overlayLayer = topLevel != null ? OverlayLayer.GetOverlayLayer(topLevel) : null;
                overlayLayer?.Children.Remove(_overlayCanvas);
            }

            _overlayCanvas = null;
        }

        _dragProxy = null;
        _dragProxyBitmap?.Dispose();
        _dragProxyBitmap = null;
    }

    private static void CommitDragOperation(ItemsControl? itemsControl)
    {
        if (_draggedData == null)
        {
            return;
        }

        // Cross-collection transfers are now fully command-driven.
        if (!ReferenceEquals(_sourceCollection, _targetCollection) && _targetCollection != null)
        {
            ExecuteDropCommand(itemsControl);
        }
        else if (_isSortableOnly && _currentIndex != _originalIndex)
        {
            // Same-collection reorder is now fully command-driven.
            ExecuteUpdateCommand(null);
        }
    }

    private static void ExecuteDropCommand(ItemsControl? itemsControl)
    {
        if (itemsControl == null)
        {
            return;
        }

        var dropCmd = SortableCommandResolver.ResolveDropCommand(
            itemsControl,
            GetDropCommand(itemsControl),
            GetTransferCommand(itemsControl));

        if (dropCmd == null)
        {
            Console.WriteLine("[DROP ERROR] Could not find DropCommand or TransferCommand");
            return;
        }

        var dropEventArgs = new SortableDropEventArgs
        {
            Item = _draggedData,
            SourceCollection = _sourceCollection,
            TargetCollection = _targetCollection,
            OldIndex = _originalSourceIndex,
            NewIndex = _currentIndex,
            TransferMode = GetCrossCollectionTransferMode(itemsControl)
        };

        if (dropCmd.CanExecute(dropEventArgs))
        {
            dropCmd.Execute(dropEventArgs);
        }
    }

    private static void ExecuteUpdateCommand(ICommand? updateCmd)
    {
        // If no command passed, try to get it from the current ItemsControl
        if (updateCmd == null && _currentItemsControl != null)
        {
            updateCmd = GetUpdateCommand(_currentItemsControl);
        }

        if (_sourceCollection != null)
        {
            if (_sourceCollection.Count > 0)
            {
                _currentIndex = Math.Max(0, Math.Min(_currentIndex, _sourceCollection.Count - 1));
            }
            else
            {
                _currentIndex = 0;
            }
        }

        var eventArgs = new SortableUpdateEventArgs
        {
            Item = _draggedData,
            OldIndex = _originalIndex,
            NewIndex = _currentIndex,
            SourceCollection = _sourceCollection,
            Mode = _currentItemsControl != null ? GetMode(_currentItemsControl) : SortableMode.Sort
        };

        if (updateCmd?.CanExecute(eventArgs) == true)
        {
            updateCmd.Execute(eventArgs);
        }
    }

    private static void ResetEngineState()
    {
        ClearCrossCollectionPanelReserve();

        _isDragging = false;
        _draggedElement = null;
        _draggedData = null;
        _currentItemsControl = null;
        _originalItemsControl = null;
        _endDragInProgress = false;
        _sourceCollection = null;
        _targetCollection = null;
        _sourceGroup = null;
        _currentPanel = null;
        _overlayCanvas = null;
        _dragProxy = null;
        _crossCollectionPlaceholder = null;
        _isSortableOnly = false;
        LogicalChildren.Clear();
        SlotBounds.Clear();
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => EndDrag();
    private static void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => EndDrag();

    private static void EndDrag()
    {
        if (!_isDragging || _draggedElement == null || _endDragInProgress) return;

        _endDragInProgress = true;

        var container = _draggedElement.FindAncestorOfType<ContentPresenter>();
        var itemsControl = _currentItemsControl ?? _draggedElement.FindAncestorOfType<ItemsControl>();
        var originalItemsControl = _originalItemsControl;

        try
        {
            CleanupProxyTransforms(container);
            CommitDragOperation(itemsControl);
        }
        finally
        {
            ResetDraggedElementVisuals();

            // Refresh snapshots for both current and original controls to keep programmatic animations stable.
            if (originalItemsControl != null)
            {
                RefreshStableBoundsSnapshot(originalItemsControl);
            }

            if (itemsControl != null && !ReferenceEquals(itemsControl, originalItemsControl))
            {
                RefreshStableBoundsSnapshot(itemsControl);
            }

            ResetEngineState();
        }
    }
}

