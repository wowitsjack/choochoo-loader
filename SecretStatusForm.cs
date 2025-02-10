using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChooChooApp
{
    /// 
    /// A modal form displaying the secret disclaimer.
    /// It shows two buttons: Agree (to continue to secret mode) and Disagree (to cancel).
    /// 
    public class SecretStatusForm : Form
    {
        private Label lblHeader = null!;
        private TextBox txtOutput = null!;
        private Button btnAgree = null!;
        private Button btnDisagree = null!;

        public SecretStatusForm()
        {
            this.Text = "Secret Feature Notice";
            this.Size = new Size(600, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            lblHeader = new Label
            {
                Text = "Warning: This tool is provided solely to unbind games you have purchased from requiring Steam to run.\n\n" +
                       "It must be used responsibly and legally. By proceeding, you agree to use this tool legally and release the creator from any liability.",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 120
            };
            this.Controls.Add(lblHeader);

            txtOutput = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10),
                Dock = DockStyle.Fill,
                Text = "Secret mode progress will be displayed once you agree."
            };
            this.Controls.Add(txtOutput);

            Panel pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            btnAgree = new Button
            {
                Text = "Agree",
                Location = new Point(400, 10),
                AutoSize = true,
                DialogResult = DialogResult.OK
            };
            pnlButtons.Controls.Add(btnAgree);

            btnDisagree = new Button
            {
                Text = "Disagree",
                Location = new Point(500, 10),
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            pnlButtons.Controls.Add(btnDisagree);

            this.Controls.Add(pnlButtons);
        }
    }
}
