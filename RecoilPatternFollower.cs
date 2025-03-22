using System;
using System.Drawing;
using System.Timers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace poji
{
    /// <summary>
    /// Manages real-time playback of recoil patterns to assist the user
    /// </summary>
    public class RecoilPatternFollower : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private readonly RecoilPattern _pattern;
        private readonly System.Timers.Timer _timer;
        private int _currentPointIndex = 0;
        private bool _isPlaying = false;
        private DateTime _playbackStartTime;
        private Point _startPosition;
        private RecoilPoint _lastPoint = null;
        private readonly object _lockObject = new object();
        private bool _isDisposed = false;
        private bool _autoCorrectMouse = false;
        private float _scaleFactor = 1.0f;
        private int _interpolationPoints = 0;
        private int _delayBeforeStart = 0;
        private bool _isDelayedStart = false;
        private System.Timers.Timer _delayTimer = null;

        public event EventHandler<RecoilPointEventArgs> PositionUpdated;
        public event EventHandler PlaybackFinished;
        public event EventHandler PlaybackStarted;
        public event EventHandler<RecoilPointEventArgs> PointReached;
        public event EventHandler PlaybackError;

        /// <summary>
        /// Gets or sets whether mouse position should be automatically adjusted during playback
        /// </summary>
        public bool AutoCorrectMouse
        {
            get { return _autoCorrectMouse; }
            set { _autoCorrectMouse = value; }
        }

        /// <summary>
        /// Gets or sets the scale factor for pattern playback (1.0 = normal)
        /// </summary>
        public float ScaleFactor
        {
            get { return _scaleFactor; }
            set { _scaleFactor = value > 0 ? value : 1.0f; }
        }

        /// <summary>
        /// Gets or sets number of interpolation points between pattern points for smoother playback
        /// </summary>
        public int InterpolationPoints
        {
            get { return _interpolationPoints; }
            set { _interpolationPoints = Math.Max(0, value); }
        }

        /// <summary>
        /// Gets or sets delay in milliseconds before pattern playback starts
        /// </summary>
        public int DelayBeforeStart
        {
            get { return _delayBeforeStart; }
            set { _delayBeforeStart = Math.Max(0, value); }
        }

        /// <summary>
        /// Gets whether the pattern is currently being followed
        /// </summary>
        public bool IsFollowing
        {
            get { lock (_lockObject) { return _isPlaying; } }
            private set { lock (_lockObject) { _isPlaying = value; } }
        }

        /// <summary>
        /// Gets the current pattern being followed
        /// </summary>
        public RecoilPattern CurrentPattern => _pattern;

        /// <summary>
        /// Gets the progress of pattern playback (0.0 to 1.0)
        /// </summary>
        public double Progress
        {
            get
            {
                if (_pattern?.Points == null || _pattern.Points.Count == 0)
                    return 0;
                return Math.Min(1.0, (double)_currentPointIndex / _pattern.Points.Count);
            }
        }

        /// <summary>
        /// Creates a new instance of the RecoilPatternFollower
        /// </summary>
        /// <param name="pattern">The recoil pattern to follow</param>
        /// <exception cref="ArgumentNullException">Thrown if pattern is null</exception>
        public RecoilPatternFollower(RecoilPattern pattern)
        {
            _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));

            // Use higher refresh rate for better accuracy
            _timer = new System.Timers.Timer(5); // 5ms timer for smooth updates 
            _timer.Elapsed += Timer_Elapsed;
            _delayTimer = new System.Timers.Timer();
            _delayTimer.Elapsed += DelayTimer_Elapsed;
            _delayTimer.AutoReset = false;
        }

        /// <summary>
        /// Starts following the recoil pattern
        /// </summary>
        /// <returns>True if successfully started, false otherwise</returns>
        public bool StartFollowing()
        {
            try
            {
                lock (_lockObject)
                {
                    if (IsFollowing || _isDisposed)
                        return false;

                    if (_pattern?.Points == null || _pattern.Points.Count == 0)
                    {
                        OnPlaybackError(new Exception("No valid pattern to follow"));
                        return false;
                    }

                    // Get initial cursor position
                    if (!GetCursorPos(out POINT point))
                    {
                        OnPlaybackError(new Exception("Failed to get cursor position"));
                        return false;
                    }

                    _startPosition = new Point(point.X, point.Y);
                    _currentPointIndex = 0;
                    _lastPoint = null;

                    // If delay is set, use a delay timer
                    if (_delayBeforeStart > 0)
                    {
                        _isDelayedStart = true;
                        _delayTimer.Interval = _delayBeforeStart;
                        _delayTimer.Start();
                        return true;
                    }
                    else
                    {
                        return ActuallyStartFollowing();
                    }
                }
            }
            catch (Exception ex)
            {
                OnPlaybackError(ex);
                return false;
            }
        }

        private bool ActuallyStartFollowing()
        {
            try
            {
                _isDelayedStart = false;
                _playbackStartTime = DateTime.Now;
                IsFollowing = true;
                OnPlaybackStarted();
                _timer.Start();
                return true;
            }
            catch (Exception ex)
            {
                OnPlaybackError(ex);
                return false;
            }
        }

        private void DelayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_isDelayedStart)
            {
                ActuallyStartFollowing();
            }
        }

        /// <summary>
        /// Stops following the recoil pattern
        /// </summary>
        public void StopFollowing()
        {
            lock (_lockObject)
            {
                if (!IsFollowing && !_isDelayedStart)
                    return;

                IsFollowing = false;
                _isDelayedStart = false;
                _timer.Stop();
                _delayTimer.Stop();
            }
        }

        /// <summary>
        /// Restarts following the recoil pattern from the beginning
        /// </summary>
        /// <returns>True if successfully restarted, false otherwise</returns>
        public bool RestartFollowing()
        {
            StopFollowing();
            return StartFollowing();
        }

        /// <summary>
        /// Processes a timer event to update the current position in the recoil pattern
        /// </summary>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsFollowing) return;

            try
            {
                // Calculate elapsed time since start
                int elapsedMs = (int)(DateTime.Now - _playbackStartTime).TotalMilliseconds;

                // Find the appropriate point in the pattern based on time
                RecoilPoint currentPoint = null;
                RecoilPoint nextPoint = null;
                int nextIndex = _currentPointIndex;
                bool indexChanged = false;

                // Find the current and next points that bracket the current time
                while (nextIndex < _pattern.Points.Count)
                {
                    var point = _pattern.Points[nextIndex];
                    if (point.TimeOffset > elapsedMs)
                    {
                        nextPoint = point;
                        break;
                    }
                    currentPoint = point;
                    nextIndex++;
                    indexChanged = true;
                }

                // If we found a valid current point
                if (currentPoint != null)
                {
                    // If we have a next point, interpolate between them based on time
                    if (nextPoint != null && _interpolationPoints > 0)
                    {
                        double timeDiff = nextPoint.TimeOffset - currentPoint.TimeOffset;
                        double timeProgress = (elapsedMs - currentPoint.TimeOffset) / timeDiff;

                        // Interpolate x and y
                        double interpolatedX = currentPoint.X + (nextPoint.X - currentPoint.X) * timeProgress;
                        double interpolatedY = currentPoint.Y + (nextPoint.Y - currentPoint.Y) * timeProgress;

                        // Create an interpolated point
                        currentPoint = new RecoilPoint(
                            (int)interpolatedX,
                            (int)interpolatedY,
                            elapsedMs
                        );
                    }

                    // Only update position if the point is different from the last one
                    if (_lastPoint == null ||
                        _lastPoint.X != currentPoint.X ||
                        _lastPoint.Y != currentPoint.Y)
                    {
                        HandlePointUpdate(currentPoint, indexChanged);
                        _lastPoint = currentPoint;
                    }

                    if (indexChanged)
                    {
                        _currentPointIndex = nextIndex;
                    }
                }

                // Check if we reached the end of the pattern
                if (_currentPointIndex >= _pattern.Points.Count)
                {
                    StopFollowing();
                    OnPlaybackFinished();
                }
            }
            catch (Exception ex)
            {
                StopFollowing();
                OnPlaybackError(ex);
            }
        }

        /// <summary>
        /// Handles updating the current position and notifying subscribers
        /// </summary>
        private void HandlePointUpdate(RecoilPoint point, bool isNewPoint)
        {
            try
            {
                // Apply scale factor to make pattern larger or smaller
                int scaledX = (int)(point.X * _scaleFactor);
                int scaledY = (int)(point.Y * _scaleFactor);

                // Calculate the absolute position from the start
                Point targetPosition = new Point(
                    _startPosition.X + scaledX,
                    _startPosition.Y + scaledY
                );

                // Check if we need to auto-correct mouse position
                if (_autoCorrectMouse)
                {
                    // Check if the cursor is within the valid screen bounds
                    if (IsPointOnScreen(targetPosition))
                    {
                        SetCursorPos(targetPosition.X, targetPosition.Y);
                    }
                }

                // Create a new event arg with the current point
                var eventArg = new RecoilPointEventArgs(point, targetPosition);

                // Notify subscribers of the updated position
                OnPositionUpdated(eventArg);

                // If this is a new point (not interpolated), raise the PointReached event
                if (isNewPoint)
                {
                    OnPointReached(eventArg);
                }
            }
            catch (Exception ex)
            {
                // Log the error and continue - don't stop the pattern
                Console.WriteLine($"Error handling point update: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a point is within the bounds of the screen
        /// </summary>
        private bool IsPointOnScreen(Point p)
        {
            return p.X >= 0 && p.Y >= 0 &&
                   p.X < Screen.PrimaryScreen.Bounds.Width &&
                   p.Y < Screen.PrimaryScreen.Bounds.Height;
        }

        /// <summary>
        /// Gets the current progress information
        /// </summary>
        /// <returns>Information about the current playback progress</returns>
        public RecoilFollowerStatus GetStatus()
        {
            lock (_lockObject)
            {
                return new RecoilFollowerStatus
                {
                    IsFollowing = IsFollowing,
                    IsDelayed = _isDelayedStart,
                    CurrentPointIndex = _currentPointIndex,
                    TotalPoints = _pattern?.Points?.Count ?? 0,
                    Progress = Progress,
                    ElapsedTime = IsFollowing
                        ? (int)(DateTime.Now - _playbackStartTime).TotalMilliseconds
                        : 0,
                    StartPosition = _startPosition,
                    LastPoint = _lastPoint
                };
            }
        }

        #region Event Raisers

        protected virtual void OnPositionUpdated(RecoilPointEventArgs e)
        {
            PositionUpdated?.Invoke(this, e);
        }

        protected virtual void OnPlaybackFinished()
        {
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPlaybackStarted()
        {
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPointReached(RecoilPointEventArgs e)
        {
            PointReached?.Invoke(this, e);
        }

        protected virtual void OnPlaybackError(Exception ex)
        {
            Console.WriteLine($"Playback error: {ex.Message}");
            PlaybackError?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region IDisposable Support

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
                    StopFollowing();
                    _timer?.Dispose();
                    _delayTimer?.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~RecoilPatternFollower()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// Enhanced event arguments for recoil point events with additional information
    /// </summary>
    public class RecoilPointEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the recoil point data
        /// </summary>
        public RecoilPoint Point { get; }

        /// <summary>
        /// Gets the absolute screen position of the point
        /// </summary>
        public Point ScreenPosition { get; }

        /// <summary>
        /// Gets the time in milliseconds from the start of the pattern
        /// </summary>
        public int TimeOffset => Point.TimeOffset;

        public RecoilPointEventArgs(RecoilPoint point, Point screenPosition)
        {
            Point = point;
            ScreenPosition = screenPosition;
        }

        public RecoilPointEventArgs(RecoilPoint point)
            : this(point, new Point(0, 0))
        {
        }
    }

    /// <summary>
    /// Represents the current status of the recoil pattern follower
    /// </summary>
    public class RecoilFollowerStatus
    {
        public bool IsFollowing { get; set; }
        public bool IsDelayed { get; set; }
        public int CurrentPointIndex { get; set; }
        public int TotalPoints { get; set; }
        public double Progress { get; set; }
        public int ElapsedTime { get; set; }
        public Point StartPosition { get; set; }
        public RecoilPoint LastPoint { get; set; }
    }
}