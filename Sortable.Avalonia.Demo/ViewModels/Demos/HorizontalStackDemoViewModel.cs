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

public partial class HorizontalStackDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _horizontalItems =
    [
        new SortableItem("Ideas")       { Tag = "💡", Note = "34 items" },
        new SortableItem("Backlog")     { Tag = "📋", Note = "67 items" },
        new SortableItem("In Progress") { Tag = "⚙",  Note = "8 active" },
        new SortableItem("In Review")   { Tag = "👁",  Note = "4 open PRs" },
        new SortableItem("Staging")     { Tag = "🚀", Note = "2 queued" },
        new SortableItem("Released")    { Tag = "✅", Note = "142 shipped" },
    ];

    [RelayCommand]
    private void OnItemRelease(SortableReleaseEventArgs e)
    {
        if (e.Item is not  SortableItem item) return;
        LogEvent("ℹ️", $"'{item.Name}' released without dropping");
    }

    [RelayCommand]
    private void OnSortHorizontalItemsProgrammatically()
    {
        if (HorizontalItems.Count > 0)
            LogEvent("⬅", $"Moved '{HorizontalItems[^1].Name}' to front");
        RotateLastItemToFront(HorizontalItems);
    }

    [RelayCommand]
    private void OnRandomizeHorizontalItems()
    {
        LogEvent("🔀", "Pipeline stages shuffled");
        ShuffleCollection(HorizontalItems);
    }
}
