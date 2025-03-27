using System;
using System.Threading;
using System.Timers;
using System.Diagnostics.CodeAnalysis;

namespace ChooChooEngine.App.UI
{
    public class ControllerInput : IDisposable
    {
        private System.Timers.Timer _pollTimer;
        private bool _isDisposed = false;
        
        // Button press states
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonAPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonBPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonXPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonYPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonStartPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonBackPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonUpPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonDownPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonLeftPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonRightPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonShoulderLeftPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonShoulderRightPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonThumbLeftPressed = false;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private bool _buttonThumbRightPressed = false;
        
        // Analog states
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private float _leftThumbX = 0f;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private float _leftThumbY = 0f;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private float _rightThumbX = 0f;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private float _rightThumbY = 0f;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private float _leftTrigger = 0f;
        [SuppressMessage("CodeQuality", "CS0414:Field is assigned but never used")]
        private float _rightTrigger = 0f;

        public event EventHandler<ControllerButtonEventArgs> ButtonPressed;
        public event EventHandler<ControllerButtonEventArgs> ButtonReleased;
        public event EventHandler<ControllerThumbStickEventArgs> ThumbStickMoved;
        public event EventHandler<ControllerTriggerEventArgs> TriggerMoved;

        // Stub for controller connection status (always false in this mock version)
        public bool IsConnected => false;
        
        public ControllerInput()
        {
            // Simulate controller input polling timer
            _pollTimer = new System.Timers.Timer(16); // ~60Hz polling rate
            _pollTimer.Elapsed += OnPollTimerElapsed;
        }

        public void Start()
        {
            _pollTimer.Start();
        }

        public void Stop()
        {
            _pollTimer.Stop();
        }

        public void Vibrate(float leftMotor, float rightMotor)
        {
            // Mock implementation - does nothing
        }

        private void OnPollTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Mock implementation - does nothing but keep this for future implementation
        }

        #region Event Methods

        protected virtual void OnButtonPressed(ControllerButtonEventArgs e)
        {
            ButtonPressed?.Invoke(this, e);
        }

        protected virtual void OnButtonReleased(ControllerButtonEventArgs e)
        {
            ButtonReleased?.Invoke(this, e);
        }

        protected virtual void OnThumbStickMoved(ControllerThumbStickEventArgs e)
        {
            ThumbStickMoved?.Invoke(this, e);
        }

        protected virtual void OnTriggerMoved(ControllerTriggerEventArgs e)
        {
            TriggerMoved?.Invoke(this, e);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _pollTimer?.Stop();
                    _pollTimer?.Dispose();
                }

                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Helper Classes

    public static class MathHelper
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    #endregion

    #region Event Arguments

    public enum ControllerButton
    {
        A,
        B,
        X,
        Y,
        Start,
        Back,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
        LeftShoulder,
        RightShoulder,
        LeftThumb,
        RightThumb
    }

    public enum ThumbStick
    {
        Left,
        Right
    }

    public enum Trigger
    {
        Left,
        Right
    }

    public class ControllerButtonEventArgs : EventArgs
    {
        public ControllerButton Button { get; }

        public ControllerButtonEventArgs(ControllerButton button)
        {
            Button = button;
        }
    }

    public class ControllerThumbStickEventArgs : EventArgs
    {
        public ThumbStick ThumbStick { get; }
        public float X { get; }
        public float Y { get; }

        public ControllerThumbStickEventArgs(ThumbStick thumbStick, float x, float y)
        {
            ThumbStick = thumbStick;
            X = x;
            Y = y;
        }
    }

    public class ControllerTriggerEventArgs : EventArgs
    {
        public Trigger Trigger { get; }
        public float Value { get; }

        public ControllerTriggerEventArgs(Trigger trigger, float value)
        {
            Trigger = trigger;
            Value = value;
        }
    }

    #endregion
} 