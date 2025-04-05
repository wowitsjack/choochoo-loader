using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ChooChooEngine.App.Core;
using ChooChooEngine.App.Injection;
using ChooChooEngine.App.Memory;
using ChooChooEngine.App.UI;

namespace ChooChooEngine.App.Forms
{
    public partial class MainForm : Form
    {
        private ProcessManager _processManager;
        private InjectionManager _injectionManager;
        private MemoryManager _memoryManager;
        private ResumePanel _resumePanel;
        
        // Tab control components
        private TabControl tabControl = new TabControl();
        private TabPage tabMain = new TabPage("Main");
        private TabPage tabHelp = new TabPage("Help");
        private TabPage tabTools = new TabPage("Tools");
        private Label lblHelpContent;
        private Label lblToolsContent;
        
        // Paths/Process Selection panel components
        private Panel panelPathsProcessSelection = new Panel();
        private Button btnRefreshProcesses = new Button();
        private ComboBox cmbRunningExe = new ComboBox();
        private ComboBox cmbGamePath = new ComboBox();
        private Button btnBrowseGame = new Button();
        private ComboBox cmbTrainerPath = new ComboBox();
        private Button btnBrowseTrainer = new Button();
        private CheckBox chkLaunchInject1 = new CheckBox();
        private ComboBox cmbDll1 = new ComboBox();
        private Button btnBrowseDll1 = new Button();
        private CheckBox chkLaunchInject2 = new CheckBox();
        private ComboBox cmbDll2 = new ComboBox();
        private Button btnBrowseDll2 = new Button();
        
        // Profiles panel components
        private Panel panelProfiles = new Panel();
        private ComboBox cmbProfiles = new ComboBox();
        private Button btnRefresh = new Button();
        private Button btnLoad = new Button();
        private Button btnSave = new Button();
        private Button btnDelete = new Button();
        private CheckBox chkAutoLoadLastProfile = new CheckBox();
        
        // Last used profile settings
        private string _lastUsedProfile = string.Empty;
        private bool _autoLoadLastProfile = false;
        
        // Console output
        private TextBox txtConsoleOutput = new TextBox();
        
        // Loaded DLLs
        private ListBox lstLoadedDlls = new ListBox();
        
        // Launch button
        private Button btnLaunch = new Button();
        
        // Launch methods panel
        private Panel panelLaunchMethods = new Panel();
        private RadioButton radCreateProcess = new RadioButton();
        private RadioButton radCmdStart = new RadioButton();
        private RadioButton radCreateThreadInjection = new RadioButton();
        private RadioButton radRemoteThreadInjection = new RadioButton();
        private RadioButton radShellExecute = new RadioButton();
        private RadioButton radProcessStart = new RadioButton();
        private FlowLayoutPanel launchMethodsFlow = new FlowLayoutPanel();
        
        // Status strip
        private StatusStrip statusStrip = new StatusStrip();
        private ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();
        
        // Recent file lists
        private List<string> _recentGamePaths = new List<string>();
        private List<string> _recentTrainerPaths = new List<string>();
        private List<string> _recentDllPaths = new List<string>();
        private List<string> _profiles = new List<string>();
        
        // Selected paths
        private string _selectedGamePath = string.Empty;
        private string _selectedTrainerPath = string.Empty;
        private string _selectedDll1Path = string.Empty;
        private string _selectedDll2Path = string.Empty;
        
        // Launch method
        private LaunchMethod _launchMethod = LaunchMethod.CreateProcess;
        
        private TableLayoutPanel mainLayout;
        private bool compactMode = false;
        private Timer resizeTimer;
        private TableLayoutPanel launchContainer;
        private Panel consolePanel;
        private Panel loadedDllsPanel;
        
        // ProfileInputDialog class for getting profile name
        public class ProfileInputDialog : Form
        {
            private TextBox txtProfileName;
            private Button btnOK;
            private Button btnCancel;
            private Label lblPrompt;
            
            public string ProfileName { get; private set; }
            
            public ProfileInputDialog(string title = "Enter Profile Name")
            {
                Text = title;
                Size = new Size(400, 180);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                
                // Configure controls
                lblPrompt = new Label
                {
                    Text = "Enter a name for this profile:",
                    Location = new Point(20, 20),
                    Size = new Size(360, 20),
                    Font = new Font("Segoe UI", 10)
                };
                
                txtProfileName = new TextBox
                {
                    Location = new Point(20, 50),
                    Size = new Size(360, 30),
                    Font = new Font("Segoe UI", 10),
                    BorderStyle = BorderStyle.FixedSingle,
                    TabIndex = 0
                };
                
                btnOK = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(205, 100),
                    Size = new Size(85, 30),
                    Font = new Font("Segoe UI", 9),
                    FlatStyle = FlatStyle.Flat,
                    TabIndex = 1
                };
                
                btnCancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(295, 100),
                    Size = new Size(85, 30),
                    Font = new Font("Segoe UI", 9),
                    FlatStyle = FlatStyle.Flat,
                    TabIndex = 2
                };
                
                // Add controls to form
                Controls.Add(lblPrompt);
                Controls.Add(txtProfileName);
                Controls.Add(btnOK);
                Controls.Add(btnCancel);
                
                // Set key events
                AcceptButton = btnOK;
                CancelButton = btnCancel;
                
