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
using System.Linq;

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

    // Minidump and native methods.
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
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpAddress, [Out] byte[] lpBuffer, uint dwSize, out UIntPtr lpNumberOfBytesRead);

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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResumeThread(IntPtr hThread);

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

    public static class Win32API
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);
    }

    public partial class MainForm : Form
    {
        // Field declarations
        private Panel overlayPanel;
        private Panel defocusOverlay; // Used only in desktop mode
        private bool isPaused = false; // True when input is paused in desktop mode
        private bool isModeSwitching = false; // Flag to avoid defocus overlay during mode switching
        private Font tvFont;
        private Panel mainPanel;
        private TabControl tabControl;
        private TabPage tabPageMain, tabPageHelp, tabPageTools;
        private CheckBox chkTVMode, chkAutoLaunch;
        private CheckBox chkLoadTVOnLaunch;
        private GroupBox groupPaths, groupProfiles;
        private Label labelRunningExes, labelGamePath, labelTrainerPath;
        private ComboBox comboRunningExes, comboGame, comboTrainer, comboProfiles;
        private Button btnBrowseGame, btnBrowseTrainer;
        private Button btnRefreshProfiles, btnLoadProfile, btnSaveProfile, btnDeleteProfile;
        private CheckBox[] chkAdditional = new CheckBox[4];
        private ComboBox[] comboAdditional = new ComboBox[4];
        private Button[] btnBrowseAdditional = new Button[4];
        private TextBox txtStatusLog, txtToolOutput;
        private ListBox listBoxDlls;
        private Button btnLaunch;
        private GroupBox groupTrainerMethods;
        private FlowLayoutPanel flpTrainerMethods;
        private RadioButton[] radioTrainerMethods = new RadioButton[6];
        private Panel tvModePanel;
        private ListView tvModeList;
        private TextBox tvInfoText; // Multiline TextBox per old logic
        private Panel fileBrowserPanel;
        private TableLayoutPanel fileBrowserLayout;
        private TextBox fileBrowserPathEntry;
        private ListView fileBrowserList;
        private FlowLayoutPanel fileBrowserButtonPanel;
        private Button btnUp, btnRefresh, btnSelect, btnCancel;
        private Control currentTargetControl;
        private string currentDirectory;
        private string recentFile = "recent.ini";
        private string profilesDir = "profiles";
        private string settingsFile = "settings.ini";
        private Process gameProcess;
        private XINPUT_STATE prevXInputState;
        private byte prevLeftTrigger, prevRightTrigger;
        private double arrowAnimationTime;
        private int arrowAnimationOffset;
        private int arrowAnimationAmplitude = 3;
        private double arrowAnimationPeriod = 3.0;
        private WinFormsTimer arrowAnimationTimer;
        private string[] additionalInjectedFile = new string[4];
        private Dictionary<string, bool> validatedDlls = new Dictionary<string, bool>();
        private bool analogDownSent, analogUpSent, analogLeftSent, analogRightSent;
        private GlobalHook globalHook;
        private GlobalMouseHook globalMouseHook;
        private Cursor myCursor;
        private Label arrowIndicator;
        private Rectangle originalBounds = Rectangle.Empty;
        // Freeze game state
        private bool gameFrozen = false;

        protected override CreateParams CreateParams
        {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00040000;
                return cp;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void OverlayPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                HideOverlay();
                e.Handled = true;
            }
        }

        // In both desktop and TV modes, when focus is lost, show the defocus overlay.
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (!isModeSwitching)
                PauseInput();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            ResumeInput();
            ReleaseCapture();
            Cursor.Clip = Rectangle.Empty;
            Cursor.Current = myCursor;
            Cursor.Show();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SETCURSOR = 0x20;
            const int WM_MOUSEACTIVATE = 0x21;
            if (m.Msg == WM_SETCURSOR)
            {
                Cursor.Current = myCursor;
                m.Result = new IntPtr(1);
                return;
            }
            else if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = new IntPtr(1);
                return;
            }
            base.WndProc(ref m);
        }

        // Show defocus overlay in desktop mode (and in TV mode if defocused).
        private void PauseInput()
        {
            if (!isPaused)
            {
                isPaused = true;
                if (defocusOverlay == null)
                    InitializeDefocusOverlay();
                defocusOverlay.Visible = true;
                defocusOverlay.BringToFront();
                if (globalHook != null) { globalHook.Dispose(); globalHook = null; }
                if (globalMouseHook != null) { globalMouseHook.Dispose(); globalMouseHook = null; }
            }
        }

        private void ResumeInput()
        {
            if (isPaused)
            {
                isPaused = false;
                defocusOverlay.Visible = false;
                globalHook = new GlobalHook();
                globalHook.KeyPressed += (s, e) => { /* global keyboard hook logic */ };
                globalMouseHook = new GlobalMouseHook();
                globalMouseHook.MouseAction += (s, e) => { /* global mouse hook logic */ };
            }
        }

        private void InitializeDefocusOverlay()
        {
            defocusOverlay = new Panel();
            defocusOverlay.Dock = DockStyle.Fill;
            defocusOverlay.BackColor = Color.Gray;
            Label resumeLabel = new Label();
            resumeLabel.Text = "CLICK TO RESUME";
            resumeLabel.Font = new Font("Tahoma", 36, FontStyle.Bold);
            resumeLabel.ForeColor = Color.White;
            resumeLabel.Dock = DockStyle.Fill;
            resumeLabel.TextAlign = ContentAlignment.MiddleCenter;
            defocusOverlay.Controls.Add(resumeLabel);
            defocusOverlay.Visible = false;
            resumeLabel.Click += (s, e) => { ResumeInput(); };
            // Removed the defocusOverlay.Click handler to prevent accidental toggling on any click.
            this.Controls.Add(defocusOverlay);
            defocusOverlay.BringToFront();
        }

        public MainForm()
        {
            this.Text = "ChooChoo Injection Engine";
            this.ShowInTaskbar = true;
            this.Owner = null;
            this.Font = new Font("Tahoma", 9);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(1200,750);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            originalBounds = this.Bounds;

            try { myCursor = new Cursor("cursor.cur"); } catch { myCursor = Cursors.Default; }

            Directory.CreateDirectory(profilesDir);
            overlayPanel = new Panel();
            overlayPanel.Dock = DockStyle.Fill;
            overlayPanel.BackColor = Color.FromArgb(60,60,60);
            overlayPanel.Visible = false;
            overlayPanel.KeyDown += OverlayPanel_KeyDown;
            this.Controls.Add(overlayPanel);
            overlayPanel.BringToFront();

            try {
                PrivateFontCollection pfc = new PrivateFontCollection();
                string fontPath = Path.Combine(Application.StartupPath, "Fonts", "MotivaSans.ttf");
                pfc.AddFontFile(fontPath);
                tvFont = new Font(pfc.Families[0], 24, FontStyle.Bold);
            } catch {
                tvFont = new Font("Tahoma", 24, FontStyle.Bold);
            }

            SetupFileBrowserPanel();
            SetupTVModePanel();

            mainPanel = new Panel() { Dock = DockStyle.Fill };
            this.Controls.Add(mainPanel);
            tabControl = new TabControl() { Dock = DockStyle.Fill, ForeColor = Color.White };
            tabControl.ItemSize = new Size(100, 25);
            tabControl.SizeMode = TabSizeMode.Fixed;
            mainPanel.Controls.Add(tabControl);
            tabPageMain = new TabPage("Main") { ForeColor = Color.White };
            tabPageHelp = new TabPage("Help") { ForeColor = Color.White };
            tabPageTools = new TabPage("Tools") { ForeColor = Color.White };
            tabControl.TabPages.Add(tabPageMain);
            tabControl.TabPages.Add(tabPageHelp);
            tabControl.TabPages.Add(tabPageTools);

            Panel mainTabPanel = new Panel() { Dock = DockStyle.Fill };
            tabPageMain.Controls.Add(mainTabPanel);

            chkLoadTVOnLaunch = new CheckBox() { Text = "Load TV Mode on Launch", Location = new Point(10,10), AutoSize = true, ForeColor = Color.White };
            mainTabPanel.Controls.Add(chkLoadTVOnLaunch);

            chkTVMode = new CheckBox() { Text = "TV Mode", Location = new Point(200,10), AutoSize = true, ForeColor = Color.White };
            chkTVMode.CheckedChanged += (s,e) =>
            {
                this.ToggleTVMode();
                this.TopMost = chkTVMode.Checked;
            };
            mainTabPanel.Controls.Add(chkTVMode);

            groupPaths = new GroupBox() { Text = "Paths & Process Selection", Bounds = new Rectangle(10,40,850,320), ForeColor = Color.White, BackColor = Color.FromArgb(45,45,48) };
            mainTabPanel.Controls.Add(groupPaths);

            labelRunningExes = new Label() { Text = "Running Exe (Optional):", Location = new Point(20,25), AutoSize = true, ForeColor = Color.White };
            groupPaths.Controls.Add(labelRunningExes);

            comboRunningExes = new ComboBox() { Location = new Point(200,20), Size = new Size(500,25), DropDownStyle = ComboBoxStyle.DropDownList, ForeColor = Color.White };
            // The "NONE" item is added only to the process list.
            comboRunningExes.Items.Clear();
            comboRunningExes.Items.Add("NONE");
            comboRunningExes.DrawMode = DrawMode.OwnerDrawFixed;
            comboRunningExes.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0)
                {
                    object item = comboRunningExes.Items[e.Index];
                    string text = item is Process proc ? $"{proc.ProcessName} (PID {proc.Id})" : item.ToString();
                    SizeF textSize = e.Graphics.MeasureString(text, comboRunningExes.Font);
                    float x = e.Bounds.Left + (e.Bounds.Width - textSize.Width) / 2;
                    float y = e.Bounds.Top + (e.Bounds.Height - textSize.Height) / 2;
                    using (SolidBrush brush = new SolidBrush(e.ForeColor))
                    {
                        e.Graphics.DrawString(text, comboRunningExes.Font, brush, x, y);
                    }
                }
                e.DrawFocusRectangle();
            };
            groupPaths.Controls.Add(comboRunningExes);
            comboRunningExes.SelectedIndexChanged += (s,e) =>
            {
                if (comboRunningExes.SelectedItem is Process proc)
                {
                    gameProcess = proc;
                    LogStatus("Process selected: " + proc.ProcessName + " (PID " + proc.Id + ")");
                }
                else
                {
                    gameProcess = null;
                }
            };

            labelGamePath = new Label() { Text = "Game Path (Optional):", Location = new Point(20,65), AutoSize = true, ForeColor = Color.White };
            groupPaths.Controls.Add(labelGamePath);

            comboGame = new ComboBox() { Location = new Point(200,60), Size = new Size(430,25), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
            groupPaths.Controls.Add(comboGame);

            btnBrowseGame = CreateFlatButton("Browse...");
            btnBrowseGame.Location = new Point(640,60);
            btnBrowseGame.Size = new Size(100,25);
            btnBrowseGame.Click += (s,e) => IntegratedBrowse(comboGame);
            groupPaths.Controls.Add(btnBrowseGame);

            labelTrainerPath = new Label() { Text = "Trainer Path:", Location = new Point(20,105), AutoSize = true, ForeColor = Color.White };
            groupPaths.Controls.Add(labelTrainerPath);

            comboTrainer = new ComboBox() { Location = new Point(130,100), Size = new Size(500,25), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
            groupPaths.Controls.Add(comboTrainer);

            btnBrowseTrainer = CreateFlatButton("Browse...");
            btnBrowseTrainer.Location = new Point(640,100);
            btnBrowseTrainer.Size = new Size(100,25);
            btnBrowseTrainer.Click += (s,e) => IntegratedBrowse(comboTrainer);
            groupPaths.Controls.Add(btnBrowseTrainer);

            for (int i = 0; i < 4; i++)
            {
                int y = 145 + i * 35;
                chkAdditional[i] = new CheckBox() { Text = "Launch/Inject (Optional)", Location = new Point(20,y), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft };
                groupPaths.Controls.Add(chkAdditional[i]);
                comboAdditional[i] = new ComboBox() { Location = new Point(130,y), Size = new Size(500,25), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
                groupPaths.Controls.Add(comboAdditional[i]);
                btnBrowseAdditional[i] = CreateFlatButton("Browse...");
                btnBrowseAdditional[i].Location = new Point(640,y);
                btnBrowseAdditional[i].Size = new Size(100,25);
                int idxLocal = i;
                btnBrowseAdditional[i].Click += (s,e) => IntegratedBrowse(comboAdditional[idxLocal]);
                groupPaths.Controls.Add(btnBrowseAdditional[i]);
            }

            chkAutoLaunch = new CheckBox() { Text = "Save Settings & Enable Autolaunch", Location = new Point(10,groupPaths.Bottom+10), AutoSize = true, ForeColor = Color.White };
            mainTabPanel.Controls.Add(chkAutoLaunch);

            groupProfiles = new GroupBox() { Text = "Profiles", Bounds = new Rectangle(870,40,300,300), ForeColor = Color.White, BackColor = Color.FromArgb(45,45,48) };
            mainTabPanel.Controls.Add(groupProfiles);

            comboProfiles = new ComboBox() { Size = new Size(280,25), Location = new Point((groupProfiles.ClientSize.Width-280)/2,40), DropDownStyle = ComboBoxStyle.DropDown, ForeColor = Color.White };
            groupProfiles.Controls.Add(comboProfiles);

            btnRefreshProfiles = CreateFlatButton("Refresh");
            btnRefreshProfiles.Size = new Size(80,30);
            btnRefreshProfiles.Location = new Point(65,80);
            btnRefreshProfiles.Click += BtnRefreshProfiles_Click;
            groupProfiles.Controls.Add(btnRefreshProfiles);

            btnLoadProfile = CreateFlatButton("Load");
            btnLoadProfile.Size = new Size(80,30);
            btnLoadProfile.Location = new Point(65+80+10,80);
            btnLoadProfile.Click += BtnLoadProfile_Click;
            groupProfiles.Controls.Add(btnLoadProfile);

            btnSaveProfile = CreateFlatButton("Save");
            btnSaveProfile.Size = new Size(170,30);
            btnSaveProfile.Location = new Point((groupProfiles.ClientSize.Width-170)/2,120);
            btnSaveProfile.Click += BtnSaveProfile_Click;
            groupProfiles.Controls.Add(btnSaveProfile);

            btnDeleteProfile = CreateFlatButton("Delete");
            btnDeleteProfile.Size = new Size(170,30);
            btnDeleteProfile.Location = new Point((groupProfiles.ClientSize.Width-170)/2,160);
            btnDeleteProfile.Click += BtnDeleteProfile_Click;
            groupProfiles.Controls.Add(btnDeleteProfile);

            txtStatusLog = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Bounds = new Rectangle(10, chkAutoLaunch.Bottom+10, 850,150), BackColor = Color.FromArgb(30,30,30), ForeColor = Color.White };
            mainTabPanel.Controls.Add(txtStatusLog);

            listBoxDlls = new ListBox() { Location = new Point(txtStatusLog.Right+10, txtStatusLog.Top), Size = new Size(300,txtStatusLog.Height), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Tahoma",10), BackColor = Color.FromArgb(45,45,48), ForeColor = Color.White };
            mainTabPanel.Controls.Add(listBoxDlls);

            Panel panelLaunch = new Panel() { Location = new Point(10, txtStatusLog.Bottom+40), Size = new Size(1170,50) };
            mainTabPanel.Controls.Add(panelLaunch);

            btnLaunch = CreateFlatButton("Launch");
            btnLaunch.Size = new Size(panelLaunch.Width, panelLaunch.Height);
            btnLaunch.Location = new Point(0,0);
            btnLaunch.Font = new Font("Tahoma",14,FontStyle.Bold);
            btnLaunch.Click += BtnLaunch_Click;
            panelLaunch.Controls.Add(btnLaunch);

            groupTrainerMethods = new GroupBox() { Text = "Trainer Launch Methods (Optional)", Bounds = new Rectangle(10, panelLaunch.Bottom+10, 1170, 60), BackColor = Color.FromArgb(45,45,48), ForeColor = Color.White };
            mainTabPanel.Controls.Add(groupTrainerMethods);
            flpTrainerMethods = new FlowLayoutPanel() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            groupTrainerMethods.Controls.Add(flpTrainerMethods);
            string[] methodNames = { "P/Invoke CreateProcess", "CMD Start", "CreateThread Injection", "Remote Thread Injection", "Shell Execute", "Raw Process.Start" };
            for (int i = 0; i < 6; i++)
            {
                radioTrainerMethods[i] = new RadioButton() { Text = methodNames[i], AutoSize = true, BackColor = Color.FromArgb(30,30,30), ForeColor = Color.White, Margin = new Padding(5) };
                flpTrainerMethods.Controls.Add(radioTrainerMethods[i]);
            }
            radioTrainerMethods[0].Checked = true;

            // Help tab.
            HelpTab helpControl = new HelpTab() { Dock = DockStyle.Fill };
            tabPageHelp.Controls.Add(helpControl);

            // Tools tab.
            int tbtnWidth = 130, tbtnHeight = 30, tpadding = 10;
            Button btnFreezeProcess = CreateFlatButton("Freeze Proc");
            btnFreezeProcess.Location = new Point(tpadding, tpadding);
            btnFreezeProcess.Size = new Size(tbtnWidth, tbtnHeight);
            btnFreezeProcess.Click += BtnFreezeProcess_Click;
            tabPageTools.Controls.Add(btnFreezeProcess);

            Button btnUnfreezeProcess = CreateFlatButton("Unfreeze Proc");
            btnUnfreezeProcess.Location = new Point(tpadding*2+tbtnWidth, tpadding);
            btnUnfreezeProcess.Size = new Size(tbtnWidth, tbtnHeight);
            btnUnfreezeProcess.Click += BtnUnfreezeProcess_Click;
            tabPageTools.Controls.Add(btnUnfreezeProcess);

            Button btnKillProcess = CreateFlatButton("Kill Proc");
            btnKillProcess.Location = new Point(tpadding*3+tbtnWidth*2, tpadding);
            btnKillProcess.Size = new Size(tbtnWidth, tbtnHeight);
            btnKillProcess.Click += BtnKillProcess_Click;
            tabPageTools.Controls.Add(btnKillProcess);

            Button btnDumpProcess = CreateFlatButton("Dump Proc");
            btnDumpProcess.Location = new Point(tpadding*4+tbtnWidth*3, tpadding);
            btnDumpProcess.Size = new Size(tbtnWidth, tbtnHeight);
            btnDumpProcess.Click += BtnDumpProcess_Click;
            tabPageTools.Controls.Add(btnDumpProcess);

            Button btnListImports = CreateFlatButton("List Imports");
            btnListImports.Location = new Point(tpadding*5+tbtnWidth*4, tbtnHeight/2);
            btnListImports.Size = new Size(tbtnWidth, tbtnHeight);
            btnListImports.Click += BtnListImports_Click;
            tabPageTools.Controls.Add(btnListImports);

            Button btnListRuntimes = CreateFlatButton("List Runtimes");
            btnListRuntimes.Location = new Point(tpadding, tpadding*2+tbtnHeight);
            btnListRuntimes.Size = new Size(tbtnWidth, tbtnHeight);
            btnListRuntimes.Click += BtnListRuntimes_Click;
            tabPageTools.Controls.Add(btnListRuntimes);

            Button btnSaveState = CreateFlatButton("Save State");
            btnSaveState.Location = new Point(tpadding*2+tbtnWidth, tpadding*2+tbtnHeight);
            btnSaveState.Size = new Size(tbtnWidth, tbtnHeight);
            btnSaveState.Click += BtnSaveState_Click;
            tabPageTools.Controls.Add(btnSaveState);

            Button btnLoadState = CreateFlatButton("Load State");
            btnLoadState.Location = new Point(tpadding*3+tbtnWidth*2, tpadding*2+tbtnHeight);
            btnLoadState.Size = new Size(tbtnWidth, tbtnHeight);
            btnLoadState.Click += BtnLoadState_Click;
            tabPageTools.Controls.Add(btnLoadState);

            Button btnListModules = CreateFlatButton("List Modules");
            btnListModules.Location = new Point(tpadding*4+tbtnWidth*3, tpadding*2+tbtnHeight);
            btnListModules.Size = new Size(tbtnWidth, tbtnHeight);
            btnListModules.Click += BtnListModules_Click;
            tabPageTools.Controls.Add(btnListModules);

            Button btnListThreads = CreateFlatButton("List Threads");
            btnListThreads.Location = new Point(tpadding*5+tbtnWidth*4, tpadding*2+tbtnHeight);
            btnListThreads.Size = new Size(tbtnWidth, tbtnHeight);
            btnListThreads.Click += BtnListThreads_Click;
            tabPageTools.Controls.Add(btnListThreads);

            txtToolOutput = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(tpadding, tpadding*3+tbtnHeight*2), Size = new Size(1150,500), BackColor = Color.FromArgb(30,30,30), ForeColor = Color.White };
            tabPageTools.Controls.Add(txtToolOutput);

            WinFormsTimer xinputTimer = new WinFormsTimer() { Interval = 16 };
            xinputTimer.Tick += XinputTimer_Tick;
            xinputTimer.Start();

            WinFormsTimer injectionTimer = new WinFormsTimer() { Interval = 1000 };
            injectionTimer.Tick += InjectionTimer_Tick;
            injectionTimer.Start();

            arrowAnimationTimer = new WinFormsTimer() { Interval = 50 };
            arrowAnimationTimer.Tick += ArrowAnimationTimer_Tick;
            arrowAnimationTimer.Start();

            arrowIndicator = new Label();
            arrowIndicator.Text = "◆";
            arrowIndicator.Font = new Font("Tahoma",12,FontStyle.Bold);
            arrowIndicator.AutoSize = true;
            arrowIndicator.ForeColor = Color.LightGray;
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

            chkAutoLaunch.CheckedChanged += (s,e) =>
            {
                SaveSettings();
                LogStatus(chkAutoLaunch.Checked ? "AUTO-LAUNCH ENABLED" : "AUTO-LAUNCH DISABLED");
            };

            RefreshDllList();
            PopulateRunningExes();
            comboRunningExes.SelectedIndexChanged += (s,e) =>
            {
                if (comboRunningExes.SelectedItem is Process proc)
                {
                    gameProcess = proc;
                    LogStatus("Process selected: " + proc.ProcessName + " (PID " + proc.Id + ")");
                }
            };

            {
                string lastProfile = Path.Combine(profilesDir, "last.ini");
                if (File.Exists(lastProfile))
                    LoadProfile(lastProfile);
            }

            XINPUT_STATE state;
            if (XInput.XInputGetState(0, out state) == 0)
                LogStatus("CONTROLLER DETECTED");

            globalHook = new GlobalHook();
            globalHook.KeyPressed += (s,e) => { /* global keyboard hook logic */ };
            globalMouseHook = new GlobalMouseHook();
            globalMouseHook.MouseAction += (s,e) => { /* global mouse hook logic */ };

            this.Resize += MainForm_Resize;
            this.FormClosing += (s,e) =>
            {
                SaveSettings();
                if (globalHook != null) globalHook.Dispose();
                if (globalMouseHook != null) globalMouseHook.Dispose();
            };
            ApplyDarkTheme(this);
            LoadSettings();
            if (chkLoadTVOnLaunch.Checked)
                chkTVMode.Checked = true;
            ProcessCommandLineArgs();

            this.Shown += (s,e) =>
            {
                this.Activate();
                if (chkTVMode.Checked && tvModeList != null && tvModeList.Items.Count > 0)
                {
                    tvModeList.SelectedIndices.Clear();
                    tvModeList.SelectedIndices.Add(0);
                    tvModeList.Focus();
                }
            };
        }

        private void XinputTimer_Tick(object sender, EventArgs e)
        {
            // If defocus overlay is active, block all input processing.
            if (defocusOverlay != null && defocusOverlay.Visible)
                return;

            XINPUT_STATE state;
            uint result = XInput.XInputGetState(0, out state);
            const short thumbDeadzone = 20000;
            if (tvModePanel != null && tvModePanel.Visible)
            {
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
                bool aButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_A) != 0;
                if (aButton && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_A) == 0)
                {
                    if (tvModeList != null && tvModeList.SelectedItems.Count > 0)
                    {
                        string action = tvModeList.SelectedItems[0].Text;
                        HandleTVModeAction(action);
                    }
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
                else if (tvModePanel != null && tvModePanel.Visible)
                    RefreshTVInfo();
            }
            bool bButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_B) != 0;
            if (bButton && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_B) == 0)
            {
                if (overlayPanel != null && overlayPanel.Visible)
                    HideOverlay();
                else if (fileBrowserPanel != null && fileBrowserPanel.Visible)
                    fileBrowserPanel.Visible = false;
                else if (chkTVMode.Checked)
                    chkTVMode.Checked = false;
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

        public void RefreshTVInfo()
        {
            string configInfo = "Game Path (Optional): " + comboGame.Text + Environment.NewLine +
                                "Trainer Path: " + comboTrainer.Text + Environment.NewLine;
            for (int i = 0; i < 4; i++)
            {
                configInfo += $"Additional {i+1} (Optional): {(string.IsNullOrEmpty(comboAdditional[i].Text) ? "[Not Set]" : comboAdditional[i].Text)} - {(chkAdditional[i].Checked ? "Inject" : "Launch/Inject (Optional)")}" + Environment.NewLine;
            }
            configInfo += Environment.NewLine + "Recent Log:" + Environment.NewLine + txtStatusLog.Text;
            if(tvInfoText != null)
                tvInfoText.Text = configInfo;
            else
                LogStatus("tvInfoText is null in RefreshTVInfo");
        }

        // ToggleTVMode switches between TV and desktop modes.
        private void ToggleTVMode()
        {
            isModeSwitching = true;
            if (chkTVMode.Checked)
            {
                if (tvModePanel != null)
                {
                    this.Controls.Remove(tvModePanel);
                    tvModePanel.Dispose();
                    tvModePanel = null;
                }
                SetupTVModePanel();
                this.FormBorderStyle = FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;
                this.TopMost = true;
                if (mainPanel != null)
                    mainPanel.Visible = false;
                tvModePanel.Visible = true;
                tvModePanel.BringToFront();
                if (defocusOverlay != null)
                    defocusOverlay.BringToFront();
                if (tvModeList != null && tvModeList.Items.Count > 0)
                {
                    tvModeList.SelectedIndices.Clear();
                    tvModeList.SelectedIndices.Add(0);
                    tvModeList.Focus();
                }
            }
            else
            {
                if (tvModePanel != null)
                {
                    tvModePanel.Visible = false;
                    this.Controls.Remove(tvModePanel);
                    tvModePanel.Dispose();
                    tvModePanel = null;
                }
                if (mainPanel != null)
                {
                    mainPanel.Visible = true;
                    mainPanel.BringToFront();
                }
                this.TopMost = false;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                if (originalBounds != Rectangle.Empty)
                    this.Bounds = originalBounds;
                this.WindowState = FormWindowState.Normal;
                this.ClientSize = new Size(1200,750);
            }
            isModeSwitching = false;
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
                case "PAUSE GAME [BETA]":
                    if (gameProcess == null)
                    {
                        LogStatus("No game process to pause.");
                    }
                    else
                    {
                        if (!gameFrozen)
                        {
                            if (FreezeGame())
                            {
                                gameFrozen = true;
                                LogStatus("Game paused.");
                            }
                        }
                        else
                        {
                            UnfreezeGame();
                            gameFrozen = false;
                            LogStatus("Game resumed.");
                        }
                    }
                    break;
                case "Exit TV Mode":
                    chkTVMode.Checked = false;
                    break;
                default:
                    break;
            }
            RefreshTVInfo();
        }

        private bool FreezeGame()
        {
            if (gameProcess == null)
            {
                LogStatus("No process to freeze.");
                return false;
            }
            try
            {
                foreach (ProcessThread thread in gameProcess.Threads)
                {
                    IntPtr hThread = NativeMethods.OpenThread(NativeMethods.THREAD_SUSPEND_RESUME, false, (uint)thread.Id);
                    if (hThread == IntPtr.Zero)
                    {
                        LogStatus($"Failed to open thread {thread.Id}.");
                        continue;
                    }
                    uint suspendCount = NativeMethods.SuspendThread(hThread);
                    LogStatus($"Suspended thread {thread.Id} (count {suspendCount}).");
                    NativeMethods.CloseHandle(hThread);
                }
                LogStatus("Process frozen.");
                return true;
            }
            catch (Exception ex)
            {
                LogStatus("Freeze error: " + ex.Message);
                return false;
            }
        }

        private void UnfreezeGame()
        {
            if (gameProcess == null)
            {
                LogStatus("No process to unfreeze.");
                return;
            }
            try
            {
                foreach (ProcessThread thread in gameProcess.Threads)
                {
                    IntPtr hThread = NativeMethods.OpenThread(NativeMethods.THREAD_SUSPEND_RESUME, false, (uint)thread.Id);
                    if (hThread != null)
                    {
                        while (NativeMethods.ResumeThread(hThread) > 0) { }
                        LogStatus($"Resumed thread {thread.Id}.");
                        NativeMethods.CloseHandle(hThread);
                    }
                }
                LogStatus("Process unfrozen.");
            }
            catch (Exception ex)
            {
                LogStatus("Unfreeze error: " + ex.Message);
            }
        }

        private void TVSelectRunningProcess()
        {
            try
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
                processList.Font = new Font("Tahoma",18,FontStyle.Bold);
                processList.ForeColor = Color.LightGray;
                processList.BackColor = Color.FromArgb(30,30,30);
                processList.Margin = new Padding(10);
                processList.Columns.Add("Process", -2, HorizontalAlignment.Center);
                processList.Columns.Add("PID", -2, HorizontalAlignment.Center);
                Process[] procs = ProcessHelper.GetProcesses();
                if (procs == null || procs.Length == 0)
                {
                    MessageBox.Show("No processes found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
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
                {
                    processList.SelectedIndices.Clear();
                    processList.SelectedIndices.Add(0);
                }
                processList.KeyDown += (s,e) =>
                {
                    try 
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
                    }
                    catch (Exception ex)
                    {
                        LogStatus("Error in process list: " + ex.Message);
                    }
                };
                panel.Controls.Add(processList);
                ShowOverlay(panel);
            }
            catch(Exception ex)
            {
                LogStatus("TVSelectRunningProcess error: " + ex.Message);
            }
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
            try
            {
                Panel panel = new Panel();
                panel.BackColor = Color.FromArgb(45,45,48);
                panel.Dock = DockStyle.Fill;
                TableLayoutPanel tbl = new TableLayoutPanel();
                tbl.Dock = DockStyle.Fill;
                tbl.RowCount = 4;
                tbl.ColumnCount = 3;
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,20));
                for (int i = 0; i < 4; i++)
                {
                    Label lbl = new Label() { Text = "Additional Injection " + (i+1) + " (Optional)", Dock = DockStyle.Fill, ForeColor = Color.White, Padding = new Padding(5) };
                    TextBox tb = new TextBox() { Text = comboAdditional[i].Text, Dock = DockStyle.Fill, Font = new Font("Tahoma",16,FontStyle.Bold), Margin = new Padding(5) };
                    // In TV mode use a standard CheckBox (no Button appearance)
                    CheckBox chk = new CheckBox() { Checked = chkAdditional[i].Checked, Dock = DockStyle.Fill, Font = new Font("Tahoma",16,FontStyle.Bold), ForeColor = Color.White, Margin = new Padding(5), Text = "Inject", TextAlign = ContentAlignment.MiddleLeft };
                    int index = i;
                    chk.CheckedChanged += (s,e) => { chkAdditional[index].Checked = chk.Checked; };
                    Button btnBrowse = CreateFlatButton("Browse");
                    btnBrowse.Click += (s,e) => { TVFileBrowserForTextBox(tb); };
                    tbl.Controls.Add(lbl, 0, i);
                    tbl.Controls.Add(tb, 1, i);
                    Panel pnl = new Panel() { Dock = DockStyle.Fill, Margin = new Padding(5) };
                    pnl.Controls.Add(chk);
                    pnl.Controls.Add(btnBrowse);
                    btnBrowse.Location = new Point(150,0);
                    tbl.Controls.Add(pnl, 2, i);
                    tb.TextChanged += (s,e) => { comboAdditional[index].Text = tb.Text; };
                }
                Button btnOk = CreateFlatButton("OK");
                btnOk.Dock = DockStyle.Bottom;
                btnOk.Height = 50;
                btnOk.Click += (s,e) => { HideOverlay(); };
                panel.Controls.Add(tbl);
                panel.Controls.Add(btnOk);
                ShowOverlay(panel);
            }
            catch(Exception ex)
            {
                LogStatus("TVConfigureDLLInjections error: " + ex.Message);
            }
        }

        private void TVManageProfiles()
        {
            try
            {
                Panel panel = new Panel();
                panel.BackColor = Color.FromArgb(45,45,48);
                panel.Dock = DockStyle.Fill;
                TableLayoutPanel tbl = new TableLayoutPanel();
                tbl.Dock = DockStyle.Fill;
                tbl.RowCount = 2;
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
                panel.Controls.Add(tbl);

                ListView lb = new ListView();
                lb.Dock = DockStyle.Fill;
                lb.View = View.Details;
                lb.FullRowSelect = true;
                lb.GridLines = true;
                lb.HeaderStyle = ColumnHeaderStyle.None;
                lb.Font = new Font("Tahoma",18,FontStyle.Bold);
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
                            lb.Items.Add(new ListViewItem(name));
                    }
                }
                if (lb.Items.Count > 0)
                {
                    lb.SelectedIndices.Clear();
                    lb.SelectedIndices.Add(0);
                }
                tbl.Controls.Add(lb, 0, 0);

                FlowLayoutPanel panelButtons = new FlowLayoutPanel();
                panelButtons.Dock = DockStyle.Fill;
                panelButtons.Padding = new Padding(10);
                Button btnLoad = CreateFlatButton("Load");
                btnLoad.Width = 150;
                btnLoad.Height = 50;
                btnLoad.Click += (s,e) =>
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
                Button btnSave = CreateFlatButton("Save");
                btnSave.Width = 150;
                btnSave.Height = 50;
                btnSave.Click += (s,e) =>
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
                                    lb.Items.Add(new ListViewItem(name));
                            }
                        }
                    }
                };
                Button btnDelete = CreateFlatButton("Delete");
                btnDelete.Width = 150;
                btnDelete.Height = 50;
                btnDelete.Click += (s,e) =>
                {
                    if (lb.SelectedItems.Count > 0)
                    {
                        string profileName = lb.SelectedItems[0].Text;
                        string filePath = Path.Combine(profilesDir, profileName + ".ini");
                        if (File.Exists(filePath))
                        {
                            var result = MessageBox.Show($"Delete profile '{profileName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (result == DialogResult.Yes)
                            {
                                File.Delete(filePath);
                                LogStatus("Profile deleted: " + profileName);
                                lb.Items.Remove(lb.SelectedItems[0]);
                            }
                        }
                    }
                };
                panelButtons.Controls.Add(btnLoad);
                panelButtons.Controls.Add(btnSave);
                panelButtons.Controls.Add(btnDelete);
                tbl.Controls.Add(panelButtons, 0, 1);

                ShowOverlay(panel);
            }
            catch(Exception ex)
            {
                LogStatus("TVManageProfiles error: " + ex.Message);
            }
        }

        private void TVViewConsoleOutput()
        {
            try
            {
                Panel panel = new Panel();
                panel.BackColor = Color.FromArgb(45,45,48);
                panel.Dock = DockStyle.Fill;
                TextBox tb = new TextBox();
                tb.Multiline = true;
                tb.ReadOnly = true;
                tb.ScrollBars = ScrollBars.Vertical;
                tb.Dock = DockStyle.Fill;
                tb.Font = new Font("Tahoma", 22, FontStyle.Bold);
                tb.BackColor = Color.FromArgb(30,30,30);
                tb.ForeColor = Color.White;
                tb.Margin = new Padding(10);
                tb.Text = txtStatusLog.Text;
                panel.Controls.Add(tb);
                ShowOverlay(panel);
            }
            catch(Exception ex)
            {
                LogStatus("TVViewConsoleOutput error: " + ex.Message);
            }
        }

        private void SetupTVModePanel()
        {
            tvModePanel = new Panel();
            tvModePanel.Dock = DockStyle.Fill;
            tvModePanel.BackColor = Color.FromArgb(45,45,48);
            tvModePanel.Visible = false;
            this.Controls.Add(tvModePanel);

            TableLayoutPanel tvLayout = new TableLayoutPanel();
            tvLayout.Dock = DockStyle.Fill;
            tvLayout.RowCount = 3;
            tvLayout.ColumnCount = 1;
            tvLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
            tvLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            tvLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
            tvModePanel.Controls.Add(tvLayout);

            // Updated TV Header Panel with centered text using a TableLayoutPanel.
            Panel tvHeaderPanel = new Panel();
            tvHeaderPanel.Dock = DockStyle.Fill;
            tvHeaderPanel.BackColor = Color.FromArgb(45,45,48);
            TableLayoutPanel headerLayout = new TableLayoutPanel();
            headerLayout.Dock = DockStyle.Fill;
            headerLayout.ColumnCount = 2;
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tvHeaderPanel.Controls.Add(headerLayout);

            Button btnTVLaunch = CreateFlatButton("Launch");
            btnTVLaunch.Font = new Font("Tahoma", 24, FontStyle.Bold);
            btnTVLaunch.Dock = DockStyle.Fill;
            btnTVLaunch.Width = 150;
            btnTVLaunch.Click += (s,e) => BtnLaunch_Click(null, EventArgs.Empty);

            Label lblTVHeader = new Label();
            lblTVHeader.Text = "CHOOCHOO LOADER (Steam Deck Mode)";
            lblTVHeader.Font = new Font("Tahoma", 24, FontStyle.Bold);
            lblTVHeader.ForeColor = Color.LightGray;
            lblTVHeader.Dock = DockStyle.Fill;
            lblTVHeader.TextAlign = ContentAlignment.MiddleCenter;

            headerLayout.Controls.Add(btnTVLaunch, 0, 0);
            headerLayout.Controls.Add(lblTVHeader, 1, 0);
            tvLayout.Controls.Add(tvHeaderPanel, 0, 0);

            Panel tvMenuPanel = new Panel();
            tvMenuPanel.Dock = DockStyle.Fill;
            tvMenuPanel.BackColor = Color.FromArgb(30,30,30);
            tvModeList = new ListView();
            tvModeList.Dock = DockStyle.Fill;
            tvModeList.View = View.List;
            tvModeList.Font = new Font("Tahoma", 22, FontStyle.Bold);
            tvModeList.ForeColor = Color.LightGray;
            tvModeList.BackColor = Color.FromArgb(30,30,30);
            string[] menuItems = new string[]
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
                "PAUSE GAME [BETA]",
                "Exit TV Mode"
            };
            tvModeList.Items.Clear();
            foreach (string item in menuItems)
                tvModeList.Items.Add(new ListViewItem(item));
            if (tvModeList.Items.Count > 0)
            {
                tvModeList.SelectedIndices.Clear();
                tvModeList.SelectedIndices.Add(0);
            }
            tvModeList.KeyDown += (s,e) =>
            {
                try 
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (tvModeList.SelectedItems.Count > 0)
                        {
                            string action = tvModeList.SelectedItems[0].Text;
                            HandleTVModeAction(action);
                        }
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.B)
                    {
                        chkTVMode.Checked = false;
                        e.Handled = true;
                    }
                }
                catch (Exception ex)
                {
                    LogStatus("TVMode list error: " + ex.Message);
                }
            };
            tvMenuPanel.Controls.Add(tvModeList);
            tvLayout.Controls.Add(tvMenuPanel, 0, 1);

            Panel tvFooterPanel = new Panel();
            tvFooterPanel.Dock = DockStyle.Fill;
            tvFooterPanel.BackColor = Color.FromArgb(50,50,50);
            tvInfoText = new TextBox();
            tvInfoText.Dock = DockStyle.Fill;
            tvInfoText.Multiline = true;
            tvInfoText.ReadOnly = true;
            tvInfoText.Font = new Font("Tahoma", 24, FontStyle.Bold);
            tvInfoText.BackColor = Color.FromArgb(30,30,30);
            tvInfoText.ForeColor = Color.LightGray;
            tvFooterPanel.Controls.Add(tvInfoText);
            tvLayout.Controls.Add(tvFooterPanel, 0, 2);
        }

        private void SaveSettings()
        {
            try {
                using (StreamWriter sw = new StreamWriter(settingsFile))
                {
                    sw.WriteLine(chkLoadTVOnLaunch.Checked);
                    sw.WriteLine(chkTVMode.Checked);
                    sw.WriteLine(chkAutoLaunch.Checked);
                }
            }
            catch (Exception ex)
            {
                LogStatus("Error saving settings: " + ex.Message);
            }
        }

        private void LoadSettings()
        {
            try {
                if (File.Exists(settingsFile))
                {
                    using (StreamReader sr = new StreamReader(settingsFile))
                    {
                        bool loadTV, tv, auto;
                        if (bool.TryParse(sr.ReadLine(), out loadTV))
                            chkLoadTVOnLaunch.Checked = loadTV;
                        if (bool.TryParse(sr.ReadLine(), out tv))
                            chkTVMode.Checked = tv;
                        if (bool.TryParse(sr.ReadLine(), out auto))
                            chkAutoLaunch.Checked = auto;
                    }
                }
            }
            catch (Exception ex)
            {
                LogStatus("Error loading settings: " + ex.Message);
            }
        }

        private void ShowOverlay(Control control)
        {
            if (overlayPanel != null)
            {
                overlayPanel.Controls.Clear();
                control.Dock = DockStyle.Fill;
                overlayPanel.Controls.Add(control);
                overlayPanel.Visible = true;
                overlayPanel.BringToFront();
                control.Focus();
            }
        }

        private void HideOverlay()
        {
            if (overlayPanel != null)
            {
                overlayPanel.Visible = false;
                overlayPanel.Controls.Clear();
            }
            if (tvModePanel != null && tvModePanel.Visible)
            {
                if (tvModeList != null && tvModeList.Items.Count > 0)
                {
                    tvModeList.SelectedIndices.Clear();
                    tvModeList.SelectedIndices.Add(0);
                }
                tvModePanel.Focus();
            }
            else if (mainPanel != null)
            {
                mainPanel.Focus();
            }
        }

        private void LogStatus(string message)
        {
            txtStatusLog.AppendText(message.ToUpper() + Environment.NewLine);
        }

        private void LogTool(string message)
        {
            txtToolOutput.AppendText(message + Environment.NewLine);
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
                    comboRunningExes.Items.Add(p);
                    LogStatus($"Detected: {p.ProcessName} (PID {p.Id})");
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
            {
                comboRunningExes.SelectedIndex = 0;
            }
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
                        string profileName = args[i+1];
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
                    int j = i+1;
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
                launchTimer.Tick += (s,e) =>
                {
                    launchTimer.Stop();
                    BtnLaunch_Click(null, EventArgs.Empty);
                };
                launchTimer.Start();
            }
        }

        private Button CreateFlatButton(string text)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.Black;
            btn.BackColor = Color.FromArgb(30,30,30);
            btn.ForeColor = Color.White;
            return btn;
        }

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
            {
                MessageBox.Show("Profile not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            if (chkTVMode.Checked)
            {
                Directory.CreateDirectory(profilesDir);
                string lastFile = Path.Combine(profilesDir, "last.ini");
                SaveProfile(lastFile);
                txtStatusLog.Clear();
            }
            Process targetProcess = null;
            if (comboRunningExes.SelectedItem is Process proc)
            {
                try { proc.Refresh(); } catch { }
                if (!proc.HasExited)
                {
                    targetProcess = proc;
                    LogStatus("Using running process: " + proc.ProcessName + " (PID " + proc.Id + ")");
                }
                else
                    LogStatus("Selected process exited. Launching new instance from Game Path.");
            }
            if (targetProcess == null)
                targetProcess = LaunchProcessFromPath(comboGame.Text, "Game");
            if (targetProcess != null)
                gameProcess = targetProcess;
            if (!string.IsNullOrEmpty(comboTrainer.Text))
            {
                int methodIndex = Array.FindIndex(radioTrainerMethods, r => r.Checked);
                Process trainerProc = LaunchTrainerUsingMethod(methodIndex, comboTrainer.Text);
                if (trainerProc != null)
                    LogStatus("Trainer launched.");
            }
            for (int i = 0; i < 4; i++)
            {
                string file = comboAdditional[i].Text.Trim();
                if (string.IsNullOrEmpty(file))
                    continue;
                if (Path.GetExtension(file).ToLowerInvariant() != ".dll")
                    LaunchProcessFromPath(file, $"Additional EXE {i+1}");
            }
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
                        while (NativeMethods.ResumeThread(hThread) > 0) { }
                        LogTool($"Resumed thread {thread.Id}.");
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
            {
                if (chkAdditional[i].Checked)
                    UpdateAdditionalInjection(i);
            }
        }

        private void ApplyDarkTheme(Control control)
        {
            control.BackColor = Color.FromArgb(45,45,48);
            control.ForeColor = Color.White;
            foreach (Control child in control.Controls)
            {
                child.ForeColor = Color.White;
                ApplyDarkTheme(child);
            }
        }

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
            fileBrowserPathEntry.Font = new Font("Tahoma",14,FontStyle.Bold);
            fileBrowserPathEntry.BackColor = Color.FromArgb(30,30,30);
            fileBrowserPathEntry.ForeColor = Color.White;
            fileBrowserPathEntry.Margin = new Padding(10);
            fileBrowserPathEntry.KeyDown += (s,e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    currentDirectory = fileBrowserPathEntry.Text;
                    RefreshFileBrowserList();
                }
            };
            fileBrowserLayout.Controls.Add(fileBrowserPathEntry,0,0);
            fileBrowserList = new ListView();
            fileBrowserList.Dock = DockStyle.Fill;
            fileBrowserList.View = View.Details;
            fileBrowserList.Font = new Font("Tahoma",14,FontStyle.Bold);
            fileBrowserList.FullRowSelect = true;
            fileBrowserList.GridLines = true;
            fileBrowserList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            fileBrowserList.Margin = new Padding(10);
            fileBrowserList.Columns.Add("Name",500);
            fileBrowserList.Columns.Add("Type",150);
            fileBrowserList.DoubleClick += (s,e) =>
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
            fileBrowserList.KeyDown += (s,e) =>
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
            fileBrowserLayout.Controls.Add(fileBrowserList,0,1);
            fileBrowserButtonPanel = new FlowLayoutPanel();
            fileBrowserButtonPanel.Dock = DockStyle.Fill;
            fileBrowserButtonPanel.FlowDirection = FlowDirection.LeftToRight;
            fileBrowserButtonPanel.Padding = new Padding(10);
            fileBrowserButtonPanel.BackColor = Color.FromArgb(45,45,48);
            btnUp = CreateFlatButton("Up");
            btnUp.Width = 100;
            btnUp.Height = 40;
            btnUp.Font = new Font("Tahoma",12,FontStyle.Bold);
            btnUp.Click += (s,e) =>
            {
                var parentDir = Directory.GetParent(currentDirectory);
                if (parentDir != null)
                {
                    currentDirectory = parentDir.FullName;
                    RefreshFileBrowserList();
                }
            };
            fileBrowserButtonPanel.Controls.Add(btnUp);
            btnRefresh = CreateFlatButton("Refresh");
            btnRefresh.Width = 150;
            btnRefresh.Height = 50;
            btnRefresh.Font = new Font("Tahoma",12,FontStyle.Bold);
            btnRefresh.Click += (s,e) => RefreshFileBrowserList();
            fileBrowserButtonPanel.Controls.Add(btnRefresh);
            btnSelect = CreateFlatButton("Select");
            btnSelect.Width = 100;
            btnSelect.Height = 40;
            btnSelect.Font = new Font("Tahoma",12,FontStyle.Bold);
            btnSelect.Click += (s,e) =>
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
            btnCancel = CreateFlatButton("Cancel");
            btnCancel.Width = 100;
            btnCancel.Height = 40;
            btnCancel.Font = new Font("Tahoma",12,FontStyle.Bold);
            btnCancel.Click += (s,e) => { fileBrowserPanel.Visible = false; };
            fileBrowserButtonPanel.Controls.Add(btnCancel);
            fileBrowserLayout.Controls.Add(fileBrowserButtonPanel,0,2);
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
                        case ".txt": fileType = "Text Document"; break;
                        case ".cs": fileType = "C# Source File"; break;
                        case ".exe": fileType = "Executable"; break;
                        case ".dll": fileType = "Dynamic Link Library"; break;
                        case ".png":
                        case ".jpg":
                        case ".jpeg":
                        case ".gif": fileType = "Image File"; break;
                        case ".ini": fileType = "Configuration File"; break;
                        default: fileType = !string.IsNullOrEmpty(ext) ? ext.ToUpper() + " File" : "Unknown File"; break;
                    }
                    item.SubItems.Add(fileType);
                    fileBrowserList.Items.Add(item);
                }
                if (fileBrowserList.Items.Count > 0)
                {
                    fileBrowserList.SelectedIndices.Clear();
                    fileBrowserList.SelectedIndices.Add(0);
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
                ctrl.Enter += (s,e) => UpdateArrowIndicator();
                if (ctrl.Controls.Count > 0)
                    AttachFocusHandlers(ctrl);
            }
        }

        private void UpdateArrowIndicator()
        {
            if (arrowIndicator == null)
                return;
            try
            {
                Control active = GetDeepActiveControl(this);
                if (active is ListView listView && listView.SelectedItems.Count > 0)
                {
                    Rectangle itemBounds = listView.SelectedItems[0].Bounds;
                    Point pt = listView.PointToScreen(itemBounds.Location);
                    pt = this.PointToClient(pt);
                    arrowIndicator.Location = new Point(pt.X - arrowIndicator.Width - 5, pt.Y + (itemBounds.Height - arrowIndicator.Height) / 2);
                }
                else if (active != null)
                {
                    Rectangle bounds = active.Bounds;
                    Point location = active.Parent.PointToScreen(bounds.Location);
                    location = this.PointToClient(location);
                    arrowIndicator.Location = new Point(location.X - arrowIndicator.Width - 5, location.Y + (bounds.Height - arrowIndicator.Height) / 2);
                }
                arrowIndicator.BringToFront();
                arrowIndicator.Visible = true;
            }
            catch 
            { 
                arrowIndicator.Visible = false; 
            }
        }

        private Control GetDeepActiveControl(Control control)
        {
            while (control is ContainerControl container && container.ActiveControl != null)
                control = container.ActiveControl;
            return control;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Escape)
                {
                    if (overlayPanel != null && overlayPanel.Visible)
                    {
                        HideOverlay();
                        e.Handled = true;
                        return;
                    }
                    else if (chkTVMode.Checked)
                    {
                        chkTVMode.Checked = false;
                        e.Handled = true;
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
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.B)
                {
                    if (overlayPanel != null && overlayPanel.Visible)
                    {
                        HideOverlay();
                        e.Handled = true;
                    }
                    else if (fileBrowserPanel != null && fileBrowserPanel.Visible)
                    {
                        fileBrowserPanel.Visible = false;
                        e.Handled = true;
                    }
                    else if (chkTVMode.Checked)
                    {
                        chkTVMode.Checked = false;
                        e.Handled = true;
                    }
                }
            }
            catch(Exception ex)
            {
                LogStatus("MainForm_KeyDown error: " + ex.Message);
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
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
                chkAdditional[index].Text = "Launch/Inject (Optional)";
                return;
            }
            chkAdditional[index].Text = "Inject (Optional)";
            if (!File.Exists(file))
            {
                if (!validatedDlls.ContainsKey(file))
                {
                    LogStatus($"DLL {Path.GetFileName(file)} NOT FOUND!");
                    validatedDlls[file] = false;
                }
                return;
            }
            if (Environment.Is64BitProcess && !IsDll64Bit(file))
            {
                LogStatus($"DLL {Path.GetFileName(file)} is 32-bit; injection aborted. Please use a 64-bit DLL.");
                validatedDlls[file] = false;
                return;
            }
            if (!validatedDlls.ContainsKey(file) || validatedDlls[file] == false)
            {
                LogStatus($"DLL {Path.GetFileName(file)} found at '{file}' with size {new FileInfo(file).Length} bytes. Beginning injection...");
                InjectDll(file, gameProcess);
                validatedDlls[file] = true;
            }
            RefreshDllList();
        }

        private bool IsDll64Bit(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
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

        private void InjectDll(string dllPath, Process targetProcess)
        {
            try
            {
                IntPtr kernel32 = NativeMethods.GetModuleHandle("kernel32.dll");
                IntPtr loadLibraryAddr = NativeMethods.GetProcAddress(kernel32, "LoadLibraryW");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    LogStatus("Failed to get address of LoadLibraryW.");
                    return;
                }
                byte[] dllBytes = System.Text.Encoding.Unicode.GetBytes(dllPath + "\0");
                IntPtr allocMemAddress = NativeMethods.VirtualAllocEx(targetProcess.Handle, IntPtr.Zero, (uint)dllBytes.Length, NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE, NativeMethods.PAGE_READWRITE);
                if (allocMemAddress == IntPtr.Zero)
                {
                    LogStatus("Failed to allocate memory in target process.");
                    return;
                }
                UIntPtr bytesWritten;
                bool writeResult = NativeMethods.WriteProcessMemory(targetProcess.Handle, allocMemAddress, dllBytes, (uint)dllBytes.Length, out bytesWritten);
                if (!writeResult || bytesWritten.ToUInt32() != dllBytes.Length)
                {
                    LogStatus("Failed to write DLL path to target process memory.");
                    return;
                }
                uint threadId;
                IntPtr remoteThread = NativeMethods.CreateRemoteThread(targetProcess.Handle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, out threadId);
                if (remoteThread == IntPtr.Zero)
                {
                    LogStatus("Failed to create remote thread for DLL injection.");
                    return;
                }
                NativeMethods.WaitForSingleObject(remoteThread, NativeMethods.INFINITE);
                NativeMethods.CloseHandle(remoteThread);
                NativeMethods.VirtualFreeEx(targetProcess.Handle, allocMemAddress, 0, NativeMethods.MEM_RELEASE);
                LogStatus($"DLL {Path.GetFileName(dllPath)} injected using LoadLibraryW (Remote thread ID {threadId}).");
            }
            catch (Exception ex)
            {
                LogStatus("DLL injection error: " + ex.Message);
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

            if (label == "Trainer")
            {
                DateTime start = DateTime.Now;
                Process proc = null;
                while ((DateTime.Now - start).TotalSeconds < 3)
                {
                    try
                    {
                        proc = RawLaunchProcess(path);
                        if (proc != null && !proc.HasExited)
                            break;
                    }
                    catch { }
                    System.Threading.Thread.Sleep(500);
                }
                if (proc == null || proc.HasExited)
                    proc = LaunchTrainerViaCmd(path);
                if (proc != null)
                {
                    LogStatus($"{label}: LAUNCHED (Trainer)");
                    AddRecent(comboTrainer, path);
                    return proc;
                }
                else
                {
                    LogStatus($"{label}: FAILED after fallback.");
                    return null;
                }
            }
            else
            {
                int attempts = 3;
                while (attempts-- > 0)
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = path,
                            WorkingDirectory = Path.GetDirectoryName(path) ?? "",
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };
                        Process proc = Process.Start(psi);
                        LogStatus($"{label}: LAUNCHED");
                        if (label == "Game")
                            AddRecent(comboGame, path);
                        return proc;
                    }
                    catch (Exception ex)
                    {
                        LogStatus($"{label}: LAUNCH ATTEMPT FAILED ({ex.Message}). Retrying...");
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                LogStatus($"{label}: FAILED after multiple attempts.");
                return null;
            }
        }

        private Process RawLaunchProcess(string path)
        {
            Win32API.STARTUPINFO si = new Win32API.STARTUPINFO();
            si.cb = Marshal.SizeOf(typeof(Win32API.STARTUPINFO));
            Win32API.PROCESS_INFORMATION pi;
            bool result = Win32API.CreateProcess(path, null, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, Path.GetDirectoryName(path), ref si, out pi);
            if (!result)
            {
                int err = Marshal.GetLastWin32Error();
                throw new Exception("CreateProcess failed, error code " + err);
            }
            if (pi.hThread != IntPtr.Zero)
                NativeMethods.CloseHandle(pi.hThread);
            Process proc = Process.GetProcessById(pi.dwProcessId);
            return proc;
        }

        private Process LaunchTrainerViaCmd(string path)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C start /B \"\" \"{path}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process proc = Process.Start(psi);
                return proc;
            }
            catch(Exception ex)
            {
                LogStatus("LaunchTrainerViaCmd failed: " + ex.Message);
                return null;
            }
        }

        private Process LaunchTrainerUsingMethod(int methodIndex, string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                LogStatus("Trainer: FAILED (NOT FOUND)");
                return null;
            }
            Process proc = null;
            switch (methodIndex)
            {
                case 0:
                    proc = RawLaunchProcess(path);
                    break;
                case 1:
                    proc = LaunchTrainerViaCmd(path);
                    break;
                case 2:
                    proc = LaunchTrainerViaCreateThread(path);
                    break;
                case 3:
                    proc = LaunchTrainerViaRemoteThread(path);
                    break;
                case 4:
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = path,
                            WorkingDirectory = Path.GetDirectoryName(path),
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };
                        proc = Process.Start(psi);
                    }
                    break;
                case 5:
                    {
                        try { proc = Process.Start(path); } catch (Exception ex) { LogStatus("Raw Process.Start failed: " + ex.Message); }
                    }
                    break;
                default:
                    proc = LaunchTrainerViaCmd(path);
                    break;
            }
            if (proc != null && !proc.HasExited)
                LogStatus($"Trainer: LAUNCHED using method {methodIndex+1}");
            return proc;
        }

        private Process LaunchTrainerViaCreateThread(string path)
        {
            try { return RawLaunchProcess(path); } catch (Exception ex) { LogStatus("CreateThread method failed: " + ex.Message); return null; }
        }

        private Process LaunchTrainerViaRemoteThread(string path)
        {
            try { return RawLaunchProcess(path); } catch (Exception ex) { LogStatus("Remote Thread method failed: " + ex.Message); return null; }
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
                sw.WriteLine("FULLSCREEN=" + chkLoadTVOnLaunch.Checked);
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
                        chkLoadTVOnLaunch.Checked = fs;
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
    }

    public class MemoryRegionDump
    {
        public IntPtr BaseAddress { get; set; }
        public uint RegionSize { get; set; }
        public byte[] MemoryContents { get; set; }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex) {
                MessageBox.Show("Unhandled exception: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
