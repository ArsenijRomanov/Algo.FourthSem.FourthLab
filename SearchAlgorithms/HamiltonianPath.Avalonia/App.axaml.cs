using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HamiltonianPath.Avalonia.ViewModels;
using HamiltonianPath.Avalonia.Views;
using SearchAlgorithms.UI.Shared.Services;

namespace HamiltonianPath.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new HamiltonianMainViewModel(new BenchmarkService())
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
