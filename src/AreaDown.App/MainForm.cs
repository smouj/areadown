using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using AreaDown.Core;
using AreaDown.Downloader;

namespace AreaDown.App;

/// <summary>
/// Main application form — matches Voryda Rec's WinForms style with Segoe UI, 420px width.
/// </summary>
public sealed class MainForm : Form
{
    // Layout constants matching Voryda Rec style
    private const int FormWidth = 420;
    private const int PaddingX = 20;
    private const int ControlHeight = 32;
    private const int LabelHeight = 20;
    private const int ButtonHeight = 36;
    private const int ProgressHeight = 26;
    private const int GapSmall = 6;
    private const int GapMedium = 10;
    private const int GapLarge = 16;

    // Colors
    private static readonly Color BackgroundColor = Color.White;
    private static readonly Color AccentColor = Color.FromArgb(0x2D, 0x8B, 0x4E); // Green accent
    private static readonly Color AccentHoverColor = Color.FromArgb(0x24, 0x70, 0x3F);
    private static readonly Color CancelColor = Color.FromArgb(0xCC, 0x33, 0x33);
    private static readonly Color LabelColor = Color.FromArgb(0x33, 0x33, 0x33);
    private static readonly Color SubLabelColor = Color.FromArgb(0x66, 0x66, 0x66);
    private static readonly Color BorderColor = Color.FromArgb(0xDD, 0xDD, 0xDD);
    private static readonly Color StatusColor = Color.FromArgb(0x55, 0x55, 0x55);

    // Controls
    private readonly Label _titleLabel;
    private readonly Label _descLabel;
    private readonly TextBox _urlTextBox;
    private readonly ComboBox _qualityCombo;
    private readonly TextBox _startTimestampTextBox;
    private readonly TextBox _endTimestampTextBox;
    private readonly TextBox _saveFolderTextBox;
    private readonly Button _browseButton;
    private readonly Button _downloadButton;
    private readonly Button _cancelButton;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayMenu;

    // State
    private AppSettings _settings;
    private YtDlpProcess? _downloader;
    private bool _isDownloading;

