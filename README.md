# AreaDown

> Paste a URL. Pick quality. Download.

A native Windows desktop video downloader built with .NET 8 WinForms.
Part of the [Area Suite](https://github.com/smouj) — simple, native tools for everyday tasks.

## Features

- **Quality selector** — Best, 1080p, 720p, 480p, or audio-only (m4a)
- **Timestamp clipping** — Download only a segment: set start and end times (e.g. `1:23` to `4:56`)
- **Progress display** — Real-time download progress with speed and ETA
- **Cancel support** — Stop a download mid-transfer
- **Remembered settings** — Save folder and quality preference persist between sessions
- **System tray** — Minimize to tray, Ctrl+Shift+D hotkey
- **Bundled tools** — Ships with yt-dlp and FFmpeg; no separate install needed

## Requirements

- Windows 10 version 2004+ (build 19041) or later
- .NET 8 runtime
- yt-dlp and FFmpeg (bundled in the release, or available on PATH)

## Build

```bash
dotnet build -c Release
```

To publish a self-contained exe:

```bash
dotnet publish src/AreaDown.App -c Release -r win-x64 --self-contained
```

Place `yt-dlp.exe` and `ffmpeg.exe` next to the published `AreaDown.exe`.

## Usage

1. Paste a video URL (YouTube, Vimeo, etc.)
2. Select quality from the dropdown
3. Optionally check "Clip by timestamp" and enter start/end times
4. Choose a save folder
5. Click **Download**

Timestamps accept `MM:SS` or `HH:MM:SS` format.

## Architecture

```
AreaDown.App        — WinForms UI (MainForm, AppSettings)
AreaDown.Core       — Domain models (DownloadSettings, TimestampClip, FormatInfo)
AreaDown.Downloader — yt-dlp process wrapper and output parser
```

## License

MIT