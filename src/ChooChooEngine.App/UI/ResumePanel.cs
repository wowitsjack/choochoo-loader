using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChooChooEngine.App.UI
{
    public class ResumePanel : Panel
    {
        private bool _isVisible = false;
        private readonly Font _textFont;
        private readonly Font _subtextFont;
        private readonly Brush _textBrush;
        private readonly Brush _backgroundBrush;
        private readonly StringFormat _stringFormat;
        
        private const string DEFAULT_TEXT = "CLICK TO RESUME";
        private const string DEFAULT_SUBTEXT = "Application paused";
        
        public new string Text { get; set; } = DEFAULT_TEXT;
        public string SubText { get; set; } = DEFAULT_SUBTEXT;
        
        public event EventHandler Resumed;
        
        public ResumePanel()
        {
            // Initialize properties
            Dock = DockStyle.Fill;
            Visible = false;
            
            // Create fonts and brushes
            _textFont = new Font("Arial", 36, FontStyle.Bold);
            _subtextFont = new Font("Arial", 14, FontStyle.Regular);
            _textBrush = new SolidBrush(Color.White);
            _backgroundBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            
            // Create string format for centered text
            _stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            // Set up event handlers
            Click += OnPanelClick;
            DoubleClick += OnPanelClick;
        }
        
        public new void Show()
        {
            if (!_isVisible)
            {
                _isVisible = true;
                Visible = true;
                BringToFront();
                Invalidate();
            }
        }
        
        public new void Hide()
        {
            if (_isVisible)
            {
                _isVisible = false;
                Visible = false;
            }
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            
            // Draw semi-transparent background
            g.FillRectangle(_backgroundBrush, ClientRectangle);
            
            // Calculate text rectangles
            Rectangle textRect = new Rectangle(0, ClientRectangle.Height / 2 - 30, ClientRectangle.Width, 60);
            Rectangle subtextRect = new Rectangle(0, ClientRectangle.Height / 2 + 30, ClientRectangle.Width, 30);
            
            // Draw main text
            g.DrawString(Text, _textFont, _textBrush, textRect, _stringFormat);
            
            // Draw subtext
            g.DrawString(SubText, _subtextFont, _textBrush, subtextRect, _stringFormat);
        }
        
        private void OnPanelClick(object sender, EventArgs e)
        {
            Hide();
            Resumed?.Invoke(this, EventArgs.Empty);
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _textFont?.Dispose();
                _subtextFont?.Dispose();
                _textBrush?.Dispose();
                _backgroundBrush?.Dispose();
                _stringFormat?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
} 