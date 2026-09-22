namespace AreaDown.Core;

/// <summary>
/// Describes an available format for a video, as reported by yt-dlp -F.
/// </summary>
/// <param name="FormatId">yt-dlp format identifier (e.g. "137").</param>
/// <param name="Extension">File extension (e.g. "mp4", "webm").</param>
/// <param name="Resolution">Resolution string (e.g. "1920x1080", "audio only").</param>
/// <param name="Filesize">File size in bytes, if known.</param>
/// <param name="Vcodec">Video codec identifier, or "none" for audio-only.</param>
/// <param name="Acodec">Audio codec identifier, or "none" for video-only.</param>
public record FormatInfo(
    string FormatId,
    string Extension,
    string Resolution,
    long? Filesize,
    string? Vcodec,
    string? Acodec)
{
    /// <summary>
    /// Whether this format contains video.
    /// </summary>
    public bool HasVideo => !string.IsNullOrEmpty(Vcodec) && Vcodec != "none";

    /// <summary>
    /// Whether this format contains audio.
    /// </summary>
    public bool HasAudio => !string.IsNullOrEmpty(Acodec) && Acodec != "none";

    /// <summary>
    /// Short label for display in the UI.
    /// </summary>
    public string Label
    {
        get
        {
            var parts = new List<string>();
            if (HasVideo)
                parts.Add(Resolution);
            else
                parts.Add("audio only");
            if (!string.IsNullOrEmpty(Extension))
                parts.Add(Extension);
            return string.Join(" · ", parts);
        }
    }
}