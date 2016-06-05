namespace MasterFudge
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.pbRenderOutput = new System.Windows.Forms.PictureBox();
            this.ofdOpenCartridge = new System.Windows.Forms.OpenFileDialog();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openCartridgeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cartridgeInformationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.tVSystemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.regionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logOpcodesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslFps = new System.Windows.Forms.ToolStripStatusLabel();
            this.limitFPSToolStripMenuItem = new MasterFudge.Controls.BindableToolStripMenuItem();
            this.enableSoundToolStripMenuItem = new MasterFudge.Controls.BindableToolStripMenuItem();
            this.nTSCToolStripMenuItem = new MasterFudge.Controls.ToolStripRadioButtonMenuItem();
            this.pALToolStripMenuItem = new MasterFudge.Controls.ToolStripRadioButtonMenuItem();
            this.japaneseToolStripMenuItem = new MasterFudge.Controls.ToolStripRadioButtonMenuItem();
            this.exportToolStripMenuItem = new MasterFudge.Controls.ToolStripRadioButtonMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pbRenderOutput)).BeginInit();
            this.menuStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbRenderOutput
            // 
            this.pbRenderOutput.BackColor = System.Drawing.Color.Black;
            this.pbRenderOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbRenderOutput.Location = new System.Drawing.Point(0, 24);
            this.pbRenderOutput.Margin = new System.Windows.Forms.Padding(0);
            this.pbRenderOutput.Name = "pbRenderOutput";
            this.pbRenderOutput.Size = new System.Drawing.Size(512, 480);
            this.pbRenderOutput.TabIndex = 3;
            this.pbRenderOutput.TabStop = false;
            this.pbRenderOutput.Paint += new System.Windows.Forms.PaintEventHandler(this.pbRenderOutput_Paint);
            // 
            // ofdOpenCartridge
            // 
            this.ofdOpenCartridge.Filter = "Sega Master System & Game Gear ROMs (*.sms;*.gg)|*.sms;*.gg|All Files (*.*)|*.*";
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.debugToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(512, 24);
            this.menuStrip.TabIndex = 7;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openCartridgeToolStripMenuItem,
            this.cartridgeInformationToolStripMenuItem,
            this.toolStripMenuItem1,
            this.recentFilesToolStripMenuItem,
            this.toolStripMenuItem3,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openCartridgeToolStripMenuItem
            // 
            this.openCartridgeToolStripMenuItem.Name = "openCartridgeToolStripMenuItem";
            this.openCartridgeToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.openCartridgeToolStripMenuItem.Text = "&Open Cartridge...";
            this.openCartridgeToolStripMenuItem.Click += new System.EventHandler(this.openCartridgeToolStripMenuItem_Click);
            // 
            // cartridgeInformationToolStripMenuItem
            // 
            this.cartridgeInformationToolStripMenuItem.Enabled = false;
            this.cartridgeInformationToolStripMenuItem.Name = "cartridgeInformationToolStripMenuItem";
            this.cartridgeInformationToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.cartridgeInformationToolStripMenuItem.Text = "Cartridge &Information";
            this.cartridgeInformationToolStripMenuItem.Click += new System.EventHandler(this.cartridgeInformationToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(186, 6);
            // 
            // recentFilesToolStripMenuItem
            // 
            this.recentFilesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearListToolStripMenuItem,
            this.toolStripMenuItem5});
            this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
            this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.recentFilesToolStripMenuItem.Text = "&Recent Files...";
            // 
            // clearListToolStripMenuItem
            // 
            this.clearListToolStripMenuItem.Name = "clearListToolStripMenuItem";
            this.clearListToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.clearListToolStripMenuItem.Text = "&Clear List";
            this.clearListToolStripMenuItem.Click += new System.EventHandler(this.clearListToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(119, 6);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(186, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.limitFPSToolStripMenuItem,
            this.toolStripMenuItem2,
            this.enableSoundToolStripMenuItem,
            this.toolStripMenuItem4,
            this.tVSystemToolStripMenuItem,
            this.regionToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(143, 6);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(143, 6);
            // 
            // tVSystemToolStripMenuItem
            // 
            this.tVSystemToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nTSCToolStripMenuItem,
            this.pALToolStripMenuItem});
            this.tVSystemToolStripMenuItem.Name = "tVSystemToolStripMenuItem";
            this.tVSystemToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.tVSystemToolStripMenuItem.Text = "&TV System";
            // 
            // regionToolStripMenuItem
            // 
            this.regionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.japaneseToolStripMenuItem,
            this.exportToolStripMenuItem});
            this.regionToolStripMenuItem.Name = "regionToolStripMenuItem";
            this.regionToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.regionToolStripMenuItem.Text = "&Region";
            // 
            // debugToolStripMenuItem
            // 
            this.debugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableLogToolStripMenuItem,
            this.logOpcodesToolStripMenuItem});
            this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            this.debugToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.debugToolStripMenuItem.Text = "&Debug";
            // 
            // enableLogToolStripMenuItem
            // 
            this.enableLogToolStripMenuItem.CheckOnClick = true;
            this.enableLogToolStripMenuItem.Name = "enableLogToolStripMenuItem";
            this.enableLogToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.enableLogToolStripMenuItem.Text = "&Enable Log";
            this.enableLogToolStripMenuItem.Click += new System.EventHandler(this.enableLogToolStripMenuItem_Click);
            // 
            // logOpcodesToolStripMenuItem
            // 
            this.logOpcodesToolStripMenuItem.CheckOnClick = true;
            this.logOpcodesToolStripMenuItem.Name = "logOpcodesToolStripMenuItem";
            this.logOpcodesToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.logOpcodesToolStripMenuItem.Text = "Log &Opcodes";
            this.logOpcodesToolStripMenuItem.Click += new System.EventHandler(this.logOpcodesToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslStatus,
            this.tsslFps});
            this.statusStrip.Location = new System.Drawing.Point(0, 482);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(512, 22);
            this.statusStrip.TabIndex = 8;
            this.statusStrip.Text = "statusStrip1";
            // 
            // tsslStatus
            // 
            this.tsslStatus.Name = "tsslStatus";
            this.tsslStatus.Size = new System.Drawing.Size(466, 17);
            this.tsslStatus.Spring = true;
            this.tsslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tsslFps
            // 
            this.tsslFps.Name = "tsslFps";
            this.tsslFps.Overflow = System.Windows.Forms.ToolStripItemOverflow.Always;
            this.tsslFps.Size = new System.Drawing.Size(0, 17);
            this.tsslFps.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // limitFPSToolStripMenuItem
            // 
            this.limitFPSToolStripMenuItem.CheckOnClick = true;
            this.limitFPSToolStripMenuItem.Name = "limitFPSToolStripMenuItem";
            this.limitFPSToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.limitFPSToolStripMenuItem.Text = "&Limit FPS";
            this.limitFPSToolStripMenuItem.Click += new System.EventHandler(this.limitFPSToolStripMenuItem_Click);
            // 
            // enableSoundToolStripMenuItem
            // 
            this.enableSoundToolStripMenuItem.CheckOnClick = true;
            this.enableSoundToolStripMenuItem.Name = "enableSoundToolStripMenuItem";
            this.enableSoundToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.enableSoundToolStripMenuItem.Text = "Enable &Sound";
            this.enableSoundToolStripMenuItem.Click += new System.EventHandler(this.enableSoundToolStripMenuItem_Click);
            // 
            // nTSCToolStripMenuItem
            // 
            this.nTSCToolStripMenuItem.CheckOnClick = true;
            this.nTSCToolStripMenuItem.Name = "nTSCToolStripMenuItem";
            this.nTSCToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.nTSCToolStripMenuItem.Text = "&NTSC";
            this.nTSCToolStripMenuItem.Click += new System.EventHandler(this.nTSCToolStripMenuItem_Click);
            // 
            // pALToolStripMenuItem
            // 
            this.pALToolStripMenuItem.CheckOnClick = true;
            this.pALToolStripMenuItem.Name = "pALToolStripMenuItem";
            this.pALToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.pALToolStripMenuItem.Text = "&PAL";
            this.pALToolStripMenuItem.Click += new System.EventHandler(this.pALToolStripMenuItem_Click);
            // 
            // japaneseToolStripMenuItem
            // 
            this.japaneseToolStripMenuItem.CheckOnClick = true;
            this.japaneseToolStripMenuItem.Name = "japaneseToolStripMenuItem";
            this.japaneseToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.japaneseToolStripMenuItem.Text = "&Japanese";
            this.japaneseToolStripMenuItem.Click += new System.EventHandler(this.japaneseToolStripMenuItem_Click);
            // 
            // exportToolStripMenuItem
            // 
            this.exportToolStripMenuItem.CheckOnClick = true;
            this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            this.exportToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.exportToolStripMenuItem.Text = "&Export";
            this.exportToolStripMenuItem.Click += new System.EventHandler(this.exportToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 504);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.pbRenderOutput);
            this.Controls.Add(this.menuStrip);
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.pbRenderOutput)).EndInit();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox pbRenderOutput;
        private System.Windows.Forms.OpenFileDialog ofdOpenCartridge;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openCartridgeToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
        private Controls.BindableToolStripMenuItem limitFPSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem logOpcodesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cartridgeInformationToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripStatusLabel tsslStatus;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tVSystemToolStripMenuItem;
        private Controls.ToolStripRadioButtonMenuItem nTSCToolStripMenuItem;
        private Controls.ToolStripRadioButtonMenuItem pALToolStripMenuItem;
        private Controls.ToolStripRadioButtonMenuItem japaneseToolStripMenuItem;
        private Controls.ToolStripRadioButtonMenuItem exportToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem regionToolStripMenuItem;
        private Controls.BindableToolStripMenuItem enableSoundToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearListToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
        private System.Windows.Forms.ToolStripStatusLabel tsslFps;
    }
}

