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
            this.btnTempRun = new System.Windows.Forms.Button();
            this.pbTempDisplay = new System.Windows.Forms.PictureBox();
            this.pbTempPalette = new System.Windows.Forms.PictureBox();
            this.ofdOpenRom = new System.Windows.Forms.OpenFileDialog();
            this.chkTempFPSLimiter = new System.Windows.Forms.CheckBox();
            this.chkTempLogZ80 = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbTempDisplay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbTempPalette)).BeginInit();
            this.SuspendLayout();
            // 
            // btnTempRun
            // 
            this.btnTempRun.Location = new System.Drawing.Point(12, 12);
            this.btnTempRun.Name = "btnTempRun";
            this.btnTempRun.Size = new System.Drawing.Size(75, 23);
            this.btnTempRun.TabIndex = 1;
            this.btnTempRun.Text = "Load && Run";
            this.btnTempRun.UseVisualStyleBackColor = true;
            this.btnTempRun.Click += new System.EventHandler(this.btnTempRun_Click);
            // 
            // pbTempDisplay
            // 
            this.pbTempDisplay.BackColor = System.Drawing.Color.LightGray;
            this.pbTempDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbTempDisplay.Location = new System.Drawing.Point(12, 41);
            this.pbTempDisplay.Name = "pbTempDisplay";
            this.pbTempDisplay.Size = new System.Drawing.Size(525, 450);
            this.pbTempDisplay.TabIndex = 3;
            this.pbTempDisplay.TabStop = false;
            // 
            // pbTempPalette
            // 
            this.pbTempPalette.BackColor = System.Drawing.Color.LightGray;
            this.pbTempPalette.Location = new System.Drawing.Point(543, 41);
            this.pbTempPalette.Name = "pbTempPalette";
            this.pbTempPalette.Size = new System.Drawing.Size(128, 256);
            this.pbTempPalette.TabIndex = 4;
            this.pbTempPalette.TabStop = false;
            // 
            // ofdOpenRom
            // 
            this.ofdOpenRom.Filter = "SMS ROMs (*.sms)|*.sms|All Files (*.*)|*.*";
            // 
            // chkTempFPSLimiter
            // 
            this.chkTempFPSLimiter.AutoSize = true;
            this.chkTempFPSLimiter.Location = new System.Drawing.Point(174, 16);
            this.chkTempFPSLimiter.Name = "chkTempFPSLimiter";
            this.chkTempFPSLimiter.Size = new System.Drawing.Size(70, 17);
            this.chkTempFPSLimiter.TabIndex = 5;
            this.chkTempFPSLimiter.Text = "Limit FPS";
            this.chkTempFPSLimiter.UseVisualStyleBackColor = true;
            // 
            // chkTempLogZ80
            // 
            this.chkTempLogZ80.AutoSize = true;
            this.chkTempLogZ80.Location = new System.Drawing.Point(250, 16);
            this.chkTempLogZ80.Name = "chkTempLogZ80";
            this.chkTempLogZ80.Size = new System.Drawing.Size(90, 17);
            this.chkTempLogZ80.TabIndex = 6;
            this.chkTempLogZ80.Text = "Log Opcodes";
            this.chkTempLogZ80.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 512);
            this.Controls.Add(this.chkTempLogZ80);
            this.Controls.Add(this.chkTempFPSLimiter);
            this.Controls.Add(this.pbTempPalette);
            this.Controls.Add(this.pbTempDisplay);
            this.Controls.Add(this.btnTempRun);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.pbTempDisplay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbTempPalette)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnTempRun;
        private System.Windows.Forms.PictureBox pbTempDisplay;
        private System.Windows.Forms.PictureBox pbTempPalette;
        private System.Windows.Forms.OpenFileDialog ofdOpenRom;
        private System.Windows.Forms.CheckBox chkTempFPSLimiter;
        private System.Windows.Forms.CheckBox chkTempLogZ80;
    }
}

