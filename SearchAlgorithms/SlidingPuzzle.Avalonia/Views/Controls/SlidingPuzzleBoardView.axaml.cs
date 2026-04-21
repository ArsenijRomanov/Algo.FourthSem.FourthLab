using global::Avalonia.Controls;
using global::Avalonia.Input;
using SlidingPuzzle.Avalonia.ViewModels;

namespace SlidingPuzzle.Avalonia.Views.Controls;

public partial class SlidingPuzzleBoardView : UserControl
{
    private int? _dragSourceIndex;
    private int? _dragTargetIndex;
    private int? _selectedSwapSourceIndex;
    private bool _dragMoved;

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
        _dragTargetIndex = tile.Index;
        _dragMoved = false;

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
        {
            _dragTargetIndex = tile.Index;
            _dragMoved = _dragSourceIndex != tile.Index;
            ViewModel.UpdateDragTarget(tile.Index);
        }
    }

    private void Tile_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragSourceIndex is null || ViewModel is null)
        {
            ViewModel?.ClearDragVisuals();
            _dragSourceIndex = null;
            _dragTargetIndex = null;
            return;
        }

        if (ViewModel.IsEditMode)
        {
            if (_dragMoved && _dragTargetIndex is not null && _dragTargetIndex != _dragSourceIndex)
            {
                ViewModel.TrySwapTiles(_dragSourceIndex.Value, _dragTargetIndex.Value);
                _selectedSwapSourceIndex = null;
                ViewModel.ClearDragVisuals();
            }
            else
            {
                HandleEditClick(_dragSourceIndex.Value);
            }
        }
        else
        {
            ViewModel.ClearDragVisuals();
        }

        _dragSourceIndex = null;
        _dragTargetIndex = null;
        _dragMoved = false;
    }

    private void HandleEditClick(int tileIndex)
    {
        if (ViewModel is null || !ViewModel.IsEditMode)
            return;

        if (_selectedSwapSourceIndex is null)
        {
            _selectedSwapSourceIndex = tileIndex;
            ViewModel.BeginDrag(tileIndex);
            return;
        }

        if (_selectedSwapSourceIndex == tileIndex)
        {
            _selectedSwapSourceIndex = null;
            ViewModel.ClearDragVisuals();
            return;
        }

        ViewModel.TrySwapTiles(_selectedSwapSourceIndex.Value, tileIndex);
        _selectedSwapSourceIndex = null;
        ViewModel.ClearDragVisuals();
    }
}
