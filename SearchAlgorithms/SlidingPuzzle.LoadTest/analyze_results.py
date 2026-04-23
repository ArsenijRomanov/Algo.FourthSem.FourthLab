#!/usr/bin/env python3
import argparse
import json
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Analyze SlidingPuzzle.LoadTest JSONL results.")
    parser.add_argument("results_path", type=Path, help="Path to JSONL file produced by SlidingPuzzle.LoadTest")
    parser.add_argument("--out-dir", type=Path, default=Path("artifacts/analysis"), help="Output directory")
    return parser.parse_args()


def load_jsonl(path: Path):
    rows = []
    with path.open("r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            rows.append(json.loads(line))
    if not rows:
        raise ValueError(f"No records in file: {path}")
    return pd.DataFrame(rows)


def add_derived_columns(df):
    df = df.copy()
    df["size_label"] = df["Width"].astype(str) + "x" + df["Height"].astype(str)
    df["size_area"] = df["Width"] * df["Height"]
    return df


def build_summary(df):
    grouped = df.groupby(["Algorithm", "Suite", "size_label", "Depth"], as_index=False)

    summary = grouped.agg(
        size_area=("size_area", "first"),
        runs=("RunIndex", "count"),
        ok_runs=("Status", lambda s: (s == "Ok").sum()),
        timeout_runs=("Status", lambda s: (s == "Timeout").sum()),
        error_runs=("Status", lambda s: (s == "Error").sum()),
        skipped_runs=("Status", lambda s: (s == "Skipped").sum()),
        solved_rate=("Solved", "mean"),
        mean_time_ms=("ElapsedMs", "mean"),
        median_time_ms=("ElapsedMs", "median"),
        p95_time_ms=("ElapsedMs", lambda s: s.quantile(0.95)),
        mean_moves=("MoveCount", "mean"),
        median_moves=("MoveCount", "median"),
        mean_mem_bytes=("MemoryDeltaBytes", "mean"),
        median_mem_bytes=("MemoryDeltaBytes", "median"),
    )

    summary["ok_rate"] = summary["ok_runs"] / summary["runs"]
    summary["timeout_rate"] = summary["timeout_runs"] / summary["runs"]
    summary["error_rate"] = summary["error_runs"] / summary["runs"]
    summary["skipped_rate"] = summary["skipped_runs"] / summary["runs"]

    return summary.sort_values(["size_area", "Depth", "Algorithm", "Suite"]).reset_index(drop=True)


def save_table(df, out_dir: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    csv_path = out_dir / "summary.csv"
    md_path = out_dir / "summary.md"

    df.to_csv(csv_path, index=False)
    md_path.write_text(df.to_markdown(index=False), encoding="utf-8")

    print(f"Saved summary CSV: {csv_path}")
    print(f"Saved summary Markdown: {md_path}")


def plot_metrics(summary, out_dir: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)

    summary = summary.copy()
    summary["suite_depth_label"] = summary["Suite"] + " (d=" + summary["Depth"].astype(str) + ")"

    size_order = (
        summary[["size_area", "Depth", "suite_depth_label"]]
        .drop_duplicates()
        .sort_values(["size_area", "Depth"])
    )
    x_labels = size_order["suite_depth_label"].tolist()

    def reindex_for(part):
        indexed = part.set_index("suite_depth_label")
        return indexed.reindex(x_labels)

    plt.figure(figsize=(12, 6))
    for algorithm, part in summary.groupby("Algorithm"):
        series = reindex_for(part)
        plt.plot(x_labels, series["median_time_ms"], marker="o", label=algorithm)
    plt.title("Median runtime by suite")
    plt.xlabel("Suite")
    plt.ylabel("Median runtime (ms)")
    plt.xticks(rotation=35, ha="right")
    plt.grid(True, alpha=0.3)
    plt.legend()
    time_path = out_dir / "median_time_ms.png"
    plt.tight_layout()
    plt.savefig(time_path, dpi=150)
    plt.close()

    plt.figure(figsize=(12, 6))
    for algorithm, part in summary.groupby("Algorithm"):
        series = reindex_for(part)
        plt.plot(x_labels, series["timeout_rate"], marker="o", label=algorithm)
    plt.title("Timeout rate by suite")
    plt.xlabel("Suite")
    plt.ylabel("Timeout rate")
    plt.ylim(0, 1)
    plt.xticks(rotation=35, ha="right")
    plt.grid(True, alpha=0.3)
    plt.legend()
    timeout_path = out_dir / "timeout_rate.png"
    plt.tight_layout()
    plt.savefig(timeout_path, dpi=150)
    plt.close()

    plt.figure(figsize=(12, 6))
    for algorithm, part in summary.groupby("Algorithm"):
        series = reindex_for(part)
        plt.plot(x_labels, series["median_mem_bytes"], marker="o", label=algorithm)
    plt.title("Median memory delta by suite")
    plt.xlabel("Suite")
    plt.ylabel("Median memory delta (bytes)")
    plt.xticks(rotation=35, ha="right")
    plt.grid(True, alpha=0.3)
    plt.legend()
    mem_path = out_dir / "median_memory_bytes.png"
    plt.tight_layout()
    plt.savefig(mem_path, dpi=150)
    plt.close()

    print(f"Saved graph: {time_path}")
    print(f"Saved graph: {timeout_path}")
    print(f"Saved graph: {mem_path}")


def main() -> None:
    args = parse_args()

    try:
        import matplotlib.pyplot as plt  # type: ignore
        import pandas as pd  # type: ignore
    except ModuleNotFoundError as exc:
        raise SystemExit("Missing Python dependency. Install with: pip install pandas matplotlib") from exc

    globals()["plt"] = plt
    globals()["pd"] = pd

    df = load_jsonl(args.results_path)
    df = add_derived_columns(df)
    summary = build_summary(df)
    save_table(summary, args.out_dir)
    plot_metrics(summary, args.out_dir)


if __name__ == "__main__":
    main()
