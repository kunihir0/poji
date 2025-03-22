using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.Json;


namespace poji
{
    /// <summary>
    /// Represents a single point in a recoil pattern
    /// </summary>
    public class RecoilPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int TimeOffset { get; set; } // Time in milliseconds from start of pattern

        public RecoilPoint() { }

        public RecoilPoint(int x, int y, int timeOffset)
        {
            X = x;
            Y = y;
            TimeOffset = timeOffset;
        }
    }

    /// <summary>
    /// Represents a complete recoil pattern with metadata
    /// </summary>
    public class RecoilPattern
    {
        public string Name { get; set; }
        public string Game { get; set; }
        public string Weapon { get; set; }
        public List<RecoilPoint> Points { get; set; } = new List<RecoilPoint>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public RecoilPattern()
        {
            Name = "New Pattern";
            Game = "Unknown";
            Weapon = "Unknown";
        }

        public RecoilPattern Clone()
        {
            return new RecoilPattern
            {
                Name = this.Name,
                Game = this.Game,
                Weapon = this.Weapon,
                CreatedDate = this.CreatedDate,
                Points = this.Points.Select(p => new RecoilPoint(p.X, p.Y, p.TimeOffset)).ToList()
            };
        }
    }

    /// <summary>
    /// Handles recording recoil patterns by tracking mouse movements
    /// </summary>
    public class RecoilPatternRecorder : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private bool _isRecording;
        private Thread _recordingThread;
        private readonly RecoilPattern _currentPattern;
        private Point _startPosition;
        private DateTime _recordingStartTime;
        private readonly object _lockObject = new object();

        public event EventHandler RecordingStarted;
        public event EventHandler RecordingStopped;
        public event EventHandler<RecoilPoint> PointRecorded;

        public RecoilPatternRecorder()
        {
            _currentPattern = new RecoilPattern();
        }

        public bool IsRecording
        {
            get { lock (_lockObject) { return _isRecording; } }
            private set { lock (_lockObject) { _isRecording = value; } }
        }

        public void StartRecording()
        {
            if (IsRecording) return;

            // Clear previous recording
            _currentPattern.Points.Clear();

            // Get initial position
            GetCursorPos(out POINT point);
            _startPosition = new Point(point.X, point.Y);
            _recordingStartTime = DateTime.Now;

            IsRecording = true;
            RecordingStarted?.Invoke(this, EventArgs.Empty);

            // Start recording thread
            _recordingThread = new Thread(RecordingLoop)
            {
                IsBackground = true
            };
            _recordingThread.Start();
        }

        public void StopRecording()
        {
            if (!IsRecording) return;

            IsRecording = false;
            RecordingStopped?.Invoke(this, EventArgs.Empty);
        }

        private void RecordingLoop()
        {
            while (IsRecording)
            {
                // Get current mouse position
                GetCursorPos(out POINT currentPoint);

                // Calculate relative position from start
                int relativeX = currentPoint.X - _startPosition.X;
                int relativeY = currentPoint.Y - _startPosition.Y;

                // Calculate time offset
                int timeOffset = (int)(DateTime.Now - _recordingStartTime).TotalMilliseconds;

                // Create and add point
                var recoilPoint = new RecoilPoint(relativeX, relativeY, timeOffset);
                _currentPattern.Points.Add(recoilPoint);

                // Notify subscribers
                PointRecorded?.Invoke(this, recoilPoint);

                // Delay to avoid too many points
                Thread.Sleep(10); // 100Hz sampling rate
            }
        }

        public RecoilPattern GetRecordedPattern()
        {
            return _currentPattern.Clone();
        }

        public void Dispose()
        {
            StopRecording();
        }
    }

    /// <summary>
    /// Handles drawing and displaying recoil patterns
    /// </summary>
    public class RecoilPatternRenderer
    {
        public RecoilPattern CurrentPattern { get; set; }
        public bool IsEnabled { get; set; }
        public float ScaleFactor { get; set; } = 1.0f;
        public Color PatternColor { get; set; } = Color.FromArgb(180, 0, 255, 0); // Semi-transparent green
        public Color GuideColor { get; set; } = Color.FromArgb(120, 255, 0, 0);   // Semi-transparent red

        public RecoilPatternRenderer()
        {
            CurrentPattern = null;
            IsEnabled = false;
        }

        public void Draw(Graphics g, int centerX, int centerY)
        {
            if (!IsEnabled || CurrentPattern == null || CurrentPattern.Points.Count < 2)
                return;

            using (var patternPen = new Pen(PatternColor, 2))
            using (var pointBrush = new SolidBrush(PatternColor))
            using (var guidePen = new Pen(GuideColor, 1))
            {
                // Draw pattern line
                var points = CurrentPattern.Points
                    .Select(p => new Point(
                        centerX + (int)(p.X * ScaleFactor),
                        centerY + (int)(p.Y * ScaleFactor)))
                    .ToArray();

                g.DrawLines(patternPen, points);

                // Draw points
                foreach (var point in points)
                {
                    g.FillEllipse(pointBrush, point.X - 2, point.Y - 2, 4, 4);
                }

                // Draw mouse guide (from center to last point)
                if (points.Length > 0)
                {
                    g.DrawLine(guidePen, centerX, centerY, points.Last().X, points.Last().Y);
                }
            }
        }
    }

    /// <summary>
    /// Handles saving and loading recoil patterns from files
    /// </summary>
    public class RecoilPatternManager
    {
        private readonly string _patternsDirectory;

        public RecoilPatternManager()
        {
            _patternsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "poji", "RecoilPatterns");

            // Ensure directory exists
            if (!Directory.Exists(_patternsDirectory))
            {
                Directory.CreateDirectory(_patternsDirectory);
            }
        }

        public void SavePattern(RecoilPattern pattern, string filename = null)
        {
            if (pattern == null || pattern.Points.Count == 0)
                throw new ArgumentException("Cannot save empty pattern");

            if (string.IsNullOrWhiteSpace(filename))
            {
                // Generate filename from pattern details
                string safeGameName = MakeValidFilename(pattern.Game);
                string safeWeaponName = MakeValidFilename(pattern.Weapon);
                string safePatternName = MakeValidFilename(pattern.Name);

                filename = $"{safeGameName}_{safeWeaponName}_{safePatternName}.json";
            }

            string fullPath = Path.Combine(_patternsDirectory, filename);
            string json = JsonSerializer.Serialize(pattern, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(fullPath, json);
        }

        public RecoilPattern LoadPattern(string filename)
        {
            string fullPath = Path.Combine(_patternsDirectory, filename);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Pattern file not found: {filename}");

            string json = File.ReadAllText(fullPath);
            return JsonSerializer.Deserialize<RecoilPattern>(json);
        }

        public List<string> GetAvailablePatternFilenames()
        {
            return Directory.GetFiles(_patternsDirectory, "*.json")
                .Select(Path.GetFileName)
                .ToList();
        }

        private string MakeValidFilename(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}