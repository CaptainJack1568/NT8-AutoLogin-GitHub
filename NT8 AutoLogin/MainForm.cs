using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NT8AutoLogin
{
    /// <summary>Keeps the standard system title bar and arranges the client area into header, actions, log card, and path bar.</summary>
    internal class MainForm : Form
    {
        private static readonly Color WindowBg = Color.FromArgb(247, 249, 251);
        private static readonly Color DividerColor = Color.FromArgb(224, 229, 235);
        private static readonly Color ButtonBg = Color.FromArgb(255, 255, 255);
        private static readonly Color ButtonHoverBg = Color.FromArgb(245, 247, 250);
        private static readonly Color ButtonPressBg = Color.FromArgb(237, 241, 245);
        private static readonly Color ButtonBorder = Color.FromArgb(204, 212, 220);
        private static readonly Color ButtonText = Color.FromArgb(37, 49, 63);
        private static readonly Color BandText = Color.FromArgb(48, 60, 74);
        private static readonly Color LogPaper = Color.White;
        private static readonly Color CardEdge = Color.FromArgb(210, 218, 226);

        private readonly TextBox _log = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = LogPaper,
            ForeColor = Color.FromArgb(28, 32, 34),
            Font = new Font("Microsoft YaHei UI", 9f),
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
        };

        private readonly LinkLabel _discordLink = new LinkLabel
        {
            Text = "Discord community: https://discord.gg/sVg76WNZdT",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            LinkBehavior = LinkBehavior.HoverUnderline,
            ActiveLinkColor = Color.FromArgb(33, 99, 235),
            LinkColor = Color.FromArgb(33, 99, 235),
            VisitedLinkColor = Color.FromArgb(33, 99, 235),
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };

        private readonly Button _btnClean = new Button { Text = "Clear Data" };
        private readonly Button _btnLaunch = new Button { Text = "Priority Launch" };
        private readonly Button _btnReset = new Button { Text = "Reset" };
        private readonly Button _btnBrowsePath = new Button { Text = "Browse..." };
        private readonly Button _btnDefaultPath = new Button { Text = "Default Path" };

        private readonly TextBox _ninjaPath = new TextBox
        {
            Text = Program.LoadPreferredNinjaExePathOrDefault(),
            Font = new Font("Microsoft YaHei UI", 9f),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
        };

        private bool _busy;

        public MainForm()
        {
            Text = "NT8 AutoLogin V1.0";
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            DoubleBuffered = true;
            BackColor = WindowBg;
            Padding = new Padding(0);
            Font = new Font("Microsoft YaHei UI", 9f);

            const int topBarHeight = 34;
            const int buttonColumnWidth = 206;
            const int sectionInset = 14;
            const int contentColumnGap = sectionInset;
            const int buttonHeight = 40;
            const int buttonGap = 10;
            const int bottomBarTopPadding = 6;
            const int bottomBarBottomPadding = 11;
            const int pathRowGap = 2;

            StyleActionButton(_btnClean);
            StyleActionButton(_btnLaunch);
            StyleActionButton(_btnReset);
            StyleActionButton(_btnBrowsePath);
            StyleActionButton(_btnDefaultPath);

            _ninjaPath.Multiline = false;
            _ninjaPath.WordWrap = false;
            _ninjaPath.ScrollBars = ScrollBars.Horizontal;
            _ninjaPath.Dock = DockStyle.Fill;
            _discordLink.LinkClicked += (_, __) => OpenLink("https://discord.gg/sVg76WNZdT");

            var topBarLabel = new Label
            {
                Text = "NinjaTrader 8  ·  Clear Local Data  ·  Priority Launch & Auto Sign-In",
                ForeColor = BandText,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
            };

            var topBar = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = WindowBg,
                Padding = new Padding(sectionInset, 0, sectionInset, 0),
                Margin = Padding.Empty,
            };
            topBar.Controls.Add(topBarLabel);

            var logCard = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = CardEdge,
                Padding = new Padding(1),
            };
            var logInner = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = LogPaper,
                Padding = new Padding(8, 8, 8, 8),
            };
            var logLayout = new BufferedTableLayout
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = LogPaper,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };
            logLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1f));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            logLayout.Controls.Add(_discordLink, 0, 0);
            logLayout.Controls.Add(new BufferedPanel { Dock = DockStyle.Fill, BackColor = DividerColor, Margin = Padding.Empty }, 0, 1);
            logLayout.Controls.Add(_log, 0, 2);
            logInner.Controls.Add(logLayout);
            logCard.Controls.Add(logInner);

            var btnStack = new BufferedTableLayout
            {
                Dock = DockStyle.Fill,
                BackColor = WindowBg,
                ColumnCount = 1,
                RowCount = 5,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
            };
            btnStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            btnStack.RowStyles.Add(new RowStyle(SizeType.Absolute, buttonHeight));
            btnStack.RowStyles.Add(new RowStyle(SizeType.Absolute, buttonGap));
            btnStack.RowStyles.Add(new RowStyle(SizeType.Absolute, buttonHeight));
            btnStack.RowStyles.Add(new RowStyle(SizeType.Absolute, buttonGap));
            btnStack.RowStyles.Add(new RowStyle(SizeType.Absolute, buttonHeight));

            _btnClean.Dock = DockStyle.Fill;
            _btnLaunch.Dock = DockStyle.Fill;
            _btnReset.Dock = DockStyle.Fill;
            _btnClean.Margin = Padding.Empty;
            _btnLaunch.Margin = Padding.Empty;
            _btnReset.Margin = Padding.Empty;
            btnStack.Controls.Add(_btnClean, 0, 0);
            btnStack.Controls.Add(new BufferedPanel { Dock = DockStyle.Fill, BackColor = WindowBg, Margin = Padding.Empty }, 0, 1);
            btnStack.Controls.Add(_btnLaunch, 0, 2);
            btnStack.Controls.Add(new BufferedPanel { Dock = DockStyle.Fill, BackColor = WindowBg, Margin = Padding.Empty }, 0, 3);
            btnStack.Controls.Add(_btnReset, 0, 4);

            var contentTable = new BufferedTableLayout
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = WindowBg,
                Margin = Padding.Empty,
                Padding = new Padding(sectionInset),
            };
            contentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, buttonColumnWidth));
            contentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, contentColumnGap));
            contentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            contentTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            contentTable.Controls.Add(btnStack, 0, 0);
            contentTable.Controls.Add(new BufferedPanel { BackColor = WindowBg, Dock = DockStyle.Fill, Margin = Padding.Empty }, 1, 0);
            contentTable.Controls.Add(logCard, 2, 0);

            var pathLabel = new Label
            {
                Text = "Main Executable Path (NinjaTrader.exe)",
                ForeColor = BandText,
                Font = new Font("Microsoft YaHei UI", 9f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            int pathLabelHeight = Math.Max(18, pathLabel.PreferredHeight);
            int pathBoxHeight = Math.Max(26, _ninjaPath.PreferredHeight + 2);

            var pathBlock = new BufferedTableLayout
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                BackColor = WindowBg,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
            };
            pathBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pathBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82f));
            pathBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82f));
            pathBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, pathLabelHeight));
            pathBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, pathRowGap));
            pathBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, pathBoxHeight));
            pathBlock.Controls.Add(pathLabel, 0, 0);
            pathBlock.SetColumnSpan(pathLabel, 3);
            pathBlock.Controls.Add(new BufferedPanel { Dock = DockStyle.Fill, BackColor = WindowBg, Margin = Padding.Empty }, 0, 1);
            pathBlock.SetColumnSpan(pathBlock.GetControlFromPosition(0, 1), 3);
            pathBlock.Controls.Add(_ninjaPath, 0, 2);
            pathBlock.Controls.Add(_btnBrowsePath, 1, 2);
            pathBlock.Controls.Add(_btnDefaultPath, 2, 2);

            _btnBrowsePath.Dock = DockStyle.Fill;
            _btnDefaultPath.Dock = DockStyle.Fill;
            _btnBrowsePath.Margin = new Padding(8, 0, 0, 0);
            _btnDefaultPath.Margin = new Padding(8, 0, 0, 0);

            var bottomBar = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = WindowBg,
                Padding = new Padding(sectionInset, bottomBarTopPadding, sectionInset, bottomBarBottomPadding),
                Margin = Padding.Empty,
            };
            bottomBar.Controls.Add(pathBlock);

            int contentHeight = buttonHeight * 3 + buttonGap * 2 + sectionInset * 2;
            int bottomBarHeight = pathLabelHeight + pathRowGap + pathBoxHeight + bottomBarTopPadding + bottomBarBottomPadding;

            var root = new BufferedTableLayout
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = WindowBg,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, topBarHeight));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, contentHeight));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, bottomBarHeight));
            root.Controls.Add(topBar, 0, 0);
            root.Controls.Add(new BufferedPanel { Dock = DockStyle.Fill, BackColor = DividerColor, Margin = Padding.Empty }, 0, 1);
            root.Controls.Add(contentTable, 0, 2);
            root.Controls.Add(new BufferedPanel { Dock = DockStyle.Fill, BackColor = DividerColor, Margin = Padding.Empty }, 0, 3);
            root.Controls.Add(bottomBar, 0, 4);

            Controls.Add(root);
            ClientSize = new Size(580, topBarHeight + contentHeight + bottomBarHeight + 2);

            _btnClean.Click += async (_, __) => await RunCleanAsync();
            _btnLaunch.Click += (_, __) => RunLaunchAsync();
            _btnReset.Click += (_, __) => OnResetCredentials();
            _btnBrowsePath.Click += (_, __) => OnBrowsePath();
            _btnDefaultPath.Click += (_, __) => OnUseDefaultPath();

            AppendLog("--------- Started ---------");
            AppendLog("Ready. Click 'Clear Data' or 'Priority Launch'. Use 'Reset' to switch accounts.");
            LogSavedPathStatus();
        }

        private static void StyleActionButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = ButtonBorder;
            b.BackColor = ButtonBg;
            b.ForeColor = ButtonText;
            b.Font = new Font("Microsoft YaHei UI", 9.75f, FontStyle.Regular);
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.UseCompatibleTextRendering = true;
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
            b.AutoSize = false;
            b.FlatAppearance.MouseOverBackColor = ButtonHoverBg;
            b.FlatAppearance.MouseDownBackColor = ButtonPressBg;
        }

        private void OnResetCredentials()
        {
            if (_busy) return;
            try
            {
                Program.ClearSavedCredentials();
                AppendLog("Saved local sign-in credentials were cleared. The next Priority Launch will prompt again.");
            }
            catch (Exception ex)
            {
                AppendLog("Reset failed: " + ex.Message);
                MessageBox.Show(this, "Unable to delete the credential file:\n" + ex.Message, "Reset", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AppendLog(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(line)));
                return;
            }
            _log.AppendText(line + Environment.NewLine);
        }

        private void OpenLink(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                AppendLog("Unable to open link: " + ex.Message);
            }
        }

        private void LogSavedPathStatus()
        {
            string savedPath;
            if (!Program.TryLoadPreferredNinjaExePath(out savedPath))
                return;

            if (File.Exists(savedPath))
                AppendLog("Loaded the last successful executable path.");
            else
                AppendLog("Note: the previously saved executable path is no longer valid. Please browse again or switch back to the default path.");
        }

        private void OnBrowsePath()
        {
            if (_busy) return;

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select NinjaTrader.exe";
                dlg.Filter = "NinjaTrader.exe|NinjaTrader.exe|Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                dlg.CheckFileExists = true;
                dlg.Multiselect = false;
                dlg.FileName = "NinjaTrader.exe";

                string currentPath = _ninjaPath.Text.Trim();
                if (File.Exists(currentPath))
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(currentPath);
                }

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                _ninjaPath.Text = dlg.FileName;
                AppendLog("Selected executable path: " + dlg.FileName);
            }
        }

        private void OnUseDefaultPath()
        {
            if (_busy) return;
            _ninjaPath.Text = Program.DefaultNinjaExePath;
            AppendLog("Restored the default executable path.");
        }

        private void SetBusy(bool busy)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetBusy(busy)));
                return;
            }
            _busy = busy;
            _btnClean.Enabled = !busy;
            _btnLaunch.Enabled = !busy;
            _btnReset.Enabled = !busy;
            _btnBrowsePath.Enabled = !busy;
            _btnDefaultPath.Enabled = !busy;
            _ninjaPath.Enabled = !busy;
        }

        private async Task RunCleanAsync()
        {
            if (_busy) return;
            SetBusy(true);
            try
            {
                AppendLog("Cleaning NinjaTrader 8 data directories...");
                await Task.Run(() => Program.RunNinjaClean(AppendLog));
                AppendLog("Cleanup complete.");
            }
            catch (Exception ex)
            {
                AppendLog("Operation failed: " + ex.Message);
                MessageBox.Show(this, "An error occurred while cleaning data:\n" + ex.Message, "Clear Data Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void RunLaunchAsync()
        {
            if (_busy) return;
            string exe = _ninjaPath.Text.Trim();
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            {
                MessageBox.Show(this, "Please provide a valid NinjaTrader main executable path (NinjaTrader.exe).", "Priority Launch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (Program.IsNinjaTraderRunning())
            {
                AppendLog("NinjaTrader is already running. Close it before using 'Priority Launch'.");
                MessageBox.Show(this, "NinjaTrader is already running. Please close it before using Priority Launch.", "Priority Launch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true);
            try
            {
                string user, pass;
                if (!Program.TryLoadCredentials(out user, out pass))
                {
                    using (var dlg = new CredentialsForm())
                    {
                        if (dlg.ShowDialog(this) != DialogResult.OK)
                        {
                            AppendLog("Canceled. Sign-in information was not saved.");
                            return;
                        }
                        if (string.IsNullOrEmpty(dlg.Password))
                        {
                            MessageBox.Show(this, "Password cannot be empty.", "Priority Launch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        Program.SaveCredentials(dlg.Username, dlg.Password);
                        user = dlg.Username;
                        pass = dlg.Password;
                    }
                }

                AppendLog("Launching NinjaTrader (high priority + auto sign-in)...");
                // Keep UI Automation on the WinForms STA thread.
                Program.LaunchNinjaTrader(user, pass, exe);
                Program.SavePreferredNinjaExePath(exe);
                AppendLog("Launch and auto sign-in completed.");
                AppendLog("The current executable path was saved automatically.");
            }
            catch (Exception ex)
            {
                AppendLog("Operation failed: " + ex.Message);
                MessageBox.Show(this, "Launch failed:\n" + ex.Message, "Priority Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private sealed class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                UpdateStyles();
            }
        }

        private sealed class BufferedTableLayout : TableLayoutPanel
        {
            public BufferedTableLayout()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                UpdateStyles();
            }
        }
    }
}
