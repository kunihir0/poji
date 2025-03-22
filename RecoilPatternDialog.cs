using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace poji
{
    /// <summary>
    /// Dialog for managing recoil patterns
    /// </summary>
    public class RecoilPatternDialog : Form
    {
        private readonly RecoilPatternManager _patternManager;
        private readonly RecoilPatternRenderer _patternRenderer;
        private readonly RecoilPatternRecorder _patternRecorder;

        private ListBox _patternsListBox;
        private Button _loadButton;
        private Button _deleteButton;
        private Button _recordButton;
        private Button _saveButton;
        private Button _cancelButton;
        private Panel _previewPanel;
        private TextBox _nameTextBox;
        private TextBox _gameTextBox;
        private TextBox _weaponTextBox;
        private Label _statusLabel;
        private bool _isRecording = false;

        public RecoilPattern SelectedPattern { get; private set; }

        public RecoilPatternDialog(RecoilPatternManager patternManager, RecoilPatternRenderer patternRenderer)
        {
            _patternManager = patternManager;
            _patternRenderer = patternRenderer;
            _patternRecorder = new RecoilPatternRecorder();

            // Set up event handlers
            _patternRecorder.RecordingStarted += PatternRecorder_RecordingStarted;
            _patternRecorder.RecordingStopped += PatternRecorder_RecordingStopped;

            InitializeComponent();
            LoadPatternList();
        }

        private void InitializeComponent()
        {
            // Form setup
            Text = "Recoil Pattern Manager";
            Size = new Size(600, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            // Create controls
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200
            };

            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            // Left panel controls
            var patternsLabel = new Label
            {
                Text = "Available Patterns:",
                Location = new Point(10, 10),
                AutoSize = true
            };

            _patternsListBox = new ListBox
            {
                Location = new Point(10, 30),
                Size = new Size(180, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right
            };
            _patternsListBox.SelectedIndexChanged += PatternsListBox_SelectedIndexChanged;

            _loadButton = new Button
            {
                Text = "Load Selected",
                Location = new Point(10, 340),
                Size = new Size(180, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _loadButton.Click += LoadButton_Click;

            _deleteButton = new Button
            {
                Text = "Delete Selected",
                Location = new Point(10, 375),
                Size = new Size(180, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _deleteButton.Click += DeleteButton_Click;

            // Right panel controls
            var detailsGroupBox = new GroupBox
            {
                Text = "Pattern Details",
                Location = new Point(10, 10),
                Size = new Size(365, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var nameLabel = new Label
            {
                Text = "Name:",
                Location = new Point(10, 25),
                AutoSize = true
            };

            _nameTextBox = new TextBox
            {
                Location = new Point(70, 22),
                Size = new Size(285, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var gameLabel = new Label
            {
                Text = "Game:",
                Location = new Point(10, 55),
                AutoSize = true
            };

            _gameTextBox = new TextBox
            {
                Location = new Point(70, 52),
                Size = new Size(285, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var weaponLabel = new Label
            {
                Text = "Weapon:",
                Location = new Point(10, 85),
                AutoSize = true
            };

            _weaponTextBox = new TextBox
            {
                Location = new Point(70, 82),
                Size = new Size(285, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _previewPanel = new Panel
            {
                Location = new Point(10, 120),
                Size = new Size(365, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Black
            };
            _previewPanel.Paint += PreviewPanel_Paint;

            _statusLabel = new Label
            {
                Text = "Ready",
                Location = new Point(10, 330),
                Size = new Size(365, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var buttonPanel = new Panel
            {
                Location = new Point(10, 360),
                Size = new Size(365, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _recordButton = new Button
            {
                Text = "Record New Pattern",
                Location = new Point(0, 0),
                Size = new Size(150, 30)
            };
            _recordButton.Click += RecordButton_Click;

            _saveButton = new Button
            {
                Text = "Save Pattern",
                Location = new Point(160, 0),
                Size = new Size(100, 30),
                Enabled = false
            };
            _saveButton.Click += SaveButton_Click;

            _cancelButton = new Button
            {
                Text = "Close",
                Location = new Point(270, 0),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Cancel
            };

            // Add controls to panels
            leftPanel.Controls.Add(patternsLabel);
            leftPanel.Controls.Add(_patternsListBox);
            leftPanel.Controls.Add(_loadButton);
            leftPanel.Controls.Add(_deleteButton);

            detailsGroupBox.Controls.Add(nameLabel);
            detailsGroupBox.Controls.Add(_nameTextBox);
            detailsGroupBox.Controls.Add(gameLabel);
            detailsGroupBox.Controls.Add(_gameTextBox);
            detailsGroupBox.Controls.Add(weaponLabel);
            detailsGroupBox.Controls.Add(_weaponTextBox);

            buttonPanel.Controls.Add(_recordButton);
            buttonPanel.Controls.Add(_saveButton);
            buttonPanel.Controls.Add(_cancelButton);

            rightPanel.Controls.Add(detailsGroupBox);
            rightPanel.Controls.Add(_previewPanel);
            rightPanel.Controls.Add(_statusLabel);
            rightPanel.Controls.Add(buttonPanel);

            // Add panels to form
            Controls.Add(rightPanel);
            Controls.Add(leftPanel);

            // Set accept and cancel buttons
            AcceptButton = _loadButton;
            CancelButton = _cancelButton;
        }

        private void LoadPatternList()
        {
            _patternsListBox.Items.Clear();
            
            foreach (string filename in _patternManager.GetAvailablePatternFilenames())
            {
                _patternsListBox.Items.Add(filename);
            }

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool patternSelected = _patternsListBox.SelectedIndex >= 0;
            bool hasCurrentPattern = _patternRecorder.GetRecordedPattern()?.Points?.Count > 0;

            _loadButton.Enabled = patternSelected && !_isRecording;
            _deleteButton.Enabled = patternSelected && !_isRecording;
            _saveButton.Enabled = hasCurrentPattern && !_isRecording;
            _recordButton.Text = _isRecording ? "Stop Recording" : "Record New Pattern";
        }

        private void PatternsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();

            if (_patternsListBox.SelectedIndex >= 0)
            {
                try
                {
                    string filename = _patternsListBox.SelectedItem.ToString();
                    RecoilPattern pattern = _patternManager.LoadPattern(filename);

                    // Display pattern info
                    _nameTextBox.Text = pattern.Name;
                    _gameTextBox.Text = pattern.Game;
                    _weaponTextBox.Text = pattern.Weapon;

                    // Update preview
                    _patternRenderer.CurrentPattern = pattern;
                    _previewPanel.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading pattern: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            if (_patternsListBox.SelectedIndex >= 0)
            {
                try
                {
                    string filename = _patternsListBox.SelectedItem.ToString();
                    SelectedPattern = _patternManager.LoadPattern(filename);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading pattern: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (_patternsListBox.SelectedIndex >= 0)
            {
                string filename = _patternsListBox.SelectedItem.ToString();
                
                if (MessageBox.Show($"Are you sure you want to delete pattern '{filename}'?", 
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        string fullPath = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "poji", "RecoilPatterns", filename);
                        
                        System.IO.File.Delete(fullPath);
                        LoadPatternList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting pattern: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RecordButton_Click(object sender, EventArgs e)
        {
            if (_isRecording)
            {
                _patternRecorder.StopRecording();
            }
            else
            {
                // Check if form fields are filled
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    MessageBox.Show("Please enter a pattern name before recording.", 
                        "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _nameTextBox.Focus();
                    return;
                }

                // Update pattern metadata
                RecoilPattern pattern = _patternRecorder.GetRecordedPattern();
                pattern.Name = _nameTextBox.Text;
                pattern.Game = _gameTextBox.Text;
                pattern.Weapon = _weaponTextBox.Text;

                _statusLabel.Text = "Position your mouse at the target center and press left mouse button to start recording...";
                
                // Start recording on mouse down
                MouseEventHandler mouseDownHandler = null;
                mouseDownHandler = (s, args) =>
                {
                    if (args.Button == MouseButtons.Left)
                    {
                        _patternRecorder.StartRecording();
                        this.MouseDown -= mouseDownHandler;
                    }
                };
                
                this.MouseDown += mouseDownHandler;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                RecoilPattern pattern = _patternRecorder.GetRecordedPattern();
                pattern.Name = _nameTextBox.Text;
                pattern.Game = _gameTextBox.Text;
                pattern.Weapon = _weaponTextBox.Text;

                _patternManager.SavePattern(pattern);
                
                MessageBox.Show("Pattern saved successfully!", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                LoadPatternList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving pattern: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PatternRecorder_RecordingStarted(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                _isRecording = true;
                _statusLabel.Text = "Recording in progress... Move your mouse to record recoil pattern. Click to stop recording.";
                UpdateButtonStates();

                // Set handler to stop recording
                MouseEventHandler mouseUpHandler = null;
                mouseUpHandler = (s, args) =>
                {
                    if (args.Button == MouseButtons.Left)
                    {
                        _patternRecorder.StopRecording();
                        this.MouseUp -= mouseUpHandler;
                    }
                };
                
                this.MouseUp += mouseUpHandler;
            }));
        }

        private void PatternRecorder_RecordingStopped(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                _isRecording = false;
                _statusLabel.Text = "Recording stopped. You can now save the pattern.";
                UpdateButtonStates();

                // Get and display the recorded pattern
                RecoilPattern pattern = _patternRecorder.GetRecordedPattern();
                _patternRenderer.CurrentPattern = pattern;
                _previewPanel.Invalidate();

                // Enable save button
                _saveButton.Enabled = pattern.Points.Count > 0;
            }));
        }

        private void PreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_patternRenderer.CurrentPattern != null)
            {
                int centerX = _previewPanel.Width / 2;
                int centerY = _previewPanel.Height / 2;

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw center reference point
                using (var brush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillEllipse(brush, centerX - 3, centerY - 3, 6, 6);
                }

                // Draw pattern
                _patternRenderer.Draw(e.Graphics, centerX, centerY);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _patternRecorder?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}