                // Apply dark theme
                BackColor = Color.FromArgb(40, 40, 40);
                ForeColor = Color.White;
                txtProfileName.BackColor = Color.FromArgb(50, 50, 50);
                txtProfileName.ForeColor = Color.White;
                btnOK.BackColor = Color.FromArgb(60, 60, 60);
                btnOK.ForeColor = Color.White;
                btnOK.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
                btnCancel.BackColor = Color.FromArgb(60, 60, 60);
                btnCancel.ForeColor = Color.White;
                btnCancel.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            }
            
            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                txtProfileName.Focus();
                txtProfileName.SelectAll();
            }
            
            protected override void OnFormClosing(FormClosingEventArgs e)
            {
                base.OnFormClosing(e);
                
                if (DialogResult == DialogResult.OK)
                {
                    if (string.IsNullOrWhiteSpace(txtProfileName.Text))
                    {
                        MessageBox.Show("Please enter a profile name.", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.Cancel = true;
                        return;
                    }
                    
                    ProfileName = txtProfileName.Text.Trim();
                }
            }
        }
        
        // Command line arguments
        private string[] _args;
        private string _profileToLoad = string.Empty;
        private string _autoLaunchPath = string.Empty;
        private bool _autoLaunchRequested = false;
        
        public MainForm()
            : this(new string[0])
        {
        }
        
        public MainForm(string[] args)
        {
            _args = args;
            
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            
            // Position form in center of screen
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Configure tab control
            ConfigureTabControl();
            
            // Configure UI layout
            ConfigureUILayout();
            
            // Apply dark theme
            ApplyDarkTheme();
            
            // Initialize managers
            InitializeManagers();
            
            // Subscribe to events and initialize controls
            RegisterEventHandlers();
            
            // Load recent files and profiles
            LoadRecentFiles();
            LoadProfiles();
            
            // Process command line arguments
            ProcessCommandLineArguments();
            
            // Load app settings
            LoadAppSettings();
            
            // Start resize timer
            resizeTimer = new Timer();
            resizeTimer.Interval = 100;
            resizeTimer.Tick += (s, e) => CheckLayoutMode();
            
            // Initial layout check
            CheckLayoutMode();
        }
        
        private void InitializeManagers()
        {
            _processManager = new ProcessManager();
            _injectionManager = new InjectionManager(_processManager);
            _memoryManager = new MemoryManager(_processManager);
            _resumePanel = new ResumePanel();
            
            // Subscribe to manager events
            _processManager.ProcessStarted += ProcessManager_ProcessStarted;
            _processManager.ProcessStopped += ProcessManager_ProcessStopped;
            _processManager.ProcessAttached += ProcessManager_ProcessAttached;
            _processManager.ProcessDetached += ProcessManager_ProcessDetached;
            
            _injectionManager.InjectionSucceeded += InjectionManager_InjectionSucceeded;
            _injectionManager.InjectionFailed += InjectionManager_InjectionFailed;
            
            _memoryManager.MemoryOperationSucceeded += MemoryManager_MemoryOperationSucceeded;
            _memoryManager.MemoryOperationFailed += MemoryManager_MemoryOperationFailed;
            
            _resumePanel.Resumed += ResumePanel_Resumed;
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Save settings and recent files
            SaveAppSettings();
            SaveRecentFiles();

            // Clean up resources
            if (_injectionManager != null)
            {
                _injectionManager.StopMonitoring();
            }

            if (_processManager != null)
            {
                _processManager.DetachFromProcess();
            }

            if (_resumePanel != null)
            {
                _resumePanel.Dispose();
            }
        }
        
        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            // Debounce resize events to avoid excessive layout changes
            if (resizeTimer == null)
            {
                resizeTimer = new Timer();
                resizeTimer.Interval = 200;
                resizeTimer.Tick += (s, args) => {
                    CheckLayoutMode();
                    resizeTimer.Stop();
                };
            }
            
            resizeTimer.Stop();
            resizeTimer.Start();
        }
        
        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            CheckLayoutMode();
        }
        
        private void CheckLayoutMode()
        {
            // Check if we need to switch to compact mode based on form width
            bool shouldBeCompact = this.Width < 950;
            
            if (compactMode != shouldBeCompact)
            {
                compactMode = shouldBeCompact;
                UpdateLayoutForCurrentMode();
            }
        }
        
        private void UpdateLayoutForCurrentMode()
        {
            if (compactMode)
            {
                // Switch to compact layout
                mainLayout.RowStyles[0] = new RowStyle(SizeType.Percent, 55F); // More space for top panels
                mainLayout.RowStyles[1] = new RowStyle(SizeType.Percent, 20F); // Less for console/DLLs
                mainLayout.RowStyles[2] = new RowStyle(SizeType.Percent, 25F); // Same for buttons
                
                // Stack the top panels vertically in compact mode
                mainLayout.SetColumn(panelProfiles, 0);  // Move profiles panel below the injection panel
                mainLayout.SetRow(panelProfiles, 1);     // In the middle row
                mainLayout.SetColumn(consolePanel, 1);   // Move console to the right
                mainLayout.SetRow(consolePanel, 0);      // In the top row
                mainLayout.SetColumn(loadedDllsPanel, 1); // Keep DLLs panel on the right
                mainLayout.SetRow(loadedDllsPanel, 1);    // Below console
                
                // Make launch methods more visible in compact mode
                mainLayout.SetColumnSpan(launchContainer, 2); // Span full width
                
                // When in very compact mode, ensure the console and DLL panels have minimum height
                int minPanelHeight = 180;
                consolePanel.MinimumSize = new Size(0, minPanelHeight);
                loadedDllsPanel.MinimumSize = new Size(0, minPanelHeight);
            }
            else
            {
                // Switch back to standard layout
                mainLayout.RowStyles[0] = new RowStyle(SizeType.Percent, 48F);
                mainLayout.RowStyles[1] = new RowStyle(SizeType.Percent, 27F);
                mainLayout.RowStyles[2] = new RowStyle(SizeType.Percent, 25F);
                
                // Restore the original panel arrangement
                mainLayout.SetColumn(panelPathsProcessSelection, 0);
                mainLayout.SetRow(panelPathsProcessSelection, 0);
                mainLayout.SetColumn(panelProfiles, 1);
                mainLayout.SetRow(panelProfiles, 0);
                mainLayout.SetColumn(consolePanel, 0);
                mainLayout.SetRow(consolePanel, 1);
                mainLayout.SetColumn(loadedDllsPanel, 1);
                mainLayout.SetRow(loadedDllsPanel, 1);
                
                // Restore launch container
                mainLayout.SetColumnSpan(launchContainer, 2);
                
                // Reset minimum sizes in standard mode to allow proper docking
                consolePanel.MinimumSize = new Size(0, 0);
                loadedDllsPanel.MinimumSize = new Size(0, 0);
            }
            
            // Adjust card sizes based on mode
            ResizeCardHeights();
        }
        
        private void ResizeCardHeights()
        {
            if (compactMode)
            {
                // Adjust card heights in compact mode - use fixed heights for predictability
                int cardContainerHeight = panelPathsProcessSelection.Height - 100; // Account for header and padding
                int targetCardHeight = Math.Max(60, cardContainerHeight / 4);
                int dllCardHeight = Math.Max(90, cardContainerHeight / 3);
                int trainerCardHeight = Math.Max(90, cardContainerHeight / 3);
                
                // Make sure we have a cardsContainer reference
                if (panelPathsProcessSelection.Controls.Count > 1 && 
                    panelPathsProcessSelection.Controls[0] is Panel pathsContent &&
                    pathsContent.Controls.Count > 0 &&
                    pathsContent.Controls[0] is TableLayoutPanel cardsContainer)
                {
                    cardsContainer.RowStyles[0] = new RowStyle(SizeType.Absolute, targetCardHeight);
                    cardsContainer.RowStyles[1] = new RowStyle(SizeType.Absolute, dllCardHeight);
                    cardsContainer.RowStyles[2] = new RowStyle(SizeType.Absolute, trainerCardHeight);
                }
            }
            else
            {
                // In normal mode, use percentage-based sizing
                // Find cardsContainer and reset row styles
                if (panelPathsProcessSelection.Controls.Count > 1 && 
                    panelPathsProcessSelection.Controls[0] is Panel pathsContent &&
                    pathsContent.Controls.Count > 0 &&
                    pathsContent.Controls[0] is TableLayoutPanel cardsContainer)
                {
                    cardsContainer.RowStyles[0] = new RowStyle(SizeType.Percent, 25F);
                    cardsContainer.RowStyles[1] = new RowStyle(SizeType.Percent, 37.5F);
                    cardsContainer.RowStyles[2] = new RowStyle(SizeType.Percent, 37.5F);
                }
            }
        }
        
        private void ApplyDarkTheme()
        {
            // Apply dark theme to form and controls
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
        }
        
        private void ConfigureTabControl()
        {
            // Setup tab control
            tabControl.Dock = DockStyle.Fill;
            tabControl.Appearance = TabAppearance.FlatButtons;
            tabControl.ItemSize = new Size(80, 30);
            tabControl.Font = new Font("Segoe UI", 10);
            tabControl.SizeMode = TabSizeMode.Fixed;
            
            // Style the tabs
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += (s, e) => {
                Graphics g = e.Graphics;
                Rectangle tabRect = tabControl.GetTabRect(e.Index);
                TabPage page = tabControl.TabPages[e.Index];
                bool isSelected = (tabControl.SelectedIndex == e.Index);
                
                // Fill tab background
                Brush backBrush = isSelected ? new SolidBrush(Color.FromArgb(45, 45, 45)) : new SolidBrush(Color.FromArgb(30, 30, 30));
                g.FillRectangle(backBrush, tabRect);
                
                // Draw text
                string text = page.Text;
                Font font = isSelected ? new Font("Segoe UI", 10, FontStyle.Bold) : new Font("Segoe UI", 10);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(text, font, Brushes.White, tabRect, sf);
                
                // Draw a border at the bottom if selected
                if (isSelected)
                {
                    Pen borderPen = new Pen(Color.FromArgb(0, 120, 215), 3);
                    g.DrawLine(borderPen, tabRect.Left, tabRect.Bottom - 2, tabRect.Right, tabRect.Bottom - 2);
                }
            };
            
            // Configure tab pages
            tabControl.Controls.Add(tabMain);
            tabControl.Controls.Add(tabHelp);
            tabControl.Controls.Add(tabTools);
            
            // Add tab control to form
            this.Controls.Add(tabControl);
            
            // Set selected tab
            tabControl.SelectedTab = tabMain;
        }
        
        private void ConfigureUILayout()
        {
            // Use TableLayoutPanel for main layout
            mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.RowCount = 3;
            mainLayout.ColumnCount = 2;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));  // Top row for panels
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 27F));  // Middle row for console/DLLs
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));  // Bottom row for buttons/methods
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));  // Left column (wider)
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));  // Right column (narrower)
            mainLayout.Padding = new Padding(3);
            mainLayout.AutoScroll = true;  // Enable auto-scrolling for very small sizes
            
            // ============================================================================================
            // COMPLETELY REDESIGNED TOP-LEFT PANEL WITH SIMPLE BUTTON-BASED NAVIGATION
            // ============================================================================================
            panelPathsProcessSelection.BorderStyle = BorderStyle.FixedSingle;
            panelPathsProcessSelection.Dock = DockStyle.Fill;
            panelPathsProcessSelection.Padding = new Padding(0);
            panelPathsProcessSelection.BackColor = Color.FromArgb(25, 25, 25);
            
            // Create header panel
            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 40;
            headerPanel.BackColor = Color.FromArgb(40, 40, 40);
            
            // Header content
            TableLayoutPanel headerContent = new TableLayoutPanel();
            headerContent.Dock = DockStyle.Fill;
            headerContent.ColumnCount = 2;
            headerContent.RowCount = 1;
            headerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            headerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            headerContent.Padding = new Padding(10, 5, 10, 5);
            headerContent.Margin = new Padding(0);
            
            Label headerLabel = new Label();
            headerLabel.Text = "INJECTION SETUP";
            headerLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            headerLabel.ForeColor = Color.White;
            headerLabel.TextAlign = ContentAlignment.MiddleLeft;
            headerLabel.Dock = DockStyle.Fill;
            
            btnRefreshProcesses.Text = "Refresh Processes";
            btnRefreshProcesses.FlatStyle = FlatStyle.Flat;
            btnRefreshProcesses.FlatAppearance.BorderSize = 0;
            btnRefreshProcesses.BackColor = Color.FromArgb(0, 120, 215);
            btnRefreshProcesses.ForeColor = Color.White;
            btnRefreshProcesses.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btnRefreshProcesses.Dock = DockStyle.Fill;
            btnRefreshProcesses.Click += (s, e) => RefreshProcessList();
            
            headerContent.Controls.Add(headerLabel, 0, 0);
            headerContent.Controls.Add(btnRefreshProcesses, 1, 0);
            headerPanel.Controls.Add(headerContent);
            
            // Create a main content area with navigation buttons and content panels
            TableLayoutPanel mainContentPanel = new TableLayoutPanel();
            mainContentPanel.Dock = DockStyle.Fill;
            mainContentPanel.ColumnCount = 2;
            mainContentPanel.RowCount = 1;
            mainContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F)); // Nav buttons column - wider to prevent cropping
            mainContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Content column
            mainContentPanel.Margin = new Padding(0);
            
            // Navigation panel with buttons
            Panel navPanel = new Panel();
            navPanel.Dock = DockStyle.Fill;
            navPanel.BackColor = Color.FromArgb(30, 30, 30);
            navPanel.Padding = new Padding(5);
            
            // Content panel to hold our "pages"
            Panel contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.FromArgb(35, 35, 35);
            contentPanel.Padding = new Padding(0);
            
            // Create navigation buttons and content panels
            Button btnTrainerSetup = CreateNavButton("Trainer Setup", true);
            Button btnTargetProcess = CreateNavButton("Target Process & DLL Injection (Optional)", false);
            
            // Stack navigation buttons
            FlowLayoutPanel navButtonsFlow = new FlowLayoutPanel();
            navButtonsFlow.Dock = DockStyle.Fill;
            navButtonsFlow.FlowDirection = FlowDirection.TopDown;
            navButtonsFlow.WrapContents = false;
            navButtonsFlow.AutoScroll = false;
            navButtonsFlow.Padding = new Padding(0, 5, 0, 5);
            
            navButtonsFlow.Controls.Add(btnTrainerSetup);
            navButtonsFlow.Controls.Add(btnTargetProcess);
            navPanel.Controls.Add(navButtonsFlow);
            
            // Create content pages - all docked to fill but initially hidden
            Panel trainerSetupPanel = CreateContentPanel();
            Panel targetProcessPanel = CreateContentPanel();
            
            // Hide all except the first panel
            trainerSetupPanel.Visible = true;
            targetProcessPanel.Visible = false;
            
            // Add navigation button click handlers
            btnTrainerSetup.Click += (s, e) => {
                SetActiveNavButton(btnTrainerSetup);
                ShowPanel(trainerSetupPanel, new[] { targetProcessPanel });
            };
            
            btnTargetProcess.Click += (s, e) => {
                SetActiveNavButton(btnTargetProcess);
                ShowPanel(targetProcessPanel, new[] { trainerSetupPanel });
            };
            
            // Add content panels to the content area
            contentPanel.Controls.Add(trainerSetupPanel);
            contentPanel.Controls.Add(targetProcessPanel);
            
            // Add nav panel and content panel to main content
            mainContentPanel.Controls.Add(navPanel, 0, 0);
            mainContentPanel.Controls.Add(contentPanel, 1, 0);
            
            // Helper methods for styling
            Button CreateNavButton(string text, bool isActive = false)
            {
                Button btn = new Button();
                btn.Text = text;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = isActive ? Color.FromArgb(45, 45, 45) : Color.FromArgb(35, 35, 35);
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 10, isActive ? FontStyle.Bold : FontStyle.Regular);
                btn.Size = new Size(200, 35);
                btn.Margin = new Padding(5, 3, 5, 5);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(10, 0, 0, 0);
                btn.AutoEllipsis = true;
                // Add a left border to show active state
                if (isActive)
                {
                    btn.FlatAppearance.BorderSize = 3;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
                }
                btn.Tag = isActive; // Store active state
                return btn;
            }
            
            Panel CreateContentPanel()
            {
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;
                panel.BackColor = Color.FromArgb(35, 35, 35);
                panel.Padding = new Padding(15);
                return panel;
            }
            
            void SetActiveNavButton(Button activeButton)
            {
                // Update all buttons in the flow panel
                foreach (Control c in navButtonsFlow.Controls)
                {
                    if (c is Button btn)
                    {
                        bool isActive = (btn == activeButton);
                        btn.BackColor = isActive ? Color.FromArgb(45, 45, 45) : Color.FromArgb(35, 35, 35);
                        btn.Font = new Font("Segoe UI", 10, isActive ? FontStyle.Bold : FontStyle.Regular);
                        btn.FlatAppearance.BorderSize = isActive ? 3 : 0;
                        btn.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
                        btn.Tag = isActive;
                    }
                }
            }
            
            void ShowPanel(Panel panelToShow, Panel[] panelsToHide)
            {
                panelToShow.Visible = true;
                panelToShow.BringToFront();
                foreach (Panel panel in panelsToHide)
                {
                    panel.Visible = false;
                }
            }
            
            // 1. TARGET PROCESS PANEL CONTENT - merged with DLL Injection
            TableLayoutPanel processContent = new TableLayoutPanel();
            processContent.Dock = DockStyle.Fill;
            processContent.ColumnCount = 1;
            processContent.RowCount = 5; // Process selector + DLL injection (header + 2 rows) + spacing
            processContent.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Process header
            processContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // Process selector
            processContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F)); // Spacing
            processContent.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // DLL header
            processContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DLL panels
            
            // Process Selection section
            Label processLabel = new Label();
            processLabel.Text = "Select the process to inject into (optional):";
            processLabel.ForeColor = Color.FromArgb(200, 200, 200);
            processLabel.Font = new Font("Segoe UI", 9);
            processLabel.AutoSize = true;
            processLabel.Dock = DockStyle.Top;
            processLabel.Padding = new Padding(0, 0, 0, 8);
            processContent.Controls.Add(processLabel, 0, 0);
            
            // Process selector with dropdown style
            Panel processPanel = new Panel();
            processPanel.Dock = DockStyle.Fill;
            processPanel.Padding = new Padding(0);
            
            cmbRunningExe.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRunningExe.BackColor = Color.FromArgb(45, 45, 45);
            cmbRunningExe.ForeColor = Color.White;
            cmbRunningExe.FlatStyle = FlatStyle.Flat;
            cmbRunningExe.Font = new Font("Segoe UI", 9.5f);
            cmbRunningExe.Dock = DockStyle.Fill;
            cmbRunningExe.Margin = new Padding(5, 8, 5, 8);
            processPanel.Controls.Add(cmbRunningExe);
            
            processContent.Controls.Add(processPanel, 0, 1);
            
            // Add spacing row
            Panel spacingPanel = new Panel();
            spacingPanel.BackColor = Color.Transparent;
            processContent.Controls.Add(spacingPanel, 0, 2);
            
            // DLL Injection section
            Label dllLabel = new Label();
            dllLabel.Text = "Select DLLs to inject (optional):";
            dllLabel.ForeColor = Color.FromArgb(200, 200, 200);
            dllLabel.Font = new Font("Segoe UI", 9);
            dllLabel.AutoSize = true;
            dllLabel.Dock = DockStyle.Top;
            dllLabel.Padding = new Padding(0, 0, 0, 10);
            processContent.Controls.Add(dllLabel, 0, 3);
            
            // DLL Panel container
            Panel dllPanelContainer = new Panel();
            dllPanelContainer.Dock = DockStyle.Fill;
            dllPanelContainer.Padding = new Padding(0);
            
            // Create DLL section using the same approach as trainer setup
            TableLayoutPanel dllsTable = new TableLayoutPanel();
            dllsTable.Dock = DockStyle.Fill;
            dllsTable.ColumnCount = 1;
            dllsTable.RowCount = 2;
            dllsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            dllsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            
            // DLL 1 Row
            Panel dll1Panel = new Panel();
            dll1Panel.Dock = DockStyle.Fill;
            dll1Panel.BackColor = Color.FromArgb(35, 35, 35);
            dll1Panel.Padding = new Padding(5);
            
            // Use TableLayoutPanel for precise control
            TableLayoutPanel dll1Layout = new TableLayoutPanel();
            dll1Layout.Dock = DockStyle.Fill;
            dll1Layout.ColumnCount = 4;
            dll1Layout.RowCount = 1;
            dll1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F)); // DLL label - wider
            dll1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F)); // Inject checkbox - wider
            dll1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // DLL dropdown
            dll1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F)); // Browse button
            dll1Layout.Padding = new Padding(0);
            dll1Layout.Margin = new Padding(0);
            dll1Layout.AutoSize = false;
            dll1Layout.Height = 35;
            
            Label dll1Label = new Label();
            dll1Label.Text = "DLL 1:";
            dll1Label.ForeColor = Color.White;
            dll1Label.Font = new Font("Segoe UI", 9);
            dll1Label.Dock = DockStyle.Fill;
            dll1Label.TextAlign = ContentAlignment.MiddleLeft;
            dll1Label.Padding = new Padding(5, 0, 0, 0);
            
            chkLaunchInject1.Text = "Inject";
            chkLaunchInject1.BackColor = Color.Transparent;
            chkLaunchInject1.ForeColor = Color.White; 
            chkLaunchInject1.Font = new Font("Segoe UI", 9);
            chkLaunchInject1.Dock = DockStyle.Fill;
            chkLaunchInject1.TextAlign = ContentAlignment.MiddleLeft;
            chkLaunchInject1.Padding = new Padding(0);
            chkLaunchInject1.Margin = new Padding(0);
            
            cmbDll1.BackColor = Color.FromArgb(45, 45, 45);
            cmbDll1.ForeColor = Color.White;
            cmbDll1.FlatStyle = FlatStyle.Flat;
            cmbDll1.Font = new Font("Segoe UI", 9.5f);
            cmbDll1.Dock = DockStyle.Fill;
            cmbDll1.Margin = new Padding(5, 3, 5, 3);
            cmbDll1.Height = 29;
            
            btnBrowseDll1.Text = "Browse...";
            btnBrowseDll1.BackColor = Color.FromArgb(60, 60, 60);
            btnBrowseDll1.ForeColor = Color.White;
            btnBrowseDll1.FlatStyle = FlatStyle.Flat;
            btnBrowseDll1.FlatAppearance.BorderSize = 0;
            btnBrowseDll1.Font = new Font("Segoe UI", 9);
            btnBrowseDll1.Dock = DockStyle.None;
            btnBrowseDll1.Size = new Size(85, 29);
            btnBrowseDll1.Margin = new Padding(5, 3, 5, 3);
            btnBrowseDll1.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            
            dll1Layout.Controls.Add(dll1Label, 0, 0);
            dll1Layout.Controls.Add(chkLaunchInject1, 1, 0);
            dll1Layout.Controls.Add(cmbDll1, 2, 0);
            dll1Layout.Controls.Add(btnBrowseDll1, 3, 0);
            
            dll1Panel.Controls.Add(dll1Layout);
            dllsTable.Controls.Add(dll1Panel, 0, 0);
            
            // DLL 2 Row - using identical styling and layout as DLL 1
            Panel dll2Panel = new Panel();
            dll2Panel.Dock = DockStyle.Fill;
            dll2Panel.BackColor = Color.FromArgb(35, 35, 35);
            dll2Panel.Padding = new Padding(5);
            
            // Use TableLayoutPanel for precise control - match DLL 1 exactly
            TableLayoutPanel dll2Layout = new TableLayoutPanel();
            dll2Layout.Dock = DockStyle.Fill;
            dll2Layout.ColumnCount = 4;
            dll2Layout.RowCount = 1;
            dll2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F)); // DLL label - wider
            dll2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F)); // Inject checkbox - wider
            dll2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // DLL dropdown
            dll2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F)); // Browse button
            dll2Layout.Padding = new Padding(0);
            dll2Layout.Margin = new Padding(0);
            dll2Layout.AutoSize = false;
            dll2Layout.Height = 35;
            
            Label dll2Label = new Label();
            dll2Label.Text = "DLL 2:";
            dll2Label.ForeColor = Color.White;
            dll2Label.Font = new Font("Segoe UI", 9);
            dll2Label.Dock = DockStyle.Fill;
            dll2Label.TextAlign = ContentAlignment.MiddleLeft;
            dll2Label.Padding = new Padding(5, 0, 0, 0);
            
            chkLaunchInject2.Text = "Inject";
            chkLaunchInject2.BackColor = Color.Transparent;
            chkLaunchInject2.ForeColor = Color.White;
            chkLaunchInject2.Font = new Font("Segoe UI", 9);
            chkLaunchInject2.Dock = DockStyle.Fill;
            chkLaunchInject2.TextAlign = ContentAlignment.MiddleLeft;
            chkLaunchInject2.Padding = new Padding(0);
            chkLaunchInject2.Margin = new Padding(0);
            
            cmbDll2.BackColor = Color.FromArgb(45, 45, 45);
            cmbDll2.ForeColor = Color.White;
            cmbDll2.FlatStyle = FlatStyle.Flat;
            cmbDll2.Font = new Font("Segoe UI", 9.5f);
            cmbDll2.Dock = DockStyle.Fill;
            cmbDll2.Margin = new Padding(5, 3, 5, 3);
            cmbDll2.Height = 29;
            
            btnBrowseDll2.Text = "Browse...";
            btnBrowseDll2.BackColor = Color.FromArgb(60, 60, 60);
            btnBrowseDll2.ForeColor = Color.White;
            btnBrowseDll2.FlatStyle = FlatStyle.Flat;
            btnBrowseDll2.FlatAppearance.BorderSize = 0;
            btnBrowseDll2.Font = new Font("Segoe UI", 9);
            btnBrowseDll2.Dock = DockStyle.None;
            btnBrowseDll2.Size = new Size(85, 29);
            btnBrowseDll2.Margin = new Padding(5, 3, 5, 3);
            btnBrowseDll2.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            
            dll2Layout.Controls.Add(dll2Label, 0, 0);
            dll2Layout.Controls.Add(chkLaunchInject2, 1, 0);
            dll2Layout.Controls.Add(cmbDll2, 2, 0);
            dll2Layout.Controls.Add(btnBrowseDll2, 3, 0);
            
            dll2Panel.Controls.Add(dll2Layout);
            dllsTable.Controls.Add(dll2Panel, 0, 1);
            
            dllPanelContainer.Controls.Add(dllsTable);
            processContent.Controls.Add(dllPanelContainer, 0, 4);
            
            targetProcessPanel.Controls.Add(processContent);
            
            // 3. TRAINER SETUP PANEL CONTENT
            TableLayoutPanel trainerContent = new TableLayoutPanel();
            trainerContent.Dock = DockStyle.Fill;
            trainerContent.ColumnCount = 1;  // Single column for consistent layout
            trainerContent.RowCount = 3;
            trainerContent.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header text
            trainerContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Game path row - increased height
            trainerContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Trainer path row - increased height
            trainerContent.Padding = new Padding(10, 0, 10, 10);
            
            Label trainerLabel = new Label();
            trainerLabel.Text = "Set up game and trainer paths (optional):";
            trainerLabel.ForeColor = Color.FromArgb(200, 200, 200);
            trainerLabel.Font = new Font("Segoe UI", 9);
            trainerLabel.AutoSize = true;
            trainerLabel.Dock = DockStyle.Top;
            trainerLabel.Padding = new Padding(0, 0, 0, 10);
            trainerContent.Controls.Add(trainerLabel, 0, 0);
            
            // Configure cmbGamePath and cmbTrainerPath with consistent settings
            cmbGamePath.BackColor = Color.FromArgb(45, 45, 45);
            cmbGamePath.ForeColor = Color.White;
            cmbGamePath.FlatStyle = FlatStyle.Flat;
            cmbGamePath.Font = new Font("Segoe UI", 9.5f);
            cmbGamePath.Dock = DockStyle.None;
            cmbGamePath.Size = new Size(290, 29);
            cmbGamePath.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            
            cmbTrainerPath.BackColor = Color.FromArgb(45, 45, 45);
            cmbTrainerPath.ForeColor = Color.White;
            cmbTrainerPath.FlatStyle = FlatStyle.Flat;
            cmbTrainerPath.Font = new Font("Segoe UI", 9.5f);
            cmbTrainerPath.Dock = DockStyle.None;
            cmbTrainerPath.Size = new Size(290, 29);
            cmbTrainerPath.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            
            // Apply consistent styling to buttons
            btnBrowseGame.Size = new Size(85, 29);
            btnBrowseGame.AutoSize = false;
            btnBrowseGame.Text = "Browse...";
            btnBrowseGame.BackColor = Color.FromArgb(60, 60, 60);
            btnBrowseGame.ForeColor = Color.White;
            btnBrowseGame.FlatStyle = FlatStyle.Flat;
            btnBrowseGame.FlatAppearance.BorderSize = 0;
            btnBrowseGame.Font = new Font("Segoe UI", 9);
            btnBrowseGame.Anchor = AnchorStyles.Right;
            
            btnBrowseTrainer.Size = new Size(85, 29);
            btnBrowseTrainer.AutoSize = false;
            btnBrowseTrainer.Text = "Browse...";
            btnBrowseTrainer.BackColor = Color.FromArgb(60, 60, 60);
            btnBrowseTrainer.ForeColor = Color.White;
            btnBrowseTrainer.FlatStyle = FlatStyle.Flat;
            btnBrowseTrainer.FlatAppearance.BorderSize = 0;
            btnBrowseTrainer.Font = new Font("Segoe UI", 9);
            btnBrowseTrainer.Anchor = AnchorStyles.Right;
            
            // Create panels for each row with fixed layout
            Panel gamePathRow = new Panel();
            gamePathRow.Dock = DockStyle.Fill;
            gamePathRow.Padding = new Padding(0);
            gamePathRow.Margin = new Padding(0);
            
            Panel trainerPathRow = new Panel();
            trainerPathRow.Dock = DockStyle.Fill;
            trainerPathRow.Padding = new Padding(0);
            trainerPathRow.Margin = new Padding(0);
            
            // Add controls to each row with absolute positioning
            Label gamePathLabel = new Label();
            gamePathLabel.Text = "Launch Game Binary (Optional):";
            gamePathLabel.ForeColor = Color.White;
            gamePathLabel.Font = new Font("Segoe UI", 9);
            gamePathLabel.Size = new Size(180, 29);
            gamePathLabel.Location = new Point(5, 10);
            gamePathLabel.TextAlign = ContentAlignment.MiddleLeft;
            
            Label trainerPathLabel = new Label();
            trainerPathLabel.Text = "Trainer Path:";
            trainerPathLabel.ForeColor = Color.White;
            trainerPathLabel.Font = new Font("Segoe UI", 9);
            trainerPathLabel.Size = new Size(90, 29);
            trainerPathLabel.Location = new Point(5, 10);
            trainerPathLabel.TextAlign = ContentAlignment.MiddleLeft;
            
            // Position the ComboBoxes and Buttons - ensure proper initial position
            const int LABEL_WIDTH = 180;
            const int BUTTON_WIDTH = 85;
            const int MARGIN = 10;
            const int CONTROL_HEIGHT = 29;
            const int VERTICAL_POSITION = 10;
            
            // Position game path labels and controls with consistent layout
            gamePathLabel.Size = new Size(LABEL_WIDTH, CONTROL_HEIGHT);
            gamePathLabel.Location = new Point(MARGIN, VERTICAL_POSITION);
            
            btnBrowseGame.Size = new Size(BUTTON_WIDTH, CONTROL_HEIGHT);
            btnBrowseGame.Margin = new Padding(0);
            btnBrowseGame.Padding = new Padding(0);
            btnBrowseGame.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnBrowseGame.Location = new Point(gamePathRow.ClientSize.Width - BUTTON_WIDTH - MARGIN, VERTICAL_POSITION);
            
            cmbGamePath.Size = new Size(gamePathRow.ClientSize.Width - LABEL_WIDTH - BUTTON_WIDTH - (MARGIN * 3), CONTROL_HEIGHT);
            cmbGamePath.Location = new Point(LABEL_WIDTH + MARGIN, VERTICAL_POSITION);
            cmbGamePath.Margin = new Padding(0);
            cmbGamePath.Padding = new Padding(0);
            cmbGamePath.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            
            // Position trainer path labels and controls with identical layout
            trainerPathLabel.Size = new Size(LABEL_WIDTH, CONTROL_HEIGHT);
            trainerPathLabel.Location = new Point(MARGIN, VERTICAL_POSITION);
            
            btnBrowseTrainer.Size = new Size(BUTTON_WIDTH, CONTROL_HEIGHT);
            btnBrowseTrainer.Margin = new Padding(0);
            btnBrowseTrainer.Padding = new Padding(0);
            btnBrowseTrainer.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnBrowseTrainer.Location = new Point(trainerPathRow.ClientSize.Width - BUTTON_WIDTH - MARGIN, VERTICAL_POSITION);
            
            cmbTrainerPath.Size = new Size(trainerPathRow.ClientSize.Width - LABEL_WIDTH - BUTTON_WIDTH - (MARGIN * 3), CONTROL_HEIGHT);
            cmbTrainerPath.Location = new Point(LABEL_WIDTH + MARGIN, VERTICAL_POSITION);
            cmbTrainerPath.Margin = new Padding(0);
            cmbTrainerPath.Padding = new Padding(0);
            cmbTrainerPath.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            
            // Handle resize events with identical handling for both rows
            gamePathRow.SizeChanged += (s, e) => {
                int availableWidth = gamePathRow.ClientSize.Width;
                btnBrowseGame.Location = new Point(availableWidth - BUTTON_WIDTH - MARGIN, VERTICAL_POSITION);
                cmbGamePath.Width = availableWidth - LABEL_WIDTH - BUTTON_WIDTH - (MARGIN * 3);
            };
            
            trainerPathRow.SizeChanged += (s, e) => {
                int availableWidth = trainerPathRow.ClientSize.Width;
                btnBrowseTrainer.Location = new Point(availableWidth - BUTTON_WIDTH - MARGIN, VERTICAL_POSITION);
                cmbTrainerPath.Width = availableWidth - LABEL_WIDTH - BUTTON_WIDTH - (MARGIN * 3);
            };
            
            // Add controls to panels
            gamePathRow.Controls.Add(gamePathLabel);
            gamePathRow.Controls.Add(cmbGamePath);
            gamePathRow.Controls.Add(btnBrowseGame);
            
            trainerPathRow.Controls.Add(trainerPathLabel);
            trainerPathRow.Controls.Add(cmbTrainerPath);
            trainerPathRow.Controls.Add(btnBrowseTrainer);
            
            // Add rows to trainer content
            trainerContent.Controls.Add(gamePathRow, 0, 1);
            trainerContent.Controls.Add(trainerPathRow, 0, 2);
            
            trainerSetupPanel.Controls.Add(trainerContent);
            
            // Add panels to the main panel
            panelPathsProcessSelection.Controls.Add(mainContentPanel);
            panelPathsProcessSelection.Controls.Add(headerPanel);
            
            // ============================================================================================
            // END OF REDESIGNED TOP-LEFT PANEL
            // ============================================================================================
            
            // Configure Profiles panel
            panelProfiles.BorderStyle = BorderStyle.FixedSingle;
            panelProfiles.Dock = DockStyle.Fill;
            panelProfiles.Padding = new Padding(0);
            panelProfiles.BackColor = Color.FromArgb(25, 25, 25);
            
            // Create a header for the profiles panel to match the style
            TableLayoutPanel profilesHeader = new TableLayoutPanel();
            profilesHeader.Dock = DockStyle.Top;
            profilesHeader.Height = 40;
            profilesHeader.BackColor = Color.FromArgb(40, 40, 40);
            profilesHeader.RowCount = 1;
            profilesHeader.ColumnCount = 1;
            profilesHeader.Margin = new Padding(0);
            profilesHeader.Padding = new Padding(10, 5, 10, 5);
            
            Label profilesHeaderLabel = new Label();
            profilesHeaderLabel.Text = "PROFILES";
            profilesHeaderLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            profilesHeaderLabel.ForeColor = Color.White;
            profilesHeaderLabel.Dock = DockStyle.Fill;
            profilesHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            
            profilesHeader.Controls.Add(profilesHeaderLabel, 0, 0);
            
            // Create a TableLayoutPanel for profile controls
            TableLayoutPanel profilesLayout = new TableLayoutPanel();
            profilesLayout.Dock = DockStyle.Fill;
            profilesLayout.RowCount = 5;
            profilesLayout.ColumnCount = 2;
            profilesLayout.Padding = new Padding(10);
            profilesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Profiles combo
            profilesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Refresh/Delete
            profilesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Load
            profilesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Auto-load checkbox
            profilesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Save
            profilesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            profilesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            
            // Profile selection combobox
            cmbProfiles.Dock = DockStyle.Fill;
            cmbProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProfiles.BackColor = Color.FromArgb(45, 45, 45);
            cmbProfiles.ForeColor = Color.White;
            cmbProfiles.FlatStyle = FlatStyle.Flat;
            cmbProfiles.Font = new Font("Segoe UI", 9.5f);
            profilesLayout.Controls.Add(cmbProfiles, 0, 0);
            profilesLayout.SetColumnSpan(cmbProfiles, 2);
            
            // Profile action buttons
            btnRefresh.Text = "Refresh";
            btnRefresh.Dock = DockStyle.Fill;
            btnRefresh.Margin = new Padding(0, 5, 3, 0);
            btnRefresh.BackColor = Color.FromArgb(60, 60, 60);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Font = new Font("Segoe UI", 9.5f);
            profilesLayout.Controls.Add(btnRefresh, 0, 1);
            
            btnDelete.Text = "Delete";
            btnDelete.Dock = DockStyle.Fill;
            btnDelete.Margin = new Padding(3, 5, 0, 0);
            btnDelete.BackColor = Color.FromArgb(180, 30, 30);
            btnDelete.ForeColor = Color.White;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Font = new Font("Segoe UI", 9.5f);
            profilesLayout.Controls.Add(btnDelete, 1, 1);
            
            btnLoad.Text = "Load";
            btnLoad.Dock = DockStyle.Fill;
            btnLoad.Margin = new Padding(0, 5, 0, 0);
            btnLoad.BackColor = Color.FromArgb(0, 120, 215);
            btnLoad.ForeColor = Color.White;
            btnLoad.FlatStyle = FlatStyle.Flat;
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Font = new Font("Segoe UI", 9.5f);
            profilesLayout.Controls.Add(btnLoad, 0, 2);
            profilesLayout.SetColumnSpan(btnLoad, 2);
            
            btnSave.Text = "Save";
            btnSave.Dock = DockStyle.Bottom;
            btnSave.Height = 40;
            btnSave.Margin = new Padding(0, 10, 0, 0);
            btnSave.BackColor = Color.FromArgb(0, 160, 70);
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            profilesLayout.Controls.Add(btnSave, 0, 4);
            profilesLayout.SetColumnSpan(btnSave, 2);
            
            // Auto-load last profile checkbox
            chkAutoLoadLastProfile = new CheckBox();
            chkAutoLoadLastProfile.Text = "Auto-load last used profile on startup";
            chkAutoLoadLastProfile.ForeColor = Color.White;
            chkAutoLoadLastProfile.BackColor = Color.Transparent;
            chkAutoLoadLastProfile.Dock = DockStyle.Fill;
            chkAutoLoadLastProfile.Checked = _autoLoadLastProfile;
            chkAutoLoadLastProfile.CheckedChanged += ChkAutoLoadLastProfile_CheckedChanged;
            profilesLayout.Controls.Add(chkAutoLoadLastProfile, 0, 3);
            profilesLayout.SetColumnSpan(chkAutoLoadLastProfile, 2);
            
            panelProfiles.Controls.Add(profilesLayout);
            panelProfiles.Controls.Add(profilesHeader);
            
            // Configure console output textbox - less height
            txtConsoleOutput.Dock = DockStyle.Fill;
            txtConsoleOutput.Multiline = true;
            txtConsoleOutput.ReadOnly = true;
            txtConsoleOutput.BackColor = Color.Black;
            txtConsoleOutput.ForeColor = Color.LightGreen;
            txtConsoleOutput.Font = new Font("Consolas", 9);
            txtConsoleOutput.ScrollBars = ScrollBars.Vertical;
            
            // Create a panel for console output
            consolePanel = new Panel();
            consolePanel.BorderStyle = BorderStyle.FixedSingle;
            consolePanel.Dock = DockStyle.Fill;
            consolePanel.Padding = new Padding(0);
            consolePanel.Controls.Add(txtConsoleOutput);
            
            // Configure loaded DLLs list
            loadedDllsPanel = new Panel();
            loadedDllsPanel.BorderStyle = BorderStyle.FixedSingle;
            loadedDllsPanel.Dock = DockStyle.Fill;
            loadedDllsPanel.Padding = new Padding(0);
            loadedDllsPanel.BackColor = Color.FromArgb(25, 25, 25);
            
            TableLayoutPanel dllsHeader = new TableLayoutPanel();
            dllsHeader.Dock = DockStyle.Top;
            dllsHeader.Height = 40;
            dllsHeader.BackColor = Color.FromArgb(40, 40, 40);
            dllsHeader.RowCount = 1;
            dllsHeader.ColumnCount = 1;
            dllsHeader.Margin = new Padding(0);
            dllsHeader.Padding = new Padding(10, 5, 10, 5);
            
            Label dllsHeaderLabel = new Label();
            dllsHeaderLabel.Text = "LOADED DLLS/MODULES";
            dllsHeaderLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dllsHeaderLabel.ForeColor = Color.White;
            dllsHeaderLabel.Dock = DockStyle.Fill;
            dllsHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            
            dllsHeader.Controls.Add(dllsHeaderLabel, 0, 0);
            
            lstLoadedDlls.Dock = DockStyle.Fill;
            lstLoadedDlls.BackColor = Color.FromArgb(40, 40, 40);
            lstLoadedDlls.ForeColor = Color.White;
            lstLoadedDlls.BorderStyle = BorderStyle.None;
            lstLoadedDlls.Font = new Font("Segoe UI", 9);
            
            Panel dllsListContainer = new Panel();
            dllsListContainer.Dock = DockStyle.Fill;
            dllsListContainer.Padding = new Padding(5);
            dllsListContainer.Controls.Add(lstLoadedDlls);
            
            loadedDllsPanel.Controls.Add(dllsListContainer);
            loadedDllsPanel.Controls.Add(dllsHeader);
            
            // Configure Launch button
            btnLaunch.Text = "Launch";
            btnLaunch.Dock = DockStyle.Fill;
            btnLaunch.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnLaunch.BackColor = Color.FromArgb(0, 120, 215);
            btnLaunch.ForeColor = Color.White;
            btnLaunch.FlatStyle = FlatStyle.Flat;
            btnLaunch.FlatAppearance.BorderSize = 0;
            
            // Launch methods panel
            panelLaunchMethods.BorderStyle = BorderStyle.FixedSingle;
            panelLaunchMethods.Dock = DockStyle.Fill;
            panelLaunchMethods.Padding = new Padding(0);
            panelLaunchMethods.BackColor = Color.FromArgb(25, 25, 25);
            
            TableLayoutPanel methodsHeader = new TableLayoutPanel();
            methodsHeader.Dock = DockStyle.Top;
            methodsHeader.Height = 40;
            methodsHeader.BackColor = Color.FromArgb(40, 40, 40);
            methodsHeader.RowCount = 1;
            methodsHeader.ColumnCount = 1;
            methodsHeader.Margin = new Padding(0);
            methodsHeader.Padding = new Padding(10, 5, 10, 5);
            
            Label methodsHeaderLabel = new Label();
            methodsHeaderLabel.Text = "LAUNCH METHODS";
            methodsHeaderLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            methodsHeaderLabel.ForeColor = Color.White;
            methodsHeaderLabel.Dock = DockStyle.Fill;
            methodsHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            
            methodsHeader.Controls.Add(methodsHeaderLabel, 0, 0);
            
            // Create a better layout for the radio buttons - FlowLayout with styled buttons
            launchMethodsFlow = new FlowLayoutPanel();
            launchMethodsFlow.Dock = DockStyle.Fill;
            launchMethodsFlow.FlowDirection = FlowDirection.LeftToRight;
            launchMethodsFlow.WrapContents = true;
            launchMethodsFlow.Padding = new Padding(5);
            launchMethodsFlow.BackColor = Color.FromArgb(30, 30, 30);
            // Center alignment properties
            launchMethodsFlow.AutoSize = true;
            launchMethodsFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            
            // Setup styled radio buttons
            radCreateProcess.Text = "P/Invoke CreateProcess";
            radCreateProcess.AutoSize = false;
            radCreateProcess.Size = new Size(180, 30);
            radCreateProcess.FlatStyle = FlatStyle.Flat;
            radCreateProcess.Appearance = Appearance.Button;
            radCreateProcess.BackColor = Color.FromArgb(0, 120, 215); // Default selected
            radCreateProcess.ForeColor = Color.White;
            radCreateProcess.TextAlign = ContentAlignment.MiddleCenter;
            radCreateProcess.Margin = new Padding(5);
            radCreateProcess.Font = new Font("Segoe UI", 9);
            
            radCmdStart.Text = "CMD Start";
            radCmdStart.AutoSize = false;
            radCmdStart.Size = new Size(140, 30);
            radCmdStart.FlatStyle = FlatStyle.Flat;
            radCmdStart.Appearance = Appearance.Button;
            radCmdStart.BackColor = Color.FromArgb(60, 60, 60);
            radCmdStart.ForeColor = Color.White;
            radCmdStart.TextAlign = ContentAlignment.MiddleCenter;
            radCmdStart.Margin = new Padding(5);
            radCmdStart.Font = new Font("Segoe UI", 9);
            
            radCreateThreadInjection.Text = "Create Thread Injection";
            radCreateThreadInjection.AutoSize = false;
            radCreateThreadInjection.Size = new Size(180, 30);
            radCreateThreadInjection.FlatStyle = FlatStyle.Flat;
            radCreateThreadInjection.Appearance = Appearance.Button;
            radCreateThreadInjection.BackColor = Color.FromArgb(60, 60, 60);
            radCreateThreadInjection.ForeColor = Color.White;
            radCreateThreadInjection.TextAlign = ContentAlignment.MiddleCenter;
            radCreateThreadInjection.Margin = new Padding(5);
            radCreateThreadInjection.Font = new Font("Segoe UI", 9);
            
            radRemoteThreadInjection.Text = "Remote Thread Injection";
            radRemoteThreadInjection.AutoSize = false;
            radRemoteThreadInjection.Size = new Size(180, 30);
            radRemoteThreadInjection.FlatStyle = FlatStyle.Flat;
            radRemoteThreadInjection.Appearance = Appearance.Button;
            radRemoteThreadInjection.BackColor = Color.FromArgb(60, 60, 60);
            radRemoteThreadInjection.ForeColor = Color.White;
            radRemoteThreadInjection.TextAlign = ContentAlignment.MiddleCenter;
            radRemoteThreadInjection.Margin = new Padding(5);
            radRemoteThreadInjection.Font = new Font("Segoe UI", 9);
            
            radShellExecute.Text = "Shell Execute";
            radShellExecute.AutoSize = false;
            radShellExecute.Size = new Size(140, 30);
            radShellExecute.FlatStyle = FlatStyle.Flat;
            radShellExecute.Appearance = Appearance.Button;
            radShellExecute.BackColor = Color.FromArgb(60, 60, 60);
            radShellExecute.ForeColor = Color.White;
            radShellExecute.TextAlign = ContentAlignment.MiddleCenter;
            radShellExecute.Margin = new Padding(5);
            radShellExecute.Font = new Font("Segoe UI", 9);
            
            radProcessStart.Text = "Raw Process Start";
            radProcessStart.AutoSize = false;
            radProcessStart.Size = new Size(160, 30);
            radProcessStart.FlatStyle = FlatStyle.Flat;
            radProcessStart.Appearance = Appearance.Button;
            radProcessStart.BackColor = Color.FromArgb(60, 60, 60);
            radProcessStart.ForeColor = Color.White;
            radProcessStart.TextAlign = ContentAlignment.MiddleCenter;
            radProcessStart.Margin = new Padding(5);
            radProcessStart.Font = new Font("Segoe UI", 9);
            
            // Add radio buttons to flow panel
            launchMethodsFlow.Controls.Add(radCreateProcess);
            launchMethodsFlow.Controls.Add(radCmdStart);
            launchMethodsFlow.Controls.Add(radCreateThreadInjection);
            launchMethodsFlow.Controls.Add(radRemoteThreadInjection);
            launchMethodsFlow.Controls.Add(radShellExecute);
            launchMethodsFlow.Controls.Add(radProcessStart);
            
            // Create a container to center the flow panel
            Panel centerContainer = new Panel();
            centerContainer.Dock = DockStyle.Fill;
            centerContainer.BackColor = Color.FromArgb(30, 30, 30);
            
            // Add the flow panel to the center container with centering
            launchMethodsFlow.Dock = DockStyle.None;
            launchMethodsFlow.Location = new Point(
                (centerContainer.ClientSize.Width - launchMethodsFlow.Width) / 2,
                (centerContainer.ClientSize.Height - launchMethodsFlow.Height) / 2);
            
            centerContainer.SizeChanged += (s, e) => {
                launchMethodsFlow.Location = new Point(
                    (centerContainer.ClientSize.Width - launchMethodsFlow.Width) / 2,
                    (centerContainer.ClientSize.Height - launchMethodsFlow.Height) / 2);
            };
            
            centerContainer.Controls.Add(launchMethodsFlow);
            
            // Add center container to launch methods panel
            panelLaunchMethods.Controls.Add(centerContainer);
            panelLaunchMethods.Controls.Add(methodsHeader);
            
            // Create a container for the Launch button and methods panel
            launchContainer = new TableLayoutPanel();
            launchContainer.Dock = DockStyle.Fill;
            launchContainer.RowCount = 2;
            launchContainer.ColumnCount = 1;
            launchContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            launchContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            
            launchContainer.Controls.Add(btnLaunch, 0, 0);
            launchContainer.Controls.Add(panelLaunchMethods, 0, 1);
            
            // Add all panels to the main layout
            mainLayout.Controls.Add(panelPathsProcessSelection, 0, 0);
            mainLayout.Controls.Add(panelProfiles, 1, 0);
            mainLayout.Controls.Add(consolePanel, 0, 1);
            mainLayout.Controls.Add(loadedDllsPanel, 1, 1);
            mainLayout.Controls.Add(launchContainer, 0, 2);
            mainLayout.SetColumnSpan(launchContainer, 2);
            
            // Add the main layout to the tab
            tabMain.Controls.Add(mainLayout);
        }
        
        private void RegisterEventHandlers()
        {
            // Form events
            FormClosing += (s, e) => OnFormClosing(e);
            KeyDown += OnFormKeyDown;
            
            // Process Manager events
            _processManager.ProcessStarted += ProcessManager_ProcessStarted;
            _processManager.ProcessStopped += ProcessManager_ProcessStopped;
            _processManager.ProcessAttached += ProcessManager_ProcessAttached;
            _processManager.ProcessDetached += ProcessManager_ProcessDetached;
            
            // Injection Manager events
            _injectionManager.InjectionSucceeded += InjectionManager_InjectionSucceeded;
            _injectionManager.InjectionFailed += InjectionManager_InjectionFailed;
            
            // Memory Manager events
            _memoryManager.MemoryOperationSucceeded += MemoryManager_MemoryOperationSucceeded;
            _memoryManager.MemoryOperationFailed += MemoryManager_MemoryOperationFailed;
            
            // Resume Panel events
            if (_resumePanel != null)
            {
                _resumePanel.Resumed += ResumePanel_Resumed;
            }
            
            // Button click events
            btnRefreshProcesses.Click += (s, e) => RefreshProcessList();
            btnBrowseGame.Click += BtnBrowseGame_Click;
            btnBrowseTrainer.Click += BtnBrowseTrainer_Click;
            btnBrowseDll1.Click += BtnBrowseDll1_Click;
            btnBrowseDll2.Click += BtnBrowseDll2_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnLoad.Click += BtnLoad_Click;
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnLaunch.Click += BtnLaunch_Click;
            
            // ComboBox events
            cmbRunningExe.SelectedIndexChanged += CmbRunningExe_SelectedIndexChanged;
            
            // Radio button events
            radCreateProcess.CheckedChanged += LaunchMethod_CheckedChanged;
            radCmdStart.CheckedChanged += LaunchMethod_CheckedChanged;
            radCreateThreadInjection.CheckedChanged += LaunchMethod_CheckedChanged;
            radRemoteThreadInjection.CheckedChanged += LaunchMethod_CheckedChanged;
            radShellExecute.CheckedChanged += LaunchMethod_CheckedChanged;
            radProcessStart.CheckedChanged += LaunchMethod_CheckedChanged;
        }
        
        private void PopulateControls()
        {
            // Add status strip
            statusStrip.Items.Add(statusLabel);
            Controls.Add(statusStrip);
            
            // Load recent files
            LoadRecentFiles();
            
            // Load profiles
            LoadProfiles();
            
            // Refresh process list
            RefreshProcessList();
            
            // Show current environment DLLs by default
            ShowCurrentEnvironmentModules();
        }
        
        private void RefreshProcessList()
        {
            try
            {
                // Clear the list
                cmbRunningExe.Items.Clear();
                
                // Add an empty item
                cmbRunningExe.Items.Add(string.Empty);
                
                // Get all running processes
                var processes = Process.GetProcesses();
                
                // Sort by name
                Array.Sort(processes, (p1, p2) => p1.ProcessName.CompareTo(p2.ProcessName));
                
                // Add each process to the list
                foreach (var process in processes)
                {
                    try
                    {
                        // Skip system processes that throw errors when accessed
                        if (process.Id <= 4)
                        {
                            continue;
                        }
                        
                        // Skip this process
                        if (process.Id == Process.GetCurrentProcess().Id)
                        {
                            continue;
                        }
                        
                        // Add the process to the list
                        cmbRunningExe.Items.Add($"{process.ProcessName} (ID: {process.Id})");
                    }
                    catch
                    {
                        // Some processes may not be accessible due to security restrictions
                        continue;
                    }
                }
                
                LogToConsole($"Refreshed process list - {cmbRunningExe.Items.Count - 1} processes found");
            }
            catch (Exception ex)
            {
                LogToConsole($"Error refreshing process list: {ex.Message}");
            }
        }
        
        private void LoadRecentFiles()
        {
            try
            {
                _recentGamePaths.Clear();
                _recentTrainerPaths.Clear();
                _recentDllPaths.Clear();
                
                string settingsPath = Path.Combine(Application.StartupPath, "settings.ini");
                
                if (!File.Exists(settingsPath))
                {
                    return;
                }
                
                string[] lines = File.ReadAllLines(settingsPath);
                string section = string.Empty;
                
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                    {
                        continue;
                    }
                    
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2);
                        continue;
                    }
                    
                    switch (section)
                    {
                        case "RecentGamePaths":
                            if (File.Exists(line))
                            {
                                _recentGamePaths.Add(line);
                                cmbGamePath.Items.Add(line);
                            }
                            break;
                            
                        case "RecentTrainerPaths":
                            if (File.Exists(line))
                            {
                                _recentTrainerPaths.Add(line);
                                cmbTrainerPath.Items.Add(line);
                            }
                            break;
                            
                        case "RecentDllPaths":
                            if (File.Exists(line))
                            {
                                _recentDllPaths.Add(line);
                                cmbDll1.Items.Add(line);
                                cmbDll2.Items.Add(line);
                            }
                            break;
                    }
                }
                
                LogToConsole($"Loaded recent files - Games: {_recentGamePaths.Count}, Trainers: {_recentTrainerPaths.Count}, DLLs: {_recentDllPaths.Count}");
            }
            catch (Exception ex)
            {
                LogToConsole($"Error loading recent files: {ex.Message}");
            }
        }
        
        private void SaveRecentFiles()
        {
            try
            {
                string settingsPath = Path.Combine(Application.StartupPath, "settings.ini");
                
                using (StreamWriter writer = new StreamWriter(settingsPath))
                {
                    writer.WriteLine("[RecentGamePaths]");
                    foreach (string path in _recentGamePaths)
                    {
                        writer.WriteLine(path);
                    }
                    
                    writer.WriteLine();
                    writer.WriteLine("[RecentTrainerPaths]");
                    foreach (string path in _recentTrainerPaths)
                    {
                        writer.WriteLine(path);
                    }
                    
                    writer.WriteLine();
                    writer.WriteLine("[RecentDllPaths]");
                    foreach (string path in _recentDllPaths)
                    {
                        writer.WriteLine(path);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Error saving recent files: {ex.Message}");
            }
        }
        
        private void LoadProfiles()
        {
            try
            {
                cmbProfiles.Items.Clear();
                _profiles.Clear();
                
                string profilesDir = Path.Combine(Application.StartupPath, "Profiles");
                
                if (!Directory.Exists(profilesDir))
                {
                    Directory.CreateDirectory(profilesDir);
                    return;
                }
                
                string[] profileFiles = Directory.GetFiles(profilesDir, "*.profile");
                
                foreach (string profileFile in profileFiles)
                {
                    string profileName = Path.GetFileNameWithoutExtension(profileFile);
                    _profiles.Add(profileName); // Store just the name
                    cmbProfiles.Items.Add(profileName);
                }
                
                UpdateStatus($"Loaded {profileFiles.Length} profiles");
                
                // Select the last used profile if available
                if (!string.IsNullOrEmpty(_lastUsedProfile) && cmbProfiles.Items.Contains(_lastUsedProfile))
                {
                    cmbProfiles.SelectedItem = _lastUsedProfile;
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Error loading profiles: {ex.Message}");
            }
        }
        
        private void SaveProfile(string profileName)
        {
            try
            {
                string profilesDir = Path.Combine(Application.StartupPath, "Profiles");
                
                if (!Directory.Exists(profilesDir))
                {
                    Directory.CreateDirectory(profilesDir);
                }
                
                string profilePath = Path.Combine(profilesDir, $"{profileName}.profile");
                
                using (StreamWriter writer = new StreamWriter(profilePath))
                {
                    // Save selected game path
                    writer.WriteLine($"GamePath={_selectedGamePath}");
                    
                    // Save selected trainer path
                    writer.WriteLine($"TrainerPath={_selectedTrainerPath}");
                    
                    // Save DLL paths
                    writer.WriteLine($"Dll1Path={_selectedDll1Path}");
                    writer.WriteLine($"Dll2Path={_selectedDll2Path}");
                    
                    // Save checkbox states
                    writer.WriteLine($"LaunchInject1={chkLaunchInject1.Checked}");
                    writer.WriteLine($"LaunchInject2={chkLaunchInject2.Checked}");
                    
                    // Save launch method
                    writer.WriteLine($"LaunchMethod={_launchMethod}");
                }
                
                if (!_profiles.Contains(profileName))
                {
                    _profiles.Add(profileName);
                    cmbProfiles.Items.Add(profileName);
                }
                
                LogToConsole($"Profile saved: {profileName}");
                UpdateStatus($"Profile saved: {profileName}");
            }
            catch (Exception ex)
            {
                LogToConsole($"Error saving profile: {ex.Message}");
                MessageBox.Show($"Error saving profile: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadProfile(string profileName)
        {
            try
            {
                string profilesDir = Path.Combine(Application.StartupPath, "Profiles");
                string profilePath = Path.Combine(profilesDir, $"{profileName}.profile");
                
                if (!File.Exists(profilePath))
                {
                    MessageBox.Show($"Profile file not found: {profilePath}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                string[] lines = File.ReadAllLines(profilePath);
                
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    
                    if (parts.Length != 2)
                    {
                        continue;
                    }
                    
                    string key = parts[0];
                    string value = parts[1];
                    
                    switch (key)
                    {
                        case "GamePath":
                            _selectedGamePath = value;
                            SetComboBoxValue(cmbGamePath, value);
                            break;
                            
                        case "TrainerPath":
                            _selectedTrainerPath = value;
                            SetComboBoxValue(cmbTrainerPath, value);
                            break;
                            
                        case "Dll1Path":
                            _selectedDll1Path = value;
                            SetComboBoxValue(cmbDll1, value);
                            break;
                            
                        case "Dll2Path":
                            _selectedDll2Path = value;
                            SetComboBoxValue(cmbDll2, value);
                            break;
                            
                        case "LaunchInject1":
                            chkLaunchInject1.Checked = bool.Parse(value);
                            break;
                            
                        case "LaunchInject2":
                            chkLaunchInject2.Checked = bool.Parse(value);
                            break;
                            
                        case "LaunchMethod":
                            if (Enum.TryParse<LaunchMethod>(value, out LaunchMethod method))
                            {
                                _launchMethod = method;
                                UpdateLaunchMethodRadioButtons();
                            }
                            break;
                    }
                }
                
                LogToConsole($"Profile loaded: {profileName}");
                UpdateStatus($"Profile loaded: {profileName}");
            }
            catch (Exception ex)
            {
                LogToConsole($"Error loading profile: {ex.Message}");
                MessageBox.Show($"Error loading profile: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            
            if (!comboBox.Items.Contains(value))
            {
                comboBox.Items.Add(value);
            }
            
            comboBox.SelectedItem = value;
        }
        
        private void UpdateLaunchMethodRadioButtons()
        {
            // Reset all radio buttons to default appearance
            foreach (Control control in launchMethodsFlow.Controls)
            {
                if (control is RadioButton rb)
                {
                    rb.BackColor = Color.FromArgb(60, 60, 60);
                    rb.ForeColor = Color.White;
                }
            }
            
            // Check the appropriate radio button based on the launch method
            radCreateProcess.Checked = _launchMethod == LaunchMethod.CreateProcess;
            radCmdStart.Checked = _launchMethod == LaunchMethod.CmdStart;
            radCreateThreadInjection.Checked = _launchMethod == LaunchMethod.CreateThreadInjection;
            radRemoteThreadInjection.Checked = _launchMethod == LaunchMethod.RemoteThreadInjection;
            radShellExecute.Checked = _launchMethod == LaunchMethod.ShellExecute;
            radProcessStart.Checked = _launchMethod == LaunchMethod.ProcessStart;
            
            // Highlight the selected radio button
            RadioButton selectedButton = null;
            if (_launchMethod == LaunchMethod.CreateProcess) selectedButton = radCreateProcess;
            else if (_launchMethod == LaunchMethod.CmdStart) selectedButton = radCmdStart;
            else if (_launchMethod == LaunchMethod.CreateThreadInjection) selectedButton = radCreateThreadInjection;
            else if (_launchMethod == LaunchMethod.RemoteThreadInjection) selectedButton = radRemoteThreadInjection;
            else if (_launchMethod == LaunchMethod.ShellExecute) selectedButton = radShellExecute;
            else if (_launchMethod == LaunchMethod.ProcessStart) selectedButton = radProcessStart;
            
            if (selectedButton != null)
            {
                selectedButton.BackColor = Color.FromArgb(0, 120, 215);
                selectedButton.ForeColor = Color.White;
            }
        }
        
        private void UpdateStatus(string message)
        {
            // Update the status strip label
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }
            
            statusLabel.Text = message;
            
            // Also log to console if it's an important message
            if (message.Contains("Error") || message.Contains("Success") || message.Contains("Started") || 
                message.Contains("Injected") || message.Contains("Launched"))
            {
                LogToConsole(message);
            }
        }
        
        private void LogToConsole(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogToConsole), message);
                return;
            }
            
            // Add timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            
            // Add to console
            txtConsoleOutput.AppendText(logEntry + Environment.NewLine);
            
            // Auto-scroll to bottom
            txtConsoleOutput.SelectionStart = txtConsoleOutput.Text.Length;
            txtConsoleOutput.ScrollToCaret();
        }
        
        #region Event Handlers
        
        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            // Handle keyboard shortcuts
            if (e.Control && e.KeyCode == Keys.T)
            {
                // TV mode removed
                e.Handled = true;
            }
        }
        
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            
            // Show click to resume panel when focus is lost
            if (_resumePanel != null)
            {
                _resumePanel.Show();
            }
        }
        
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            
            // Hide click to resume panel when focus is gained
            if (_resumePanel != null)
            {
                _resumePanel.Hide();
            }
        }
        
        #endregion
        
        #region UI Control Event Handlers
        
        private void BtnBrowseGame_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Executable Files (*.exe;*.bat;*.cmd;*.com;*.scr)|*.exe;*.bat;*.cmd;*.com;*.scr|All Files (*.*)|*.*";
                dialog.Title = "Select Game Executable";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedGamePath = dialog.FileName;
                    
                    // Add to combobox and recent paths if not already there
                    if (!cmbGamePath.Items.Contains(_selectedGamePath))
                    {
                        cmbGamePath.Items.Add(_selectedGamePath);
                        _recentGamePaths.Add(_selectedGamePath);
                    }
                    
                    cmbGamePath.SelectedItem = _selectedGamePath;
                    
                    UpdateStatus($"Selected game path: {_selectedGamePath}");
                    LogToConsole($"Selected game: {Path.GetFileName(_selectedGamePath)}");
                    
                    // Save recent files
                    SaveRecentFiles();
                }
            }
        }
        
        private void BtnBrowseTrainer_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Executable Files (*.exe;*.bat;*.cmd;*.com;*.scr)|*.exe;*.bat;*.cmd;*.com;*.scr|All Files (*.*)|*.*";
                dialog.Title = "Select Trainer Executable";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedTrainerPath = dialog.FileName;
                    
                    // Add to combobox and recent paths if not already there
                    if (!cmbTrainerPath.Items.Contains(_selectedTrainerPath))
                    {
                        cmbTrainerPath.Items.Add(_selectedTrainerPath);
                        _recentTrainerPaths.Add(_selectedTrainerPath);
                    }
                    
                    cmbTrainerPath.SelectedItem = _selectedTrainerPath;
                    
                    UpdateStatus($"Selected trainer path: {_selectedTrainerPath}");
                    LogToConsole($"Selected trainer: {Path.GetFileName(_selectedTrainerPath)}");
                    
                    // Save recent files
                    SaveRecentFiles();
                }
            }
        }
        
        private void BtnBrowseDll1_Click(object sender, EventArgs e)
        {
            BrowseForDll(cmbDll1, path => _selectedDll1Path = path);
        }
        
        private void BtnBrowseDll2_Click(object sender, EventArgs e)
        {
            BrowseForDll(cmbDll2, path => _selectedDll2Path = path);
        }
        
        private void BrowseForDll(ComboBox comboBox, Action<string> setPathAction)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*";
                dialog.Title = "Select DLL";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string dllPath = dialog.FileName;
                    
                    // Set the path using the provided action
                    setPathAction(dllPath);
                    
                    // Add to combobox and recent paths if not already there
                    if (!comboBox.Items.Contains(dllPath))
                    {
                        comboBox.Items.Add(dllPath);
                    }
                    
                    if (!_recentDllPaths.Contains(dllPath))
                    {
                        _recentDllPaths.Add(dllPath);
                    }
                    
                    comboBox.SelectedItem = dllPath;
                    
                    UpdateStatus($"Selected DLL path: {dllPath}");
                    LogToConsole($"Selected DLL: {Path.GetFileName(dllPath)}");
                    
                    // Save recent files
                    SaveRecentFiles();
                }
            }
        }
        
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadProfiles();
            LogToConsole("Profiles refreshed");
            UpdateStatus("Profiles refreshed");
        }
        
        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (cmbProfiles.SelectedItem == null)
            {
                MessageBox.Show("Please select a profile to load.", "No Profile Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string profileName = cmbProfiles.SelectedItem.ToString();
            LoadProfile(profileName);
            
            // Save as last used profile
            _lastUsedProfile = profileName;
            SaveAppSettings();
        }
        
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Show dialog to get profile name
            using (var dialog = new ProfileInputDialog("Save Profile"))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string profileName = dialog.ProfileName;
                    
                    // Check if profile already exists
                    if (cmbProfiles.Items.Contains(profileName))
                    {
                        DialogResult result = MessageBox.Show(
                            $"Profile '{profileName}' already exists. Do you want to overwrite it?", 
                            "Profile Exists", 
                            MessageBoxButtons.YesNo, 
                            MessageBoxIcon.Question);
                        
                        if (result != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                    
                    SaveProfile(profileName);
                    
                    // Update last used profile
                    _lastUsedProfile = profileName;
                    SaveAppSettings();
                    
                    // Select the profile in the dropdown
                    cmbProfiles.SelectedItem = profileName;
                }
            }
        }
        
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (cmbProfiles.SelectedItem == null)
            {
                MessageBox.Show("Please select a profile to delete.", "No Profile Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string profileName = cmbProfiles.SelectedItem.ToString();
            
            // Confirm deletion
            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete the profile '{profileName}'?", 
                "Confirm Delete", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes)
            {
                return;
            }
            
            try
            {
                string profilesDir = Path.Combine(Application.StartupPath, "Profiles");
                string profilePath = Path.Combine(profilesDir, $"{profileName}.profile");
                
                if (File.Exists(profilePath))
                {
                    File.Delete(profilePath);
                    
                    // Remove from list
                    _profiles.Remove(profileName);
                    cmbProfiles.Items.Remove(profileName);
                    
                    // If we deleted the last used profile, clear it
                    if (_lastUsedProfile == profileName)
                    {
                        _lastUsedProfile = string.Empty;
                        SaveAppSettings();
                    }
                    
                    LogToConsole($"Profile deleted: {profileName}");
                    UpdateStatus($"Profile deleted: {profileName}");
                }
                else
                {
                    MessageBox.Show($"Profile file not found: {profilePath}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Error deleting profile: {ex.Message}");
                MessageBox.Show($"Error deleting profile: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedGamePath))
                {
                    // Skip the confirmation dialog if this is an auto-launch with just a trainer
                    if (_autoLaunchRequested && !string.IsNullOrEmpty(_selectedTrainerPath))
                    {
                        LogToConsole("Auto-launching trainer without game executable");
                    }
                    else
                    {
                        // Since we've marked the game executable as optional in the UI, 
                        // we should ask the user to confirm they want to proceed without a game path
                        DialogResult result = MessageBox.Show(
                            "No game executable selected. Do you want to continue?\n\n" +
                            "Select 'Yes' to continue without launching a game executable.\n" +
                            "Select 'No' to cancel and select a game executable.",
                            "No Game Executable",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                            
                        if (result == DialogResult.No)
                        {
                            return;
                        }
                    }
                    
                    // Check if we have a trainer to launch
                    if (!string.IsNullOrEmpty(_selectedTrainerPath))
                    {
                        LogToConsole($"Launching trainer without game: {_selectedTrainerPath}");
                        
                        string trainerDir = Path.GetDirectoryName(_selectedTrainerPath);
                        
                        if (_processManager.LaunchProcess(_selectedTrainerPath, trainerDir, _launchMethod))
                        {
                            LogToConsole($"Trainer launched successfully (PID: {_processManager.ProcessId})");
                        }
                        else
                        {
                            LogToConsole("Failed to launch trainer");
                        }
                    }
                    else if (!_autoLaunchRequested) // Only show this message if not auto-launching
                    {
                        // If no game and no trainer, show message
                        MessageBox.Show(
                            "Please select at least a game executable or trainer to launch.", 
                            "Nothing to Launch", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                    }
                    
                    return;
                }
                
                // Launch the game
                LogToConsole($"Launching game: {_selectedGamePath}");
                LogToConsole($"Launch method: {_launchMethod}");
                
                string gameDir = Path.GetDirectoryName(_selectedGamePath);
                
                if (_processManager.LaunchProcess(_selectedGamePath, gameDir, _launchMethod))
                {
                    LogToConsole($"Game launched successfully (PID: {_processManager.ProcessId})");
                    
                    // Check if we have DLLs to inject
                    if (chkLaunchInject1.Checked && !string.IsNullOrEmpty(_selectedDll1Path))
                    {
                        LogToConsole($"Auto-injecting: {_selectedDll1Path}");
                        InjectDll(_selectedDll1Path);
                    }
                    
                    if (chkLaunchInject2.Checked && !string.IsNullOrEmpty(_selectedDll2Path))
                    {
                        LogToConsole($"Auto-injecting: {_selectedDll2Path}");
                        InjectDll(_selectedDll2Path);
                    }
                    
                    // Launch trainer if needed
                    if (!string.IsNullOrEmpty(_selectedTrainerPath))
                    {
                        LogToConsole($"Launching trainer: {_selectedTrainerPath}");
                        
                        string trainerDir = Path.GetDirectoryName(_selectedTrainerPath);
                        
                        if (_processManager.LaunchProcess(_selectedTrainerPath, trainerDir, _launchMethod))
                        {
                            LogToConsole($"Trainer launched successfully (PID: {_processManager.ProcessId})");
                        }
                        else
                        {
                            LogToConsole("Failed to launch trainer");
                        }
                    }
                }
                else
                {
                    LogToConsole("Failed to launch game");
                    
                    // Only show error message if not auto-launching (since window would be minimized)
                    if (!_autoLaunchRequested)
                    {
                        MessageBox.Show("Failed to launch game. Please check the log for details.", 
                            "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Error launching: {ex.Message}");
                
                // Only show error message if not auto-launching (since window would be minimized)
                if (!_autoLaunchRequested)
                {
                    MessageBox.Show($"Error launching: {ex.Message}", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void InjectDll(string dllPath)
        {
            try
            {
                if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                {
                    LogToConsole($"Invalid DLL path: {dllPath}");
                    return;
                }
                
                if (_injectionManager.InjectDll(dllPath))
                {
                    LogToConsole($"Successfully injected: {dllPath}");
                    
                    // Update the loadeddlls list
                    RefreshLoadedDllsList();
                }
                else
                {
                    LogToConsole($"Failed to inject: {dllPath}");
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Error injecting DLL: {ex.Message}");
            }
        }
        
        private void LaunchMethod_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton == null || !radioButton.Checked)
                return;
            
            // Reset all radio buttons to default appearance
            foreach (Control control in launchMethodsFlow.Controls)
            {
                if (control is RadioButton rb)
                {
                    rb.BackColor = Color.FromArgb(60, 60, 60);
                    rb.ForeColor = Color.White;
                }
            }
            
            // Highlight the selected radio button
            radioButton.BackColor = Color.FromArgb(0, 120, 215);
            radioButton.ForeColor = Color.White;
            
            // Update the launch method based on the selected radio button
            if (radioButton == radCreateProcess)
                _launchMethod = LaunchMethod.CreateProcess;
            else if (radioButton == radCmdStart)
                _launchMethod = LaunchMethod.CmdStart;
            else if (radioButton == radCreateThreadInjection)
                _launchMethod = LaunchMethod.CreateThreadInjection;
            else if (radioButton == radRemoteThreadInjection)
                _launchMethod = LaunchMethod.RemoteThreadInjection;
            else if (radioButton == radShellExecute)
                _launchMethod = LaunchMethod.ShellExecute;
            else if (radioButton == radProcessStart)
                _launchMethod = LaunchMethod.ProcessStart;
            
            UpdateStatus($"Launch method selected: {_launchMethod}");
        }
        
        private void CmbRunningExe_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbRunningExe.SelectedItem != null)
            {
                string selectedProcess = cmbRunningExe.SelectedItem.ToString();
                int processId = ExtractProcessId(selectedProcess);
                
                if (processId > 0)
                {
                    UpdateStatus($"Selected process: {selectedProcess}");
                    
                    // Refresh loaded DLLs list to show modules for the selected process
                    RefreshLoadedDllsList();
                }
            }
        }
        
        #endregion
        
        #region Process Manager Event Handlers
        
        private void ProcessManager_ProcessStarted(object sender, ProcessEventArgs e)
        {
            UpdateStatus($"Process started: {e.Process.ProcessName} (PID: {e.Process.Id})");
            LogToConsole($"Process started: {e.Process.ProcessName} (PID: {e.Process.Id})");
            RefreshLoadedDllsList();
        }
        
        private void ProcessManager_ProcessStopped(object sender, ProcessEventArgs e)
        {
            UpdateStatus($"Process stopped: {e.Process.ProcessName}");
            LogToConsole($"Process stopped: {e.Process.ProcessName}");
            RefreshLoadedDllsList();
        }
        
        private void ProcessManager_ProcessAttached(object sender, ProcessEventArgs e)
        {
            UpdateStatus($"Attached to process: {e.Process.ProcessName} (PID: {e.Process.Id})");
            LogToConsole($"Attached to process: {e.Process.ProcessName} (PID: {e.Process.Id})");
            RefreshLoadedDllsList();
        }
        
        private void ProcessManager_ProcessDetached(object sender, ProcessEventArgs e)
        {
            UpdateStatus($"Detached from process: {e.Process.ProcessName}");
            LogToConsole($"Detached from process: {e.Process.ProcessName}");
            RefreshLoadedDllsList();
        }
        
        #endregion
        
        #region Injection Manager Event Handlers
        
        private void InjectionManager_InjectionSucceeded(object sender, InjectionEventArgs e)
        {
            UpdateStatus($"Injection succeeded: {e.DllPath}");
            LogToConsole($"Injection succeeded: {e.DllPath}");
            RefreshLoadedDllsList();
        }
        
        private void InjectionManager_InjectionFailed(object sender, InjectionEventArgs e)
        {
            UpdateStatus($"Injection failed: {e.DllPath} - {e.Message}");
            LogToConsole($"Injection failed: {e.DllPath} - {e.Message}");
        }
        
        #endregion
        
        #region Memory Manager Event Handlers
        
        private void MemoryManager_MemoryOperationSucceeded(object sender, MemoryEventArgs e)
        {
            UpdateStatus(e.Message);
            LogToConsole(e.Message);
        }
        
        private void MemoryManager_MemoryOperationFailed(object sender, MemoryEventArgs e)
        {
            UpdateStatus($"Memory operation failed: {e.Message}");
            LogToConsole($"Memory operation failed: {e.Message}");
        }
        
        #endregion
        
        #region UI Component Event Handlers
        
        private void ResumePanel_Resumed(object sender, EventArgs e)
        {
            // Handle resume
            UpdateStatus("Application resumed");
        }
        
        #endregion
        
        #region Helper Methods
        
        private void RefreshLoadedDllsList()
        {
            try
            {
                // Clear the list
                lstLoadedDlls.Items.Clear();
                
                // Get selected process if any
                string selectedProcess = cmbRunningExe.SelectedItem?.ToString();
                
                if (!string.IsNullOrEmpty(selectedProcess))
                {
                    // If process is selected, extract process ID and show its modules
                    int processId = ExtractProcessId(selectedProcess);
                    
                    if (processId > 0)
                    {
                        try
                        {
                            Process proc = Process.GetProcessById(processId);
                            UpdateStatus($"Showing modules for process: {proc.ProcessName} (PID: {proc.Id})");
                            
                            foreach (ProcessModule module in proc.Modules)
                            {
                                lstLoadedDlls.Items.Add(module.FileName);
                            }
                            
                            LogToConsole($"Refreshed loaded modules list - {proc.Modules.Count} modules found for {proc.ProcessName}");
                        }
                        catch (Exception ex)
                        {
                            LogToConsole($"Error getting modules for selected process: {ex.Message}");
                            
                            // Fall back to current process modules
                            ShowCurrentEnvironmentModules();
                        }
                    }
                    else
                    {
                        // Fall back to current process modules
                        ShowCurrentEnvironmentModules();
                    }
                }
                else
                {
                    // No process selected, show current environment modules
                    ShowCurrentEnvironmentModules();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error refreshing loaded DLLs: {ex.Message}");
                LogToConsole($"Error refreshing loaded DLLs: {ex.Message}");
            }
        }
        
        private void ShowCurrentEnvironmentModules()
        {
            try
            {
                // Show modules for the current process (our application)
                Process currentProc = Process.GetCurrentProcess();
                UpdateStatus("Showing modules for current Windows environment");
                
                foreach (ProcessModule module in currentProc.Modules)
                {
                    lstLoadedDlls.Items.Add(module.FileName);
                }
                
                LogToConsole($"Refreshed loaded modules list - {currentProc.Modules.Count} modules found for current environment");
            }
            catch (Exception ex)
            {
                LogToConsole($"Error getting modules for current environment: {ex.Message}");
            }
        }
        
        private int ExtractProcessId(string processText)
        {
            try
            {
                // Format: "ProcessName (ID: 1234)"
                int startIndex = processText.IndexOf("(ID: ");
                if (startIndex >= 0)
                {
                    startIndex += 5; // Move past "(ID: "
                    int endIndex = processText.IndexOf(')', startIndex);
                    if (endIndex >= 0)
                    {
                        string pidString = processText.Substring(startIndex, endIndex - startIndex);
                        if (int.TryParse(pidString, out int pid))
                        {
                            return pid;
                        }
                    }
                }
                
                return -1;
            }
            catch
            {
                return -1;
            }
        }
        
        // Helper method to create improved field panels with better styling
        private TableLayoutPanel CreateImprovedFieldPanel(string labelText, Control inputControl, Control buttonControl)
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(5);
            panel.Margin = new Padding(0);
            panel.ColumnCount = 3;
            panel.RowCount = 1;
            panel.Height = 40;
            panel.AutoSize = false;
            
            // Column styles - exact same values for both panels
            panel.ColumnStyles.Clear();
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F)); // Label
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Input
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F)); // Button
            
            // Create label
            Label label = new Label();
            label.Text = labelText;
            label.ForeColor = Color.White;
            label.Font = new Font("Segoe UI", 9);
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Padding = new Padding(5, 0, 0, 0);
            
            // Configure input control
            inputControl.Dock = DockStyle.Fill;
            inputControl.Margin = new Padding(0, 3, 5, 3);
            
            // Configure button - check that it's a Button type first
            if (buttonControl is Button btn)
            {
                btn.Size = new Size(90, 29);
                btn.Dock = DockStyle.Fill;
                btn.AutoSize = false;
                btn.Margin = new Padding(5, 3, 5, 3);
                btn.Text = "Browse...";
                btn.BackColor = Color.FromArgb(60, 60, 60);
                btn.ForeColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Font = new Font("Segoe UI", 9);
            }
            else
            {
                buttonControl.Size = new Size(90, 29);
                buttonControl.Dock = DockStyle.Fill;
                buttonControl.AutoSize = false;
                buttonControl.Margin = new Padding(5, 3, 5, 3);
            }
            
            // Add controls
            panel.Controls.Add(label, 0, 0);
            panel.Controls.Add(inputControl, 1, 0);
            panel.Controls.Add(buttonControl, 2, 0);
            
            return panel;
        }
        
        // Helper method to create improved DLL injection panels with better styling
        private TableLayoutPanel CreateImprovedDllPanel(CheckBox checkbox, string labelText, ComboBox comboBox, Button button)
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.ColumnCount = 4;
            panel.RowCount = 1;
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 2, 0, 2);
            
            // Configure columns
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));  // Checkbox
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));  // Label
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));  // ComboBox
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));  // Button
            
            // Configure checkbox
            checkbox.Text = "Launch/Inject";
            checkbox.AutoSize = true;
            checkbox.Anchor = AnchorStyles.Left;
            checkbox.Dock = DockStyle.Fill;
            checkbox.ForeColor = Color.White;
            panel.Controls.Add(checkbox, 0, 0);
            
            // Create label
            Label label = new Label();
            label.Text = labelText;
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Left;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.White;
            label.Dock = DockStyle.Fill;
            panel.Controls.Add(label, 1, 0);
            
            // Configure combobox
            comboBox.Dock = DockStyle.Fill;
            panel.Controls.Add(comboBox, 2, 0);
            
            // Configure button
            button.Text = "Browse...";
            button.Dock = DockStyle.Fill;
            panel.Controls.Add(button, 3, 0);
            
            return panel;
        }
        
        #endregion
        
        private void ProcessCommandLineArguments()
        {
            if (_args == null || _args.Length == 0)
                return;
                
            LogToConsole("Processing command line arguments: " + string.Join(" ", _args));
            
            // Parse arguments
            for (int i = 0; i < _args.Length; i++)
            {
                string arg = _args[i].ToLower();
                
                // Profile parameter
                if (arg == "-p" && i + 1 < _args.Length)
                {
                    _profileToLoad = _args[++i].Trim('"');
                    LogToConsole($"Profile requested: {_profileToLoad}");
                    
                    // Load the profile
                    if (!string.IsNullOrEmpty(_profileToLoad))
                    {
                        // First, ensure profiles are loaded
                        LoadProfiles();
                        
                        if (cmbProfiles.Items.Contains(_profileToLoad))
                        {
                            // Select the profile in the combo box
                            cmbProfiles.SelectedItem = _profileToLoad;
                            
                            // Load the profile
                            LoadProfile(_profileToLoad);
                            
                            // Save as last used profile
                            _lastUsedProfile = _profileToLoad;
                            SaveAppSettings();
                            
                            LogToConsole($"Profile '{_profileToLoad}' loaded");
                        }
                        else
                        {
                            LogToConsole($"Error: Profile '{_profileToLoad}' not found");
                        }
                    }
                }
                // Auto-launch parameter - everything after -autolaunch is considered part of the path
                else if (arg == "-autolaunch" && i + 1 < _args.Length)
                {
                    // Collect all remaining arguments as part of the command
                    List<string> commandParts = new List<string>();
                    for (int j = i + 1; j < _args.Length; j++)
                    {
                        commandParts.Add(_args[j]);
                    }
                    
                    // Join all parts into a single path/command
                    _autoLaunchPath = string.Join(" ", commandParts);
                    // Remove any surrounding quotes
                    _autoLaunchPath = _autoLaunchPath.Trim('"', '\'');
                    
                    _autoLaunchRequested = true;
                    LogToConsole($"Auto-launch requested for: {_autoLaunchPath}");
                    
                    // Set the game path
                    _selectedGamePath = _autoLaunchPath;
                    SetComboBoxValue(cmbGamePath, _selectedGamePath);
                    LogToConsole($"Game path set to: {_selectedGamePath}");
                    
                    // Break the loop as we've processed all remaining arguments
                    break;
                }
            }
            
            // If auto-launch was requested, perform the launch
            if (_autoLaunchRequested)
            {
                LogToConsole("Auto-launching game...");
                
                // Make sure we have a valid path to auto-launch or a trainer set in profile
                if (!string.IsNullOrEmpty(_autoLaunchPath) || !string.IsNullOrEmpty(_selectedTrainerPath))
                {
                    // Delay launch slightly to allow UI to initialize properly
                    System.Timers.Timer launchTimer = new System.Timers.Timer(1000);
                    launchTimer.Elapsed += (s, e) => {
                        launchTimer.Stop();
                        this.BeginInvoke(new Action(() => {
                            LogToConsole("Executing launch command...");
                            BtnLaunch_Click(this, EventArgs.Empty);
                            
                            // Minimize the window when auto-launching
                            this.WindowState = FormWindowState.Minimized;
                        }));
                    };
                    launchTimer.Start();
                }
                else
                {
                    LogToConsole("Error: No valid path for auto-launch and no trainer specified in profile");
                }
            }
        }
        
        // Methods to save and load application settings
        private void SaveAppSettings()
        {
            try
            {
                string settingsDir = Path.Combine(Application.StartupPath, "Settings");
                
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }
                
                string settingsPath = Path.Combine(settingsDir, "AppSettings.ini");
                
                using (StreamWriter writer = new StreamWriter(settingsPath))
                {
                    // Save auto-load preference
                    writer.WriteLine($"AutoLoadLastProfile={_autoLoadLastProfile}");
                    
                    // Save last used profile
                    writer.WriteLine($"LastUsedProfile={_lastUsedProfile}");
                }
                
                LogToConsole("Application settings saved");
            }
            catch (Exception ex)
            {
                LogToConsole($"Error saving app settings: {ex.Message}");
            }
        }
        
        private void LoadAppSettings()
        {
            try
            {
                string settingsDir = Path.Combine(Application.StartupPath, "Settings");
                string settingsPath = Path.Combine(settingsDir, "AppSettings.ini");
                
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }
                
                if (!File.Exists(settingsPath))
                {
                    // Default settings
                    _autoLoadLastProfile = false;
                    _lastUsedProfile = string.Empty;
                    return;
                }
                
                string[] lines = File.ReadAllLines(settingsPath);
                
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    
                    if (parts.Length != 2)
                    {
                        continue;
                    }
                    
                    string key = parts[0];
                    string value = parts[1];
                    
                    switch (key)
                    {
                        case "AutoLoadLastProfile":
                            _autoLoadLastProfile = bool.Parse(value);
                            if (chkAutoLoadLastProfile != null)
                                chkAutoLoadLastProfile.Checked = _autoLoadLastProfile;
                            break;
                            
                        case "LastUsedProfile":
                            _lastUsedProfile = value;
                            break;
                    }
                }
                
                LogToConsole("Application settings loaded");
            }
            catch (Exception ex)
            {
                LogToConsole($"Error loading app settings: {ex.Message}");
            }
        }
        
        private void ChkAutoLoadLastProfile_CheckedChanged(object sender, EventArgs e)
        {
            _autoLoadLastProfile = chkAutoLoadLastProfile.Checked;
            SaveAppSettings();
            
            if (_autoLoadLastProfile)
                LogToConsole("Auto-load last profile enabled");
            else
                LogToConsole("Auto-load last profile disabled");
        }
    }
} 