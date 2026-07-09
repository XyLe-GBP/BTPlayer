using System.Threading;

namespace BTPlayer
{
    internal sealed class FormSettings : Form
    {
        private readonly CheckBox checkBoxAutoPlay;
        private readonly CheckBox checkBoxCompatibilityReopen;

        public bool AutoPlayOnDeviceConnected => checkBoxAutoPlay.Checked;
        public bool CompatibilityReopenOnOpen => checkBoxCompatibilityReopen.Checked;

        public FormSettings(bool autoPlayOnDeviceConnected, bool compatibilityReopenOnOpen)
        {
            Text = GetUiText("Settings", "設定");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(470, 172);
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

            checkBoxCompatibilityReopen = new CheckBox
            {
                AutoSize = false,
                Checked = compatibilityReopenOnOpen,
                Font = new Font("Yu Gothic UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 128),
                Location = new Point(18, 54),
                Size = new Size(430, 30),
                Text = GetUiText(
                    "Use DAP compatibility resync after audio open",
                    "音声Open後にDAP向け互換再同期を行う"),
                UseVisualStyleBackColor = true
            };

            var buttonOk = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(276, 122),
                Size = new Size(82, 32),
                Text = "OK",
                UseVisualStyleBackColor = true
            };

            var buttonCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(368, 122),
                Size = new Size(82, 32),
                Text = GetUiText("Cancel", "キャンセル"),
                UseVisualStyleBackColor = true
            };

            Controls.Add(checkBoxAutoPlay);
            Controls.Add(checkBoxCompatibilityReopen);
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
