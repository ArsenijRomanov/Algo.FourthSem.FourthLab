using global::Avalonia;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Markup.Xaml;
using SearchAlgorithms.UI.Shared.Services;
using SlidingPuzzle.Avalonia.ViewModels;
using SlidingPuzzle.Avalonia.Views;

namespace SlidingPuzzle.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new SlidingPuzzleMainViewModel(new BenchmarkService(), new FileStorageService())
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
