namespace fwServer
{
    partial class serverHMI
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(serverHMI));
            this.msgViewer = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.ClearBtn = new System.Windows.Forms.Button();
            this.userBox = new System.Windows.Forms.CheckedListBox();
            this.Main = new System.Windows.Forms.Button();
            this.DB_Show = new System.Windows.Forms.Button();
            this.DBPanel = new System.Windows.Forms.Panel();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.refresh = new System.Windows.Forms.Button();
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.Open = new System.Windows.Forms.ToolStripMenuItem();
            this.closePro = new System.Windows.Forms.ToolStripMenuItem();
            this.StopDisplay = new System.Windows.Forms.CheckBox();
            this.DBPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.trayContext.SuspendLayout();
            this.SuspendLayout();
            // 
            // msgViewer
            // 
            this.msgViewer.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.msgViewer.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.msgViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.msgViewer.Location = new System.Drawing.Point(0, 0);
            this.msgViewer.Name = "msgViewer";
            this.msgViewer.ReadOnly = true;
            this.msgViewer.Size = new System.Drawing.Size(919, 699);
            this.msgViewer.TabIndex = 0;
            this.msgViewer.Text = "";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(968, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(173, 15);
            this.label3.TabIndex = 12;
            this.label3.Text = "방화문인터페이스 개소";
            // 
            // ClearBtn
            // 
            this.ClearBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClearBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClearBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ClearBtn.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.ClearBtn.FlatAppearance.BorderSize = 2;
            this.ClearBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ClearBtn.Location = new System.Drawing.Point(848, 45);
            this.ClearBtn.Name = "ClearBtn";
            this.ClearBtn.Size = new System.Drawing.Size(85, 23);
            this.ClearBtn.TabIndex = 15;
            this.ClearBtn.Text = "Clear";
            this.ClearBtn.UseVisualStyleBackColor = false;
            this.ClearBtn.Click += new System.EventHandler(this.ClearBtn_Click);
            // 
            // userBox
            // 
            this.userBox.CheckOnClick = true;
            this.userBox.FormattingEnabled = true;
            this.userBox.HorizontalScrollbar = true;
            this.userBox.Items.AddRange(new object[] {
            "ALL"});
            this.userBox.Location = new System.Drawing.Point(939, 51);
            this.userBox.Name = "userBox";
            this.userBox.Size = new System.Drawing.Size(230, 724);
            this.userBox.TabIndex = 3;
            this.userBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.userBox_ItemCheck);
            // 
            // Main
            // 
            this.Main.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.Main.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Main.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Main.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.Main.FlatAppearance.BorderSize = 2;
            this.Main.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Main.Location = new System.Drawing.Point(833, 9);
            this.Main.Name = "Main";
            this.Main.Size = new System.Drawing.Size(100, 30);
            this.Main.TabIndex = 16;
            this.Main.Text = "전문 수신 확인";
            this.Main.UseVisualStyleBackColor = false;
            this.Main.Click += new System.EventHandler(this.Main_Click);
            // 
            // DB_Show
            // 
            this.DB_Show.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.DB_Show.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DB_Show.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.DB_Show.FlatAppearance.BorderSize = 2;
            this.DB_Show.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.DB_Show.Location = new System.Drawing.Point(823, 9);
            this.DB_Show.Name = "DB_Show";
            this.DB_Show.Size = new System.Drawing.Size(111, 30);
            this.DB_Show.TabIndex = 17;
            this.DB_Show.Text = "방화문 현재 상태";
            this.DB_Show.UseVisualStyleBackColor = false;
            this.DB_Show.Click += new System.EventHandler(this.DB_Show_Click);
            // 
            // DBPanel
            // 
            this.DBPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DBPanel.Controls.Add(this.dataGridView1);
            this.DBPanel.Controls.Add(this.msgViewer);
            this.DBPanel.Location = new System.Drawing.Point(12, 74);
            this.DBPanel.Name = "DBPanel";
            this.DBPanel.Size = new System.Drawing.Size(921, 701);
            this.DBPanel.TabIndex = 18;
            // 
            // dataGridView1
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(919, 699);
            this.dataGridView1.TabIndex = 0;
            // 
            // refresh
            // 
            this.refresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.refresh.Location = new System.Drawing.Point(731, 9);
            this.refresh.Name = "refresh";
            this.refresh.Size = new System.Drawing.Size(86, 30);
            this.refresh.TabIndex = 19;
            this.refresh.Text = "새로고침";
            this.refresh.UseVisualStyleBackColor = true;
            this.refresh.Visible = false;
            this.refresh.Click += new System.EventHandler(this.refresh_Click);
            // 
            // trayIcon
            // 
            this.trayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
            this.trayIcon.ContextMenuStrip = this.trayContext;
            this.trayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("trayIcon.Icon")));
            this.trayIcon.Text = "TASKCON";
            this.trayIcon.Visible = true;
            this.trayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.trayIcon_MouseDoubleClick);
            // 
            // trayContext
            // 
            this.trayContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Open,
            this.closePro});
            this.trayContext.Name = "trayContext";
            this.trayContext.Size = new System.Drawing.Size(151, 48);
            this.trayContext.Text = "trayContext";
            // 
            // Open
            // 
            this.Open.Name = "Open";
            this.Open.Size = new System.Drawing.Size(150, 22);
            this.Open.Text = "열기";
            // 
            // closePro
            // 
            this.closePro.Name = "closePro";
            this.closePro.Size = new System.Drawing.Size(150, 22);
            this.closePro.Text = "프로그램 종료";
            this.closePro.Click += new System.EventHandler(this.Close_Click);
            // 
            // StopDisplay
            // 
            this.StopDisplay.AutoSize = true;
            this.StopDisplay.Location = new System.Drawing.Point(741, 49);
            this.StopDisplay.Name = "StopDisplay";
            this.StopDisplay.Size = new System.Drawing.Size(100, 16);
            this.StopDisplay.TabIndex = 21;
            this.StopDisplay.Text = "로그표시 중지";
            this.StopDisplay.UseVisualStyleBackColor = true;
            // 
            // serverHMI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1183, 786);
            this.Controls.Add(this.StopDisplay);
            this.Controls.Add(this.refresh);
            this.Controls.Add(this.DBPanel);
            this.Controls.Add(this.DB_Show);
            this.Controls.Add(this.Main);
            this.Controls.Add(this.userBox);
            this.Controls.Add(this.ClearBtn);
            this.Controls.Add(this.label3);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "serverHMI";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Text = "방화문 통신 프로그램";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.serverHMI_FormClosing_1);
            this.Load += new System.EventHandler(this.serverHMI_Load);
            this.DBPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.trayContext.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox msgViewer;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button ClearBtn;
        private System.Windows.Forms.CheckedListBox userBox;
        private System.Windows.Forms.Button Main;
        private System.Windows.Forms.Button DB_Show;
        private System.Windows.Forms.Panel DBPanel;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button refresh;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayContext;
        private System.Windows.Forms.ToolStripMenuItem Open;
        private System.Windows.Forms.ToolStripMenuItem closePro;
        private System.Windows.Forms.CheckBox StopDisplay;
    }
}

