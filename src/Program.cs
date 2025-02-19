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
    // Custom ListView subclass to reliably process the Enter key.
    public class CustomListView : ListView
    {
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                if (this.SelectedItems.Count > 0)
                {
                    OnEnterPressed();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public event EventHandler EnterPressed;

        protected virtual void OnEnterPressed()
        {
            EnterPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    // A simple input box for TV mode.
    public static class SimpleInputBox
    {
        public static string Show(string prompt, string title, string defaultValue)
        {
            Form inputForm = new Form();
            inputForm.Text = title;
            inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputForm.StartPosition = FormStartPosition.CenterScreen;
            inputForm.ClientSize = new Size(400, 120);
            Label label = new Label() { Left = 10, Top = 10, Text = prompt, AutoSize = true, ForeColor = Color.White };
            TextBox textBox = new TextBox() { Left = 10, Top = 40, Width = 380, Text = defaultValue, BackColor = Color.FromArgb(30,30,30), ForeColor = Color.White };
            Button okButton = new Button() { Text = "OK", Left = 220, Width = 80, Top = 70, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(45,45,48), ForeColor = Color.White };
            Button cancelButton = new Button() { Text = "Cancel", Left = 310, Width = 80, Top = 70, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(45,45,48), ForeColor = Color.White };
            inputForm.Controls.Add(label);
            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(okButton);
            inputForm.Controls.Add(cancelButton);
            inputForm.AcceptButton = okButton;
            inputForm.CancelButton = cancelButton;
            DialogResult result = inputForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                return textBox.Text;
            }
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

    // --- New Win32 API declarations (inside helper class) ---
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
    // -------------------------------

    public partial class MainForm : Form
    {
        // ---------- Field declarations ----------
        private Panel overlayPanel;
        private Font tvFont;
        private Panel mainPanel;
        private TabControl tabControl;
        private TabPage tabPageMain, tabPageHelp, tabPageTools;
        private CheckBox chkFullscreen, chkTVMode, chkAutoLaunch;
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
        // ---------- New fields for trainer launching ----------
        private CheckBox chkCMDLaunch;
        private GroupBox groupLaunchMethods;
        private RadioButton[] rdoLaunchMethods;
        private bool trainerLaunched = false;
        // --------------------------------------------
        private Button btnFreezeProcess, btnUnfreezeProcess, btnKillProcess, btnDumpProcess, btnListImports, btnListRuntimes, btnSaveState, btnLoadState, btnListModules, btnListThreads;
        private WinFormsTimer xinputTimer, injectionTimer, arrowAnimationTimer;
        private Label arrowIndicator;
        // TV Mode controls
        private Panel tvModePanel;
        private CustomListView tvModeList;
        private Panel tvInfoPanel;
        private TextBox tvInfoText;
        // File Browser controls
        private Panel fileBrowserPanel;
        private TableLayoutPanel fileBrowserLayout;
        private TextBox fileBrowserPathEntry;
        private ListView fileBrowserList;
        private FlowLayoutPanel fileBrowserButtonPanel;
        private Button btnUp, btnRefresh, btnSelect, btnCancel;
        private Control currentTargetControl;
        private string currentDirectory;
        // Other fields
        private string recentFile = "recent.ini";
        private string profilesDir = "profiles";
        private string settingsFile = "settings.ini";
        private Process gameProcess;
        // XInput tracking fields
        private XINPUT_STATE prevXInputState;
        private byte prevLeftTrigger, prevRightTrigger;
        // Arrow animation fields
        private double arrowAnimationTime;
        private int arrowAnimationOffset;
        private int arrowAnimationAmplitude = 3;
        private double arrowAnimationPeriod = 3.0;
        // For DLL injection validation
        private string[] additionalInjectedFile = new string[4];
        private Dictionary<string, bool> validatedDlls = new Dictionary<string, bool>();
        // New: For tracking which DLLs have been injected
        private Dictionary<string, bool> injectedDlls = new Dictionary<string, bool>();
        // Missing analog sent flags
        private bool analogDownSent, analogUpSent, analogLeftSent, analogRightSent;
        // Global hooks for exclusive fullscreen mode
        private GlobalHook globalHook;
        private GlobalMouseHook globalMouseHook;
        // Our custom cursor (loaded from "cursor.cur" if available)
        private Cursor myCursor;

        // ---------- Standalone Window Fix via CreateParams ----------
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00040000;
                return cp;
            }
        }

        // ---------- Override OnActivated and WndProc to manage our own cursor and activation ----------
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
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

        // ---------- Override ProcessCmdKey to handle Enter in TV mode ----------
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (tvModePanel != null && tvModePanel.Visible && keyData == Keys.Enter)
            {
                if (tvModeList.SelectedItems.Count > 0)
                {
                    HandleTVModeAction(tvModeList.SelectedItems[0].Text);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---------- Constructor ----------
        public MainForm()
        {
            this.Text = "ChooChoo Injection Engine - Standalone";
            this.ShowInTaskbar = true;
            this.Owner = null;
            this.Font = new Font("Tahoma", 9);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(1200, 750);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            try
            {
                myCursor = new Cursor("cursor.cur");
            }
            catch
            {
                myCursor = Cursors.Default;
            }

            Directory.CreateDirectory(profilesDir);
            string lastProfile = Path.Combine(profilesDir, "last.ini");
            if (File.Exists(lastProfile))
            {
                LoadProfile(lastProfile);
            }

            overlayPanel = new Panel();
            overlayPanel.Dock = DockStyle.Fill;
            overlayPanel.BackColor = Color.FromArgb(60, 60, 60);
            overlayPanel.Visible = false;
            overlayPanel.TabIndex = 1000;
            overlayPanel.KeyDown += OverlayPanel_KeyDown;
            this.Controls.Add(overlayPanel);
            overlayPanel.BringToFront();

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
            SetupTVModePanel();  // <-- Now defined as a separate method

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

            chkFullscreen = new CheckBox() { Text = "Fullscreen Mode", Location = new Point(10, 10), AutoSize = true, ForeColor = Color.White };
            mainTabPanel.Controls.Add(chkFullscreen);

            chkTVMode = new CheckBox() { Text = "TV Mode", Location = new Point(150, 10), AutoSize = true, ForeColor = Color.White };
            chkTVMode.CheckedChanged += (s, e) => ToggleTVMode();
            mainTabPanel.Controls.Add(chkTVMode);

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

            listBoxDlls = new ListBox() { Location = new Point(txtStatusLog.Right + 10, txtStatusLog.Top), Size = new Size(300, txtStatusLog.Height), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Tahoma", 10), ForeColor = Color.White, BackColor = Color.FromArgb(45,45,48) };
            mainTabPanel.Controls.Add(listBoxDlls);

            Panel panelLaunch = new Panel() { Location = new Point(10, txtStatusLog.Bottom + 40), Size = new Size(1170, 50) };
            mainTabPanel.Controls.Add(panelLaunch);

            btnLaunch = new Button() { Text = "Launch", Size = new Size(300, panelLaunch.Height), Location = new Point(0, 0), Font = new Font("Tahoma", 14, FontStyle.Bold), ForeColor = Color.White };
            btnLaunch.Click += BtnLaunch_Click;
            panelLaunch.Controls.Add(btnLaunch);

            chkCMDLaunch = new CheckBox() { Text = "CMD", Location = new Point(btnLaunch.Right + 10, (panelLaunch.Height - 25) / 2), AutoSize = true, ForeColor = Color.White };
            panelLaunch.Controls.Add(chkCMDLaunch);

            groupLaunchMethods = new GroupBox() { Text = "Launch Method", Location = new Point(chkCMDLaunch.Right + 20, 0), Size = new Size(panelLaunch.Width - (chkCMDLaunch.Right + 20), panelLaunch.Height), ForeColor = Color.White };
            panelLaunch.Controls.Add(groupLaunchMethods);

            rdoLaunchMethods = new RadioButton[6];
            string[] methodLabels = new string[] { "CreateProcess", "Proc.Start (no shell)", "Proc.Start (shell)", "Shell Min", "CMD Start", "P/Invoke" };
            int rbWidth = (groupLaunchMethods.Width - 20) / 6;
            for (int i = 0; i < 6; i++)
            {
                rdoLaunchMethods[i] = new RadioButton()
                {
                    Text = methodLabels[i],
                    Location = new Point(10 + i * rbWidth, (groupLaunchMethods.Height - 20) / 2),
                    Size = new Size(rbWidth, 20),
                    ForeColor = Color.White,
                    Tag = i
                };
                groupLaunchMethods.Controls.Add(rdoLaunchMethods[i]);
            }
            if (rdoLaunchMethods.Length > 0)
                rdoLaunchMethods[0].Checked = true;

            HelpTab helpControl = new HelpTab() { Dock = DockStyle.Fill };
            tabPageHelp.Controls.Add(helpControl);

            int tbtnWidth = 130, tbtnHeight = 30, tpadding = 10;
            btnFreezeProcess = new Button() { Text = "Freeze Proc", Location = new Point(tpadding, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnFreezeProcess.Click += BtnFreezeProcess_Click;
            tabPageTools.Controls.Add(btnFreezeProcess);

            btnUnfreezeProcess = new Button() { Text = "Unfreeze Proc", Location = new Point(tpadding * 2 + tbtnWidth, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnUnfreezeProcess.Click += BtnUnfreezeProcess_Click;
            tabPageTools.Controls.Add(btnUnfreezeProcess);

            btnKillProcess = new Button() { Text = "Kill Proc", Location = new Point(tpadding * 3 + tbtnWidth * 2, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnKillProcess.Click += BtnKillProcess_Click;
            tabPageTools.Controls.Add(btnKillProcess);

            btnDumpProcess = new Button() { Text = "Dump Proc", Location = new Point(tpadding * 4 + tbtnWidth * 3, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnDumpProcess.Click += BtnDumpProcess_Click;
            tabPageTools.Controls.Add(btnDumpProcess);

            btnListImports = new Button() { Text = "List Imports", Location = new Point(tpadding * 5 + tbtnWidth * 4, tpadding), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnListImports.Click += BtnListImports_Click;
            tabPageTools.Controls.Add(btnListImports);

            btnListRuntimes = new Button() { Text = "List Runtimes", Location = new Point(tpadding, tpadding * 2 + tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnListRuntimes.Click += BtnListRuntimes_Click;
            tabPageTools.Controls.Add(btnListRuntimes);

            btnSaveState = new Button() { Text = "Save State", Location = new Point(tpadding * 2 + tbtnWidth, tpadding * 2 + tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnSaveState.Click += BtnSaveState_Click;
            tabPageTools.Controls.Add(btnSaveState);

            btnLoadState = new Button() { Text = "Load State", Location = new Point(tpadding * 3 + tbtnWidth * 2, tpadding * 2 + tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnLoadState.Click += BtnLoadState_Click;
            tabPageTools.Controls.Add(btnLoadState);

            btnListModules = new Button() { Text = "List Modules", Location = new Point(tpadding * 4 + tbtnWidth * 3, tpadding * 2 + tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnListModules.Click += BtnListModules_Click;
            tabPageTools.Controls.Add(btnListModules);

            btnListThreads = new Button() { Text = "List Threads", Location = new Point(tpadding * 5 + tbtnWidth * 4, tpadding * 2 + tbtnHeight), Size = new Size(tbtnWidth, tbtnHeight), ForeColor = Color.White };
            btnListThreads.Click += BtnListThreads_Click;
            tabPageTools.Controls.Add(btnListThreads);

            txtToolOutput = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(tpadding, tpadding * 3 + tbtnHeight * 2), Size = new Size(1150, 500), ForeColor = Color.White, BackColor = Color.FromArgb(30,30,30) };
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

            globalHook = new GlobalHook();
            globalHook.KeyPressed += (s, e) => { /* details suppressed */ };
            globalMouseHook = new GlobalMouseHook();
            globalMouseHook.MouseAction += (s, e) => { /* details suppressed */ };

            this.Resize += MainForm_Resize;
            this.FormClosing += (s, e) =>
            {
                SaveSettings();
                globalHook.Dispose();
                globalMouseHook.Dispose();
            };
            ApplyDarkTheme(this);
            LoadSettings();
            ProcessCommandLineArgs();
            this.Shown += (s, e) => this.Activate();
        }

        // New: TV Mode panel setup method
        private void SetupTVModePanel()
        {
            // Create header panel
            TableLayoutPanel tvHeaderPanel = new TableLayoutPanel();
            tvHeaderPanel.Name = "tvHeaderPanel";
            tvHeaderPanel.Dock = DockStyle.Top;
            tvHeaderPanel.Height = (int)(this.ClientSize.Height * 0.15);
            tvHeaderPanel.BackColor = Color.FromArgb(45,45,48);
            tvHeaderPanel.ColumnCount = 2;
            tvHeaderPanel.RowCount = 1;
            tvHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            tvHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Button tvHeaderLaunch = new Button();
            tvHeaderLaunch.Text = "Launch\r\nStart";
            tvHeaderLaunch.Font = tvFont;
            tvHeaderLaunch.Dock = DockStyle.Fill;
            tvHeaderLaunch.Padding = new Padding(5);
            tvHeaderLaunch.Click += (s, e) =>
            {
                BtnLaunch_Click(null, EventArgs.Empty);
            };

            Label tvHeaderLabel = new Label();
            tvHeaderLabel.Text = "CHOOCHOO LOADER - TV MODE";
            tvHeaderLabel.Font = tvFont;
            tvHeaderLabel.ForeColor = Color.White;
            tvHeaderLabel.TextAlign = ContentAlignment.MiddleCenter;
            tvHeaderLabel.Dock = DockStyle.Fill;

            tvHeaderPanel.Controls.Add(tvHeaderLaunch, 0, 0);
            tvHeaderPanel.Controls.Add(tvHeaderLabel, 1, 0);

            // Create the TV mode menu list (without Advanced Tools)
            tvModeList = new CustomListView();
            tvModeList.TabStop = true;
            tvModeList.View = View.Details;
            tvModeList.FullRowSelect = true;
            tvModeList.GridLines = false;
            tvModeList.HeaderStyle = ColumnHeaderStyle.None;
            tvModeList.Font = new Font(tvFont.FontFamily, 22, FontStyle.Bold);
            tvModeList.ForeColor = Color.White;
            tvModeList.BackColor = Color.FromArgb(30,30,30);
            tvModeList.Margin = new Padding(10);
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
                "Exit TV Mode"
            };
            foreach (string item in tvMenuItems)
            {
                tvModeList.Items.Add(new ListViewItem(item));
            }
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
                if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.Left) && tvModeList.SelectedIndices.Count > 0)
                {
                    int idx = tvModeList.SelectedIndices[0];
                    if (idx > 0)
                    {
                        tvModeList.SelectedIndices.Clear();
                        tvModeList.SelectedIndices.Add(idx - 1);
                        e.Handled = true;
                    }
                }
                else if ((e.KeyCode == Keys.Down || e.KeyCode == Keys.Right) && tvModeList.SelectedIndices.Count > 0)
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
            ((CustomListView)tvModeList).EnterPressed += (s, e) =>
            {
                if (tvModeList.SelectedItems.Count > 0)
                    HandleTVModeAction(tvModeList.SelectedItems[0].Text);
            };

            // Create info panel at the bottom
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

            // Create the main TV mode panel and add header, menu list, and info panel.
            tvModePanel = new Panel();
            tvModePanel.Dock = DockStyle.Fill;
            tvModePanel.BackColor = Color.FromArgb(45,45,48);
            tvModePanel.Controls.Add(tvModeList);
            tvModePanel.Controls.Add(tvInfoPanel);
            tvModePanel.Controls.Add(tvHeaderPanel);
            tvModePanel.KeyDown += TvModePanel_KeyDown;
            tvModePanel.Visible = false;
            this.Controls.Add(tvModePanel);
            tvModePanel.BringToFront();
        }

        private void OverlayPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                HideOverlay();
                e.SuppressKeyPress = true;
            }
        }

        private void ShowOverlay(Control content)
        {
            overlayPanel.Controls.Clear();
            content.Dock = DockStyle.Fill;
            overlayPanel.Controls.Add(content);
            overlayPanel.Visible = true;
            overlayPanel.BringToFront();
            content.Focus();
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

        // ---------- TV Mode Functions ----------
        private void TvModePanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (!tvModeList.Focused)
            {
                tvModeList.Focus();
            }
            // Prevent Esc from exiting TV mode
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
            }
        }

        private void ToggleTVMode()
        {
            if (chkTVMode.Checked)
            {
                if (tvModePanel == null)
                    SetupTVModePanel();
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                mainPanel.Visible = false;
                tvModePanel.Visible = true;
                tvModePanel.BringToFront();
                tvModePanel.Focus();
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
                HideOverlay();
                tvModePanel.Visible = false;
                mainPanel.Visible = true;
                mainPanel.BringToFront();
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.WindowState = FormWindowState.Normal;
                this.ClientSize = new Size(1200, 750);
            }
        }

        // ---------- Fully Functional TV Mode Action Handler ----------
        private void HandleTVModeAction(string action)
        {
            switch(action)
            {
                case "Select Running Process":
                    ShowProcessListOverlay();
                    break;
                case "Set Game Path":
                    ShowIntegratedFileBrowser(comboGame);
                    break;
                case "Set Trainer Path":
                    ShowIntegratedFileBrowser(comboTrainer);
                    break;
                case "Set Additional Injection 1":
                    ShowIntegratedFileBrowser(comboAdditional[0]);
                    break;
                case "Set Additional Injection 2":
                    ShowIntegratedFileBrowser(comboAdditional[1]);
                    break;
                case "Set Additional Injection 3":
                    ShowIntegratedFileBrowser(comboAdditional[2]);
                    break;
                case "Set Additional Injection 4":
                    ShowIntegratedFileBrowser(comboAdditional[3]);
                    break;
                case "Configure DLL Injections":
                    RefreshDllList();
                    MessageBox.Show("DLL Injections configured. Please check the DLL list for validation status.", "DLL Injection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "Manage Profiles":
                    BtnRefreshProfiles_Click(this, EventArgs.Empty);
                    MessageBox.Show("Profiles refreshed. Use the Profiles tab to manage them.", "Profiles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "Launch Application":
                    BtnLaunch_Click(this, EventArgs.Empty);
                    break;
                case "View Console Output":
                    ShowOverlay(new TextBox() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, Text = txtStatusLog.Text, BackColor = Color.FromArgb(30,30,30), ForeColor = Color.White });
                    break;
                case "Exit TV Mode":
                    chkTVMode.Checked = false;
                    break;
                default:
                    MessageBox.Show("Unrecognized action: " + action);
                    break;
            }
        }

        public void RefreshTVInfo()
        {
            string configInfo = "Game Path: " + comboGame.Text + Environment.NewLine +
                                "Trainer Path: " + comboTrainer.Text + Environment.NewLine;
            for (int i = 0; i < 4; i++)
            {
                configInfo += $"Additional {i + 1}: {(string.IsNullOrEmpty(comboAdditional[i].Text) ? "[Not Set]" : comboAdditional[i].Text)} - {(chkAdditional[i].Checked ? "Inject" : "Launch/Inject")}" + Environment.NewLine;
            }
            configInfo += Environment.NewLine + "Recent Log:" + Environment.NewLine + txtStatusLog.Text;
            if (injectedDlls.Count > 0)
            {
                configInfo += Environment.NewLine + "Injected DLLs:" + Environment.NewLine;
                foreach (var dll in injectedDlls.Keys)
                    configInfo += dll + Environment.NewLine;
            }
            tvInfoText.Text = configInfo;
        }

        // ---------- Process List Overlay for TV Mode ----------
        private class ProcessListItem
        {
            public Process Proc { get; private set; }
            public ProcessListItem(Process proc) { Proc = proc; }
            public override string ToString()
            {
                return $"{Proc.ProcessName} (PID {Proc.Id})";
            }
        }

        private void ShowProcessListOverlay()
        {
            ListBox processListBox = new ListBox();
            processListBox.Dock = DockStyle.Fill;
            processListBox.Font = new Font("Tahoma", 14, FontStyle.Bold);
            processListBox.BackColor = Color.FromArgb(30,30,30);
            processListBox.ForeColor = Color.White;
            processListBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    HideOverlay();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Enter && processListBox.SelectedItem != null)
                {
                    ProcessListItem pli = processListBox.SelectedItem as ProcessListItem;
                    if (pli != null)
                    {
                        gameProcess = pli.Proc;
                        LogStatus("Selected process: " + pli.ToString());
                        HideOverlay();
                    }
                    e.SuppressKeyPress = true;
                }
            };
            processListBox.DoubleClick += (s, e) =>
            {
                if (processListBox.SelectedItem != null)
                {
                    ProcessListItem pli = processListBox.SelectedItem as ProcessListItem;
                    if (pli != null)
                    {
                        gameProcess = pli.Proc;
                        LogStatus("Selected process: " + pli.ToString());
                        HideOverlay();
                    }
                }
            };
            Process[] procs = ProcessHelper.GetProcesses();
            foreach (Process p in procs)
            {
                try { processListBox.Items.Add(new ProcessListItem(p)); } catch { }
            }
            ShowOverlay(processListBox);
            processListBox.Focus();
        }

        // ---------- File Browser Functions ----------
        private void SetupFileBrowserPanel()
        {
            fileBrowserPanel = new Panel();
            fileBrowserPanel.Dock = DockStyle.Fill;
            fileBrowserPanel.BackColor = Color.FromArgb(50,50,50);
            fileBrowserPanel.Padding = new Padding(20);
            fileBrowserPanel.Visible = false;
            fileBrowserPanel.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    fileBrowserPanel.Visible = false;
                    e.SuppressKeyPress = true;
                }
            };
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
                else if (fileBrowserPanel.Visible)
                {
                    fileBrowserPanel.Visible = false;
                    e.SuppressKeyPress = true;
                    return;
                }
                else if (chkTVMode.Checked)
                {
                    // In TV mode, Esc will not exit the mode
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

        // --- Robust Process Spawner ---
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
                int attempts = 3;
                while (attempts-- > 0)
                {
                    try
                    {
                        Process proc = RawLaunchProcess(path);
                        LogStatus($"{label}: LAUNCHED via CreateProcess");
                        AddRecent(comboTrainer, path);
                        return proc;
                    }
                    catch (Exception ex)
                    {
                        LogStatus($"{label}: LAUNCH ATTEMPT FAILED via CreateProcess ({ex.Message}). Retrying...");
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                LogStatus($"{label}: FAILED after multiple attempts.");
                return null;
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
            bool result = Win32API.CreateProcess(
                path,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                0,
                IntPtr.Zero,
                Path.GetDirectoryName(path),
                ref si,
                out pi);
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
        // --- End Robust Process Spawner ---

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

        // ***** SINGLE SaveSettings() Method (duplicates removed) *****
        private void SaveSettings()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(settingsFile))
                {
                    sw.WriteLine("AUTO-LAUNCH=" + chkAutoLaunch.Checked);
                    sw.WriteLine("FULLSCREEN=" + chkFullscreen.Checked);
                }
                LogStatus("Settings saved.");
            }
            catch (Exception ex)
            {
                LogStatus("SaveSettings error: " + ex.Message);
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

        private void XinputTimer_Tick(object sender, EventArgs e)
        {
            XINPUT_STATE state;
            uint result = XInput.XInputGetState(0, out state);
            const short thumbDeadzone = 20000;
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
                    if (tvModePanel.Visible && tvModeList.SelectedItems.Count > 0)
                        HandleTVModeAction(tvModeList.SelectedItems[0].Text);
                    else
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
                bool aBtn = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_A) != 0;
                if (aBtn && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_A) == 0)
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

        // ----------------------- Missing Event Handler Definitions -----------------------
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
                    LogStatus("Profile deleted: " + profileName);
                    UpdateProfileList();
                }
            }
            else
                MessageBox.Show("Profile not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (!trainerLaunched)
            {
                string trainerPath = comboTrainer.Text.Trim();
                if (string.IsNullOrEmpty(trainerPath) || !File.Exists(trainerPath))
                {
                    LogStatus("Trainer path is invalid.");
                }
                else
                {
                    Process trainerProc = null;
                    try
                    {
                        if (chkCMDLaunch.Checked)
                        {
                            trainerProc = LaunchTrainerViaCMD(trainerPath);
                            LogStatus("Trainer launched via CMD.");
                        }
                        else
                        {
                            int method = 0;
                            for (int i = 0; i < rdoLaunchMethods.Length; i++)
                            {
                                if (rdoLaunchMethods[i].Checked)
                                {
                                    method = i;
                                    break;
                                }
                            }
                            switch (method)
                            {
                                case 0:
                                    trainerProc = LaunchTrainerMethod0(trainerPath);
                                    break;
                                case 1:
                                    trainerProc = LaunchTrainerMethod1(trainerPath);
                                    break;
                                case 2:
                                    trainerProc = LaunchTrainerMethod2(trainerPath);
                                    break;
                                case 3:
                                    trainerProc = LaunchTrainerMethod3(trainerPath);
                                    break;
                                case 4:
                                    trainerProc = LaunchTrainerMethod4(trainerPath);
                                    break;
                                case 5:
                                    trainerProc = LaunchTrainerMethod5(trainerPath);
                                    break;
                            }
                            LogStatus("Trainer launched via method " + (method + 1));
                        }
                        if (trainerProc != null)
                        {
                            trainerLaunched = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogStatus("Trainer launch failed: " + ex.Message);
                    }
                }
            }
            else
            {
                LogStatus("Trainer already launched, skipping duplicate launch.");
            }
            // Handle Additional Injection items:
            for (int i = 0; i < 4; i++)
            {
                string file = comboAdditional[i].Text.Trim();
                if (string.IsNullOrEmpty(file))
                    continue;
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".dll")
                {
                    if (gameProcess != null && !gameProcess.HasExited)
                    {
                        if (!injectedDlls.ContainsKey(file))
                        {
                            InjectDll(gameProcess, file);
                            injectedDlls[file] = true;
                        }
                    }
                }
                else
                {
                    LaunchProcessFromPath(file, $"Additional EXE {i + 1}");
                }
            }
        }
        
        // New: DLL Injection function
        private void InjectDll(Process targetProcess, string dllPath)
        {
            try
            {
                IntPtr hKernel32 = NativeMethods.GetModuleHandle("kernel32.dll");
                if (hKernel32 == IntPtr.Zero)
                {
                    LogStatus("Failed to get handle of kernel32.dll.");
                    return;
                }
                IntPtr loadLibraryAddr = NativeMethods.GetProcAddress(hKernel32, "LoadLibraryA");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    LogStatus("Failed to get address of LoadLibraryA.");
                    return;
                }
                byte[] dllBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + "\0");
                IntPtr allocMemAddress = NativeMethods.VirtualAllocEx(targetProcess.Handle, IntPtr.Zero, (uint)dllBytes.Length, NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE, NativeMethods.PAGE_READWRITE);
                if (allocMemAddress == IntPtr.Zero)
                {
                    LogStatus("Failed to allocate memory in target process.");
                    return;
                }
                UIntPtr bytesWritten;
                bool result = NativeMethods.WriteProcessMemory(targetProcess.Handle, allocMemAddress, dllBytes, (uint)dllBytes.Length, out bytesWritten);
                if (!result || bytesWritten.ToUInt32() != dllBytes.Length)
                {
                    LogStatus("Failed to write DLL path into target process memory.");
                    return;
                }
                uint threadId;
                IntPtr hThread = NativeMethods.CreateRemoteThread(targetProcess.Handle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, out threadId);
                if (hThread == IntPtr.Zero)
                {
                    LogStatus("Failed to create remote thread for DLL injection.");
                    return;
                }
                NativeMethods.WaitForSingleObject(hThread, NativeMethods.INFINITE);
                NativeMethods.CloseHandle(hThread);
                NativeMethods.VirtualFreeEx(targetProcess.Handle, allocMemAddress, 0, NativeMethods.MEM_RELEASE);
                LogStatus($"DLL Injected: {Path.GetFileName(dllPath)} into {targetProcess.ProcessName} (PID {targetProcess.Id})");
            }
            catch(Exception ex)
            {
                LogStatus("DLL Injection error: " + ex.Message);
            }
        }
        
        private Process LaunchTrainerViaCMD(string path)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/C \"" + path + "\"";
            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;
            return Process.Start(psi);
        }

        private Process LaunchTrainerMethod0(string path)
        {
            return RawLaunchProcess(path);
        }

        private Process LaunchTrainerMethod1(string path)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path) ?? "",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            return Process.Start(psi);
        }

        private Process LaunchTrainerMethod2(string path)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path) ?? "",
                UseShellExecute = true,
                CreateNoWindow = false
            };
            return Process.Start(psi);
        }

        private Process LaunchTrainerMethod3(string path)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path) ?? "",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };
            return Process.Start(psi);
        }

        private Process LaunchTrainerMethod4(string path)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/C start \"\" \"" + path + "\"";
            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;
            return Process.Start(psi);
        }

        private Process LaunchTrainerMethod5(string path)
        {
            return RawLaunchProcess(path);
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
        // ----------------------- End Missing Event Handler Definitions -----------------------

        // Fix for missing method: PopulateRunningExes
        private void PopulateRunningExes()
        {
            comboRunningExes.Items.Clear();
            Process[] procs = ProcessHelper.GetProcesses();
            foreach (Process p in procs)
            {
                comboRunningExes.Items.Add(p);
            }
        }

        // Fix for missing method: ProcessCommandLineArgs
        private void ProcessCommandLineArgs()
        {
            // No command-line argument processing implemented.
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
    } // End of MainForm class

    // Added definition for MemoryRegionDump to fix build errors.
    public class MemoryRegionDump
    {
        public IntPtr BaseAddress { get; set; }
        public uint RegionSize { get; set; }
        public byte[] MemoryContents { get; set; }
    }

    // ***** Program class moved OUTSIDE of MainForm *****
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unhandled exception: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
