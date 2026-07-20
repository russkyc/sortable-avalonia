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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sortable.Avalonia.Demo.Models;

namespace Sortable.Avalonia.Demo.ViewModels.Demos;

public partial class VerticalListDemoViewModel : DemoViewModelBase
{
    [ObservableProperty] private ObservableCollection<SortableItem> _simpleList = [];

    public VerticalListDemoViewModel()
    {
        LoadItems();
    }

    [RelayCommand]
    private void LoadItems()
    {
        SimpleList =
        [
            new SortableItem("Fix auth timeout regression") { Tag = "P0", Note = "Backend · 2 PRs waiting" },
            new SortableItem("Resolve billing edge case") { Tag = "P0", Note = "Finance · Regression test" },
            new SortableItem("Prioritize customer bug reports") { Tag = "P1", Note = "CS escalation · 5 affected" },
            new SortableItem("Update analytics dashboard") { Tag = "P1", Note = "Product · Design ready" },
            new SortableItem("Migrate legacy endpoints") { Tag = "P2", Note = "Platform · Phase 2 scope" },
            new SortableItem("Audit third-party API tokens") { Tag = "P2", Note = "Security · Compliance due" },
            new SortableItem("Update dependency packages") { Tag = "P3", Note = "Maintenance" },
            new SortableItem("Redesign onboarding flow") { Tag = "P1", Note = "UX Design" },
            new SortableItem("Refactor data layer caching") { Tag = "P1", Note = "Performance" },
            new SortableItem("Fix memory leak in image parser") { Tag = "P0", Note = "Core Lib" },
            new SortableItem("Document new WebSocket API") { Tag = "P2", Note = "Docs" },
            new SortableItem("Set up nightly performance tests") { Tag = "P2", Note = "QA" },
            new SortableItem("Optimize database indexes") { Tag = "P1", Note = "DB Admin" },
            new SortableItem("Localize UI to French and Spanish") { Tag = "P3", Note = "L10n" },
            new SortableItem("Improve test coverage of auth module") { Tag = "P2", Note = "Testing" },
            new SortableItem("Add dark mode support") { Tag = "P3", Note = "Aesthetics" },
            new SortableItem("Resolve race condition in message queue") { Tag = "P0", Note = "Infra" },
            new SortableItem("Review community PRs") { Tag = "P2", Note = "OSS" },
            new SortableItem("Update privacy policy page") { Tag = "P3", Note = "Legal" },
            new SortableItem("Configure production alerting thresholds") { Tag = "P1", Note = "SRE" }
        ];
    }

    [RelayCommand]
    private void OnSortSimpleListProgrammatically()
    {
        if (SimpleList.Count > 0)
            LogEvent("⬆", $"Escalated '{SimpleList[^1].Name}' to top");
        RotateLastItemToFront(SimpleList);
    }

    [RelayCommand]
    private void OnRandomizeSimpleList()
    {
        LogEvent("🔀", "Queue shuffled randomly");
        ShuffleCollection(SimpleList);
    }

    [RelayCommand]
    private void OnItemReleased(SortableReleaseEventArgs e)
    {
        if (e.Item is not SortableItem item) return;
        LogEvent("ℹ️", $"'{item.Name}' removed from queue");
        SimpleList.Remove(item);
    }
}