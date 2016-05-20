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
            this.lbTempDisasm = new System.Windows.Forms.ListBox();
            this.btnTempRun = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbTempDisasm
            // 
            this.lbTempDisasm.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbTempDisasm.Font = new System.Drawing.Font("DejaVu Sans Mono", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbTempDisasm.FormattingEnabled = true;
            this.lbTempDisasm.IntegralHeight = false;
            this.lbTempDisasm.ItemHeight = 14;
            this.lbTempDisasm.Location = new System.Drawing.Point(12, 41);
            this.lbTempDisasm.Name = "lbTempDisasm";
            this.lbTempDisasm.ScrollAlwaysVisible = true;
            this.lbTempDisasm.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.lbTempDisasm.Size = new System.Drawing.Size(560, 509);
            this.lbTempDisasm.TabIndex = 0;
            // 
            // btnTempRun
            // 
            this.btnTempRun.Location = new System.Drawing.Point(12, 12);
            this.btnTempRun.Name = "btnTempRun";
            this.btnTempRun.Size = new System.Drawing.Size(75, 23);
            this.btnTempRun.TabIndex = 1;
            this.btnTempRun.Text = "Run";
            this.btnTempRun.UseVisualStyleBackColor = true;
            this.btnTempRun.Click += new System.EventHandler(this.btnTempRun_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 562);
            this.Controls.Add(this.btnTempRun);
            this.Controls.Add(this.lbTempDisasm);
            this.Name = "MainForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lbTempDisasm;
        private System.Windows.Forms.Button btnTempRun;
    }
}

