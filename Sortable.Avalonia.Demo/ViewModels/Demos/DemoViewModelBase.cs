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
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sortable.Avalonia.Demo.Models;

namespace Sortable.Avalonia.Demo.ViewModels.Demos;

public abstract partial class DemoViewModelBase : ViewModelBase
{
    private const int MaxLogEntries = 50;

    [ObservableProperty]
    private ObservableCollection<EventLogEntry> _eventLog = new();

    [RelayCommand]
    private void OnItemUpdated(SortableUpdateEventArgs e)
    {
        var updated = e.ApplyUpdateMutation();
        if (!updated) return;

        if (e.Item is SortableItem item)
            LogEvent("↕", $"'{item.Name}' moved from position {e.OldIndex + 1} → {e.NewIndex + 1}");
    }

    [RelayCommand]
    private void ClearEventLog() => EventLog.Clear();

    protected void LogEvent(string emoji, string message)
    {
        EventLog.Insert(0, new EventLogEntry(emoji, message, DateTime.Now.ToString("HH:mm:ss")));
        while (EventLog.Count > MaxLogEntries)
            EventLog.RemoveAt(EventLog.Count - 1);
    }

    protected static bool ApplyDropMutation(SortableDropEventArgs e) => e.ApplyDropMutation();

    protected static bool TryApplyDropMutation(SortableDropEventArgs e)
    {
        var applied = ApplyDropMutation(e);
        if (!applied) e.IsAccepted = false;
        return applied;
    }

    protected static void RotateLastItemToFront(ObservableCollection<SortableItem> items)
    {
        if (items.Count < 2) return;
        items.Move(items.Count - 1, 0);
    }

    protected static void SwapFirstAndLast(ObservableCollection<SortableItem> items)
    {
        if (items.Count < 2) return;
        items.Move(items.Count - 1, 0);
        items.Move(1, items.Count - 1);
    }

    protected static void SwapRandomItems(ObservableCollection<SortableItem> items)
    {
        if (items.Count < 2) return;
        var random = new Random();
        var swapCount = Math.Max(2, items.Count / 2);
        for (var i = 0; i < swapCount; i++)
        {
            var index1 = random.Next(items.Count);
            var index2 = random.Next(items.Count);
            if (index1 == index2) continue;
            var minIndex = Math.Min(index1, index2);
            var maxIndex = Math.Max(index1, index2);
            items.Move(maxIndex, minIndex);
            items.Move(minIndex + 1, maxIndex);
        }
    }

    protected static void ShuffleCollection(ObservableCollection<SortableItem> items)
    {
        if (items.Count < 2) return;
        var random = new Random();
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            if (i != j) items.Move(i, j);
        }
    }

    protected static void MoveTopItem(ObservableCollection<SortableItem> source, ObservableCollection<SortableItem> target)
    {
        if (source.Count == 0) return;
        var item = source[0];
        source.RemoveAt(0);
        target.Add(item);
    }
}
