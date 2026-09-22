using System.Globalization;
using System.Text.RegularExpressions;

namespace AreaDown.Downloader;

/// <summary>
/// Parses yt-dlp stdout lines into structured progress events.
/// </summary>
public static class YtDlpOutputParser
{
    // yt-dlp download progress: [download]  45.2% of ~123.45MiB at 5.67MiB/s ETA 00:15
    private static readonly Regex DownloadPercentRegex = new(
        @"^\[download\]\s+([\d.]+)%\s+of\s+(~?[\d.]+\w+)(?:\s+at\s+([\d.]+\w+/s))?(?:\s+ETA\s+([\d:]+))?",
        RegexOptions.Compiled);

    // yt-dlp merge progress: [Merger] Merging formats into "filename.mp4"
    private static readonly Regex MergingRegex = new(
        @"^\[Merger\]\s+Merging formats into",
        RegexOptions.Compiled);

    // yt-dlp destination line: [download] Destination: /path/to/file.mp4
    private static readonly Regex DestinationRegex = new(
        @"^\[download\]\s+Destination:\s+(.+)$",
        RegexOptions.Compiled);

    // yt-dlp has already downloaded: [download] 100% of ...
    private static readonly Regex AlreadyDownloadedRegex = new(
        @"^\[download\]\s+100% of",
        RegexOptions.Compiled);

    // yt-dlp extracting: [ExtractAudio] Destination: ...
    private static readonly Regex ExtractAudioRegex = new(
        @"^\[ExtractAudio\]",
        RegexOptions.Compiled);

    // Generic error detection
    private static readonly Regex ErrorRegex = new(
        @"^(ERROR|error):",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a single line of yt-dlp output and returns the corresponding event, or null.
    /// </summary>
    public static DownloadEvent? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        // Download progress
        var match = DownloadPercentRegex.Match(line);
        if (match.Success)
        {
            var percent = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var size = match.Groups[2].Value;
            var speed = match.Groups[3].Success ? match.Groups[3].Value : null;
            var eta = match.Groups[4].Success ? match.Groups[4].Value : null;

            return new DownloadEvent(DownloadEventType.Progress, percent, size, speed, eta, null);
        }

        // Already downloaded (100%)
        if (AlreadyDownloadedRegex.IsMatch(line))
        {
            return new DownloadEvent(DownloadEventType.Progress, 100.0, null, null, null, null);
        }

        // Merging
        if (MergingRegex.IsMatch(line))
        {
            return new DownloadEvent(DownloadEventType.Merging, null, null, null, null, null);
        }

        // Destination
        match = DestinationRegex.Match(line);
        if (match.Success)
        {
            return new DownloadEvent(DownloadEventType.Destination, null, null, null, null, match.Groups[1].Value.Trim());
        }

        // Extract audio
        if (ExtractAudioRegex.IsMatch(line))
        {
            return new DownloadEvent(DownloadEventType.Extracting, null, null, null, null, null);
        }

        // Error
        if (ErrorRegex.IsMatch(line))
        {
            return new DownloadEvent(DownloadEventType.Error, null, null, null, null, line.Trim());
        }

        return null;
    }
}

/// <summary>
/// Type of event from yt-dlp output.
/// </summary>
public enum DownloadEventType
{
    Progress,
    Merging,
    Destination,
    Extracting,
    Error
}

/// <summary>
/// Structured event parsed from yt-dlp output.
/// </summary>
/// <param name="Type">The type of event.</param>
/// <param name="Percent">Download percentage (0-100) for Progress events.</param>
/// <param name="Size">Total file size string (e.g. "123.45MiB").</param>
/// <param name="Speed">Download speed string (e.g. "5.67MiB/s").</param>
/// <param name="Eta">Estimated time remaining string (e.g. "00:15").</param>
/// <param name="FilePath">File path for Destination events, or error message for Error events.</param>
public record DownloadEvent(
    DownloadEventType Type,
    double? Percent,
    string? Size,
    string? Speed,
    string? Eta,
    string? FilePath);