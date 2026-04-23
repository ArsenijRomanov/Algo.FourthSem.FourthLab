using System.Diagnostics;
using System.Text.Json;
using HamiltonianPath.Core;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Strategies;

const string defaultConfigPath = "loadtest.config.json";

var configPath = args.Length > 0 ? args[0] : defaultConfigPath;
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
var disabledAlgorithms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

ConsoleUi.WriteLine("=== HamiltonianPath.LoadTest started ===", ConsoleColor.Cyan);
ConsoleUi.WriteLine($"Config: {Path.GetFullPath(configPath)}", ConsoleColor.DarkCyan);
ConsoleUi.WriteLine($"Output: {outputPath}", ConsoleColor.DarkCyan);
ConsoleUi.WriteLine($"Algorithms: {string.Join(", ", config.Algorithms)}", ConsoleColor.DarkCyan);

foreach (var size in config.Sizes)
{
    ConsoleUi.WriteLine($"[SIZE] {size.Width}x{size.Height} | cases={size.Cases}, runs={size.Runs}", ConsoleColor.Cyan);
    var cases = GenerateFeasibleCases(size, config);
    ConsoleUi.WriteLine($"[SIZE] {size.Width}x{size.Height} -> feasible cases generated: {cases.Count}", ConsoleColor.Green);

    foreach (var generatedCase in cases)
    {
        ConsoleUi.WriteLine(
            $"  [CASE {generatedCase.CaseId}] start=({generatedCase.Start.X},{generatedCase.Start.Y}) finish=({generatedCase.Finish.X},{generatedCase.Finish.Y})",
            ConsoleColor.Gray);

        foreach (var algorithmName in config.Algorithms)
        {
            if (disabledAlgorithms.Contains(algorithmName))
            {
                ConsoleUi.WriteLine($"    [ALG] {algorithmName} -> skipped (disabled after timeout)", ConsoleColor.DarkYellow);
                continue;
            }

            var factory = algorithmFactories[algorithmName];
            ConsoleUi.WriteLine($"    [ALG] {algorithmName}", ConsoleColor.Yellow);

            for (var runIndex = 1; runIndex <= size.Runs; runIndex++)
            {
                for (var warmup = 0; warmup < config.WarmupRuns; warmup++)
                    _ = RunSingle(factory, generatedCase, config.TimeoutMs, measureMemory: false);

                var result = RunSingle(factory, generatedCase, config.TimeoutMs, config.MeasureMemory);

                var row = new BenchmarkResultRow(
                    runStartedUtc,
                    DateTime.UtcNow,
                    generatedCase.Width,
                    generatedCase.Height,
                    generatedCase.CaseId,
                    generatedCase.Start.X,
                    generatedCase.Start.Y,
                    generatedCase.Finish.X,
                    generatedCase.Finish.Y,
                    algorithmName,
                    runIndex,
                    result.Status,
                    result.Solved,
                    result.ElapsedMs,
                    result.MemoryDeltaBytes,
                    result.Error
                );

                await writer.WriteLineAsync(JsonSerializer.Serialize(row));
                rowCount++;
                switch (result.Status)
                {
                    case RunStatus.Ok:
                        okRows++;
                        break;
                    case RunStatus.Timeout:
                        timeoutRows++;
                        break;
                    case RunStatus.Error:
                        errorRows++;
                        break;
                }

                PrintRunResult(size, generatedCase, algorithmName, runIndex, size.Runs, result);

                if (result.Status == RunStatus.Timeout && config.StopOnTimeout)
                {
                    disabledAlgorithms.Add(algorithmName);
                    ConsoleUi.WriteLine(
                        $"    [ALG DISABLED] {algorithmName} exceeded timeout and will be skipped in all next cases/sizes.",
                        ConsoleColor.Magenta);
                    break;
                }
            }
        }
    }
}

await writer.FlushAsync();
ConsoleUi.WriteLine("=== HamiltonianPath.LoadTest finished ===", ConsoleColor.Cyan);
ConsoleUi.WriteLine($"[TOTAL] rows={rowCount}, ok={okRows}, timeout={timeoutRows}, error={errorRows}", ConsoleColor.Cyan);
if (disabledAlgorithms.Count > 0)
{
    ConsoleUi.WriteLine(
        $"Disabled algorithms due to timeout: {string.Join(", ", disabledAlgorithms)}",
        ConsoleColor.Magenta);
}
ConsoleUi.WriteLine($"Results file: {outputPath}", ConsoleColor.Cyan);

