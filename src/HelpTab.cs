using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChooChooApp
{
    /// <summary>
    /// A UserControl that implements the Help page.
    /// It provides an overview of the application (without revealing any secret keyword).
    /// However, typing the magic password anywhere on this tab will trigger the secret :D.
    /// </summary>
    public class HelpTab : UserControl
    {
        private string inputBuffer = "";

        public HelpTab()
        {
            InitializeComponent();
            this.KeyDown += HelpTab_KeyDown;
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            RichTextBox rtb = new RichTextBox()
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None
            };

            rtb.Text =
@"=======================================
         ChooChoo Injection Engine
               Help Guide

Overview
--------
ChooChoo Injection Engine is a neato little thinger that automates the process of launching your game and trainer in WINE/Proton gaming setups,
while offering advanced features such as DLL injection (validation check) and profile management.
You can navigate the application using either your keyboard or a controller.

Key Features
------------
• Launch your game and trainer with a single click.
• Validate DLLs for injection – when you tick 'Inject', the application checks if the DLL is valid.
  (Untick to 'unload' the DLL.)
• Save and load profiles to retain your settings.
• Use 'Save Settings & Enable Autolaunch' to preserve your configuration.
• Enjoy full controller navigation:
    - DPad for directional movement.
    - A button to select/confirm.
    - B button works naturally.
    - LB/RB to cycle focus among groups.
    - LT/RT to switch between tabs.
• Teehee there's a secret hidden feature if you're an explorer. :D

Usage Tips
----------
• Ensure your game and trainer file paths are set correctly.
• Use the Browse buttons to quickly locate your files.
• The status log provides real-time feedback.
• You can export the list of loaded DLLs if needed.

Enjoy using ChooChoo Injection Engine!";
            rtb.KeyDown += Rtb_KeyDown;
            rtb.TabStop = true;
            this.Controls.Add(rtb);
        }

        private void Rtb_KeyDown(object? sender, KeyEventArgs e)
        {
            HelpTab_KeyDown(sender, e);
        }

        private void HelpTab_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char letter = (char)('A' + (e.KeyCode - Keys.A));
                inputBuffer += letter;
                if (inputBuffer.EndsWith("NOSTEAM", StringComparison.OrdinalIgnoreCase))
                {
                    if (this.FindForm() is MainForm mainForm)
                    {
                        mainForm.ShowSecretStatus();
                    }
                    inputBuffer = "";
                    e.SuppressKeyPress = true;
                }
            }
            else
            {
                inputBuffer = "";
            }
        }
    }
}
