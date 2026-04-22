using Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.VisualTree;
using SlidingPuzzle.Avalonia.ViewModels;
using SlidingPuzzle.Core.Enums;

namespace SlidingPuzzle.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private SlidingPuzzleMainViewModel? ViewModel => DataContext as SlidingPuzzleMainViewModel;

    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.ImportBoardAsync(this);
    }

    private async void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.ExportBoardAsync(this);
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is null || ViewModel.IsEditMode)
            return;

        if (FocusManager.GetFocusedElement() is Visual focused)
        {
            if (focused.GetSelfAndVisualAncestors().OfType<ComboBoxItem>().Any()
                || focused is ComboBox
                || focused is TextBox
                || focused is NumericUpDown)
            {
                return;
            }
        }

        switch (e.Key)
        {
            case Key.Left:
                ViewModel.TryManualMove(Direction.Left);
                e.Handled = true;
                break;
            case Key.Right:
                ViewModel.TryManualMove(Direction.Right);
                e.Handled = true;
                break;
            case Key.Up:
                ViewModel.TryManualMove(Direction.Up);
                e.Handled = true;
                break;
            case Key.Down:
                ViewModel.TryManualMove(Direction.Down);
                e.Handled = true;
                break;
        }
    }

    private void AlgorithmComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Focus();
    }
}
