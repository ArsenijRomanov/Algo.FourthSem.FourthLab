using System.Diagnostics;
using System.Text.Json;
using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Solvers;

const string DefaultConfigPath = "slidingpuzzle.loadtest.config.json";

var configPath = args.Length > 0 ? args[0] : DefaultConfigPath;
if (!File.Exists(configPath))
{
    ConsoleUi.WriteLine($"Config file not found: {configPath}", ConsoleColor.Red);
    Environment.Exit(1);
    return;
}

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
};

LoadTestConfig config;
await using (var fs = File.OpenRead(configPath))
{
    config = await JsonSerializer.DeserializeAsync<LoadTestConfig>(fs, jsonOptions)
             ?? throw new InvalidOperationException("Cannot parse load test config.");
}

config.Validate();

var algorithmFactories = BuildAlgorithmFactories();
foreach (var algorithmName in config.Algorithms)
{
    if (!algorithmFactories.ContainsKey(algorithmName))
        throw new InvalidOperationException($"Unknown algorithm: {algorithmName}");
}

var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? Directory.GetCurrentDirectory();
var suiteInputs = LoadSuiteInputs(config, configDirectory);

var outputPath = Path.GetFullPath(config.Output.RawResultsFile);
var outputDir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrWhiteSpace(outputDir))
    Directory.CreateDirectory(outputDir);

await using var writer = new StreamWriter(outputPath, false);
var runStartedUtc = DateTime.UtcNow;
var rowCount = 0;
var timeoutRows = 0;
var errorRows = 0;
var okRows = 0;
var skippedRows = 0;

var blockedAlgorithms = new Dictionary<string, BlockedAlgorithmState>(StringComparer.OrdinalIgnoreCase);

ConsoleUi.WriteLine("=== SlidingPuzzle.LoadTest started ===", ConsoleColor.Cyan);
ConsoleUi.WriteLine($"Config: {Path.GetFullPath(configPath)}", ConsoleColor.DarkCyan);
ConsoleUi.WriteLine($"Output: {outputPath}", ConsoleColor.DarkCyan);
ConsoleUi.WriteLine($"Algorithms: {string.Join(", ", config.Algorithms)}", ConsoleColor.DarkCyan);

for (var suiteIndex = 0; suiteIndex < suiteInputs.Count; suiteIndex++)
{
    var suite = suiteInputs[suiteIndex];
    ConsoleUi.WriteLine($"[SUITE] {suite.Definition.Name} | {suite.Definition.Width}x{suite.Definition.Height} depth={suite.Definition.Depth} cases={suite.Cases.Count} runs={suite.Definition.Runs}", ConsoleColor.Cyan);

    foreach (var caseItem in suite.Cases)
    {
        ConsoleUi.WriteLine($"  [CASE {caseItem.CaseId}] {FormatBoard(caseItem.Tiles, suite.Definition.Width)}", ConsoleColor.Gray);

        foreach (var algorithmName in config.Algorithms)
        {
            if (ShouldSkipAlgorithm(algorithmName, suite.Definition, blockedAlgorithms, out var skipReason))
            {
                for (var runIndex = 1; runIndex <= suite.Definition.Runs; runIndex++)
                {
                    var skippedResult = SingleRunMeasurement.Skipped(skipReason);
                    var skippedRow = CreateResultRow(runStartedUtc, suite.Definition, caseItem, algorithmName, runIndex, skippedResult);
                    await writer.WriteLineAsync(JsonSerializer.Serialize(skippedRow));
                    rowCount++;
                    skippedRows++;
                    PrintRunResult(suite.Definition, caseItem, algorithmName, runIndex, suite.Definition.Runs, skippedResult);
                }

                continue;
            }

            var factory = algorithmFactories[algorithmName];
            ConsoleUi.WriteLine($"    [ALG] {algorithmName}", ConsoleColor.Yellow);

            var timeoutEncountered = false;

            for (var runIndex = 1; runIndex <= suite.Definition.Runs; runIndex++)
            {
                for (var warmup = 0; warmup < config.Execution.WarmupRuns; warmup++)
                    _ = RunSingle(factory, caseItem, suite.Definition, config.Execution.TimeoutMs, measureMemory: false, forceFullGcBeforeRun: false);

                var result = RunSingle(
                    factory,
                    caseItem,
                    suite.Definition,
                    config.Execution.TimeoutMs,
                    config.Measurements.MeasureMemory,
                    config.Measurements.ForceFullGcBeforeRun);

                var row = CreateResultRow(runStartedUtc, suite.Definition, caseItem, algorithmName, runIndex, result);
                await writer.WriteLineAsync(JsonSerializer.Serialize(row));
                rowCount++;

                switch (result.Status)
                {
                    case RunStatus.Ok:
                        okRows++;
                        break;
                    case RunStatus.Timeout:
                        timeoutRows++;
                        timeoutEncountered = true;
                        break;
                    case RunStatus.Error:
                        errorRows++;
                        break;
                    case RunStatus.Skipped:
                        skippedRows++;
                        break;
                }

                PrintRunResult(suite.Definition, caseItem, algorithmName, runIndex, suite.Definition.Runs, result);

                if (result.Status == RunStatus.Timeout && config.Execution.StopOnTimeout.Enabled)
                {
                    var blockState = new BlockedAlgorithmState(
                        suite.Definition.Width,
                        suite.Definition.Height,
                        suite.Definition.Depth,
                        suite.Definition.Name,
                        config.Execution.StopOnTimeout.SkipAllNextSuitesWithLargerSize,
                        config.Execution.StopOnTimeout.SkipAllNextSuitesWithSameSizeAndGreaterDepth);

                    blockedAlgorithms[algorithmName] = blockState;

                    if (config.Execution.StopOnTimeout.SkipCurrentSuiteRemainingCases)
                    {
                        ConsoleUi.WriteLine(
                            $"    [STOP] {algorithmName} timed out. Remaining cases of suite '{suite.Definition.Name}' and configured future suites will be skipped.",
                            ConsoleColor.Magenta);
                        break;
                    }
                }
            }

            if (timeoutEncountered && config.Execution.StopOnTimeout.Enabled && config.Execution.StopOnTimeout.SkipCurrentSuiteRemainingCases)
                break;
        }
    }
}

