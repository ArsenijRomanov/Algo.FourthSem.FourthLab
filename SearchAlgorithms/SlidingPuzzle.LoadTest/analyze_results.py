#!/usr/bin/env python3
import argparse
import json
import re
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
    df["suite_depth_label"] = df["Suite"] + " (d=" + df["Depth"].astype(str) + ")"
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


def _sanitize_filename(value: str) -> str:
    return re.sub(r"[^a-zA-Z0-9_.-]+", "_", value).strip("_")


def _is_ok_status(series):
    return (series == "Ok") | (series == 0)


def plot_time_boxplots_multiscale(df, out_dir: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)

    order_df = (
        df[["size_area", "Depth", "suite_depth_label"]]
        .drop_duplicates()
        .sort_values(["size_area", "Depth"])
    )
    suite_order = order_df["suite_depth_label"].tolist()
    algorithms = sorted(df["Algorithm"].unique())

    scales = [
        ("linear", "linear"),
        ("log", "log"),
        ("symlog", "symlog"),
    ]

    for suite_label in suite_order:
        part = df[df["suite_depth_label"] == suite_label]

        values_by_algorithm = [
            part.loc[
                (part["Algorithm"] == algorithm)
                & _is_ok_status(part["Status"])
                & (part["ElapsedMs"] > 0),
                "ElapsedMs",
            ].dropna().tolist()
            for algorithm in algorithms
        ]

        if all(len(values) == 0 for values in values_by_algorithm):
            continue

        fig, axes = plt.subplots(1, len(scales), figsize=(6 * len(scales), 6), sharex=True)
        if len(scales) == 1:
            axes = [axes]

        for ax, (scale_name, y_scale) in zip(axes, scales):
            ax.boxplot(values_by_algorithm, labels=algorithms, showfliers=False)
            ax.set_title(f"{suite_label} [{scale_name}]")
            ax.set_xlabel("Algorithm")
            ax.set_ylabel("Runtime (ms)")
            ax.set_yscale(y_scale)
            ax.grid(True, alpha=0.25, axis="y")
            plt.setp(ax.get_xticklabels(), rotation=20, ha="right")

        fig.suptitle("Runtime distribution by algorithm", fontsize=14)
        fig.tight_layout(rect=[0, 0, 1, 0.95])

        boxplot_path = out_dir / f"time_boxplot_multiscale_{_sanitize_filename(suite_label)}.png"
        fig.savefig(boxplot_path, dpi=150)
        plt.close(fig)

        print(f"Saved graph: {boxplot_path}")


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
    plot_time_boxplots_multiscale(df, args.out_dir)


if __name__ == "__main__":
    main()