static Dictionary<string, Func<HamiltonianPathSolver>> BuildAlgorithmFactories() =>
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["baseline"] = () => new HamiltonianPathSolver(new BaseChooseDirection(), new BaseCommitValidator(), false),
        ["warnsdorff_heuristic"] = () => new HamiltonianPathSolver(new WarnsdorffChooseDirection(), new BaseCommitValidator(), false),
        ["connectivity_pruning"] = () => new HamiltonianPathSolver(new BaseChooseDirection(), new ConnectivityCommitValidator(), false),
        ["backjump_backtracking"] = () => new HamiltonianPathSolver(new BaseChooseDirection(), new BaseCommitValidator(), true)
    };

static List<GeneratedCase> GenerateFeasibleCases(SizeConfig size, LoadTestConfig config)
{
    var random = new Random(unchecked(config.Seed + size.Width * 73856093 ^ size.Height * 19349663));
    var results = new List<GeneratedCase>(size.Cases);

    var attempts = 0;
    var maxAttempts = size.Cases * config.Precheck.MaxAttemptsPerCase;

    while (results.Count < size.Cases && attempts < maxAttempts)
    {
        attempts++;

        var start = new Point(random.Next(size.Width), random.Next(size.Height));
        var finish = new Point(random.Next(size.Width), random.Next(size.Height));
        if (start == finish)
            continue;

        if (!config.Precheck.Enabled)
        {
            results.Add(new GeneratedCase(results.Count + 1, size.Width, size.Height, start, finish));
            continue;
        }

        var board = new Board(size.Height, size.Width, start, finish);

        if (!HamiltonianPathSolver.CanHaveHamiltonianPath(board))
            continue;

        if (config.Precheck.VerifyWithFastSolver)
        {
            var solver = new HamiltonianPathSolver(new WarnsdorffChooseDirection(), new ConnectivityCommitValidator(), true);
            using var cts = new CancellationTokenSource(config.Precheck.SolverTimeoutMs);

            var feasible = false;
            try
            {
                feasible = solver.Solve(new Board(size.Height, size.Width, start, finish), cts.Token);
            }
            catch (OperationCanceledException)
            {
                feasible = false;
            }

            if (!feasible)
                continue;
        }

        results.Add(new GeneratedCase(results.Count + 1, size.Width, size.Height, start, finish));
    }

    if (results.Count < size.Cases)
        throw new InvalidOperationException(
            $"Cannot generate enough feasible cases for {size.Width}x{size.Height}: {results.Count}/{size.Cases} after {attempts} attempts.");

    return results;
}

static SingleRunMeasurement RunSingle(
    Func<HamiltonianPathSolver> solverFactory,
    GeneratedCase generatedCase,
    int timeoutMs,
    bool measureMemory)
{
    var board = new Board(generatedCase.Height, generatedCase.Width, generatedCase.Start, generatedCase.Finish);
    var solver = solverFactory();

    long before = 0;
    if (measureMemory)
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
        var solved = solver.Solve(board, cts.Token);
        sw.Stop();

        var memoryDelta = 0L;
        if (measureMemory)
        {
            var after = GC.GetTotalMemory(true);
            memoryDelta = Math.Max(0, after - before);
        }

        return new SingleRunMeasurement(RunStatus.Ok, solved, sw.Elapsed.TotalMilliseconds, memoryDelta, null);
    }
    catch (OperationCanceledException)
    {
        sw.Stop();
        return new SingleRunMeasurement(RunStatus.Timeout, false, sw.Elapsed.TotalMilliseconds, 0, null);
    }
    catch (Exception ex)
    {
        sw.Stop();
        return new SingleRunMeasurement(RunStatus.Error, false, sw.Elapsed.TotalMilliseconds, 0, ex.GetType().Name + ": " + ex.Message);
    }
}

