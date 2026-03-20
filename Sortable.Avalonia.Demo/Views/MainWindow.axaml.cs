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

using System.Collections.Generic;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Sortable.Avalonia.Demo.ViewModels;
using Sortable.Avalonia.Demo.Views.Demos;

namespace Sortable.Avalonia.Demo.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, Control> _viewCache = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => SelectFirstNavItem();
    }

    private void SelectFirstNavItem()
    {
        // Walk MenuItems to find the first NavigationViewItem (skip headers)
        foreach (var menuItem in NavView.MenuItems)
        {
            if (menuItem is NavigationViewItem navItem)
            {
                NavView.SelectedItem = navItem;
                return;
            }
        }
    }

    private void NavView_OnSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (e.SelectedItem is not NavigationViewItem item) return;

        var tag = item.Tag?.ToString();
        if (tag is null) return;

        if (!_viewCache.TryGetValue(tag, out var view))
        {
            view = tag switch
            {
                "VerticalList"               => new VerticalListDemoView { DataContext = vm.VerticalListDemo },
                "HorizontalStack"            => new HorizontalStackDemoView { DataContext = vm.HorizontalStackDemo },
                "GridLayout"                 => new GridLayoutDemoView { DataContext = vm.GridLayoutDemo },
                "Kanban"                     => new KanbanBoardDemoView { DataContext = vm.KanbanBoardDemo },
                "DragHandle"                 => new DragHandleDemoView { DataContext = vm.DragHandleDemo },
                "DraggingTemplate"           => new DraggingTemplateDemoView { DataContext = new ViewModels.Demos.DraggingTemplateDemoViewModel() },
                "MultipleGroups"             => new MultipleGroupsDemoView { DataContext = vm.MultipleGroupsDemo },
                "DisabledItems"              => new DisabledItemsDemoView { DataContext = vm.DisabledItemsDemo },
                "SortableOnly"               => new SortableOnlyDemoView { DataContext = vm.SortableOnlyDemo },
                "DroppableOnly"              => new DroppableOnlyDemoView { DataContext = vm.DroppableOnlyDemo },
                "CrossCollectionInteraction" => new CrossCollectionInteractionDemoView { DataContext = vm.CrossCollectionInteractionDemo },
                "ConditionalAcceptance"      => new ConditionalAcceptanceDemoView { DataContext = vm.ConditionalAcceptanceDemo },
                "CopyMode"                   => new CopyModeDemoView { DataContext = vm.CopyModeDemo },
                "SortMode"                   => new SortModeDemoView { DataContext = vm.SortModeDemo },
                "SwapMode"                   => new SwapModeDemoView { DataContext = vm.SwapModeDemo },
                "CrossSwap"                  => new CrossCollectionSwapDemoView { DataContext = vm.CrossCollectionSwapDemo },
                "UniformGridSwap"            => new UniformGridSwapDemoView { DataContext = vm.UniformGridSwapDemo },
                _                            => null
            };

            if (view is not null)
                _viewCache[tag] = view;
        }

        DemoContent.Content = view;
    }
}
