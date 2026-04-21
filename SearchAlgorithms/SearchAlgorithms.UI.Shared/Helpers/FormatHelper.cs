using SearchAlgorithms.UI.Shared.Models;

namespace SearchAlgorithms.UI.Shared.Helpers;

public static class FormatHelper
{
    public static string FormatBytes(long bytes)
    {
        var sign = bytes < 0 ? "-" : string.Empty;
        var value = Math.Abs((double)bytes);
        var suffixes = new[] { "B", "KB", "MB", "GB" };
        var suffixIndex = 0;

        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return $"{sign}{value:0.##} {suffixes[suffixIndex]}";
    }

    public static string PlaybackStatusText(PlaybackStatus status) => status switch
    {
        PlaybackStatus.FollowingSolution => "Following solution",
        PlaybackStatus.Diverged => "Diverged",
        PlaybackStatus.ManualEdit => "Manual edit",
        _ => "No solution loaded"
    };
}
