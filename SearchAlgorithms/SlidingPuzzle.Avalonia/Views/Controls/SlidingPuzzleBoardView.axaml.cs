using Avalonia.Controls;
using Avalonia.Input;
using SlidingPuzzle.Avalonia.ViewModels;

namespace SlidingPuzzle.Avalonia.Views.Controls;

public partial class SlidingPuzzleBoardView : UserControl
{
    private int? _dragSourceIndex;

    public SlidingPuzzleBoardView()
    {
        InitializeComponent();
    }

    private SlidingPuzzleMainViewModel? ViewModel => DataContext as SlidingPuzzleMainViewModel;

    private void Tile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border { DataContext: PuzzleTileViewModel tile } || ViewModel is null)
            return;

        _dragSourceIndex = tile.Index;

        if (ViewModel.IsEditMode)
        {
            ViewModel.BeginDrag(tile.Index);
            return;
        }

        if (!tile.IsBlank)
            ViewModel.TryHandleTileClick(tile.Index);
    }

    private void Tile_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (_dragSourceIndex is null || sender is not Border { DataContext: PuzzleTileViewModel tile } || ViewModel is null)
            return;

        if (ViewModel.IsEditMode)
            ViewModel.UpdateDragTarget(tile.Index);
    }

    private void Tile_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragSourceIndex is null || sender is not Border { DataContext: PuzzleTileViewModel tile } || ViewModel is null)
        {
            ViewModel?.ClearDragVisuals();
            _dragSourceIndex = null;
            return;
        }

        if (ViewModel.IsEditMode)
            ViewModel.TrySwapTiles(_dragSourceIndex.Value, tile.Index);

        ViewModel.ClearDragVisuals();
        _dragSourceIndex = null;
    }
}
