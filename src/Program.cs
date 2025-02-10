#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Net.Http;
using System.IO.Compression;
using Microsoft.Win32;  // For registry access
// Alias for System.Windows.Forms.Timer.
using WinFormsTimer = System.Windows.Forms.Timer;

namespace ChooChooApp
{
    // ----- XInput Definitions -----
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

    // ----- NativeMethods Definitions -----
    public static class NativeMethods
    {
        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint PAGE_READWRITE = 0x04;
        public const uint MEM_RELEASE = 0x8000;
        public const uint INFINITE = 0xFFFFFFFF;
        public const uint THREAD_SUSPEND_RESUME = 0x0002;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

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

        [DllImport("kernel32.dll")]
        public static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);
    }

    // ----- MainForm Definition -----
    public class MainForm : Form
    {
        // UI Controls.
        private Panel mainPanel;
        private TabControl tabControl;
        private TabPage tabPageMain;
        private TabPage tabPageHelp;
        private Label arrowIndicator;

        // Paths group.
        private GroupBox groupPaths;
        private Label labelGamePath;
        private ComboBox comboGame;
        private Button btnBrowseGame;
        private Button btnBrowseTrainer;
        private Label labelTrainerPath;
        private ComboBox comboTrainer;
        private CheckBox[] chkAdditional;
        private ComboBox[] comboAdditional;
        private Button[] btnBrowseAdditional;
        private string?[] additionalInjectedFile;

        // Settings & Profiles.
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

        // Timers.
        private WinFormsTimer xinputTimer;
        private WinFormsTimer injectionTimer;
        private WinFormsTimer arrowAnimationTimer;
        private double arrowAnimationTime;
        private int arrowAnimationOffset;
        private int arrowAnimationAmplitude = 3;
        private double arrowAnimationPeriod = 3.0;

        // Analog input flags.
        private bool analogDownSent, analogUpSent, analogLeftSent, analogRightSent;

        // Data files.
        private readonly string recentFile = "recent.ini";
        private readonly string profilesDir = "profiles";
        private readonly string settingsFile = "settings.ini";

        // XInput tracking.
        private XINPUT_STATE prevXInputState;
        private byte prevLeftTrigger, prevRightTrigger;

        // DLL validation tracking.
        private Dictionary<string, bool> validatedDlls;

        // Flags for modal popups.
        private bool isMagicInputOpen = false;
        private bool isSecretPopupOpen = false;

        // Game process.
        private Process? gameProcess;

        // Flag to prevent multiple launches.
        private bool hasLaunched = false;

        public MainForm()
        {
            // Initialize arrays and collections.
            mainPanel = new Panel();
            tabControl = new TabControl();
            tabPageMain = new TabPage("Main");
            tabPageHelp = new TabPage("Help");
            arrowIndicator = new Label();

            groupPaths = new GroupBox();
            labelGamePath = new Label();
            comboGame = new ComboBox();
            btnBrowseGame = new Button();
            btnBrowseTrainer = new Button();
            labelTrainerPath = new Label();
            comboTrainer = new ComboBox();
            chkAdditional = new CheckBox[5];
            comboAdditional = new ComboBox[5];
            btnBrowseAdditional = new Button[5];
            additionalInjectedFile = new string?[5];

            chkAutoLaunch = new CheckBox();
            groupProfiles = new GroupBox();
            comboProfiles = new ComboBox();
            btnRefreshProfiles = new Button();
            btnLoadProfile = new Button();
            btnSaveProfile = new Button();
            btnDeleteProfile = new Button();
            txtStatusLog = new TextBox();
            btnLaunch = new Button();
            listBoxDlls = new ListBox();

            xinputTimer = new WinFormsTimer();
            injectionTimer = new WinFormsTimer();
            arrowAnimationTimer = new WinFormsTimer();

            validatedDlls = new Dictionary<string, bool>();

            // Global key handler: clear active ComboBox when X is pressed.
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            InitializeComponent();
            AttachFocusHandlers(this);
            this.ActiveControl = comboGame;
            UpdateArrowIndicator();
            LogStatus("CHOOCHOO INJECTION ENGINE STARTED");

            LoadRecentsForCombo(comboGame);
            LoadRecentsForCombo(comboTrainer);
            for (int i = 0; i < 5; i++)
                LoadRecentsForCombo(comboAdditional[i]);
            UpdateProfileList();

            chkAutoLaunch.CheckedChanged += (s, e) =>
            {
                SaveSettings();
                LogStatus(chkAutoLaunch.Checked ? "SETTINGS SAVED AND AUTO-LAUNCH ENABLED" : "AUTO-LAUNCH DISABLED");
            };

            // Refresh DLL list once on first load.
            RefreshDllList();

            XINPUT_STATE state;
            if (XInput.XInputGetState(0, out state) == 0)
                LogStatus("CONTROLLER: DETECTED");

            arrowAnimationTimer.Start();

            injectionTimer = new WinFormsTimer { Interval = 1000 };
            injectionTimer.Tick += InjectionTimer_Tick;
            injectionTimer.Start();

            ProcessCommandLineArgs();

            xinputTimer = new WinFormsTimer { Interval = 16 };
            xinputTimer.Tick += XinputTimer_Tick;
            xinputTimer.Start();

            this.FormClosing += (s, e) => SaveSettings();
        }

        // Process command-line arguments.
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
                        {
                            LoadProfile(filePath);
                        }
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
                    while (j < args.Length && !args[j].StartsWith("-") && index < 5)
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

        // Attach focus handlers recursively.
        private void AttachFocusHandlers(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                ctrl.Enter += (s, e) => { UpdateArrowIndicator(); };
                if (ctrl.Controls.Count > 0)
                    AttachFocusHandlers(ctrl);
            }
        }

        // Update arrow indicator position.
        private void UpdateArrowIndicator()
        {
            try
            {
                Control active = GetDeepActiveControl(this);
                if (active != null && active.IsHandleCreated && active.Visible)
                {
                    Point activeScreenPoint = active.PointToScreen(new Point(0, 0));
                    Point clientPoint = this.PointToClient(activeScreenPoint);
                    int margin = 5;
                    int arrowX = clientPoint.X - arrowIndicator.Width - margin;
                    int arrowY = clientPoint.Y + (active.Height - arrowIndicator.Height) / 2;
                    if (arrowX < 0)
                    {
                        arrowX = clientPoint.X + active.Width + margin;
                    }
                    arrowX += arrowAnimationOffset;
                    arrowIndicator.Location = new Point(arrowX, arrowY);
                    arrowIndicator.BringToFront();
                    arrowIndicator.Visible = true;
                }
                else
                {
                    arrowIndicator.Visible = false;
                }
            }
            catch
            {
                arrowIndicator.Visible = false;
            }
        }

        // Get deepest active control.
        private Control GetDeepActiveControl(Control control)
        {
            while (control is ContainerControl container && container.ActiveControl != null)
            {
                control = container.ActiveControl;
            }
            return control;
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // If X is pressed, clear the text of the active ComboBox.
            if (e.KeyCode == Keys.X)
            {
                if (this.ActiveControl is ComboBox cb)
                {
                    cb.Text = "";
                    e.SuppressKeyPress = true;
                }
            }
            // If a dropdown is open and user presses B, close it.
            if (e.KeyCode == Keys.B)
            {
                if (this.ActiveControl is ComboBox cb2 && cb2.DroppedDown)
                {
                    cb2.DroppedDown = false;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "ChooChoo - Proton/Wine Trainer/DLL Loader";
            this.ClientSize = new Size(1200, 700);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            arrowIndicator.Text = "♦";
            arrowIndicator.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            arrowIndicator.ForeColor = Color.Black;
            arrowIndicator.AutoSize = true;
            this.Controls.Add(arrowIndicator);
            arrowIndicator.BringToFront();

            mainPanel.Dock = DockStyle.Fill;
            this.Controls.Add(mainPanel);

            tabControl.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(tabControl);
            tabControl.TabPages.Add(tabPageMain);
            tabControl.TabPages.Add(tabPageHelp);

            Panel mainTabPanel = new Panel { Dock = DockStyle.Fill };
            tabPageMain.Controls.Add(mainTabPanel);

            groupPaths = new GroupBox { Text = "Paths", Bounds = new Rectangle(10, 10, 850, 300) };
            mainTabPanel.Controls.Add(groupPaths);

            labelGamePath = new Label { Text = "Game Path:", Location = new Point(20, 30), AutoSize = true };
            groupPaths.Controls.Add(labelGamePath);

            comboGame = new ComboBox { Location = new Point(130, 30), Size = new Size(500, 25), DropDownStyle = ComboBoxStyle.DropDown, DropDownWidth = 600 };
            groupPaths.Controls.Add(comboGame);

            btnBrowseGame = new Button { Text = "Browse...", Location = new Point(640, 30), Size = new Size(100, 25) };
            btnBrowseGame.Click += (s, e) =>
            {
                string file = BrowseForFile();
                if (!string.IsNullOrEmpty(file))
                {
                    comboGame.Text = file;
                    AddRecent(comboGame, file);
                }
            };
            groupPaths.Controls.Add(btnBrowseGame);

            labelTrainerPath = new Label { Text = "Trainer Path:", Location = new Point(20, 70), AutoSize = true };
            groupPaths.Controls.Add(labelTrainerPath);

            comboTrainer = new ComboBox { Location = new Point(130, 70), Size = new Size(500, 25), DropDownStyle = ComboBoxStyle.DropDown, DropDownWidth = 600 };
            groupPaths.Controls.Add(comboTrainer);

            btnBrowseTrainer = new Button { Text = "Browse...", Location = new Point(640, 70), Size = new Size(100, 25) };
            btnBrowseTrainer.Click += (s, e) =>
            {
                string file = BrowseForFile();
                if (!string.IsNullOrEmpty(file))
                {
                    comboTrainer.Text = file;
                    AddRecent(comboTrainer, file);
                }
            };
            groupPaths.Controls.Add(btnBrowseTrainer);

            chkAdditional = new CheckBox[5];
            comboAdditional = new ComboBox[5];
            btnBrowseAdditional = new Button[5];
            additionalInjectedFile = new string?[5];
            for (int i = 0; i < 5; i++)
            {
                int y = 130 + i * 35;
                chkAdditional[i] = new CheckBox { Text = "Launch/Inject", Location = new Point(20, y) };
                int index = i;
                chkAdditional[i].CheckedChanged += (s, e) => UpdateAdditionalInjection(index);
                groupPaths.Controls.Add(chkAdditional[i]);

                comboAdditional[i] = new ComboBox { Location = new Point(130, y), Size = new Size(500, 25), DropDownStyle = ComboBoxStyle.DropDown, DropDownWidth = 600 };
                groupPaths.Controls.Add(comboAdditional[i]);

                btnBrowseAdditional[i] = new Button { Text = "Browse...", Location = new Point(640, y), Size = new Size(100, 25) };
                int indexLocal = i;
                btnBrowseAdditional[i].Click += (s, e) =>
                {
                    string file = BrowseForFile();
                    if (!string.IsNullOrEmpty(file))
                    {
                        comboAdditional[indexLocal].Text = file;
                        AddRecent(comboAdditional[indexLocal], file);
                        if (Path.GetExtension(file).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                            chkAdditional[indexLocal].Text = "Inject";
                        else
                            chkAdditional[indexLocal].Text = "Launch/Inject";
                    }
                };
                groupPaths.Controls.Add(btnBrowseAdditional[i]);
            }

            chkAutoLaunch = new CheckBox { Text = "Save Settings & Enable Autolaunch", Location = new Point(10, groupPaths.Bottom + 10), TabStop = true };
            mainTabPanel.Controls.Add(chkAutoLaunch);

            groupProfiles = new GroupBox { Text = "Profiles", Bounds = new Rectangle(870, 10, 300, 300) };
            mainTabPanel.Controls.Add(groupProfiles);

            comboProfiles = new ComboBox { Size = new Size(280, 25), Location = new Point((groupProfiles.ClientSize.Width - 280) / 2, 40), DropDownStyle = ComboBoxStyle.DropDown };
            groupProfiles.Controls.Add(comboProfiles);

            btnRefreshProfiles = new Button { Size = new Size(80, 30), Location = new Point(65, 80), Text = "Refresh" };
            btnRefreshProfiles.Click += BtnRefreshProfiles_Click;
            groupProfiles.Controls.Add(btnRefreshProfiles);

            btnLoadProfile = new Button { Size = new Size(80, 30), Location = new Point(65 + 80 + 10, 80), Text = "Load" };
            btnLoadProfile.Click += BtnLoadProfile_Click;
            groupProfiles.Controls.Add(btnLoadProfile);

            btnSaveProfile = new Button { Size = new Size(170, 30), Location = new Point((groupProfiles.ClientSize.Width - 170) / 2, 120), Text = "Save" };
            btnSaveProfile.Click += BtnSaveProfile_Click;
            groupProfiles.Controls.Add(btnSaveProfile);

            btnDeleteProfile = new Button { Size = new Size(170, 30), Location = new Point((groupProfiles.ClientSize.Width - 170) / 2, 160), Text = "Delete" };
            btnDeleteProfile.Click += BtnDeleteProfile_Click;
            groupProfiles.Controls.Add(btnDeleteProfile);

            txtStatusLog = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Bounds = new Rectangle(10, chkAutoLaunch.Bottom + 10, 850, 150) };
            mainTabPanel.Controls.Add(txtStatusLog);

            listBoxDlls = new ListBox { Location = new Point(txtStatusLog.Right + 10, txtStatusLog.Top), Size = new Size(300, txtStatusLog.Height), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            mainTabPanel.Controls.Add(listBoxDlls);

            Button btnRefreshDlls = new Button { Text = "Refresh DLL List", Location = new Point(listBoxDlls.Left, listBoxDlls.Bottom + 5), Size = new Size(140, 30) };
            btnRefreshDlls.Click += (s, e) => RefreshDllList();
            mainTabPanel.Controls.Add(btnRefreshDlls);

            Button btnExportDlls = new Button { Text = "Export DLL List", Location = new Point(listBoxDlls.Left + 150, listBoxDlls.Bottom + 5), Size = new Size(140, 30) };
            btnExportDlls.Click += (s, e) => ExportDllList();
            mainTabPanel.Controls.Add(btnExportDlls);

            Panel panelLaunch = new Panel { Location = new Point(10, txtStatusLog.Bottom + 40), Size = new Size(1170, 50) };
            mainTabPanel.Controls.Add(panelLaunch);

            btnLaunch = new Button { Text = "Launch", Size = new Size(panelLaunch.Width, panelLaunch.Height), Location = new Point(0, 0), Font = new Font("Segoe UI", 14, FontStyle.Bold) };
            btnLaunch.Click += BtnLaunch_Click;
            panelLaunch.Controls.Add(btnLaunch);

            HelpTab helpControl = new HelpTab { Dock = DockStyle.Fill };
            tabPageHelp.Controls.Add(helpControl);

            xinputTimer = new WinFormsTimer { Interval = 16 };
            xinputTimer.Tick += XinputTimer_Tick;
            xinputTimer.Start();

            injectionTimer = new WinFormsTimer { Interval = 1000 };
            injectionTimer.Tick += InjectionTimer_Tick;
            injectionTimer.Start();

            arrowAnimationTimer = new WinFormsTimer { Interval = 50 };
            arrowAnimationTimer.Tick += ArrowAnimationTimer_Tick;
        }

        #region Arrow Animation
        private void ArrowAnimationTimer_Tick(object? sender, EventArgs e)
        {
            arrowAnimationTime += arrowAnimationTimer.Interval / 1000.0;
            arrowAnimationOffset = (int)(arrowAnimationAmplitude * Math.Sin(2 * Math.PI * arrowAnimationTime / arrowAnimationPeriod));
            UpdateArrowIndicator();
        }
        #endregion

        #region "DLL CHECKER" Helpers
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
                    validatedDlls.ContainsKey(additionalInjectedFile[index]!))
                {
                    LogStatus($"DLL {Path.GetFileName(additionalInjectedFile[index])} UNLOADED!");
                    validatedDlls.Remove(additionalInjectedFile[index]!);
                    additionalInjectedFile[index] = null;
                    RefreshDllList();
                }
                return;
            }
            if (additionalInjectedFile[index] != file)
            {
                if (!string.IsNullOrEmpty(additionalInjectedFile[index]) &&
                    validatedDlls.ContainsKey(additionalInjectedFile[index]!))
                {
                    LogStatus($"DLL {Path.GetFileName(additionalInjectedFile[index])} UNLOADED!");
                    validatedDlls.Remove(additionalInjectedFile[index]!);
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
                        LogStatus($"DLL {Path.GetFileName(file)} IS 32-BIT; PLEASE SUPPLY A 64-BIT COMPATIBLE DLL. SKIPPING INJECTION.");
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
                    LogStatus($"DLL {Path.GetFileName(file)} IS VALID FOR INJECTION!");
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

        // Helper: Check if DLL is 64-bit.
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
            catch
            {
                return false;
            }
        }
        #endregion

        #region Other Helpers
        private void InjectionTimer_Tick(object? sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                if (chkAdditional[i].Checked)
                    UpdateAdditionalInjection(i);
            }
        }

        private void BtnRefreshProfiles_Click(object? sender, EventArgs e)
        {
            UpdateProfileList();
        }

        private void BtnLoadProfile_Click(object? sender, EventArgs e)
        {
            string profileName = comboProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter a profile name to load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void BtnSaveProfile_Click(object? sender, EventArgs e)
        {
            string profileName = comboProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter a profile name to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Directory.CreateDirectory(profilesDir);
            string filePath = Path.Combine(profilesDir, profileName + ".ini");
            SaveProfile(filePath);
            UpdateProfileList();
        }

        private void BtnDeleteProfile_Click(object? sender, EventArgs e)
        {
            string profileName = comboProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter a profile name to delete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string filePath = Path.Combine(profilesDir, profileName + ".ini");
            if (File.Exists(filePath))
            {
                var result = MessageBox.Show("Are you sure you want to delete profile '" + profileName + "'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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

        private void BtnLaunch_Click(object? sender, EventArgs e)
        {
            // Prevent multiple launches.
            if (hasLaunched)
                return;
            hasLaunched = true;

            Directory.CreateDirectory(profilesDir);
            string lastFile = Path.Combine(profilesDir, "last.ini");
            SaveProfile(lastFile);
            txtStatusLog.Clear();
            Process? proc = LaunchProcessFromPath(comboGame.Text, "Game");
            if (proc != null)
                gameProcess = proc;
            LaunchProcessFromPath(comboTrainer.Text, "Trainer");
            for (int i = 0; i < 5; i++)
            {
                string file = comboAdditional[i].Text.Trim();
                if (string.IsNullOrEmpty(file))
                    continue;
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".dll")
                    continue;
                LaunchProcessFromPath(file, "Additional EXE " + (i + 1));
            }
        }

        private Process? LaunchProcessFromPath(string path, string label)
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
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = path;
                psi.WorkingDirectory = Path.GetDirectoryName(path) ?? "";
                psi.UseShellExecute = true;
                Process proc = Process.Start(psi)!;
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
                for (int i = 0; i < 5; i++)
                {
                    sw.WriteLine((chkAdditional[i].Checked ? "1" : "0") + "," + comboAdditional[i].Text);
                }
            }
        }

        private void LoadProfile(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                comboGame.Text = sr.ReadLine() ?? "";
                comboTrainer.Text = sr.ReadLine() ?? "";
                for (int i = 0; i < 5; i++)
                {
                    string? line = sr.ReadLine();
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
            {
                all += (all == "" ? "" : ";") + (item?.ToString() ?? "");
            }
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
                    {
                        sw.WriteLine(item?.ToString() ?? "");
                    }
                }
                MessageBox.Show("DLL list exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Refresh the DLL list without interfering with scrolling.
        private void RefreshDllList()
        {
            int topIndex = listBoxDlls.TopIndex;
            List<string> dllList = new List<string>();
            dllList.Add("Current loaded DLLs/modules:");
            try
            {
                ProcessModuleCollection modules = Process.GetCurrentProcess().Modules;
                foreach (ProcessModule module in modules)
                {
                    string display = module.ModuleName;
                    dllList.Add(display);
                }
            }
            catch (Exception ex)
            {
                dllList.Add("Error: " + ex.Message);
            }
            // Force-add validated DLLs if not already in the list.
            foreach (var kvp in validatedDlls)
            {
                if (kvp.Value == true)
                {
                    string dllName = Path.GetFileName(kvp.Key);
                    if (!dllList.Contains(dllName))
                    {
                        dllList.Add(dllName + " (Validated)");
                    }
                }
            }
            listBoxDlls.BeginUpdate();
            listBoxDlls.Items.Clear();
            foreach (var item in dllList)
            {
                listBoxDlls.Items.Add(item);
            }
            listBoxDlls.EndUpdate();
            try { listBoxDlls.TopIndex = topIndex; } catch { }
        }

        // Process controller input only if no modal popup is open.
        private void XinputTimer_Tick(object? sender, EventArgs e)
        {
            if (isMagicInputOpen || isSecretPopupOpen)
                return;
            
            XINPUT_STATE state;
            uint result = XInput.XInputGetState(0, out state);
            if (result == 0)
            {
                if (this.ActiveControl is ComboBox activeCbDropdown && activeCbDropdown.DroppedDown)
                {
                    bool dpadDown = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_DOWN) != 0;
                    bool dpadUp = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_UP) != 0;
                    bool dpadLeft = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_LEFT) != 0;
                    bool dpadRight = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_RIGHT) != 0;
                    int currentIndex = activeCbDropdown.SelectedIndex;
                    if (dpadDown && currentIndex < activeCbDropdown.Items.Count - 1)
                        activeCbDropdown.SelectedIndex = currentIndex + 1;
                    if (dpadUp && currentIndex > 0)
                        activeCbDropdown.SelectedIndex = currentIndex - 1;
                    if (dpadLeft && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_LEFT) == 0)
                        SendKeys.Send("{LEFT}");
                    if (dpadRight && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_RIGHT) == 0)
                        SendKeys.Send("{RIGHT}");
                }
                else
                {
                    bool dpadDown = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_DOWN) != 0;
                    bool dpadUp = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_UP) != 0;
                    bool dpadLeft = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_LEFT) != 0;
                    bool dpadRight = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_RIGHT) != 0;
                    if (dpadDown && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_DOWN) == 0)
                        this.SelectNextControl(this.ActiveControl, true, true, true, true);
                    if (dpadUp && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_UP) == 0)
                        this.SelectNextControl(this.ActiveControl, false, true, true, true);
                    if (dpadLeft && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_LEFT) == 0)
                        SendKeys.Send("{LEFT}");
                    if (dpadRight && (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_RIGHT) == 0)
                        SendKeys.Send("{RIGHT}");
                }
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
                if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_START) != 0 &&
                    (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_START) == 0)
                {
                    btnLaunch.PerformClick();
                }
                bool leftThumbClicked = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_LEFT_THUMB) != 0;
                bool rightThumbClicked = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_RIGHT_THUMB) != 0;
                bool prevLeftThumbClicked = (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_LEFT_THUMB) != 0;
                bool prevRightThumbClicked = (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_RIGHT_THUMB) != 0;
                if (leftThumbClicked && rightThumbClicked &&
                    !(prevLeftThumbClicked && prevRightThumbClicked) && !isMagicInputOpen)
                {
                    ShowMagicCodeInput();
                }
                bool lbButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_LEFT_SHOULDER) != 0;
                bool rbButton = (state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0;
                if ((lbButton || rbButton) &&
                    ((prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_LEFT_SHOULDER) == 0 &&
                     (prevXInputState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_RIGHT_SHOULDER) == 0))
                {
                    if (groupPaths.Controls.Contains(this.ActiveControl))
                        chkAutoLaunch.Focus();
                    else if (this.ActiveControl == chkAutoLaunch)
                        comboProfiles.Focus();
                    else if (groupProfiles.Controls.Contains(this.ActiveControl))
                        comboGame.Focus();
                    else
                        chkAutoLaunch.Focus();
                }
                const byte triggerThreshold = 30;
                if (state.Gamepad.bLeftTrigger > triggerThreshold && prevLeftTrigger <= triggerThreshold)
                {
                    int newIndex = (tabControl.SelectedIndex - 1 + tabControl.TabCount) % tabControl.TabCount;
                    tabControl.SelectedIndex = newIndex;
                }
                if (state.Gamepad.bRightTrigger > triggerThreshold && prevRightTrigger <= triggerThreshold)
                {
                    int newIndex = (tabControl.SelectedIndex + 1) % tabControl.TabCount;
                    tabControl.SelectedIndex = newIndex;
                }
                const short thumbDeadzone = 8000;
                if (state.Gamepad.sThumbLY < -thumbDeadzone)
                {
                    if (!analogDownSent)
                    {
                        SendKeys.Send("{DOWN}");
                        analogDownSent = true;
                    }
                }
                else
                {
                    analogDownSent = false;
                }
                if (state.Gamepad.sThumbLY > thumbDeadzone)
                {
                    if (!analogUpSent)
                    {
                        SendKeys.Send("{UP}");
                        analogUpSent = true;
                    }
                }
                else
                {
                    analogUpSent = false;
                }
                if (state.Gamepad.sThumbLX < -thumbDeadzone)
                {
                    if (!analogLeftSent)
                    {
                        SendKeys.Send("{LEFT}");
                        analogLeftSent = true;
                    }
                }
                else
                {
                    analogLeftSent = false;
                }
                if (state.Gamepad.sThumbLX > thumbDeadzone)
                {
                    if (!analogRightSent)
                    {
                        SendKeys.Send("{RIGHT}");
                        analogRightSent = true;
                    }
                }
                else
                {
                    analogRightSent = false;
                }
                prevLeftTrigger = state.Gamepad.bLeftTrigger;
                prevRightTrigger = state.Gamepad.bRightTrigger;
                prevXInputState = state;
            }
            this.Invalidate();
        }

        private void ShowMagicCodeInput()
        {
            if (isMagicInputOpen) return;
            isMagicInputOpen = true;
            using (MagicInputForm mif = new MagicInputForm())
            {
                mif.StartPosition = FormStartPosition.CenterParent;
                mif.Activated += (s, e) => mif.Focus();
                if (mif.ShowDialog(this) == DialogResult.OK)
                {
                    ShowSecretStatus();
                }
            }
            isMagicInputOpen = false;
        }

        // When showing the secret disclaimer popup, disable XInput processing.
        public void ShowSecretStatus()
        {
            isSecretPopupOpen = true;
            xinputTimer.Stop();
            using (SecretStatusForm ssf = new SecretStatusForm())
            {
                ssf.StartPosition = FormStartPosition.CenterParent;
                DialogResult result = ssf.ShowDialog(this);
                isSecretPopupOpen = false;
                xinputTimer.Start();
                if (result == DialogResult.OK)
                {
                    ExecuteKonamiSecret();
                }
                else
                {
                    LogStatus("SECRET MODE CANCELLED (DISAGREED)");
                }
            }
        }

        private async void ExecuteKonamiSecret()
        {
            try
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string extractDir = Path.Combine(currentDir, "SecretRelease");
                using (SecretProgressForm progress = new SecretProgressForm())
                {
                    progress.Show();
                    progress.AppendMessage("CHECKING FOR SECRET FILES...");
                    if (Directory.Exists(extractDir) && Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories).Length > 0)
                    {
                        progress.AppendMessage("SECRET FILES FOUND. SKIPPING DOWNLOAD.");
                    }
                    else
                    {
                        progress.AppendMessage("DOWNLOADING SECRET RELEASE...");
                        using (HttpClient client = new HttpClient())
                        {
                            byte[] data = await client.GetByteArrayAsync("https://github.com/BigBoiCJ/SteamAutoCracker/releases/download/2.2.1-gui/Steam.Auto.Cracker.GUI.v2.2.1.zip");
                            string zipPath = Path.Combine(currentDir, "SteamAutoCracker.zip");
                            File.WriteAllBytes(zipPath, data);
                            progress.AppendMessage("DOWNLOAD COMPLETE.");
                            progress.AppendMessage("EXTRACTING SECRET RELEASE...");
                            if (Directory.Exists(extractDir))
                                Directory.Delete(extractDir, true);
                            Directory.CreateDirectory(extractDir);
                            ZipFile.ExtractToDirectory(zipPath, extractDir);
                            progress.AppendMessage("EXTRACTION COMPLETE.");
                        }
                    }
                    progress.AppendMessage("SEARCHING FOR SECRET EXECUTABLE...");
                    string foundExe = null;
                    foreach (string file in Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories))
                    {
                        if (file.IndexOf("steam_auto_cracker_gui", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            foundExe = file;
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(foundExe))
                    {
                        MessageBox.Show("SECRET EXECUTABLE NOT FOUND.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        progress.Close();
                        return;
                    }
                    progress.AppendMessage("SECRET MODE MENU FOUND!");
                    ProcessStartInfo psi = new ProcessStartInfo()
                    {
                        FileName = foundExe,
                        WorkingDirectory = Path.GetDirectoryName(foundExe),
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    progress.AppendMessage("SECRET MODE LAUNCHED!!");
                    progress.AppendMessage("SECRET MODE IS NOW ACTIVE.");
                    progress.Close();
                    LogStatus("SECRET MODE MENU FOUND!");
                    LogStatus("SECRET MODE LAUNCHED!!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR EXECUTING SECRET FUNCTION: " + ex.Message.ToUpper(), "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LogStatus(string message)
        {
            txtStatusLog.AppendText(message.ToUpper() + Environment.NewLine);
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
                        if (bool.TryParse(val, out bool autoLaunchValue))
                            chkAutoLaunch.Checked = autoLaunchValue;
                    }
                }
            }
        }

        private void SaveSettings()
        {
            File.WriteAllText(settingsFile, $"AUTO-LAUNCH={chkAutoLaunch.Checked}");
        }

        private string BrowseForFile()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                    return ofd.FileName;
            }
            return "";
        }

        // =========================
        // New methods for checking and installing .NET 6+ using embedded installer.
        // These methods try multiple approaches.
        // =========================

        /// <summary>
        /// Determines whether a valid .NET 6+ runtime (version ≥ 6.0.2) is installed.
        /// It checks by running "dotnet --version", "dotnet --list-runtimes", and by reading registry keys.
        /// </summary>
        private static bool IsDotNet6InstalledIntelligently()
        {
            // 1. Try "dotnet --version"
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("dotnet", "--version");
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (Process proc = Process.Start(psi)!)
                {
                    string output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    if (Version.TryParse(output, out Version ver))
                    {
                        if (ver >= new Version("6.0.2"))
                            return true;
                    }
                }
            }
            catch { }

            // 2. Try "dotnet --list-runtimes"
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("dotnet", "--list-runtimes");
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (Process proc = Process.Start(psi)!)
                {
                    string output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    string[] lines = output.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Microsoft.NETCore.App"))
                        {
                            // Expected format: "Microsoft.NETCore.App 6.0.8 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]"
                            string remainder = line.Substring("Microsoft.NETCore.App".Length).Trim();
                            string[] tokens = remainder.Split(' ');
                            if (tokens.Length > 0)
                            {
                                if (Version.TryParse(tokens[0], out Version ver))
                                {
                                    if (ver >= new Version("6.0.2"))
                                        return true;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 3. Check registry keys in HKLM and HKCU.
            if (CheckRegistryForDotNet6(Registry.LocalMachine))
                return true;
            if (CheckRegistryForDotNet6(Registry.CurrentUser))
                return true;

            return false;
        }

        /// <summary>
        /// Checks the specified registry root for installed versions of Microsoft.NETCore.App.
        /// Returns true if any version ≥ 6.0.2 is found.
        /// </summary>
        private static bool CheckRegistryForDotNet6(RegistryKey root)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            if (Version.TryParse(subKeyName, out Version ver))
                            {
                                if (ver >= new Version("6.0.2"))
                                    return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Ensures that a valid .NET 6+ runtime is installed.
        /// If not, runs the embedded installer.
        /// </summary>
        private static void EnsureDotNet6Installed()
        {
            if (!IsDotNet6InstalledIntelligently())
            {
                MessageBox.Show(".NET 6.0.2 or higher is required. The embedded installer will now run.", "Runtime Update Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                InstallDotNet();
            }
        }

        /// <summary>
        /// Extracts the embedded dotnet6.exe installer to a temporary file and runs it.
        /// After installation, informs the user and exits.
        /// </summary>
        private static void InstallDotNet()
        {
            try
            {
                string tempInstallerPath = Path.Combine(Path.GetTempPath(), "dotnet6.exe");
                // Since your folder/namespace is "ChooChooApp", the embedded resource name is "ChooChooApp.dotnet6.exe".
                using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ChooChooApp.dotnet6.exe"))
                {
                    if (stream == null)
                    {
                        MessageBox.Show("Embedded installer not found.", "Installer Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(1);
                    }
                    using (var fileStream = new FileStream(tempInstallerPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = tempInstallerPath;
                psi.UseShellExecute = true;
                Process proc = Process.Start(psi)!;
                proc.WaitForExit();
                try { File.Delete(tempInstallerPath); } catch { }
                if (proc.ExitCode != 0)
                {
                    MessageBox.Show("Embedded dotnet6 installer failed. Please install .NET 6.0.2 manually.", "Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
                else
                {
                    MessageBox.Show(".NET 6.0.2 installed successfully. Please restart the application.", "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while attempting to install .NET 6.0.2:\n" + ex.Message, "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }
        // =========================
        // End of .NET 6+ check and installer methods.
        // =========================
        #endregion

        [STAThread]
        static void Main()
        {
            // Before running the main application, ensure that .NET 6+ is installed.
            EnsureDotNet6Installed();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
