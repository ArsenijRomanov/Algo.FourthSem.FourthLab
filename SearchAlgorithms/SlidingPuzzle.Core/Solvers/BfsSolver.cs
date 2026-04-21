using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Helpers;

namespace SlidingPuzzle.Core.Solvers;

public class BfsSolver : ISolver
{
    public SolveResult Solve(PuzzleBoard board)
    {
        var visited = new HashSet<PuzzleBoard>();
        var parents = new Dictionary<PuzzleBoard, (PuzzleBoard parent, Direction dir)>();
        var queue = new Queue<PuzzleBoard>();
        
        queue.Enqueue(board);
        visited.Add(board);

        while (queue.Count != 0)
        {
            var curState = queue.Dequeue();
            if (curState.IsGoal)
                return new SolveResult(PathBuilder.Build(parents, curState), true);
            
            foreach (var dir in curState.GetValidSteps())
            {
                var nextState = curState.MakeStep(dir);
                if (visited.Contains(nextState)) continue;
                
                parents[nextState] = (curState, dir);
                visited.Add(nextState);
                queue.Enqueue(nextState);
            }
        }

        return new SolveResult([], false);
    }
}
