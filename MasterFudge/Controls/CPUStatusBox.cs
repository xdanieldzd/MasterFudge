using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MasterFudge.Emulation;
using MasterFudge.Emulation.CPU;

namespace MasterFudge.Controls
{
    public enum CPUStatusBoxType
    {
        Registers,
        Stack
    }

    public partial class CPUStatusBox : UserControl, IDebuggerControl
    {
        BaseUnitOld.CoreDebugSnapshot snapshot;

        int visibleValues, lineHeight;

        public CPUStatusBoxType BoxType { get; set; }

        public CPUStatusBox()
        {
            InitializeComponent();

            Font = new Font(FontFamily.GenericMonospace, 9.0f);

            visibleValues = 0;
            lineHeight = (Font.Height + 1);
        }

        public void UpdateControl(BaseUnitOld.CoreDebugSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        protected override void OnResize(EventArgs e)
        {
            visibleValues = Height / (Font.Height + 1);

            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (snapshot != null)
            {
                switch (BoxType)
                {
                    case CPUStatusBoxType.Stack:
                        {
                            ushort currentSP = snapshot.CPU.SP;

                            int y = 0;
                            for (int i = 0; i < (visibleValues - 1); i++, y += lineHeight)
                            {
                                ushort stackPointer = (ushort)(currentSP - (i * 2));
                                ushort stackValue = snapshot.GetMemory16(stackPointer);
                                e.Graphics.DrawString(string.Format("{0:X4}:{1:X4}{2}", stackPointer, stackValue, (stackPointer == currentSP ? "■" : "")), Font, SystemBrushes.WindowText, 0, y);
                            }
                        }
                        break;

                    case CPUStatusBoxType.Registers:
                        {
                            int y = 0;

                            e.Graphics.DrawString(string.Format("AF:{0:X4} [{1:X4}]", snapshot.CPU.AF.Word, snapshot.CPU.AFShadow.Word), Font, SystemBrushes.WindowText, 0, y); y += lineHeight;
                            e.Graphics.DrawString(string.Format("BC:{0:X4} [{1:X4}]", snapshot.CPU.BC.Word, snapshot.CPU.BCShadow.Word), Font, SystemBrushes.WindowText, 0, y); y += lineHeight;
                            e.Graphics.DrawString(string.Format("DE:{0:X4} [{1:X4}]", snapshot.CPU.DE.Word, snapshot.CPU.DEShadow.Word), Font, SystemBrushes.WindowText, 0, y); y += lineHeight;
                            e.Graphics.DrawString(string.Format("HL:{0:X4} [{1:X4}]", snapshot.CPU.HL.Word, snapshot.CPU.HLShadow.Word), Font, SystemBrushes.WindowText, 0, y); y += lineHeight;
                            e.Graphics.DrawString(string.Format("IX:{0:X4} I:{1:X2}", snapshot.CPU.IX.Word, snapshot.CPU.I), Font, SystemBrushes.WindowText, 0, y); y += lineHeight;
                            e.Graphics.DrawString(string.Format("IY:{0:X4} R:{1:X2}", snapshot.CPU.IY.Word, snapshot.CPU.R), Font, SystemBrushes.WindowText, 0, y); y += lineHeight;
                            e.Graphics.DrawString(string.Format("SP:{0:X4} PC:{1:X4}", snapshot.CPU.SP, snapshot.CPU.PC), Font, SystemBrushes.WindowText, 0, y); y += lineHeight;
                            e.Graphics.DrawString(string.Format("{0} {1} {2} IM{3:D1}",
                                (snapshot.CPU.IFF1 ? "IF1" : "---"),
                                (snapshot.CPU.IFF2 ? "IF2" : "---"),
                                (snapshot.CPU.Halted ? "HLT" : "---"),
                                snapshot.CPU.InterruptMode),
                                Font, SystemBrushes.ControlDark, 0, y);
                            y += lineHeight;

                            e.Graphics.DrawString(snapshot.CPU.GetFlagsString(), Font, SystemBrushes.ControlDark, 0, y); y += lineHeight;
                        }
                        break;
                }

            }
            else
                e.Graphics.DrawString("Warning: snapshot is null", Font, Brushes.DarkRed, 0, 0);

            base.OnPaint(e);
        }
    }
}
