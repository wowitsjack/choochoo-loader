using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChooChooApp
{
    // A simple help tab.
    public class HelpTab : UserControl
    {
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
                BackColor = Color.FromArgb(30,30,30),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 11),
                BorderStyle = BorderStyle.None
            };
            rtb.Text =
@"=======================================
         CHOOCHOO INJECTION ENGINE
               HELP GUIDE

Overview
--------
Launch your game/trainer and use our advanced tools for process
manipulation. Features include:
 • Fullscreen mode for uninterrupted XInput.
 • Smart detection of running game processes.
 • DLL injection into the target process.
 • State saving and restoring.
 • A full suite of tools for freeze, dump, list imports/runtimes, etc.

That's it.
";
            rtb.KeyDown += HelpTab_KeyDown;
            rtb.TabStop = true;
            this.Controls.Add(rtb);
        }
        private void HelpTab_KeyDown(object sender, KeyEventArgs e)
        {
            // No special key handling.
        }
    }
}
