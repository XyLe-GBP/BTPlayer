using System.Threading;

namespace BTPlayer
{
    internal sealed class FormSettings : Form
    {
        private readonly CheckBox checkBoxAutoPlay;

        public bool AutoPlayOnDeviceConnected => checkBoxAutoPlay.Checked;

        public FormSettings(bool autoPlayOnDeviceConnected)
        {
            Text = GetUiText("Settings", "設定");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(430, 132);
            BackColor = Color.FromArgb(245, 247, 250);

            checkBoxAutoPlay = new CheckBox
            {
                AutoSize = false,
                Checked = autoPlayOnDeviceConnected,
                Font = new Font("Yu Gothic UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 128),
                Location = new Point(18, 18),
                Size = new Size(390, 30),
                Text = GetUiText(
                    "Automatically open audio when a device is connected",
                    "デバイス接続時に自動でOpenする"),
                UseVisualStyleBackColor = true
            };

            var buttonOk = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(236, 82),
                Size = new Size(82, 32),
                Text = "OK",
                UseVisualStyleBackColor = true
            };

            var buttonCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(328, 82),
                Size = new Size(82, 32),
                Text = GetUiText("Cancel", "キャンセル"),
                UseVisualStyleBackColor = true
            };

            Controls.Add(checkBoxAutoPlay);
            Controls.Add(buttonOk);
            Controls.Add(buttonCancel);

            AcceptButton = buttonOk;
            CancelButton = buttonCancel;
        }

        private static string GetUiText(string english, string japanese)
        {
            return Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase)
                ? japanese
                : english;
        }
    }
}
