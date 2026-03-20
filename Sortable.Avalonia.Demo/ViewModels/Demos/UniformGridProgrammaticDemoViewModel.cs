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

public partial class UniformGridProgrammaticDemoViewModel : DemoViewModelBase
{
    private readonly Random _random = new();

    [ObservableProperty]
    private ObservableCollection<SortableItem> _laneA =
    [
        new SortableItem("Auth Service"),
        new SortableItem("Billing API"),
        new SortableItem("Search Engine"),
        new SortableItem("Notification Worker")
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _laneB =
    [
        new SortableItem("Mobile iOS"),
        new SortableItem("Mobile Android"),
        new SortableItem("Admin Portal")
    ];

    [RelayCommand]
    private void MoveTopAToB() => MoveTopItem(LaneA, LaneB);

    [RelayCommand]
    private void MoveTopBToA() => MoveTopItem(LaneB, LaneA);

    [RelayCommand]
    private void MoveRandomDirection()
    {
        var moveAToB = LaneB.Count == 0 || (LaneA.Count > 0 && _random.Next(2) == 0);
        if (moveAToB)
        {
            MoveTopItem(LaneA, LaneB);
            return;
        }

        MoveTopItem(LaneB, LaneA);
    }

    [RelayCommand]
    private void OnItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem)
        {
            return;
        }

        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Move;
        if (!TryApplyDropMutation(e))
        {
            return;
        }
    }

    private new static void MoveTopItem(ObservableCollection<SortableItem> source, ObservableCollection<SortableItem> target)
    {
        if (source.Count == 0)
        {
            return;
        }

        var item = source[0];
        source.RemoveAt(0);
        target.Add(item);
    }
}
