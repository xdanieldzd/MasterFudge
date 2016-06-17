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
    public partial class DisassemblyBox : UserControl, IDebuggerControl
    {
        static readonly StringFormat stringFormatSpaces = new StringFormat() { FormatFlags = StringFormatFlags.MeasureTrailingSpaces };

        BaseUnitOld.CoreDebugSnapshot snapshot;
        ushort memoryAddress, endAddress, topOpcodeLength;

        int visibleOps, lineHeight;
        float[] xPositions;

        public ushort CurrentAddress { get { return memoryAddress; } }

        public DisassemblyBox()
        {
            InitializeComponent();

            Font = new Font(FontFamily.GenericMonospace, 9.0f);

            snapshot = null;
            memoryAddress = endAddress = topOpcodeLength = 0x0000;

            visibleOps = 0;
            lineHeight = (Font.Height + 1);

            xPositions = null;
        }

        public void UpdateControl(BaseUnitOld.CoreDebugSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public void SetDisassemblyAddress(ushort address)
        {
            vsbAddress.Value = memoryAddress = address;
        }

        protected override void OnResize(EventArgs e)
        {
            visibleOps = Height / (Font.Height + 1);
            vsbAddress.LargeChange = visibleOps;

            base.OnResize(e);
        }

        private void pbOpcodes_Paint(object sender, PaintEventArgs e)
        {
            if (snapshot != null)
            {
                ushort currentPC = snapshot.CPU.PC;
                ushort tempAddress = memoryAddress;

                if (xPositions == null)
                {
                    xPositions = new float[3];
                    xPositions[0] = 16;
                    xPositions[1] = xPositions[0] + e.Graphics.MeasureString(string.Empty.PadRight(9), Font, -1, stringFormatSpaces).Width;
                    xPositions[2] = xPositions[1] + e.Graphics.MeasureString(string.Empty.PadRight(13), Font, -1, stringFormatSpaces).Width;
                }

                e.Graphics.FillRectangle(Brushes.WhiteSmoke, 0, 0, xPositions[0], Height);
                e.Graphics.DrawLine(SystemPens.WindowText, xPositions[0], 0, xPositions[0], Height);

                for (int i = 0, y = 0; i < (visibleOps + 1); i++, y += lineHeight)
                {
                    byte[] opcodeBytes = snapshot.CPU.GetOpcodeBytes(tempAddress);

                    string addressString = string.Format("{0:X2}:{1:X4}", 0, tempAddress);
                    string byteString = snapshot.CPU.GetOpcodeBytesString(opcodeBytes);
                    string mnemonicString = snapshot.CPU.GetOpcodesMnemonicString(opcodeBytes);

                    Brush bytesBrush = (currentPC == tempAddress ? Brushes.SteelBlue : SystemBrushes.ControlDark);
                    Brush mainBrush = (currentPC == tempAddress ? Brushes.MediumBlue : SystemBrushes.WindowText);
                    {
                        e.Graphics.DrawString(addressString, Font, mainBrush, xPositions[0], y);
                        e.Graphics.DrawString(byteString, Font, bytesBrush, xPositions[1], y);
                        e.Graphics.DrawString(mnemonicString, Font, mainBrush, xPositions[2], y);
                    }

                    if (currentPC == tempAddress)
                    {
                        e.Graphics.DrawLine(SystemPens.WindowText, xPositions[0], y, Width, y);
                        e.Graphics.DrawLine(SystemPens.WindowText, xPositions[0], y + lineHeight, Width, y + lineHeight);

                        if (true)
                        {
                            PointF[] points = new PointF[]
                            {
                                new PointF(4.0f, y),
                                new PointF(12.0f, (y + (lineHeight / 2.0f))),
                                new PointF(4.0f, (y + lineHeight))
                            };
                            e.Graphics.FillPolygon(Brushes.Green, points);
                        }
                        else
                        {
                            e.Graphics.FillRectangle(Brushes.Blue, 3.0f, (y + 2.0f), 4.0f, (lineHeight - 2.0f));
                            e.Graphics.FillRectangle(Brushes.Blue, 9.0f, (y + 2.0f), 4.0f, (lineHeight - 2.0f));
                        }
                    }

                    if (i == 0) topOpcodeLength = (ushort)opcodeBytes.Length;
                    tempAddress += (ushort)opcodeBytes.Length;
                }

                endAddress = tempAddress;
            }
            else
                e.Graphics.DrawString("Warning: snapshot is null", Font, Brushes.DarkRed, 0, 0);
        }

        private void vsbAddress_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.EndScroll || e.Type == ScrollEventType.ThumbPosition || e.NewValue == e.OldValue) return;

            if (e.NewValue <= 0) memoryAddress = 0;
            else if (e.NewValue > 0xFFFF) memoryAddress = 0xFFFF;
            else if (e.Type == ScrollEventType.SmallDecrement) memoryAddress = (ushort)(memoryAddress - 1);
            else if (e.Type == ScrollEventType.LargeDecrement) memoryAddress = (ushort)(memoryAddress - (endAddress - memoryAddress));
            else if (e.Type == ScrollEventType.SmallIncrement) memoryAddress = (ushort)(memoryAddress + topOpcodeLength);
            else if (e.Type == ScrollEventType.LargeIncrement) memoryAddress = endAddress;
            else memoryAddress = (ushort)e.NewValue;

            pbOpcodes.Invalidate();
        }
    }
}
