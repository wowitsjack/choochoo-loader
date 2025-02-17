using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Win32;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace ChooChooApp
{
    // A simple input box for TV mode.
    public static class SimpleInputBox
    {
        public static string Show(string prompt, string title, string defaultValue)
        {
            return defaultValue;
        }
    }

    // XInput definitions.
    public static class XInputConstants
    {
        public const ushort XINPUT_GAMEPAD_DPAD_UP = 0x0001;
        public const ushort XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
        public const ushort XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
        public const ushort XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
        public const ushort XINPUT_GAMEPAD_START = 0x0010;
        public const ushort XINPUT_GAMEPAD_BACK = 0x0020;
        public const ushort XINPUT_GAMEPAD_LEFT_THUMB = 0x0040;
        public const ushort XINPUT_GAMEPAD_RIGHT_THUMB = 0x0080;
        public const ushort XINPUT_GAMEPAD_LEFT_SHOULDER = 0x0100;
        public const ushort XINPUT_GAMEPAD_RIGHT_SHOULDER = 0x0200;
        public const ushort XINPUT_GAMEPAD_A = 0x1000;
        public const ushort XINPUT_GAMEPAD_B = 0x2000;
        public const ushort XINPUT_GAMEPAD_X = 0x4000;
        public const ushort XINPUT_GAMEPAD_Y = 0x8000;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    public static class XInput
    {
        [DllImport("xinput1_4.dll")]
        public static extern uint XInputGetState(uint dwUserIndex, out XINPUT_STATE pState);
    }

    // DbgHelp and NativeMethods.
    public enum MINIDUMP_TYPE : uint
    {
        MiniDumpNormal = 0x00000000,
        MiniDumpWithDataSegs = 0x00000001,
        MiniDumpWithFullMemory = 0x00000002
    }

    public static class NativeMethods
    {
        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint PAGE_READWRITE = 0x04;
        public const uint MEM_RELEASE = 0x8000;
        public const uint INFINITE = 0xFFFFFFFF;
        public const uint THREAD_SUSPEND_RESUME = 0x0002;

        [DllImport("dbghelp.dll", SetLastError = true)]
        public static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint ProcessId,
            IntPtr hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallbackParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize,
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    public partial class MainForm : Form
    {
        // Desktop UI Controls.
        private Panel mainPanel;
        private TabControl tabControl;
        private TabPage tabPageMain;
        private TabPage tabPageHelp;
        private TabPage tabPageTools;
        // Diamond indicator.
        private Label arrowIndicator;
        private Label labelRunningExes;
        private ComboBox comboRunningExes;
        private GroupBox groupPaths;
        private Label labelGamePath;
        private ComboBox comboGame;
        private Button btnBrowseGame;
        private Button btnBrowseTrainer;
        private Label labelTrainerPath;
        private ComboBox comboTrainer;
        private CheckBox[] chkAdditional = new CheckBox[4];
        private ComboBox[] comboAdditional = new ComboBox[4];
        private Button[] btnBrowseAdditional = new Button[4];
        private string[] additionalInjectedFile = new string[4];
        private CheckBox chkFullscreen;
        private CheckBox chkTVMode;
        private CheckBox chkAutoLaunch;
        private GroupBox groupProfiles;
        private ComboBox comboProfiles;
        private Button btnRefreshProfiles;
        private Button btnLoadProfile;
        private Button btnSaveProfile;
        private Button btnDeleteProfile;
        private TextBox txtStatusLog;
        private Button btnLaunch;
        private ListBox listBoxDlls;
        // Tools Tab Controls.
        private Button btnFreezeProcess;
        private Button btnUnfreezeProcess;
        private Button btnKillProcess;
        private Button btnDumpProcess;
        private Button btnListImports;
        private Button btnListRuntimes;
        private Button btnSaveState;
        private Button btnLoadState;
        private Button btnListModules;
        private Button btnListThreads;
        private TextBox txtToolOutput;
        // Timers.
        private WinFormsTimer xinputTimer;
        private WinFormsTimer injectionTimer;
        private WinFormsTimer arrowAnimationTimer;
        private double arrowAnimationTime;
        private int arrowAnimationOffset;
        private int arrowAnimationAmplitude = 3;
        private double arrowAnimationPeriod = 3.0;
        // Analog Input Flags.
        private bool analogDownSent, analogUpSent, analogLeftSent, analogRightSent;
        // Data Files.
        private readonly string recentFile = "recent.ini";
        private readonly string profilesDir = "profiles";
        private readonly string settingsFile = "settings.ini";
        // XInput Tracking.
        private XINPUT_STATE prevXInputState;
        private byte prevLeftTrigger, prevRightTrigger;
        // DLL Validation.
        private Dictionary<string, bool> validatedDlls = new Dictionary<string, bool>();
        // Target Process.
        private Process gameProcess;
        // File Browser Controls.
        private Panel fileBrowserPanel;
        private TableLayoutPanel fileBrowserLayout;
        private TextBox fileBrowserPathEntry;
        private ListView fileBrowserList;
        private FlowLayoutPanel fileBrowserButtonPanel;
        private Button btnUp;
        private Button btnRefresh; // TV-mode file browser refresh button.
        private Button btnSelect;
        private Button btnCancel;
        private string currentDirectory;
        private Control currentTargetControl;
        // TV Mode Controls.
        private Panel tvModePanel;
        private ListView tvModeList;
        private Panel tvInfoPanel;
        private TextBox tvInfoText;
        // Overlay Panel.
        private Panel overlayPanel;

        // TV Mode font (MotivaSans).
        private Font tvFont;

        public MainForm()
        {
            this.Font = new Font("Tahoma", 9);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(1200, 750);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Set up overlay panel.
            overlayPanel = new Panel();
            overlayPanel.Dock = DockStyle.Fill;
            overlayPanel.BackColor = Color.FromArgb(60, 60, 60);
            overlayPanel.Visible = false;
            overlayPanel.TabIndex = 1000;
            overlayPanel.KeyDown += OverlayPanel_KeyDown;
            this.Controls.Add(overlayPanel);
            overlayPanel.BringToFront();

            // Load TV mode font from Fonts\MotivaSans.ttf.
            try
            {
                PrivateFontCollection pfc = new PrivateFontCollection();
                string fontPath = Path.Combine(Application.StartupPath, "Fonts", "MotivaSans.ttf");
                pfc.AddFontFile(fontPath);
                tvFont = new Font(pfc.Families[0], 24, FontStyle.Bold);
            }
            catch
            {
                tvFont = new Font("Tahoma", 24, FontStyle.Bold);
            }

            SetupFileBrowserPanel();
            SetupTVModePanel();

            mainPanel = new Panel() { Dock = DockStyle.Fill };
            this.Controls.Add(mainPanel);
            tabControl = new TabControl() { Dock = DockStyle.Fill, ForeColor = Color.White };
            mainPanel.Controls.Add(tabControl);
            tabPageMain = new TabPage("Main") { ForeColor = Color.White };
            tabPageHelp = new TabPage("Help") { ForeColor = Color.White };
            tabPageTools = new TabPage("Tools") { ForeColor = Color.White };
            tabControl.TabPages.Add(tabPageMain);
            tabControl.TabPages.Add(tabPageHelp);
            tabControl.TabPages.Add(tabPageTools);
            Panel mainTabPanel = new Panel() { Dock = DockStyle.Fill };
            tabPageMain.Controls.Add(mainTabPanel);

            // Desktop controls.
            mainTabPanel.Controls.Add(chkFullscreen = new CheckBox() { Text = "Fullscreen Mode", Location = new Point(10, 10), AutoSize = true, ForeColor = Color.White });
            mainTabPanel.Controls.Add(chkTVMode = new CheckBox() { Text = "TV Mode", Location = new Point(150, 10), AutoSize = true, ForeColor = Color.White });
            chkTVMode.CheckedChanged += (s, e) => ToggleTVMode();
            groupPaths = new GroupBox() { Text = "Paths & Process Selection", Bounds = new Rectangle(10, 40, 850, 320), ForeColor = Color.White };
            mainTabPanel.Controls.Add(groupPaths);
            labelRunningExes = new Label() { Text = "Running Exe:", Location = new Point(20, 25), AutoSize = true, ForeColor = Color.White };
            groupPaths.Controls.Add(labelRunningExes);
            comboRunningExes = new ComboBox() { Location = new Point(130, 20), Size = new Size(500, 25), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
            comboRunningExes.DrawMode = DrawMode.OwnerDrawFixed;
            comboRunningExes.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0)
                {
                    // Fixed typo: using 'comboRunningExes' instead of 'comboRunningExoes'
                    string text = comboRunningExes.Items[e.Index].ToString();
                    using (SolidBrush brush = new SolidBrush(e.ForeColor))
                    {
                        SizeF textSize = e.Graphics.MeasureString(text, comboRunningExes.Font);
                        float x = e.Bounds.Left + (e.Bounds.Width - textSize.Width) / 2;
                        float y = e.Bounds.Top + (e.Bounds.Height - textSize.Height) / 2;
                        e.Graphics.DrawString(text, comboRunningExes.Font, brush, x, y);
                    }
                }
                e.DrawFocusRectangle();
            };
            comboRunningExes.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
                    comboRunningExes.DroppedDown = true;
            };
            groupPaths.Controls.Add(comboRunningExes);
            comboRunningExes.SelectedIndexChanged += (s, e) =>
            {
                if (comboRunningExes.SelectedItem is Process proc)
                {
                    gameProcess = proc;
                    LogStatus("Process selected: " + proc.ProcessName + " (PID " + proc.Id + ")");
                }
            };
            labelGamePath = new Label() { Text = "Game Path (if not running):", Location = new Point(20, 65), AutoSize = true, ForeColor = Color.White };
            groupPaths.Controls.Add(labelGamePath);
            comboGame = new ComboBox() { Location = new Point(200, 60), Size = new Size(430, 25), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
            groupPaths.Controls.Add(comboGame);
            btnBrowseGame = new Button() { Text = "Browse...", Location = new Point(640, 60), Size = new Size(100, 25), ForeColor = Color.White };
            btnBrowseGame.Click += (s, e) => IntegratedBrowse(comboGame);
            groupPaths.Controls.Add(btnBrowseGame);
            labelTrainerPath = new Label() { Text = "Trainer Path:", Location = new Point(20, 105), AutoSize = true, ForeColor = Color.White };
            groupPaths.Controls.Add(labelTrainerPath);
            comboTrainer = new ComboBox() { Location = new Point(130, 100), Size = new Size(500, 25), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
            groupPaths.Controls.Add(comboTrainer);
            btnBrowseTrainer = new Button() { Text = "Browse...", Location = new Point(640, 100), Size = new Size(100, 25), ForeColor = Color.White };
            btnBrowseTrainer.Click += (s, e) => IntegratedBrowse(comboTrainer);
            groupPaths.Controls.Add(btnBrowseTrainer);
            for (int i = 0; i < 4; i++)
            {
                int y = 145 + i * 35;
                chkAdditional[i] = new CheckBox() { Text = "Launch/Inject", Location = new Point(20, y), ForeColor = Color.White };
                groupPaths.Controls.Add(chkAdditional[i]);
                comboAdditional[i] = new ComboBox() { Location = new Point(130, y), Size = new Size(500, 25), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
                groupPaths.Controls.Add(comboAdditional[i]);
                btnBrowseAdditional[i] = new Button() { Text = "Browse...", Location = new Point(640, y), Size = new Size(100, 25), ForeColor = Color.White };
                int idxLocal = i;
                btnBrowseAdditional[i].Click += (s, e) => IntegratedBrowse(comboAdditional[idxLocal]);
                groupPaths.Controls.Add(btnBrowseAdditional[i]);
            }
            chkAutoLaunch = new CheckBox() { Text = "Save Settings & Enable Autolaunch", Location = new Point(10, groupPaths.Bottom + 10), AutoSize = true, ForeColor = Color.White };
            mainTabPanel.Controls.Add(chkAutoLaunch);
            groupProfiles = new GroupBox() { Text = "Profiles", Bounds = new Rectangle(870, 40, 300, 300), ForeColor = Color.White };
            mainTabPanel.Controls.Add(groupProfiles);
            comboProfiles = new ComboBox() { Size = new Size(280, 25), Location = new Point((groupProfiles.ClientSize.Width - 280) / 2, 40), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
            groupProfiles.Controls.Add(comboProfiles);
            btnRefreshProfiles = new Button() { Size = new Size(80, 30), Location = new Point(65, 80), Text = "Refresh", ForeColor = Color.White };
            btnRefreshProfiles.Click += BtnRefreshProfiles_Click;
            groupProfiles.Controls.Add(btnRefreshProfiles);
            btnLoadProfile = new Button() { Size = new Size(80, 30), Location = new Point(65 + 80 + 10, 80), Text = "Load", ForeColor = Color.White };
            btnLoadProfile.Click += BtnLoadProfile_Click;
            groupProfiles.Controls.Add(btnLoadProfile);
            btnSaveProfile = new Button() { Size = new Size(170, 30), Location = new Point((groupProfiles.ClientSize.Width - 170) / 2, 120), Text = "Save", ForeColor = Color.White };
            btnSaveProfile.Click += BtnSaveProfile_Click;
            groupProfiles.Controls.Add(btnSaveProfile);
            btnDeleteProfile = new Button() { Size = new Size(170, 30), Location = new Point((groupProfiles.ClientSize.Width - 170) / 2, 160), Text = "Delete", ForeColor = Color.White };
            btnDeleteProfile.Click += BtnDeleteProfile_Click;
            groupProfiles.Controls.Add(btnDeleteProfile);
            txtStatusLog = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Bounds = new Rectangle(10, chkAutoLaunch.Bottom + 10, 850, 150), ForeColor = Color.White, BackColor = Color.FromArgb(30,30,30) };
            mainTabPanel.Controls.Add(txtStatusLog);
            // Desktop file-browser refresh button removed.
            listBoxDlls = new ListBox() { Location = new Point(txtStatusLog.Right + 10, txtStatusLog.Top), Size = new Size(300, txtStatusLog.Height), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Tahoma", 10), ForeColor = Color.White, BackColor = Color.FromArgb(45,45,48) };
            mainTabPanel.Controls.Add(listBoxDlls);
            Panel panelLaunch = new Panel() { Location = new Point(10, txtStatusLog.Bottom + 40), Size = new Size(1170, 50) };
            mainTabPanel.Controls.Add(panelLaunch);
            btnLaunch = new Button() { Text = "Launch", Size = new Size(panelLaunch.Width, panelLaunch.Height), Location = new Point(0,0), Font = new Font("Tahoma",14,FontStyle.Bold), ForeColor = Color.White };
            btnLaunch.Click += BtnLaunch_Click;
            panelLaunch.Controls.Add(btnLaunch);
            HelpTab helpControl = new HelpTab() { Dock = DockStyle.Fill };
            tabPageHelp.Controls.Add(helpControl);
            int tbtnWidth = 130, tbtnHeight = 30, tpadding = 10;
            btnFreezeProcess = new Button() { Text = "Freeze Proc", Location = new Point(tpadding, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnFreezeProcess.Click += BtnFreezeProcess_Click;
            tabPageTools.Controls.Add(btnFreezeProcess);
            btnUnfreezeProcess = new Button() { Text = "Unfreeze Proc", Location = new Point(tpadding*2+tbtnWidth, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnUnfreezeProcess.Click += BtnUnfreezeProcess_Click;
            tabPageTools.Controls.Add(btnUnfreezeProcess);
            btnKillProcess = new Button() { Text = "Kill Proc", Location = new Point(tpadding*3+tbtnWidth*2, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnKillProcess.Click += BtnKillProcess_Click;
            tabPageTools.Controls.Add(btnKillProcess);
            btnDumpProcess = new Button() { Text = "Dump Proc", Location = new Point(tpadding*4+tbtnWidth*3, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnDumpProcess.Click += BtnDumpProcess_Click;
            tabPageTools.Controls.Add(btnDumpProcess);
            btnListImports = new Button() { Text = "List Imports", Location = new Point(tpadding*5+tbtnWidth*4, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnListImports.Click += BtnListImports_Click;
            tabPageTools.Controls.Add(btnListImports);
            btnListRuntimes = new Button() { Text = "List Runtimes", Location = new Point(tpadding, tpadding*2+tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnListRuntimes.Click += BtnListRuntimes_Click;
            tabPageTools.Controls.Add(btnListRuntimes);
            btnSaveState = new Button() { Text = "Save State", Location = new Point(tpadding*2+tbtnWidth, tpadding*2+tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnSaveState.Click += BtnSaveState_Click;
            tabPageTools.Controls.Add(btnSaveState);
            btnLoadState = new Button() { Text = "Load State", Location = new Point(tpadding*3+tbtnWidth*2, tpadding*2+tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnLoadState.Click += BtnLoadState_Click;
            tabPageTools.Controls.Add(btnLoadState);
            btnListModules = new Button() { Text = "List Modules", Location = new Point(tpadding*4+tbtnWidth*3, tpadding*2+tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnListModules.Click += BtnListModules_Click;
            tabPageTools.Controls.Add(btnListModules);
            btnListThreads = new Button() { Text = "List Threads", Location = new Point(tpadding*5+tbtnWidth*4, tpadding*2+tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnListThreads.Click += BtnListThreads_Click;
            tabPageTools.Controls.Add(btnListThreads);
            txtToolOutput = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(tpadding, tpadding*3+tbtnHeight*2), Size = new Size(1150,500), ForeColor = Color.White, BackColor = Color.FromArgb(30,30,30) };
            tabPageTools.Controls.Add(txtToolOutput);
            xinputTimer = new WinFormsTimer() { Interval = 16 };
            xinputTimer.Tick += XinputTimer_Tick;
            xinputTimer.Start();
            injectionTimer = new WinFormsTimer() { Interval = 1000 };
            injectionTimer.Tick += InjectionTimer_Tick;
            injectionTimer.Start();
            arrowAnimationTimer = new WinFormsTimer() { Interval = 50 };
            arrowAnimationTimer.Tick += ArrowAnimationTimer_Tick;
            arrowAnimationTimer.Start();

            // Set up diamond indicator.
            arrowIndicator = new Label();
            arrowIndicator.Text = "◆";
            arrowIndicator.Font = new Font("Tahoma", 12, FontStyle.Bold);
            arrowIndicator.AutoSize = true;
            arrowIndicator.ForeColor = Color.White;
            this.Controls.Add(arrowIndicator);
            arrowIndicator.BringToFront();

            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            AttachFocusHandlers(this);
            this.ActiveControl = comboRunningExes;
            UpdateArrowIndicator();
            LogStatus("CHOOCHOO INJECTION ENGINE STARTED");
            LoadRecentsForCombo(comboGame);
            LoadRecentsForCombo(comboTrainer);
            for (int i = 0; i < 4; i++)
                LoadRecentsForCombo(comboAdditional[i]);
            UpdateProfileList();
            chkAutoLaunch.CheckedChanged += (s, e) =>
            {
                SaveSettings();
                LogStatus(chkAutoLaunch.Checked ? "AUTO-LAUNCH ENABLED" : "AUTO-LAUNCH DISABLED");
            };
            RefreshDllList();
            PopulateRunningExes();
            comboRunningExes.SelectedIndexChanged += (s, e) =>
            {
                if (comboRunningExes.SelectedItem is Process proc)
                {
                    gameProcess = proc;
                    LogStatus("Process selected: " + proc.ProcessName + " (PID " + proc.Id + ")");
                }
            };
            XINPUT_STATE state;
            if (XInput.XInputGetState(0, out state) == 0)
                LogStatus("CONTROLLER DETECTED");
            this.Resize += MainForm_Resize;
            this.FormClosing += (s, e) => SaveSettings();
            ApplyDarkTheme(this);
            LoadSettings();
        }

        // --- Overlay Helper Methods ---
        private void ShowOverlay(Control content)
        {
            overlayPanel.Controls.Clear();
            content.Dock = DockStyle.Fill;
            overlayPanel.Controls.Add(content);
            overlayPanel.Visible = true;
            overlayPanel.BringToFront();
            overlayPanel.Focus();
        }

        private void HideOverlay()
        {
            overlayPanel.Visible = false;
            overlayPanel.Controls.Clear();
            if (tvModePanel.Visible)
            {
                if (tvModeList.Items.Count > 0)
                {
                    tvModeList.SelectedIndices.Clear();
                    tvModeList.SelectedIndices.Add(0);
                }
                tvModePanel.Focus();
            }
            else
                mainPanel.Focus();
        }

        private void OverlayPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                HideOverlay();
                e.SuppressKeyPress = true;
            }
        }

        // --- TV Mode Functions ---
        private void SetupTVModePanel()
        {
            // TV Mode Header.
            TableLayoutPanel tvHeaderPanel = new TableLayoutPanel();
            tvHeaderPanel.Name = "tvHeaderPanel";
            tvHeaderPanel.Dock = DockStyle.Top;
            tvHeaderPanel.Height = (int)(this.ClientSize.Height * 0.15);
            tvHeaderPanel.BackColor = Color.FromArgb(45,45,48);
            // Increase first column width from 120 to 200 for a longer Launch button.
            tvHeaderPanel.ColumnCount = 2;
            tvHeaderPanel.RowCount = 1;
            tvHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            tvHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Button tvHeaderLaunch = new Button();
            // Modified text: "Launch" with "Start" underneath.
            tvHeaderLaunch.Text = "Launch\r\nStart";
            tvHeaderLaunch.Font = tvFont;
            tvHeaderLaunch.Dock = DockStyle.Fill;
            tvHeaderLaunch.Padding = new Padding(5);
            tvHeaderLaunch.Click += (s, e) => BtnLaunch_Click(null, EventArgs.Empty);

            Label tvHeaderLabel = new Label();
            tvHeaderLabel.Text = "CHOOCHOO LOADER - TV MODE";
            tvHeaderLabel.Font = tvFont;
            tvHeaderLabel.ForeColor = Color.White;
            tvHeaderLabel.TextAlign = ContentAlignment.MiddleCenter;
            tvHeaderLabel.Dock = DockStyle.Fill;

            tvHeaderPanel.Controls.Add(tvHeaderLaunch, 0, 0);
            tvHeaderPanel.Controls.Add(tvHeaderLabel, 1, 0);

            // TV Mode Menu List.
            tvModeList = new ListView();
            // Use Details view with one column.
            tvModeList.View = View.Details;
            tvModeList.FullRowSelect = true;
            tvModeList.GridLines = false;
            tvModeList.HeaderStyle = ColumnHeaderStyle.None;
            tvModeList.Font = new Font(tvFont.FontFamily, 22, FontStyle.Bold);
            tvModeList.ForeColor = Color.White;
            tvModeList.BackColor = Color.FromArgb(30,30,30);
            tvModeList.Margin = new Padding(10);
            // Create a single column; we'll adjust its width later.
            tvModeList.Columns.Add("Menu", 0, HorizontalAlignment.Center);
            string[] tvMenuItems = new string[]
            {
                "Select Running Process",
                "Set Game Path",
                "Set Trainer Path",
                "Set Additional Injection 1",
                "Set Additional Injection 2",
                "Set Additional Injection 3",
                "Set Additional Injection 4",
                "Configure DLL Injections",
                "Manage Profiles",
                "Launch Application",
                "View Console Output",
                "Advanced Tools",
                "Exit TV Mode"
            };
            foreach (string item in tvMenuItems)
            {
                ListViewItem lvi = new ListViewItem(item);
                tvModeList.Items.Add(lvi);
            }
            // Instead of auto-resizing by header size, set the column width to fill the ListView.
            tvModeList.Resize += (s, e) =>
            {
                if (tvModeList.Columns.Count > 0)
                    tvModeList.Columns[0].Width = tvModeList.ClientSize.Width;
            };
            if (tvModeList.Items.Count > 0)
                tvModeList.SelectedIndices.Add(0);
            tvModeList.Dock = DockStyle.Fill;
            tvModeList.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Left)
                {
                    int idx = tvModeList.SelectedIndices[0];
                    if (idx > 0)
                    {
                        tvModeList.SelectedIndices.Clear();
                        tvModeList.SelectedIndices.Add(idx - 1);
                        e.Handled = true;
                    }
                }
                else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Right)
                {
                    int idx = tvModeList.SelectedIndices[0];
                    if (idx < tvModeList.Items.Count - 1)
                    {
                        tvModeList.SelectedIndices.Clear();
                        tvModeList.SelectedIndices.Add(idx + 1);
                        e.Handled = true;
                    }
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    if (tvModeList.SelectedItems.Count > 0)
                        HandleTVModeAction(tvModeList.SelectedItems[0].Text);
                    e.Handled = true;
                }
            };

            // TV Mode Info Panel.
            tvInfoPanel = new Panel();
            tvInfoPanel.Name = "tvInfoPanel";
            tvInfoPanel.Dock = DockStyle.Bottom;
            int infoHeight = (int)(this.ClientSize.Height * 0.20);
            tvInfoPanel.Height = infoHeight;
            tvInfoPanel.BackColor = Color.FromArgb(45,45,48);
            tvInfoText = new TextBox();
            tvInfoText.Multiline = true;
            tvInfoText.ReadOnly = true;
            tvInfoText.ScrollBars = ScrollBars.Vertical;
            tvInfoText.Font = new Font(tvFont.FontFamily, 18, FontStyle.Bold);
            tvInfoText.BackColor = Color.FromArgb(30,30,30);
            tvInfoText.ForeColor = Color.White;
            tvInfoText.Dock = DockStyle.Fill;
            tvInfoPanel.Controls.Add(tvInfoText);

            // Assemble TV Mode Panel.
            tvModePanel = new Panel();
            tvModePanel.Dock = DockStyle.Fill;
            tvModePanel.BackColor = Color.FromArgb(45,45,48);
            tvModePanel.Controls.Add(tvModeList);
            tvModePanel.Controls.Add(tvInfoPanel);
            tvModePanel.Controls.Add(tvHeaderPanel);
            tvModePanel.Visible = false;
            this.Controls.Add(tvModePanel);
            tvModePanel.BringToFront();
        }

        public void RefreshTVInfo()
        {
            string configInfo = "Game Path: " + comboGame.Text + Environment.NewLine +
                                "Trainer Path: " + comboTrainer.Text + Environment.NewLine;
            for (int i = 0; i < 4; i++)
            {
                configInfo += $"Additional {i+1}: {(string.IsNullOrEmpty(comboAdditional[i].Text) ? "[Not Set]" : comboAdditional[i].Text)} - {(chkAdditional[i].Checked ? "Inject" : "Launch/Inject")}" + Environment.NewLine;
            }
            configInfo += Environment.NewLine + "Recent Log:" + Environment.NewLine + txtStatusLog.Text;
            tvInfoText.Text = configInfo;
        }

        private void ToggleTVMode()
        {
            if (chkTVMode.Checked)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                mainPanel.Visible = false;
                tvModePanel.Visible = true;
                tvModePanel.BringToFront();
                // Automatically select the first item and focus the list.
                if (tvModeList.Items.Count > 0)
                {
                    tvModeList.SelectedIndices.Clear();
                    tvModeList.SelectedIndices.Add(0);
                    tvModeList.Focus();
                }
                RefreshTVInfo();
            }
            else
            {
                // Hide any overlays before exiting TV mode.
                HideOverlay();
                tvModePanel.Visible = false;
                mainPanel.Visible = true;
                mainPanel.BringToFront();
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.WindowState = FormWindowState.Normal;
                this.ClientSize = new Size(1200, 750);
            }
        }

        private void HandleTVModeAction(string action)
        {
            switch (action)
            {
                case "Select Running Process":
                    TVSelectRunningProcess();
                    break;
                case "Set Game Path":
                    TVFileBrowser(comboGame);
                    break;
                case "Set Trainer Path":
                    TVFileBrowser(comboTrainer);
                    break;
                case "Set Additional Injection 1":
                    TVFileBrowser(comboAdditional[0]);
                    break;
                case "Set Additional Injection 2":
                    TVFileBrowser(comboAdditional[1]);
                    break;
                case "Set Additional Injection 3":
                    TVFileBrowser(comboAdditional[2]);
                    break;
                case "Set Additional Injection 4":
                    TVFileBrowser(comboAdditional[3]);
                    break;
                case "Configure DLL Injections":
                    TVConfigureDLLInjections();
                    break;
                case "Manage Profiles":
                    TVManageProfiles();
                    break;
                case "Launch Application":
                    BtnLaunch_Click(null, EventArgs.Empty);
                    break;
                case "View Console Output":
                    TVViewConsoleOutput();
                    break;
                case "Advanced Tools":
                    TVAdvancedTools();
                    break;
                case "Exit TV Mode":
                    chkTVMode.Checked = false;
                    break;
                default:
                    break;
            }
            RefreshTVInfo();
        }

        // TV Process Selection using a styled ListView.
        private void TVSelectRunningProcess()
        {
            Panel panel = new Panel();
            panel.BackColor = Color.FromArgb(45,45,48);
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            ListView processList = new ListView();
            processList.Dock = DockStyle.Fill;
            processList.View = View.Details;
            processList.FullRowSelect = true;
            processList.GridLines = true;
            processList.HeaderStyle = ColumnHeaderStyle.None;
            processList.Font = new Font(tvFont.FontFamily, 18, FontStyle.Bold);
            processList.ForeColor = Color.White;
            processList.BackColor = Color.FromArgb(30,30,30);
            processList.Margin = new Padding(10);
            processList.Columns.Add("Process", -2, HorizontalAlignment.Center);
            processList.Columns.Add("PID", -2, HorizontalAlignment.Center);
            Process[] procs = ProcessHelper.GetProcesses();
            foreach (Process p in procs)
            {
                try
                {
                    ListViewItem lvi = new ListViewItem(p.ProcessName);
                    lvi.SubItems.Add(p.Id.ToString());
                    processList.Items.Add(lvi);
                }
                catch { }
            }
            if (processList.Items.Count > 0)
                processList.SelectedIndices.Add(0);
            processList.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && processList.SelectedItems.Count > 0)
                {
                    string sel = processList.SelectedItems[0].SubItems[0].Text;
                    foreach (Process p in procs)
                    {
                        if (p.ProcessName == sel)
                        {
                            gameProcess = p;
                            LogStatus("Process selected: " + p.ProcessName + " (PID " + p.Id + ")");
                            break;
                        }
                    }
                    HideOverlay();
                    e.Handled = true;
                }
            };
            panel.Controls.Add(processList);
            ShowOverlay(panel);
        }

        private void TVFileBrowser(Control target)
        {
            ShowIntegratedFileBrowser(target);
        }

        private void TVFileBrowserForTextBox(TextBox target)
        {
            ShowIntegratedFileBrowser(target);
        }

        private void TVConfigureDLLInjections()
        {
            Panel panel = new Panel();
            panel.BackColor = Color.FromArgb(45,45,48);
            panel.Dock = DockStyle.Fill;
            TableLayoutPanel tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Fill;
            tbl.RowCount = 4;
            tbl.ColumnCount = 3;
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            for (int i = 0; i < 4; i++)
            {
                Label lbl = new Label() { Text = "Additional Injection " + (i + 1), Dock = DockStyle.Fill, ForeColor = Color.White, Padding = new Padding(5) };
                TextBox tb = new TextBox() { Text = comboAdditional[i].Text, Dock = DockStyle.Fill, Font = new Font(tvFont.FontFamily, 16, FontStyle.Bold), Margin = new Padding(5) };
                CheckBox chk = new CheckBox() { Checked = chkAdditional[i].Checked, Dock = DockStyle.Fill, Font = new Font(tvFont.FontFamily, 16, FontStyle.Bold), ForeColor = Color.White, Margin = new Padding(5) };
                int index = i;
                chk.CheckedChanged += (s, e) => { chkAdditional[index].Checked = chk.Checked; };
                Button btnBrowse = new Button() { Text = "Browse", Dock = DockStyle.Fill, Font = tvFont, Margin = new Padding(5) };
                btnBrowse.Click += (s, e) => { TVFileBrowserForTextBox(tb); };
                tbl.Controls.Add(lbl, 0, i);
                tbl.Controls.Add(tb, 1, i);
                Panel pnl = new Panel() { Dock = DockStyle.Fill, Margin = new Padding(5) };
                pnl.Controls.Add(chk);
                pnl.Controls.Add(btnBrowse);
                btnBrowse.Location = new Point(150, 0);
                tbl.Controls.Add(pnl, 2, i);
                tb.TextChanged += (s, e) => { comboAdditional[index].Text = tb.Text; };
            }
            Button btnOk = new Button() { Text = "OK", Dock = DockStyle.Bottom, Height = 50, Font = tvFont, Margin = new Padding(5) };
            btnOk.Click += (s, e) => { HideOverlay(); };
            panel.Controls.Add(tbl);
            panel.Controls.Add(btnOk);
            ShowOverlay(panel);
        }

        private void TVManageProfiles()
        {
            Panel panel = new Panel();
            panel.BackColor = Color.FromArgb(45,45,48);
            panel.Dock = DockStyle.Fill;
            ListView lb = new ListView();
            lb.Dock = DockStyle.Top;
            lb.View = View.Details;
            lb.FullRowSelect = true;
            lb.GridLines = true;
            lb.HeaderStyle = ColumnHeaderStyle.None;
            lb.Font = new Font(tvFont.FontFamily, 18, FontStyle.Bold);
            lb.ForeColor = Color.White;
            lb.BackColor = Color.FromArgb(30,30,30);
            lb.Margin = new Padding(10);
            lb.Columns.Add("Profile", -2, HorizontalAlignment.Center);
            if (Directory.Exists(profilesDir))
            {
                string[] files = Directory.GetFiles(profilesDir, "*.ini");
                foreach (string file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (name.ToLower() != "last")
                    {
                        ListViewItem lvi = new ListViewItem(name);
                        lb.Items.Add(lvi);
                    }
                }
            }
            if (lb.Items.Count > 0)
                lb.SelectedIndices.Add(0);
            lb.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && lb.SelectedItems.Count > 0)
                {
                    string profileName = lb.SelectedItems[0].Text;
                    string filePath = Path.Combine(profilesDir, profileName + ".ini");
                    if (File.Exists(filePath))
                    {
                        LoadProfile(filePath);
                        LogStatus("Profile loaded: " + profileName);
                    }
                    HideOverlay();
                    e.Handled = true;
                }
            };

            FlowLayoutPanel panelButtons = new FlowLayoutPanel();
            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Height = 70;
            panelButtons.Padding = new Padding(10);
            Button btnLoad = new Button() { Text = "Load", Width = 150, Height = 50, Font = tvFont, Margin = new Padding(5) };
            btnLoad.Click += (s, e) =>
            {
                if (lb.SelectedItems.Count > 0)
                {
                    string profileName = lb.SelectedItems[0].Text;
                    string filePath = Path.Combine(profilesDir, profileName + ".ini");
                    if (File.Exists(filePath))
                    {
                        LoadProfile(filePath);
                        LogStatus("Profile loaded: " + profileName);
                    }
                }
            };
            Button btnSave = new Button() { Text = "Save", Width = 150, Height = 50, Font = tvFont, Margin = new Padding(5) };
            btnSave.Click += (s, e) =>
            {
                string profileName = comboProfiles.Text.Trim();
                if (!string.IsNullOrEmpty(profileName))
                {
                    string filePath = Path.Combine(profilesDir, profileName + ".ini");
                    SaveProfile(filePath);
                    LogStatus("Profile saved: " + profileName);
                    lb.Items.Clear();
                    if (Directory.Exists(profilesDir))
                    {
                        string[] files = Directory.GetFiles(profilesDir, "*.ini");
                        foreach (string file in files)
                        {
                            string name = Path.GetFileNameWithoutExtension(file);
                            if (name.ToLower() != "last")
                            {
                                ListViewItem lvi = new ListViewItem(name);
                                lb.Items.Add(lvi);
                            }
                        }
                    }
                }
            };
            Button btnDelete = new Button() { Text = "Delete", Width = 150, Height = 50, Font = tvFont, Margin = new Padding(5) };
            btnDelete.Click += (s, e) =>
            {
                if (lb.SelectedItems.Count > 0)
                {
                    string profileName = lb.SelectedItems[0].Text;
                    string filePath = Path.Combine(profilesDir, profileName + ".ini");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        LogStatus("Profile deleted: " + profileName);
                        lb.Items.Remove(lb.SelectedItems[0]);
                    }
                }
            };
            panelButtons.Controls.Add(btnLoad);
            panelButtons.Controls.Add(btnSave);
            panelButtons.Controls.Add(btnDelete);
            panel.Controls.Add(lb);
            panel.Controls.Add(panelButtons);
            ShowOverlay(panel);
        }

        private void TVViewConsoleOutput()
        {
            Panel panel = new Panel();
            panel.BackColor = Color.FromArgb(45,45,48);
            panel.Dock = DockStyle.Fill;
            TextBox tb = new TextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.ScrollBars = ScrollBars.Vertical;
            tb.Dock = DockStyle.Fill;
            tb.Font = tvFont;
            tb.BackColor = Color.FromArgb(30,30,30);
            tb.ForeColor = Color.White;
            tb.Margin = new Padding(10);
            tb.Text = txtStatusLog.Text;
            panel.Controls.Add(tb);
            ShowOverlay(panel);
        }

        private void TVAdvancedTools()
        {
            Panel panel = new Panel();
            panel.BackColor = Color.FromArgb(45,45,48);
            panel.Dock = DockStyle.Fill;
            ListView lb = new ListView();
            lb.Dock = DockStyle.Fill;
            lb.View = View.Details;
            lb.FullRowSelect = true;
            lb.GridLines = true;
            lb.HeaderStyle = ColumnHeaderStyle.None;
            lb.Font = tvFont;
            lb.ForeColor = Color.White;
            lb.BackColor = Color.FromArgb(30,30,30);
            lb.Margin = new Padding(10);
            lb.Columns.Add("Tool", -2, HorizontalAlignment.Center);
            string[] tools = new string[]
            {
                "Freeze Process",
                "Unfreeze Process",
                "Kill Process",
                "Dump Process",
                "List Imports",
                "List Runtimes",
                "Save State",
                "Load State",
                "List Modules",
                "List Threads"
            };
            foreach (string t in tools)
            {
                ListViewItem lvi = new ListViewItem(t);
                lb.Items.Add(lvi);
            }
            lb.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            if (lb.Items.Count > 0)
                lb.SelectedIndices.Add(0);
            lb.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && lb.SelectedItems.Count > 0)
                {
                    string act = lb.SelectedItems[0].Text;
                    switch (act)
                    {
                        case "Freeze Process":
                            BtnFreezeProcess_Click(null, EventArgs.Empty);
                            break;
                        case "Unfreeze Process":
                            BtnUnfreezeProcess_Click(null, EventArgs.Empty);
                            break;
                        case "Kill Process":
                            BtnKillProcess_Click(null, EventArgs.Empty);
                            break;
                        case "Dump Process":
                            BtnDumpProcess_Click(null, EventArgs.Empty);
                            break;
                        case "List Imports":
                            BtnListImports_Click(null, EventArgs.Empty);
                            break;
                        case "List Runtimes":
                            BtnListRuntimes_Click(null, EventArgs.Empty);
                            break;
                        case "Save State":
                            BtnSaveState_Click(null, EventArgs.Empty);
                            break;
                        case "Load State":
                            BtnLoadState_Click(null, EventArgs.Empty);
                            break;
                        case "List Modules":
                            BtnListModules_Click(null, EventArgs.Empty);
                            break;
                        case "List Threads":
                            BtnListThreads_Click(null, EventArgs.Empty);
                            break;
                    }
                    HideOverlay();
                    e.Handled = true;
                }
            };
            panel.Controls.Add(lb);
            ShowOverlay(panel);
        }

        // --- File Browser Functions ---
        private void SetupFileBrowserPanel()
        {
            fileBrowserPanel = new Panel();
            fileBrowserPanel.Dock = DockStyle.Fill;
            fileBrowserPanel.BackColor = Color.FromArgb(50,50,50);
            fileBrowserPanel.Padding = new Padding(20);
            fileBrowserPanel.Visible = false;

            fileBrowserLayout = new TableLayoutPanel();
            fileBrowserLayout.Dock = DockStyle.Fill;
            fileBrowserLayout.RowCount = 3;
            fileBrowserLayout.ColumnCount = 1;
            fileBrowserLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            fileBrowserLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            fileBrowserLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            fileBrowserPanel.Controls.Add(fileBrowserLayout);

            fileBrowserPathEntry = new TextBox();
            fileBrowserPathEntry.Dock = DockStyle.Fill;
            fileBrowserPathEntry.Font = new Font("Tahoma", 14, FontStyle.Bold);
            fileBrowserPathEntry.BackColor = Color.FromArgb(30,30,30);
            fileBrowserPathEntry.ForeColor = Color.White;
            fileBrowserPathEntry.Margin = new Padding(10);
            fileBrowserPathEntry.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    currentDirectory = fileBrowserPathEntry.Text;
                    RefreshFileBrowserList();
                }
            };
            fileBrowserLayout.Controls.Add(fileBrowserPathEntry, 0, 0);

            fileBrowserList = new ListView();
            fileBrowserList.Dock = DockStyle.Fill;
            fileBrowserList.View = View.Details;
            fileBrowserList.Font = new Font("Tahoma", 14, FontStyle.Bold);
            fileBrowserList.FullRowSelect = true;
            fileBrowserList.GridLines = true;
            fileBrowserList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            fileBrowserList.MultiSelect = false;
            fileBrowserList.Margin = new Padding(10);
            fileBrowserList.Columns.Add("Name", 500);
            fileBrowserList.Columns.Add("Type", 150);
            fileBrowserList.DoubleClick += (s, e) =>
            {
                if (fileBrowserList.SelectedItems.Count > 0)
                {
                    ListViewItem item = fileBrowserList.SelectedItems[0];
                    if ((string)item.SubItems[1].Text == "FOLDER")
                    {
                        currentDirectory = Path.Combine(currentDirectory, item.Text);
                        RefreshFileBrowserList();
                    }
                    else
                    {
                        currentTargetControl.Text = Path.Combine(currentDirectory, item.Text);
                        fileBrowserPanel.Visible = false;
                    }
                }
            };
            fileBrowserList.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (fileBrowserList.SelectedItems.Count > 0)
                    {
                        ListViewItem item = fileBrowserList.SelectedItems[0];
                        if ((string)item.SubItems[1].Text == "FOLDER")
                        {
                            currentDirectory = Path.Combine(currentDirectory, item.Text);
                            RefreshFileBrowserList();
                        }
                        else
                        {
                            currentTargetControl.Text = Path.Combine(currentDirectory, item.Text);
                            fileBrowserPanel.Visible = false;
                        }
                    }
                }
                else if (e.KeyCode == Keys.Back)
                {
                    var parentDir = Directory.GetParent(currentDirectory);
                    if (parentDir == null)
                        fileBrowserPanel.Visible = false;
                    else
                    {
                        currentDirectory = parentDir.FullName;
                        RefreshFileBrowserList();
                    }
                }
            };
            fileBrowserLayout.Controls.Add(fileBrowserList, 0, 1);

            fileBrowserButtonPanel = new FlowLayoutPanel();
            fileBrowserButtonPanel.Dock = DockStyle.Fill;
            fileBrowserButtonPanel.FlowDirection = FlowDirection.LeftToRight;
            fileBrowserButtonPanel.Padding = new Padding(10);
            fileBrowserButtonPanel.BackColor = Color.FromArgb(45,45,48);

            btnUp = new Button();
            btnUp.Text = "Up";
            btnUp.Width = 100;
            btnUp.Height = 40;
            btnUp.Font = new Font("Tahoma", 12, FontStyle.Bold);
            btnUp.BackColor = Color.FromArgb(45,45,48);
            btnUp.ForeColor = Color.White;
            btnUp.Click += (s, e) =>
            {
                var parentDir = Directory.GetParent(currentDirectory);
                if (parentDir != null)
                {
                    currentDirectory = parentDir.FullName;
                    RefreshFileBrowserList();
                }
            };
            fileBrowserButtonPanel.Controls.Add(btnUp);

            btnRefresh = new Button();
            btnRefresh.Text = "Refresh";
            btnRefresh.Width = 150;
            btnRefresh.Height = 50;
            btnRefresh.Font = new Font("Tahoma", 12, FontStyle.Bold);
            btnRefresh.BackColor = Color.FromArgb(45,45,48);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Click += (s, e) => RefreshFileBrowserList();
            fileBrowserButtonPanel.Controls.Add(btnRefresh);

            btnSelect = new Button();
            btnSelect.Text = "Select";
            btnSelect.Width = 100;
            btnSelect.Height = 40;
            btnSelect.Font = new Font("Tahoma", 12, FontStyle.Bold);
            btnSelect.BackColor = Color.FromArgb(45,45,48);
            btnSelect.ForeColor = Color.White;
            btnSelect.Click += (s, e) =>
            {
                if (fileBrowserList.SelectedItems.Count > 0)
                {
                    ListViewItem item = fileBrowserList.SelectedItems[0];
                    if ((string)item.SubItems[1].Text == "FOLDER") return;
                    currentTargetControl.Text = Path.Combine(currentDirectory, item.Text);
                    fileBrowserPanel.Visible = false;
                }
            };
            fileBrowserButtonPanel.Controls.Add(btnSelect);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Width = 100;
            btnCancel.Height = 40;
            btnCancel.Font = new Font("Tahoma", 12, FontStyle.Bold);
            btnCancel.BackColor = Color.FromArgb(45,45,48);
            btnCancel.ForeColor = Color.White;
            btnCancel.Click += (s, e) => { fileBrowserPanel.Visible = false; };
            fileBrowserButtonPanel.Controls.Add(btnCancel);

            fileBrowserLayout.Controls.Add(fileBrowserButtonPanel, 0, 2);

            this.Controls.Add(fileBrowserPanel);
            fileBrowserPanel.BringToFront();
        }

        private void RefreshFileBrowserList()
        {
            fileBrowserPathEntry.Text = currentDirectory;
            fileBrowserList.Items.Clear();
            try
            {
                foreach (string dir in Directory.GetDirectories(currentDirectory))
                {
                    ListViewItem item = new ListViewItem(Path.GetFileName(dir));
                    item.SubItems.Add("FOLDER");
                    fileBrowserList.Items.Add(item);
                }
                foreach (string file in Directory.GetFiles(currentDirectory))
                {
                    ListViewItem item = new ListViewItem(Path.GetFileName(file));
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    string fileType;
                    switch (ext)
                    {
                        case ".txt":
                            fileType = "Text Document";
                            break;
                        case ".cs":
                            fileType = "C# Source File";
                            break;
                        case ".exe":
                            fileType = "Executable";
                            break;
                        case ".dll":
                            fileType = "Dynamic Link Library";
                            break;
                        case ".png":
                        case ".jpg":
                        case ".jpeg":
                        case ".gif":
                            fileType = "Image File";
                            break;
                        case ".ini":
                            fileType = "Configuration File";
                            break;
                        default:
                            fileType = !string.IsNullOrEmpty(ext) ? ext.ToUpper() + " File" : "Unknown File";
                            break;
                    }
                    item.SubItems.Add(fileType);
                    fileBrowserList.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                ListViewItem errorItem = new ListViewItem("Error: " + ex.Message);
                errorItem.SubItems.Add("");
                fileBrowserList.Items.Add(errorItem);
            }
        }

        private void ShowIntegratedFileBrowser(Control target)
        {
            currentTargetControl = target;
            currentDirectory = Environment.CurrentDirectory;
            RefreshFileBrowserList();
            fileBrowserPanel.Visible = true;
            fileBrowserPanel.BringToFront();
            fileBrowserList.Focus();
        }

        private void IntegratedBrowse(Control target)
        {
            ShowIntegratedFileBrowser(target);
        }

        private void AttachFocusHandlers(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                ctrl.Enter += (s, e) => UpdateArrowIndicator();
                if (ctrl.Controls.Count > 0)
                    AttachFocusHandlers(ctrl);
            }
        }

        // Update diamond indicator position.
        private void UpdateArrowIndicator()
        {
            if (arrowIndicator == null)
                return;
            try
            {
                Control active = GetDeepActiveControl(this);
                if (active == null)
                    active = btnLaunch;
                int margin = 5;
                int arrowX, arrowY;
                if (active is CheckBox)
                    arrowX = active.Right + margin;
                else
                {
                    arrowX = active.Left - arrowIndicator.Width - margin;
                    if (arrowX < 0)
                        arrowX = active.Right + margin;
                }
                arrowY = active.Top + (active.Height - arrowIndicator.Height) / 2;
                arrowX += arrowAnimationOffset;
                arrowIndicator.Location = new Point(arrowX, arrowY);
                arrowIndicator.BringToFront();
                arrowIndicator.Visible = true;
            }
            catch { arrowIndicator.Visible = false; }
        }

        private Control GetDeepActiveControl(Control control)
        {
            while (control is ContainerControl container && container.ActiveControl != null)
                control = container.ActiveControl;
            return control;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (overlayPanel.Visible)
                {
                    HideOverlay();
                    e.SuppressKeyPress = true;
                    return;
                }
                else if (chkTVMode.Checked)
                {
                    chkTVMode.Checked = false;
                    e.SuppressKeyPress = true;
                    return;
                }
                else
                {
                    this.Close();
                }
            }
            if (e.KeyCode == Keys.X)
            {
                SendKeys.Send("{UP}");
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.B)
            {
                if (overlayPanel.Visible)
                {
                    HideOverlay();
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (chkTVMode.Checked)
            {
                int headerHeight = (int)(this.ClientSize.Height * 0.15);
                int infoHeight = (int)(this.ClientSize.Height * 0.20);
                foreach (Control ctrl in tvModePanel.Controls)
                {
                    if (ctrl is Panel)
                    {
                        if (ctrl.Name == "tvHeaderPanel")
                            ctrl.Height = headerHeight;
                        else if (ctrl.Name == "tvInfoPanel")
                            ctrl.Height = infoHeight;
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "ChooChoo - Trainer/DLL Loader";
            this.ClientSize = new Size(1200, 750);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ForeColor = Color.White;
            arrowIndicator = new Label()
            {
                Text = "◆",
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };
            this.Controls.Add(arrowIndicator);
            arrowIndicator.BringToFront();
        }

        private void ArrowAnimationTimer_Tick(object sender, EventArgs e)
        {
            arrowAnimationTime += arrowAnimationTimer.Interval / 1000.0;
            arrowAnimationOffset = (int)(arrowAnimationAmplitude * Math.Sin(2 * Math.PI * arrowAnimationTime / arrowAnimationPeriod));
            UpdateArrowIndicator();
        }

        private void UpdateAdditionalInjection(int index)
        {
            string file = comboAdditional[index].Text.Trim();
            if (Path.GetExtension(file).ToLowerInvariant() != ".dll")
            {
                additionalInjectedFile[index] = null;
                chkAdditional[index].Text = "Launch/Inject";
                return;
            }
            chkAdditional[index].Text = "Inject";
            if (!chkAdditional[index].Checked)
            {
                if (!string.IsNullOrEmpty(additionalInjectedFile[index]) &&
                    validatedDlls.ContainsKey(additionalInjectedFile[index]))
                {
                    LogStatus($"DLL {Path.GetFileName(additionalInjectedFile[index])} UNLOADED from {gameProcess?.ProcessName ?? "N/A"}!");
                    validatedDlls.Remove(additionalInjectedFile[index]);
                    additionalInjectedFile[index] = null;
                    RefreshDllList();
                }
                return;
            }
            if (additionalInjectedFile[index] != file)
            {
                if (!string.IsNullOrEmpty(additionalInjectedFile[index]) &&
                    validatedDlls.ContainsKey(additionalInjectedFile[index]))
                {
                    LogStatus($"DLL {Path.GetFileName(additionalInjectedFile[index])} UNLOADED from {gameProcess?.ProcessName ?? "N/A"}!");
                    validatedDlls.Remove(additionalInjectedFile[index]);
                }
            }
            additionalInjectedFile[index] = file;
            if (!File.Exists(file))
            {
                if (!validatedDlls.ContainsKey(file))
                {
                    LogStatus($"DLL {Path.GetFileName(file)} NOT FOUND!");
                    validatedDlls[file] = false;
                }
                return;
            }
            if (Environment.Is64BitProcess)
            {
                if (!IsDll64Bit(file))
                {
                    if (!validatedDlls.ContainsKey(file))
                    {
                        LogStatus($"DLL {Path.GetFileName(file)} IS 32-BIT; SKIPPING INJECTION.");
                        validatedDlls[file] = false;
                    }
                    return;
                }
            }
            if (Path.GetExtension(file).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                if (!validatedDlls.ContainsKey(file))
                {
                    validatedDlls[file] = true;
                    LogStatus($"DLL {Path.GetFileName(file)} IS VALID and will be injected into {gameProcess?.ProcessName ?? "N/A"}.");
                }
            }
            else
            {
                if (!validatedDlls.ContainsKey(file))
                {
                    LogStatus($"DLL {Path.GetFileName(file)} IS INVALID!");
                    validatedDlls[file] = false;
                }
            }
            RefreshDllList();
        }

        private bool IsDll64Bit(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peOffset = br.ReadInt32();
                    fs.Seek(peOffset, SeekOrigin.Begin);
                    uint peHead = br.ReadUInt32();
                    ushort machine = br.ReadUInt16();
                    return machine == 0x8664;
                }
            }
            catch { return false; }
        }

        private void BtnFreezeProcess_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            try
            {
                foreach (ProcessThread thread in gameProcess.Threads)
                {
                    IntPtr hThread = NativeMethods.OpenThread(NativeMethods.THREAD_SUSPEND_RESUME, false, (uint)thread.Id);
                    if (hThread != IntPtr.Zero)
                    {
                        uint suspendCount = NativeMethods.SuspendThread(hThread);
                        LogTool($"Suspended thread {thread.Id} (count {suspendCount}).");
                        NativeMethods.CloseHandle(hThread);
                    }
                }
                LogTool("Process frozen.");
            }
            catch (Exception ex)
            {
                LogTool("Freeze error: " + ex.Message);
            }
        }

        private void BtnUnfreezeProcess_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            try
            {
                foreach (ProcessThread thread in gameProcess.Threads)
                {
                    IntPtr hThread = NativeMethods.OpenThread(NativeMethods.THREAD_SUSPEND_RESUME, false, (uint)thread.Id);
                    if (hThread != IntPtr.Zero)
                    {
                        int resumeCount = 0;
                        while (NativeMethods.ResumeThread(hThread) > 0)
                        {
                            resumeCount++;
                        }
                        LogTool($"Resumed thread {thread.Id} (resumed {resumeCount} times).");
                        NativeMethods.CloseHandle(hThread);
                    }
                }
                LogTool("Process unfrozen.");
            }
            catch (Exception ex)
            {
                LogTool("Unfreeze error: " + ex.Message);
            }
        }

        private void BtnKillProcess_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            try
            {
                gameProcess.Kill();
                LogTool("Process killed.");
            }
            catch (Exception ex)
            {
                LogTool("Kill error: " + ex.Message);
            }
        }

        private void BtnDumpProcess_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Minidump Files (*.dmp)|*.dmp";
            sfd.FileName = gameProcess.ProcessName + "_" + gameProcess.Id + ".dmp";
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                LogTool("Dump cancelled.");
                return;
            }
            using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                bool result = NativeMethods.MiniDumpWriteDump(
                    gameProcess.Handle,
                    (uint)gameProcess.Id,
                    fs.SafeFileHandle.DangerousGetHandle(),
                    MINIDUMP_TYPE.MiniDumpWithFullMemory,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);
                if (result)
                    LogTool("Minidump written to " + sfd.FileName);
                else
                    LogTool("MiniDumpWriteDump failed: " + Marshal.GetLastWin32Error());
            }
        }

        private void BtnListImports_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            try
            {
                string modulePath = gameProcess.MainModule.FileName;
                using (FileStream fs = new FileStream(modulePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    if (br.ReadUInt16() != 0x5A4D)
                    {
                        LogTool("Not a valid PE file.");
                        return;
                    }
                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peOffset = br.ReadInt32();
                    fs.Seek(peOffset, SeekOrigin.Begin);
                    if (br.ReadUInt32() != 0x00004550)
                    {
                        LogTool("Invalid PE signature.");
                        return;
                    }
                    br.ReadUInt16();
                    br.ReadUInt16();
                    fs.Seek(12, SeekOrigin.Current);
                    ushort optHeaderSize = br.ReadUInt16();
                    fs.Seek(2, SeekOrigin.Current);
                    long optHeaderStart = fs.Position;
                    fs.Seek(optHeaderStart + 96, SeekOrigin.Begin);
                    uint importRVA = br.ReadUInt32();
                    uint importSize = br.ReadUInt32();
                    if (importRVA == 0)
                    {
                        LogTool("No import table.");
                        return;
                    }
                    fs.Seek(importRVA, SeekOrigin.Begin);
                    List<string> imports = new List<string>();
                    while (true)
                    {
                        uint originalFirstThunk = br.ReadUInt32();
                        uint timeDateStamp = br.ReadUInt32();
                        uint forwarderChain = br.ReadUInt32();
                        uint nameRVA = br.ReadUInt32();
                        uint firstThunk = br.ReadUInt32();
                        if (originalFirstThunk == 0 && nameRVA == 0 && firstThunk == 0)
                            break;
                        long currentPos = fs.Position;
                        fs.Seek(nameRVA, SeekOrigin.Begin);
                        string dllName = "";
                        while (true)
                        {
                            char c = (char)br.ReadByte();
                            if (c == '\0') break;
                            dllName += c;
                        }
                        imports.Add(dllName);
                        fs.Seek(currentPos, SeekOrigin.Begin);
                    }
                    txtToolOutput.Text = "Imports:\r\n" + string.Join("\r\n", imports);
                }
            }
            catch (Exception ex)
            {
                LogTool("List Imports error: " + ex.Message);
            }
        }

        private void BtnListRuntimes_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            try
            {
                List<string> runtimes = new List<string>();
                string[] knownRuntimes = { "clr.dll", "coreclr.dll", "mscoree.dll", "mono.dll", "mrt100.dll" };
                foreach (ProcessModule mod in gameProcess.Modules)
                {
                    foreach (string rt in knownRuntimes)
                    {
                        if (mod.ModuleName.Equals(rt, StringComparison.OrdinalIgnoreCase))
                        {
                            runtimes.Add($"{mod.ModuleName} - {mod.FileVersionInfo.FileVersion}");
                        }
                    }
                }
                txtToolOutput.Text = "Runtimes Used:\r\n" + (runtimes.Count > 0 ? string.Join("\r\n", runtimes) : "None detected.");
            }
            catch (Exception ex)
            {
                LogTool("List Runtimes error: " + ex.Message);
            }
        }

        private void BtnSaveState_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            BtnFreezeProcess_Click(sender, e);
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "State Files (*.state)|*.state";
            sfd.FileName = gameProcess.ProcessName + "_" + gameProcess.Id + ".state";
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                LogTool("State save cancelled.");
                BtnUnfreezeProcess_Click(sender, e);
                return;
            }
            try
            {
                List<MemoryRegionDump> regions = new List<MemoryRegionDump>();
                IntPtr addr = IntPtr.Zero;
                MEMORY_BASIC_INFORMATION mbi;
                while (NativeMethods.VirtualQueryEx(gameProcess.Handle, addr, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != IntPtr.Zero)
                {
                    if (mbi.State == 0x1000 && (mbi.Protect & 0x04) != 0)
                    {
                        byte[] buffer = new byte[(int)mbi.RegionSize];
                        UIntPtr bytesRead;
                        if (NativeMethods.ReadProcessMemory(gameProcess.Handle, mbi.BaseAddress, buffer, (uint)buffer.Length, out bytesRead))
                        {
                            regions.Add(new MemoryRegionDump
                            {
                                BaseAddress = mbi.BaseAddress,
                                RegionSize = (uint)buffer.Length,
                                MemoryContents = buffer
                            });
                        }
                    }
                    addr = new IntPtr(mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64());
                }
                using (BinaryWriter bw = new BinaryWriter(File.Open(sfd.FileName, FileMode.Create)))
                {
                    bw.Write(regions.Count);
                    foreach (var region in regions)
                    {
                        bw.Write(region.BaseAddress.ToInt64());
                        bw.Write(region.RegionSize);
                        bw.Write(region.MemoryContents.Length);
                        bw.Write(region.MemoryContents);
                    }
                }
                LogTool("Process state saved to " + sfd.FileName);
            }
            catch (Exception ex)
            {
                LogTool("Save State error: " + ex.Message);
            }
            BtnUnfreezeProcess_Click(sender, e);
        }

        private void BtnLoadState_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            BtnFreezeProcess_Click(sender, e);
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "State Files (*.state)|*.state";
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                LogTool("State load cancelled.");
                BtnUnfreezeProcess_Click(sender, e);
                return;
            }
            try
            {
                using (BinaryReader br = new BinaryReader(File.Open(ofd.FileName, FileMode.Open)))
                {
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        long baseAddrLong = br.ReadInt64();
                        uint regionSize = br.ReadUInt32();
                        int contentLength = br.ReadInt32();
                        byte[] contents = br.ReadBytes(contentLength);
                        IntPtr baseAddress = new IntPtr(baseAddrLong);
                        UIntPtr bytesWritten;
                        bool success = NativeMethods.WriteProcessMemory(gameProcess.Handle, baseAddress, contents, (uint)contents.Length, out bytesWritten);
                        if (!success)
                        {
                            LogTool("WriteProcessMemory failed at " + baseAddress + ": " + Marshal.GetLastWin32Error());
                        }
                    }
                }
                LogTool("Process state loaded from " + ofd.FileName);
            }
            catch (Exception ex)
            {
                LogTool("Load State error: " + ex.Message);
            }
            BtnUnfreezeProcess_Click(sender, e);
        }

        private void BtnListModules_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            try
            {
                string output = "Modules in " + gameProcess.ProcessName + ":\r\n";
                foreach (ProcessModule mod in gameProcess.Modules)
                {
                    output += $"{mod.ModuleName} - Base: 0x{mod.BaseAddress.ToInt64():X}, Size: {mod.ModuleMemorySize} bytes\r\n";
                }
                txtToolOutput.Text = output;
            }
            catch (Exception ex)
            {
                LogTool("List Modules error: " + ex.Message);
            }
        }

        private void BtnListThreads_Click(object sender, EventArgs e)
        {
            if (gameProcess == null)
            {
                LogTool("No target process.");
                return;
            }
            try
            {
                string output = "Threads in " + gameProcess.ProcessName + ":\r\n";
                foreach (ProcessThread t in gameProcess.Threads)
                {
                    output += $"Thread ID: {t.Id}, State: {t.ThreadState}\r\n";
                }
                txtToolOutput.Text = output;
            }
            catch (Exception ex)
            {
                LogTool("List Threads error: " + ex.Message);
            }
        }

        private void InjectionTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 4; i++)
                if (chkAdditional[i].Checked)
                    UpdateAdditionalInjection(i);
        }

        // Profile Handlers.
        private void BtnRefreshProfiles_Click(object sender, EventArgs e)
        {
            UpdateProfileList();
        }

        private void BtnLoadProfile_Click(object sender, EventArgs e)
        {
            string profileName = comboProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Enter a profile name to load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string filePath = Path.Combine(profilesDir, profileName + ".ini");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Profile not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LoadProfile(filePath);
        }

        private void BtnSaveProfile_Click(object sender, EventArgs e)
        {
            string profileName = comboProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Enter a profile name to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Directory.CreateDirectory(profilesDir);
            string filePath = Path.Combine(profilesDir, profileName + ".ini");
            SaveProfile(filePath);
            UpdateProfileList();
        }

        private void BtnDeleteProfile_Click(object sender, EventArgs e)
        {
            string profileName = comboProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Enter a profile name to delete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string filePath = Path.Combine(profilesDir, profileName + ".ini");
            if (File.Exists(filePath))
            {
                var result = MessageBox.Show($"Delete profile '{profileName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    File.Delete(filePath);
                    UpdateProfileList();
                }
            }
            else
                MessageBox.Show("Profile not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            // If in TV mode, exit TV mode first so the launch operation works.
            if (chkTVMode.Checked)
                chkTVMode.Checked = false;

            Directory.CreateDirectory(profilesDir);
            string lastFile = Path.Combine(profilesDir, "last.ini");
            SaveProfile(lastFile);
            txtStatusLog.Clear();
            Process targetProcess = null;
            if (comboRunningExes.SelectedItem != null && comboRunningExes.SelectedItem is Process proc)
            {
                try { proc.Refresh(); } catch { }
                if (!proc.HasExited)
                {
                    targetProcess = proc;
                    LogStatus("Using running process: " + proc.ProcessName + " (PID " + proc.Id + ")");
                }
                else
                {
                    LogStatus("Selected process exited. Launching new instance from Game Path.");
                }
            }
            if (targetProcess == null)
            {
                targetProcess = LaunchProcessFromPath(comboGame.Text, "Game");
            }
            if (targetProcess != null)
                gameProcess = targetProcess;
            LaunchProcessFromPath(comboTrainer.Text, "Trainer");
            for (int i = 0; i < 4; i++)
            {
                string file = comboAdditional[i].Text.Trim();
                if (string.IsNullOrEmpty(file))
                    continue;
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".dll")
                    continue;
                LaunchProcessFromPath(file, $"Additional EXE {i + 1}");
            }
        }

        private Process LaunchProcessFromPath(string path, string label)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (!File.Exists(path))
            {
                LogStatus($"{label}: FAILED (NOT FOUND)");
                return null;
            }
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".dll")
                return null;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = Path.GetDirectoryName(path) ?? "",
                    UseShellExecute = true
                };
                Process proc = Process.Start(psi);
                LogStatus($"{label}: LAUNCHED");
                if (label == "Game")
                    AddRecent(comboGame, path);
                else if (label == "Trainer")
                    AddRecent(comboTrainer, path);
                return proc;
            }
            catch (Exception ex)
            {
                LogStatus($"{label}: FAILED ({ex.Message})");
                return null;
            }
        }

        private void UpdateProfileList()
        {
            comboProfiles.Items.Clear();
            if (Directory.Exists(profilesDir))
            {
                string[] files = Directory.GetFiles(profilesDir, "*.ini");
                foreach (string file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (name.ToLower() != "last")
                        comboProfiles.Items.Add(name);
                }
            }
        }

        private void SaveProfile(string filePath)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine(comboGame.Text);
                sw.WriteLine(comboTrainer.Text);
                for (int i = 0; i < 4; i++)
                    sw.WriteLine((chkAdditional[i].Checked ? "1" : "0") + "," + comboAdditional[i].Text);
                sw.WriteLine("FULLSCREEN=" + chkFullscreen.Checked);
            }
        }

        private void LoadProfile(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                comboGame.Text = sr.ReadLine() ?? "";
                comboTrainer.Text = sr.ReadLine() ?? "";
                for (int i = 0; i < 4; i++)
                {
                    string line = sr.ReadLine();
                    if (line != null)
                    {
                        string[] parts = line.Split(new char[] { ',' }, 2);
                        if (parts.Length == 2)
                        {
                            chkAdditional[i].Checked = parts[0] == "1";
                            comboAdditional[i].Text = parts[1];
                        }
                    }
                }
                string settingsLine = sr.ReadLine();
                if (settingsLine != null && settingsLine.StartsWith("FULLSCREEN=", StringComparison.OrdinalIgnoreCase))
                {
                    string val = settingsLine.Substring("FULLSCREEN=".Length).Trim();
                    bool fs;
                    if (bool.TryParse(val, out fs))
                        chkFullscreen.Checked = fs;
                }
            }
        }

        private void LoadRecentsForCombo(ComboBox combo)
        {
            if (combo == null) return;
            if (File.Exists(recentFile))
            {
                string all = File.ReadAllText(recentFile);
                string[] items = all.Split(';');
                combo.Items.Clear();
                foreach (string item in items)
                {
                    string trimmed = item.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        combo.Items.Add(trimmed);
                }
            }
        }

        private void AddRecent(ComboBox combo, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            if (!combo.Items.Contains(text))
            {
                combo.Items.Insert(0, text);
                while (combo.Items.Count > 10)
                    combo.Items.RemoveAt(combo.Items.Count - 1);
                SaveRecents(combo);
            }
        }

        private void SaveRecents(ComboBox combo)
        {
            string all = "";
            foreach (object item in combo.Items)
                all += (all == "" ? "" : ";") + (item?.ToString() ?? "");
            File.WriteAllText(recentFile, all);
        }

        private void ExportDllList()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Text Files (*.txt)|*.txt";
            sfd.FileName = "LoadedDlls.txt";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.WriteLine("Current loaded DLLs/modules:");
                    foreach (object item in listBoxDlls.Items)
                        sw.WriteLine(item?.ToString() ?? "");
                }
                MessageBox.Show("DLL list exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RefreshDllList()
        {
            int topIndex = listBoxDlls.TopIndex;
            List<string> dllList = new List<string>();
            dllList.Add("Current loaded DLLs/modules:");
            try
            {
                ProcessModuleCollection modules = Process.GetCurrentProcess().Modules;
                foreach (ProcessModule module in modules)
                    dllList.Add(module.ModuleName);
            }
            catch (Exception ex)
            {
                dllList.Add("Error: " + ex.Message);
            }
            foreach (var kvp in validatedDlls)
            {
                if (kvp.Value)
                {
                    string dllName = Path.GetFileName(kvp.Key);
                    if (!dllList.Contains(dllName))
                        dllList.Add(dllName + " (Validated)");
                }
            }
            listBoxDlls.BeginUpdate();
            listBoxDlls.Items.Clear();
            foreach (var item in dllList)
                listBoxDlls.Items.Add(item);
            listBoxDlls.EndUpdate();
            try { listBoxDlls.TopIndex = topIndex; } catch { }
        }

        // --- Xinput Timer: Controller handling.
        // This single definition now handles both TV and Desktop modes.
        private void XinputTimer_Tick(object sender, EventArgs e)
        {
            XINPUT_STATE state;
            uint result = XInput.XInputGetState(0, out state);
            const short thumbDeadzone = 20000;

            // If TV mode is active, lock the controller to the TV menu.
            if (tvModePanel.Visible)
            {
                if (state.Gamepad.sThumbLY < -thumbDeadzone)
                {
                    if (!analogDownSent)
                    {
                        SendKeys.Send("{DOWN}");
                        analogDownSent = true;
                    }
                }
                else
                    analogDownSent = false;

                if (state.Gamepad.sThumbLY > thumbDeadzone)
                {
                    if (!analogUpSent)
                    {
                        SendKeys.Send("{UP}");
                        analogUpSent = true;
                    }
                }
                else
                    analogUpSent = false;

                bool aButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_A) != 0;
                if (aButton && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_A) == 0)
                {
                    SendKeys.Send("{ENTER}");
                }

                if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_START) != 0 &&
                    (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_START) == 0)
                    BtnLaunch_Click(null, EventArgs.Empty);

                prevLeftTrigger = state.Gamepad.bLeftTrigger;
                prevRightTrigger = state.Gamepad.bRightTrigger;
                prevXInputState = state;
                this.Invalidate();
                return;
            }

            // Desktop mode handling.
            if (this.ActiveControl is ComboBox activeCbDropdown && activeCbDropdown.DroppedDown)
            {
                bool dpadDown = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_DOWN) != 0;
                bool dpadUp = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_UP) != 0;
                int currentIndex = activeCbDropdown.SelectedIndex;
                if (dpadDown && currentIndex < activeCbDropdown.Items.Count - 1)
                    activeCbDropdown.SelectedIndex = currentIndex + 1;
                if (dpadUp && currentIndex > 0)
                    activeCbDropdown.SelectedIndex = currentIndex - 1;
                if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_LEFT) != 0 &&
                    (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_LEFT) == 0)
                    SendKeys.Send("{LEFT}");
                if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_RIGHT) != 0 &&
                    (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_RIGHT) == 0)
                    SendKeys.Send("{RIGHT}");
                bool aButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_A) != 0;
                if (aButton && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_A) == 0)
                {
                    if (this.ActiveControl is ComboBox activeCb)
                        activeCb.DroppedDown = true;
                    else if (this.ActiveControl is Button btn)
                        btn.PerformClick();
                    else if (this.ActiveControl is CheckBox chk)
                        chk.Checked = !chk.Checked;
                    else
                        SendKeys.Send("{ENTER}");
                }
            }
            bool xButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_X) != 0;
            if (xButton && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_X) == 0)
                SendKeys.Send("{UP}");
            bool yButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_Y) != 0;
            if (yButton && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_Y) == 0)
            {
                if (fileBrowserPanel.Visible)
                    RefreshFileBrowserList();
                else if (tvModePanel.Visible)
                    RefreshTVInfo();
            }
            bool bButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_B) != 0;
            if (bButton && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_B) == 0)
            {
                if (overlayPanel.Visible)
                    HideOverlay();
                else if (fileBrowserPanel.Visible)
                    fileBrowserPanel.Visible = false;
            }
            if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_START) != 0 &&
                (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_START) == 0)
                BtnLaunch_Click(null, EventArgs.Empty);
            if (state.Gamepad.bLeftTrigger > 30 && prevLeftTrigger <= 30)
                tabControl.SelectedIndex = (tabControl.SelectedIndex - 1 + tabControl.TabCount) % tabControl.TabCount;
            if (state.Gamepad.bRightTrigger > 30 && prevRightTrigger <= 30)
                tabControl.SelectedIndex = (tabControl.SelectedIndex + 1) % tabControl.TabCount;
            if (state.Gamepad.sThumbLY < -thumbDeadzone)
            {
                if (!analogDownSent)
                {
                    SendKeys.Send("{DOWN}");
                    analogDownSent = true;
                }
            }
            else analogDownSent = false;
            if (state.Gamepad.sThumbLY > thumbDeadzone)
            {
                if (!analogUpSent)
                {
                    SendKeys.Send("{UP}");
                    analogUpSent = true;
                }
            }
            else analogUpSent = false;
            if (state.Gamepad.sThumbLX < -thumbDeadzone)
            {
                if (!analogLeftSent)
                {
                    SendKeys.Send("{LEFT}");
                    analogLeftSent = true;
                }
            }
            else analogLeftSent = false;
            if (state.Gamepad.sThumbLX > thumbDeadzone)
            {
                if (!analogRightSent)
                {
                    SendKeys.Send("{RIGHT}");
                    analogRightSent = true;
                }
            }
            else analogRightSent = false;
            prevLeftTrigger = state.Gamepad.bLeftTrigger;
            prevRightTrigger = state.Gamepad.bRightTrigger;
            prevXInputState = state;
            this.Invalidate();
        }

        private void LogStatus(string message)
        {
            txtStatusLog.AppendText(message.ToUpper() + Environment.NewLine);
        }

        private void LogTool(string message)
        {
            txtToolOutput.AppendText(message + Environment.NewLine);
        }

        private void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                string[] lines = File.ReadAllLines(settingsFile);
                foreach (string line in lines)
                {
                    if (line.StartsWith("AUTO-LAUNCH=", StringComparison.OrdinalIgnoreCase))
                    {
                        string val = line.Substring("AUTO-LAUNCH=".Length).Trim();
                        bool auto;
                        if (bool.TryParse(val, out auto))
                            chkAutoLaunch.Checked = auto;
                    }
                    else if (line.StartsWith("FULLSCREEN=", StringComparison.OrdinalIgnoreCase))
                    {
                        string val = line.Substring("FULLSCREEN=".Length).Trim();
                        bool fs;
                        if (bool.TryParse(val, out fs))
                            chkFullscreen.Checked = fs;
                    }
                }
            }
        }

        private void SaveSettings()
        {
            File.WriteAllText(settingsFile, $"AUTO-LAUNCH={chkAutoLaunch.Checked}\r\nFULLSCREEN={chkFullscreen.Checked}");
        }

        private class MemoryRegionDump
        {
            public IntPtr BaseAddress;
            public uint RegionSize;
            public byte[] MemoryContents;
        }

        private void PopulateRunningExes()
        {
            comboRunningExes.Items.Clear();
            LogStatus("Scanning running processes...");
            Process[] procs = ProcessHelper.GetProcesses();
            foreach (Process p in procs)
            {
                try
                {
                    string display = $"{p.ProcessName} (PID {p.Id}) - {p.MainWindowTitle}";
                    comboRunningExes.Items.Add(p);
                    LogStatus("Detected: " + display);
                }
                catch (Exception ex)
                {
                    LogStatus("Error: " + ex.Message);
                }
            }
            if (comboRunningExes.Items.Count == 0)
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(p.MainWindowTitle))
                        {
                            comboRunningExes.Items.Add(p);
                            LogStatus("Detected (fallback): " + p.ProcessName);
                        }
                    }
                    catch { }
                }
            }
            if (comboRunningExes.Items.Count > 0)
                comboRunningExes.SelectedIndex = 0;
        }

        private void ProcessCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                if (arg == "-p" || arg == "--profile")
                {
                    if (i + 1 < args.Length)
                    {
                        string profileName = args[i + 1];
                        comboProfiles.Text = profileName;
                        string filePath = Path.Combine(profilesDir, profileName + ".ini");
                        if (File.Exists(filePath))
                            LoadProfile(filePath);
                        i++;
                    }
                }
                else if (arg == "-autolaunch")
                {
                    chkAutoLaunch.Checked = true;
                }
                else if (arg == "-dllinject")
                {
                    int j = i + 1;
                    int index = 0;
                    while (j < args.Length && !args[j].StartsWith("-") && index < 4)
                    {
                        comboAdditional[index].Text = args[j];
                        chkAdditional[index].Checked = true;
                        index++;
                        j++;
                    }
                    i = j - 1;
                }
            }
            if (chkAutoLaunch.Checked)
            {
                var launchTimer = new WinFormsTimer { Interval = 2000 };
                launchTimer.Tick += (s, e) =>
                {
                    launchTimer.Stop();
                    BtnLaunch_Click(null, EventArgs.Empty);
                };
                launchTimer.Start();
            }
        }

        private void ApplyDarkTheme(Control control)
        {
            if (control is Button btn)
            {
                btn.UseVisualStyleBackColor = false;
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = Color.FromArgb(45,45,48);
                btn.ForeColor = Color.White;
            }
            else if (control is TextBox || control is RichTextBox)
            {
                control.BackColor = Color.FromArgb(30,30,30);
                control.ForeColor = Color.White;
            }
            else if (control is ComboBox || control is CheckBox)
            {
                control.BackColor = Color.FromArgb(45,45,48);
                control.ForeColor = Color.White;
            }
            else
            {
                control.BackColor = Color.FromArgb(45,45,48);
                control.ForeColor = Color.White;
            }
            foreach (Control child in control.Controls)
            {
                child.ForeColor = Color.White;
                ApplyDarkTheme(child);
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
