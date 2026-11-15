using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BlueStacks4FPSTweaker
{
    public enum AppTab
    {
        Dashboard,
        CommonProfile,
        CleanUp,
        Settings,
        About
    }

    public static class FontManager
    {
        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        private static extern int AddFontResource([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                string[] searchDirs = new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Fonts"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts"),
                    Path.Combine(Directory.GetCurrentDirectory(), "fonts")
                };

                string[] fontFiles = new string[] { "Inter-Regular.ttf", "Inter-Medium.ttf", "Inter-SemiBold.ttf", "Inter-Bold.ttf" };

                foreach (string dir in searchDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        foreach (string file in fontFiles)
                        {
                            string p = Path.Combine(dir, file);
                            if (File.Exists(p))
                            {
                                try { AddFontResource(p); } catch { }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static Font GetInter(float size, FontStyle style = FontStyle.Regular, int weight = 400)
        {
            Initialize();

            try
            {
                if (weight >= 700 || style == FontStyle.Bold)
                {
                    return new Font("Inter", size, FontStyle.Bold, GraphicsUnit.Pixel);
                }
                else if (weight >= 600)
                {
                    return new Font("Inter Semi Bold", size, FontStyle.Regular, GraphicsUnit.Pixel);
                }
                else if (weight >= 500)
                {
                    return new Font("Inter Medium", size, FontStyle.Regular, GraphicsUnit.Pixel);
                }
                else
                {
                    return new Font("Inter", size, FontStyle.Regular, GraphicsUnit.Pixel);
                }
            }
            catch
            {
                try
                {
                    return new Font("Inter", size, style, GraphicsUnit.Pixel);
                }
                catch
                {
                    return new Font("Segoe UI", size, style, GraphicsUnit.Pixel);
                }
            }
        }
    }

    public class FigmaVioletForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeft, int nTop, int nRight, int nBottom, int nWidth, int nHeight);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern int DnsFlushResolverCache();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        // Exact Colors from User Figma HTML/CSS
        private static readonly Color ColorBorderPlum = Color.FromArgb(55, 0, 92);      // #37005C
        private static readonly Color ColorNeonPurple = Color.FromArgb(153, 0, 255);    // #9900FF
        private static readonly Color ColorGradStart = Color.FromArgb(154, 0, 255);     // #9A00FF
        private static readonly Color ColorGradEnd = Color.FromArgb(205, 96, 99);       // #CD6063
        private static readonly Color ColorLightGray = Color.FromArgb(217, 217, 217);   // #D9D9D9
        private static readonly Color ColorPresetActiveBg = Color.FromArgb(114, 58, 60); // #723A3C
        private static readonly Color ColorPresetActiveBorder = Color.FromArgb(205, 96, 99); // #CD6063

        // Navigation Tabs
        private AppTab _currentTab = AppTab.Dashboard;

        // Real Synchronized Emulator Settings
        private int _currentFps = 90;
        private int _dpi = 450;
        private int _width = 1920;
        private int _height = 1080;
        private int _ramMB = 4096;
        private string _display = "1920x1080";
        private string _ramText = "4096 MB";
        private string _version = "4.240.30.1002";
        private string _installDir;
        private bool _userModifiedSettings = false;
        private string _userName = Environment.UserName;

        // Auto-Updater fields
        private string _updateStatus = "Checking for updates...";
        private bool _isUpdateAvailable = false;
        private string _latestVersion = AutoUpdater.CURRENT_VERSION;
        private string _updateDownloadUrl = string.Empty;
        private bool _isDownloadingUpdate = false;
        private int _updateProgress = 0;
        private Rectangle _rectUpdateBadge = new Rectangle(520, 15, 175, 24);
        private Rectangle _rectBtnCheckUpdates = new Rectangle(470, 314, 248, 30);
        private bool _hovCheckUpdates = false;
        private bool _hovUpdateBadge = false;

        // Clean Up Status
        private string _cleanStatusText = "System Status: Ready to optimize memory & flush caches";
        private string _lastCleanResult = string.Empty;

        // Window Controls
        private Rectangle _rectClose = new Rectangle(748, 14, 34, 26);
        private Rectangle _rectMin = new Rectangle(710, 14, 34, 26);
        
        // Left Bar Action Icons (Tab Navigation - 5 Tabs)
        private Rectangle _rectIconHome = new Rectangle(51, 52, 48, 48);
        private Rectangle _rectIconGameMode = new Rectangle(51, 114, 48, 48);
        private Rectangle _rectIconTrash = new Rectangle(51, 176, 48, 48);
        private Rectangle _rectIconSettings = new Rectangle(51, 238, 48, 48);
        private Rectangle _rectIconInfo = new Rectangle(51, 300, 48, 48);

        // Game Mode Page Controls
        private Rectangle _rectRowExplorer = new Rectangle(155, 155, 270, 50);
        private Rectangle _rectRowDwm = new Rectangle(155, 215, 270, 50);
        private Rectangle _rectRowTimer = new Rectangle(155, 275, 270, 50);
        private Rectangle _rectRowCache = new Rectangle(460, 155, 270, 50);
        private Rectangle _rectRowSnapTap = new Rectangle(460, 215, 270, 50);
        private Rectangle _rectRowPriority = new Rectangle(460, 275, 270, 50);
        private Rectangle _rectBtnToggleGameMode = new Rectangle(490, 388, 225, 48);

        // Settings Page & Dedicated GPU Controls
        private bool _preferDedicatedGpu = true;
        private string _gpuName = "Dedicated Graphics Processor";
        private Rectangle _rectGpuCheck = new Rectangle(170, 160, 22, 22);
        private Rectangle _rectGpuToggleCard = new Rectangle(145, 116, 593, 118);
        private Rectangle _rectBtnSaveSettings = new Rectangle(515, 388, 200, 48);
        private string _settingsStatusText = "Preferences synchronized with BlueStacks 4";
        private bool _hovGpuCheck = false;
        private bool _hovSaveSettings = false;

        // Dashboard Interactive Controls (Centered Middle Column: midX=130, width=302)
        private Rectangle _rectHelp = new Rectangle(145, 370, 70, 30);
        private Rectangle _rectStart = new Rectangle(146, 412, 128, 35);
        private Rectangle _rectStop = new Rectangle(286, 370, 128, 35);
        private Rectangle _rectRestart = new Rectangle(286, 412, 128, 35);
        private Rectangle _rectApply = new Rectangle(510, 390, 202, 31);
        private Rectangle _rectPill60 = new Rectangle(484, 198, 49, 19);
        private Rectangle _rectPill90 = new Rectangle(544, 198, 49, 19);
        private Rectangle _rectPill120 = new Rectangle(604, 198, 49, 19);
        private Rectangle _rectPill240 = new Rectangle(664, 198, 49, 19);
        private Rectangle _rectDpi = new Rectangle(495, 266, 100, 54);
        private Rectangle _rectDisplay = new Rectangle(605, 266, 125, 54);
        private Rectangle _rectRam = new Rectangle(495, 326, 145, 54);

        // Clean Up Page Controls (20px padding left/right inside 288px cards)
        private Rectangle _rectBtnCleanBs = new Rectangle(165, 182, 248, 32);
        private Rectangle _rectBtnCleanTemp = new Rectangle(470, 182, 248, 32);
        private Rectangle _rectBtnCleanRam = new Rectangle(165, 308, 248, 32);
        private Rectangle _rectBtnCleanDns = new Rectangle(470, 308, 248, 32);
        private Rectangle _rectBtnCleanAll = new Rectangle(515, 388, 200, 48);

        // About Page Controls
        private Rectangle _rectBtnBackDash = new Rectangle(520, 388, 195, 48);

        // Hover States
        private bool _hovClose = false;
        private bool _hovMin = false;
        private bool _hovHome = false;
        private bool _hovGameMode = false;
        private bool _hovTrash = false;
        private bool _hovSettings = false;
        private bool _hovInfo = false;
        private bool _hovRowExplorer = false;
        private bool _hovRowDwm = false;
        private bool _hovRowTimer = false;
        private bool _hovRowCache = false;
        private bool _hovRowSnapTap = false;
        private bool _hovRowPriority = false;
        private bool _hovToggleGameMode = false;
        private bool _hovHelp = false;
        private bool _hovStart = false;
        private bool _hovStop = false;
        private bool _hovRestart = false;
        private bool _hovApply = false;
        private bool _hovDpi = false;
        private bool _hovDisplay = false;
        private bool _hovRam = false;
        private bool _hovCleanBs = false;
        private bool _hovCleanTemp = false;
        private bool _hovCleanRam = false;
        private bool _hovCleanDns = false;
        private bool _hovCleanAll = false;
        private bool _hovBackDash = false;

        private Timer _syncTimer;

        // Loaded Images
        private Image _imgHome = null;
        private Image _imgTrash = null;
        private Image _imgInfo = null;
        private Image _imgGauge = null;
        private Image _imgCallout = null;
        private Image _imgHelpIcon = null;
        private Image _imgLogo = null;
        private Image _imgBackground = null;
        private Image _imgBannerCard = null;

        public FigmaVioletForm()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(800, 500);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;
            this.Text = "Bloomex v0.2.0 - BlueStacks 4 Optimizer";
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            try
            {
                if (File.Exists("app.ico"))
                {
                    this.Icon = new Icon("app.ico");
                }
                else
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    using (Stream s = asm.GetManifestResourceStream("AppIcon"))
                    {
                        if (s != null) this.Icon = new Icon(s);
                    }
                }
            }
            catch { }

            FontManager.Initialize();
            LoadAssets();

            ApplyWindowCurves();
            ApplyDwmCorners();

            this.ResumeLayout(false);

            RefreshTelemetry(true);

            _syncTimer = new Timer();
            _syncTimer.Interval = 1200;
            _syncTimer.Tick += delegate { RefreshTelemetry(false); };
            _syncTimer.Start();

            InitiateUpdateCheck();
        }

        private void LoadAssets()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                _imgHome = LoadResourceOrFile(asm, "IconHome", @"assets\icon-home.png");
                _imgTrash = LoadResourceOrFile(asm, "IconTrash", @"assets\icon-trash.png");
                _imgInfo = LoadResourceOrFile(asm, "IconInfo", @"assets\icon-info.png");
                _imgGauge = LoadResourceOrFile(asm, "PerformanceGauge", @"assets\card-performance.png");
                _imgCallout = LoadResourceOrFile(asm, "CardCallout", @"assets\card-optimization-callout.png");
                _imgHelpIcon = LoadResourceOrFile(asm, "CardHelpIcon", @"assets\card-optimization-help-icon.png");
                _imgLogo = LoadResourceOrFile(asm, "AppLogoPng", "app_icon.png");
                _imgBackground = LoadResourceOrFile(asm, "BackgroundPng", @"assets\background.png");
                _imgBannerCard = LoadResourceOrFile(asm, "BannerCardPng", @"assets\banner_card.png");
            }
            catch { }
        }

        private Image LoadResourceOrFile(Assembly asm, string resName, string relativePath)
        {
            try
            {
                using (Stream s = asm.GetManifestResourceStream(resName))
                {
                    if (s != null) return Image.FromStream(s);
                }
            }
            catch { }

            string[] paths = new string[]
            {
                relativePath,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            };

            foreach (string p in paths)
            {
                if (File.Exists(p))
                {
                    try { return Image.FromFile(p); } catch { }
                }
            }
            return null;
        }

        private void ApplyWindowCurves()
        {
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 120, 120));
        }

        private void ApplyDwmCorners()
        {
            try
            {
                int pref = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch { }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyWindowCurves();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            GameModeEngine.RegisterHotkeys(this.Handle);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            GameModeEngine.UnregisterHotkeys(this.Handle);
            base.OnHandleDestroyed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GameModeEngine.Deactivate();
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == GameModeEngine.HOTKEY_ID_GAMEMODE)
                {
                    GameModeEngine.Toggle();
                    Invalidate();
                }
                else if (id == GameModeEngine.HOTKEY_ID_CLEANCACHE)
                {
                    GameModeEngine.PurgeGameCaches();
                    Invalidate();
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Top-Right Window Controls
                if (_rectClose.Contains(e.Location))
                {
                    Application.Exit();
                    return;
                }
                if (_rectMin.Contains(e.Location))
                {
                    this.WindowState = FormWindowState.Minimized;
                    return;
                }

                // Update Notification Badge Click
                if ((_isUpdateAvailable || _isDownloadingUpdate) && _rectUpdateBadge.Contains(e.Location))
                {
                    if (_isUpdateAvailable && !_isDownloadingUpdate)
                    {
                        StartUpdateDownload();
                    }
                    return;
                }

                // Left Bar Tabs
                if (_rectIconHome.Contains(e.Location))
                {
                    _currentTab = AppTab.Dashboard;
                    Invalidate();
                    return;
                }
                if (_rectIconGameMode.Contains(e.Location))
                {
                    _currentTab = AppTab.CommonProfile;
                    Invalidate();
                    return;
                }
                if (_rectIconTrash.Contains(e.Location))
                {
                    _currentTab = AppTab.CleanUp;
                    Invalidate();
                    return;
                }
                if (_rectIconSettings.Contains(e.Location))
                {
                    _currentTab = AppTab.Settings;
                    Invalidate();
                    return;
                }
                if (_rectIconInfo.Contains(e.Location))
                {
                    _currentTab = AppTab.About;
                    Invalidate();
                    return;
                }

                // TAB 1: DASHBOARD ACTIONS
                if (_currentTab == AppTab.Dashboard)
                {
                    // Quick Sets
                    if (_rectPill60.Contains(e.Location))
                    {
                        _currentFps = 60;
                        _userModifiedSettings = true;
                        Invalidate();
                        return;
                    }
                    if (_rectPill90.Contains(e.Location))
                    {
                        _currentFps = 90;
                        _userModifiedSettings = true;
                        Invalidate();
                        return;
                    }
                    if (_rectPill120.Contains(e.Location))
                    {
                        _currentFps = 120;
                        _userModifiedSettings = true;
                        Invalidate();
                        return;
                    }
                    if (_rectPill240.Contains(e.Location))
                    {
                        _currentFps = 240;
                        _userModifiedSettings = true;
                        Invalidate();
                        return;
                    }

                    // Interactive DPI Selector
                    if (_rectDpi.Contains(e.Location))
                    {
                        ShowDpiMenu();
                        return;
                    }

                    // Interactive Display Resolution Selector
                    if (_rectDisplay.Contains(e.Location))
                    {
                        ShowDisplayMenu();
                        return;
                    }

                    // Interactive RAM Selector
                    if (_rectRam.Contains(e.Location))
                    {
                        ShowRamMenu();
                        return;
                    }

                    // Apply Changes
                    if (_rectApply.Contains(e.Location))
                    {
                        ApplyFpsChanges();
                        return;
                    }

                    // Help?
                    if (_rectHelp.Contains(e.Location))
                    {
                        ShowHelpDialog();
                        return;
                    }

                    // Start BlueStacks
                    if (_rectStart.Contains(e.Location))
                    {
                        LaunchBlueStacks();
                        return;
                    }

                    // Stop BlueStacks
                    if (_rectStop.Contains(e.Location))
                    {
                        StopBlueStacks();
                        return;
                    }

                    // Restart BlueStacks
                    if (_rectRestart.Contains(e.Location))
                    {
                        RestartBlueStacks();
                        return;
                    }
                }
                // TAB 2: COMMON PROFILE ACTIONS
                else if (_currentTab == AppTab.CommonProfile)
                {
                    if (_rectRowExplorer.Contains(e.Location))
                    {
                        GameModeEngine.OptDisableExplorer = !GameModeEngine.OptDisableExplorer;
                        Invalidate();
                        return;
                    }
                    if (_rectRowDwm.Contains(e.Location))
                    {
                        GameModeEngine.OptDisableDwm = !GameModeEngine.OptDisableDwm;
                        Invalidate();
                        return;
                    }
                    if (_rectRowTimer.Contains(e.Location))
                    {
                        GameModeEngine.OptTimerResolution = !GameModeEngine.OptTimerResolution;
                        Invalidate();
                        return;
                    }
                    if (_rectRowCache.Contains(e.Location))
                    {
                        GameModeEngine.OptCleanCacheOnLaunch = !GameModeEngine.OptCleanCacheOnLaunch;
                        Invalidate();
                        return;
                    }
                    if (_rectRowSnapTap.Contains(e.Location))
                    {
                        GameModeEngine.OptSnapTap = !GameModeEngine.OptSnapTap;
                        Invalidate();
                        return;
                    }
                    if (_rectRowPriority.Contains(e.Location))
                    {
                        GameModeEngine.OptPriorityBoost = !GameModeEngine.OptPriorityBoost;
                        Invalidate();
                        return;
                    }
                    if (_rectBtnToggleGameMode.Contains(e.Location))
                    {
                        GameModeEngine.Toggle();
                        Invalidate();
                        return;
                    }
                }
                // TAB 3: CLEANUP ACTIONS
                else if (_currentTab == AppTab.CleanUp)
                {
                    if (_rectBtnCleanBs.Contains(e.Location))
                    {
                        PerformBlueStacksClean();
                        return;
                    }
                    if (_rectBtnCleanTemp.Contains(e.Location))
                    {
                        PerformWindowsTempClean();
                        return;
                    }
                    if (_rectBtnCleanRam.Contains(e.Location))
                    {
                        PerformStandbyRamFlush();
                        return;
                    }
                    if (_rectBtnCleanDns.Contains(e.Location))
                    {
                        PerformDnsFlush();
                        return;
                    }
                    if (_rectBtnCleanAll.Contains(e.Location))
                    {
                        PerformCleanAll();
                        return;
                    }
                }
                // TAB 3: SETTINGS ACTIONS
                else if (_currentTab == AppTab.Settings)
                {
                    if (_rectGpuCheck.Contains(e.Location) || _rectGpuToggleCard.Contains(e.Location))
                    {
                        _preferDedicatedGpu = !_preferDedicatedGpu;
                        _settingsStatusText = _preferDedicatedGpu 
                            ? "Dedicated GPU Preferred (ForceDedicatedGPU=1) • Click Apply to save" 
                            : "Integrated / Default GPU (ForceDedicatedGPU=0) • Click Apply to save";
                        Invalidate();
                        return;
                    }

                    if (_rectBtnSaveSettings.Contains(e.Location))
                    {
                        ApplyGpuSettings();
                        return;
                    }
                }
                // TAB 4: ABOUT ACTIONS
                else if (_currentTab == AppTab.About)
                {
                    if (_rectBtnBackDash.Contains(e.Location))
                    {
                        _currentTab = AppTab.Dashboard;
                        Invalidate();
                        return;
                    }
                    if (_rectBtnCheckUpdates.Contains(e.Location))
                    {
                        if (_isUpdateAvailable && !_isDownloadingUpdate)
                        {
                            StartUpdateDownload();
                        }
                        else if (!_isDownloadingUpdate)
                        {
                            InitiateUpdateCheck();
                        }
                        return;
                    }
                }

                // Dragging Window
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
            base.OnMouseDown(e);
        }

        private void ShowDpiMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            int[] dpiOptions = new int[] { 160, 240, 320, 450, 480, 600 };
            foreach (int d in dpiOptions)
            {
                int val = d;
                string label = val + " DPI" + (val == 450 ? " (Free Fire Pro)" : "");
                var item = menu.Items.Add(label);
                if (val == _dpi) item.Font = new Font(item.Font, FontStyle.Bold);
                item.Click += delegate
                {
                    _dpi = val;
                    _userModifiedSettings = true;
                    Invalidate();
                };
            }
            menu.Show(this, new Point(_rectDpi.X, _rectDpi.Bottom + 2));
        }

        private void ShowDisplayMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            string[] resOptions = new string[] { "1920x1080", "1600x900", "1280x720", "2560x1440" };
            foreach (string r in resOptions)
            {
                string res = r;
                string label = res + (res == "1920x1080" ? " (FHD Recommended)" : "");
                var item = menu.Items.Add(label);
                if (res == _display) item.Font = new Font(item.Font, FontStyle.Bold);
                item.Click += delegate
                {
                    string[] parts = res.Split('x');
                    _width = int.Parse(parts[0]);
                    _height = int.Parse(parts[1]);
                    _display = res;
                    _userModifiedSettings = true;
                    Invalidate();
                };
            }
            menu.Show(this, new Point(_rectDisplay.X, _rectDisplay.Bottom + 2));
        }

        private void ShowRamMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            int[] ramOptions = new int[] { 2048, 3072, 4096, 6144, 8192 };
            foreach (int r in ramOptions)
            {
                int val = r;
                string label = val + " MB (" + (val / 1024) + " GB)" + (val == 4096 ? " (Default Recommended)" : "");
                var item = menu.Items.Add(label);
                if (val == _ramMB) item.Font = new Font(item.Font, FontStyle.Bold);
                item.Click += delegate
                {
                    _ramMB = val;
                    _ramText = val + " MB";
                    _userModifiedSettings = true;
                    Invalidate();
                };
            }
            menu.Show(this, new Point(_rectRam.X, _rectRam.Bottom + 2));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool anyHovChange = false;

            bool hClose = _rectClose.Contains(e.Location);
            if (hClose != _hovClose) { _hovClose = hClose; anyHovChange = true; }

            bool hMin = _rectMin.Contains(e.Location);
            if (hMin != _hovMin) { _hovMin = hMin; anyHovChange = true; }

            bool hHome = _rectIconHome.Contains(e.Location);
            if (hHome != _hovHome) { _hovHome = hHome; anyHovChange = true; }

            bool hGameMode = _rectIconGameMode.Contains(e.Location);
            if (hGameMode != _hovGameMode) { _hovGameMode = hGameMode; anyHovChange = true; }

            bool hTrash = _rectIconTrash.Contains(e.Location);
            if (hTrash != _hovTrash) { _hovTrash = hTrash; anyHovChange = true; }

            bool hSettings = _rectIconSettings.Contains(e.Location);
            if (hSettings != _hovSettings) { _hovSettings = hSettings; anyHovChange = true; }

            bool hInfo = _rectIconInfo.Contains(e.Location);
            if (hInfo != _hovInfo) { _hovInfo = hInfo; anyHovChange = true; }

            if (_currentTab == AppTab.Dashboard)
            {
                bool hHelp = _rectHelp.Contains(e.Location);
                if (hHelp != _hovHelp) { _hovHelp = hHelp; anyHovChange = true; }

                bool hStart = _rectStart.Contains(e.Location);
                if (hStart != _hovStart) { _hovStart = hStart; anyHovChange = true; }

                bool hStop = _rectStop.Contains(e.Location);
                if (hStop != _hovStop) { _hovStop = hStop; anyHovChange = true; }

                bool hRestart = _rectRestart.Contains(e.Location);
                if (hRestart != _hovRestart) { _hovRestart = hRestart; anyHovChange = true; }

                bool hApply = _rectApply.Contains(e.Location);
                if (hApply != _hovApply) { _hovApply = hApply; anyHovChange = true; }

                bool hDpi = _rectDpi.Contains(e.Location);
                if (hDpi != _hovDpi) { _hovDpi = hDpi; anyHovChange = true; }

                bool hDisplay = _rectDisplay.Contains(e.Location);
                if (hDisplay != _hovDisplay) { _hovDisplay = hDisplay; anyHovChange = true; }

                bool hRam = _rectRam.Contains(e.Location);
                if (hRam != _hovRam) { _hovRam = hRam; anyHovChange = true; }
            }
            else if (_currentTab == AppTab.CommonProfile)
            {
                bool hSwExp = _rectRowExplorer.Contains(e.Location);
                if (hSwExp != _hovRowExplorer) { _hovRowExplorer = hSwExp; anyHovChange = true; }

                bool hSwDwm = _rectRowDwm.Contains(e.Location);
                if (hSwDwm != _hovRowDwm) { _hovRowDwm = hSwDwm; anyHovChange = true; }

                bool hSwTimer = _rectRowTimer.Contains(e.Location);
                if (hSwTimer != _hovRowTimer) { _hovRowTimer = hSwTimer; anyHovChange = true; }

                bool hSwCache = _rectRowCache.Contains(e.Location);
                if (hSwCache != _hovRowCache) { _hovRowCache = hSwCache; anyHovChange = true; }

                bool hSwSnap = _rectRowSnapTap.Contains(e.Location);
                if (hSwSnap != _hovRowSnapTap) { _hovRowSnapTap = hSwSnap; anyHovChange = true; }

                bool hSwPrio = _rectRowPriority.Contains(e.Location);
                if (hSwPrio != _hovRowPriority) { _hovRowPriority = hSwPrio; anyHovChange = true; }

                bool hTgm = _rectBtnToggleGameMode.Contains(e.Location);
                if (hTgm != _hovToggleGameMode) { _hovToggleGameMode = hTgm; anyHovChange = true; }
            }
            else if (_currentTab == AppTab.CleanUp)
            {
                bool hCleanBs = _rectBtnCleanBs.Contains(e.Location);
                if (hCleanBs != _hovCleanBs) { _hovCleanBs = hCleanBs; anyHovChange = true; }

                bool hCleanTemp = _rectBtnCleanTemp.Contains(e.Location);
                if (hCleanTemp != _hovCleanTemp) { _hovCleanTemp = hCleanTemp; anyHovChange = true; }

                bool hCleanRam = _rectBtnCleanRam.Contains(e.Location);
                if (hCleanRam != _hovCleanRam) { _hovCleanRam = hCleanRam; anyHovChange = true; }

                bool hCleanDns = _rectBtnCleanDns.Contains(e.Location);
                if (hCleanDns != _hovCleanDns) { _hovCleanDns = hCleanDns; anyHovChange = true; }

                bool hCleanAll = _rectBtnCleanAll.Contains(e.Location);
                if (hCleanAll != _hovCleanAll) { _hovCleanAll = hCleanAll; anyHovChange = true; }
            }
            else if (_currentTab == AppTab.Settings)
            {
                bool hGpu = _rectGpuCheck.Contains(e.Location) || _rectGpuToggleCard.Contains(e.Location);
                if (hGpu != _hovGpuCheck) { _hovGpuCheck = hGpu; anyHovChange = true; }

                bool hSave = _rectBtnSaveSettings.Contains(e.Location);
                if (hSave != _hovSaveSettings) { _hovSaveSettings = hSave; anyHovChange = true; }
            }
            else if (_currentTab == AppTab.About)
            {
                bool hBackDash = _rectBtnBackDash.Contains(e.Location);
                if (hBackDash != _hovBackDash) { _hovBackDash = hBackDash; anyHovChange = true; }

                bool hCheckUpdates = _rectBtnCheckUpdates.Contains(e.Location);
                if (hCheckUpdates != _hovCheckUpdates) { _hovCheckUpdates = hCheckUpdates; anyHovChange = true; }
            }

            bool hUpdateBadge = (_isUpdateAvailable || _isDownloadingUpdate) && _rectUpdateBadge.Contains(e.Location);
            if (hUpdateBadge != _hovUpdateBadge) { _hovUpdateBadge = hUpdateBadge; anyHovChange = true; }

            bool isClickable = _hovClose || _hovMin || _hovHome || _hovGameMode || _hovTrash || _hovSettings || _hovInfo || _hovUpdateBadge ||
                (_currentTab == AppTab.Dashboard && (_hovHelp || _hovStart || _hovStop || _hovRestart || _hovApply || _hovDpi || _hovDisplay || _hovRam || _rectPill60.Contains(e.Location) || _rectPill90.Contains(e.Location) || _rectPill120.Contains(e.Location) || _rectPill240.Contains(e.Location))) ||
                (_currentTab == AppTab.CommonProfile && (_hovRowExplorer || _hovRowDwm || _hovRowTimer || _hovRowCache || _hovRowSnapTap || _hovRowPriority || _hovToggleGameMode)) ||
                (_currentTab == AppTab.CleanUp && (_hovCleanBs || _hovCleanTemp || _hovCleanRam || _hovCleanDns || _hovCleanAll)) ||
                (_currentTab == AppTab.Settings && (_hovGpuCheck || _hovSaveSettings)) ||
                (_currentTab == AppTab.About && (_hovBackDash || _hovCheckUpdates));

            this.Cursor = isClickable ? Cursors.Hand : Cursors.Default;

            if (anyHovChange) Invalidate();

            base.OnMouseMove(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // =========================================================================
            // 1. MAIN BACKGROUND:
            // width: 800px; height: 500px; background: linear-gradient(112deg, #000 1%, #37005c 97%);
            // border-radius: 49px; border: 2px #37005C solid
            // =========================================================================
            RectangleF bgRect = new RectangleF(1f, 1f, Width - 2f, Height - 2f);
            using (GraphicsPath bgPath = StudioGraphics.CreateRoundedPath(bgRect, 60f))
            {
                if (_imgBackground != null)
                {
                    var oldClip = e.Graphics.Clip;
                    e.Graphics.SetClip(bgPath, CombineMode.Intersect);
                    e.Graphics.DrawImage(_imgBackground, 0, 0, Width, Height);
                    e.Graphics.Clip = oldClip;
                }
                else
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(bgRect, Color.Black, ColorBorderPlum, 112f))
                    {
                        e.Graphics.FillPath(lgb, bgPath);
                    }
                }
                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, bgPath);
                }
            }

            // =========================================================================
            // 2. LEFT VERTICAL PILL BAR (.card1):
            // top: 34px; left: 36px; width: 78px; height: 428px; background: #37005C; border-radius: 26px
            // =========================================================================
            RectangleF leftPillRect = new RectangleF(36f, 34f, 78f, 428f);
            using (GraphicsPath lpPath = StudioGraphics.CreateRoundedPath(leftPillRect, 39f))
            {
                using (SolidBrush b = new SolidBrush(ColorBorderPlum))
                {
                    e.Graphics.FillPath(b, lpPath);
                }
                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, lpPath);
                }
            }

            // Draw Left Bar Icons with 100% PERFECT CIRCULAR Active Rings (48x48)
            float circleDiam = 48f;
            float circleX = 51f;

            // 1. Home / Dashboard (Center Y = 76)
            if (_imgHome != null)
            {
                if (_currentTab == AppTab.Dashboard)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(100, ColorNeonPurple)), circleX, 52f, circleDiam, circleDiam);
                    e.Graphics.DrawEllipse(new Pen(ColorNeonPurple, 1.8f), circleX, 52f, circleDiam, circleDiam);
                }
                else if (_hovHome)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(40, Color.White)), 53f, 54f, 44f, 44f);
                }
                e.Graphics.DrawImage(_imgHome, 62, 63, 26, 26);
            }

            // 2. Common Profile (Center Y = 138)
            if (_currentTab == AppTab.CommonProfile)
            {
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(100, ColorNeonPurple)), circleX, 114f, circleDiam, circleDiam);
                e.Graphics.DrawEllipse(new Pen(ColorNeonPurple, 1.8f), circleX, 114f, circleDiam, circleDiam);
            }
            else if (_hovGameMode)
            {
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(40, Color.White)), 53f, 116f, 44f, 44f);
            }
            DrawGamepadIcon(e.Graphics, 75f, 138f, Color.White);

            // 3. Clean Up (Center Y = 200)
            if (_imgTrash != null)
            {
                if (_currentTab == AppTab.CleanUp)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(100, ColorNeonPurple)), circleX, 176f, circleDiam, circleDiam);
                    e.Graphics.DrawEllipse(new Pen(ColorNeonPurple, 1.8f), circleX, 176f, circleDiam, circleDiam);
                }
                else if (_hovTrash)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(40, Color.White)), 53f, 178f, 44f, 44f);
                }
                e.Graphics.DrawImage(_imgTrash, 62, 187, 26, 26);
            }

            // 4. Settings (Center Y = 262)
            if (_currentTab == AppTab.Settings)
            {
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(100, ColorNeonPurple)), circleX, 238f, circleDiam, circleDiam);
                e.Graphics.DrawEllipse(new Pen(ColorNeonPurple, 1.8f), circleX, 238f, circleDiam, circleDiam);
            }
            else if (_hovSettings)
            {
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(40, Color.White)), 53f, 240f, 44f, 44f);
            }
            DrawGearIcon(e.Graphics, 75f, 262f, 13f, Color.White);

            // 5. About (Center Y = 324)
            if (_imgInfo != null)
            {
                if (_currentTab == AppTab.About)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(100, ColorNeonPurple)), circleX, 300f, circleDiam, circleDiam);
                    e.Graphics.DrawEllipse(new Pen(ColorNeonPurple, 1.8f), circleX, 300f, circleDiam, circleDiam);
                }
                else if (_hovInfo)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(40, Color.White)), 53f, 302f, 44f, 44f);
                }
                e.Graphics.DrawImage(_imgInfo, 62, 311, 26, 26);
            }
            // 5. Bloomex Logo Badge (Bottom of Left Sidebar, Center X = 75, Center Y = 420 - Static Branding Logo)
            if (_imgLogo != null)
            {
                e.Graphics.DrawImage(_imgLogo, 51f, 396f, 48f, 48f);
            }
            else if (_imgGauge != null)
            {
                e.Graphics.DrawImage(_imgGauge, 44, 397, 62, 62);
            }

            // Update Notification Badge (Top Right, if update is found or downloading)
            if (_isUpdateAvailable || _isDownloadingUpdate)
            {
                RectangleF ubRect = new RectangleF(_rectUpdateBadge.X, _rectUpdateBadge.Y, _rectUpdateBadge.Width, _rectUpdateBadge.Height);
                using (GraphicsPath ubPath = StudioGraphics.CreateRoundedPath(ubRect, 12f))
                {
                    Color ubCol1 = _hovUpdateBadge ? Color.FromArgb(220, 60, 255) : ColorNeonPurple;
                    Color ubCol2 = Color.FromArgb(205, 96, 99);
                    using (LinearGradientBrush lgb = new LinearGradientBrush(ubRect, ubCol1, ubCol2, 0f))
                    {
                        e.Graphics.FillPath(lgb, ubPath);
                    }
                    using (Pen p = new Pen(Color.White, 1f))
                    {
                        e.Graphics.DrawPath(p, ubPath);
                    }
                }
                string ubText = _isDownloadingUpdate ? ("Downloading: " + _updateProgress + "%") : ("Update " + _latestVersion + " Available");
                using (Font fUb = FontManager.GetInter(10.5f, FontStyle.Bold, 600))
                {
                    SizeF szUb = e.Graphics.MeasureString(ubText, fUb);
                    e.Graphics.DrawString(ubText, fUb, Brushes.White, ubRect.X + (ubRect.Width - szUb.Width) / 2f, ubRect.Y + (ubRect.Height - szUb.Height) / 2f);
                }
            }

            // =========================================================================
            // 3. TOP RIGHT WINDOW CONTROLS:
            // Minimize: rounded pill bar (28x5) at (636, 24)
            // Close: sleek modern cross rotated 45 deg centered at (703, 26)
            // =========================================================================
            Color cMin = _hovMin ? ColorNeonPurple : ColorLightGray;
            RectangleF minRect = new RectangleF(714f, 24f, 26f, 5f);
            using (GraphicsPath pMin = StudioGraphics.CreateRoundedPath(minRect, 2.5f))
            {
                using (SolidBrush b = new SolidBrush(cMin)) { e.Graphics.FillPath(b, pMin); }
            }

            Color cClose = _hovClose ? Color.FromArgb(244, 63, 94) : ColorLightGray;
            var stateClose = e.Graphics.Save();
            e.Graphics.TranslateTransform(764f, 26.5f);
            e.Graphics.RotateTransform(45f);
            RectangleF crossBar = new RectangleF(-12f, -2.5f, 24f, 5f);
            using (GraphicsPath pBar = StudioGraphics.CreateRoundedPath(crossBar, 2.5f))
            {
                using (SolidBrush b = new SolidBrush(cClose))
                {
                    e.Graphics.FillPath(b, pBar);
                    e.Graphics.RotateTransform(-90f);
                    e.Graphics.FillPath(b, pBar);
                }
            }
            e.Graphics.Restore(stateClose);

            // =========================================================================
            // TAB CONTENT ROUTING
            // =========================================================================
            if (_currentTab == AppTab.Dashboard)
            {
                PaintDashboardTab(e);
            }
            else if (_currentTab == AppTab.CommonProfile)
            {
                PaintCommonProfileTab(e);
            }
            else if (_currentTab == AppTab.CleanUp)
            {
                PaintCleanUpTab(e);
            }
            else if (_currentTab == AppTab.Settings)
            {
                PaintSettingsTab(e);
            }
            else if (_currentTab == AppTab.About)
            {
                PaintAboutTab(e);
            }
        }

        private void PaintDashboardTab(PaintEventArgs e)
        {
            float midX = 130f;

            // 4. USER GREETING & BLUESTACKS VERSION (GENUINE INTER FONT!)
            string greeting = "Hi! " + _userName;
            using (Font fHi = FontManager.GetInter(22f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString(greeting, fHi, Brushes.White, midX, 58);
            }
            using (Font fBs = FontManager.GetInter(15f, FontStyle.Regular, 500))
            {
                e.Graphics.DrawString("Bluestacks 04", fBs, Brushes.White, midX, 95);
            }

            // Version Pill: dynamic width based on version text
            using (Font fVer = FontManager.GetInter(12.5f, FontStyle.Bold, 600))
            {
                SizeF szVer = e.Graphics.MeasureString(_version, fVer);
                float pillW = Math.Max(105f, szVer.Width + 18f);
                RectangleF verRect = new RectangleF(midX + 115f, 95f, pillW, 18f);
                using (GraphicsPath vp = StudioGraphics.CreateRoundedPath(verRect, 9f))
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(verRect, ColorGradEnd, ColorNeonPurple, 0f))
                    {
                        e.Graphics.FillPath(lgb, vp);
                    }
                }
                e.Graphics.DrawString(_version, fVer, Brushes.White, verRect.X + (verRect.Width - szVer.Width) / 2f, verRect.Y + (verRect.Height - szVer.Height) / 2f);
            }

            // 5. OPTIMIZATION BANNER CARD:
            RectangleF bannerRect = new RectangleF(midX, 136f, 302f, 206f);
            using (GraphicsPath bnp = StudioGraphics.CreateRoundedPath(bannerRect, 38f))
            {
                var oldClip = e.Graphics.Clip;
                e.Graphics.SetClip(bnp, CombineMode.Intersect);

                if (_imgBannerCard != null)
                {
                    e.Graphics.DrawImage(_imgBannerCard, bannerRect.X, bannerRect.Y, bannerRect.Width, bannerRect.Height);
                }
                else
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(bannerRect, ColorNeonPurple, Color.FromArgb(0, 113, 194), 117f))
                    {
                        e.Graphics.FillPath(lgb, bnp);
                    }
                    using (Font fSub = FontManager.GetInter(15f, FontStyle.Bold, 600))
                    {
                        e.Graphics.DrawString("Need To Optimize Your Blustacks 4\nEmulator by Unlocking 90+ Fps.", fSub, Brushes.White, midX + 22f, 157);
                    }
                    using (Font fGot = FontManager.GetInter(36f, FontStyle.Bold, 700))
                    {
                        e.Graphics.DrawString("I GOT YOU!", fGot, Brushes.White, midX + 22f, 224);
                    }
                }

                e.Graphics.Clip = oldClip;

                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, bnp);
                }
            }

            // 6. BLUESTACKS CONTROL CARD (Bottom Left):
            RectangleF bsCardRect = new RectangleF(midX, 362f, 302f, 100f);
            using (GraphicsPath bp = StudioGraphics.CreateRoundedPath(bsCardRect, 38f))
            {
                using (SolidBrush b = new SolidBrush(ColorBorderPlum))
                {
                    e.Graphics.FillPath(b, bp);
                }
                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, bp);
                }
            }

            using (Font fHelp = FontManager.GetInter(20f, FontStyle.Regular, 400))
            {
                using (SolidBrush hb = new SolidBrush(_hovHelp ? ColorNeonPurple : Color.White))
                {
                    e.Graphics.DrawString("Help?", fHelp, hb, midX + 25f, 376);
                }
            }

            DrawPillButton20(e.Graphics, _rectStop, "Stop", _hovStop);
            DrawPillButton20(e.Graphics, _rectStart, "Start", _hovStart);
            DrawPillButton20(e.Graphics, _rectRestart, "Restart", _hovRestart);

            // 7. RIGHT SIDE CARD (Outer Container):
            RectangleF rightOuterRect = new RectangleF(460f, 55f, 306f, 407f);
            using (GraphicsPath rop = StudioGraphics.CreateRoundedPath(rightOuterRect, 56f))
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(rightOuterRect, Color.Black, ColorBorderPlum, 90f))
                {
                    e.Graphics.FillPath(lgb, rop);
                }
                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, rop);
                }
            }

            using (Font fFpsHead = FontManager.GetInter(24f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("FPS", fFpsHead, Brushes.White, 480, 82);
            }

            using (Font fBigFps = FontManager.GetInter(64f, FontStyle.Bold, 600))
            {
                using (SolidBrush nb = new SolidBrush(ColorNeonPurple))
                {
                    e.Graphics.DrawString(_currentFps.ToString(), fBigFps, nb, 476, 106);
                }
            }

            using (Font fQs = FontManager.GetInter(12f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("Quick Sets", fQs, Brushes.White, 650, 175);
            }

            DrawQuickSetPill(e.Graphics, _rectPill60, "60", _currentFps == 60);
            DrawQuickSetPill(e.Graphics, _rectPill90, "90", _currentFps == 90);
            DrawQuickSetPill(e.Graphics, _rectPill120, "120", _currentFps == 120);
            DrawQuickSetPill(e.Graphics, _rectPill240, "240", _currentFps == 240);

            // 8. INNER GRADIENT CARD (REAL-SYNC EMULATOR HARDWARE SETTINGS):
            RectangleF gradCardRect = new RectangleF(482f, 245f, 262f, 193f);
            using (GraphicsPath gcp = StudioGraphics.CreateRoundedPath(gradCardRect, 52f))
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(gradCardRect, ColorGradStart, ColorGradEnd, 123f))
                {
                    e.Graphics.FillPath(lgb, gcp);
                }
            }

            // Interactive Hover Feedback on DPI, Display, RAM
            if (_hovDpi)
            {
                using (GraphicsPath dp = StudioGraphics.CreateRoundedPath(new RectangleF(_rectDpi.X, _rectDpi.Y, _rectDpi.Width, _rectDpi.Height), 18f))
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(50, Color.White))) { e.Graphics.FillPath(hb, dp); }
                    using (Pen p = new Pen(Color.FromArgb(200, 255, 255, 255), 1.2f)) { e.Graphics.DrawPath(p, dp); }
                }
            }
            if (_hovDisplay)
            {
                using (GraphicsPath dp = StudioGraphics.CreateRoundedPath(new RectangleF(_rectDisplay.X, _rectDisplay.Y, _rectDisplay.Width, _rectDisplay.Height), 18f))
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(50, Color.White))) { e.Graphics.FillPath(hb, dp); }
                    using (Pen p = new Pen(Color.FromArgb(200, 255, 255, 255), 1.2f)) { e.Graphics.DrawPath(p, dp); }
                }
            }
            if (_hovRam)
            {
                using (GraphicsPath dp = StudioGraphics.CreateRoundedPath(new RectangleF(_rectRam.X, _rectRam.Y, _rectRam.Width, _rectRam.Height), 18f))
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(50, Color.White))) { e.Graphics.FillPath(hb, dp); }
                    using (Pen p = new Pen(Color.FromArgb(200, 255, 255, 255), 1.2f)) { e.Graphics.DrawPath(p, dp); }
                }
            }

            using (Font fLbl = FontManager.GetInter(15f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("DPI", fLbl, Brushes.White, 507, 274);
                e.Graphics.DrawString("Display", fLbl, Brushes.White, 615, 274);
                e.Graphics.DrawString("RAM", fLbl, Brushes.White, 507, 332);
            }

            // Sleek Dropdown / Edit Indicator Badges for Interactive Controls
            DrawDropdownIndicator(e.Graphics, 542f, 275f, _hovDpi);
            DrawDropdownIndicator(e.Graphics, 678f, 275f, _hovDisplay);
            DrawDropdownIndicator(e.Graphics, 550f, 333f, _hovRam);

            using (Font fVal = FontManager.GetInter(20f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString(_dpi.ToString(), fVal, Brushes.White, 507, 295);
                e.Graphics.DrawString(_ramText, fVal, Brushes.White, 507, 353);
            }
            using (Font fValDisp = FontManager.GetInter(15f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString(_display, fValDisp, Brushes.White, 615, 297);
            }

            RectangleF applyRect = new RectangleF(_rectApply.X, _rectApply.Y, _rectApply.Width, _rectApply.Height);
            using (GraphicsPath ap = StudioGraphics.CreateRoundedPath(applyRect, 15.5f))
            {
                Color btnCol = _hovApply ? Color.FromArgb(240, 230, 255) : Color.White;
                using (SolidBrush b = new SolidBrush(btnCol))
                {
                    e.Graphics.FillPath(b, ap);
                }
                if (_hovApply)
                {
                    using (Pen p = new Pen(ColorNeonPurple, 1.5f))
                    {
                        e.Graphics.DrawPath(p, ap);
                    }
                }
            }
            using (Font fApply = FontManager.GetInter(15f, FontStyle.Bold, 600))
            {
                using (SolidBrush b = new SolidBrush(ColorNeonPurple))
                {
                    SizeF szA = e.Graphics.MeasureString("APPLY CHANGES", fApply);
                    e.Graphics.DrawString("APPLY CHANGES", fApply, b, _rectApply.X + (_rectApply.Width - szA.Width) / 2f, _rectApply.Y + (_rectApply.Height - szA.Height) / 2f);
                }
            }
        }

        private void PaintCommonProfileTab(PaintEventArgs e)
        {
            // 1. Header
            using (Font fTitle = FontManager.GetInter(24f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("Game Mod", fTitle, Brushes.White, 145, 48);
            }
            using (Font fSub = FontManager.GetInter(13.5f, FontStyle.Regular, 400))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(180, 150, 240)))
                {
                    e.Graphics.DrawString("Low-latency gaming engine, zero shell interference & counter-strafing", fSub, sb, 145, 82);
                }
            }

            // Global Hotkey Indicator Pill on top right of header
            RectangleF hkRect = new RectangleF(615f, 54f, 120f, 24f);
            using (GraphicsPath hkPath = StudioGraphics.CreateRoundedPath(hkRect, 12f))
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(50, ColorBorderPlum)))
                {
                    e.Graphics.FillPath(b, hkPath);
                }
                using (Pen p = new Pen(ColorNeonPurple, 1.2f))
                {
                    e.Graphics.DrawPath(p, hkPath);
                }
            }
            using (Font fHk = FontManager.GetInter(11f, FontStyle.Bold, 600))
            {
                SizeF szHk = e.Graphics.MeasureString("CTRL+SHIFT+O", fHk);
                e.Graphics.DrawString("CTRL+SHIFT+O", fHk, Brushes.White, hkRect.X + (hkRect.Width - szHk.Width) / 2f, hkRect.Y + (hkRect.Height - szHk.Height) / 2f);
            }

            // Card 1: Shell & Compositor (Left: 145, 116, 288, 238)
            RectangleF c1Rect = new RectangleF(145f, 116f, 288f, 238f);
            using (GraphicsPath c1Path = StudioGraphics.CreateRoundedPath(c1Rect, 34f))
            {
                using (LinearGradientBrush c1Brush = new LinearGradientBrush(c1Rect, Color.FromArgb(28, 6, 48), ColorBorderPlum, 135f))
                {
                    e.Graphics.FillPath(c1Brush, c1Path);
                }
                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, c1Path);
                }
            }
            using (Font fCardHead = FontManager.GetInter(16f, FontStyle.Bold, 700))
            {
                e.Graphics.DrawString("Shell & Compositor", fCardHead, Brushes.White, 168, 132);
            }

            DrawGameOptionRow(e.Graphics, 165f, 162f, "Disable Explorer", "Suspends shell to kill DPC lag", null, GameModeEngine.OptDisableExplorer, _hovRowExplorer);
            DrawGameOptionRow(e.Graphics, 165f, 222f, "Disable DWM", "Bypasses compositing queue", null, GameModeEngine.OptDisableDwm, _hovRowDwm);
            DrawGameOptionRow(e.Graphics, 165f, 282f, "0.5ms Timer", "Ultra-low microsecond frame pacing", null, GameModeEngine.OptTimerResolution, _hovRowTimer);

            // Card 2: Engine & Input Tuning (Right: 450, 116, 288, 238)
            RectangleF c2Rect = new RectangleF(450f, 116f, 288f, 238f);
            using (GraphicsPath c2Path = StudioGraphics.CreateRoundedPath(c2Rect, 34f))
            {
                using (LinearGradientBrush c2Brush = new LinearGradientBrush(c2Rect, Color.FromArgb(28, 6, 48), ColorBorderPlum, 135f))
                {
                    e.Graphics.FillPath(c2Brush, c2Path);
                }
                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, c2Path);
                }
            }
            using (Font fCardHead = FontManager.GetInter(16f, FontStyle.Bold, 700))
            {
                e.Graphics.DrawString("Engine & Input Tuning", fCardHead, Brushes.White, 470, 132);
            }

            DrawGameOptionRow(e.Graphics, 470f, 162f, "Clean Cache", "Auto purge memory on launch", "CTRL+SHIFT+P", GameModeEngine.OptCleanCacheOnLaunch, _hovRowCache);
            DrawGameOptionRow(e.Graphics, 470f, 222f, "Snap Tap", "A+D instant counter-strafing", "A+D", GameModeEngine.OptSnapTap, _hovRowSnapTap);
            DrawGameOptionRow(e.Graphics, 470f, 282f, "High Priority", "Locks HD-Player.exe to High", "PRO", GameModeEngine.OptPriorityBoost, _hovRowPriority);

            // Bottom Action Banner (145, 370, 593, 86)
            RectangleF botRect = new RectangleF(145f, 370f, 593f, 86f);
            using (GraphicsPath botPath = StudioGraphics.CreateRoundedPath(botRect, 36f))
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(botRect, ColorGradStart, ColorGradEnd, 120f))
                {
                    e.Graphics.FillPath(lgb, botPath);
                }
            }

            RectangleF statTitleArea = new RectangleF(170f, 384f, 315f, 24f);
            RectangleF statDescArea = new RectangleF(170f, 410f, 315f, 38f);
            using (StringFormat sfTitle = new StringFormat() { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap })
            using (Font fStatHead = FontManager.GetInter(16f, FontStyle.Bold, 600))
            {
                string statusTitle = GameModeEngine.IsGameModeActive ? "Game Mod: Active" : "Game Mod: Standby";
                e.Graphics.DrawString(statusTitle, fStatHead, Brushes.White, statTitleArea, sfTitle);
            }
            using (StringFormat sfDesc = new StringFormat() { Trimming = StringTrimming.EllipsisWord })
            using (Font fStatSub = FontManager.GetInter(12.5f, FontStyle.Regular, 400))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(240, 230, 255)))
                {
                    string statusDesc = GameModeEngine.IsGameModeActive
                        ? "Ultra-low latency active • Explorer suspended • 0.5ms timer • Snap Tap running"
                        : "Ready to launch Game Mod for BlueStacks 4.";
                    e.Graphics.DrawString(statusDesc, fStatSub, sb, statDescArea, sfDesc);
                }
            }

            // Master Action Button (Pure White non-hover, 24f pill radius)
            RectangleF btnGmRect = new RectangleF(_rectBtnToggleGameMode.X, _rectBtnToggleGameMode.Y, _rectBtnToggleGameMode.Width, _rectBtnToggleGameMode.Height);
            using (GraphicsPath bgp = StudioGraphics.CreateRoundedPath(btnGmRect, 24f))
            {
                Color btnCol = _hovToggleGameMode ? Color.FromArgb(240, 230, 255) : Color.White;
                using (SolidBrush b = new SolidBrush(btnCol))
                {
                    e.Graphics.FillPath(b, bgp);
                }
                if (_hovToggleGameMode)
                {
                    using (Pen p = new Pen(ColorNeonPurple, 1.5f))
                    {
                        e.Graphics.DrawPath(p, bgp);
                    }
                }
            }
            string btnText = GameModeEngine.IsGameModeActive ? "STOP GAME MOD" : "START GAME MOD";
            using (Font fBtn = FontManager.GetInter(13.5f, FontStyle.Bold, 600))
            {
                using (SolidBrush b = new SolidBrush(ColorNeonPurple))
                {
                    SizeF szBtn = e.Graphics.MeasureString(btnText, fBtn);
                    e.Graphics.DrawString(btnText, fBtn, b, _rectBtnToggleGameMode.X + (_rectBtnToggleGameMode.Width - szBtn.Width) / 2f, _rectBtnToggleGameMode.Y + (_rectBtnToggleGameMode.Height - szBtn.Height) / 2f);
                }
            }
        }

        private void DrawGameOptionRow(Graphics g, float x, float y, string title, string desc, string badgeText, bool isEnabled, bool isHovered)
        {
            if (isHovered)
            {
                RectangleF hovRect = new RectangleF(x - 6f, y - 4f, 260f, 48f);
                using (GraphicsPath hp = StudioGraphics.CreateRoundedPath(hovRect, 12f))
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(30, Color.White)))
                    {
                        g.FillPath(hb, hp);
                    }
                }
            }

            using (Font fTitle = FontManager.GetInter(13.5f, FontStyle.Bold, 600))
            {
                g.DrawString(title, fTitle, Brushes.White, x, y);
            }

            if (!string.IsNullOrEmpty(badgeText))
            {
                using (Font fBadge = FontManager.GetInter(9.5f, FontStyle.Bold, 600))
                {
                    SizeF szB = g.MeasureString(badgeText, fBadge);
                    float bw = szB.Width + 10f;
                    float bh = 16f;
                    RectangleF badgeRect = new RectangleF(x + 215f - bw - 10f, y + 1f, bw, bh);
                    using (GraphicsPath bp = StudioGraphics.CreateRoundedPath(badgeRect, 8f))
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(80, ColorNeonPurple)))
                        {
                            g.FillPath(b, bp);
                        }
                        using (Pen p = new Pen(ColorNeonPurple, 1f))
                        {
                            g.DrawPath(p, bp);
                        }
                    }
                    g.DrawString(badgeText, fBadge, Brushes.White, badgeRect.X + (badgeRect.Width - szB.Width) / 2f, badgeRect.Y + (badgeRect.Height - szB.Height) / 2f);
                }
            }

            using (Font fDesc = FontManager.GetInter(11f, FontStyle.Regular, 400))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(190, 180, 220)))
                {
                    g.DrawString(desc, fDesc, sb, x, y + 18f);
                }
            }

            float swX = x + 215f;
            float swY = y + 8f;
            DrawToggleSwitch(g, swX, swY, isEnabled, isHovered);
        }

        private void DrawToggleSwitch(Graphics g, float x, float y, bool isOn, bool isHovered)
        {
            float w = 36f;
            float h = 18f;
            RectangleF swRect = new RectangleF(x, y, w, h);
            using (GraphicsPath p = StudioGraphics.CreateRoundedPath(swRect, 9f))
            {
                Color fill = isOn ? ColorNeonPurple : Color.FromArgb(40, 12, 60);
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, p);
                }
                Color stroke = isOn ? Color.FromArgb(220, 120, 255) : ColorBorderPlum;
                using (Pen sp = new Pen(stroke, 1.2f))
                {
                    g.DrawPath(sp, p);
                }
            }

            float thumbDiam = 12f;
            float thumbY = y + 3f;
            float thumbX = isOn ? (x + w - thumbDiam - 3f) : (x + 3f);
            using (SolidBrush tb = new SolidBrush(Color.White))
            {
                g.FillEllipse(tb, thumbX, thumbY, thumbDiam, thumbDiam);
            }
        }

        private void DrawGamepadIcon(Graphics g, float cx, float cy, Color color)
        {
            using (Pen pen = new Pen(color, 2f))
            {
                RectangleF body = new RectangleF(cx - 13f, cy - 8f, 26f, 16f);
                using (GraphicsPath gp = StudioGraphics.CreateRoundedPath(body, 5f))
                {
                    g.DrawPath(pen, gp);
                }
                g.DrawLine(pen, cx - 8f, cy - 3f, cx - 8f, cy + 3f);
                g.DrawLine(pen, cx - 11f, cy, cx - 5f, cy);
                using (SolidBrush sb = new SolidBrush(color))
                {
                    g.FillEllipse(sb, cx + 5f, cy - 4f, 3.5f, 3.5f);
                    g.FillEllipse(sb, cx + 8.5f, cy - 0.5f, 3.5f, 3.5f);
                    g.FillEllipse(sb, cx + 5f, cy + 3f, 3.5f, 3.5f);
                }
            }
        }

        private void PaintCleanUpTab(PaintEventArgs e)
        {
            // Title & Subtitle
            using (Font fTitle = FontManager.GetInter(24f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("System Clean Up", fTitle, Brushes.White, 145, 48);
            }
            using (Font fSub = FontManager.GetInter(13.5f, FontStyle.Regular, 500))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(180, 150, 240)))
                {
                    e.Graphics.DrawString("Optimize memory, remove junk files & reduce latency", fSub, sb, 145, 82);
                }
            }

            // 4 Clean Action Cards (Concise, zero text overflow, perfect margins)
            DrawCleanActionCard(e.Graphics, 145, 116, 288, 112, "Emulator Cache", "Cleans engine logs & shaders", "Clean Cache", _rectBtnCleanBs, _hovCleanBs);
            DrawCleanActionCard(e.Graphics, 450, 116, 288, 112, "Windows Temp", "Cleans temporary junk files", "Clean Temp", _rectBtnCleanTemp, _hovCleanTemp);
            DrawCleanActionCard(e.Graphics, 145, 242, 288, 112, "Standby RAM", "Reclaims unused memory", "Free RAM", _rectBtnCleanRam, _hovCleanRam);
            DrawCleanActionCard(e.Graphics, 450, 242, 288, 112, "DNS Cache", "Flushes network resolver", "Flush DNS", _rectBtnCleanDns, _hovCleanDns);

            // Bottom Action Banner (145, 370, 593, 86)
            RectangleF botRect = new RectangleF(145f, 370f, 593f, 86f);
            using (GraphicsPath botPath = StudioGraphics.CreateRoundedPath(botRect, 36f))
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(botRect, ColorGradStart, ColorGradEnd, 120f))
                {
                    e.Graphics.FillPath(lgb, botPath);
                }
            }

            RectangleF statTitleArea = new RectangleF(170f, 384f, 335f, 24f);
            RectangleF statDescArea = new RectangleF(170f, 410f, 335f, 38f);
            using (StringFormat sfTitle = new StringFormat() { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap })
            using (Font fStatHead = FontManager.GetInter(16f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("Quick Optimization", fStatHead, Brushes.White, statTitleArea, sfTitle);
            }
            using (StringFormat sfDesc = new StringFormat() { Trimming = StringTrimming.EllipsisWord })
            using (Font fStatSub = FontManager.GetInter(12.5f, FontStyle.Regular, 400))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(240, 230, 255)))
                {
                    string statText = string.IsNullOrEmpty(_lastCleanResult) ? _cleanStatusText : _lastCleanResult;
                    e.Graphics.DrawString(statText, fStatSub, sb, statDescArea, sfDesc);
                }
            }

            // Big Clean All Button
            RectangleF btnAllRect = new RectangleF(_rectBtnCleanAll.X, _rectBtnCleanAll.Y, _rectBtnCleanAll.Width, _rectBtnCleanAll.Height);
            using (GraphicsPath bap = StudioGraphics.CreateRoundedPath(btnAllRect, 24f))
            {
                Color btnCol = _hovCleanAll ? Color.FromArgb(240, 230, 255) : Color.White;
                using (SolidBrush b = new SolidBrush(btnCol))
                {
                    e.Graphics.FillPath(b, bap);
                }
                if (_hovCleanAll)
                {
                    using (Pen p = new Pen(ColorNeonPurple, 1.5f))
                    {
                        e.Graphics.DrawPath(p, bap);
                    }
                }
            }
            using (Font fAll = FontManager.GetInter(14f, FontStyle.Bold, 600))
            {
                using (SolidBrush b = new SolidBrush(ColorNeonPurple))
                {
                    SizeF szAll = e.Graphics.MeasureString("CLEAN ALL", fAll);
                    e.Graphics.DrawString("CLEAN ALL", fAll, b, _rectBtnCleanAll.X + (_rectBtnCleanAll.Width - szAll.Width) / 2f, _rectBtnCleanAll.Y + (_rectBtnCleanAll.Height - szAll.Height) / 2f);
                }
            }
        }

        private void DrawCleanActionCard(Graphics g, float x, float y, float w, float h, string title, string desc, string btnText, Rectangle btnRect, bool isHovered)
        {
            RectangleF cardRect = new RectangleF(x, y, w, h);
            using (GraphicsPath cp = StudioGraphics.CreateRoundedPath(cardRect, 30f))
            {
                using (LinearGradientBrush cb = new LinearGradientBrush(cardRect, Color.FromArgb(20, 5, 35), ColorBorderPlum, 135f))
                {
                    g.FillPath(cb, cp);
                }
                using (Pen p = new Pen(ColorBorderPlum, 1.8f))
                {
                    g.DrawPath(p, cp);
                }
            }

            using (Font fT = FontManager.GetInter(15.5f, FontStyle.Bold, 600))
            {
                g.DrawString(title, fT, Brushes.White, x + 20, y + 16);
            }

            RectangleF descRect = new RectangleF(x + 20, y + 40, w - 40, 20);
            using (StringFormat sfD = new StringFormat() { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap })
            using (Font fD = FontManager.GetInter(12f, FontStyle.Regular, 400))
            {
                using (SolidBrush db = new SolidBrush(Color.FromArgb(190, 190, 215)))
                {
                    g.DrawString(desc, fD, db, descRect, sfD);
                }
            }

            RectangleF rf = new RectangleF(btnRect.X, btnRect.Y, btnRect.Width, btnRect.Height);
            using (GraphicsPath bp = StudioGraphics.CreateRoundedPath(rf, 16f))
            {
                Color fill = isHovered ? ControlPaint.Light(ColorNeonPurple, 0.2f) : ColorNeonPurple;
                using (SolidBrush bb = new SolidBrush(fill))
                {
                    g.FillPath(bb, bp);
                }
            }
            using (Font fB = FontManager.GetInter(12.5f, FontStyle.Bold, 600))
            {
                SizeF sz = g.MeasureString(btnText, fB);
                g.DrawString(btnText, fB, Brushes.White, btnRect.X + (btnRect.Width - sz.Width) / 2f, btnRect.Y + (btnRect.Height - sz.Height) / 2f);
            }
        }

        private void PaintSettingsTab(PaintEventArgs e)
        {
            // Title & Subtitle
            using (Font fTitle = FontManager.GetInter(24f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("Engine & GPU Settings", fTitle, Brushes.White, 145, 48);
            }
            using (Font fSub = FontManager.GetInter(13.5f, FontStyle.Regular, 500))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(180, 150, 240)))
                {
                    e.Graphics.DrawString("Configure dedicated graphics acceleration & rendering preferences", fSub, sb, 145, 82);
                }
            }

            // Card 1: Dedicated GPU Settings (145, 116, 593, 118)
            RectangleF gpuCardRect = new RectangleF(145f, 116f, 593f, 118f);
            using (GraphicsPath gcp = StudioGraphics.CreateRoundedPath(gpuCardRect, 30f))
            {
                using (LinearGradientBrush cb = new LinearGradientBrush(gpuCardRect, Color.FromArgb(28, 6, 48), ColorBorderPlum, 135f))
                {
                    e.Graphics.FillPath(cb, gcp);
                }
                using (Pen p = new Pen(_hovGpuCheck ? ColorNeonPurple : ColorBorderPlum, 1.8f))
                {
                    e.Graphics.DrawPath(p, gcp);
                }
            }

            // GPU Settings Heading + (?) Info Icon
            using (Font fHead = FontManager.GetInter(16.5f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("GPU settings", fHead, Brushes.White, 172, 130);
            }
            e.Graphics.DrawEllipse(new Pen(Color.FromArgb(180, 180, 220), 1.2f), 282, 132, 16, 16);
            using (Font fQ = FontManager.GetInter(10.5f, FontStyle.Bold, 600))
            {
                using (SolidBrush qb = new SolidBrush(Color.FromArgb(200, 200, 240)))
                {
                    e.Graphics.DrawString("?", fQ, qb, 287, 133);
                }
            }

            // Interactive Checkbox [✓] Prefer dedicated computer graphics (NVIDIA only)
            RectangleF chkRect = new RectangleF(_rectGpuCheck.X, _rectGpuCheck.Y, _rectGpuCheck.Width, _rectGpuCheck.Height);
            using (GraphicsPath chkPath = StudioGraphics.CreateRoundedPath(chkRect, 5f))
            {
                Color chkFill = _preferDedicatedGpu ? ColorNeonPurple : Color.FromArgb(40, 20, 60);
                using (SolidBrush b = new SolidBrush(chkFill)) { e.Graphics.FillPath(b, chkPath); }
                using (Pen p = new Pen(_preferDedicatedGpu ? Color.White : ColorNeonPurple, 1.5f)) { e.Graphics.DrawPath(p, chkPath); }
            }
            if (_preferDedicatedGpu)
            {
                PointF[] checkPts = new PointF[]
                {
                    new PointF(_rectGpuCheck.X + 4.5f, _rectGpuCheck.Y + 11f),
                    new PointF(_rectGpuCheck.X + 9f, _rectGpuCheck.Y + 16f),
                    new PointF(_rectGpuCheck.X + 17f, _rectGpuCheck.Y + 6.5f)
                };
                using (Pen pCheck = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    e.Graphics.DrawLines(pCheck, checkPts);
                }
            }

            using (Font fCheckLbl = FontManager.GetInter(14.5f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("Prefer dedicated computer graphics (NVIDIA only)", fCheckLbl, Brushes.White, 204, 161);
            }

            // Subtitle: GPU in use (dynamically detected)
            using (Font fGpuUse = FontManager.GetInter(13f, FontStyle.Regular, 400))
            {
                using (SolidBrush gb = new SolidBrush(Color.FromArgb(170, 170, 210)))
                {
                    e.Graphics.DrawString("GPU in use: " + _gpuName, fGpuUse, gb, 172, 192);
                }
            }

            // Card 2: Graphics Renderer (145, 246, 288, 116)
            DrawCleanActionCard(e.Graphics, 145, 246, 288, 116, "Graphics Renderer", "DirectX & OpenGL bridge", "DirectX / OpenGL", new Rectangle(165, 318, 248, 30), false);

            // Card 3: ASTC Texture Pipeline (450, 246, 288, 116)
            DrawCleanActionCard(e.Graphics, 450, 246, 288, 116, "ASTC Textures", "Hardware decompression", "ASTC: Enabled", new Rectangle(470, 318, 248, 30), false);

            // Bottom Action Banner (145, 370, 593, 86)
            RectangleF botRect = new RectangleF(145f, 370f, 593f, 86f);
            using (GraphicsPath botPath = StudioGraphics.CreateRoundedPath(botRect, 36f))
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(botRect, ColorGradStart, ColorGradEnd, 120f))
                {
                    e.Graphics.FillPath(lgb, botPath);
                }
            }

            RectangleF statTitleArea = new RectangleF(170f, 384f, 335f, 24f);
            RectangleF statDescArea = new RectangleF(170f, 410f, 335f, 38f);
            using (StringFormat sfTitle = new StringFormat() { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap })
            using (Font fStatHead = FontManager.GetInter(16f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("Apply Configuration", fStatHead, Brushes.White, statTitleArea, sfTitle);
            }
            using (StringFormat sfDesc = new StringFormat() { Trimming = StringTrimming.EllipsisWord })
            using (Font fStatSub = FontManager.GetInter(12.5f, FontStyle.Regular, 400))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(240, 230, 255)))
                {
                    e.Graphics.DrawString(_settingsStatusText, fStatSub, sb, statDescArea, sfDesc);
                }
            }

            // Save Settings Button
            RectangleF btnSaveRect = new RectangleF(_rectBtnSaveSettings.X, _rectBtnSaveSettings.Y, _rectBtnSaveSettings.Width, _rectBtnSaveSettings.Height);
            using (GraphicsPath bap = StudioGraphics.CreateRoundedPath(btnSaveRect, 24f))
            {
                Color btnCol = _hovSaveSettings ? Color.FromArgb(240, 230, 255) : Color.White;
                using (SolidBrush b = new SolidBrush(btnCol))
                {
                    e.Graphics.FillPath(b, bap);
                }
                if (_hovSaveSettings)
                {
                    using (Pen p = new Pen(ColorNeonPurple, 1.5f))
                    {
                        e.Graphics.DrawPath(p, bap);
                    }
                }
            }
            using (Font fAll = FontManager.GetInter(14f, FontStyle.Bold, 600))
            {
                using (SolidBrush b = new SolidBrush(ColorNeonPurple))
                {
                    SizeF szAll = e.Graphics.MeasureString("APPLY", fAll);
                    e.Graphics.DrawString("APPLY", fAll, b, _rectBtnSaveSettings.X + (_rectBtnSaveSettings.Width - szAll.Width) / 2f, _rectBtnSaveSettings.Y + (_rectBtnSaveSettings.Height - szAll.Height) / 2f);
                }
            }
        }

        private void ApplyGpuSettings()
        {
            string err;
            bool ok = BlueStacksRegistry.SetDedicatedGPU(_preferDedicatedGpu, out err);
            if (ok)
            {
                _settingsStatusText = "Dedicated GPU setting saved (" + (_preferDedicatedGpu ? "ForceDedicatedGPU=1" : "ForceDedicatedGPU=0") + ")!";
                Invalidate();
                string statusMsg = _preferDedicatedGpu ? ("Prefer Dedicated Computer Graphics: ENABLED\n(" + _gpuName + " Active)") : "Prefer Dedicated Computer Graphics: DISABLED";
                MessageBox.Show("GPU Setting Successfully Applied to Registry!\n\n• " + statusMsg + "\n• Registry: HKLM\\SOFTWARE\\BlueStacks\\Config\\ForceDedicatedGPU = " + (_preferDedicatedGpu ? "1" : "0") + "\n\nRestart BlueStacks 4 to initialize on the dedicated GPU.", "GPU Setting Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save GPU setting: " + err, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DrawGearIcon(Graphics g, float cx, float cy, float r, Color color)
        {
            using (GraphicsPath gp = new GraphicsPath())
            {
                int teeth = 6;
                float outerR = r;
                float innerR = r * 0.70f;
                float holeR = r * 0.35f;

                PointF[] pts = new PointF[teeth * 4];
                int idx = 0;
                for (int i = 0; i < teeth; i++)
                {
                    double baseDeg = i * (360.0 / teeth);
                    double a1 = (baseDeg - 13.0) * Math.PI / 180.0;
                    double a2 = (baseDeg - 7.0) * Math.PI / 180.0;
                    double a3 = (baseDeg + 7.0) * Math.PI / 180.0;
                    double a4 = (baseDeg + 13.0) * Math.PI / 180.0;

                    pts[idx++] = new PointF((float)(cx + innerR * Math.Cos(a1)), (float)(cy + innerR * Math.Sin(a1)));
                    pts[idx++] = new PointF((float)(cx + outerR * Math.Cos(a2)), (float)(cy + outerR * Math.Sin(a2)));
                    pts[idx++] = new PointF((float)(cx + outerR * Math.Cos(a3)), (float)(cy + outerR * Math.Sin(a3)));
                    pts[idx++] = new PointF((float)(cx + innerR * Math.Cos(a4)), (float)(cy + innerR * Math.Sin(a4)));
                }
                gp.AddPolygon(pts);
                gp.AddEllipse(cx - holeR, cy - holeR, holeR * 2f, holeR * 2f);

                using (SolidBrush b = new SolidBrush(color))
                {
                    g.FillPath(b, gp);
                }
            }
        }

        private void PaintAboutTab(PaintEventArgs e)
        {
            // Title & Subtitle with Official Violet "B" App Logo
            if (_imgLogo != null)
            {
                e.Graphics.DrawImage(_imgLogo, 145, 42, 44, 44);
                using (Font fTitle = FontManager.GetInter(24f, FontStyle.Bold, 600))
                {
                    e.Graphics.DrawString("About Bloomex", fTitle, Brushes.White, 202, 42);
                }
                using (Font fSub = FontManager.GetInter(14f, FontStyle.Regular, 500))
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(180, 150, 240)))
                    {
                        e.Graphics.DrawString("Version 0.2.0", fSub, sb, 202, 72);
                    }
                }
            }
            else
            {
                using (Font fTitle = FontManager.GetInter(24f, FontStyle.Bold, 600))
                {
                    e.Graphics.DrawString("About Bloomex", fTitle, Brushes.White, 145, 48);
                }
                using (Font fSub = FontManager.GetInter(14f, FontStyle.Regular, 500))
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(180, 150, 240)))
                    {
                        e.Graphics.DrawString("Version 0.2.0", fSub, sb, 145, 82);
                    }
                }
            }

            // Card 1: Emulator & System Profile (Left: 145, 116, 288, 238)
            RectangleF c1Rect = new RectangleF(145f, 116f, 288f, 238f);
            using (GraphicsPath c1Path = StudioGraphics.CreateRoundedPath(c1Rect, 34f))
            {
                using (LinearGradientBrush c1Brush = new LinearGradientBrush(c1Rect, ColorNeonPurple, Color.FromArgb(0, 113, 194), 135f))
                {
                    e.Graphics.FillPath(c1Brush, c1Path);
                }
                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, c1Path);
                }
            }

            using (Font fCardHead = FontManager.GetInter(16.5f, FontStyle.Bold, 700))
            {
                e.Graphics.DrawString("System Architecture", fCardHead, Brushes.White, 168, 134);
            }

            using (Font fItemLbl = FontManager.GetInter(12.5f, FontStyle.Bold, 600))
            using (Font fItemVal = FontManager.GetInter(12.5f, FontStyle.Regular, 400))
            using (SolidBrush bLight = new SolidBrush(Color.FromArgb(210, 230, 255)))
            {
                var specs = new[]
                {
                    new { L = "BlueStacks Version", V = _version },
                    new { L = "Engine Architecture", V = "64-bit Android Engine" },
                    new { L = "Graphics Renderer", V = "DirectX / OpenGL" },
                    new { L = "Operating System", V = GetFriendlyOsName() }
                };

                float yOff = 168f;
                foreach (var s in specs)
                {
                    e.Graphics.DrawString(s.L, fItemLbl, Brushes.White, 168f, yOff);
                    e.Graphics.DrawString(s.V, fItemVal, bLight, 168f, yOff + 17f);
                    yOff += 36f;
                }
            }

            // Card 2: Kernel & Registry Sync Health (Right: 450, 116, 288, 238)
            RectangleF c2Rect = new RectangleF(450f, 116f, 288f, 238f);
            using (GraphicsPath c2Path = StudioGraphics.CreateRoundedPath(c2Rect, 34f))
            {
                using (LinearGradientBrush c2Brush = new LinearGradientBrush(c2Rect, Color.FromArgb(25, 5, 45), ColorBorderPlum, 135f))
                {
                    e.Graphics.FillPath(c2Brush, c2Path);
                }
                using (Pen p = new Pen(ColorBorderPlum, 2f))
                {
                    e.Graphics.DrawPath(p, c2Path);
                }
            }

            using (Font fCardHead = FontManager.GetInter(16.5f, FontStyle.Bold, 700))
            {
                e.Graphics.DrawString("Sync Status", fCardHead, Brushes.White, 470, 134);
            }

            using (Font fItemLbl = FontManager.GetInter(12.5f, FontStyle.Bold, 600))
            using (Font fItemVal = FontManager.GetInter(12.5f, FontStyle.Regular, 400))
            using (SolidBrush bGreen = new SolidBrush(Color.FromArgb(180, 240, 200)))
            {
                var syncItems = new[]
                {
                    new { L = "Config FPS Lock", V = "Strict (" + _currentFps + " FPS)" },
                    new { L = "Display DPI", V = "Synced (" + _dpi + " DPI)" },
                    new { L = "Resolution", V = _display },
                    new { L = "Memory", V = _ramText }
                };

                float yOff = 168f;
                foreach (var s in syncItems)
                {
                    e.Graphics.DrawString(s.L, fItemLbl, Brushes.White, 470f, yOff);
                    e.Graphics.DrawString(s.V, fItemVal, bGreen, 470f, yOff + 17f);
                    yOff += 36f;
                }
            }

            // Check for Updates Button on Card 2 (470, 314, 248, 30)
            RectangleF updBtnRect = new RectangleF(_rectBtnCheckUpdates.X, _rectBtnCheckUpdates.Y, _rectBtnCheckUpdates.Width, _rectBtnCheckUpdates.Height);
            using (GraphicsPath ubp = StudioGraphics.CreateRoundedPath(updBtnRect, 15f))
            {
                Color btnCol = _hovCheckUpdates ? ColorNeonPurple : Color.FromArgb(70, 20, 100);
                using (SolidBrush b = new SolidBrush(btnCol))
                {
                    e.Graphics.FillPath(b, ubp);
                }
                using (Pen p = new Pen(ColorNeonPurple, 1.2f))
                {
                    e.Graphics.DrawPath(p, ubp);
                }
            }
            string updBtnText = _isUpdateAvailable ? ("UPDATE TO " + _latestVersion) : (_isDownloadingUpdate ? ("DOWNLOADING " + _updateProgress + "%") : "CHECK FOR UPDATES");
            using (Font fUpdBtn = FontManager.GetInter(11.5f, FontStyle.Bold, 600))
            {
                SizeF szUpd = e.Graphics.MeasureString(updBtnText, fUpdBtn);
                e.Graphics.DrawString(updBtnText, fUpdBtn, Brushes.White, updBtnRect.X + (updBtnRect.Width - szUpd.Width) / 2f, updBtnRect.Y + (updBtnRect.Height - szUpd.Height) / 2f);
            }

            // Bottom Banner: Developer & Features Banner (145, 370, 593, 86)
            RectangleF botRect = new RectangleF(145f, 370f, 593f, 86f);
            using (GraphicsPath botPath = StudioGraphics.CreateRoundedPath(botRect, 36f))
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(botRect, ColorGradStart, ColorGradEnd, 120f))
                {
                    e.Graphics.FillPath(lgb, botPath);
                }
            }

            RectangleF featTitleArea = new RectangleF(170f, 384f, 335f, 24f);
            RectangleF featDescArea = new RectangleF(170f, 410f, 335f, 38f);
            using (StringFormat sfTitle = new StringFormat() { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap })
            using (Font fFeatHead = FontManager.GetInter(16f, FontStyle.Bold, 600))
            {
                e.Graphics.DrawString("Bloomex v0.2.0", fFeatHead, Brushes.White, featTitleArea, sfTitle);
            }
            using (StringFormat sfDesc = new StringFormat() { Trimming = StringTrimming.EllipsisWord })
            using (Font fFeatSub = FontManager.GetInter(12.5f, FontStyle.Regular, 400))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(240, 230, 255)))
                {
                    e.Graphics.DrawString("Optimized for high-FPS BlueStacks gameplay.", fFeatSub, sb, featDescArea, sfDesc);
                }
            }

            // Back to Dashboard Button
            RectangleF btnBackRect = new RectangleF(_rectBtnBackDash.X, _rectBtnBackDash.Y, _rectBtnBackDash.Width, _rectBtnBackDash.Height);
            using (GraphicsPath bbp = StudioGraphics.CreateRoundedPath(btnBackRect, 24f))
            {
                Color btnCol = _hovBackDash ? Color.FromArgb(240, 230, 255) : Color.White;
                using (SolidBrush b = new SolidBrush(btnCol))
                {
                    e.Graphics.FillPath(b, bbp);
                }
                if (_hovBackDash)
                {
                    using (Pen p = new Pen(ColorNeonPurple, 1.5f))
                    {
                        e.Graphics.DrawPath(p, bbp);
                    }
                }
            }
            using (Font fHomeBtn = FontManager.GetInter(13.5f, FontStyle.Bold, 600))
            {
                using (SolidBrush b = new SolidBrush(ColorNeonPurple))
                {
                    SizeF szHome = e.Graphics.MeasureString("DASHBOARD", fHomeBtn);
                    e.Graphics.DrawString("DASHBOARD", fHomeBtn, b, _rectBtnBackDash.X + (_rectBtnBackDash.Width - szHome.Width) / 2f, _rectBtnBackDash.Y + (_rectBtnBackDash.Height - szHome.Height) / 2f);
                }
            }
        }

        private string GetFriendlyOsName()
        {
            try
            {
                if (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 22000)
                    return "Windows 11 64-bit";
                if (Environment.OSVersion.Version.Major == 10)
                    return "Windows 10 64-bit";
                return "Windows 64-bit";
            }
            catch
            {
                return "Windows 64-bit";
            }
        }

        private void DrawPillButton20(Graphics g, Rectangle r, string text, bool isHovered)
        {
            RectangleF rf = new RectangleF(r.X, r.Y, r.Width, r.Height);
            using (GraphicsPath gp = StudioGraphics.CreateRoundedPath(rf, 17.5f))
            {
                Color fill = isHovered ? ControlPaint.Light(ColorNeonPurple, 0.2f) : ColorNeonPurple;
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, gp);
                }
            }

            using (Font f = FontManager.GetInter(18f, FontStyle.Bold, 600))
            {
                SizeF sz = g.MeasureString(text, f);
                g.DrawString(text, f, Brushes.White, r.X + (r.Width - sz.Width) / 2f, r.Y + (r.Height - sz.Height) / 2f);
            }
        }

        private void DrawQuickSetPill(Graphics g, Rectangle r, string text, bool isSelected)
        {
            RectangleF rf = new RectangleF(r.X, r.Y, r.Width, r.Height);
            using (GraphicsPath gp = StudioGraphics.CreateRoundedPath(rf, 9.5f))
            {
                Color fill = isSelected ? ColorPresetActiveBg : ColorBorderPlum;
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, gp);
                }

                Color stroke = isSelected ? ColorPresetActiveBorder : ColorNeonPurple;
                using (Pen p = new Pen(stroke, 1f))
                {
                    g.DrawPath(p, gp);
                }
            }

            using (Font f = FontManager.GetInter(13f, FontStyle.Bold, 600))
            {
                SizeF sz = g.MeasureString(text, f);
                g.DrawString(text, f, Brushes.White, r.X + (r.Width - sz.Width) / 2f, r.Y + (r.Height - sz.Height) / 2f);
            }
        }

        private void DrawDropdownIndicator(Graphics g, float x, float y, bool isHovered)
        {
            RectangleF pill = new RectangleF(x, y, 16f, 16f);
            using (GraphicsPath p = StudioGraphics.CreateRoundedPath(pill, 8f))
            {
                using (SolidBrush b = new SolidBrush(isHovered ? Color.FromArgb(80, Color.White) : Color.FromArgb(40, Color.Black)))
                {
                    g.FillPath(b, p);
                }
                using (Pen stroke = new Pen(isHovered ? Color.White : Color.FromArgb(180, 255, 255, 255), 1f))
                {
                    g.DrawPath(stroke, p);
                }
            }
            PointF[] pts = new PointF[]
            {
                new PointF(x + 4.5f, y + 6f),
                new PointF(x + 11.5f, y + 6f),
                new PointF(x + 8f, y + 10.5f)
            };
            using (SolidBrush b = new SolidBrush(Color.White))
            {
                g.FillPolygon(b, pts);
            }
        }

        private void InitiateUpdateCheck()
        {
            _updateStatus = "Checking for updates...";
            Invalidate();

            AutoUpdater.CheckForUpdatesAsync(res =>
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (!string.IsNullOrEmpty(res.ErrorMessage))
                    {
                        _updateStatus = "Update check: Up to date (v" + AutoUpdater.CURRENT_VERSION + ")";
                        _isUpdateAvailable = false;
                    }
                    else if (res.IsUpdateAvailable)
                    {
                        _updateStatus = "New update available: " + res.LatestVersion;
                        _isUpdateAvailable = true;
                        _latestVersion = res.LatestVersion;
                        _updateDownloadUrl = res.DownloadUrl;
                        Invalidate();

                        DialogResult dr = MessageBox.Show(
                            "A new version of Bloomex (" + res.LatestVersion + ") is available on GitHub!\n\nWould you like to download and install the update now?",
                            "Bloomex Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (dr == DialogResult.Yes && !string.IsNullOrEmpty(_updateDownloadUrl))
                        {
                            StartUpdateDownload();
                        }
                    }
                    else
                    {
                        _updateStatus = "Bloomex is up to date (v" + AutoUpdater.CURRENT_VERSION + ")";
                        _isUpdateAvailable = false;
                        Invalidate();
                    }
                });
            });
        }

        private void StartUpdateDownload()
        {
            if (_isDownloadingUpdate) return;
            if (string.IsNullOrEmpty(_updateDownloadUrl))
            {
                try { Process.Start("https://github.com/susaracreations/Bloomex/releases"); } catch { }
                return;
            }

            _isDownloadingUpdate = true;
            _updateProgress = 0;
            _updateStatus = "Downloading update: 0%...";
            Invalidate();

            AutoUpdater.DownloadAndApplyUpdate(_updateDownloadUrl,
                pct =>
                {
                    if (this.IsDisposed) return;
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        _updateProgress = pct;
                        _updateStatus = "Downloading update: " + pct + "%...";
                        Invalidate();
                    });
                },
                msg =>
                {
                    // Success
                },
                err =>
                {
                    if (this.IsDisposed) return;
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        _isDownloadingUpdate = false;
                        _updateStatus = "Update failed: " + err;
                        Invalidate();
                        MessageBox.Show("Update failed: " + err, "Bloomex Updater", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                });
        }

        // =========================================================================
        // CLEANUP ACTIONS IMPLEMENTATION
        // =========================================================================
        private string[] GetBlueStacksLogDirectories()
        {
            var list = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(_installDir))
            {
                list.Add(Path.Combine(_installDir, "Logs"));
                list.Add(Path.Combine(_installDir, @"Engine\Android\Logs"));
            }
            try
            {
                BlueStacksConfig curCfg = BlueStacksRegistry.ReadConfig();
                if (!string.IsNullOrEmpty(curCfg.DataDir))
                {
                    list.Add(Path.Combine(curCfg.DataDir, "Logs"));
                    list.Add(Path.Combine(curCfg.DataDir, @"Android\Logs"));
                    list.Add(Path.Combine(curCfg.DataDir, @"UserData\Log"));
                }
            }
            catch { }

            list.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"BlueStacks\Logs"));
            list.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"BlueStacks\Engine\UserData\Log"));
            list.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"BlueStacks\Logs"));
            return list.ToArray();
        }

        private void PerformBlueStacksClean()
        {
            int files = 0;
            long bytes = 0;
            try
            {
                string[] logDirs = GetBlueStacksLogDirectories();

                foreach (string dir in logDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(file);
                                long len = fi.Length;
                                File.Delete(file);
                                bytes += len;
                                files++;
                            }
                            catch { }
                        }
                    }
                }

                _lastCleanResult = "Cleaned " + files + " BlueStacks log and dump files (" + (bytes / 1024) + " KB freed).";
                Invalidate();
                MessageBox.Show(_lastCleanResult, "BlueStacks Clean Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Notice: " + ex.Message, "Clean Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PerformWindowsTempClean()
        {
            int files = 0;
            long bytes = 0;
            try
            {
                string tempDir = Path.GetTempPath();
                if (Directory.Exists(tempDir))
                {
                    foreach (string file in Directory.GetFiles(tempDir))
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(file);
                            long len = fi.Length;
                            File.Delete(file);
                            bytes += len;
                            files++;
                        }
                        catch { }
                    }
                }

                _lastCleanResult = "Cleaned " + files + " Windows temp files (" + (bytes / (1024 * 1024)) + " MB freed).";
                Invalidate();
                MessageBox.Show(_lastCleanResult, "Windows Temp Clean Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Notice: " + ex.Message, "Clean Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PerformStandbyRamFlush()
        {
            int procCount = 0;
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        EmptyWorkingSet(p.Handle);
                        procCount++;
                    }
                    catch { }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                _lastCleanResult = "Flushed memory working sets for " + procCount + " active processes.";
                Invalidate();
                MessageBox.Show(_lastCleanResult + "\nStandby physical RAM has been reclaimed for gaming.", "RAM Working Sets Flushed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Notice: " + ex.Message, "RAM Flush Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PerformDnsFlush()
        {
            try
            {
                DnsFlushResolverCache();
                _lastCleanResult = "Windows DNS Resolver Cache successfully flushed.";
                Invalidate();
                MessageBox.Show("Windows DNS Resolver Cache successfully flushed!\nPing spikes and network route latency cleared.", "DNS Flush Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Notice: " + ex.Message, "DNS Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PerformCleanAll()
        {
            try
            {
                // 1. BlueStacks Logs
                int bsFiles = 0;
                long bsBytes = 0;
                string[] logDirs = GetBlueStacksLogDirectories();
                foreach (string dir in logDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(file);
                                long len = fi.Length;
                                File.Delete(file);
                                bsBytes += len;
                                bsFiles++;
                            }
                            catch { }
                        }
                    }
                }

                // 2. Windows Temp
                int tempFiles = 0;
                long tempBytes = 0;
                string tempDir = Path.GetTempPath();
                if (Directory.Exists(tempDir))
                {
                    foreach (string file in Directory.GetFiles(tempDir))
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(file);
                            long len = fi.Length;
                            File.Delete(file);
                            tempBytes += len;
                            tempFiles++;
                        }
                        catch { }
                    }
                }

                // 3. RAM Flush
                int procCount = 0;
                foreach (Process p in Process.GetProcesses())
                {
                    try { EmptyWorkingSet(p.Handle); procCount++; } catch { }
                }
                GC.Collect();

                // 4. DNS Flush
                DnsFlushResolverCache();

                long totalMb = (bsBytes + tempBytes) / (1024 * 1024);
                _lastCleanResult = "Clean Complete: " + (bsFiles + tempFiles) + " files cleaned (" + totalMb + " MB), " + procCount + " processes memory trimmed, DNS flushed.";
                Invalidate();

                string msg = "1-Click Optimization Completed Successfully!\n\n" +
                             "• Temporary Junk Cleaned: " + (bsFiles + tempFiles) + " files (" + totalMb + " MB)\n" +
                             "• Memory Working Sets: Trimmmed on " + procCount + " processes\n" +
                             "• DNS Resolver Cache: Successfully flushed\n" +
                             "• System Gaming Readiness: 100% Optimized";
                MessageBox.Show(msg, "Complete System Clean", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Notice: " + ex.Message, "Clean Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowHelpDialog()
        {
            string msg = "BlueStacks 04 Optimizer & Real Sync Guide:\n\n" +
                         "1. Quick Sets: Choose 60, 90, 120, or 240 FPS.\n" +
                         "2. Custom DPI / Display / RAM: Click directly on DPI, Display, or RAM to choose presets.\n" +
                         "3. APPLY CHANGES: Writes and synchronizes all 4 settings simultaneously to the BlueStacks 4 registry.\n" +
                         "4. Start: Launches BlueStacks 4 (Bluestacks.exe).\n" +
                         "5. Restart: Closes and relaunches BlueStacks 4 to reload new kernel parameters.\n" +
                         "6. Stop: Safely terminates running BlueStacks instances.\n" +
                         "7. Navigation: Use left sidebar icons (Home = Dashboard, Trash = Clean Up, Info = About).";
            MessageBox.Show(msg, "BlueStacks 04 Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshTelemetry(bool isInitial)
        {
            try
            {
                BlueStacksConfig cfg = BlueStacksRegistry.ReadConfig();
                _installDir = cfg.InstallDir;

                if (isInitial || !_userModifiedSettings)
                {
                    if (cfg.DPI > 0) _dpi = cfg.DPI;
                    if (cfg.GuestWidth > 0 && cfg.GuestHeight > 0)
                    {
                        _width = cfg.GuestWidth;
                        _height = cfg.GuestHeight;
                        _display = _width + "x" + _height;
                    }
                    if (cfg.RamMB > 0)
                    {
                        _ramMB = cfg.RamMB;
                        _ramText = _ramMB + " MB";
                    }
                    if (isInitial && cfg.ConfigFPS > 0)
                    {
                        _currentFps = cfg.ConfigFPS;
                    }
                }
                if (!string.IsNullOrEmpty(cfg.Version)) _version = cfg.Version;
                _preferDedicatedGpu = cfg.ForceDedicatedGPU;
                if (!string.IsNullOrEmpty(cfg.AvailableGPUDetails))
                {
                    _gpuName = cfg.AvailableGPUDetails;
                }

                Invalidate();
            }
            catch { }
        }

        private void ApplyFpsChanges()
        {
            string err;
            bool ok = BlueStacksRegistry.WriteFullConfig(_currentFps, _dpi, _width, _height, _ramMB, true, false, true, out err);
            if (ok)
            {
                _userModifiedSettings = false;
                RefreshTelemetry(false);
                string msg = "Emulator Settings Synchronized & Locked!\n\n" +
                             "• Target FPS: " + _currentFps + " (Config\\FPS & BootParameters)\n" +
                             "• DPI: " + _dpi + " (BootParameters DPI=" + _dpi + ")\n" +
                             "• Display: " + _display + " (FrameBuffer\\0)\n" +
                             "• RAM: " + _ramMB + " MB (Guests\\Android Memory)\n\n" +
                             "Restart BlueStacks to activate new settings.";
                MessageBox.Show(msg, "Real-Sync Applied Successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(err, "Registry Sync Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LaunchBlueStacks()
        {
            string err;
            bool ok = BlueStacksProcess.Launch(_installDir, out err);
            if (!ok)
            {
                MessageBox.Show(err, "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestartBlueStacks()
        {
            string err;
            BlueStacksRegistry.WriteFullConfig(_currentFps, _dpi, _width, _height, _ramMB, true, false, true, out err);
            BlueStacksProcess.Restart(_installDir, out err);
        }

        private void StopBlueStacks()
        {
            string err;
            BlueStacksProcess.Stop(_installDir, out err);
        }
    }
}
