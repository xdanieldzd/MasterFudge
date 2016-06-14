namespace MasterFudge.Controls
{
    partial class DisassemblyBox
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

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.pbOpcodes = new System.Windows.Forms.PictureBox();
            this.vsbAddress = new System.Windows.Forms.VScrollBar();
            ((System.ComponentModel.ISupportInitialize)(this.pbOpcodes)).BeginInit();
            this.SuspendLayout();
            // 
            // pbOpcodes
            // 
            this.pbOpcodes.BackColor = System.Drawing.SystemColors.Window;
            this.pbOpcodes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbOpcodes.Location = new System.Drawing.Point(0, 0);
            this.pbOpcodes.Margin = new System.Windows.Forms.Padding(0);
            this.pbOpcodes.Name = "pbOpcodes";
            this.pbOpcodes.Size = new System.Drawing.Size(183, 200);
            this.pbOpcodes.TabIndex = 0;
            this.pbOpcodes.TabStop = false;
            this.pbOpcodes.Paint += new System.Windows.Forms.PaintEventHandler(this.pbOpcodes_Paint);
            // 
            // vsbAddress
            // 
            this.vsbAddress.Dock = System.Windows.Forms.DockStyle.Right;
            this.vsbAddress.Location = new System.Drawing.Point(183, 0);
            this.vsbAddress.Maximum = 65535;
            this.vsbAddress.Name = "vsbAddress";
            this.vsbAddress.Size = new System.Drawing.Size(17, 200);
            this.vsbAddress.TabIndex = 1;
            this.vsbAddress.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vsbAddress_Scroll);
            // 
            // DisassemblyBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.pbOpcodes);
            this.Controls.Add(this.vsbAddress);
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "DisassemblyBox";
            this.Size = new System.Drawing.Size(200, 200);
            ((System.ComponentModel.ISupportInitialize)(this.pbOpcodes)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbOpcodes;
        private System.Windows.Forms.VScrollBar vsbAddress;
    }
}
