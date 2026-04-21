using Avalonia.Controls;
using Avalonia.Input;
using SlidingPuzzle.App.ViewModels;

namespace SlidingPuzzle.App;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        switch (e.Key)
        {
            case Key.Up: vm.MoveBlankUp(); break;
            case Key.Down: vm.MoveBlankDown(); break;
            case Key.Left: vm.MoveBlankLeft(); break;
            case Key.Right: vm.MoveBlankRight(); break;
        }
    }
}
