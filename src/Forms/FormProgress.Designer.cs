namespace BTPlayer
{
    partial class FormProgress
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            progressBar_MainProgress = new ProgressBar();
            label_log1 = new Label();
            label_Progress = new Label();
            timer_interval = new System.Windows.Forms.Timer(components);
            button_Abort = new Button();
            SuspendLayout();
            // 
            // progressBar_MainProgress
            // 
            progressBar_MainProgress.Location = new Point(12, 49);
            progressBar_MainProgress.Name = "progressBar_MainProgress";
            progressBar_MainProgress.Size = new Size(460, 23);
            progressBar_MainProgress.TabIndex = 0;
            // 
            // label_log1
            // 
            label_log1.Location = new Point(12, 9);
            label_log1.Name = "label_log1";
            label_log1.Size = new Size(460, 37);
            label_log1.TabIndex = 1;
            label_log1.Text = "log1";
            label_log1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label_Progress
            // 
            label_Progress.AutoSize = true;
            label_Progress.Location = new Point(12, 75);
            label_Progress.Name = "label_Progress";
            label_Progress.Size = new Size(52, 15);
            label_Progress.TabIndex = 2;
            label_Progress.Text = "Progress";
            // 
            // timer_interval
            // 
            timer_interval.Tick += timer_interval_Tick;
            // 
            // button_Abort
            // 
            button_Abort.Location = new Point(12, 106);
            button_Abort.Name = "button_Abort";
            button_Abort.Size = new Size(460, 23);
            button_Abort.TabIndex = 3;
            button_Abort.Text = "Abort";
            button_Abort.UseVisualStyleBackColor = true;
            button_Abort.Click += button_Abort_Click;
            // 
            // FormProgress
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 141);
            ControlBox = false;
            Controls.Add(button_Abort);
            Controls.Add(label_Progress);
            Controls.Add(label_log1);
            Controls.Add(progressBar_MainProgress);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "FormProgress";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FormProgress";
            Load += FormProgress_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ProgressBar progressBar_MainProgress;
        private Label label_log1;
        private Label label_Progress;
        private System.Windows.Forms.Timer timer_interval;
        private Button button_Abort;
    }
}