await writer.FlushAsync();
ConsoleUi.WriteLine("=== SlidingPuzzle.LoadTest finished ===", ConsoleColor.Cyan);
ConsoleUi.WriteLine($"[TOTAL] rows={rowCount}, ok={okRows}, timeout={timeoutRows}, error={errorRows}, skipped={skippedRows}", ConsoleColor.Cyan);
ConsoleUi.WriteLine($"Results file: {outputPath}", ConsoleColor.Cyan);

static Dictionary<string, Func<ISolver>> BuildAlgorithmFactories() =>
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["bfs"] = () => new BfsSolver(),
        ["astar"] = () => new AStarSolver(),
        ["idastar"] = () => new IdaSolver(),
        ["idabackjump"] = () => new IdaBackJumpSolver()
    };

static List<SuiteInput> LoadSuiteInputs(LoadTestConfig config, string configDirectory)
{
    var list = new List<SuiteInput>(config.Suites.Count);

    foreach (var suite in config.Suites)
    {
        var path = Path.IsPathRooted(suite.CasesFile)
            ? suite.CasesFile
            : Path.GetFullPath(Path.Combine(configDirectory, suite.CasesFile));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Cases file not found: {path}");

        var file = JsonSerializer.Deserialize<CaseFile>(File.ReadAllText(path))
                   ?? throw new InvalidOperationException($"Cannot parse cases file: {path}");

        if (file.Boards.Count == 0)
            throw new InvalidOperationException($"No boards in cases file: {path}");

        if (file.Depth != 0 && file.Depth != suite.Depth)
            throw new InvalidOperationException($"Depth mismatch in {path}: suite.depth={suite.Depth}, file.depth={file.Depth}.");

        var cases = new List<PuzzleCase>(file.Boards.Count);
        for (var i = 0; i < file.Boards.Count; i++)
        {
            var tiles = file.Boards[i];
            if (tiles.Count != suite.Width * suite.Height)
                throw new InvalidOperationException($"Board #{i + 1} in {path} has invalid tile count. Expected {suite.Width * suite.Height}, got {tiles.Count}.");

            var byteTiles = tiles.Select(t => checked((byte)t)).ToArray();
            if (!PuzzleBoard.IsValidBoard(byteTiles, (byte)suite.Height, (byte)suite.Width))
                throw new InvalidOperationException($"Board #{i + 1} in {path} is invalid.");

            if (!PuzzleBoard.IsSolvable(byteTiles, (byte)suite.Height, (byte)suite.Width))
                throw new InvalidOperationException($"Board #{i + 1} in {path} is not solvable.");

            cases.Add(new PuzzleCase(i + 1, byteTiles));
        }

        list.Add(new SuiteInput(suite, cases));
    }

    return list;
}

