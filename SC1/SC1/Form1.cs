using System.Drawing.Imaging;
using System.Text.Json;
using System.Timers;

namespace SC1
{
    public partial class Form1 : Form
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private System.Timers.Timer? screenshotTimer;
        private readonly string _screenshotLocation;

        private int _screenshotIntervalInSec = 30;
        private int counter = 0;
        private readonly object lockObj = new();

        public Form1()
        {
            InitializeComponent();

            // Load settings
            if (!LoadAppSettings(out string? folderPath, out int interval))
            {
                MessageBox.Show("Failed to load app settings.");
                Application.Exit();
                return;
            }

            _screenshotIntervalInSec = interval;
            _screenshotLocation = Path.Combine(folderPath, "TrayScreenshots");
            Directory.CreateDirectory(_screenshotLocation);

            // Setup tray menu and icon
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Restart", null, Start);
            trayMenu.Items.Add("Quit", null, OnExit);

            trayIcon = new NotifyIcon
            {
                Text = "Screenshot Tray App",
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            // Start screenshot capture
            Start(this, EventArgs.Empty);
        }

        private bool LoadAppSettings(out string? folderPath, out int interval)
        {
            folderPath = null;
            interval = 30;

            try
            {
                string settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "AppSettings.json");
                if (!File.Exists(settingsPath)) return false;

                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(settingsPath));
                var root = doc.RootElement;

                if (root.TryGetProperty("ScreenshotIntervalInSec", out JsonElement scInt))
                    int.TryParse(scInt.GetString(), out interval);

                if (root.TryGetProperty("ScreenshotLocation", out JsonElement scLoc))
                    folderPath = scLoc.GetString();

                return !string.IsNullOrWhiteSpace(folderPath);
            }
            catch
            {
                return false;
            }
        }

        private void Start(object? sender, EventArgs? e)
        {
            screenshotTimer?.Stop();

            screenshotTimer = new System.Timers.Timer(_screenshotIntervalInSec * 1000);
            screenshotTimer.Elapsed += TakeScreenshot;
            screenshotTimer.AutoReset = true;
            screenshotTimer.Start();

            // Minimize and hide form
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Load += (_, _) => Hide();
        }

        private void TakeScreenshot(object? sender, ElapsedEventArgs e)
        {
            lock (lockObj)
            {
                var bounds = Screen.PrimaryScreen.Bounds;
                using Bitmap bmp = new(bounds.Width, bounds.Height);
                using Graphics g = Graphics.FromImage(bmp);
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                string filename = $"{DateTime.Now:yyyyMMdd_HHmmss}_{counter++}.jpg";
                string path = Path.Combine(_screenshotLocation, filename);
                bmp.Save(path, ImageFormat.Jpeg);
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            screenshotTimer?.Stop();
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            trayIcon.Visible = false;
            screenshotTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
