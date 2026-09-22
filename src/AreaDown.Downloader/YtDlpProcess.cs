using System.Diagnostics;
using AreaDown.Core;

namespace AreaDown.Downloader;

/// <summary>
/// Wraps yt-dlp.exe as an external process. Supports progress reporting and cancellation.
/// </summary>
public sealed class YtDlpProcess : IDisposable
{
    private Process? _process;
    private bool _disposed;

    /// <summary>
    /// Fired when a parsed progress line is received from yt-dlp.
    /// </summary>
    public event EventHandler<DownloadEvent>? ProgressReceived;

    /// <summary>
    /// Fired when yt-dlp exits. The exit code is provided.
    /// </summary>
    public event EventHandler<int>? Exited;

    /// <summary>
    /// Whether a download is currently in progress.
    /// </summary>
    public bool IsRunning => _process is not null && !_process.HasExited;

    /// <summary>
    /// Finds the yt-dlp executable. Checks next to the app exe first, then PATH.
    /// </summary>
    public static string FindYtDlp()
    {
        // Check next to the running executable (extraResources / publish folder)
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var bundled = Path.Combine(appDir, "yt-dlp.exe");
        if (File.Exists(bundled))
            return bundled;

        // Check extraResources subfolder (single-file publish layout)
        var extra = Path.Combine(appDir, "extraResources", "yt-dlp.exe");
        if (File.Exists(extra))
            return extra;

        // Fall back to PATH
        var pathExe = FindInPath("yt-dlp.exe");
        if (pathExe is not null)
            return pathExe;

        throw new FileNotFoundException(
            "yt-dlp.exe not found. Place it next to AreaDown.exe or install it on your PATH.");
    }

    /// <summary>
    /// Finds the ffmpeg executable. Checks next to the app exe first, then PATH.
    /// </summary>
    public static string FindFfmpeg()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var bundled = Path.Combine(appDir, "ffmpeg.exe");
        if (File.Exists(bundled))
            return bundled;

        var extra = Path.Combine(appDir, "extraResources", "ffmpeg.exe");
        if (File.Exists(extra))
            return extra;

        var pathExe = FindInPath("ffmpeg.exe");
        if (pathExe is not null)
            return pathExe;

        throw new FileNotFoundException(
            "ffmpeg.exe not found. Place it next to AreaDown.exe or install it on your PATH.");
    }

    /// <summary>
    /// Starts a yt-dlp download with the given settings.
    /// </summary>
    /// <param name="settings">Download settings.</param>
    /// <param name="ffmpegPath">Path to ffmpeg for merging. Null to skip --ffmpeg-location.</param>
    public void Start(DownloadSettings settings, string? ffmpegPath = null)
    {
        if (IsRunning)
            throw new InvalidOperationException("A download is already in progress.");

        var ytDlpPath = FindYtDlp();
        var args = BuildArguments(settings, ffmpegPath);

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnErrorDataReceived;
        _process.Exited += OnProcessExited;

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    /// <summary>
    /// Cancels the running download by killing the yt-dlp process.
    /// </summary>
    public void Cancel()
    {
        if (_process is not null && !_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Process may have already exited
            }
        }
    }

    /// <summary>
    /// Builds the yt-dlp command-line arguments from the download settings.
    /// </summary>
    private static string BuildArguments(DownloadSettings settings, string? ffmpegPath)
    {
        var args = new List<string>();

        // Format selection
        args.Add("-f");
        args.Add(settings.GetFormatArg());

        // Merge output format
        args.Add("--merge-output-format");
        args.Add(settings.GetMergeFormatArg());

        // Output template
        args.Add("-o");
        args.Add($"\"{settings.GetOutputTemplate()}\"");

        // FFmpeg location
        if (ffmpegPath is not null)
        {
            args.Add("--ffmpeg-location");
            args.Add($"\"{ffmpegPath}\"");
        }

        // Timestamp clipping
        if (settings.Clip is not null)
        {
            args.Add("--download-sections");
            args.Add(settings.Clip.ToDownloadSectionsArg());
            args.Add("--force-keyframes-at-cuts");
        }

        // Progress template for machine-readable output
        args.Add("--progress-template");
        args.Add("\"%(progress.downloaded_bytes)s/%(progress.total_bytes)s %(progress.speed)s %(progress.eta)s\"");

        // No playlist
        args.Add("--no-playlist");

        // Verbose enough to get progress
        args.Add("--newline");

        // The URL
        args.Add($"\"{settings.Url}\"");

        return string.Join(" ", args);
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null) return;
        var evt = YtDlpOutputParser.ParseLine(e.Data);
        if (evt is not null)
            ProgressReceived?.Invoke(this, evt);
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null) return;
        var evt = YtDlpOutputParser.ParseLine(e.Data);
        if (evt is not null)
            ProgressReceived?.Invoke(this, evt);
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (_process is null) return;

        int exitCode;
        try
        {
            exitCode = _process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            exitCode = -1;
        }

        Exited?.Invoke(this, exitCode);

        Cleanup();
    }

    private void Cleanup()
    {
        if (_process is null) return;

        try
        {
            _process.OutputDataReceived -= OnOutputDataReceived;
            _process.ErrorDataReceived -= OnErrorDataReceived;
            _process.Exited -= OnProcessExited;
            _process.Close();
        }
        catch
        {
            // Swallow cleanup error
        }

        _process = null;
    }

    private static string? FindInPath(string exeName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir, exeName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Cancel();
        Cleanup();
        _disposed = true;
    }
}