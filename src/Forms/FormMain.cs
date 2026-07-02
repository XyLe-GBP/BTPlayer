using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Media.Audio;
using Windows.System;
using Application = System.Windows.Forms.Application;
using FontStyle = System.Drawing.FontStyle;
using MessageBox = System.Windows.Forms.MessageBox;
using Localization = BTPlayer.Localizations.Localization;

namespace BTPlayer
{
    public partial class FormMain : Form
    {
        #region NetworkCommon
        private static readonly HttpClientHandler handler = new()
        {
            UseProxy = false,
            UseCookies = false
        };
        private static readonly HttpClient appUpdatechecker = new(handler);
        #endregion

        private const string SettingsFileName = "last_device.txt";
        private const string AppSettingsFileName = "settings.txt";
        private const int ClosedStateGracePeriodMs = 8000;

        private DeviceWatcher? deviceWatcher;
        private readonly Dictionary<string, DeviceInformation> devices = new();
        private readonly Dictionary<string, AudioPlaybackConnection> audioPlaybackConnections = new();
        private readonly Dictionary<string, CancellationTokenSource> pendingCloseConfirmations = new();
        private readonly SemaphoreSlim audioConnectionLock = new(1, 1);

        private string? currentOpenedDeviceId;
        private string? lastSelectedDeviceId;
        private string? pendingRestoreDeviceId;
        private bool suppressSelectionPersistence;
        private bool autoPlayOnDeviceConnected;

        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private ToolStripMenuItem? trayShowMenuItem;
        private ToolStripMenuItem? trayConnectMenuItem;
        private ToolStripMenuItem? trayOpenMenuItem;
        private ToolStripMenuItem? trayDisconnectMenuItem;
        private ToolStripMenuItem? trayExitMenuItem;

        //private ImageList? deviceStateImageList;
        private Bitmap? iconDefault;
        private Bitmap? iconReady;
        private Bitmap? iconConnected;
        private Bitmap? iconError;

        private Font? normalListFont;
        private Font? boldListFont;

        public FormMain()
        {
            InitializeComponent();
        }

