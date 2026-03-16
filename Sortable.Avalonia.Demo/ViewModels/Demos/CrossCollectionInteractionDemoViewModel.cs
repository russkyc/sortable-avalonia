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

public partial class CrossCollectionInteractionDemoViewModel : DemoViewModelBase
{
    private readonly Random _random = new();

    [ObservableProperty]
    private ObservableCollection<SortableItem> _intake =
    [
        new SortableItem("SSO timeout after MFA challenge")         { Tag = "📥", Note = "Reported by 3 enterprise tenants" },
        new SortableItem("API 429 spikes in EU region")             { Tag = "📥", Note = "Rate-limit config suspected" },
        new SortableItem("Intermittent webhook retries overloading") { Tag = "📥", Note = "Retry storm on staging env" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _resolution =
    [
        new SortableItem("Patch window approval confirmed") { Tag = "✅", Note = "Change board approved Fri 22:00" },
        new SortableItem("Incident timeline published")    { Tag = "✅", Note = "Shared with CS and stakeholders" },
    ];

    [RelayCommand]
    private void OnItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem item)
            return;
        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Move;
        var isToResolution = ReferenceEquals(e.TargetCollection, Resolution);
        if (TryApplyDropMutation(e))
            LogEvent(isToResolution ? "✅" : "📥",
                $"'{item.Name}' moved to {(isToResolution ? "Resolved" : "Intake")} (drag)");
    }

    [RelayCommand]
    private void OnItemReleased(SortableReleaseEventArgs e)
    {
        LogEvent("ℹ️", $"'{e.Item}' released without dropping");
    }

    [RelayCommand]
    private void MoveTopIntakeToResolution()
    {
        if (Intake.Count == 0) return;
        LogEvent("✅", $"'{Intake[0].Name}' resolved (programmatic)");
        MoveTopItem(Intake, Resolution);
    }

    [RelayCommand]
    private void MoveTopResolutionToIntake()
    {
        if (Resolution.Count == 0) return;
        LogEvent("📥", $"'{Resolution[0].Name}' re-opened (programmatic)");
        MoveTopItem(Resolution, Intake);
    }

    [RelayCommand]
    private void MoveRandomDirection()
    {
        var moveAToB = Resolution.Count == 0 || (Intake.Count > 0 && _random.Next(2) == 0);
        if (moveAToB)
        {
            if (Intake.Count == 0) return;
            LogEvent("✅", $"'{Intake[0].Name}' resolved (random)");
            MoveTopItem(Intake, Resolution);
        }
        else
        {
            if (Resolution.Count == 0) return;
            LogEvent("📥", $"'{Resolution[0].Name}' re-opened (random)");
            MoveTopItem(Resolution, Intake);
        }
    }

    private static void MoveTopItem(ObservableCollection<SortableItem> source, ObservableCollection<SortableItem> target)
    {
        if (source.Count == 0) return;
        var item = source[0];
        source.RemoveAt(0);
        target.Insert(0, item);
    }
}
