using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using HamiltonianPath.Avalonia.Models;
using HamiltonianPath.Avalonia.ViewModels;

namespace HamiltonianPath.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => UpdateToolSelectionVisual();
    }

    private HamiltonianMainViewModel? ViewModel => DataContext as HamiltonianMainViewModel;

    private void StartToolButton_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel?.SetTool(HamiltonianTool.Start);
        UpdateToolSelectionVisual();
    }

    private void FinishToolButton_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel?.SetTool(HamiltonianTool.Finish);
        UpdateToolSelectionVisual();
    }

    private void WallToolButton_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel?.SetTool(HamiltonianTool.Wall);
        UpdateToolSelectionVisual();
    }

    private void EraseToolButton_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel?.SetTool(HamiltonianTool.Erase);
        UpdateToolSelectionVisual();
    }

    private void UpdateToolSelectionVisual()
    {
        StartToolButton.Classes.Set("selected-tool", ViewModel?.SelectedTool == HamiltonianTool.Start);
        FinishToolButton.Classes.Set("selected-tool", ViewModel?.SelectedTool == HamiltonianTool.Finish);
        WallToolButton.Classes.Set("selected-tool", ViewModel?.SelectedTool == HamiltonianTool.Wall);
        EraseToolButton.Classes.Set("selected-tool", ViewModel?.SelectedTool == HamiltonianTool.Erase);
    }
}