static void PrintRunResult(
    SizeConfig size,
    GeneratedCase generatedCase,
    string algorithm,
    int runIndex,
    int totalRuns,
    SingleRunMeasurement result)
{
    var prefix = $"      [{size.Width}x{size.Height}] case={generatedCase.CaseId} alg={algorithm} run={runIndex}/{totalRuns}";
    var details = $"time={result.ElapsedMs:F3} ms";
    var mem = $", memDelta={result.MemoryDeltaBytes} B";

    switch (result.Status)
    {
        case RunStatus.Ok:
            ConsoleUi.WriteLine(
                $"{prefix} -> OK ({(result.Solved ? "solved" : "not solved")}, {details}{mem})",
                ConsoleColor.Green);
            break;

        case RunStatus.Timeout:
            ConsoleUi.WriteLine($"{prefix} -> TIMEOUT ({details})", ConsoleColor.Magenta);
            break;

        case RunStatus.Error:
            ConsoleUi.WriteLine($"{prefix} -> ERROR ({result.Error ?? "unknown error"})", ConsoleColor.Red);
            break;
    }
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

internal sealed record GeneratedCase(int CaseId, int Width, int Height, Point Start, Point Finish);

internal enum RunStatus
{
    Ok,
    Timeout,
    Error
}

internal sealed record SingleRunMeasurement(
    RunStatus Status,
    bool Solved,
    double ElapsedMs,
    long MemoryDeltaBytes,
    string? Error);

internal sealed record BenchmarkResultRow(
    DateTime RunStartedUtc,
    DateTime MeasuredAtUtc,
    int Width,
    int Height,
    int CaseId,
    int StartX,
    int StartY,
    int FinishX,
    int FinishY,
    string Algorithm,
    int RunIndex,
    RunStatus Status,
    bool Solved,
    double ElapsedMs,
    long MemoryDeltaBytes,
    string? Error);

internal sealed class LoadTestConfig
{
    public int Seed { get; init; } = 42;
    public List<SizeConfig> Sizes { get; init; } = [];
    public List<string> Algorithms { get; init; } = [];
    public PrecheckConfig Precheck { get; init; } = new();
    public int TimeoutMs { get; init; } = 5000;
    public bool StopOnTimeout { get; init; }
    public bool MeasureMemory { get; init; } = true;
    public int WarmupRuns { get; init; } = 1;
    public OutputConfig Output { get; init; } = new();

    public void Validate()
    {
        if (Sizes.Count == 0)
            throw new InvalidOperationException("Config must contain at least one size entry.");

        if (Algorithms.Count == 0)
            throw new InvalidOperationException("Config must contain at least one algorithm.");

        if (TimeoutMs <= 0)
            throw new InvalidOperationException("timeoutMs must be > 0.");

        if (WarmupRuns < 0)
            throw new InvalidOperationException("warmupRuns must be >= 0.");

        foreach (var size in Sizes)
            size.Validate();

        Precheck.Validate();
        Output.Validate();
    }
}

internal sealed class SizeConfig
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Cases { get; init; }
    public int Runs { get; init; }

    public void Validate()
    {
        if (Width <= 0 || Height <= 0)
            throw new InvalidOperationException("size.width and size.height must be > 0.");

        if (Cases <= 0)
            throw new InvalidOperationException("size.cases must be > 0.");

        if (Runs <= 0)
            throw new InvalidOperationException("size.runs must be > 0.");
    }
}

internal sealed class PrecheckConfig
{
    public bool Enabled { get; init; } = true;
    public int MaxAttemptsPerCase { get; init; } = 200;
    public bool VerifyWithFastSolver { get; init; } = true;
    public int SolverTimeoutMs { get; init; } = 250;

    public void Validate()
    {
        if (MaxAttemptsPerCase <= 0)
            throw new InvalidOperationException("precheck.maxAttemptsPerCase must be > 0.");

        if (SolverTimeoutMs <= 0)
            throw new InvalidOperationException("precheck.solverTimeoutMs must be > 0.");
    }
}

internal sealed class OutputConfig
{
    public string RawResultsFile { get; init; } = "artifacts/hamiltonian-loadtest-results.jsonl";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RawResultsFile))
            throw new InvalidOperationException("output.rawResultsFile is required.");
    }
}
