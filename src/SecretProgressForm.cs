using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChooChooApp
{
    /// <summary>
    /// A simple modal progress popup that displays status messages while secret mode is processed.
    /// </summary>
    public class SecretProgressForm : Form
    {
        private TextBox txtProgress;
        public SecretProgressForm()
        {
            this.Text = "Secret Mode Progress";
            this.Size = new Size(500, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            txtProgress = new TextBox()
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(txtProgress);
        }
        public void AppendMessage(string message)
        {
            txtProgress.AppendText(message.ToUpper() + Environment.NewLine);
            txtProgress.Refresh();
            Application.DoEvents();
        }
    }
}
