using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace Sortable.Avalonia.Internal
{
    internal static class SortableEventHandlerHelper
    {
        // Methods and fields will be moved here from Sortable.Methods.cs

        public static void AttachProgrammaticAnimationTracking(ItemsControl itemsControl,
            WeakControlSet trackedItemsControls,
            Action<ItemsControl> refreshCollectionSubscription,
            Action<ItemsControl> queueStableBoundsWarmup,
            EventHandler<AvaloniaPropertyChangedEventArgs> propertyChangedHandler,
            EventHandler<VisualTreeAttachmentEventArgs> attachedHandler,
            EventHandler<VisualTreeAttachmentEventArgs> detachedHandler,
            EventHandler layoutUpdatedHandler)
        {
            if (!trackedItemsControls.Add(itemsControl))
            {
                refreshCollectionSubscription(itemsControl);
                queueStableBoundsWarmup(itemsControl);
                return;
            }

            itemsControl.PropertyChanged += propertyChangedHandler;
            itemsControl.AttachedToVisualTree += attachedHandler;
            itemsControl.DetachedFromVisualTree += detachedHandler;
            itemsControl.LayoutUpdated += layoutUpdatedHandler;

            refreshCollectionSubscription(itemsControl);
            queueStableBoundsWarmup(itemsControl);
        }

        public static void DetachProgrammaticAnimationTracking(ItemsControl itemsControl,
            WeakControlSet trackedItemsControls,
            ConditionalWeakTable<ItemsControl, INotifyCollectionChanged> observedCollections,
            ConditionalWeakTable<ItemsControl, NotifyCollectionChangedEventHandler> collectionHandlers,
            ConditionalWeakTable<ItemsControl, Dictionary<object, Rect>> lastStableBounds,
            List<ProgrammaticRemovalSnapshot> pendingProgrammaticRemovals,
            EventHandler<AvaloniaPropertyChangedEventArgs> propertyChangedHandler,
            EventHandler<VisualTreeAttachmentEventArgs> attachedHandler,
            EventHandler<VisualTreeAttachmentEventArgs> detachedHandler,
            EventHandler layoutUpdatedHandler)
        {
            if (!trackedItemsControls.Remove(itemsControl))
            {
                return;
            }

            itemsControl.PropertyChanged -= propertyChangedHandler;
            itemsControl.AttachedToVisualTree -= attachedHandler;
            itemsControl.DetachedFromVisualTree -= detachedHandler;
            itemsControl.LayoutUpdated -= layoutUpdatedHandler;

            if (observedCollections.TryGetValue(itemsControl, out var observedCollection) &&
                collectionHandlers.TryGetValue(itemsControl, out var handler))
            {
                observedCollection.CollectionChanged -= handler;
            }

            observedCollections.Remove(itemsControl);
            collectionHandlers.Remove(itemsControl);
            lastStableBounds.Remove(itemsControl);
            pendingProgrammaticRemovals.RemoveAll(snapshot => ReferenceEquals(snapshot.SourceItemsControl, itemsControl));
        }
    }
}
