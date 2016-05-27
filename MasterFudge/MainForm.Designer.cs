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
            this.btnTempPause = new System.Windows.Forms.Button();
            this.pbTempDisplay = new System.Windows.Forms.PictureBox();
            this.pbTempPalette = new System.Windows.Forms.PictureBox();
            this.ofdOpenRom = new System.Windows.Forms.OpenFileDialog();
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
            this.btnTempRun.Text = "Reload/Run";
            this.btnTempRun.UseVisualStyleBackColor = true;
            this.btnTempRun.Click += new System.EventHandler(this.btnTempRun_Click);
            // 
            // btnTempPause
            // 
            this.btnTempPause.Location = new System.Drawing.Point(93, 12);
            this.btnTempPause.Name = "btnTempPause";
            this.btnTempPause.Size = new System.Drawing.Size(75, 23);
            this.btnTempPause.TabIndex = 2;
            this.btnTempPause.Text = "Pause";
            this.btnTempPause.UseVisualStyleBackColor = true;
            this.btnTempPause.Click += new System.EventHandler(this.btnTempPause_Click);
            // 
            // pbTempDisplay
            // 
            this.pbTempDisplay.BackColor = System.Drawing.Color.LightGray;
            this.pbTempDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbTempDisplay.Location = new System.Drawing.Point(12, 41);
            this.pbTempDisplay.Name = "pbTempDisplay";
            this.pbTempDisplay.Size = new System.Drawing.Size(300, 300);
            this.pbTempDisplay.TabIndex = 3;
            this.pbTempDisplay.TabStop = false;
            // 
            // pbTempPalette
            // 
            this.pbTempPalette.BackColor = System.Drawing.Color.LightGray;
            this.pbTempPalette.Location = new System.Drawing.Point(318, 41);
            this.pbTempPalette.Name = "pbTempPalette";
            this.pbTempPalette.Size = new System.Drawing.Size(128, 256);
            this.pbTempPalette.TabIndex = 4;
            this.pbTempPalette.TabStop = false;
            // 
            // ofdOpenRom
            // 
            this.ofdOpenRom.Filter = "SMS ROMs (*.sms)|*.sms|All Files (*.*)|*.*";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 562);
            this.Controls.Add(this.pbTempPalette);
            this.Controls.Add(this.pbTempDisplay);
            this.Controls.Add(this.btnTempPause);
            this.Controls.Add(this.btnTempRun);
            this.Name = "MainForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbTempDisplay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbTempPalette)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnTempRun;
        private System.Windows.Forms.Button btnTempPause;
        private System.Windows.Forms.PictureBox pbTempDisplay;
        private System.Windows.Forms.PictureBox pbTempPalette;
        private System.Windows.Forms.OpenFileDialog ofdOpenRom;
    }
}

