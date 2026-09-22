namespace AreaDown.Core;

/// <summary>
/// Complete settings for a download operation.
/// </summary>
/// <param name="Url">The video URL to download.</param>
/// <param name="Quality">Quality preset: "best", "1080p", "720p", "480p", or "audio".</param>
/// <param name="Clip">Optional timestamp range for clipping.</param>
/// <param name="SaveFolder">Directory where the downloaded file will be saved.</param>
/// <param name="FileName">Output filename pattern (may include yt-dlp template variables).</param>
public record DownloadSettings(
    string Url,
    string Quality,
    TimestampClip? Clip,
    string SaveFolder,
    string FileName)
{
    /// <summary>
    /// Returns the yt-dlp format string for the selected quality preset.
    /// </summary>
    public string GetFormatArg() => Quality switch
    {
        "best" => "bv*[height<=?1080]+ba/b[height<=?1080]/b",
        "1080p" => "bv*[height<=?1080]+ba/b[height<=?1080]/b",
        "720p" => "bv*[height<=?720]+ba/b[height<=?720]/b",
        "480p" => "bv*[height<=?480]+ba/b[height<=?480]/b",
        "audio" => "ba/b",
        _ => "bv*[height<=?1080]+ba/b[height<=?1080]/b"
    };

    /// <summary>
    /// Returns the merge output format. Audio-only downloads use m4a; everything else uses mp4.
    /// </summary>
    public string GetMergeFormatArg() => Quality == "audio" ? "m4a" : "mp4";

    /// <summary>
    /// Returns the output template for yt-dlp -o argument.
    /// </summary>
    public string GetOutputTemplate()
    {
        var fileName = string.IsNullOrWhiteSpace(FileName) ? "%(title)s.%(ext)s" : FileName;
        return System.IO.Path.Combine(SaveFolder, fileName);
    }
}