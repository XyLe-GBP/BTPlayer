using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using static BTPlayer.Common;
using BTPlayer.Localizations;
using Localization = BTPlayer.Localizations.Localization;

namespace BTPlayer
{
    public partial class FormProgress : Form
    {
        public FormProgress()
        {
            InitializeComponent();
        }

        private int DownloadProgress = 0;
        private string DownloadStatus = "";

        private void FormProgress_Load(object sender, EventArgs e)
        {
            Text = Localization.ProcessingText;
            label_log1.Text = Localization.InitializingText;
            label_Progress.Text = string.Empty;
            //label_log3.Text = string.Empty;
            timer_interval.Interval = 3000;
            progressBar_MainProgress.Value = 0;
            progressBar_MainProgress.Minimum = 0;
            progressBar_MainProgress.Maximum = ProgressMax;

            RunTask();
        }

        private async void RunTask()
        {
            switch (ProgressType)
            {
                case 0: // Update
                    {
                        label_Progress.Text = Localization.ProcessingText;
                        cts = new CancellationTokenSource();
                        var cT = cts.Token;
                        var p = new Progress<int>(UpdateProgress);

                        Result = await Task.Run(() => Download_DoWork(p, cT));
                    }
                    break;
                default:
                    {
                        label_Progress.Text = Localization.ProcessUnknownText;
                    }
                    break;
            }
            timer_interval.Enabled = true;
        }

        private bool Download_DoWork(IProgress<int> p, CancellationToken cToken)
        {
            if (downloadClient == null)
            {
#pragma warning disable SYSLIB0014 // 型またはメンバーが旧型式です
                downloadClient = new System.Net.WebClient();
#pragma warning restore SYSLIB0014 // 型またはメンバーが旧型式です
                downloadClient.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(DownloadClient_DownloadProgressChanged);
                downloadClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadClient_DownloadFileCompleted);
            }

            Invoke(new Action(() => Text = Localization.FormDownloadingCaption));
            var task = Download(cToken);

            while (task.IsCompleted != true)
            {
                if (task.Result == false)
                {
                    return false;
                }
            }
            if (cToken.IsCancellationRequested == true)
            {
                return false;
            }
            else
            {
                Invoke(new Action(() => Text = Localization.ProcessingText));
                Invoke(new Action(() => label_Progress.Text = Localization.ProcessingText));
                Invoke(new Action(() => button_Abort.Enabled = false));
            }

            return true;
        }

        private async Task<bool> Download(CancellationToken cToken)
        {
            Uri uri;

            switch (ApplicationPortable)
            {
                case false:
                    {
                        uri = new("https://github.com/XyLe-GBP/btplayer/releases/download/v" + GitHubLatestVersion + "/btplayer-release.zip");
                    }
                    break;
                case true:
                    {
                        uri = new("https://github.com/XyLe-GBP/btplayer/releases/download/v" + GitHubLatestVersion + "/btplayer-portable.zip");
                    }
                    break;
            }

            switch (ApplicationPortable)
            {
                case false: // release
                    {
                        downloadClient.DownloadFileAsync(uri, Directory.GetCurrentDirectory() + @"\res\btplayer.zip");
                    }
                    break;
                case true: // portable
                    {
                        downloadClient.DownloadFileAsync(uri, Directory.GetCurrentDirectory() + @"\res\btplayer.zip");
                    }
                    break;
            }
            IsDownloading = true;

            while (downloadClient.IsBusy)
            {
                if (cToken.IsCancellationRequested == true)
                {
                    return await Task.FromResult(false);
                }
                Invoke(new Action(() => progressBar_MainProgress.Value = DownloadProgress));
                Invoke(new Action(() => label_log1.Text = DownloadStatus));
            }
            return await Task.FromResult(true);
        }

        private void DownloadClient_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            if (IsDownloading == true)
            {
                progressBar_MainProgress.Maximum = 100;
                IsDownloading = false;
            }
            DownloadProgress = e.ProgressPercentage;
            DownloadStatus = string.Format(Localization.DownloadingCaption, e.ProgressPercentage, e.TotalBytesToReceive / 1024, e.BytesReceived / 1024);
        }

        private void DownloadClient_DownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled) // Cancelled
            {
                downloadClient!.Dispose();
            }
            else if (e.Error != null) // Error
            {
                downloadClient!.Dispose();
            }
            else
            {
                downloadClient!.Dispose();
            }
        }

        private void UpdateProgress(int p)
        {
            switch (ProgressType)
            {
                default:
                    progressBar_MainProgress.Value = p;
                    label_log1.Text = string.Format(Localization.ProgressText, p, Count);
                    break;
            }
        }

        private void timer_interval_Tick(object sender, EventArgs e)
        {
            System.Media.SystemSounds.Asterisk.Play();
            Close();
        }

        private void button_Abort_Click(object sender, EventArgs e)
        {
            if (cts != null)
            {
                if (downloadClient.IsBusy)
                {
                    DialogResult dr = System.Windows.Forms.MessageBox.Show(Localization.DownloadAbortConfirmCaption, Localization.WarningCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        cts.Cancel();
                        downloadClient.CancelAsync();
                        Close();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    cancel.Cancel();
                    Close();
                }
            }
        }
    }
}
