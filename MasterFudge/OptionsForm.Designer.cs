namespace MasterFudge
{
    partial class OptionsForm
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOkay = new System.Windows.Forms.Button();
            this.gbBootstrap = new System.Windows.Forms.GroupBox();
            this.btnBootstrapGGBrowse = new System.Windows.Forms.Button();
            this.lblBootstrapGG = new System.Windows.Forms.Label();
            this.txtBootstrapGG = new System.Windows.Forms.TextBox();
            this.btnBootstrapSMSBrowse = new System.Windows.Forms.Button();
            this.lblBootstrapSMS = new System.Windows.Forms.Label();
            this.txtBootstrapSMS = new System.Windows.Forms.TextBox();
            this.chkBootstrapEnable = new System.Windows.Forms.CheckBox();
            this.gbKeyConfig = new System.Windows.Forms.GroupBox();
            this.ofdBootstrapSMS = new System.Windows.Forms.OpenFileDialog();
            this.ofdBootstrapGG = new System.Windows.Forms.OpenFileDialog();
            this.lbKeyConfigButton = new System.Windows.Forms.ListBox();
            this.lbKeyConfigKeys = new System.Windows.Forms.ListBox();
            this.lblKeyConfigButtons = new System.Windows.Forms.Label();
            this.lblKeyConfigKeys = new System.Windows.Forms.Label();
            this.gbBootstrap.SuspendLayout();
            this.gbKeyConfig.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(397, 277);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOkay
            // 
            this.btnOkay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOkay.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOkay.Location = new System.Drawing.Point(316, 277);
            this.btnOkay.Name = "btnOkay";
            this.btnOkay.Size = new System.Drawing.Size(75, 23);
            this.btnOkay.TabIndex = 1;
            this.btnOkay.Text = "&OK";
            this.btnOkay.UseVisualStyleBackColor = true;
            // 
            // gbBootstrap
            // 
            this.gbBootstrap.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbBootstrap.Controls.Add(this.btnBootstrapGGBrowse);
            this.gbBootstrap.Controls.Add(this.lblBootstrapGG);
            this.gbBootstrap.Controls.Add(this.txtBootstrapGG);
            this.gbBootstrap.Controls.Add(this.btnBootstrapSMSBrowse);
            this.gbBootstrap.Controls.Add(this.lblBootstrapSMS);
            this.gbBootstrap.Controls.Add(this.txtBootstrapSMS);
            this.gbBootstrap.Location = new System.Drawing.Point(12, 12);
            this.gbBootstrap.Name = "gbBootstrap";
            this.gbBootstrap.Size = new System.Drawing.Size(460, 80);
            this.gbBootstrap.TabIndex = 2;
            this.gbBootstrap.TabStop = false;
            // 
            // btnBootstrapGGBrowse
            // 
            this.btnBootstrapGGBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBootstrapGGBrowse.Location = new System.Drawing.Point(424, 45);
            this.btnBootstrapGGBrowse.Name = "btnBootstrapGGBrowse";
            this.btnBootstrapGGBrowse.Size = new System.Drawing.Size(30, 20);
            this.btnBootstrapGGBrowse.TabIndex = 6;
            this.btnBootstrapGGBrowse.Text = "...";
            this.btnBootstrapGGBrowse.UseVisualStyleBackColor = true;
            this.btnBootstrapGGBrowse.Click += new System.EventHandler(this.btnBootstrapGGBrowse_Click);
            // 
            // lblBootstrapGG
            // 
            this.lblBootstrapGG.AutoSize = true;
            this.lblBootstrapGG.Location = new System.Drawing.Point(6, 48);
            this.lblBootstrapGG.Name = "lblBootstrapGG";
            this.lblBootstrapGG.Size = new System.Drawing.Size(64, 13);
            this.lblBootstrapGG.TabIndex = 5;
            this.lblBootstrapGG.Text = "Game Gear:";
            // 
            // txtBootstrapGG
            // 
            this.txtBootstrapGG.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBootstrapGG.Location = new System.Drawing.Point(100, 45);
            this.txtBootstrapGG.Name = "txtBootstrapGG";
            this.txtBootstrapGG.Size = new System.Drawing.Size(318, 20);
            this.txtBootstrapGG.TabIndex = 4;
            // 
            // btnBootstrapSMSBrowse
            // 
            this.btnBootstrapSMSBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBootstrapSMSBrowse.Location = new System.Drawing.Point(424, 19);
            this.btnBootstrapSMSBrowse.Name = "btnBootstrapSMSBrowse";
            this.btnBootstrapSMSBrowse.Size = new System.Drawing.Size(30, 20);
            this.btnBootstrapSMSBrowse.TabIndex = 3;
            this.btnBootstrapSMSBrowse.Text = "...";
            this.btnBootstrapSMSBrowse.UseVisualStyleBackColor = true;
            this.btnBootstrapSMSBrowse.Click += new System.EventHandler(this.btnBootstrapSMSBrowse_Click);
            // 
            // lblBootstrapSMS
            // 
            this.lblBootstrapSMS.AutoSize = true;
            this.lblBootstrapSMS.Location = new System.Drawing.Point(6, 22);
            this.lblBootstrapSMS.Name = "lblBootstrapSMS";
            this.lblBootstrapSMS.Size = new System.Drawing.Size(79, 13);
            this.lblBootstrapSMS.TabIndex = 2;
            this.lblBootstrapSMS.Text = "Master System:";
            // 
            // txtBootstrapSMS
            // 
            this.txtBootstrapSMS.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBootstrapSMS.Location = new System.Drawing.Point(100, 19);
            this.txtBootstrapSMS.Name = "txtBootstrapSMS";
            this.txtBootstrapSMS.Size = new System.Drawing.Size(318, 20);
            this.txtBootstrapSMS.TabIndex = 1;
            // 
            // chkBootstrapEnable
            // 
            this.chkBootstrapEnable.AutoSize = true;
            this.chkBootstrapEnable.Location = new System.Drawing.Point(21, 12);
            this.chkBootstrapEnable.Name = "chkBootstrapEnable";
            this.chkBootstrapEnable.Size = new System.Drawing.Size(126, 17);
            this.chkBootstrapEnable.TabIndex = 0;
            this.chkBootstrapEnable.Text = "Use Bootstrap ROMs";
            this.chkBootstrapEnable.UseVisualStyleBackColor = true;
            // 
            // gbKeyConfig
            // 
            this.gbKeyConfig.Controls.Add(this.lblKeyConfigKeys);
            this.gbKeyConfig.Controls.Add(this.lblKeyConfigButtons);
            this.gbKeyConfig.Controls.Add(this.lbKeyConfigKeys);
            this.gbKeyConfig.Controls.Add(this.lbKeyConfigButton);
            this.gbKeyConfig.Location = new System.Drawing.Point(12, 98);
            this.gbKeyConfig.Name = "gbKeyConfig";
            this.gbKeyConfig.Size = new System.Drawing.Size(260, 170);
            this.gbKeyConfig.TabIndex = 3;
            this.gbKeyConfig.TabStop = false;
            this.gbKeyConfig.Text = "Keyboard Configuration";
            // 
            // ofdBootstrapSMS
            // 
            this.ofdBootstrapSMS.Filter = "Sega Master System ROMs (*.sms)|*.sms|All Files (*.*)|*.*";
            // 
            // ofdBootstrapGG
            // 
            this.ofdBootstrapGG.Filter = "Sega Game Gear ROMs (*.gg)|*.gg|All Files (*.*)|*.*";
            // 
            // lbKeyConfigButton
            // 
            this.lbKeyConfigButton.FormattingEnabled = true;
            this.lbKeyConfigButton.IntegralHeight = false;
            this.lbKeyConfigButton.Location = new System.Drawing.Point(6, 35);
            this.lbKeyConfigButton.Name = "lbKeyConfigButton";
            this.lbKeyConfigButton.Size = new System.Drawing.Size(120, 120);
            this.lbKeyConfigButton.TabIndex = 0;
            this.lbKeyConfigButton.SelectedIndexChanged += new System.EventHandler(this.lbKeyConfigButton_SelectedIndexChanged);
            // 
            // lbKeyConfigKeys
            // 
            this.lbKeyConfigKeys.FormattingEnabled = true;
            this.lbKeyConfigKeys.IntegralHeight = false;
            this.lbKeyConfigKeys.Location = new System.Drawing.Point(132, 35);
            this.lbKeyConfigKeys.Name = "lbKeyConfigKeys";
            this.lbKeyConfigKeys.Size = new System.Drawing.Size(120, 120);
            this.lbKeyConfigKeys.TabIndex = 2;
            this.lbKeyConfigKeys.SelectedIndexChanged += new System.EventHandler(this.lbKeyConfigKeys_SelectedIndexChanged);
            // 
            // lblKeyConfigButtons
            // 
            this.lblKeyConfigButtons.AutoSize = true;
            this.lblKeyConfigButtons.Location = new System.Drawing.Point(6, 19);
            this.lblKeyConfigButtons.Name = "lblKeyConfigButtons";
            this.lblKeyConfigButtons.Size = new System.Drawing.Size(46, 13);
            this.lblKeyConfigButtons.TabIndex = 3;
            this.lblKeyConfigButtons.Text = "Buttons:";
            // 
            // lblKeyConfigKeys
            // 
            this.lblKeyConfigKeys.AutoSize = true;
            this.lblKeyConfigKeys.Location = new System.Drawing.Point(132, 19);
            this.lblKeyConfigKeys.Name = "lblKeyConfigKeys";
            this.lblKeyConfigKeys.Size = new System.Drawing.Size(33, 13);
            this.lblKeyConfigKeys.TabIndex = 4;
            this.lblKeyConfigKeys.Text = "Keys:";
            // 
            // OptionsForm
            // 
            this.AcceptButton = this.btnOkay;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(484, 312);
            this.Controls.Add(this.gbKeyConfig);
            this.Controls.Add(this.chkBootstrapEnable);
            this.Controls.Add(this.gbBootstrap);
            this.Controls.Add(this.btnOkay);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            this.gbBootstrap.ResumeLayout(false);
            this.gbBootstrap.PerformLayout();
            this.gbKeyConfig.ResumeLayout(false);
            this.gbKeyConfig.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOkay;
        private System.Windows.Forms.GroupBox gbBootstrap;
        private System.Windows.Forms.CheckBox chkBootstrapEnable;
        private System.Windows.Forms.Label lblBootstrapSMS;
        private System.Windows.Forms.TextBox txtBootstrapSMS;
        private System.Windows.Forms.Button btnBootstrapSMSBrowse;
        private System.Windows.Forms.Button btnBootstrapGGBrowse;
        private System.Windows.Forms.Label lblBootstrapGG;
        private System.Windows.Forms.TextBox txtBootstrapGG;
        private System.Windows.Forms.GroupBox gbKeyConfig;
        private System.Windows.Forms.OpenFileDialog ofdBootstrapSMS;
        private System.Windows.Forms.OpenFileDialog ofdBootstrapGG;
        private System.Windows.Forms.ListBox lbKeyConfigButton;
        private System.Windows.Forms.ListBox lbKeyConfigKeys;
        private System.Windows.Forms.Label lblKeyConfigKeys;
        private System.Windows.Forms.Label lblKeyConfigButtons;
    }
}