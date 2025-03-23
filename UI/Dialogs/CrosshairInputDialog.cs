using System;
using System.Drawing;
using System.Windows.Forms;
using poji.Controls.Crosshair;
using poji.Models;
using poji.Properties;
using Color = System.Drawing.Color;

namespace poji.UI.Dialogs
{
    /// <summary>
    /// Dialog for inputting and previewing crosshair configurations.
    /// </summary>
    public class CrosshairInputDialog : Form
    {
        private const int DefaultTrackbarValue = 10;
        private const float ScaleValueDivisor = 10.0f;

        private readonly TextBox _textBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private readonly Label _promptLabel;
        private readonly CrosshairPreviewPanel _previewPanel;
        private readonly Label _validationLabel;
        private readonly TrackBar _scaleTrackBar;
        private readonly Label _scaleLabel;

        /// <summary>
        /// Gets the input text entered by the user.
        /// </summary>
        public string InputText => _textBox.Text;

        /// <summary>
        /// Gets the crosshair information from the preview panel.
        /// </summary>
        public CrosshairInfo CrosshairInfo => _previewPanel.GetCrosshairInfo();

        /// <summary>
        /// Gets the preview scale value.
        /// </summary>
        public float PreviewScale { get; private set; } = 1.0f;

        /// <summary>
        /// Initializes a new instance of the CrosshairInputDialog.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="prompt">The prompt text.</param>
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

            // Initialize all controls
            _promptLabel = CreatePromptLabel(prompt);
            _textBox = CreateTextBox();
            var previewButton = CreatePreviewButton();
            _validationLabel = CreateValidationLabel();
            _previewPanel = CreatePreviewPanel();
            _scaleLabel = CreateScaleLabel();
            _scaleTrackBar = CreateScaleTrackBar();
            _okButton = CreateOkButton();
            _cancelButton = CreateCancelButton();

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

            // Set up event handlers
            _textBox.TextChanged += TextBox_TextChanged;
            previewButton.Click += PreviewButton_Click;
            _previewPanel.ValidationStatusChanged += PreviewPanel_ValidationStatusChanged;
            _scaleTrackBar.ValueChanged += ScaleTrackBar_ValueChanged;
        }

        /// <summary>
        /// Override of Text property to make it sealed.
        /// </summary>
        public sealed override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        #region Control Creation Methods

        private Label CreatePromptLabel(string prompt)
        {
            return new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(430, 20)
            };
        }

        private TextBox CreateTextBox()
        {
            return new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 25)
            };
        }

        private Button CreatePreviewButton()
        {
            return new Button
            {
                Text = "Preview",
                Location = new Point(340, 38),
                Size = new Size(90, 28)
            };
        }

        private Label CreateValidationLabel()
        {
            return new Label
            {
                Location = new Point(10, 70),
                Size = new Size(430, 20),
                ForeColor = Color.Red
            };
        }

        private CrosshairPreviewPanel CreatePreviewPanel()
        {
            return new CrosshairPreviewPanel
            {
                Location = new Point(75, 75),
                Size = new Size(300, 200)
            };
        }

        private Label CreateScaleLabel()
        {
            return new Label
            {
                Text = "Preview Scale: 1.0x",
                Location = new Point(10, 290),
                Size = new Size(150, 20)
            };
        }

        private TrackBar CreateScaleTrackBar()
        {
            return new TrackBar
            {
                Location = new Point(150, 286),
                Size = new Size(280, 45),
                Minimum = 5,
                Maximum = 30,
                Value = DefaultTrackbarValue,
                TickFrequency = 5,
                LargeChange = 5,
                SmallChange = 1
            };
        }

        private Button CreateOkButton()
        {
            return new Button
            {
                Text = Resources.InputDialog_InitializeControls_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(265, 330),
                Size = new Size(75, 28),
                Enabled = false // Disabled until valid preview
            };
        }

        private Button CreateCancelButton()
        {
            return new Button
            {
                Text = Resources.InputDialog_InitializeControls_Cancel,
                DialogResult = DialogResult.Cancel,
                Location = new Point(345, 330),
                Size = new Size(85, 28)
            };
        }

        #endregion

        #region Event Handlers

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            // Optional: Auto-validation could be implemented here
            // For better performance, a timer could be used to delay validation
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            bool isValid = _previewPanel.TrySetCrosshair(_textBox.Text);
            _okButton.Enabled = isValid;
        }

        private void PreviewPanel_ValidationStatusChanged(object sender, EventArgs e)
        {
            UpdateValidationStatus();
        }

        private void ScaleTrackBar_ValueChanged(object sender, EventArgs e)
        {
            UpdateScaleValue();
        }

        #endregion

        #region Helper Methods

        private void UpdateValidationStatus()
        {
            _validationLabel.Text = _previewPanel.ValidationMessage;
            _validationLabel.ForeColor = _previewPanel.IsValid ? Color.Green : Color.Red;
            _okButton.Enabled = _previewPanel.IsValid;
        }

        private void UpdateScaleValue()
        {
            float scale = _scaleTrackBar.Value / ScaleValueDivisor;
            PreviewScale = scale;
            _scaleLabel.Text = $"Preview Scale: {scale:F1}x";
            _previewPanel.SetScale(scale);
        }

        #endregion
    }
}