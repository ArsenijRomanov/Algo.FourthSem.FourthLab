using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;

namespace SlidingPuzzle.Core.Helpers;

public static class PathBuilder
{
    public static IReadOnlyList<Direction> Build(
        Dictionary<PuzzleBoard, (PuzzleBoard parent, Direction dir)> parents, 
        PuzzleBoard lastState)
    {
        var path = new List<Direction>();

        while (parents.TryGetValue(lastState, out var info))
        {
            path.Add(info.dir);
            lastState = info.parent;
        }

        var pathLen = path.Count;
        for (var i = 0; i < pathLen / 2; ++i)
            (path[i], path[pathLen - i - 1]) = (path[pathLen - i - 1], path[i]);

        return path;
    }
}