    public MainForm()
    {
        _settings = AppSettings.Load();

        // Form setup
        Text = "Voryda Down";
        Size = new Size(FormWidth, 520);
        MinimumSize = new Size(FormWidth, 480);
        MaximumSize = new Size(FormWidth + 100, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BackgroundColor;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Icon = LoadIcon();

        var contentY = GapLarge;

        // Title
        _titleLabel = new Label
        {
            Text = "Voryda Down",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = LabelColor,
            Location = new Point(PaddingX, contentY),
            AutoSize = true
        };
        Controls.Add(_titleLabel);
        contentY += 32;

        // Description
        _descLabel = new Label
        {
            Text = "Paste a URL. Pick quality. Download.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = SubLabelColor,
            Location = new Point(PaddingX, contentY),
            AutoSize = true
        };
        Controls.Add(_descLabel);
        contentY += 24;

        // URL label
        var urlLabel = new Label
        {
            Text = "Video URL",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = LabelColor,
            Location = new Point(PaddingX, contentY),
            AutoSize = true
        };
        Controls.Add(urlLabel);
        contentY += LabelHeight;

        // URL text box
        _urlTextBox = new TextBox
        {
            Location = new Point(PaddingX, contentY),
            Size = new Size(FormWidth - PaddingX * 2 - SystemInformation.VerticalScrollBarWidth - 10, ControlHeight),
            Font = new Font("Segoe UI", 9f),
            Text = _settings.LastUrl,
            PlaceholderText = "https://www.youtube.com/watch?v=..."
        };
        Controls.Add(_urlTextBox);
        contentY += ControlHeight + GapMedium;

        // Quality label
        var qualityLabel = new Label
        {
            Text = "Quality",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = LabelColor,
            Location = new Point(PaddingX, contentY),
            AutoSize = true
        };
        Controls.Add(qualityLabel);
        contentY += LabelHeight;

        // Quality combo
        _qualityCombo = new ComboBox
        {
            Location = new Point(PaddingX, contentY),
            Size = new Size(FormWidth - PaddingX * 2 - SystemInformation.VerticalScrollBarWidth - 10, ControlHeight),
            Font = new Font("Segoe UI", 9f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Items = { "Best (merge)", "1080p", "720p", "480p", "Audio only (m4a)" }
        };
        _qualityCombo.SelectedIndex = QualityToIndex(_settings.LastQuality);
        Controls.Add(_qualityCombo);
        contentY += ControlHeight + GapMedium;

        // Timestamp clipping section
        var clipLabel = new Label
        {
            Text = "Timestamp Clipping (optional)",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = LabelColor,
            Location = new Point(PaddingX, contentY),
            AutoSize = true
        };
        Controls.Add(clipLabel);
        contentY += LabelHeight;

        // Start timestamp
        var startLabel = new Label
        {
            Text = "Start",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SubLabelColor,
            Location = new Point(PaddingX, contentY + 4),
            AutoSize = true
        };
        Controls.Add(startLabel);

        var tsWidth = (FormWidth - PaddingX * 2 - SystemInformation.VerticalScrollBarWidth - 10 - 90) / 2;
        _startTimestampTextBox = new TextBox
        {
            Location = new Point(PaddingX + 42, contentY),
            Size = new Size(tsWidth, ControlHeight),
            Font = new Font("Segoe UI", 9f),
            PlaceholderText = "1:23 or 0:01:23"
        };
        Controls.Add(_startTimestampTextBox);

        // End timestamp
        var endLabel = new Label
        {
            Text = "End",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SubLabelColor,
            Location = new Point(PaddingX + 42 + tsWidth + GapSmall + 10, contentY + 4),
            AutoSize = true
        };
        Controls.Add(endLabel);

        _endTimestampTextBox = new TextBox
        {
            Location = new Point(PaddingX + 42 + tsWidth + GapSmall + 10 + 28, contentY),
            Size = new Size(tsWidth, ControlHeight),
            Font = new Font("Segoe UI", 9f),
            PlaceholderText = "4:56 or 0:04:56"
        };
        Controls.Add(_endTimestampTextBox);
        contentY += ControlHeight + GapMedium;

        // Save folder section
        var folderLabel = new Label
        {
            Text = "Save Folder",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = LabelColor,
            Location = new Point(PaddingX, contentY),
            AutoSize = true
        };
        Controls.Add(folderLabel);
        contentY += LabelHeight;

        var folderWidth = FormWidth - PaddingX * 2 - SystemInformation.VerticalScrollBarWidth - 10 - 80;
        _saveFolderTextBox = new TextBox
        {
            Location = new Point(PaddingX, contentY),
            Size = new Size(folderWidth, ControlHeight),
            Font = new Font("Segoe UI", 9f),
            Text = _settings.SaveFolder,
            ReadOnly = false
        };
        Controls.Add(_saveFolderTextBox);

        _browseButton = new Button
        {
            Text = "Browse",
            Location = new Point(PaddingX + folderWidth + GapSmall, contentY),
            Size = new Size(75, ControlHeight),
            Font = new Font("Segoe UI", 9f),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0),
            Cursor = Cursors.Hand
        };
        _browseButton.FlatAppearance.BorderColor = BorderColor;
        _browseButton.Click += BrowseButton_Click;
        Controls.Add(_browseButton);
        contentY += ControlHeight + GapLarge;

        // Buttons
        _downloadButton = new Button
        {
            Text = "⬇ Download",
            Location = new Point(PaddingX, contentY),
            Size = new Size((FormWidth - PaddingX * 2 - GapSmall - SystemInformation.VerticalScrollBarWidth - 10) / 2, ButtonHeight),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        _downloadButton.FlatAppearance.BorderSize = 0;
        _downloadButton.Click += DownloadButton_Click;
        Controls.Add(_downloadButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(PaddingX + _downloadButton.Width + GapSmall, contentY),
            Size = new Size(_downloadButton.Width, ButtonHeight),
            Font = new Font("Segoe UI", 10f),
            FlatStyle = FlatStyle.Flat,
            BackColor = CancelColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _cancelButton.FlatAppearance.BorderSize = 0;
        _cancelButton.Click += CancelButton_Click;
        Controls.Add(_cancelButton);
        contentY += ButtonHeight + GapLarge;

        // Progress bar
        _progressBar = new ProgressBar
        {
            Location = new Point(PaddingX, contentY),
            Size = new Size(FormWidth - PaddingX * 2 - SystemInformation.VerticalScrollBarWidth - 10, ProgressHeight),
            Style = ProgressBarStyle.Continuous,
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        Controls.Add(_progressBar);
        contentY += ProgressHeight + GapSmall;

        // Status label
        _statusLabel = new Label
        {
            Text = "Ready",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = StatusColor,
            Location = new Point(PaddingX, contentY),
            Size = new Size(FormWidth - PaddingX * 2, LabelHeight),
            AutoEllipsis = true
        };
        Controls.Add(_statusLabel);

        // Tray icon
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Show", null, (_, _) => ShowFromTray());
        _trayMenu.Items.Add("Exit", null, (_, _) => CloseApp());

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon() ?? SystemIcons.Application,
            Text = "Voryda Down",
            Visible = false,
            ContextMenuStrip = _trayMenu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();

        // Form events
        Resize += MainForm_Resize;
        FormClosing += MainForm_FormClosing;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _downloader?.Dispose();
            _notifyIcon?.Dispose();
            _trayMenu?.Dispose();
        }
        base.Dispose(disposing);
    }

    // ── Quality mapping ──

    private static string IndexToQuality(int index) => index switch
    {
        0 => "best",
        1 => "1080p",
        2 => "720p",
        3 => "480p",
        4 => "audio",
        _ => "best"
    };

    private static int QualityToIndex(string quality) => quality switch
    {
        "best" => 0,
        "1080p" => 1,
        "720p" => 2,
        "480p" => 3,
        "audio" => 4,
        _ => 0
    };

    // ── Timestamp parsing ──

    // ── Event handlers ──

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select download folder",
            UseDescriptionForTitle = true,
            SelectedPath = _saveFolderTextBox.Text
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _saveFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void DownloadButton_Click(object? sender, EventArgs e)
    {
        var url = _urlTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("Please enter a video URL.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _urlTextBox.Focus();
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            MessageBox.Show("Please enter a valid HTTP/HTTPS URL.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _urlTextBox.Focus();
            return;
        }

        var saveFolder = _saveFolderTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(saveFolder))
        {
            MessageBox.Show("Please specify a save folder.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _saveFolderTextBox.Focus();
            return;
        }

        if (!Directory.Exists(saveFolder))
        {
            try
            {
                Directory.CreateDirectory(saveFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot create folder: {ex.Message}", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        var quality = IndexToQuality(_qualityCombo.SelectedIndex);
        if (!TimestampParser.TryParse(_startTimestampTextBox.Text, out var startTs))
        {
            MessageBox.Show("Enter Start as seconds, MM:SS, or HH:MM:SS.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _startTimestampTextBox.Focus();
            return;
        }

        if (!TimestampParser.TryParse(_endTimestampTextBox.Text, out var endTs))
        {
            MessageBox.Show("Enter End as seconds, MM:SS, or HH:MM:SS.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _endTimestampTextBox.Focus();
            return;
        }

        var startTime = startTs ?? TimeSpan.Zero;
        if (endTs.HasValue && endTs.Value <= startTime)
        {
            MessageBox.Show("End timestamp must be after start timestamp.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _endTimestampTextBox.Focus();
            return;
        }

        TimestampClip? clip = startTs.HasValue || endTs.HasValue
            ? new TimestampClip(startTime, endTs)
            : null;

        // Save settings
        _settings = _settings with
        {
            LastUrl = url,
            LastQuality = quality,
            SaveFolder = saveFolder
        };
        AppSettings.Save(_settings);

        var settings = new DownloadSettings(url, quality, clip, saveFolder, "%(title)s.%(ext)s");

        await RunDownloadAsync(settings);
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (_isDownloading && _downloader is not null)
        {
            _downloader.Cancel();
            SetStatus("Cancelling…");
        }
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            HideToTray();
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isDownloading)
        {
            var result = MessageBox.Show(
                "A download is in progress. Are you sure you want to exit?",
                "Voryda Down",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            _downloader?.Cancel();
        }

        _notifyIcon.Visible = false;
    }

    // ── Download logic ──

    private async Task RunDownloadAsync(DownloadSettings settings)
    {
        SetDownloadingState(true);
        _progressBar.Value = 0;
        SetStatus("Starting download…");

        try
        {
            _downloader = new YtDlpProcess();
            _downloader.ProgressReceived += OnProgressReceived;
            _downloader.Exited += OnDownloadExited;

            string? ffmpegPath;
            try
            {
                ffmpegPath = YtDlpProcess.FindFfmpeg();
            }
            catch (FileNotFoundException)
            {
                ffmpegPath = null;
            }

            try
            {
                _downloader.Start(settings, ffmpegPath);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message, "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetDownloadingState(false);
                SetStatus("Error: yt-dlp not found");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start download:\n{ex.Message}", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetDownloadingState(false);
                SetStatus("Error starting download");
                return;
            }

            // Await completion is handled by Exited event
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error:\n{ex.Message}", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetDownloadingState(false);
            SetStatus("Error");
        }
    }

    private void OnProgressReceived(object? sender, DownloadEvent evt)
    {
        // Marshal to UI thread
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnProgressReceived(sender, evt)));
            return;
        }

        switch (evt.Type)
        {
            case DownloadEventType.Progress:
                if (evt.Percent.HasValue)
                {
                    _progressBar.Value = Math.Clamp((int)Math.Round(evt.Percent.Value), 0, 100);
                }
                var parts = new List<string>();
                if (evt.Size is not null) parts.Add(evt.Size);
                if (evt.Speed is not null) parts.Add($"Speed: {evt.Speed}");
                if (evt.Eta is not null) parts.Add($"ETA: {evt.Eta}");
                SetStatus(parts.Count > 0 ? string.Join("  ·  ", parts) : "Downloading…");
                break;

            case DownloadEventType.Merging:
                SetStatus("Merging formats…");
                break;

            case DownloadEventType.Destination:
                SetStatus($"Saving: {Path.GetFileName(evt.FilePath ?? "video")}");
                break;

            case DownloadEventType.Extracting:
                SetStatus("Extracting audio…");
                break;

            case DownloadEventType.Error:
                SetStatus($"Error: {evt.FilePath ?? "unknown"}");
                break;
        }
    }

    private void OnDownloadExited(object? sender, int exitCode)
    {
        // Marshal to UI thread
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnDownloadExited(sender, exitCode)));
            return;
        }

        if (exitCode == 0)
        {
            _progressBar.Value = 100;
            SetStatus("Download complete ✓");
        }
        else if (_isDownloading)
        {
            // Was not cancelled by user
            SetStatus($"Download failed (exit code {exitCode})");
        }
        else
        {
            SetStatus("Download cancelled");
        }

        _downloader?.Dispose();
        _downloader = null;
        SetDownloadingState(false);
    }

    // ── UI helpers ──

    private void SetDownloadingState(bool downloading)
    {
        _isDownloading = downloading;
        _downloadButton.Enabled = !downloading;
        _cancelButton.Enabled = downloading;
        _urlTextBox.Enabled = !downloading;
        _qualityCombo.Enabled = !downloading;
        _startTimestampTextBox.Enabled = !downloading;
        _endTimestampTextBox.Enabled = !downloading;
        _saveFolderTextBox.Enabled = !downloading;
        _browseButton.Enabled = !downloading;

    }

    private void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => _statusLabel.Text = text));
        }
        else
        {
            _statusLabel.Text = text;
        }
    }

    private void ShowFromTray()
    {
        WindowState = FormWindowState.Normal;
        Show();
        Activate();
    }

    private void HideToTray()
    {
        _notifyIcon.Visible = true;
        Hide();
    }

    private void CloseApp()
    {
        _notifyIcon.Visible = false;
        Close();
    }

    private static Icon? LoadIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AreaDown.ico");
            if (File.Exists(iconPath))
                return new Icon(iconPath);

            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "assets", "AreaDown.ico");
            if (File.Exists(assetsPath))
                return new Icon(assetsPath);
        }
        catch
        {
            // Fall back to default system icon
        }
        return null;
    }
}