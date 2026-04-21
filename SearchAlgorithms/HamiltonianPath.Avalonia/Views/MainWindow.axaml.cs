using Avalonia.Controls;
using Avalonia.Interactivity;
using HamiltonianPath.Avalonia.Models;
using HamiltonianPath.Avalonia.ViewModels;

namespace HamiltonianPath.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private HamiltonianMainViewModel? ViewModel => DataContext as HamiltonianMainViewModel;

    private void StartToolButton_Click(object? sender, RoutedEventArgs e) => ViewModel?.SetTool(HamiltonianTool.Start);
    private void FinishToolButton_Click(object? sender, RoutedEventArgs e) => ViewModel?.SetTool(HamiltonianTool.Finish);
    private void WallToolButton_Click(object? sender, RoutedEventArgs e) => ViewModel?.SetTool(HamiltonianTool.Wall);
    private void EraseToolButton_Click(object? sender, RoutedEventArgs e) => ViewModel?.SetTool(HamiltonianTool.Erase);
}
