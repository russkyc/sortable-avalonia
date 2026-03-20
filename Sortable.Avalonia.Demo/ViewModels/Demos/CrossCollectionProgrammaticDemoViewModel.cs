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

public partial class CrossCollectionProgrammaticDemoViewModel : DemoViewModelBase
{
    private readonly Random _random = new();

    [ObservableProperty]
    private ObservableCollection<SortableItem> _queueA =
    [
        new SortableItem("Run billing reconciliation"),
        new SortableItem("Re-index search aliases"),
        new SortableItem("Review failed backup notifications")
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _queueB =
    [
        new SortableItem("Prepare release notes"),
        new SortableItem("Draft customer status email")
    ];

    [RelayCommand]
    private void OnItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem)
        {
            return;
        }

        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Move;
        _ = TryApplyDropMutation(e);
    }

    [RelayCommand]
    private void MoveTopAToB() => MoveTopItem(QueueA, QueueB);

    [RelayCommand]
    private void MoveTopBToA() => MoveTopItem(QueueB, QueueA);

    [RelayCommand]
    private void MoveRandomDirection()
    {
        var moveAToB = QueueB.Count == 0 || (QueueA.Count > 0 && _random.Next(2) == 0);
        if (moveAToB)
        {
            MoveTopItem(QueueA, QueueB);
            return;
        }

        MoveTopItem(QueueB, QueueA);
    }

    private new static void MoveTopItem(ObservableCollection<SortableItem> source, ObservableCollection<SortableItem> target)
    {
        if (source.Count == 0)
        {
            return;
        }

        var item = source[0];
        source.RemoveAt(0);

        var targetIndex = Math.Min(1, target.Count);
        target.Insert(targetIndex, item);
    }
}
