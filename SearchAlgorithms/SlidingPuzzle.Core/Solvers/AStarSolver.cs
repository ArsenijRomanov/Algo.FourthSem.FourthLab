using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Helpers;

namespace SlidingPuzzle.Core.Solvers;

public class AStarSolver : ISolver
{
    public SolveResult Solve(PuzzleBoard board, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(board);

        var closed = new HashSet<PuzzleBoard>();
        var parents = new Dictionary<PuzzleBoard, (PuzzleBoard parent, Direction dir)>();
        var gScore = new Dictionary<PuzzleBoard, int> { [board] = 0 };

        var openSet = new PriorityQueue<AStarPathState, int>();
        openSet.Enqueue(new AStarPathState(board, 0), 0 + board.TotalManhattanDistance);

        while (openSet.Count != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var curState = openSet.Dequeue();

            if (closed.Contains(curState.Board))
                continue;

            if (gScore.TryGetValue(curState.Board, out var bestG) && curState.StepCount > bestG)
                continue;

            if (curState.Board.IsGoal)
                return new SolveResult(PathBuilder.Build(parents, curState.Board), true);

            closed.Add(curState.Board);

            foreach (var dir in curState.Board.GetValidSteps())
            {
                var nextBoard = curState.Board.MakeStep(dir);

                if (closed.Contains(nextBoard))
                    continue;

                var tentativeG = curState.StepCount + 1;

                if (gScore.TryGetValue(nextBoard, out var knownG) && tentativeG >= knownG)
                    continue;

                gScore[nextBoard] = tentativeG;
                parents[nextBoard] = (curState.Board, dir);

                var nextState = new AStarPathState(nextBoard, tentativeG);
                openSet.Enqueue(nextState, nextState.Score);
            }
        }

        return new SolveResult([], false);
    }
}
