# HamiltonianPath.LoadTest

## Run benchmark

```bash
dotnet run --project HamiltonianPath.LoadTest/HamiltonianPath.LoadTest.csproj -- HamiltonianPath.LoadTest/loadtest.config.json
```

## Analyze results

```bash
python3 HamiltonianPath.LoadTest/analyze_results.py artifacts/hamiltonian-loadtest-results.jsonl --out-dir artifacts/analysis
```

## Result format

The runner writes JSONL rows with fields:
- `RunStartedUtc`, `MeasuredAtUtc`
- `Width`, `Height`, `CaseId`, `StartX`, `StartY`, `FinishX`, `FinishY`
- `Algorithm`, `RunIndex`
- `Status` (`Ok`, `Timeout`, `Error`)
- `Solved`, `ElapsedMs`, `MemoryDeltaBytes`, `SolutionCount`, `Error`

If `stopOnTimeout=true`, the runner disables only the timed-out algorithm for all remaining cases/sizes and continues benchmarking other algorithms.
