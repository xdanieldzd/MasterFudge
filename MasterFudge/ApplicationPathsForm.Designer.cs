namespace MasterFudge
{
    partial class ApplicationPathsForm
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
            this.btnOkay = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblSaveFilePath = new System.Windows.Forms.Label();
            this.txtSaveFilePath = new System.Windows.Forms.TextBox();
            this.lblScreenshotPath = new System.Windows.Forms.Label();
            this.txtScreenshotPath = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnOkay
            // 
            this.btnOkay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOkay.Location = new System.Drawing.Point(266, 77);
            this.btnOkay.Name = "btnOkay";
            this.btnOkay.Size = new System.Drawing.Size(75, 23);
            this.btnOkay.TabIndex = 0;
            this.btnOkay.Text = "&OK";
            this.btnOkay.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(347, 77);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lblSaveFilePath
            // 
            this.lblSaveFilePath.AutoSize = true;
            this.lblSaveFilePath.Location = new System.Drawing.Point(12, 15);
            this.lblSaveFilePath.Name = "lblSaveFilePath";
            this.lblSaveFilePath.Size = new System.Drawing.Size(59, 13);
            this.lblSaveFilePath.TabIndex = 5;
            this.lblSaveFilePath.Text = "Save Files:";
            // 
            // txtSaveFilePath
            // 
            this.txtSaveFilePath.Location = new System.Drawing.Point(122, 12);
            this.txtSaveFilePath.Name = "txtSaveFilePath";
            this.txtSaveFilePath.Size = new System.Drawing.Size(300, 20);
            this.txtSaveFilePath.TabIndex = 4;
            // 
            // lblScreenshotPath
            // 
            this.lblScreenshotPath.AutoSize = true;
            this.lblScreenshotPath.Location = new System.Drawing.Point(12, 41);
            this.lblScreenshotPath.Name = "lblScreenshotPath";
            this.lblScreenshotPath.Size = new System.Drawing.Size(69, 13);
            this.lblScreenshotPath.TabIndex = 7;
            this.lblScreenshotPath.Text = "Screenshots:";
            // 
            // txtScreenshotPath
            // 
            this.txtScreenshotPath.Location = new System.Drawing.Point(122, 38);
            this.txtScreenshotPath.Name = "txtScreenshotPath";
            this.txtScreenshotPath.Size = new System.Drawing.Size(300, 20);
            this.txtScreenshotPath.TabIndex = 6;
            // 
            // ApplicationPathsForm
            // 
            this.AcceptButton = this.btnOkay;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(434, 112);
            this.Controls.Add(this.lblScreenshotPath);
            this.Controls.Add(this.txtScreenshotPath);
            this.Controls.Add(this.lblSaveFilePath);
            this.Controls.Add(this.txtSaveFilePath);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOkay);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApplicationPathsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Paths";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOkay;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblSaveFilePath;
        private System.Windows.Forms.TextBox txtSaveFilePath;
        private System.Windows.Forms.Label lblScreenshotPath;
        private System.Windows.Forms.TextBox txtScreenshotPath;
    }
}