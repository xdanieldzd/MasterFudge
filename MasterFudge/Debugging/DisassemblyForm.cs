﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MasterFudge.Emulation;
using MasterFudge.Emulation.CPU;

namespace MasterFudge.Debugging
{
    public partial class DisassemblyForm : Form
    {
        BaseUnit emulator;
        BaseUnit.CoreDebugSnapshot lastSnapshot;

        public DisassemblyForm(BaseUnit emulator)
        {
            InitializeComponent();

            this.emulator = emulator;
            lastSnapshot = BaseUnit.Debugging.GetCoreDebugSnapshot(emulator);

            disassemblyBox.UpdateControl(lastSnapshot);
            disassemblyBox.SetDisassemblyAddress(0x0000);

            csbRegisters.UpdateControl(lastSnapshot);
            csbStack.UpdateControl(lastSnapshot);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (chkTrace.Checked)
            {
                //TODO: make less performance-sucking, snapshot stuff is demanding

                lastSnapshot = BaseUnit.Debugging.GetCoreDebugSnapshot(emulator);
                disassemblyBox.UpdateControl(lastSnapshot);
                disassemblyBox.SetDisassemblyAddress(lastSnapshot.CPU.PC);
                csbRegisters.UpdateControl(lastSnapshot);
                csbStack.UpdateControl(lastSnapshot);
                Invalidate(true);
            }
        }
    }
}
