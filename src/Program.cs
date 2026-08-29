using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Antigravity 中文助手")]
[assembly: AssemblyDescription("Google Antigravity 离线界面汉化伴侣")]
[assembly: AssemblyCompany("Local Companion")]
[assembly: AssemblyProduct("Antigravity 中文助手")]
[assembly: AssemblyVersion("0.6.8.0")]
[assembly: AssemblyFileVersion("0.6.8.0")]

namespace AntigravityZhAssistant
{
    internal static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);

        [STAThread]
        private static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            bool created;
            using (Mutex mutex = new Mutex(true, "Local\\AntigravityZhAssistant.Singleton", out created))
            using (EventWaitHandle activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset,
                "Local\\AntigravityZhAssistant.Activate"))
            {
                if (!created)
                {
                    activationEvent.Set();
                    IntPtr existing = FindWindow(null, "Antigravity 中文助手");
                    if (existing != IntPtr.Zero)
                    {
                        ShowWindow(existing, 9);
                        SetForegroundWindow(existing);
                    }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                bool startupMode = Array.Exists(args, delegate(string arg)
                {
                    return string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase);
                });
                Application.Run(new MainForm(startupMode, activationEvent));
            }
        }
    }

    internal sealed class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        private const string AppName = "Antigravity 中文助手";
        private const string RunValueName = "AntigravityZhAssistant";
        private const string PackManifestUrl = "https://raw.githubusercontent.com/CX-ARTLab/antigravity-zh-assistant/main/translation/manifest.json";
        private readonly bool startupMode;
        private Label statusLabel;
        private Label detailLabel;
        private Button startButton;
        private Button hideButton;
        private CheckBox startupCheckBox;
        private CheckBox monitorCheckBox;
        private CheckBox adaptCheckBox;
        private CheckBox packUpdateCheckBox;
        private Label versionLabel;
        private Label unknownLabel;
        private Label scanLabel;
        private StatusPillButton statusPill;
        private CardPanel onboardingCard;
        private Label infoLabel;
        private readonly NotifyIcon trayIcon;
        private readonly ToolStripMenuItem trayStartupItem;
        private readonly System.Windows.Forms.Timer monitorTimer;
        private readonly HttpClient localHttp;
        private readonly HttpClient updateHttp;
        private readonly string translationScriptTemplate;
        private readonly EventWaitHandle activationEvent;
        private readonly Thread activationThread;
        private bool busy;
        private bool exiting;
        private bool initialized;
        private DateTime lastInjection = DateTime.MinValue;
        private float uiScale = 1F;
        private readonly HashSet<string> unknownStrings = new HashSet<string>(StringComparer.Ordinal);
        private string lastAntigravityVersion = "未检测";
        private DateTime lastPackWrite = DateTime.MinValue;

        private static string DataDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Antigravity 中文助手"); }
        }

        private static string AssistantVersionText
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return version == null
                    ? "未知"
                    : version.Major + "." + version.Minor + "." + version.Build;
            }
        }

        private static string PackPath { get { return Path.Combine(DataDirectory, "translation-pack.json"); } }
        private static string ReportPath { get { return Path.Combine(DataDirectory, "待适配词条.json"); } }

        public MainForm(bool startupMode, EventWaitHandle activationEvent)
        {
            this.startupMode = startupMode;
            this.activationEvent = activationEvent;
            translationScriptTemplate = LoadEmbeddedText("TranslatorJs");
            Directory.CreateDirectory(DataDirectory);
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseProxy = false;
            localHttp = new HttpClient(handler);
            localHttp.Timeout = TimeSpan.FromSeconds(4);
            updateHttp = new HttpClient();
            updateHttp.Timeout = TimeSpan.FromSeconds(8);
            updateHttp.DefaultRequestHeaders.UserAgent.ParseAdd("AntigravityZhAssistant/0.6.8");

            Text = AppName;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            ClientSize = new Size(820, 650);
            MinimumSize = new Size(760, 620);
            Font = new Font("Microsoft YaHei UI", 9F);
            Icon = LoadAssistantIcon();
            BackColor = Color.FromArgb(248, 250, 253);

            GradientPanel header = new GradientPanel();
            header.Location = new Point(0, 0);
            header.Size = new Size(820, 104);
            header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            header.Color1 = Color.FromArgb(248, 250, 253);
            header.Color2 = Color.FromArgb(248, 250, 253);
            Controls.Add(header);

            PictureBox logo = new PictureBox();
            logo.Image = Icon.ToBitmap();
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Location = new Point(32, 25);
            logo.Size = new Size(50, 50);
            header.Controls.Add(logo);

            Label title = new Label();
            title.Text = "Antigravity 中文助手";
            title.Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(31, 31, 31);
            title.BackColor = Color.Transparent;
            title.Location = new Point(98, 17);
            title.AutoSize = true;
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "自动适配 · 离线优先 · 不修改官方文件";
            subtitle.ForeColor = Color.FromArgb(68, 71, 70);
            subtitle.BackColor = Color.Transparent;
            subtitle.Location = new Point(100, 58);
            subtitle.Size = new Size(430, 24);
            header.Controls.Add(subtitle);

            Label versionBadge = new Label();
            versionBadge.Text = "v" + AssistantVersionText;
            versionBadge.TextAlign = ContentAlignment.MiddleCenter;
            versionBadge.ForeColor = Color.FromArgb(11, 87, 208);
            versionBadge.BackColor = Color.FromArgb(211, 227, 253);
            versionBadge.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            versionBadge.Location = new Point(704, 34);
            versionBadge.Size = new Size(84, 30);
            versionBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ApplyRoundedRegion(versionBadge, 15);
            header.Controls.Add(versionBadge);

            CardPanel statusPanel = new CardPanel();
            statusPanel.Location = new Point(32, 112);
            statusPanel.Size = new Size(756, 132);
            statusPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(statusPanel);

            Panel statusDot = new Panel();
            statusDot.BackColor = Color.FromArgb(24, 128, 56);
            statusDot.Location = new Point(22, 24);
            statusDot.Size = new Size(10, 10);
            statusPanel.Controls.Add(statusDot);

            statusLabel = new Label();
            statusLabel.Text = "准备就绪";
            statusLabel.Font = new Font("Microsoft YaHei UI", 11.5F, FontStyle.Bold);
            statusLabel.ForeColor = Color.FromArgb(24, 128, 56);
            statusLabel.Location = new Point(43, 16);
            statusLabel.AutoSize = true;
            statusPanel.Controls.Add(statusLabel);

            detailLabel = new Label();
            detailLabel.Text = "等待连接 Antigravity。";
            detailLabel.ForeColor = Color.FromArgb(68, 71, 70);
            detailLabel.Location = new Point(43, 46);
            detailLabel.Size = new Size(680, 24);
            detailLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            statusPanel.Controls.Add(detailLabel);

            versionLabel = CreateMetricLabel(statusPanel, "Antigravity：未检测", 22);
            unknownLabel = CreateMetricLabel(statusPanel, "待适配：0", 286);
            scanLabel = CreateMetricLabel(statusPanel, "上次扫描：尚未", 514);
            versionLabel.Top = unknownLabel.Top = scanLabel.Top = 92;
            scanLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            CardPanel settingsPanel = new CardPanel();
            settingsPanel.Location = new Point(32, 260);
            settingsPanel.Size = new Size(756, 286);
            settingsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(settingsPanel);

            Label settingsTitle = new Label();
            settingsTitle.Text = "自动化设置";
            settingsTitle.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            settingsTitle.Location = new Point(20, 16);
            settingsTitle.AutoSize = true;
            settingsPanel.Controls.Add(settingsTitle);

            monitorCheckBox = CreateSettingCheckBox("后台监控 Antigravity", 22, 52, GetSetting("AutoMonitor", true));
            adaptCheckBox = CreateSettingCheckBox("Antigravity 更新后自动适配", 22, 100, GetSetting("AutoAdapt", true));
            packUpdateCheckBox = CreateSettingCheckBox("自动加载最新汉化包", 22, 148, GetSetting("AutoPack", true));
            settingsPanel.Controls.Add(monitorCheckBox);
            settingsPanel.Controls.Add(adaptCheckBox);
            settingsPanel.Controls.Add(packUpdateCheckBox);

            settingsPanel.Controls.Add(CreateSettingDescription("发现窗口后自动重新应用汉化。", 340, 54));
            settingsPanel.Controls.Add(CreateSettingDescription("检测新版本与新增英文，并生成待适配记录。", 340, 102));
            settingsPanel.Controls.Add(CreateSettingDescription("优先读取本机更新的翻译包，无需重装助手。", 340, 150));

            startupCheckBox = new CheckBox();
            startupCheckBox.Text = "随 Windows 启动";
            startupCheckBox.Location = new Point(22, 196);
            startupCheckBox.Size = new Size(280, 28);
            startupCheckBox.ForeColor = Color.FromArgb(31, 31, 31);
            startupCheckBox.Checked = IsAutoStartEnabled();
            startupCheckBox.CheckedChanged += StartupCheckBoxChanged;
            settingsPanel.Controls.Add(startupCheckBox);
            settingsPanel.Controls.Add(CreateSettingDescription("可随时关闭；启动后默认驻留系统托盘。", 340, 198));

            Label networkLabel = new Label();
            networkLabel.Text = "隐私：仅扫描 Antigravity 系统界面，不读取对话、代码或项目文件。";
            networkLabel.ForeColor = Color.FromArgb(95, 99, 104);
            networkLabel.Location = new Point(22, 246);
            networkLabel.Size = new Size(700, 24);
            networkLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            settingsPanel.Controls.Add(networkLabel);

            startButton = new Button();
            startButton.Text = "立即检测并应用";
            startButton.Location = new Point(604, 568);
            startButton.Size = new Size(184, 46);
            startButton.BackColor = Color.FromArgb(11, 87, 208);
            startButton.ForeColor = Color.White;
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.FlatAppearance.BorderSize = 0;
            startButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(8, 76, 181);
            startButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ApplyRoundedRegion(startButton, 23);
            startButton.Click += async delegate { await StartAndTranslateAsync(true); };
            Controls.Add(startButton);

            hideButton = new Button();
            hideButton.Text = "隐藏到托盘";
            hideButton.Location = new Point(466, 568);
            hideButton.Size = new Size(126, 46);
            StyleSecondaryButton(hideButton);
            hideButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            hideButton.Click += delegate { HideToTray(); };
            Controls.Add(hideButton);

            Button exitButton = new Button();
            exitButton.Text = "退出助手";
            exitButton.Location = new Point(32, 568);
            exitButton.Size = new Size(104, 46);
            StyleTextButton(exitButton);
            exitButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            exitButton.Click += delegate { ExitAssistant(); };
            Controls.Add(exitButton);

            Button reportButton = new Button();
            reportButton.Text = "查看扫描记录";
            reportButton.Location = new Point(326, 568);
            reportButton.Size = new Size(128, 46);
            StyleTextButton(reportButton);
            reportButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            reportButton.Click += delegate { ShowScanReport(); };
            Controls.Add(reportButton);

            monitorCheckBox.CheckedChanged += AutomationSettingChanged;
            adaptCheckBox.CheckedChanged += AutomationSettingChanged;
            packUpdateCheckBox.CheckedChanged += AutomationSettingChanged;

            RebuildInterface();

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem showItem = new ToolStripMenuItem("打开中文助手");
            showItem.Click += delegate { ShowFromTray(); };
            trayMenu.Items.Add(showItem);
            ToolStripMenuItem translateItem = new ToolStripMenuItem("启动 / 重新应用汉化");
            translateItem.Click += async delegate { await StartAndTranslateAsync(true); };
            trayMenu.Items.Add(translateItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayStartupItem = new ToolStripMenuItem("随 Windows 启动");
            trayStartupItem.CheckOnClick = true;
            trayStartupItem.Checked = startupCheckBox.Checked;
            trayStartupItem.CheckedChanged += TrayStartupChanged;
            trayMenu.Items.Add(trayStartupItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出助手");
            exitItem.Click += delegate { ExitAssistant(); };
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Text = AppName;
            trayIcon.Icon = Icon;
            trayIcon.Visible = true;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += delegate { ShowFromTray(); };

            monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = 3000;
            monitorTimer.Tick += async delegate { await MonitorTickAsync(); };
            monitorTimer.Start();

            activationThread = new Thread(ActivationLoop);
            activationThread.IsBackground = true;
            activationThread.Name = "AntigravityZhAssistant.Activation";
            activationThread.Start();

            Shown += async delegate
            {
                initialized = true;
                if (packUpdateCheckBox.Checked) await TryUpdateTranslationPackAsync();
                if (this.startupMode)
                {
                    HideToTray();
                    await StartAndTranslateAsync(false, true);
                }
                else
                {
                    await StartAndTranslateAsync(true);
                }
            };
            FormClosing += MainFormClosing;
        }

        private void RebuildInterface()
        {
            SuspendLayout();
            Control[] previousControls = new Control[Controls.Count];
            Controls.CopyTo(previousControls, 0);
            Controls.Clear();
            foreach (Control control in previousControls) control.Dispose();

            using (Graphics dpiGraphics = CreateGraphics())
                uiScale = Math.Max(1F, dpiGraphics.DpiX / 96F);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(U(500), U(550));
            MinimumSize = MaximumSize = SizeFromClientSize(ClientSize);
            BackColor = Color.FromArgb(234, 236, 240);
            BackgroundImage = null;
            Padding = Padding.Empty;
            DoubleBuffered = true;
            ApplyRoundedRegion(this, U(8));
            Paint += DrawOnboardingBackground;

            WindowGlyphButton minimizeButton = new WindowGlyphButton("\uE921");
            minimizeButton.SetBounds(U(364), 0, U(45), U(32));
            minimizeButton.TabStop = false;
            minimizeButton.Click += delegate { WindowState = FormWindowState.Minimized; };
            Controls.Add(minimizeButton);

            WindowGlyphButton maximizeButton = new WindowGlyphButton("\uE922");
            maximizeButton.SetBounds(U(409), 0, U(45), U(32));
            maximizeButton.TabStop = false;
            Controls.Add(maximizeButton);

            WindowGlyphButton closeButton = new WindowGlyphButton("\uE8BB");
            closeButton.SetBounds(U(454), 0, U(46), U(32));
            closeButton.IsCloseButton = true;
            closeButton.TabStop = false;
            closeButton.Click += delegate { Close(); };
            Controls.Add(closeButton);

            PictureBox brand = new PictureBox();
            brand.Image = LoadEmbeddedImage("AssistantIconPng");
            brand.SizeMode = PictureBoxSizeMode.Zoom;
            brand.BackColor = Color.Transparent;
            brand.SetBounds(U(218), U(109), U(64), U(64));
            Controls.Add(brand);

            Label assistantTitle = FixedPixelLabel("Welcome to Antigravity", 24F, FontStyle.Regular, Color.FromArgb(76, 79, 105));
            assistantTitle.Font = new Font("Segoe UI Variable Display Semib", 24F * uiScale,
                FontStyle.Regular, GraphicsUnit.Pixel);
            assistantTitle.SetBounds(U(78), U(205), U(344), U(32));
            assistantTitle.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(assistantTitle);

            DrawOnboardingCard();

            statusLabel = new Label();
            statusLabel.Visible = false;
            detailLabel = new Label();
            detailLabel.Visible = false;
            versionLabel = new Label();
            versionLabel.Visible = false;
            unknownLabel = new Label();
            unknownLabel.Visible = false;
            scanLabel = new Label();
            scanLabel.Visible = false;
            Controls.Add(statusLabel);
            Controls.Add(detailLabel);
            Controls.Add(versionLabel);
            Controls.Add(unknownLabel);
            Controls.Add(scanLabel);

            monitorCheckBox = new CheckBox();
            monitorCheckBox.Checked = true;
            adaptCheckBox = new SoftCheckBox();
            adaptCheckBox.Text = "自动更新";
            adaptCheckBox.Font = new Font("Microsoft YaHei UI", 12F * uiScale, FontStyle.Regular, GraphicsUnit.Pixel);
            adaptCheckBox.Checked = GetSetting("AutoAdapt", true) && GetSetting("AutoPack", true);
            adaptCheckBox.SetBounds(U(86), U(123), U(80), U(24));
            onboardingCard.Controls.Add(adaptCheckBox);

            packUpdateCheckBox = new CheckBox();
            packUpdateCheckBox.Checked = adaptCheckBox.Checked;
            packUpdateCheckBox.Visible = false;

            startupCheckBox = new SoftCheckBox();
            startupCheckBox.Text = "开机启动";
            startupCheckBox.Font = new Font("Microsoft YaHei UI", 12F * uiScale, FontStyle.Regular, GraphicsUnit.Pixel);
            startupCheckBox.Checked = IsAutoStartEnabled();
            startupCheckBox.SetBounds(U(178), U(123), U(80), U(24));
            onboardingCard.Controls.Add(startupCheckBox);

            hideButton = new Button();
            hideButton.Visible = false;
            adaptCheckBox.CheckedChanged += AutoUpdateCheckChanged;
            adaptCheckBox.CheckedChanged += AutomationSettingChanged;
            packUpdateCheckBox.CheckedChanged += AutomationSettingChanged;
            startupCheckBox.CheckedChanged += StartupCheckBoxChanged;

            lastAntigravityVersion = GetAntigravityVersion();
            UpdateMetrics();
            ResumeLayout(true);
        }

        private void DrawOnboardingCard()
        {
            onboardingCard = new CardPanel();
            CardPanel card = onboardingCard;
            card.SetBounds(U(78), U(269), U(344), U(168));
            card.BackColor = Color.FromArgb(236, 239, 242);
            card.BorderColor = Color.FromArgb(215, 217, 222);
            card.CornerRadius = U(12);
            Controls.Add(card);

            Label assistantVersion = FixedPixelLabel("汉化助手 v" + AssistantVersionText, 14F,
                FontStyle.Regular, Color.FromArgb(76, 79, 105));
            assistantVersion.Font = new Font("Microsoft YaHei UI", 14F * uiScale,
                FontStyle.Bold, GraphicsUnit.Pixel);
            assistantVersion.SetBounds(U(24), U(21), U(296), U(20));
            assistantVersion.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(assistantVersion);

            statusPill = new StatusPillButton();
            statusPill.Text = "尚未汉化";
            statusPill.Font = new Font("Microsoft YaHei UI", 15F * uiScale,
                FontStyle.Bold, GraphicsUnit.Pixel);
            statusPill.SetBounds(U(28), U(62), U(272), U(40));
            statusPill.Click += async delegate { await ToggleLocalizationAsync(); };
            startButton = statusPill;
            card.Controls.Add(statusPill);

            infoLabel = FixedPixelLabel(BuildInfoText(), 12F, FontStyle.Regular, Color.FromArgb(103, 107, 129));
            infoLabel.Font = new Font("Segoe UI", 12F * uiScale, FontStyle.Regular, GraphicsUnit.Pixel);
            infoLabel.SetBounds(U(114), U(454), U(272), U(24));
            infoLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(infoLabel);
        }

        private string BuildInfoText()
        {
            return "Antigravity " + lastAntigravityVersion + "  ·  待适配 " + unknownStrings.Count;
        }

        private void DrawOnboardingBackground(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            GraphicsState state = e.Graphics.Save();
            e.Graphics.ScaleTransform(uiScale, uiScale);
            DrawSoftGlow(e.Graphics, new Rectangle(142, 57, 148, 158), Color.FromArgb(66, 53, 206, 255));
            DrawSoftGlow(e.Graphics, new Rectangle(198, 43, 148, 148), Color.FromArgb(54, 255, 197, 76));
            DrawSoftGlow(e.Graphics, new Rectangle(230, 61, 134, 144), Color.FromArgb(48, 255, 91, 119));
            DrawSoftGlow(e.Graphics, new Rectangle(183, 101, 151, 150), Color.FromArgb(48, 102, 105, 255));

            using (SolidBrush titleButtons = new SolidBrush(Color.FromArgb(228, 230, 234)))
                e.Graphics.FillRectangle(titleButtons, 364, 0, 136, 32);

            using (GraphicsPath shadow = CreateRoundedPath(new Rectangle(78, 271, 344, 168), 12))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(22, 0, 0, 0)))
                e.Graphics.FillPath(shadowBrush, shadow);

            e.Graphics.Restore(state);

            using (GraphicsPath border = CreateRoundedPath(
                new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1), U(8)))
            using (Pen pen = new Pen(Color.FromArgb(65, 68, 78)))
                e.Graphics.DrawPath(pen, border);
        }

        private static void DrawSoftGlow(Graphics graphics, Rectangle bounds, Color centerColor)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = centerColor;
                    brush.SurroundColors = new[] { Color.FromArgb(0, centerColor.R, centerColor.G, centerColor.B) };
                    brush.FocusScales = new PointF(0.08F, 0.08F);
                    graphics.FillEllipse(brush, bounds);
                }
            }
        }

        private void AutoUpdateCheckChanged(object sender, EventArgs e)
        {
            if (packUpdateCheckBox != null) packUpdateCheckBox.Checked = adaptCheckBox.Checked;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y < U(32))
            {
                ReleaseCapture();
                SendMessage(Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
            }
            base.OnMouseDown(e);
        }

        private int U(int value)
        {
            return Math.Max(1, (int)Math.Round(value * uiScale));
        }

        private Label FixedLabel(string text, float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Microsoft YaHei UI", size, style);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.AutoEllipsis = true;
            return label;
        }

        private Label FixedPixelLabel(string text, float pixelSize, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Microsoft YaHei UI", pixelSize, style, GraphicsUnit.Pixel);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.AutoEllipsis = false;
            label.UseCompatibleTextRendering = false;
            return label;
        }

        private CardPanel FixedCard(int x, int y, int width, int height)
        {
            CardPanel card = new CardPanel();
            card.SetBounds(U(x), U(y), U(width), U(height));
            card.CornerRadius = U(16);
            return card;
        }

        private Label AddFixedMetric(Control parent, string caption, string value, int x, int y, int width)
        {
            CardPanel metric = new CardPanel();
            metric.SetBounds(U(x), U(y), U(width), U(62));
            metric.BackColor = Color.FromArgb(241, 244, 249);
            metric.BorderColor = metric.BackColor;
            metric.CornerRadius = U(10);
            parent.Controls.Add(metric);
            Label captionLabel = FixedLabel(caption, 7.5F, FontStyle.Bold, Color.FromArgb(95, 99, 104));
            captionLabel.SetBounds(U(12), U(8), U(width - 24), U(20));
            metric.Controls.Add(captionLabel);
            Label valueLabel = FixedLabel(value, 9.5F, FontStyle.Bold, Color.FromArgb(31, 31, 31));
            valueLabel.SetBounds(U(12), U(29), U(width - 24), U(25));
            metric.Controls.Add(valueLabel);
            return valueLabel;
        }

        private CheckBox AddFixedSetting(Control parent, string text, bool value, int y)
        {
            Label label = FixedLabel(text, 9F, FontStyle.Bold, Color.FromArgb(31, 31, 31));
            label.SetBounds(U(20), U(y), U(500), U(28));
            label.TextAlign = ContentAlignment.MiddleLeft;
            parent.Controls.Add(label);
            MaterialSwitch toggle = new MaterialSwitch();
            toggle.Checked = value;
            toggle.AccessibleName = text;
            toggle.SetBounds(U(586), U(y + 1), U(46), U(26));
            parent.Controls.Add(toggle);
            return toggle;
        }

        private MaterialButton FixedButton(string text, MaterialButtonVariant variant, int x, int y, int width)
        {
            MaterialButton button = new MaterialButton();
            button.Text = text;
            button.Variant = variant;
            button.SetBounds(U(x), U(y), U(width), U(44));
            return button;
        }

        private Control BuildHeader()
        {
            TableLayoutPanel header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.Transparent;
            header.Margin = new Padding(0);
            header.ColumnCount = 3;
            header.RowCount = 1;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            PictureBox logo = new PictureBox();
            logo.Image = Icon.ToBitmap();
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Dock = DockStyle.Fill;
            logo.Margin = new Padding(4, 13, 10, 13);
            header.Controls.Add(logo, 0, 0);

            TableLayoutPanel heading = new TableLayoutPanel();
            heading.Dock = DockStyle.Fill;
            heading.BackColor = Color.Transparent;
            heading.Margin = new Padding(0);
            heading.RowCount = 1;
            heading.ColumnCount = 1;
            heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label title = new Label();
            title.Text = "Antigravity 中文助手";
            title.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(31, 31, 31);
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Margin = new Padding(0);
            heading.Controls.Add(title, 0, 0);
            header.Controls.Add(heading, 1, 0);

            Label versionBadge = new Label();
            versionBadge.Text = "v" + AssistantVersionText;
            versionBadge.TextAlign = ContentAlignment.MiddleCenter;
            versionBadge.ForeColor = Color.FromArgb(11, 87, 208);
            versionBadge.BackColor = Color.FromArgb(211, 227, 253);
            versionBadge.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            versionBadge.Dock = DockStyle.Fill;
            versionBadge.Margin = new Padding(4, 25, 0, 25);
            ApplyRoundedRegion(versionBadge, 16);
            header.Controls.Add(versionBadge, 2, 0);
            return header;
        }

        private Control BuildStatusCard()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0);
            card.Padding = new Padding(20, 14, 20, 14);
            card.CornerRadius = 16;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.BackColor = Color.Transparent;
            layout.Margin = new Padding(0);
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            card.Controls.Add(layout);

            FlowLayoutPanel heading = new FlowLayoutPanel();
            heading.Dock = DockStyle.Fill;
            heading.BackColor = Color.Transparent;
            heading.WrapContents = false;
            heading.Margin = new Padding(0);
            Panel dot = new Panel();
            dot.Size = new Size(10, 10);
            dot.BackColor = Color.FromArgb(24, 128, 56);
            dot.Margin = new Padding(0, 8, 12, 0);
            dot.Resize += delegate { dot.Region = new Region(new Rectangle(0, 0, dot.Width, dot.Height)); };
            heading.Controls.Add(dot);

            statusLabel = new Label();
            statusLabel.Text = "准备就绪";
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            statusLabel.ForeColor = Color.FromArgb(24, 128, 56);
            statusLabel.Margin = new Padding(0, 1, 0, 0);
            heading.Controls.Add(statusLabel);
            layout.Controls.Add(heading, 0, 0);

            detailLabel = new Label();
            detailLabel.Text = "等待连接 Antigravity。";

            TableLayoutPanel metrics = new TableLayoutPanel();
            metrics.Dock = DockStyle.Fill;
            metrics.BackColor = Color.Transparent;
            metrics.Margin = new Padding(0, 3, 0, 0);
            metrics.ColumnCount = 3;
            metrics.RowCount = 1;
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            metrics.Controls.Add(BuildMetricCell("ANTIGRAVITY", "未检测", out versionLabel), 0, 0);
            metrics.Controls.Add(BuildMetricCell("待适配", "0", out unknownLabel), 1, 0);
            metrics.Controls.Add(BuildMetricCell("上次扫描", "尚未", out scanLabel), 2, 0);
            layout.Controls.Add(metrics, 0, 1);
            return card;
        }

        private static Control BuildMetricCell(string captionText, string valueText, out Label valueLabel)
        {
            CardPanel cell = new CardPanel();
            cell.Dock = DockStyle.Fill;
            cell.Margin = new Padding(0, 0, 8, 0);
            cell.Padding = new Padding(12, 5, 12, 4);
            cell.BackColor = Color.FromArgb(241, 244, 249);
            cell.BorderColor = Color.FromArgb(241, 244, 249);
            cell.CornerRadius = 10;

            TableLayoutPanel stack = new TableLayoutPanel();
            stack.Dock = DockStyle.Fill;
            stack.BackColor = Color.Transparent;
            stack.Margin = new Padding(0);
            stack.ColumnCount = 1;
            stack.RowCount = 2;
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));

            Label caption = new Label();
            caption.Text = captionText;
            caption.Dock = DockStyle.Fill;
            caption.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            caption.ForeColor = Color.FromArgb(95, 99, 104);
            caption.TextAlign = ContentAlignment.BottomLeft;
            caption.Margin = new Padding(0);
            stack.Controls.Add(caption, 0, 0);

            valueLabel = new Label();
            valueLabel.Text = valueText;
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            valueLabel.ForeColor = Color.FromArgb(31, 31, 31);
            valueLabel.TextAlign = ContentAlignment.TopLeft;
            valueLabel.AutoEllipsis = true;
            valueLabel.Margin = new Padding(0);
            stack.Controls.Add(valueLabel, 0, 1);
            cell.Controls.Add(stack);
            return cell;
        }

        private Control BuildSettingsCard()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0);
            card.Padding = new Padding(20, 10, 20, 10);
            card.CornerRadius = 16;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.BackColor = Color.Transparent;
            layout.Margin = new Padding(0);
            layout.ColumnCount = 1;
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
            card.Controls.Add(layout);

            Label title = new Label();
            title.Text = "自动化设置";
            title.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(31, 31, 31);
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Margin = new Padding(2, 0, 0, 0);
            layout.Controls.Add(title, 0, 0);

            monitorCheckBox = new CheckBox();
            monitorCheckBox.Checked = true;
            adaptCheckBox = CreateMaterialSwitch("Antigravity 更新后自动适配", GetSetting("AutoAdapt", true));
            packUpdateCheckBox = CreateMaterialSwitch("自动加载最新汉化包", GetSetting("AutoPack", true));
            startupCheckBox = CreateMaterialSwitch("随 Windows 启动", IsAutoStartEnabled());

            layout.Controls.Add(BuildSettingRow("Antigravity 更新后自动适配", adaptCheckBox), 0, 1);
            layout.Controls.Add(BuildSettingRow("自动加载最新汉化包", packUpdateCheckBox), 0, 2);
            layout.Controls.Add(BuildSettingRow("随 Windows 启动", startupCheckBox), 0, 3);

            adaptCheckBox.CheckedChanged += AutomationSettingChanged;
            packUpdateCheckBox.CheckedChanged += AutomationSettingChanged;
            startupCheckBox.CheckedChanged += StartupCheckBoxChanged;
            return card;
        }

        private static CheckBox CreateMaterialSwitch(string accessibleName, bool isChecked)
        {
            MaterialSwitch toggle = new MaterialSwitch();
            toggle.Checked = isChecked;
            toggle.AccessibleName = accessibleName;
            toggle.Anchor = AnchorStyles.Right;
            toggle.Margin = new Padding(8, 0, 0, 0);
            return toggle;
        }

        private static Control BuildSettingRow(string titleText, CheckBox toggle)
        {
            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.BackColor = Color.Transparent;
            row.Margin = new Padding(0);
            row.ColumnCount = 2;
            row.RowCount = 1;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label title = new Label();
            title.Text = titleText;
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(31, 31, 31);
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.AutoEllipsis = true;
            title.Margin = new Padding(2, 0, 10, 0);

            row.Controls.Add(title, 0, 0);
            row.Controls.Add(toggle, 1, 0);
            return row;
        }

        private Control BuildActionBar()
        {
            TableLayoutPanel bar = new TableLayoutPanel();
            bar.Dock = DockStyle.Fill;
            bar.BackColor = Color.Transparent;
            bar.Margin = new Padding(0);
            bar.ColumnCount = 2;
            bar.RowCount = 1;
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            MaterialButton exitButton = new MaterialButton();
            exitButton.Text = "退出助手";
            exitButton.Variant = MaterialButtonVariant.Text;
            exitButton.Size = new Size(94, 42);
            exitButton.Margin = new Padding(0);
            exitButton.Click += delegate { ExitAssistant(); };

            FlowLayoutPanel leftActions = new FlowLayoutPanel();
            leftActions.AutoSize = true;
            leftActions.WrapContents = false;
            leftActions.Anchor = AnchorStyles.Left;
            leftActions.Margin = new Padding(0, 7, 0, 0);
            leftActions.Controls.Add(exitButton);
            bar.Controls.Add(leftActions, 0, 0);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.WrapContents = false;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.Anchor = AnchorStyles.Right;
            actions.Margin = new Padding(0, 7, 0, 0);

            MaterialButton reportButton = new MaterialButton();
            reportButton.Text = "扫描记录";
            reportButton.Variant = MaterialButtonVariant.Text;
            reportButton.Size = new Size(104, 42);
            reportButton.Margin = new Padding(0, 0, 6, 0);
            reportButton.Click += delegate { ShowScanReport(); };
            actions.Controls.Add(reportButton);

            hideButton = new MaterialButton();
            hideButton.Text = "隐藏到托盘";
            ((MaterialButton)hideButton).Variant = MaterialButtonVariant.Outlined;
            hideButton.Size = new Size(118, 42);
            hideButton.Margin = new Padding(0, 0, 10, 0);
            hideButton.Click += delegate { HideToTray(); };
            actions.Controls.Add(hideButton);

            startButton = new MaterialButton();
            startButton.Text = "立即检测并应用";
            ((MaterialButton)startButton).Variant = MaterialButtonVariant.Filled;
            startButton.Size = new Size(166, 42);
            startButton.Margin = new Padding(0);
            startButton.Click += async delegate { await StartAndTranslateAsync(true); };
            actions.Controls.Add(startButton);
            bar.Controls.Add(actions, 1, 0);
            return bar;
        }

        private void ActivationLoop()
        {
            while (!exiting)
            {
                activationEvent.WaitOne();
                if (exiting || IsDisposed) return;
                try { BeginInvoke(new Action(HandleExternalActivation)); }
                catch { return; }
            }
        }

        private async void HandleExternalActivation()
        {
            ShowFromTray();
            await StartAndTranslateAsync(false);
        }

        private async Task MonitorTickAsync()
        {
            if (busy || !monitorCheckBox.Checked || !IsAntigravityRunning()) return;
            string currentVersion = GetAntigravityVersion();
            DateTime packWrite = File.Exists(PackPath) ? File.GetLastWriteTimeUtc(PackPath) : DateTime.MinValue;
            bool versionChanged = !string.Equals(currentVersion, lastAntigravityVersion, StringComparison.OrdinalIgnoreCase);
            bool packChanged = packUpdateCheckBox.Checked && packWrite != lastPackWrite;
            if (!versionChanged && !packChanged && (DateTime.Now - lastInjection).TotalSeconds < 12) return;
            await StartAndTranslateAsync(false);
        }

        private async Task ToggleLocalizationAsync()
        {
            if (busy) return;
            if (statusPill != null && statusPill.IsLocalized)
                await DisableLocalizationAsync();
            else
                await StartAndTranslateAsync(true, false, true);
        }

        private async Task DisableLocalizationAsync()
        {
            if (busy) return;
            busy = true;
            startButton.Enabled = false;
            try
            {
                if (!IsAntigravityRunning())
                {
                    SetStatus("尚未汉化", "Antigravity 当前没有运行。", Color.FromArgb(75, 88, 115));
                    return;
                }

                SetStatus("正在恢复英文", "正在撤销界面汉化……", Color.FromArgb(75, 88, 115));
                int count = await ExecuteInAntigravityAsync(BuildDisableScript(), false, false);
                if (count > 0)
                {
                    lastInjection = DateTime.MinValue;
                    SetStatus("尚未汉化", "Antigravity 界面已恢复英文。", Color.FromArgb(75, 88, 115));
                    PlayToggleSound(false);
                }
                else
                {
                    SetStatus("恢复失败", "没有找到可恢复的 Antigravity 界面。", Color.Firebrick);
                }
            }
            catch (Exception ex)
            {
                SetStatus("恢复失败", FriendlyError(ex), Color.Firebrick);
            }
            finally
            {
                busy = false;
                startButton.Enabled = true;
            }
        }

        private async Task StartAndTranslateAsync(bool launchIfNeeded, bool hideAfter = false, bool playSound = false)
        {
            if (busy) return;
            busy = true;
            startButton.Enabled = false;
            try
            {
                if (!IsAntigravityRunning())
                {
                    if (!launchIfNeeded)
                    {
                        SetStatus("等待 Antigravity", "助手正在后台监控。", Color.FromArgb(90, 90, 90));
                        return;
                    }
                    string exe = FindAntigravityExecutable();
                    if (exe == null)
                    {
                        SetStatus("未找到 Antigravity", "请先安装 Google Antigravity。", Color.Firebrick);
                        Show();
                        return;
                    }
                    SetStatus("正在启动", "等待 Antigravity 界面和调试接口就绪……", Color.FromArgb(26, 90, 170));
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = exe;
                    startInfo.UseShellExecute = true;
                    Process.Start(startInfo);
                }
                else
                {
                    SetStatus("正在连接", "正在连接 Antigravity 并应用离线词典……", Color.FromArgb(26, 90, 170));
                }

                Exception lastError = null;
                for (int attempt = 0; attempt < 45; attempt++)
                {
                    try
                    {
                        int count = await InjectIntoAntigravityAsync();
                        if (count > 0)
                        {
                            lastInjection = DateTime.Now;
                            lastAntigravityVersion = GetAntigravityVersion();
                            lastPackWrite = File.Exists(PackPath) ? File.GetLastWriteTimeUtc(PackPath) : DateTime.MinValue;
                            SaveUnknownReport();
                            UpdateMetrics();
                            string detail = unknownStrings.Count == 0
                                ? "界面已扫描，当前没有发现待适配的系统词条。"
                                : "已自动处理常见新增词条；另发现 " + unknownStrings.Count + " 条待适配系统文字。";
                            SetStatus("汉化已生效", detail, Color.FromArgb(38, 125, 66));
                            if (playSound) PlayToggleSound(true);
                            if (hideAfter)
                            {
                                await Task.Delay(700);
                                HideToTray();
                            }
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                    }
                    await Task.Delay(1000);
                }
                string reason = lastError == null ? "调试页面尚未就绪。" : FriendlyError(lastError);
                SetStatus("暂未连接", reason + " 助手会继续后台重试。", Color.DarkOrange);
            }
            finally
            {
                busy = false;
                startButton.Enabled = true;
            }
        }

        private async Task<int> InjectIntoAntigravityAsync()
        {
            unknownStrings.Clear();
            return await ExecuteInAntigravityAsync(BuildTranslationScript(), true, true);
        }

        private async Task<int> ExecuteInAntigravityAsync(string script, bool persistOnNewDocument, bool collectUnknown)
        {
            string portFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Antigravity", "DevToolsActivePort");
            if (!File.Exists(portFile)) return 0;
            string[] lines = File.ReadAllLines(portFile);
            int port;
            if (lines.Length == 0 || !int.TryParse(lines[0].Trim(), out port)) return 0;

            string json = await localHttp.GetStringAsync("http://127.0.0.1:" + port + "/json/list");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            object parsed = serializer.DeserializeObject(json);
            object[] targets = parsed as object[];
            if (targets == null) return 0;
            int injected = 0;
            foreach (object item in targets)
            {
                IDictionary target = item as IDictionary;
                if (target == null) continue;
                string type = ReadString(target, "type");
                string url = ReadString(target, "url");
                string ws = ReadString(target, "webSocketDebuggerUrl");
                if (!string.Equals(type, "page", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(url)) continue;
                if (url.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) < 0 &&
                    url.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) < 0) continue;
                await InjectTargetAsync(ws, serializer, script, persistOnNewDocument, collectUnknown);
                injected++;
            }
            return injected;
        }

        private async Task InjectTargetAsync(string webSocketUrl, JavaScriptSerializer serializer, string script,
            bool persistOnNewDocument, bool collectUnknown)
        {
            using (ClientWebSocket socket = new ClientWebSocket())
            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
            {
                socket.Options.Proxy = new WebProxy();
                await socket.ConnectAsync(new Uri(webSocketUrl), timeout.Token);
                int commandId = 1;
                if (persistOnNewDocument)
                {
                    Hashtable enableParams = new Hashtable();
                    enableParams["expression"] = "localStorage.removeItem('__antigravityZhAssistantDisabled')";
                    enableParams["returnByValue"] = true;
                    await SendCommandAsync(socket, serializer, commandId, "Runtime.evaluate", enableParams, timeout.Token);
                    await WaitForResponseAsync(socket, commandId++, timeout.Token);

                    Hashtable addParams = new Hashtable();
                    addParams["source"] = script;
                    await SendCommandAsync(socket, serializer, commandId, "Page.addScriptToEvaluateOnNewDocument", addParams, timeout.Token);
                    await WaitForResponseAsync(socket, commandId++, timeout.Token);
                }

                Hashtable evaluateParams = new Hashtable();
                evaluateParams["expression"] = script;
                evaluateParams["returnByValue"] = true;
                evaluateParams["awaitPromise"] = false;
                await SendCommandAsync(socket, serializer, commandId, "Runtime.evaluate", evaluateParams, timeout.Token);
                await WaitForResponseAsync(socket, commandId++, timeout.Token);

                if (collectUnknown)
                {
                    Hashtable scanParams = new Hashtable();
                    scanParams["expression"] = "window.__antigravityZhAssistant && window.__antigravityZhAssistant.collectUnknown ? window.__antigravityZhAssistant.collectUnknown() : []";
                    scanParams["returnByValue"] = true;
                    await SendCommandAsync(socket, serializer, commandId, "Runtime.evaluate", scanParams, timeout.Token);
                    IDictionary scanResponse = await WaitForResponseAsync(socket, commandId, timeout.Token);
                    AddUnknownFromResponse(scanResponse);
                }
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None); }
                catch { }
            }
        }

        private static async Task SendCommandAsync(ClientWebSocket socket, JavaScriptSerializer serializer,
            int id, string method, Hashtable parameters, CancellationToken token)
        {
            Hashtable command = new Hashtable();
            command["id"] = id;
            command["method"] = method;
            command["params"] = parameters;
            byte[] bytes = Encoding.UTF8.GetBytes(serializer.Serialize(command));
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }

        private static async Task<IDictionary> WaitForResponseAsync(ClientWebSocket socket, int expectedId, CancellationToken token)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            byte[] buffer = new byte[32768];
            while (socket.State == WebSocketState.Open)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close) return null;
                        stream.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);
                    string message = Encoding.UTF8.GetString(stream.ToArray());
                    IDictionary response = serializer.DeserializeObject(message) as IDictionary;
                    if (response != null && response.Contains("id") && Convert.ToInt32(response["id"]) == expectedId) return response;
                }
            }
            return null;
        }

        private void AddUnknownFromResponse(IDictionary response)
        {
            if (response == null || !response.Contains("result")) return;
            IDictionary outer = response["result"] as IDictionary;
            if (outer == null || !outer.Contains("result")) return;
            IDictionary inner = outer["result"] as IDictionary;
            if (inner == null || !inner.Contains("value")) return;
            object[] values = inner["value"] as object[];
            if (values == null) return;
            foreach (object value in values)
            {
                string text = Convert.ToString(value);
                if (!string.IsNullOrWhiteSpace(text)) unknownStrings.Add(text.Trim());
            }
        }

        private static string ReadString(IDictionary dictionary, string key)
        {
            return dictionary.Contains(key) && dictionary[key] != null ? Convert.ToString(dictionary[key]) : null;
        }

        private string BuildTranslationScript()
        {
            Dictionary<string, string> translations = new Dictionary<string, string>(StringComparer.Ordinal);
            if (packUpdateCheckBox.Checked && File.Exists(PackPath))
            {
                try
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    Dictionary<string, string> loaded = serializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(PackPath, Encoding.UTF8));
                    if (loaded != null)
                    {
                        foreach (KeyValuePair<string, string> item in loaded)
                        {
                            if (!string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                                translations[item.Key.Trim()] = item.Value.Trim();
                        }
                    }
                }
                catch { }
            }
            JavaScriptSerializer json = new JavaScriptSerializer();
            return translationScriptTemplate
                .Replace("__AUTO_ADAPT__", adaptCheckBox.Checked ? "true" : "false")
                .Replace("__EXTRA_TRANSLATIONS__", json.Serialize(translations));
        }

        private static string BuildDisableScript()
        {
            return "(() => { " +
                "localStorage.setItem('__antigravityZhAssistantDisabled', '1'); " +
                "const assistant = window.__antigravityZhAssistant; " +
                "if (assistant && typeof assistant.restore === 'function') return assistant.restore(); " +
                "try { if (assistant && assistant.observer) assistant.observer.disconnect(); } catch (_) {} " +
                "delete window.__antigravityZhAssistant; " +
                "return { ok: true, restored: false }; " +
                "})();";
        }

        private static void PlayToggleSound(bool enabled)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    const int sampleRate = 22050;
                    const int noteSamples = 1544;
                    const int gapSamples = 220;
                    int totalSamples = noteSamples * 2 + gapSamples;
                    using (MemoryStream stream = new MemoryStream())
                    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                    {
                        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                        writer.Write(36 + totalSamples * 2);
                        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                        writer.Write(16);
                        writer.Write((short)1);
                        writer.Write((short)1);
                        writer.Write(sampleRate);
                        writer.Write(sampleRate * 2);
                        writer.Write((short)2);
                        writer.Write((short)16);
                        writer.Write(Encoding.ASCII.GetBytes("data"));
                        writer.Write(totalSamples * 2);

                        double first = enabled ? 660.0 : 880.0;
                        double second = enabled ? 880.0 : 660.0;
                        WriteSoftTone(writer, first, noteSamples, sampleRate);
                        for (int i = 0; i < gapSamples; i++) writer.Write((short)0);
                        WriteSoftTone(writer, second, noteSamples, sampleRate);
                        writer.Flush();
                        stream.Position = 0;
                        using (SoundPlayer player = new SoundPlayer(stream)) player.PlaySync();
                    }
                }
                catch { }
            });
        }

        private static void WriteSoftTone(BinaryWriter writer, double frequency, int samples, int sampleRate)
        {
            int fadeSamples = Math.Max(1, sampleRate / 100);
            for (int i = 0; i < samples; i++)
            {
                double envelope = 1.0;
                if (i < fadeSamples) envelope = (double)i / fadeSamples;
                else if (i > samples - fadeSamples) envelope = (double)(samples - i) / fadeSamples;
                double value = Math.Sin(2.0 * Math.PI * frequency * i / sampleRate) * envelope;
                writer.Write((short)(value * 3600));
            }
        }

        private async Task TryUpdateTranslationPackAsync()
        {
            try
            {
                string manifestJson = await updateHttp.GetStringAsync(PackManifestUrl);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                IDictionary manifest = serializer.DeserializeObject(manifestJson) as IDictionary;
                if (manifest == null) return;
                string remoteVersion = ReadString(manifest, "version");
                string packUrl = ReadString(manifest, "packUrl");
                if (string.IsNullOrWhiteSpace(remoteVersion) || string.IsNullOrWhiteSpace(packUrl)) return;
                if (string.Equals(GetPackVersion(), remoteVersion, StringComparison.OrdinalIgnoreCase) && File.Exists(PackPath)) return;

                string packJson = await updateHttp.GetStringAsync(packUrl);
                Dictionary<string, string> translations = serializer.Deserialize<Dictionary<string, string>>(packJson);
                if (translations == null || translations.Count == 0) return;
                foreach (KeyValuePair<string, string> item in translations)
                {
                    if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value)) return;
                }

                Directory.CreateDirectory(DataDirectory);
                string temporaryPath = PackPath + ".download";
                File.WriteAllText(temporaryPath, packJson, new UTF8Encoding(false));
                if (File.Exists(PackPath)) File.Delete(PackPath);
                File.Move(temporaryPath, PackPath);
                SetPackVersion(remoteVersion);
                lastPackWrite = File.GetLastWriteTimeUtc(PackPath);
            }
            catch
            {
                // Offline or an unavailable update server must never prevent local localization.
            }
        }

        private static string GetPackVersion()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\AntigravityZhAssistant"))
                {
                    return key == null ? string.Empty : Convert.ToString(key.GetValue("TranslationPackVersion", string.Empty));
                }
            }
            catch { return string.Empty; }
        }

        private static void SetPackVersion(string version)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\AntigravityZhAssistant"))
                    key.SetValue("TranslationPackVersion", version, RegistryValueKind.String);
            }
            catch { }
        }

        private void SaveUnknownReport()
        {
            try
            {
                ArrayList values = new ArrayList();
                foreach (string value in unknownStrings) values.Add(value);
                Hashtable report = new Hashtable();
                report["assistantVersion"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                report["antigravityVersion"] = lastAntigravityVersion;
                report["scannedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                report["count"] = values.Count;
                report["strings"] = values;
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                File.WriteAllText(ReportPath, serializer.Serialize(report), Encoding.UTF8);
            }
            catch { }
        }

        private void UpdateMetrics()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateMetrics));
                return;
            }
            versionLabel.Text = lastAntigravityVersion;
            unknownLabel.Text = unknownStrings.Count.ToString();
            scanLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            if (infoLabel != null) infoLabel.Text = BuildInfoText();
        }

        private static string GetAntigravityVersion()
        {
            try
            {
                string path = FindAntigravityExecutable();
                if (path == null) return "未安装";
                string version = FileVersionInfo.GetVersionInfo(path).FileVersion;
                return string.IsNullOrWhiteSpace(version) ? "未知" : version;
            }
            catch { return "未知"; }
        }

        private static Label CreateMetricLabel(Control parent, string text, int left)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Color.FromArgb(68, 71, 70);
            label.Font = new Font("Microsoft YaHei UI", 8.5F);
            label.Location = new Point(left, 84);
            label.Size = new Size(240, 24);
            parent.Controls.Add(label);
            return label;
        }

        private static CheckBox CreateSettingCheckBox(string text, int left, int top, bool value)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Location = new Point(left, top);
            box.Size = new Size(300, 28);
            box.Checked = value;
            box.ForeColor = Color.FromArgb(31, 31, 31);
            box.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            return box;
        }

        private static Label CreateSettingDescription(string text, int left, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(left, top);
            label.Size = new Size(380, 24);
            label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label.ForeColor = Color.FromArgb(95, 99, 104);
            label.AutoEllipsis = true;
            return label;
        }

        private static void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(116, 119, 117);
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(11, 87, 208);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(237, 243, 252);
            ApplyRoundedRegion(button, 23);
        }

        private static void StyleTextButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.FromArgb(248, 250, 253);
            button.ForeColor = Color.FromArgb(11, 87, 208);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 240, 254);
            ApplyRoundedRegion(button, 23);
        }

        private static void ApplyRoundedRegion(Control control, int radius)
        {
            Action update = delegate
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                Rectangle bounds = new Rectangle(0, 0, control.Width, control.Height);
                using (GraphicsPath path = CreateRoundedPath(bounds, radius))
                {
                    Region previous = control.Region;
                    control.Region = new Region(path);
                    if (previous != null) previous.Dispose();
                }
            };
            control.Resize += delegate { update(); };
            update();
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            GraphicsPath path = new GraphicsPath();
            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void AutomationSettingChanged(object sender, EventArgs e)
        {
            if (!initialized) return;
            SetSetting("AutoMonitor", monitorCheckBox.Checked);
            SetSetting("AutoAdapt", adaptCheckBox.Checked);
            SetSetting("AutoPack", packUpdateCheckBox.Checked);
            lastInjection = DateTime.MinValue;
        }

        private static bool GetSetting(string name, bool defaultValue)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\AntigravityZhAssistant"))
                {
                    if (key == null) return defaultValue;
                    object value = key.GetValue(name);
                    return value == null ? defaultValue : Convert.ToInt32(value) != 0;
                }
            }
            catch { return defaultValue; }
        }

        private static void SetSetting(string name, bool value)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\AntigravityZhAssistant"))
                    key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
            }
            catch { }
        }

        private void ShowScanReport()
        {
            try
            {
                if (!File.Exists(ReportPath))
                {
                    MessageBox.Show("还没有扫描记录，请先点击“立即检测并应用”。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                ProcessStartInfo info = new ProcessStartInfo("explorer.exe", "/select,\"" + ReportPath + "\"");
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开扫描记录：" + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetStatus(string title, string detail, Color color)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, Color>(SetStatus), title, detail, color);
                return;
            }
            statusLabel.Text = title;
            statusLabel.ForeColor = color;
            detailLabel.Text = detail;
            if (statusPill != null)
            {
                statusPill.IsLocalized = string.Equals(title, "汉化已生效", StringComparison.Ordinal);
                statusPill.Text = title;
            }
            trayIcon.Text = title.Length > 50 ? AppName : AppName + " - " + title;
        }

        private void StartupCheckBoxChanged(object sender, EventArgs e)
        {
            if (!initialized) return;
            try
            {
                SetAutoStart(startupCheckBox.Checked);
                trayStartupItem.CheckedChanged -= TrayStartupChanged;
                trayStartupItem.Checked = startupCheckBox.Checked;
                trayStartupItem.CheckedChanged += TrayStartupChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法修改开机启动设置：" + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TrayStartupChanged(object sender, EventArgs e)
        {
            if (!initialized) return;
            startupCheckBox.Checked = trayStartupItem.Checked;
        }

        private static bool IsAutoStartEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
            {
                return key != null && key.GetValue(RunValueName) != null;
            }
        }

        private static void SetAutoStart(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
            {
                if (enabled)
                    key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\" --startup", RegistryValueKind.String);
                else
                    key.DeleteValue(RunValueName, false);
            }
        }

        private static bool IsAntigravityRunning()
        {
            Process[] processes = Process.GetProcessesByName("Antigravity");
            try { return processes.Length > 0; }
            finally
            {
                foreach (Process process in processes) process.Dispose();
            }
        }

        private static string FindAntigravityExecutable()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Antigravity", "Antigravity.exe");
            return File.Exists(path) ? path : null;
        }

        private static Icon LoadAssistantIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AssistantIcon"))
            {
                if (stream == null) return SystemIcons.Application;
                using (Icon source = new Icon(stream)) return (Icon)source.Clone();
            }
        }

        private static string LoadEmbeddedText(string resourceName)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new InvalidOperationException("缺少内置汉化词典。");
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) return reader.ReadToEnd();
            }
        }

        private static Image LoadEmbeddedImage(string resourceName)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new InvalidOperationException("缺少内置界面资源：" + resourceName);
                using (Image source = Image.FromStream(stream)) return new Bitmap(source);
            }
        }

        private static string FriendlyError(Exception ex)
        {
            Exception current = ex;
            while (current.InnerException != null) current = current.InnerException;
            if (current is OperationCanceledException) return "连接等待超时。";
            return current.Message;
        }

        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
        }

        private void ShowFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitAssistant()
        {
            exiting = true;
            activationEvent.Set();
            trayIcon.Visible = false;
            Close();
        }

        private void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (exiting) return;
            e.Cancel = true;
            HideToTray();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                monitorTimer.Dispose();
                trayIcon.Dispose();
                localHttp.Dispose();
                updateHttp.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class StatusPillButton : Button
    {
        private bool hovering;
        private bool isLocalized;

        public bool IsLocalized
        {
            get { return isLocalized; }
            set
            {
                if (isLocalized == value) return;
                isLocalized = value;
                Invalidate();
            }
        }

        public StatusPillButton()
        {
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int radius = (int)Math.Round(8 * e.Graphics.DpiX / 96F);
            Rectangle shadowBounds = new Rectangle(1, 2, Width - 2, Height - 3);
            using (GraphicsPath shadow = RoundedRectangle(shadowBounds, radius))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(24, 0, 0, 0)))
                e.Graphics.FillPath(shadowBrush, shadow);

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 3);
            using (GraphicsPath path = RoundedRectangle(bounds, radius))
            {
                Color fillColor = IsLocalized
                    ? (hovering && Enabled ? Color.FromArgb(124, 48, 221) : Color.FromArgb(136, 57, 239))
                    : (hovering && Enabled ? Color.FromArgb(222, 224, 230) : Color.FromArgb(230, 232, 236));
                using (SolidBrush fill = new SolidBrush(fillColor)) e.Graphics.FillPath(fill, path);
            }

            Color textColor = IsLocalized ? Color.FromArgb(234, 236, 240) : Color.FromArgb(76, 79, 105);
            if (IsLocalized)
            {
                float scale = e.Graphics.DpiX / 96F;
                using (Font iconFont = new Font("Segoe Fluent Icons", 16F * scale, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    Size textSize = TextRenderer.MeasureText(Text, Font, Size.Empty,
                        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                    int iconSize = (int)Math.Round(16 * scale);
                    int gap = (int)Math.Round(8 * scale);
                    int totalWidth = iconSize + gap + textSize.Width;
                    int left = (Width - totalWidth) / 2;
                    Rectangle iconBounds = new Rectangle(left, 0, iconSize, bounds.Height);
                    Rectangle textBounds = new Rectangle(left + iconSize + gap, 0, textSize.Width, bounds.Height);
                    TextRenderer.DrawText(e.Graphics, "\uE73E", iconFont, iconBounds, textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, textColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, Text, Font, bounds, textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width <= 0 || Height <= 0) return;
            float scale;
            using (Graphics graphics = CreateGraphics()) scale = graphics.DpiX / 96F;
            using (GraphicsPath path = RoundedRectangle(new Rectangle(0, 0, Width, Height), (int)Math.Round(8 * scale)))
            {
                Region oldRegion = Region;
                Region = new Region(path);
                if (oldRegion != null) oldRegion.Dispose();
            }
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class OnboardingInfoButton : Button
    {
        public OnboardingInfoButton()
        {
            TabStop = false;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(76, 79, 105);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int radius = (int)Math.Round(8 * e.Graphics.DpiX / 96F);
            Rectangle shadowBounds = new Rectangle(1, 2, Width - 2, Height - 3);
            using (GraphicsPath shadow = RoundedRectangle(shadowBounds, radius))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(22, 0, 0, 0)))
                e.Graphics.FillPath(shadowBrush, shadow);

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 3);
            using (GraphicsPath path = RoundedRectangle(bounds, radius))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(230, 232, 236)))
            using (Pen border = new Pen(Color.FromArgb(213, 215, 220)))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, bounds, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class OnboardingFooterButton : Control
    {
        public bool Filled { get; set; }

        public OnboardingFooterButton()
        {
            TabStop = false;
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            ForeColor = Color.FromArgb(194, 197, 207);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int radius = (int)Math.Round(8 * e.Graphics.DpiX / 96F);
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            if (Filled)
            {
                using (GraphicsPath path = RoundedRectangle(bounds, radius))
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(226, 228, 233)))
                    e.Graphics.FillPath(fill, path);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, bounds, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class SoftCheckBox : CheckBox
    {
        public SoftCheckBox()
        {
            AutoSize = false;
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Pixel);
            ForeColor = Color.FromArgb(150, 154, 172);
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = e.Graphics.DpiX / 96F;
            int boxSize = (int)Math.Round(14 * scale);
            Rectangle box = new Rectangle(0, (Height - boxSize) / 2, boxSize, boxSize);
            using (GraphicsPath path = RoundedRectangle(box, (int)Math.Round(4 * scale)))
            using (SolidBrush fill = new SolidBrush(Checked ? Color.FromArgb(136, 57, 239) : Color.FromArgb(230, 232, 236)))
            using (Pen border = new Pen(Checked ? Color.FromArgb(136, 57, 239) : Color.FromArgb(203, 205, 212)))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }

            if (Checked)
            {
                using (Pen check = new Pen(Color.White, 1.5F * scale))
                {
                    check.StartCap = LineCap.Round;
                    check.EndCap = LineCap.Round;
                    e.Graphics.DrawLines(check, new[]
                    {
                        new Point(box.Left + (int)Math.Round(3 * scale), box.Top + (int)Math.Round(7 * scale)),
                        new Point(box.Left + (int)Math.Round(6 * scale), box.Top + (int)Math.Round(10 * scale)),
                        new Point(box.Left + (int)Math.Round(11 * scale), box.Top + (int)Math.Round(4 * scale))
                    });
                }
            }

            int textGap = (int)Math.Round(6 * scale);
            Rectangle textBounds = new Rectangle(box.Right + textGap, 0, Width - box.Right - textGap, Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class WindowGlyphButton : Button
    {
        private bool hovering;
        public bool IsCloseButton { get; set; }

        public WindowGlyphButton(string glyph)
        {
            Text = glyph;
            Font = new Font("Segoe Fluent Icons", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            ForeColor = Color.FromArgb(76, 79, 105);
            BackColor = Color.Transparent;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (hovering)
            {
                Color hover = IsCloseButton ? Color.FromArgb(196, 43, 28) : Color.FromArgb(55, 255, 255, 255);
                Rectangle hoverBounds = new Rectangle(4, 4, Width - 8, Height - 8);
                using (GraphicsPath hoverPath = RoundedRectangle(hoverBounds, 8))
                using (SolidBrush brush = new SolidBrush(hover))
                    e.Graphics.FillPath(brush, hoverPath);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
                hovering && IsCloseButton ? Color.White : ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal enum MaterialButtonVariant
    {
        Filled,
        Outlined,
        Text
    }

    internal sealed class MaterialButton : Button
    {
        private bool hovering;
        private bool pressed;
        private MaterialButtonVariant variant;

        public MaterialButtonVariant Variant
        {
            get { return variant; }
            set { variant = value; Invalidate(); }
        }

        public MaterialButton()
        {
            variant = MaterialButtonVariant.Filled;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            MinimumSize = new Size(1, 42);
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovering = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.FromArgb(248, 250, 253));
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedRectangle(bounds, Height / 2))
            {
                Color fill;
                Color text;
                Color border = Color.Transparent;

                if (!Enabled)
                {
                    fill = variant == MaterialButtonVariant.Text ? Color.Transparent : Color.FromArgb(232, 234, 237);
                    text = Color.FromArgb(154, 160, 166);
                    border = variant == MaterialButtonVariant.Outlined ? Color.FromArgb(218, 220, 224) : Color.Transparent;
                }
                else if (variant == MaterialButtonVariant.Filled)
                {
                    fill = pressed ? Color.FromArgb(6, 64, 158) : hovering ? Color.FromArgb(8, 76, 181) : Color.FromArgb(11, 87, 208);
                    text = Color.White;
                }
                else if (variant == MaterialButtonVariant.Outlined)
                {
                    fill = pressed ? Color.FromArgb(220, 232, 250) : hovering ? Color.FromArgb(237, 243, 252) : Color.White;
                    text = Color.FromArgb(11, 87, 208);
                    border = Color.FromArgb(116, 119, 117);
                }
                else
                {
                    fill = pressed ? Color.FromArgb(220, 232, 250) : hovering ? Color.FromArgb(232, 240, 254) : Color.Transparent;
                    text = Color.FromArgb(11, 87, 208);
                }

                using (SolidBrush fillBrush = new SolidBrush(fill)) e.Graphics.FillPath(fillBrush, path);
                if (border.A > 0)
                {
                    using (Pen borderPen = new Pen(border)) e.Graphics.DrawPath(borderPen, path);
                }
                TextRenderer.DrawText(e.Graphics, Text, Font, bounds, text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                if (Focused && ShowFocusCues)
                {
                    Rectangle focus = Rectangle.Inflate(bounds, -5, -5);
                    ControlPaint.DrawFocusRectangle(e.Graphics, focus, text, fill);
                }
            }
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            GraphicsPath path = new GraphicsPath();
            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class MaterialSwitch : CheckBox
    {
        public MaterialSwitch()
        {
            AutoSize = false;
            Size = new Size(46, 26);
            MinimumSize = new Size(46, 26);
            Text = string.Empty;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            return new Size(46, 26);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.White);
            if (Width < 8 || Height < 8) return;
            int trackHeight = Math.Min(22, Height - 4);
            Rectangle track = new Rectangle(1, (Height - trackHeight) / 2, Width - 3, trackHeight);
            using (GraphicsPath path = RoundedRectangle(track, track.Height / 2))
            {
                if (Checked)
                {
                    using (SolidBrush brush = new SolidBrush(Enabled ? Color.FromArgb(11, 87, 208) : Color.FromArgb(174, 203, 242)))
                        e.Graphics.FillPath(brush, path);
                }
                else
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(231, 234, 237))) e.Graphics.FillPath(brush, path);
                    using (Pen pen = new Pen(Enabled ? Color.FromArgb(116, 119, 117) : Color.FromArgb(189, 193, 198)))
                        e.Graphics.DrawPath(pen, path);
                }
            }

            int thumbSize = 18;
            int thumbX = Checked ? Width - thumbSize - 4 : 4;
            Rectangle thumb = new Rectangle(thumbX, (Height - thumbSize) / 2, thumbSize, thumbSize);
            using (SolidBrush brush = new SolidBrush(Checked ? Color.White : Color.FromArgb(95, 99, 104)))
                e.Graphics.FillEllipse(brush, thumb);
            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
            GraphicsPath path = new GraphicsPath();
            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class GradientPanel : Panel
    {
        public Color Color1 { get; set; }
        public Color Color2 { get; set; }

        public GradientPanel()
        {
            DoubleBuffered = true;
            Color1 = Color.RoyalBlue;
            Color2 = Color.MediumPurple;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, Color1, Color2, 15F))
                e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }

    internal sealed class CardPanel : Panel
    {
        public Color BorderColor { get; set; }
        public int CornerRadius { get; set; }

        public CardPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            BorderColor = Color.FromArgb(218, 220, 224);
            CornerRadius = 16;
            Padding = new Padding(1);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (Width <= 2 || Height <= 2) return;
            using (GraphicsPath path = RoundedRectangle(new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region previous = Region;
                Region = new Region(path);
                if (previous != null) previous.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width <= 2 || Height <= 2) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedRectangle(rect, CornerRadius))
            using (Pen pen = new Pen(BorderColor))
                e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            GraphicsPath path = new GraphicsPath();
            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