static bool ShouldSkipAlgorithm(
    string algorithm,
    SuiteConfig suite,
    IReadOnlyDictionary<string, BlockedAlgorithmState> blocked,
    out string reason)
{
    reason = "";

    if (!blocked.TryGetValue(algorithm, out var state))
        return false;

    if (state.ShouldSkip(suite))
    {
        reason = $"Algorithm skipped after timeout at suite '{state.TimeoutSuiteName}' ({state.TimeoutWidth}x{state.TimeoutHeight}, depth={state.TimeoutDepth}).";
        return true;
    }

    return false;
}

static SingleRunMeasurement RunSingle(
    Func<ISolver> solverFactory,
    PuzzleCase caseItem,
    SuiteConfig suite,
    int timeoutMs,
    bool measureMemory,
    bool forceFullGcBeforeRun)
{
    var board = new PuzzleBoard(caseItem.Tiles, (byte)suite.Height, (byte)suite.Width);
    var solver = solverFactory();

    long before = 0;
    if (measureMemory && forceFullGcBeforeRun)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        before = GC.GetTotalMemory(true);
    }

    var sw = Stopwatch.StartNew();

    try
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        var solveResult = solver.Solve(board, cts.Token);
        sw.Stop();

        var memoryDelta = 0L;
        if (measureMemory)
        {
            var after = GC.GetTotalMemory(true);
            memoryDelta = Math.Max(0, after - before);
        }

        return new SingleRunMeasurement(RunStatus.Ok, solveResult.IsSolved, solveResult.MoveCount, sw.Elapsed.TotalMilliseconds, memoryDelta, null);
    }
    catch (OperationCanceledException)
    {
        sw.Stop();
        return new SingleRunMeasurement(RunStatus.Timeout, false, null, sw.Elapsed.TotalMilliseconds, 0, null);
    }
    catch (Exception ex)
    {
        sw.Stop();
        return new SingleRunMeasurement(RunStatus.Error, false, null, sw.Elapsed.TotalMilliseconds, 0, ex.GetType().Name + ": " + ex.Message);
    }
}

static BenchmarkResultRow CreateResultRow(
    DateTime runStartedUtc,
    SuiteConfig suite,
    PuzzleCase caseItem,
    string algorithmName,
    int runIndex,
    SingleRunMeasurement result)
{
    return new BenchmarkResultRow(
        runStartedUtc,
        DateTime.UtcNow,
        suite.Name,
        suite.Width,
        suite.Height,
        suite.Depth,
        caseItem.CaseId,
        caseItem.Tiles,
        algorithmName,
        runIndex,
        result.Status,
        result.Solved,
        result.MoveCount,
        result.ElapsedMs,
        result.MemoryDeltaBytes,
        result.Error);
}

static void PrintRunResult(
    SuiteConfig suite,
    PuzzleCase caseItem,
    string algorithm,
    int runIndex,
    int totalRuns,
    SingleRunMeasurement result)
{
    var prefix = $"      [{suite.Width}x{suite.Height} depth={suite.Depth}] case={caseItem.CaseId} alg={algorithm} run={runIndex}/{totalRuns}";
    var details = $"time={result.ElapsedMs:F3} ms";
    var moves = result.MoveCount.HasValue ? $", moves={result.MoveCount.Value}" : "";
    var mem = $", memDelta={result.MemoryDeltaBytes} B";

    switch (result.Status)
    {
        case RunStatus.Ok:
            ConsoleUi.WriteLine($"{prefix} -> OK ({(result.Solved ? "solved" : "not solved")}, {details}{moves}{mem})", ConsoleColor.Green);
            break;

        case RunStatus.Timeout:
            ConsoleUi.WriteLine($"{prefix} -> TIMEOUT ({details})", ConsoleColor.Magenta);
            break;

        case RunStatus.Error:
            ConsoleUi.WriteLine($"{prefix} -> ERROR ({result.Error ?? "unknown error"})", ConsoleColor.Red);
            break;

        case RunStatus.Skipped:
            ConsoleUi.WriteLine($"{prefix} -> SKIPPED ({result.Error ?? "policy"})", ConsoleColor.DarkYellow);
            break;
    }
}

static string FormatBoard(byte[] tiles, int width)
{
    var rows = new List<string>();
    for (var y = 0; y < tiles.Length / width; y++)
    {
        var start = y * width;
        rows.Add(string.Join(' ', tiles.Skip(start).Take(width)));
    }

    return string.Join(" | ", rows);
}

internal static class ConsoleUi
{
    public static void WriteLine(string message, ConsoleColor color)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = previousColor;
    }
}

internal enum RunStatus
{
    Ok,
    Timeout,
    Error,
    Skipped
}

internal sealed record SingleRunMeasurement(
    RunStatus Status,
    bool Solved,
    int? MoveCount,
    double ElapsedMs,
    long MemoryDeltaBytes,
    string? Error)
{
    public static SingleRunMeasurement Skipped(string reason) => new(RunStatus.Skipped, false, null, 0, 0, reason);
}

