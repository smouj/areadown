namespace AreaDown.Core;

/// <summary>
/// Timestamp range for downloading a segment of a video.
/// </summary>
/// <param name="Start">Start time of the clip.</param>
/// <param name="End">Optional end time. Null means download from start to the end of the video.</param>
public record TimestampClip(TimeSpan Start, TimeSpan? End)
{
    /// <summary>
    /// Formats the clip for yt-dlp --download-sections argument.
    /// Example: "*00:01:23-00:04:56"
    /// </summary>
    public string ToDownloadSectionsArg()
    {
        var startFmt = FormatTs(Start);
        var endFmt = End.HasValue ? FormatTs(End.Value) : "";
        return $"*{startFmt}-{endFmt}";
    }

    /// <summary>
    /// Formats the clip for ffmpeg -ss and -to arguments.
    /// </summary>
    public string FfmpegStartArg() => Start.ToString(@"hh\:mm\:ss\.fff");

    public string? FfmpegEndArg() => End?.ToString(@"hh\:mm\:ss\.fff");

    /// <summary>
    /// Human-readable representation.
    /// </summary>
    public override string ToString()
    {
        var s = FormatTs(Start);
        return End.HasValue ? $"{s} → {FormatTs(End.Value)}" : $"{s} → end";
    }

    private static string FormatTs(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return ts.ToString(@"h\:mm\:ss");
        return ts.ToString(@"m\:ss");
    }
}