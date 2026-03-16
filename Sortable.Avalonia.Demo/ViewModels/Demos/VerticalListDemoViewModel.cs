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
    [ObservableProperty] private ObservableCollection<SortableItem> _simpleList;

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