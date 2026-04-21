using Avalonia.Controls;
using Avalonia.Input;
using HamiltonianPath.Avalonia.Models;
using HamiltonianPath.Avalonia.ViewModels;

namespace HamiltonianPath.Avalonia.Views.Controls;

public partial class HamiltonianBoardView : UserControl
{
    private bool _isPointerDown;

    public HamiltonianBoardView()
    {
        InitializeComponent();
    }

    private HamiltonianMainViewModel? ViewModel => DataContext as HamiltonianMainViewModel;

    private void Cell_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: HamiltonianBoardCellViewModel cell })
            return;

        _isPointerDown = true;
        Apply(cell);
    }

    private void Cell_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (!_isPointerDown)
            return;

        if (sender is not Control { DataContext: HamiltonianBoardCellViewModel cell })
            return;

        if (ViewModel?.SelectedTool is HamiltonianTool.Wall)
            ViewModel.PaintWalls(cell.Row, cell.Column, erase: false);
        else if (ViewModel?.SelectedTool is HamiltonianTool.Erase)
            ViewModel.PaintWalls(cell.Row, cell.Column, erase: true);
    }

    private void Cell_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => _isPointerDown = false;

    private void Apply(HamiltonianBoardCellViewModel cell)
    {
        if (ViewModel is null)
            return;

        if (ViewModel.SelectedTool is HamiltonianTool.Wall)
            ViewModel.PaintWalls(cell.Row, cell.Column, erase: false);
        else if (ViewModel.SelectedTool is HamiltonianTool.Erase)
            ViewModel.PaintWalls(cell.Row, cell.Column, erase: true);
        else
            ViewModel.ApplyToolToCell(cell.Row, cell.Column);
    }
}
