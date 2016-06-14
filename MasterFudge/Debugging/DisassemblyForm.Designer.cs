namespace MasterFudge.Debugging
{
    partial class DisassemblyForm
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
            this.components = new System.ComponentModel.Container();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.chkTrace = new System.Windows.Forms.CheckBox();
            this.csbRegisters = new MasterFudge.Controls.CPUStatusBox();
            this.csbStack = new MasterFudge.Controls.CPUStatusBox();
            this.disassemblyBox = new MasterFudge.Controls.DisassemblyBox();
            this.SuspendLayout();
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 50;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // chkTrace
            // 
            this.chkTrace.AutoSize = true;
            this.chkTrace.Location = new System.Drawing.Point(12, 320);
            this.chkTrace.Name = "chkTrace";
            this.chkTrace.Size = new System.Drawing.Size(96, 17);
            this.chkTrace.TabIndex = 1;
            this.chkTrace.Text = "&Trace Enabled";
            this.chkTrace.UseVisualStyleBackColor = true;
            // 
            // csbRegisters
            // 
            this.csbRegisters.BackColor = System.Drawing.SystemColors.Window;
            this.csbRegisters.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.csbRegisters.BoxType = MasterFudge.Controls.CPUStatusBoxType.Registers;
            this.csbRegisters.Font = new System.Drawing.Font("Courier New", 9F);
            this.csbRegisters.Location = new System.Drawing.Point(421, 12);
            this.csbRegisters.Name = "csbRegisters";
            this.csbRegisters.Size = new System.Drawing.Size(120, 305);
            this.csbRegisters.TabIndex = 3;
            // 
            // csbStack
            // 
            this.csbStack.BackColor = System.Drawing.SystemColors.Window;
            this.csbStack.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.csbStack.BoxType = MasterFudge.Controls.CPUStatusBoxType.Stack;
            this.csbStack.Font = new System.Drawing.Font("Courier New", 9F);
            this.csbStack.Location = new System.Drawing.Point(547, 12);
            this.csbStack.Name = "csbStack";
            this.csbStack.Size = new System.Drawing.Size(85, 305);
            this.csbStack.TabIndex = 2;
            // 
            // disassemblyBox
            // 
            this.disassemblyBox.BackColor = System.Drawing.SystemColors.Window;
            this.disassemblyBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.disassemblyBox.Font = new System.Drawing.Font("Courier New", 9F);
            this.disassemblyBox.Location = new System.Drawing.Point(12, 12);
            this.disassemblyBox.Margin = new System.Windows.Forms.Padding(0);
            this.disassemblyBox.Name = "disassemblyBox";
            this.disassemblyBox.Size = new System.Drawing.Size(400, 305);
            this.disassemblyBox.TabIndex = 0;
            // 
            // DisassemblyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(644, 362);
            this.Controls.Add(this.csbRegisters);
            this.Controls.Add(this.csbStack);
            this.Controls.Add(this.chkTrace);
            this.Controls.Add(this.disassemblyBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "DisassemblyForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Disassembly";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Controls.DisassemblyBox disassemblyBox;
        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.CheckBox chkTrace;
        private Controls.CPUStatusBox csbStack;
        private Controls.CPUStatusBox csbRegisters;
    }
}