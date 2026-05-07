namespace BTPlayer
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            toolStrip_Main = new ToolStrip();
            toolStripButton_Add = new ToolStripButton();
            toolStripButton_Remove = new ToolStripButton();
            toolStripButton_Refresh = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripButton_Exit = new ToolStripButton();
            toolStripButton_About = new ToolStripButton();
            panel_HeaderCard = new Panel();
            label_Title = new Label();
            label_Status = new Label();
            tableLayoutPanel_Main = new TableLayoutPanel();
            panel_DeviceCard = new Panel();
            label_DeviceList = new Label();
            dataGridView_Device = new DataGridView();
            panel_LogCard = new Panel();
            label_Log = new Label();
            textBox_Log = new TextBox();
            panel_ActionBar = new Panel();
            button_Connect = new Button();
            button_Open = new Button();
            toolStripButton_Update = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            toolStrip_Main.SuspendLayout();
            panel_HeaderCard.SuspendLayout();
            tableLayoutPanel_Main.SuspendLayout();
            panel_DeviceCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_Device).BeginInit();
            panel_LogCard.SuspendLayout();
            panel_ActionBar.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip_Main
            // 
            toolStrip_Main.BackColor = Color.White;
            toolStrip_Main.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip_Main.ImageScalingSize = new Size(20, 20);
            toolStrip_Main.Items.AddRange(new ToolStripItem[] { toolStripButton_Add, toolStripButton_Remove, toolStripButton_Refresh, toolStripSeparator1, toolStripButton_Exit, toolStripButton_About, toolStripSeparator2, toolStripButton_Update });
            toolStrip_Main.Location = new Point(0, 0);
            toolStrip_Main.Name = "toolStrip_Main";
            toolStrip_Main.Padding = new Padding(12, 6, 12, 6);
            toolStrip_Main.Size = new Size(1084, 35);
            toolStrip_Main.TabIndex = 0;
            // 
            // toolStripButton_Add
            // 
            toolStripButton_Add.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_Add.Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 128);
            toolStripButton_Add.Name = "toolStripButton_Add";
            toolStripButton_Add.Size = new Size(72, 20);
            toolStripButton_Add.Text = "Add Device";
            toolStripButton_Add.Click += AddToolStripMenuItem_Click;
            // 
            // toolStripButton_Remove
            // 
            toolStripButton_Remove.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_Remove.Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 128);
            toolStripButton_Remove.Name = "toolStripButton_Remove";
            toolStripButton_Remove.Size = new Size(93, 20);
            toolStripButton_Remove.Text = "Remove Device";
            toolStripButton_Remove.Click += RemoveToolStripMenuItem_Click;
            // 
            // toolStripButton_Refresh
            // 
            toolStripButton_Refresh.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_Refresh.Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 128);
            toolStripButton_Refresh.Name = "toolStripButton_Refresh";
            toolStripButton_Refresh.Size = new Size(50, 20);
            toolStripButton_Refresh.Text = "Refresh";
            toolStripButton_Refresh.Click += RefleshToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 23);
            // 
            // toolStripButton_Exit
            // 
            toolStripButton_Exit.Alignment = ToolStripItemAlignment.Right;
            toolStripButton_Exit.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_Exit.Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 128);
            toolStripButton_Exit.ForeColor = Color.Red;
            toolStripButton_Exit.Name = "toolStripButton_Exit";
            toolStripButton_Exit.Size = new Size(30, 20);
            toolStripButton_Exit.Text = "Exit";
            toolStripButton_Exit.Click += ExitXToolStripMenuItem_Click;
            // 
            // toolStripButton_About
            // 
            toolStripButton_About.Alignment = ToolStripItemAlignment.Right;
            toolStripButton_About.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_About.Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 128);
            toolStripButton_About.Name = "toolStripButton_About";
            toolStripButton_About.Size = new Size(44, 20);
            toolStripButton_About.Text = "About";
            toolStripButton_About.Click += ToolStripButton_About_Click;
            // 
            // panel_HeaderCard
            // 
            panel_HeaderCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel_HeaderCard.BackColor = Color.White;
            panel_HeaderCard.Controls.Add(label_Title);
            panel_HeaderCard.Controls.Add(label_Status);
            panel_HeaderCard.Location = new Point(16, 52);
            panel_HeaderCard.Name = "panel_HeaderCard";
            panel_HeaderCard.Padding = new Padding(16, 14, 16, 14);
            panel_HeaderCard.Size = new Size(1052, 78);
            panel_HeaderCard.TabIndex = 1;
            // 
            // label_Title
            // 
            label_Title.Font = new Font("Yu Gothic UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 128);
            label_Title.ForeColor = Color.FromArgb(25, 25, 25);
            label_Title.Location = new Point(13, 8);
            label_Title.Name = "label_Title";
            label_Title.Size = new Size(1023, 30);
            label_Title.TabIndex = 0;
            label_Title.Text = "BTPlayer";
            label_Title.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label_Status
            // 
            label_Status.AutoEllipsis = true;
            label_Status.Font = new Font("Yu Gothic UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 128);
            label_Status.ForeColor = Color.FromArgb(25, 25, 25);
            label_Status.Location = new Point(13, 42);
            label_Status.Name = "label_Status";
            label_Status.Size = new Size(1023, 22);
            label_Status.TabIndex = 1;
            label_Status.Text = "Status: Disconnected";
            label_Status.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel_Main
            // 
            tableLayoutPanel_Main.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel_Main.ColumnCount = 1;
            tableLayoutPanel_Main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel_Main.Controls.Add(panel_DeviceCard, 0, 0);
            tableLayoutPanel_Main.Controls.Add(panel_LogCard, 0, 1);
            tableLayoutPanel_Main.Location = new Point(16, 146);
            tableLayoutPanel_Main.Name = "tableLayoutPanel_Main";
            tableLayoutPanel_Main.RowCount = 2;
            tableLayoutPanel_Main.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tableLayoutPanel_Main.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            tableLayoutPanel_Main.Size = new Size(1052, 494);
            tableLayoutPanel_Main.TabIndex = 2;
            // 
            // panel_DeviceCard
            // 
            panel_DeviceCard.BackColor = Color.White;
            panel_DeviceCard.Controls.Add(label_DeviceList);
            panel_DeviceCard.Controls.Add(dataGridView_Device);
            panel_DeviceCard.Dock = DockStyle.Fill;
            panel_DeviceCard.Location = new Point(0, 0);
            panel_DeviceCard.Margin = new Padding(0, 0, 0, 12);
            panel_DeviceCard.Name = "panel_DeviceCard";
            panel_DeviceCard.Padding = new Padding(16, 14, 16, 16);
            panel_DeviceCard.Size = new Size(1052, 284);
            panel_DeviceCard.TabIndex = 0;
            // 
            // label_DeviceList
            // 
            label_DeviceList.AutoSize = true;
            label_DeviceList.Font = new Font("Yu Gothic UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 128);
            label_DeviceList.ForeColor = Color.FromArgb(60, 60, 60);
            label_DeviceList.Location = new Point(16, 12);
            label_DeviceList.Name = "label_DeviceList";
            label_DeviceList.Size = new Size(77, 19);
            label_DeviceList.TabIndex = 0;
            label_DeviceList.Text = "Device List";
            // 
            // dataGridView_Device
            // 
            dataGridView_Device.AllowUserToAddRows = false;
            dataGridView_Device.AllowUserToDeleteRows = false;
            dataGridView_Device.AllowUserToResizeRows = false;
            dataGridView_Device.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_Device.BackgroundColor = Color.White;
            dataGridView_Device.BorderStyle = BorderStyle.None;
            dataGridView_Device.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView_Device.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridView_Device.Location = new Point(16, 40);
            dataGridView_Device.MultiSelect = false;
            dataGridView_Device.Name = "dataGridView_Device";
            dataGridView_Device.ReadOnly = true;
            dataGridView_Device.RowHeadersVisible = false;
            dataGridView_Device.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView_Device.Size = new Size(1020, 228);
            dataGridView_Device.TabIndex = 1;
            dataGridView_Device.SelectionChanged += DataGridView_Device_SelectionChanged;
            dataGridView_Device.DoubleClick += DataGridView_Device_DoubleClick;
            // 
            // panel_LogCard
            // 
            panel_LogCard.BackColor = Color.White;
            panel_LogCard.Controls.Add(label_Log);
            panel_LogCard.Controls.Add(textBox_Log);
            panel_LogCard.Dock = DockStyle.Fill;
            panel_LogCard.Location = new Point(0, 296);
            panel_LogCard.Margin = new Padding(0);
            panel_LogCard.Name = "panel_LogCard";
            panel_LogCard.Padding = new Padding(16, 14, 16, 16);
            panel_LogCard.Size = new Size(1052, 198);
            panel_LogCard.TabIndex = 1;
            // 
            // label_Log
            // 
            label_Log.AutoSize = true;
            label_Log.Font = new Font("Yu Gothic UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 128);
            label_Log.ForeColor = Color.FromArgb(60, 60, 60);
            label_Log.Location = new Point(16, 12);
            label_Log.Name = "label_Log";
            label_Log.Size = new Size(32, 19);
            label_Log.TabIndex = 0;
            label_Log.Text = "Log";
            // 
            // textBox_Log
            // 
            textBox_Log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBox_Log.BackColor = Color.FromArgb(248, 249, 251);
            textBox_Log.BorderStyle = BorderStyle.FixedSingle;
            textBox_Log.Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            textBox_Log.Location = new Point(16, 40);
            textBox_Log.Multiline = true;
            textBox_Log.Name = "textBox_Log";
            textBox_Log.ReadOnly = true;
            textBox_Log.ScrollBars = ScrollBars.Both;
            textBox_Log.Size = new Size(1020, 142);
            textBox_Log.TabIndex = 1;
            textBox_Log.WordWrap = false;
            // 
            // panel_ActionBar
            // 
            panel_ActionBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel_ActionBar.BackColor = Color.Transparent;
            panel_ActionBar.Controls.Add(button_Connect);
            panel_ActionBar.Controls.Add(button_Open);
            panel_ActionBar.Location = new Point(16, 648);
            panel_ActionBar.Name = "panel_ActionBar";
            panel_ActionBar.Size = new Size(1052, 56);
            panel_ActionBar.TabIndex = 3;
            // 
            // button_Connect
            // 
            button_Connect.Anchor = AnchorStyles.Left;
            button_Connect.BackColor = Color.FromArgb(0, 120, 215);
            button_Connect.FlatAppearance.BorderSize = 0;
            button_Connect.FlatStyle = FlatStyle.Flat;
            button_Connect.Font = new Font("Yu Gothic UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 128);
            button_Connect.ForeColor = Color.White;
            button_Connect.Location = new Point(0, 5);
            button_Connect.Name = "button_Connect";
            button_Connect.Size = new Size(240, 46);
            button_Connect.TabIndex = 0;
            button_Connect.Text = "Connect";
            button_Connect.UseVisualStyleBackColor = false;
            button_Connect.Click += Button_Connect_Click;
            // 
            // button_Open
            // 
            button_Open.Anchor = AnchorStyles.Right;
            button_Open.BackColor = Color.FromArgb(32, 32, 32);
            button_Open.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            button_Open.FlatStyle = FlatStyle.Flat;
            button_Open.Font = new Font("Yu Gothic UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 128);
            button_Open.ForeColor = Color.White;
            button_Open.Location = new Point(812, 5);
            button_Open.Name = "button_Open";
            button_Open.Size = new Size(240, 46);
            button_Open.TabIndex = 1;
            button_Open.Text = "Open";
            button_Open.UseVisualStyleBackColor = false;
            button_Open.Click += Button_Open_Click;
            // 
            // toolStripButton_Update
            // 
            toolStripButton_Update.Alignment = ToolStripItemAlignment.Right;
            toolStripButton_Update.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_Update.Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 128);
            toolStripButton_Update.Name = "toolStripButton_Update";
            toolStripButton_Update.Size = new Size(49, 20);
            toolStripButton_Update.Text = "Update";
            toolStripButton_Update.Click += ToolStripButton_Update_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Alignment = ToolStripItemAlignment.Right;
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 23);
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 247, 250);
            ClientSize = new Size(1084, 721);
            Controls.Add(panel_ActionBar);
            Controls.Add(tableLayoutPanel_Main);
            Controls.Add(panel_HeaderCard);
            Controls.Add(toolStrip_Main);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1100, 760);
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BTPlayer";
            FormClosing += FormMain_FormClosing;
            Load += FormMain_Load;
            toolStrip_Main.ResumeLayout(false);
            toolStrip_Main.PerformLayout();
            panel_HeaderCard.ResumeLayout(false);
            tableLayoutPanel_Main.ResumeLayout(false);
            panel_DeviceCard.ResumeLayout(false);
            panel_DeviceCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_Device).EndInit();
            panel_LogCard.ResumeLayout(false);
            panel_LogCard.PerformLayout();
            panel_ActionBar.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip_Main;
        private ToolStripButton toolStripButton_Add;
        private ToolStripButton toolStripButton_Remove;
        private ToolStripButton toolStripButton_Refresh;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton toolStripButton_Exit;
        private ToolStripButton toolStripButton_About;
        private Panel panel_HeaderCard;
        private Label label_Title;
        private Label label_Status;
        private TableLayoutPanel tableLayoutPanel_Main;
        private Panel panel_DeviceCard;
        private Label label_DeviceList;
        private DataGridView dataGridView_Device;
        private Panel panel_LogCard;
        private Label label_Log;
        private TextBox textBox_Log;
        private Panel panel_ActionBar;
        private Button button_Connect;
        private Button button_Open;
        private ToolStripButton toolStripButton_Update;
        private ToolStripSeparator toolStripSeparator2;
    }
}