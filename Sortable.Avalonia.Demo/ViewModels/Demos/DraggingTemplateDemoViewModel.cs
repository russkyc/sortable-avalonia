// MIT License
// Copyright (c) 2026 Russell Camo (russkyc)

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sortable.Avalonia.Demo.Models;
namespace Sortable.Avalonia.Demo.ViewModels.Demos;
public partial class DraggingTemplateDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _intake = new()
    {
        new SortableItem("Shipping") { Tag = "🚚", Note = "Logistics and tracking" },
        new SortableItem("Inventory") { Tag = "📦", Note = "Stock and warehouse" },
        new SortableItem("Returns") { Tag = "↩️", Note = "Customer returns" }
    };

    [ObservableProperty]
    private ObservableCollection<SortableItem> _resolution = new()
    {
        new SortableItem("Billing") { Tag = "💳", Note = "Invoices and payments" },
        new SortableItem("Support") { Tag = "🛟", Note = "Customer support" }
    };

    private readonly Random _random = new();

    [RelayCommand]
    private void ItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem item)
            return;
        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Move;
        var isToResolution = ReferenceEquals(e.TargetCollection, Resolution);
        if (TryApplyDropMutation(e))
            LogEvent(isToResolution ? "💳" : "🚚",
                $"'{item.Name}' moved to {(isToResolution ? "Resolved" : "Intake")}");
    }

    [RelayCommand]
    private void ItemReleased(SortableReleaseEventArgs e)
    {
        LogEvent("ℹ️", $"'{e.Item}' released without dropping");
    }

    [RelayCommand]
    private void ItemUpdated(SortableUpdateEventArgs e)
    {
        // Optionally handle update mutation here, or rely on base class command.
        // If you want to log, you can do so here.
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
}
