#!/usr/bin/env python3
import argparse
import json
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Analyze HamiltonianPath.LoadTest JSONL results.")
    parser.add_argument("results_path", type=Path, help="Path to JSONL file produced by HamiltonianPath.LoadTest")
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


def add_size_columns(df):
    df = df.copy()
    df["size_label"] = df["Width"].astype(str) + "x" + df["Height"].astype(str)
    df["size_area"] = df["Width"] * df["Height"]
    return df


def build_summary(df):
    grouped = df.groupby(["Algorithm", "size_label"], as_index=False)

    summary = grouped.agg(
        runs=("RunIndex", "count"),
        ok_runs=("Status", lambda s: (s == "Ok").sum()),
        timeout_runs=("Status", lambda s: (s == "Timeout").sum()),
        error_runs=("Status", lambda s: (s == "Error").sum()),
        solved_rate=("Solved", "mean"),
        mean_time_ms=("ElapsedMs", "mean"),
        median_time_ms=("ElapsedMs", "median"),
        p95_time_ms=("ElapsedMs", lambda s: s.quantile(0.95)),
        mean_mem_bytes=("MemoryDeltaBytes", "mean"),
        median_mem_bytes=("MemoryDeltaBytes", "median"),
    )

    summary["ok_rate"] = summary["ok_runs"] / summary["runs"]
    summary["timeout_rate"] = summary["timeout_runs"] / summary["runs"]
    summary["error_rate"] = summary["error_runs"] / summary["runs"]
    return summary.sort_values(["size_label", "Algorithm"]).reset_index(drop=True)


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

    size_order = sorted(summary["size_label"].unique(), key=lambda s: int(s.split("x")[0]) * int(s.split("x")[1]))

    # Median time plot
    plt.figure(figsize=(10, 6))
    for algorithm, part in summary.groupby("Algorithm"):
        series = part.set_index("size_label").reindex(size_order)
        plt.plot(size_order, series["median_time_ms"], marker="o", label=algorithm)
    plt.title("Median runtime by size")
    plt.xlabel("Board size")
    plt.ylabel("Median runtime (ms)")
    plt.grid(True, alpha=0.3)
    plt.legend()
    time_path = out_dir / "median_time_ms.png"
    plt.tight_layout()
    plt.savefig(time_path, dpi=150)
    plt.close()

    # Timeout rate plot
    plt.figure(figsize=(10, 6))
    for algorithm, part in summary.groupby("Algorithm"):
        series = part.set_index("size_label").reindex(size_order)
        plt.plot(size_order, series["timeout_rate"], marker="o", label=algorithm)
    plt.title("Timeout rate by size")
    plt.xlabel("Board size")
    plt.ylabel("Timeout rate")
    plt.ylim(0, 1)
    plt.grid(True, alpha=0.3)
    plt.legend()
    timeout_path = out_dir / "timeout_rate.png"
    plt.tight_layout()
    plt.savefig(timeout_path, dpi=150)
    plt.close()

    # Memory plot
    plt.figure(figsize=(10, 6))
    for algorithm, part in summary.groupby("Algorithm"):
        series = part.set_index("size_label").reindex(size_order)
        plt.plot(size_order, series["median_mem_bytes"], marker="o", label=algorithm)
    plt.title("Median memory delta by size")
    plt.xlabel("Board size")
    plt.ylabel("Median memory delta (bytes)")
    plt.grid(True, alpha=0.3)
    plt.legend()
    mem_path = out_dir / "median_memory_bytes.png"
    plt.tight_layout()
    plt.savefig(mem_path, dpi=150)
    plt.close()

    print(f"Saved graph: {time_path}")
    print(f"Saved graph: {timeout_path}")
    print(f"Saved graph: {mem_path}")


def plot_distribution_charts(df, out_dir: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)

    size_order = sorted(
        df["size_label"].unique(),
        key=lambda s: int(s.split("x")[0]) * int(s.split("x")[1]),
    )
    algorithms = sorted(df["Algorithm"].unique())
    metrics = [
        ("ElapsedMs", "Runtime distribution by size", "Runtime (ms)", "time"),
        ("MemoryDeltaBytes", "Memory distribution by size", "Memory delta (bytes)", "memory"),
    ]

    for metric, title, ylabel, filename_prefix in metrics:
        fig, axes = plt.subplots(
            len(algorithms),
            1,
            figsize=(max(10, len(size_order) * 1.3), 4 * len(algorithms)),
            sharex=True,
        )
        if len(algorithms) == 1:
            axes = [axes]

        for ax, algorithm in zip(axes, algorithms):
            part = df[df["Algorithm"] == algorithm]
            values_by_size = []

            for size in size_order:
                values = part.loc[part["size_label"] == size, metric].dropna().tolist()
                values_by_size.append(values)

            ax.boxplot(values_by_size, labels=size_order, showfliers=False)
            ax.set_title(f"{algorithm}")
            ax.set_ylabel(ylabel)
            ax.grid(True, alpha=0.25, axis="y")

        axes[-1].set_xlabel("Board size")
        fig.suptitle(title)
        fig.tight_layout(rect=[0, 0.02, 1, 0.96])
        boxplot_path = out_dir / f"{filename_prefix}_boxplot_by_size.png"
        fig.savefig(boxplot_path, dpi=150)
        plt.close(fig)

        fig, axes = plt.subplots(
            len(algorithms),
            1,
            figsize=(max(10, len(size_order) * 1.3), 4 * len(algorithms)),
            sharex=True,
        )
        if len(algorithms) == 1:
            axes = [axes]

        for ax, algorithm in zip(axes, algorithms):
            part = df[df["Algorithm"] == algorithm]
            for x_pos, size in enumerate(size_order, start=1):
                y_values = part.loc[part["size_label"] == size, metric].dropna()
                x_values = [x_pos] * len(y_values)
                ax.scatter(x_values, y_values, alpha=0.6, s=20)

            ax.set_title(f"{algorithm}")
            ax.set_ylabel(ylabel)
            ax.grid(True, alpha=0.25, axis="y")
            ax.set_xlim(0.5, len(size_order) + 0.5)
            ax.set_xticks(range(1, len(size_order) + 1), size_order)

        axes[-1].set_xlabel("Board size")
        fig.suptitle(f"{title} (scatter columns)")
        fig.tight_layout(rect=[0, 0.02, 1, 0.96])
        scatter_path = out_dir / f"{filename_prefix}_scatter_by_size.png"
        fig.savefig(scatter_path, dpi=150)
        plt.close(fig)

        print(f"Saved graph: {boxplot_path}")
        print(f"Saved graph: {scatter_path}")


def main() -> None:
    args = parse_args()

    try:
        import matplotlib.pyplot as plt  # type: ignore
        import pandas as pd  # type: ignore
    except ModuleNotFoundError as exc:
        raise SystemExit(
            "Missing Python dependency. Install with: pip install pandas matplotlib"
        ) from exc

    globals()["plt"] = plt
    globals()["pd"] = pd

    df = load_jsonl(args.results_path)
    df = add_size_columns(df)
    summary = build_summary(df)
    save_table(summary, args.out_dir)
    plot_metrics(summary, args.out_dir)
    plot_distribution_charts(df, args.out_dir)


if __name__ == "__main__":
    main()
