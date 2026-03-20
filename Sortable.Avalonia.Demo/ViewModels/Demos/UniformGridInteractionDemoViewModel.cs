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

public partial class UniformGridInteractionDemoViewModel : DemoViewModelBase
{
    private readonly Random _random = new();

    [ObservableProperty]
    private ObservableCollection<SortableItem> _laneA =
    [
        new SortableItem("Auth Service")       { Tag = "🔑", Note = "3 replicas · Healthy" },
        new SortableItem("Billing API")        { Tag = "💳", Note = "1 replica · Degraded" },
        new SortableItem("Search Engine")      { Tag = "🔍", Note = "5 replicas · Healthy" },
        new SortableItem("Notification Worker") { Tag = "🔔", Note = "2 replicas · Healthy" },
        new SortableItem("Data Warehouse")     { Tag = "🗄",  Note = "1 replica · Maintenance" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _laneB =
    [
        new SortableItem("Mobile iOS")    { Tag = "📱", Note = "CDN · v4.2.1" },
        new SortableItem("Mobile Android") { Tag = "🤖", Note = "CDN · v4.1.9" },
        new SortableItem("Admin Portal")  { Tag = "🖥",  Note = "Edge · v2.0.3" },
        new SortableItem("Feature Flags") { Tag = "🚩", Note = "Global · 48 flags" },
    ];

    [RelayCommand]
    private void OnItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem item)
            return;
        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Move;
        var isToB = ReferenceEquals(e.TargetCollection, LaneB);
        if (TryApplyDropMutation(e))
            LogEvent("🔄", $"'{item.Name}' transferred to {(isToB ? "Product" : "Platform")} Team (drag)");
    }

    [RelayCommand]
    private void MoveTopAToB()
    {
        if (LaneA.Count == 0) return;
        LogEvent("→", $"'{LaneA[0].Name}' transferred to Product Team");
        MoveTopItem(LaneA, LaneB);
    }

    [RelayCommand]
    private void MoveTopBToA()
    {
        if (LaneB.Count == 0) return;
        LogEvent("←", $"'{LaneB[0].Name}' transferred to Platform Team");
        MoveTopItem(LaneB, LaneA);
    }

    [RelayCommand]
    private void MoveRandomDirection()
    {
        var moveAToB = LaneB.Count == 0 || (LaneA.Count > 0 && _random.Next(2) == 0);
        if (moveAToB)
        {
            if (LaneA.Count == 0) return;
            LogEvent("→", $"'{LaneA[0].Name}' transferred to Product Team (random)");
            MoveTopItem(LaneA, LaneB);
        }
        else
        {
            if (LaneB.Count == 0) return;
            LogEvent("←", $"'{LaneB[0].Name}' transferred to Platform Team (random)");
            MoveTopItem(LaneB, LaneA);
        }
    }

    private new static void MoveTopItem(ObservableCollection<SortableItem> source, ObservableCollection<SortableItem> target)
    {
        if (source.Count == 0) return;
        var item = source[0];
        source.RemoveAt(0);
        target.Add(item);
    }
}
