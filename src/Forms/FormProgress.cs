using System.IO;
using System.Net.Http;
using static BTPlayer.Common;
using Localization = BTPlayer.Localizations.Localization;

namespace BTPlayer
{
    public partial class FormProgress : Form
    {
        private static readonly HttpClientHandler downloadHandler = new()
        {
            UseProxy = false,
            UseCookies = false
        };

        private static readonly HttpClient updateDownloadClient = new(downloadHandler);

        private bool isDownloading;

        public FormProgress()
        {
            InitializeComponent();
        }

        private void FormProgress_Load(object sender, EventArgs e)
        {
            Text = Localization.ProcessingText;
            label_log1.Text = Localization.InitializingText;
            label_Progress.Text = string.Empty;
            timer_interval.Interval = 3000;
            progressBar_MainProgress.Style = ProgressBarStyle.Blocks;
            progressBar_MainProgress.Minimum = 0;
            progressBar_MainProgress.Maximum = Math.Max(1, ProgressMax);
            progressBar_MainProgress.Value = 0;

            RunTask();
        }

        private async void RunTask()
        {
            Result = false;

            try
            {
                switch (ProgressType)
                {
                    case 0:
                    case 7:
                        label_Progress.Text = Localization.ProcessingText;
                        cts?.Dispose();
                        cts = new CancellationTokenSource();
                        Result = await DownloadUpdatePackageAsync(cts.Token);
                        break;
                    default:
                        label_Progress.Text = Localization.ProcessUnknownText;
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                Result = false;
                label_Progress.Text = Localization.CancelledCaption;
            }
            catch (Exception ex)
            {
                Result = false;
                label_Progress.Text = Localization.ErrorCaption;
                label_log1.Text = string.Format(Localization.UnExpectedErrorCaption, ex.Message);
            }
            finally
            {
                isDownloading = false;
                button_Abort.Enabled = false;
                timer_interval.Enabled = true;
            }
        }

        private async Task<bool> DownloadUpdatePackageAsync(CancellationToken cancellationToken)
        {
            Text = Localization.FormDownloadingCaption;
            label_Progress.Text = Localization.ProcessingText;
            label_log1.Text = Localization.InitializingText;
            button_Abort.Enabled = true;
            isDownloading = true;

            string destinationPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "res",
                "btplayer.zip");

            var progress = new Progress<DownloadProgressInfo>(UpdateDownloadProgress);

            await Common.Network.DownloadFileWithProgressAsync(
                updateDownloadClient,
                GetUpdatePackageUri(),
                destinationPath,
                progress,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            Text = Localization.ProcessingText;
            label_Progress.Text = Localization.ProcessingText;
            button_Abort.Enabled = false;
            isDownloading = false;

            return true;
        }

        private static Uri GetUpdatePackageUri()
        {
            if (string.IsNullOrWhiteSpace(GitHubLatestVersion))
            {
                throw new InvalidOperationException("GitHubLatestVersion is empty.");
            }

            string packageName = ApplicationPortable
                ? "btplayer-portable.zip"
                : "btplayer-release.zip";

            return Common.Network.GetUri(
                "https://github.com/XyLe-GBP/btplayer/releases/download/" +
                GitHubLatestVersion +
                "/" +
                packageName);
        }

        private void UpdateDownloadProgress(DownloadProgressInfo progress)
        {
            progressBar_MainProgress.Maximum = 100;
            progressBar_MainProgress.Value = Math.Clamp(
                progress.ProgressPercentage,
                progressBar_MainProgress.Minimum,
                progressBar_MainProgress.Maximum);

            long receivedKiB = progress.BytesReceived / 1024;

            if (progress.TotalBytesToReceive is long totalBytes)
            {
                long totalKiB = totalBytes / 1024;
                label_log1.Text = string.Format(Localization.DownloadingCaption, receivedKiB, totalKiB);
            }
            else
            {
                label_log1.Text = receivedKiB + " KiB";
            }
        }

        private void timer_interval_Tick(object sender, EventArgs e)
        {
            System.Media.SystemSounds.Asterisk.Play();
            Close();
        }

        private void button_Abort_Click(object sender, EventArgs e)
        {
            if (cts == null || cts.IsCancellationRequested)
            {
                return;
            }

            if (isDownloading)
            {
                DialogResult dialogResult = MessageBox.Show(
                    Localization.DownloadAbortConfirmCaption,
                    Localization.WarningCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (dialogResult != DialogResult.Yes)
                {
                    return;
                }
            }

            cts.Cancel();
            button_Abort.Enabled = false;
            label_Progress.Text = Localization.CancelledCaption;
        }
    }
}
