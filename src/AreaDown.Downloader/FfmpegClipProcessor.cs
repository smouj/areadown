using System.Diagnostics;

namespace AreaDown.Downloader;

/// <summary>
/// Post-processes a downloaded video file with ffmpeg to extract an exact segment.
/// Used as a fallback when yt-dlp's native --download-sections doesn't produce a precise clip.
/// </summary>
public static class FfmpegClipProcessor
{
    /// <summary>
    /// Clips a segment from a video file using ffmpeg stream copy (fast seek).
    /// </summary>
    /// <param name="ffmpegPath">Path to ffmpeg.exe.</param>
    /// <param name="inputPath">Path to the source video file.</param>
    /// <param name="outputPath">Path for the clipped output file.</param>
    /// <param name="start">Start time of the clip.</param>
    /// <param name="end">End time of the clip (null = to end of file).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if ffmpeg exited with code 0.</returns>
    public static async Task<bool> ClipAsync(
        string ffmpegPath,
        string inputPath,
        string outputPath,
        TimeSpan start,
        TimeSpan? end,
        CancellationToken cancellationToken = default)
    {
        var args = new List<string>();

        // Input seek before -i for fast seeking
        args.Add("-ss");
        args.Add(start.ToString(@"hh\:mm\:ss\.fff"));

        args.Add("-i");
        args.Add($"\"{inputPath}\"");

        // End time
        if (end.HasValue)
        {
            args.Add("-to");
            args.Add(end.Value.ToString(@"hh\:mm\:ss\.fff"));
        }

        // Stream copy for fast processing (no re-encode)
        args.Add("-c");
        args.Add("copy");

        // Overwrite output without asking
        args.Add("-y");

        args.Add($"\"{outputPath}\"");

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var tcs = new TaskCompletionSource<bool>();

        process.Exited += (s, e) =>
        {
            try
            {
                tcs.TrySetResult(process.ExitCode == 0);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Register cancellation
        await using var reg = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process may have exited already
            }
            tcs.TrySetCanceled(cancellationToken);
        });

        return await tcs.Task;
    }
}