internal sealed record BenchmarkResultRow(
    DateTime RunStartedUtc,
    DateTime MeasuredAtUtc,
    string Suite,
    int Width,
    int Height,
    int Depth,
    int CaseId,
    IReadOnlyList<byte> Tiles,
    string Algorithm,
    int RunIndex,
    RunStatus Status,
    bool Solved,
    int? MoveCount,
    double ElapsedMs,
    long MemoryDeltaBytes,
    string? Error);

internal sealed record PuzzleCase(int CaseId, byte[] Tiles);

internal sealed record SuiteInput(SuiteConfig Definition, List<PuzzleCase> Cases);

internal sealed record BlockedAlgorithmState(
    int TimeoutWidth,
    int TimeoutHeight,
    int TimeoutDepth,
    string TimeoutSuiteName,
    bool SkipLargerSize,
    bool SkipSameSizeGreaterDepth)
{
    public bool ShouldSkip(SuiteConfig suite)
    {
        if (suite.Width == TimeoutWidth && suite.Height == TimeoutHeight && suite.Depth == TimeoutDepth)
            return true;

        var timeoutArea = TimeoutWidth * TimeoutHeight;
        var suiteArea = suite.Width * suite.Height;

        if (SkipLargerSize && suiteArea > timeoutArea)
            return true;

        if (SkipSameSizeGreaterDepth && suiteArea == timeoutArea && suite.Depth > TimeoutDepth)
            return true;

        return false;
    }
}

internal sealed class CaseFile
{
    public int Depth { get; init; }
    public List<List<int>> Boards { get; init; } = [];
}

internal sealed class LoadTestConfig
{
    public int Seed { get; init; } = 42;
    public List<string> Algorithms { get; init; } = [];
    public List<SuiteConfig> Suites { get; init; } = [];
    public ExecutionConfig Execution { get; init; } = new();
    public MeasurementConfig Measurements { get; init; } = new();
    public OutputConfig Output { get; init; } = new();

    public void Validate()
    {
        if (Algorithms.Count == 0)
            throw new InvalidOperationException("Config must contain at least one algorithm.");

        if (Suites.Count == 0)
            throw new InvalidOperationException("Config must contain at least one suite.");

        foreach (var suite in Suites)
            suite.Validate();

        Execution.Validate();
        Output.Validate();
    }
}

internal sealed class SuiteConfig
{
    public string Name { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public int Depth { get; init; }
    public string CasesFile { get; init; } = string.Empty;
    public int Runs { get; init; } = 1;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("suite.name is required.");

        if (Width <= 0 || Height <= 0)
            throw new InvalidOperationException("suite.width and suite.height must be > 0.");

        if (Depth < 0)
            throw new InvalidOperationException("suite.depth must be >= 0.");

        if (string.IsNullOrWhiteSpace(CasesFile))
            throw new InvalidOperationException("suite.casesFile is required.");

        if (Runs <= 0)
            throw new InvalidOperationException("suite.runs must be > 0.");
    }
}

internal sealed class ExecutionConfig
{
    public int WarmupRuns { get; init; } = 1;
    public int TimeoutMs { get; init; } = 5000;
    public StopOnTimeoutConfig StopOnTimeout { get; init; } = new();

    public void Validate()
    {
        if (WarmupRuns < 0)
            throw new InvalidOperationException("execution.warmupRuns must be >= 0.");

        if (TimeoutMs <= 0)
            throw new InvalidOperationException("execution.timeoutMs must be > 0.");

        StopOnTimeout.Validate();
    }
}

internal sealed class StopOnTimeoutConfig
{
    public bool Enabled { get; init; }
    public string Scope { get; init; } = "algorithm";
    public bool SkipCurrentSuiteRemainingCases { get; init; } = true;
    public bool SkipAllNextSuitesWithLargerSize { get; init; } = true;
    public bool SkipAllNextSuitesWithSameSizeAndGreaterDepth { get; init; } = true;

    public void Validate()
    {
        if (!string.Equals(Scope, "algorithm", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("execution.stopOnTimeout.scope currently supports only 'algorithm'.");
    }
}

internal sealed class MeasurementConfig
{
    public bool MeasureMemory { get; init; } = true;
    public bool ForceFullGcBeforeRun { get; init; } = true;
}

internal sealed class OutputConfig
{
    public string RawResultsFile { get; init; } = "artifacts/slidingpuzzle-loadtest-results.jsonl";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RawResultsFile))
            throw new InvalidOperationException("output.rawResultsFile is required.");
    }
}
