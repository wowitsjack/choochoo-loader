using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
// Alias for System.Windows.Forms.Timer.
using WinFormsTimer = System.Windows.Forms.Timer;

namespace ChooChooApp
{
    /// <summary>
    /// A custom modal form for entering the Konami code.
    /// The expected sequence is: U, R, D, L.
    /// Input is accepted via both keyboard and controller.
    /// The window has no title and forces focus so that its input is captured exclusively.
    /// </summary>
    public class MagicInputForm : Form
    {
        private FlowLayoutPanel panel;
        private List<Label> labelSequence;
        private readonly string[] expectedSequence = new string[] { "U", "R", "D", "L" };
        private int currentIndex = 0;
        private WinFormsTimer dummyTimer;
        private WinFormsTimer controllerTimer;
        private XINPUT_STATE prevControllerState;

        public MagicInputForm()
        {
            // Remove window title.
            this.Text = "";
            this.ClientSize = new Size(600, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.KeyPreview = true;
            this.Shown += (s, e) => { CenterPanel(); this.Activate(); this.Focus(); };

            panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };
            // Center the panel manually.
            panel.Anchor = AnchorStyles.None;
            this.Controls.Add(panel);

            labelSequence = new List<Label>();
            for (int i = 0; i < expectedSequence.Length; i++)
            {
                Label lbl = new Label
                {
                    Text = "♦",
                    Font = new Font("Segoe UI", 24, FontStyle.Bold),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Margin = new Padding(10)
                };
                labelSequence.Add(lbl);
                panel.Controls.Add(lbl);
            }

            this.KeyDown += MagicInputForm_KeyDown;
            dummyTimer = new WinFormsTimer { Interval = 50 };
            dummyTimer.Start();

            // Controller timer to poll XInput.
            controllerTimer = new WinFormsTimer { Interval = 50 };
            controllerTimer.Tick += ControllerTimer_Tick;
            controllerTimer.Start();
        }

        private void CenterPanel()
        {
            panel.Location = new Point((this.ClientSize.Width - panel.Width) / 2, (this.ClientSize.Height - panel.Height) / 2);
        }

        private void MagicInputForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Always suppress the key so that input remains in this form.
            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.Up)
            {
                ProcessInput("U");
            }
            else if (e.KeyCode == Keys.Right)
            {
                ProcessInput("R");
            }
            else if (e.KeyCode == Keys.Down)
            {
                ProcessInput("D");
            }
            else if (e.KeyCode == Keys.Left)
            {
                ProcessInput("L");
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        // Poll controller state for directional input.
        private void ControllerTimer_Tick(object? sender, EventArgs e)
        {
            XINPUT_STATE state;
            uint result = XInput.XInputGetState(0, out state);
            if (result == 0)
            {
                if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_UP) != 0 &&
                    (prevControllerState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_UP) == 0)
                {
                    ProcessInput("U");
                }
                if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_RIGHT) != 0 &&
                    (prevControllerState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_RIGHT) == 0)
                {
                    ProcessInput("R");
                }
                if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_DOWN) != 0 &&
                    (prevControllerState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_DOWN) == 0)
                {
                    ProcessInput("D");
                }
                if ((state.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_LEFT) != 0 &&
                    (prevControllerState.Gamepad.wButtons & XInputConstants.XINPUT_GAMEPAD_DPAD_LEFT) == 0)
                {
                    ProcessInput("L");
                }
                prevControllerState = state;
            }
        }

        private void ProcessInput(string input)
        {
            // Guard: if already complete, do nothing.
            if (currentIndex >= expectedSequence.Length)
                return;
            if (string.Equals(input.ToUpper(), expectedSequence[currentIndex].ToUpper()))
            {
                labelSequence[currentIndex].Text = expectedSequence[currentIndex];
                labelSequence[currentIndex].ForeColor = Color.Black;
                currentIndex++;
                if (currentIndex >= expectedSequence.Length)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                CenterPanel(); // re-center after label size changes
            }
        }
    }
}
