
using HamiltonianPath.Core;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Strategies;

const int height = 7;
const int width = 7;

var matrix = new int[height, width];
var start = new Point(0, 0);
var finish = new Point(0, 3);

var board = new Board(matrix, start, finish);

var chooseDirection = new BaseChooseDirection();
var commitValidator = new BaseCommitValidator();

var solver = new HamiltonianPathSolver(chooseDirection, commitValidator, false);

var solution = solver.Solve(board);
if (!solution)
{
    Console.WriteLine("fail");
    return;
}

for (var y = 0; y < height; ++y)
{
    for (var x = 0; x < width; ++x)
        Console.Write($"{board[y, x]} ");
    
    Console.WriteLine();
}


