using System.Drawing;
using System.Windows.Forms;
using poji.Properties;

namespace poji
{
    public class InputDialog : Form
    {
        private TextBox _textBox;
        private Button _okButton;
        private Button _cancelButton;
        private Label _promptLabel;

        public string InputText => _textBox.Text;

        public InputDialog(string title, string prompt)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(350, 150);
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
            _promptLabel = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(300, 20)
            };

            _textBox = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(320, 20)
            };

            _okButton = new Button
            {
                Text = Resources.InputDialog_InitializeControls_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(165, 70),
                Size = new Size(75, 23)
            };

            _cancelButton = new Button
            {
                Text = Resources.InputDialog_InitializeControls_Cancel,
                DialogResult = DialogResult.Cancel,
                Location = new Point(245, 70),
                Size = new Size(75, 23)
            };

            Controls.Add(_promptLabel);
            Controls.Add(_textBox);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }
    }
}