using System;
using System.Drawing;
using System.Windows.Forms;
using poji.Properties;

namespace poji
{
    public class CrosshairInputDialog : Form
    {
        private TextBox _textBox;
        private Button _okButton;
        private Button _cancelButton;
        private Label _promptLabel;
        private CrosshairPreviewPanel _previewPanel;
        private Label _validationLabel;
        private TrackBar _scaleTrackBar;
        private Label _scaleLabel;

        public string InputText => _textBox.Text;
        public CsgoCrosshairDecoder.CrosshairInfo CrosshairInfo => _previewPanel.GetCrosshairInfo();
        public float PreviewScale { get; private set; } = 1.0f;

        public CrosshairInputDialog(string title, string prompt)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(450, 400);
            AcceptButton = null;
            CancelButton = null;

            InitializeControls(prompt);
        }

        public sealed override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        private void InitializeControls(string prompt)
        {
            // Prompt label
            _promptLabel = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(430, 20)
            };

            // Text input box
            _textBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 25)
            };

            // Preview button next to text box
            var previewButton = new Button
            {
                Text = "Preview",
                Location = new Point(340, 38),
                Size = new Size(90, 28)
            };
            previewButton.Click += PreviewButton_Click;

            // Validation message
            _validationLabel = new Label
            {
                Location = new Point(10, 70),
                Size = new Size(430, 20),
                ForeColor = Color.Red
            };

            // Preview panel
            _previewPanel = new CrosshairPreviewPanel
            {
                Location = new Point(75, 75),
                Size = new Size(300, 200)
            };
            _previewPanel.ValidationStatusChanged += PreviewPanel_ValidationStatusChanged;

            // Scale trackbar
            _scaleLabel = new Label
            {
                Text = "Preview Scale: 1.0x",
                Location = new Point(10, 290),
                Size = new Size(150, 20)
            };

            _scaleTrackBar = new TrackBar
            {
                Location = new Point(150, 286),
                Size = new Size(280, 45),
                Minimum = 5,
                Maximum = 30,
                Value = 10,
                TickFrequency = 5,
                LargeChange = 5,
                SmallChange = 1
            };
            _scaleTrackBar.ValueChanged += ScaleTrackBar_ValueChanged;

            // OK and Cancel buttons
            _okButton = new Button
            {
                Text = Resources.InputDialog_InitializeControls_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(265, 330),
                Size = new Size(75, 28),
                Enabled = false // Disabled until valid preview
            };

            _cancelButton = new Button
            {
                Text = Resources.InputDialog_InitializeControls_Cancel,
                DialogResult = DialogResult.Cancel,
                Location = new Point(345, 330),
                Size = new Size(85, 28)
            };

            // Add controls to form
            Controls.Add(_promptLabel);
            Controls.Add(_textBox);
            Controls.Add(previewButton);
            Controls.Add(_validationLabel);
            Controls.Add(_previewPanel);
            Controls.Add(_scaleLabel);
            Controls.Add(_scaleTrackBar);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);

            CancelButton = _cancelButton;

            // Set up auto-validation when typing
            _textBox.TextChanged += TextBox_TextChanged;
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            // Optional: Auto-validation as user types
            // For better performance, consider using a timer to delay validation
            // until typing pauses
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            bool isValid = _previewPanel.TrySetCrosshair(_textBox.Text);
            _okButton.Enabled = isValid;
        }

        private void PreviewPanel_ValidationStatusChanged(object sender, EventArgs e)
        {
            _validationLabel.Text = _previewPanel.ValidationMessage;
            _validationLabel.ForeColor = _previewPanel.IsValid ? Color.Green : Color.Red;
            _okButton.Enabled = _previewPanel.IsValid;
        }

        private void ScaleTrackBar_ValueChanged(object sender, EventArgs e)
        {
            float scale = _scaleTrackBar.Value / 10.0f;
            PreviewScale = scale;
            _scaleLabel.Text = $"Preview Scale: {scale:F1}x";
            _previewPanel.SetScale(scale);
        }
    }
}