        private async void FormMain_Load(object sender, EventArgs e)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + @"\updated.dat"))
            {
                File.Delete(Directory.GetCurrentDirectory() + @"\updated.dat");
                string updpath = Directory.GetCurrentDirectory()[..Directory.GetCurrentDirectory().LastIndexOf('\\')];
                File.Delete(updpath + @"\updater.exe");
                File.Delete(updpath + @"\btplayer.zip");
                Common.Utils.DeleteDirectory(updpath + @"\updater-temp");

                MessageBox.Show(this, Localization.UpdateCompletedCaption, Localization.DoneCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var update = Task.Run(() => CheckForUpdatesForInit());
                update.Wait();
            }

            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(System.Windows.Forms.Application.ExecutablePath);
            if (ver.FileVersion is not null)
            {
                Text = "BlueTooth audio Player - Client " + ver.FileVersion;
                label_Title.Text = "BlueTooth audio Player - Play audio via Bluetooth A2DP Sink(SNK) on Windows. [BTPlayer] ver " + ver.FileVersion;
            }
            else
            {
                Text = "BlueTooth audio Player - Client";
                label_Title.Text = "BlueTooth audio Player - Play audio via Bluetooth A2DP Sink(SNK) on Windows. [BTPlayer]";
            }

            normalListFont = new Font(dataGridView_Device.Font, FontStyle.Regular);
            boldListFont = new Font(dataGridView_Device.Font, FontStyle.Bold);

            iconDefault = CreateStatusIcon(Color.Gray);
            iconReady = CreateStatusIcon(Color.DodgerBlue);
            iconConnected = CreateStatusIcon(Color.SeaGreen);
            iconError = CreateStatusIcon(Color.Crimson);

            InitializeDeviceGrid();
            InitializeTrayIcon();
            LoadLastSelectedDevice();
            LoadAppSettings();

            SetConnectionState(Localization.ConnectionDisconectCaption);
            Log("Application started.");

            toolStripButton_Add.Text = Localization.AddDeviceMenuCaption;
            toolStripButton_Remove.Text = Localization.RemoveDiviceMenuCaption;
            toolStripButton_Refresh.Text = Localization.RefreshDeviceCaption;
            toolStripButton_Settings.Text = GetUiText("Settings", "設定");
            toolStripButton_Update.Text = Localization.UpdateMenuCaption;
            toolStripButton_About.Text = Localization.AboutMenuCaption;
            toolStripButton_Exit.Text = Localization.ExitCaption;
            label_DeviceList.Text = Localization.DeviceListCaption;
            label_Log.Text = Localization.LogCaption;

            if (!await EnsureBluetoothAvailableAsync())
            {
                return;
            }

            UpdateOpenButtonState();
            UpdateConnectButtonState();
            UpdateTrayMenuState();
            UpdateTrayTooltip();
            UpdateActionButtonStyle();

            StartDeviceWatcher();

            Log("Device watcher started.");
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveLastSelectedDevice();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            StopDeviceWatcher(false);

            foreach (var closeConfirmation in pendingCloseConfirmations.Values)
            {
                closeConfirmation.Cancel();
                closeConfirmation.Dispose();
            }

            pendingCloseConfirmations.Clear();

            foreach (var pair in audioPlaybackConnections)
            {
                pair.Value.StateChanged -= AudioPlaybackConnection_ConnectionStateChanged;
                pair.Value.Dispose();
            }

            audioPlaybackConnections.Clear();
            currentOpenedDeviceId = null;

            normalListFont?.Dispose();
            boldListFont?.Dispose();
            components?.Dispose();
        }

        private async Task<bool> EnsureBluetoothAvailableAsync()
        {
            try
            {
                var radios = await Radio.GetRadiosAsync();
                var bluetoothRadio = radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);

                if (bluetoothRadio == null)
                {
                    MessageBox.Show(
                        Localization.NotBluetooth,
                        "BlueTooth audio Player",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    Log("Bluetooth radio not found.");
                    Close();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Localization.BluetoothException, ex.Message),
                    "BlueTooth audio Player",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Log("Bluetooth availability check failed: " + ex.Message);
                Close();
                return false;
            }
        }

        private void EnsureDeviceSelection()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(EnsureDeviceSelection));
                return;
            }

            if (dataGridView_Device.Rows.Count == 0)
            {
                UpdateUiFromSelection();
                return;
            }

            suppressSelectionPersistence = true;

            try
            {
                dataGridView_Device.ClearSelection();

                DataGridViewRow? targetRow = null;

                // 1. Open中デバイスを最優先
                if (!string.IsNullOrWhiteSpace(currentOpenedDeviceId))
                {
                    foreach (DataGridViewRow row in dataGridView_Device.Rows)
                    {
                        if (row.Tag is DeviceInformation info && info.Id == currentOpenedDeviceId)
                        {
                            targetRow = row;
                            break;
                        }
                    }
                }

                // 2. Connect済みデバイス
                if (targetRow == null && audioPlaybackConnections.Count > 0)
                {
                    string? connectedDeviceId = audioPlaybackConnections.Keys.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(connectedDeviceId))
                    {
                        foreach (DataGridViewRow row in dataGridView_Device.Rows)
                        {
                            if (row.Tag is DeviceInformation info && info.Id == connectedDeviceId)
                            {
                                targetRow = row;
                                break;
                            }
                        }
                    }
                }

                // 3. 起動時復元対象を最優先
                if (targetRow == null && !string.IsNullOrWhiteSpace(pendingRestoreDeviceId))
                {
                    foreach (DataGridViewRow row in dataGridView_Device.Rows)
                    {
                        if (row.Tag is DeviceInformation info && info.Id == pendingRestoreDeviceId)
                        {
                            targetRow = row;
                            pendingRestoreDeviceId = null;
                            break;
                        }
                    }
                }

                // 4. 通常の前回選択
                if (targetRow == null && !string.IsNullOrWhiteSpace(lastSelectedDeviceId))
                {
                    foreach (DataGridViewRow row in dataGridView_Device.Rows)
                    {
                        if (row.Tag is DeviceInformation info && info.Id == lastSelectedDeviceId)
                        {
                            targetRow = row;
                            break;
                        }
                    }
                }

                // 5. 見つからなければ先頭
                targetRow ??= dataGridView_Device.Rows.Count > 0 ? dataGridView_Device.Rows[0] : null;

                if (targetRow != null)
                {
                    targetRow.Selected = true;
                    dataGridView_Device.CurrentCell = targetRow.Cells["colName"];

                    if (targetRow.Tag is DeviceInformation selectedInfo)
                    {
                        lastSelectedDeviceId = selectedInfo.Id;
                    }
                }
            }
            finally
            {
                suppressSelectionPersistence = false;
            }

            UpdateUiFromSelection();
        }

        private async Task AddBluetoothDeviceAsync()
        {
            try
            {
                Log("Opening Windows Bluetooth settings...");
                await Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth"));
            }
            catch (Exception ex)
            {
                Log("Failed to open Bluetooth settings: " + ex.Message);
                MessageBox.Show(
                    string.Format(Localization.NotOpenBluetoothSettings, ex.Message),
                    "BlueTooth audio Player",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task RemoveSelectedBluetoothDeviceAsync()
        {
            var selectedDevice = GetSelectedDeviceInformation();
            if (selectedDevice == null)
            {
                MessageBox.Show(Localization.BluetoothDeleteDevice);
                return;
            }

            if (audioPlaybackConnections.ContainsKey(selectedDevice.Id) ||
                currentOpenedDeviceId == selectedDevice.Id)
            {
                MessageBox.Show(Localization.BluetoothAlreadyDeviceDelete);
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(selectedDevice.Name)
                ? Localization.NoNameCaption
                : selectedDevice.Name;

            var confirm = MessageBox.Show(
                string.Format(Localization.BluetoothDeviceDeleteConfirm, $"\"{displayName}\""),
                //$"\"{displayName}\" を削除しますか？",
                "BlueTooth audio Player",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Log("Removing device: " + displayName);

                var result = await selectedDevice.Pairing.UnpairAsync();

                Log($"Unpair result: {result.Status}");

                if (result.Status == DeviceUnpairingResultStatus.Unpaired ||
                    result.Status == DeviceUnpairingResultStatus.AlreadyUnpaired)
                {
                    SetConnectionState(Localization.ConnectionDisconectCaption);
                    RefreshDeviceList();
                }
                else
                {
                    MessageBox.Show(
                        string.Format(Localization.BluetoothDeviceDeleteFailedS, result.Status),
                        "BlueTooth audio Player",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Log("Remove failed: " + ex.Message);
                MessageBox.Show(
                    string.Format(Localization.BluetoothDeviceDeleteFailed, ex.Message),
                    "BlueTooth audio Player",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            AdjustDeviceGridColumnWidths();

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();

                if (trayIcon != null)
                {
                    string connectedName = GetCurrentConnectedDeviceName();

                    trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                    trayIcon.BalloonTipTitle = "BlueTooth audio Player";
                    trayIcon.BalloonTipText = string.IsNullOrWhiteSpace(connectedName)
                        ? Localization.TasktrayMinimized
                        : string.Format(Localization.TasktrayMinimizedwConnected, connectedName);
                    trayIcon.ShowBalloonTip(1000);
                }

                Log("Window minimized to tray.");
            }
        }

        private static Bitmap CreateStatusIcon(Color color)
        {
            const int size = 48;
            const int circleSize = 20;
            const int offset = (size - circleSize) / 2;

            Bitmap bmp = new(size, size);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.Clear(Color.Transparent);

                using (SolidBrush brush = new(color))
                {
                    g.FillEllipse(brush, offset, offset, circleSize, circleSize);
                }

                using (Pen pen = new(Color.FromArgb(90, 30, 30, 30), 1.2f))
                {
                    g.DrawEllipse(pen, offset, offset, circleSize, circleSize);
                }
            }

            return bmp;
        }

        private void InitializeDeviceGrid()
        {
            dataGridView_Device.AutoGenerateColumns = false;
            dataGridView_Device.Columns.Clear();

            dataGridView_Device.MultiSelect = false;
            dataGridView_Device.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dataGridView_Device.EnableHeadersVisualStyles = false;
            dataGridView_Device.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
            dataGridView_Device.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 60, 60);
            dataGridView_Device.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 247, 250);
            dataGridView_Device.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(60, 60, 60);
            dataGridView_Device.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic UI", 9.5F, FontStyle.Bold);
            dataGridView_Device.ColumnHeadersHeight = 38;
            dataGridView_Device.RowHeadersVisible = false;
            dataGridView_Device.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView_Device.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            dataGridView_Device.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 241, 252);
            dataGridView_Device.DefaultCellStyle.SelectionForeColor = Color.Black;
            dataGridView_Device.DefaultCellStyle.BackColor = Color.White;
            dataGridView_Device.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView_Device.DefaultCellStyle.Font = new Font("Yu Gothic UI", 9.5F, FontStyle.Regular);

            dataGridView_Device.GridColor = Color.FromArgb(235, 238, 242);
            dataGridView_Device.RowTemplate.Height = 34;
            dataGridView_Device.ScrollBars = ScrollBars.Both;

            var colIcon = new DataGridViewImageColumn
            {
                Name = "colIcon",
                HeaderText = "",
                Width = 34,
                MinimumWidth = 34,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colName = new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = Localization.DeviceName,
                Width = 220,
                MinimumWidth = 180,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colState = new DataGridViewTextBoxColumn
            {
                Name = "colState",
                HeaderText = Localization.StateCaption,
                Width = 120,
                MinimumWidth = 100,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colId = new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "ID",
                Width = 500,
                MinimumWidth = 300,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            dataGridView_Device.Columns.AddRange(colIcon, colName, colState, colId);
            AdjustDeviceGridColumnWidths();
        }

        /// <summary>
        /// タスクトレイメニュー
        /// </summary>
        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            trayShowMenuItem = new ToolStripMenuItem(Localization.ShowCaption, null, (_, __) => RestoreFromTray());
            trayConnectMenuItem = new ToolStripMenuItem(Localization.ConnectCaption, null, async (_, __) => await ConnectSelectedDeviceAsync())
            {
                ForeColor = Color.SeaGreen,
            };
            trayOpenMenuItem = new ToolStripMenuItem(Localization.OpenCaption, null, async (_, __) => await OpenSelectedDeviceAsync())
            {
                ForeColor = Color.DodgerBlue,
            };
            trayDisconnectMenuItem = new ToolStripMenuItem(Localization.DisconnectCaption, null, async (_, __) => await DisconnectSelectedDeviceAsync())
            {
                ForeColor = Color.Crimson,
            };
            trayExitMenuItem = new ToolStripMenuItem(Localization.ExitCaption, null, (_, __) => Close())
            {
                ForeColor = Color.Red,
            };

            trayMenu.Items.Add(trayShowMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayConnectMenuItem);
            trayMenu.Items.Add(trayOpenMenuItem);
            trayMenu.Items.Add(trayDisconnectMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayExitMenuItem);

            trayIcon = new NotifyIcon
            {
                Text = "BlueTooth audio Player",
                Icon = Icon,
                Visible = true,
                ContextMenuStrip = trayMenu
            };

            trayIcon.DoubleClick += (_, __) => RestoreFromTray();

            UpdateTrayMenuState();
            UpdateTrayTooltip();
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void SetConnectionState(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetConnectionState(text)));
                return;
            }

            label_Status.Text = string.Format(Localization.StatusCaption, text);

            if (text.Contains(Localization.ConnectionDisconectCaption, StringComparison.OrdinalIgnoreCase))
            {
                label_Status.ForeColor = Color.Crimson;
            }
            else if (text.Contains(Localization.ConnectionConnectCaption, StringComparison.OrdinalIgnoreCase))
            {
                label_Status.ForeColor = Color.SeaGreen;
            }
            else if (text.Contains(Localization.ConnectionReadyCaption, StringComparison.OrdinalIgnoreCase))
            {
                label_Status.ForeColor = Color.DodgerBlue;
            }
            else if (text.Contains(Localization.ConnectionFailedCaption, StringComparison.OrdinalIgnoreCase) || text.Contains(Localization.ErrorCaption, StringComparison.OrdinalIgnoreCase))
            {
                label_Status.ForeColor = Color.Crimson;
            }
            else
            {
                label_Status.ForeColor = Color.FromArgb(25, 25, 25);
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(message)));
                return;
            }

            string line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            textBox_Log.AppendText(line);
        }

        /// <summary>
        /// 行の見た目更新
        /// </summary>
        private void RefreshDeviceRowAppearance()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshDeviceRowAppearance));
                return;
            }

            bool anyDeviceConnected = audioPlaybackConnections.Count > 0;

            foreach (DataGridViewRow row in dataGridView_Device.Rows)
            {
                if (row.Tag is not DeviceInformation info)
                {
                    continue;
                }

                string state = Convert.ToString(row.Cells["colState"].Value) ?? string.Empty;
                bool isOpened = currentOpenedDeviceId == info.Id;
                bool isReady = audioPlaybackConnections.ContainsKey(info.Id);

                row.DefaultCellStyle.Font = normalListFont ?? dataGridView_Device.Font;
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = Color.Black;

                if (isOpened || state == Localization.ConnectionConnectCaption)
                {
                    row.DefaultCellStyle.BackColor = Color.Honeydew;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    row.DefaultCellStyle.Font = boldListFont ?? dataGridView_Device.Font;
                    row.Cells["colIcon"].Value = iconConnected;
                }
                else if (isReady || state == Localization.ConnectionReadyCaption)
                {
                    row.DefaultCellStyle.BackColor = Color.AliceBlue;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    row.Cells["colIcon"].Value = iconReady;
                }
                else if (state == Localization.ErrorCaption || state.Contains(Localization.ConnectionFailedCaption, StringComparison.OrdinalIgnoreCase))
                {
                    row.DefaultCellStyle.BackColor = Color.MistyRose;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    row.Cells["colIcon"].Value = iconError;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.ForeColor = anyDeviceConnected
                        ? Color.FromArgb(140, 140, 140)
                        : Color.Black;
                    row.Cells["colIcon"].Value = iconDefault;
                }
            }
        }

        /// <summary>
        /// 失敗表示リセット
        /// </summary>
        private void ResetInactiveFailureStates()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ResetInactiveFailureStates));
                return;
            }

            foreach (DataGridViewRow row in dataGridView_Device.Rows)
            {
                if (row.Tag is not DeviceInformation info)
                {
                    continue;
                }

                string state = Convert.ToString(row.Cells["colState"].Value) ?? string.Empty;

                bool isFailureState =
                    state.Equals(Localization.OpenFailedCaption, StringComparison.OrdinalIgnoreCase) ||
                    state.StartsWith(Localization.OpenFailedCaption + ":", StringComparison.OrdinalIgnoreCase) ||
                    state.Equals(Localization.ErrorCaption, StringComparison.OrdinalIgnoreCase);

                if (!isFailureState)
                {
                    continue;
                }

                if (audioPlaybackConnections.ContainsKey(info.Id))
                {
                    continue;
                }

                row.Cells["colState"].Value = Localization.DetectedCaption;
            }

            RefreshDeviceRowAppearance();
        }

        /// <summary>
        /// 状態更新 helper
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="state"></param>
        private void SetDeviceRowState(string deviceId, string state)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetDeviceRowState(deviceId, state)));
                return;
            }

            foreach (DataGridViewRow row in dataGridView_Device.Rows)
            {
                if (row.Tag is DeviceInformation info && info.Id == deviceId)
                {
                    row.Cells["colState"].Value = state;
                    break;
                }
            }

            RefreshDeviceRowAppearance();
        }

        // Update系ヘルパー

        private void UpdateOpenButtonState()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateOpenButtonState));
                return;
            }

            var selectedDeviceId = GetSelectedDeviceId();

            if (selectedDeviceId == null)
            {
                button_Open.Enabled = false;
                button_Open.Text = Localization.OpenCaption;
                return;
            }

            if (!audioPlaybackConnections.ContainsKey(selectedDeviceId))
            {
                button_Open.Enabled = false;
                button_Open.Text = Localization.OpenCaption;
                return;
            }

            button_Open.Enabled = true;
            button_Open.Text = currentOpenedDeviceId == selectedDeviceId ? Localization.DisconnectCaption : Localization.OpenCaption;
        }

        private void UpdateConnectButtonState()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateConnectButtonState));
                return;
            }

            var selectedDeviceId = GetSelectedDeviceId();

            if (selectedDeviceId == null)
            {
                button_Connect.Enabled = false;
                return;
            }

            bool anyDeviceConnected = audioPlaybackConnections.Count > 0;
            bool selectedAlreadyConnected = audioPlaybackConnections.ContainsKey(selectedDeviceId);

            // すでに何か1台でも Connect 済みなら、他は Connect 不可
            // ただし選択中のデバイス自身がすでに Connect 済みなら当然 Connect 不可
            button_Connect.Enabled = !anyDeviceConnected && !selectedAlreadyConnected;
            button_Connect.Text = Localization.ConnectCaption;
        }

        private void UpdateTrayMenuState()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateTrayMenuState));
                return;
            }

            var selectedDeviceId = GetSelectedDeviceId();
            bool hasSelection = selectedDeviceId != null;
            bool hasConnection = hasSelection && selectedDeviceId != null && audioPlaybackConnections.ContainsKey(selectedDeviceId);
            bool isOpened = hasSelection && selectedDeviceId != null && currentOpenedDeviceId == selectedDeviceId;
            bool anyDeviceConnected = audioPlaybackConnections.Count > 0;

            string selectedDeviceName = TrimMenuDeviceName(GetSelectedDeviceDisplayName());
            string connectedDeviceName = TrimMenuDeviceName(GetCurrentConnectedDeviceName());

            string connectSuffix = string.IsNullOrWhiteSpace(selectedDeviceName)
                ? string.Empty
                : $"    [{selectedDeviceName}]";

            string openSuffix = string.IsNullOrWhiteSpace(selectedDeviceName)
                ? string.Empty
                : $"    [{selectedDeviceName}]";

            string disconnectSuffix = string.IsNullOrWhiteSpace(connectedDeviceName)
                ? string.Empty
                : $"    [{connectedDeviceName}]";

            if (trayConnectMenuItem != null)
            {
                trayConnectMenuItem.Text = Localization.ConnectCaption + connectSuffix;
                trayConnectMenuItem.Enabled = hasSelection && !anyDeviceConnected;
            }

            if (trayOpenMenuItem != null)
            {
                trayOpenMenuItem.Text = Localization.OpenCaption + openSuffix;
                trayOpenMenuItem.Enabled = hasConnection && !isOpened;
            }

            if (trayDisconnectMenuItem != null)
            {
                trayDisconnectMenuItem.Text = Localization.DisconnectCaption + disconnectSuffix;
                trayDisconnectMenuItem.Enabled = !string.IsNullOrWhiteSpace(currentOpenedDeviceId);
            }
        }

        private void UpdateTrayTooltip()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateTrayTooltip));
                return;
            }

            if (trayIcon == null)
            {
                return;
            }

            string tooltip = "BlueTooth audio Player";
            string connectedName = GetCurrentConnectedDeviceName();

            if (!string.IsNullOrWhiteSpace(connectedName))
            {
                tooltip = $"BlueTooth audio Player - " + string.Format(Localization.ConnectedNameCaption, connectedName);
            }
            else if (!string.IsNullOrWhiteSpace(lastSelectedDeviceId) &&
                     devices.TryGetValue(lastSelectedDeviceId, out var selectedDevice))
            {
                string deviceName = string.IsNullOrWhiteSpace(selectedDevice.Name)
                    ? Localization.NoNameCaption
                    : selectedDevice.Name;

                tooltip = $"BlueTooth audio Player - " + string.Format(Localization.SelectedNameCaption, deviceName);
            }

            if (tooltip.Length > 63)
            {
                tooltip = tooltip.Substring(0, 63);
            }

            trayIcon.Text = tooltip;
        }

        private void UpdateActionButtonStyle()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateActionButtonStyle));
                return;
            }

            var selectedDeviceId = GetSelectedDeviceId();

            bool selectedIsPrepared =
                selectedDeviceId != null &&
                audioPlaybackConnections.ContainsKey(selectedDeviceId);

            // 基本色
            Color activeBlue = Color.FromArgb(0, 120, 215);
            Color darkBlack = Color.FromArgb(32, 32, 32);
            Color disabledGray = Color.FromArgb(160, 160, 160);
            Color disabledText = Color.White;

            // まず初期化
            button_Connect.ForeColor = Color.White;
            button_Open.ForeColor = Color.White;

            // 選択中デバイスが Connect 済みなら Open 側を主操作に
            if (selectedIsPrepared)
            {
                button_Connect.BackColor = darkBlack;
                button_Open.BackColor = activeBlue;
            }
            else
            {
                button_Connect.BackColor = activeBlue;
                button_Open.BackColor = darkBlack;
            }

            // 無効ボタンは色を落とす
            if (!button_Connect.Enabled)
            {
                button_Connect.BackColor = disabledGray;
                button_Connect.ForeColor = disabledText;
            }

            if (!button_Open.Enabled)
            {
                button_Open.BackColor = disabledGray;
                button_Open.ForeColor = disabledText;
            }
        }

        private void UpdateUiFromSelection()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateUiFromSelection));
                return;
            }

            string? selectedId = GetSelectedDeviceId();

            if (!suppressSelectionPersistence)
            {
                lastSelectedDeviceId = selectedId ?? lastSelectedDeviceId;
                SaveLastSelectedDevice();
            }

            UpdateOpenButtonState();
            UpdateConnectButtonState();
            UpdateTrayMenuState();
            RefreshDeviceRowAppearance();
            UpdateTrayTooltip();
            UpdateActionButtonStyle();
        }

        // Get系ヘルパー

        private string? GetSelectedDeviceId()
        {
            if (dataGridView_Device.SelectedRows.Count == 0)
            {
                return null;
            }

            if (dataGridView_Device.SelectedRows[0].Tag is DeviceInformation deviceInfo)
            {
                return deviceInfo.Id;
            }

            return null;
        }

        private DeviceInformation? GetSelectedDeviceInformation()
        {
            if (dataGridView_Device.SelectedRows.Count == 0)
            {
                return null;
            }

            return dataGridView_Device.SelectedRows[0].Tag as DeviceInformation;
        }

        private static string GetSettingsFilePath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BTPlayer");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, SettingsFileName);
        }

        private static string GetAppSettingsFilePath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BTPlayer");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, AppSettingsFileName);
        }

        private static string GetUiText(string english, string japanese)
        {
            return Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase)
                ? japanese
                : english;
        }

        private string GetCurrentConnectedDeviceName()
        {
            if (!string.IsNullOrWhiteSpace(currentOpenedDeviceId) &&
                devices.TryGetValue(currentOpenedDeviceId, out var openedDevice))
            {
                return string.IsNullOrWhiteSpace(openedDevice.Name)
                    ? Localization.NoNameCaption
                    : openedDevice.Name;
            }

            return string.Empty;
        }

        private string GetSelectedDeviceDisplayName()
        {
            string? selectedDeviceId = GetSelectedDeviceId();
            if (string.IsNullOrWhiteSpace(selectedDeviceId))
            {
                return string.Empty;
            }

            return GetDeviceDisplayName(selectedDeviceId);
        }

        private void SaveLastSelectedDevice()
        {
            try
            {
                string? selectedDeviceId = GetSelectedDeviceId();
                if (!string.IsNullOrWhiteSpace(selectedDeviceId))
                {
                    lastSelectedDeviceId = selectedDeviceId;
                }

                File.WriteAllText(GetSettingsFilePath(), lastSelectedDeviceId ?? string.Empty);
            }
            catch (Exception ex)
            {
                Log("Failed to save last selected device: " + ex.Message);
            }
        }

        private void LoadLastSelectedDevice()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    string value = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        lastSelectedDeviceId = value;
                        pendingRestoreDeviceId = value;
                        Log("Loaded last selected device.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Failed to load last selected device: " + ex.Message);
            }
        }

        /// <summary>
        /// 選択復元
        /// </summary>
        private void TryRestoreLastSelection()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(TryRestoreLastSelection));
                return;
            }

            EnsureDeviceSelection();
        }

        private async Task ConnectSelectedDeviceAsync()
        {
            var selectedDeviceId = GetSelectedDeviceId();
            if (selectedDeviceId == null)
            {
                MessageBox.Show(Localization.DeviceSelectionCaption);
                return;
            }

            await audioConnectionLock.WaitAsync();
            try
            {
                // すでに別のデバイスが Connect 済みなら拒否
                if (audioPlaybackConnections.Count > 0 && !audioPlaybackConnections.ContainsKey(selectedDeviceId))
                {
                    MessageBox.Show(Localization.DeviceAlreadyConnectCaption);
                    Log("Connect blocked: another device is already connected.");
                    return;
                }

                if (audioPlaybackConnections.ContainsKey(selectedDeviceId))
                {
                    SetConnectionState(Localization.ConnectionReadyCaption);
                    SetDeviceRowState(selectedDeviceId, Localization.ConnectionReadyCaption);
                    Log("Already connected: " + GetDeviceDisplayName(selectedDeviceId));

                    UpdateOpenButtonState();
                    UpdateConnectButtonState();
                    UpdateTrayMenuState();
                    RefreshDeviceRowAppearance();
                    UpdateTrayTooltip();
                    UpdateActionButtonStyle();
                    return;
                }

                var playbackConnection = AudioPlaybackConnection.TryCreateFromId(selectedDeviceId);
                if (playbackConnection == null)
                {
                    MessageBox.Show(Localization.AudioPlaybackConnectionError);
                    Log("TryCreateFromId failed.");
                    return;
                }

                playbackConnection.StateChanged += AudioPlaybackConnection_ConnectionStateChanged;
                audioPlaybackConnections[selectedDeviceId] = playbackConnection;

                try
                {
                    PauseDeviceWatcherForAudioConnection();
                    Log("Connecting: " + GetDeviceDisplayName(selectedDeviceId));
                    await playbackConnection.StartAsync();

                    SetConnectionState(Localization.ConnectionReadyCaption);
                    SetDeviceRowState(selectedDeviceId, Localization.ConnectionReadyCaption);
                    ResetInactiveFailureStates();
                    Log("Connect succeeded: " + GetDeviceDisplayName(selectedDeviceId));

                    if (autoPlayOnDeviceConnected)
                    {
                        await OpenConnectedDeviceAsync(selectedDeviceId, playbackConnection, showErrorMessage: false);
                    }
                }
                catch (Exception ex)
                {
                    playbackConnection.StateChanged -= AudioPlaybackConnection_ConnectionStateChanged;
                    playbackConnection.Dispose();
                    audioPlaybackConnections.Remove(selectedDeviceId);

                    SetConnectionState(Localization.ConnectionDisconectCaption);
                    SetDeviceRowState(selectedDeviceId, Localization.ErrorCaption);
                    Log("Connect failed: " + ex.Message);
                    MessageBox.Show(string.Format(Localization.StartAsyncError, ex.Message));
                    ResumeDeviceWatcherIfIdle();
                }

                UpdateOpenButtonState();
                UpdateConnectButtonState();
                UpdateTrayMenuState();
                RefreshDeviceRowAppearance();
                UpdateTrayTooltip();
                UpdateActionButtonStyle();
            }
            finally
            {
                audioConnectionLock.Release();
            }
        }

        private void LoadAppSettings()
        {
            try
            {
                string path = GetAppSettingsFilePath();
                if (!File.Exists(path))
                {
                    return;
                }

                foreach (string line in File.ReadAllLines(path))
                {
                    string trimmedLine = line.Trim();
                    if (trimmedLine.Length == 0 || trimmedLine.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    string key = trimmedLine[..separatorIndex].Trim();
                    string value = trimmedLine[(separatorIndex + 1)..].Trim();

                    if (key.Equals("AutoPlayOnDeviceConnected", StringComparison.OrdinalIgnoreCase) &&
                        bool.TryParse(value, out bool parsedValue))
                    {
                        autoPlayOnDeviceConnected = parsedValue;
                    }
                }

                Log("Loaded app settings.");
            }
            catch (Exception ex)
            {
                Log("Failed to load app settings: " + ex.Message);
            }
        }

        private void SaveAppSettings()
        {
            try
            {
                string settingsText =
                    "AutoPlayOnDeviceConnected=" + autoPlayOnDeviceConnected + Environment.NewLine;

                File.WriteAllText(GetAppSettingsFilePath(), settingsText);
            }
            catch (Exception ex)
            {
                Log("Failed to save app settings: " + ex.Message);
            }
        }

        private void ClearOtherConnectedStates(string keepDeviceId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ClearOtherConnectedStates(keepDeviceId)));
                return;
            }

            foreach (DataGridViewRow row in dataGridView_Device.Rows)
            {
                if (row.Tag is not DeviceInformation info)
                {
                    continue;
                }

                if (info.Id == keepDeviceId)
                {
                    continue;
                }

                string state = Convert.ToString(row.Cells["colState"].Value) ?? string.Empty;
                if (state == Localization.ConnectionConnectCaption)
                {
                    if (audioPlaybackConnections.ContainsKey(info.Id))
                    {
                        SetDeviceRowState(info.Id, Localization.ConnectionReadyCaption);
                    }
                    else
                    {
                        SetDeviceRowState(info.Id, Localization.ConnectionDisconectCaption);
                    }
                }
            }
        }

        private async Task OpenConnectedDeviceAsync(
            string selectedDeviceId,
            AudioPlaybackConnection selectedConnection,
            bool showErrorMessage)
        {
            try
            {
                PauseDeviceWatcherForAudioConnection();
                Log("Opening: " + GetDeviceDisplayName(selectedDeviceId));
                var result = await selectedConnection.OpenAsync();

                if (result.Status == AudioPlaybackConnectionOpenResultStatus.Success)
                {
                    // 他のデバイスが誤って Connected 表示のまま残らないように整理
                    ClearOtherConnectedStates(selectedDeviceId);

                    currentOpenedDeviceId = selectedDeviceId;
                    SetConnectionState(Localization.ConnectionConnectCaption);
                    SetDeviceRowState(selectedDeviceId, Localization.ConnectionConnectCaption);
                    ResetInactiveFailureStates();
                    Log("Open succeeded: " + GetDeviceDisplayName(selectedDeviceId));
                }
                else
                {
                    Log("Open failed: " + result.Status);

                    // 失敗した接続は残さず破棄
                    CancelPendingCloseConfirmation(selectedDeviceId);
                    selectedConnection.StateChanged -= AudioPlaybackConnection_ConnectionStateChanged;
                    selectedConnection.Dispose();
                    audioPlaybackConnections.Remove(selectedDeviceId);

                    if (currentOpenedDeviceId == selectedDeviceId)
                    {
                        currentOpenedDeviceId = null;
                    }

                    SetConnectionState(Localization.OpenFailedCaption + ": " + result.Status);
                    SetDeviceRowState(selectedDeviceId, Localization.OpenFailedCaption);
                    ResumeDeviceWatcherIfIdle();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    CancelPendingCloseConfirmation(selectedDeviceId);
                    selectedConnection.StateChanged -= AudioPlaybackConnection_ConnectionStateChanged;
                    selectedConnection.Dispose();
                    audioPlaybackConnections.Remove(selectedDeviceId);
                }
                catch
                {
                }

                if (currentOpenedDeviceId == selectedDeviceId)
                {
                    currentOpenedDeviceId = null;
                }

                SetConnectionState(Localization.ConnectionDisconectCaption);
                SetDeviceRowState(selectedDeviceId, Localization.ErrorCaption);
                Log("Open failed: " + ex.Message);

                if (showErrorMessage)
                {
                    MessageBox.Show(string.Format(Localization.OpenAsyncError, ex.Message));
                }

                ResumeDeviceWatcherIfIdle();
            }
        }

        /// <summary>
        /// デバイスオープン
        /// </summary>
        /// <returns>なし</returns>
        private async Task OpenSelectedDeviceAsync()
        {
            var selectedDeviceId = GetSelectedDeviceId();
            if (selectedDeviceId == null)
            {
                MessageBox.Show(Localization.DeviceSelectionCaption);
                return;
            }

            await audioConnectionLock.WaitAsync();
            try
            {
                if (!audioPlaybackConnections.TryGetValue(selectedDeviceId, out var selectedConnection))
                {
                    MessageBox.Show(Localization.FirstConnectCaption);
                    return;
                }

                await OpenConnectedDeviceAsync(selectedDeviceId, selectedConnection, showErrorMessage: true);

                UpdateOpenButtonState();
                UpdateConnectButtonState();
                UpdateTrayMenuState();
                RefreshDeviceRowAppearance();
                UpdateTrayTooltip();
                UpdateActionButtonStyle();
            }
            finally
            {
                audioConnectionLock.Release();
            }
        }

        private async Task DisconnectSelectedDeviceAsync()
        {
            var selectedDeviceId = GetSelectedDeviceId();
            if (selectedDeviceId == null)
            {
                MessageBox.Show(Localization.DeviceSelectionCaption);
                return;
            }

            await audioConnectionLock.WaitAsync();
            try
            {
                if (!audioPlaybackConnections.TryGetValue(selectedDeviceId, out var selectedConnection))
                {
                    SetConnectionState(Localization.ConnectionDisconectCaption);
                    SetDeviceRowState(selectedDeviceId, Localization.ConnectionDisconectCaption);

                    UpdateOpenButtonState();
                    UpdateConnectButtonState();
                    UpdateTrayMenuState();
                    RefreshDeviceRowAppearance();
                    UpdateTrayTooltip();
                    UpdateActionButtonStyle();
                    return;
                }

                try
                {
                    Log("Disconnecting: " + GetDeviceDisplayName(selectedDeviceId));

                    CancelPendingCloseConfirmation(selectedDeviceId);
                    selectedConnection.StateChanged -= AudioPlaybackConnection_ConnectionStateChanged;
                    selectedConnection.Dispose();
                    audioPlaybackConnections.Remove(selectedDeviceId);

                    if (currentOpenedDeviceId == selectedDeviceId)
                    {
                        currentOpenedDeviceId = null;
                    }

                    SetConnectionState(Localization.ConnectionDisconectCaption);
                    SetDeviceRowState(selectedDeviceId, Localization.ConnectionDisconectCaption);
                    Log("Disconnect succeeded: " + GetDeviceDisplayName(selectedDeviceId));
                }
                catch (Exception ex)
                {
                    SetConnectionState(Localization.ErrorCaption);
                    SetDeviceRowState(selectedDeviceId, Localization.ErrorCaption);
                    Log("Disconnect failed: " + ex.Message);
                    MessageBox.Show(string.Format(Localization.DisconnectError, ex.Message));
                }

                UpdateOpenButtonState();
                UpdateConnectButtonState();
                UpdateTrayMenuState();
                RefreshDeviceRowAppearance();
                UpdateTrayTooltip();
                UpdateActionButtonStyle();
                ResumeDeviceWatcherIfIdle();

                await Task.CompletedTask;
            }
            finally
            {
                audioConnectionLock.Release();
            }
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => DeviceWatcher_Added(sender, deviceInfo)));
                return;
            }

            if (devices.ContainsKey(deviceInfo.Id))
            {
                return;
            }

            devices[deviceInfo.Id] = deviceInfo;

            string name = string.IsNullOrWhiteSpace(deviceInfo.Name)
                ? Localization.NoNameCaption
                : deviceInfo.Name;

            string initialState;
            Bitmap? initialIcon;

            if (currentOpenedDeviceId == deviceInfo.Id)
            {
                initialState = Localization.ConnectionConnectCaption;
                initialIcon = iconConnected;
            }
            else if (audioPlaybackConnections.ContainsKey(deviceInfo.Id))
            {
                initialState = Localization.ConnectionReadyCaption;
                initialIcon = iconReady;
            }
            else
            {
                initialState = Localization.DetectedCaption;
                initialIcon = iconDefault;
            }

            int rowIndex = dataGridView_Device.Rows.Add(
                initialIcon!,
                name,
                initialState,
                deviceInfo.Id);

            dataGridView_Device.Rows[rowIndex].Tag = deviceInfo;

            Log("Device detected: " + name);
            EnsureDeviceSelection();
            RefreshDeviceRowAppearance();
            UpdateTrayTooltip();
            AdjustDeviceGridColumnWidths();
            UpdateActionButtonStyle();
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => DeviceWatcher_Updated(sender, update)));
                return;
            }

            if (!devices.TryGetValue(update.Id, out var existingDevice))
            {
                return;
            }

            existingDevice.Update(update);

            foreach (DataGridViewRow row in dataGridView_Device.Rows)
            {
                if (row.Tag is DeviceInformation info && info.Id == update.Id)
                {
                    row.Cells["colName"].Value = string.IsNullOrWhiteSpace(existingDevice.Name)
                        ? Localization.NoNameCaption
                        : existingDevice.Name;

                    row.Cells["colId"].Value = existingDevice.Id;
                    break;
                }
            }

            UpdateTrayTooltip();
            AdjustDeviceGridColumnWidths();
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => DeviceWatcher_Removed(sender, update)));
                return;
            }

            if (audioPlaybackConnections.ContainsKey(update.Id) ||
                currentOpenedDeviceId == update.Id)
            {
                Log("Active audio device removal reported by watcher; keeping playback connection.");
                return;
            }

            devices.Remove(update.Id);

            DataGridViewRow? removeRow = null;
            foreach (DataGridViewRow row in dataGridView_Device.Rows)
            {
                if (row.Tag is DeviceInformation info && info.Id == update.Id)
                {
                    removeRow = row;
                    break;
                }
            }

            if (removeRow != null)
            {
                string removedName = Convert.ToString(removeRow.Cells["colName"].Value) ?? Localization.NoNameCaption;
                dataGridView_Device.Rows.Remove(removeRow);
                Log("Device removed: " + removedName);
            }

            if (audioPlaybackConnections.TryGetValue(update.Id, out var connection))
            {
                connection.StateChanged -= AudioPlaybackConnection_ConnectionStateChanged;
                connection.Dispose();
                audioPlaybackConnections.Remove(update.Id);
            }

            if (currentOpenedDeviceId == update.Id)
            {
                currentOpenedDeviceId = null;
                SetConnectionState(Localization.ConnectionDisconectCaption);
            }

            UpdateOpenButtonState();
            UpdateConnectButtonState();
            UpdateTrayMenuState();
            RefreshDeviceRowAppearance();
            UpdateTrayTooltip();
            UpdateActionButtonStyle();
            AdjustDeviceGridColumnWidths();
        }

        /// <summary>
        /// A2DP SNKで音声を流す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void AudioPlaybackConnection_ConnectionStateChanged(AudioPlaybackConnection sender, object args)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AudioPlaybackConnection_ConnectionStateChanged(sender, args)));
                return;
            }

            string? matchedDeviceId = null;

            foreach (var pair in audioPlaybackConnections)
            {
                if (ReferenceEquals(pair.Value, sender))
                {
                    matchedDeviceId = pair.Key;
                    break;
                }
            }

            if (sender.State == AudioPlaybackConnectionState.Opened)
            {
                if (matchedDeviceId != null)
                {
                    CancelPendingCloseConfirmation(matchedDeviceId);
                    ClearOtherConnectedStates(matchedDeviceId);

                    currentOpenedDeviceId = matchedDeviceId;
                    SetDeviceRowState(matchedDeviceId, Localization.ConnectionConnectCaption);
                    Log("State changed: Connected - " + GetDeviceDisplayName(matchedDeviceId));
                }

                SetConnectionState(Localization.ConnectionConnectCaption);
            }
            else if (sender.State == AudioPlaybackConnectionState.Closed)
            {
                if (matchedDeviceId != null)
                {
                    ScheduleCloseConfirmation(sender, matchedDeviceId);
                    Log("State changed: Closed pending confirmation - " + GetDeviceDisplayName(matchedDeviceId));
                }
                else
                {
                    SetConnectionState(Localization.ConnectionUnknownCaption);
                }
            }
            else
            {
                if (matchedDeviceId != null)
                {
                    SetDeviceRowState(matchedDeviceId, Localization.ConnectionUnknownCaption);
                    Log("State changed: Unknown - " + GetDeviceDisplayName(matchedDeviceId));
                }

                SetConnectionState(Localization.ConnectionUnknownCaption);
            }

            UpdateOpenButtonState();
            UpdateConnectButtonState();
            UpdateTrayMenuState();
            RefreshDeviceRowAppearance();
            UpdateTrayTooltip();
            UpdateActionButtonStyle();
            ResumeDeviceWatcherIfIdle();
        }

        private void ScheduleCloseConfirmation(AudioPlaybackConnection connection, string deviceId)
        {
            CancelPendingCloseConfirmation(deviceId);

            var cancellation = new CancellationTokenSource();
            pendingCloseConfirmations[deviceId] = cancellation;
            _ = ConfirmClosedStateAfterDelayAsync(connection, deviceId, cancellation);
        }

        private async Task ConfirmClosedStateAfterDelayAsync(
            AudioPlaybackConnection connection,
            string deviceId,
            CancellationTokenSource cancellation)
        {
            try
            {
                await Task.Delay(ClosedStateGracePeriodMs, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (IsDisposed)
            {
                cancellation.Dispose();
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ConfirmClosedState(connection, deviceId, cancellation)));
                return;
            }

            ConfirmClosedState(connection, deviceId, cancellation);
        }

        private void ConfirmClosedState(
            AudioPlaybackConnection connection,
            string deviceId,
            CancellationTokenSource cancellation)
        {
            if (!pendingCloseConfirmations.TryGetValue(deviceId, out var currentCancellation) ||
                !ReferenceEquals(currentCancellation, cancellation))
            {
                cancellation.Dispose();
                return;
            }

            pendingCloseConfirmations.Remove(deviceId);
            cancellation.Dispose();

            if (!audioPlaybackConnections.TryGetValue(deviceId, out var currentConnection) ||
                !ReferenceEquals(currentConnection, connection))
            {
                return;
            }

            if (connection.State != AudioPlaybackConnectionState.Closed)
            {
                return;
            }

            connection.StateChanged -= AudioPlaybackConnection_ConnectionStateChanged;
            audioPlaybackConnections.Remove(deviceId);
            connection.Dispose();

            if (currentOpenedDeviceId == deviceId)
            {
                currentOpenedDeviceId = null;
            }

            SetDeviceRowState(deviceId, Localization.ConnectionDisconectCaption);
            SetConnectionState(Localization.ConnectionDisconectCaption);
            Log("State changed: Disconnected - " + GetDeviceDisplayName(deviceId));

            UpdateOpenButtonState();
            UpdateConnectButtonState();
            UpdateTrayMenuState();
            RefreshDeviceRowAppearance();
            UpdateTrayTooltip();
            UpdateActionButtonStyle();
            ResumeDeviceWatcherIfIdle();
        }

        private void CancelPendingCloseConfirmation(string deviceId)
        {
            if (!pendingCloseConfirmations.Remove(deviceId, out var cancellation))
            {
                return;
            }

            cancellation.Cancel();
            cancellation.Dispose();
        }

        /// <summary>
        /// デバイス名を取得
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private string GetDeviceDisplayName(string deviceId)
        {
            if (devices.TryGetValue(deviceId, out var device))
            {
                return string.IsNullOrWhiteSpace(device.Name) ? Localization.NoNameCaption : device.Name;
            }

            return deviceId;
        }

        /// <summary>
        /// デバイス探索の開始
        /// </summary>
        private void StartDeviceWatcher()
        {
            if (audioPlaybackConnections.Count > 0)
            {
                Log("Device watcher start skipped during audio connection.");
                return;
            }

            StopDeviceWatcher(clearList: true);

            deviceWatcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Start();
        }

        /// <summary>
        /// デバイス探索の停止
        /// </summary>
        /// <param name="clearList"></param>
        private void StopDeviceWatcher(bool clearList)
        {
            if (deviceWatcher != null)
            {
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                deviceWatcher.Removed -= DeviceWatcher_Removed;

                if (deviceWatcher.Status == DeviceWatcherStatus.Started ||
                    deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    deviceWatcher.Stop();
                }

                deviceWatcher = null;
            }

            if (clearList)
            {
                devices.Clear();
                dataGridView_Device.Rows.Clear();
            }
        }

        private void PauseDeviceWatcherForAudioConnection()
        {
            if (deviceWatcher == null)
            {
                return;
            }

            StopDeviceWatcher(clearList: false);
            Log("Device watcher paused during audio connection.");
        }

        private void ResumeDeviceWatcherIfIdle()
        {
            if (audioPlaybackConnections.Count > 0 || deviceWatcher != null || IsDisposed)
            {
                return;
            }

            StartDeviceWatcher();
            Log("Device watcher resumed.");
        }

        /// <summary>
        /// デバイスリストの更新
        /// </summary>
        private void RefreshDeviceList()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshDeviceList));
                return;
            }

            if (audioPlaybackConnections.Count > 0)
            {
                Log("Refresh skipped during audio connection.");
                EnsureDeviceSelection();
                UpdateUiFromSelection();
                return;
            }

            // Connect / Open 中のデバイスがあれば最優先でそれを維持
            if (!string.IsNullOrWhiteSpace(currentOpenedDeviceId))
            {
                lastSelectedDeviceId = currentOpenedDeviceId;
            }
            else if (audioPlaybackConnections.Count > 0)
            {
                lastSelectedDeviceId = audioPlaybackConnections.Keys.FirstOrDefault() ?? lastSelectedDeviceId;
            }
            else
            {
                lastSelectedDeviceId = GetSelectedDeviceId() ?? lastSelectedDeviceId;
            }

            SaveLastSelectedDevice();

            Log("Refreshing device list...");
            StartDeviceWatcher();
            EnsureDeviceSelection();
            AdjustDeviceGridColumnWidths();
        }

        /// <summary>
        /// 最長の ID テキストに合わせて列幅を決める
        /// </summary>
        private void AdjustDeviceGridColumnWidths()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(AdjustDeviceGridColumnWidths));
                return;
            }

            if (dataGridView_Device.Columns.Count < 4)
            {
                return;
            }

            const int iconWidth = 34;
            const int nameWidth = 220;
            const int stateWidth = 120;
            const int extraPadding = 80;
            const int minIdWidth = 300;

            dataGridView_Device.Columns["colIcon"]!.Width = iconWidth;
            dataGridView_Device.Columns["colName"]!.Width = nameWidth;
            dataGridView_Device.Columns["colState"]!.Width = stateWidth;

            int maxIdTextWidth = TextRenderer.MeasureText("ID", dataGridView_Device.Font).Width;

            foreach (DataGridViewRow row in dataGridView_Device.Rows)
            {
                if (row.Cells["colId"].Value is string idText)
                {
                    int width = TextRenderer.MeasureText(idText, dataGridView_Device.Font).Width;
                    if (width > maxIdTextWidth)
                    {
                        maxIdTextWidth = width;
                    }
                }
            }

            int contentBasedWidth = Math.Max(minIdWidth, maxIdTextWidth + extraPadding);

            int remainingWidth =
                dataGridView_Device.ClientSize.Width
                - iconWidth
                - nameWidth
                - stateWidth
                - 6;

            int finalIdWidth = Math.Max(contentBasedWidth, remainingWidth);

            dataGridView_Device.Columns["colId"]!.Width = finalIdWidth;
        }

        /// <summary>
        /// デバイス名表示の際にトレイメニュー表示を少し切る
        /// </summary>
        /// <param name="name"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private static string TrimMenuDeviceName(string name, int maxLength = 24)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return name.Length <= maxLength
                ? name
                : name.Substring(0, maxLength - 1) + "…";
        }

        private async void Button_Connect_Click(object sender, EventArgs e)
        {
            await ConnectSelectedDeviceAsync();
        }

        private async void Button_Open_Click(object sender, EventArgs e)
        {
            var selectedDeviceId = GetSelectedDeviceId();
            if (selectedDeviceId == null)
            {
                MessageBox.Show(Localization.DeviceSelectionCaption);
                return;
            }

            if (currentOpenedDeviceId == selectedDeviceId)
            {
                await DisconnectSelectedDeviceAsync();
            }
            else
            {
                await OpenSelectedDeviceAsync();
            }
        }

        private void DataGridView_Device_SelectionChanged(object sender, EventArgs e)
        {
            UpdateUiFromSelection();
        }

        private async void DataGridView_Device_DoubleClick(object sender, EventArgs e)
        {
            var selectedDeviceId = GetSelectedDeviceId();
            if (selectedDeviceId == null)
            {
                return;
            }

            if (!audioPlaybackConnections.ContainsKey(selectedDeviceId))
            {
                await ConnectSelectedDeviceAsync();
                return;
            }

            if (currentOpenedDeviceId != selectedDeviceId)
            {
                await OpenSelectedDeviceAsync();
            }
        }

        private void ExitXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void AddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await AddBluetoothDeviceAsync();
        }

        private async void RemoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await RemoveSelectedBluetoothDeviceAsync();
        }

        private void RefleshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshDeviceList();
        }

        private void ToolStripButton_Settings_Click(object sender, EventArgs e)
        {
            using var settingsForm = new FormSettings(autoPlayOnDeviceConnected);
            if (settingsForm.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            autoPlayOnDeviceConnected = settingsForm.AutoPlayOnDeviceConnected;
            SaveAppSettings();
            Log("Settings saved. Auto open: " + autoPlayOnDeviceConnected);
        }

        private void ToolStripButton_About_Click(object sender, EventArgs e)
        {
            WindowAbout about = new();
            about.ShowDialog();
        }

        private async Task CheckForUpdatesForInit()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    string hv = null!;

                    using Stream hcs = await Task.Run(() => Common.Network.GetWebStreamAsync(appUpdatechecker, Common.Network.GetUri("https://raw.githubusercontent.com/XyLe-GBP/btplayer/master/VERSIONINFO")));
                    using StreamReader hsr = new(hcs);
                    hv = await Task.Run(() => hsr.ReadToEndAsync());
                    Common.GitHubLatestVersion = hv[8..].Replace("\n", "");

                    FileVersionInfo ver = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);

                    if (ver.FileVersion != null)
                    {
                        switch (ver.FileVersion.ToString().CompareTo(hv[8..].Replace("\n", "")))
                        {
                            case -1:
                                DialogResult dr = MessageBox.Show(Localization.LatestCaption + hv[8..].Replace("\n", "") + "\n" + Localization.CurrentCaption + ver.FileVersion + "\n" + Localization.UpdateConfirmCaption, Localization.MSGBoxConfirmCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (dr == DialogResult.Yes)
                                {
                                    using FormSelectApplicationType formtype = new();
                                    if (formtype.ShowDialog() == DialogResult.Cancel) return;

                                    Common.ProgressType = 0;
                                    Common.ProgressMax = 100;
                                    using FormProgress form = new();
                                    form.ShowDialog();

                                    if (Common.Result == false)
                                    {
                                        Common.cts.Dispose();
                                        MessageBox.Show(Localization.CancelledCaption, Localization.MSGBoxAbortedCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return;
                                    }

                                    string updpath = Directory.GetCurrentDirectory()[..Directory.GetCurrentDirectory().LastIndexOf('\\')];
                                    File.Move(Directory.GetCurrentDirectory() + @"\res\updater.exe", updpath + @"\updater.exe");
                                    string wtext;
                                    switch (Common.ApplicationPortable)
                                    {
                                        case false:
                                            {
                                                wtext = Directory.GetCurrentDirectory() + "\r\nrelease";
                                            }
                                            break;
                                        case true:
                                            {
                                                wtext = Directory.GetCurrentDirectory() + "\r\nportable";
                                            }
                                            break;
                                    }
                                    File.WriteAllText(updpath + @"\updater.txt", wtext);
                                    File.Move(updpath + @"\updater.txt", updpath + @"\updater.dat");
                                    if (File.Exists(Directory.GetCurrentDirectory() + @"\res\btplayer.zip"))
                                    {
                                        File.Move(Directory.GetCurrentDirectory() + @"\res\btplayer.zip", updpath + @"\btplayer.zip");
                                    }

                                    ProcessStartInfo pi = new()
                                    {
                                        FileName = updpath + @"\updater.exe",
                                        Arguments = null,
                                        UseShellExecute = true,
                                        WindowStyle = ProcessWindowStyle.Normal,
                                    };
                                    Process.Start(pi);
                                    Close();
                                    return;
                                }
                                else
                                {
                                    DialogResult dr2 = MessageBox.Show(Localization.LatestCaption + hv[8..].Replace("\n", "") + "\n" + Localization.CurrentCaption + ver.FileVersion + "\n" + Localization.SiteOpenCaption, Localization.MSGBoxConfirmCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (dr2 == DialogResult.Yes)
                                    {
                                        Common.Utils.OpenURI("https://github.com/XyLe-GBP/btplayer/releases");
                                        return;
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            case 0:
                                break;
                            case 1:
                                throw new Exception(hv[8..].Replace("\n", "").ToString() + " < " + ver.FileVersion.ToString());
                        }
                        return;
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        private async void ToolStripButton_Update_Click(object sender, EventArgs e)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    string hv = null!;

                    using Stream hcs = await Task.Run(() => Common.Network.GetWebStreamAsync(appUpdatechecker, Common.Network.GetUri("https://raw.githubusercontent.com/XyLe-GBP/btplayer/master/VERSIONINFO")));
                    using StreamReader hsr = new(hcs);
                    hv = await Task.Run(() => hsr.ReadToEndAsync());
                    Common.GitHubLatestVersion = hv[8..].Replace("\n", "");

                    FileVersionInfo ver = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);

                    if (ver.FileVersion != null)
                    {
                        switch (ver.FileVersion.ToString().CompareTo(hv[8..].Replace("\n", "")))
                        {
                            case -1:
                                DialogResult dr = MessageBox.Show(Localization.LatestCaption + hv[8..].Replace("\n", "") + "\n" + Localization.CurrentCaption + ver.FileVersion + "\n" + Localization.UpdateConfirmCaption, Localization.MSGBoxConfirmCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (dr == DialogResult.Yes)
                                {
                                    using FormSelectApplicationType formtype = new();
                                    if (formtype.ShowDialog() == DialogResult.Cancel) return;

                                    Common.ProgressType = 7;
                                    Common.ProgressMax = 100;
                                    using FormProgress form = new();
                                    form.ShowDialog();

                                    if (Common.Result == false)
                                    {
                                        Common.cts.Dispose();
                                        MessageBox.Show(Localization.CancelledCaption, Localization.MSGBoxAbortedCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return;
                                    }

                                    string updpath = Directory.GetCurrentDirectory()[..Directory.GetCurrentDirectory().LastIndexOf('\\')];
                                    File.Move(Directory.GetCurrentDirectory() + @"\res\updater.exe", updpath + @"\updater.exe");
                                    string wtext;
                                    switch (Common.ApplicationPortable)
                                    {
                                        case false:
                                            {
                                                wtext = Directory.GetCurrentDirectory() + "\r\nrelease";
                                            }
                                            break;
                                        case true:
                                            {
                                                wtext = Directory.GetCurrentDirectory() + "\r\nportable";
                                            }
                                            break;
                                    }
                                    File.WriteAllText(updpath + @"\updater.txt", wtext);
                                    File.Move(updpath + @"\updater.txt", updpath + @"\updater.dat");
                                    if (File.Exists(Directory.GetCurrentDirectory() + @"\res\btplayer.zip"))
                                    {
                                        File.Move(Directory.GetCurrentDirectory() + @"\res\btplayer.zip", updpath + @"\btplayer.zip");
                                    }

                                    ProcessStartInfo pi = new()
                                    {
                                        FileName = updpath + @"\updater.exe",
                                        Arguments = null,
                                        UseShellExecute = true,
                                        WindowStyle = ProcessWindowStyle.Normal,
                                    };
                                    Process.Start(pi);
                                    Close();
                                    return;
                                }
                                else
                                {
                                    DialogResult dr2 = MessageBox.Show(this, Localization.LatestCaption + hv[8..].Replace("\n", "") + "\n" + Localization.CurrentCaption + ver.FileVersion + "\n" + Localization.SiteOpenCaption, Localization.MSGBoxConfirmCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (dr2 == DialogResult.Yes)
                                    {
                                        Common.Utils.OpenURI("https://github.com/XyLe-GBP/btplayer/releases");
                                        return;
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            case 0:
                                MessageBox.Show(this, Localization.LatestCaption + hv[8..].Replace("\n", "") + "\n" + Localization.CurrentCaption + ver.FileVersion + "\n" + Localization.UptodateCaption, Localization.DoneCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                            case 1:
                                throw new Exception(hv[8..].Replace("\n", "").ToString() + " < " + ver.FileVersion.ToString());
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, string.Format(Localization.UnExpectedErrorCaption, ex.ToString()), Localization.ErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show(this, Localization.NetworkNotConnectedCaption, Localization.ErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
    }
}
