using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using MasterFudge.Emulation;
using MasterFudge.Emulation.Cartridges;

namespace MasterFudge
{
    public partial class MainForm : Form
    {
        // TODO: lots of kludges & debug crap, fix or remove once unneccesary! also fix threading and related shit! sooo much to do beyond just the emulation...

        MasterSystem ms;
        Bitmap screenBitmap, paletteBitmap;
        int zoom;

        static string romFile;

        public MainForm()
        {
            InitializeComponent();

            Text = Application.ProductName;

            zoom = 1;

            // TODO: remove eventually, or fix up somehow
            System.IO.TextWriter writer = new System.IO.StreamWriter(@"E:\temp\sms\log.txt");
            FormClosing += ((s, ev) =>
            {
                ms?.Stop();

                if (IsHandleCreated && !Disposing)
                {
                    if (InvokeRequired)
                        Invoke(new Action(() => { writer?.Close(); }));
                    else
                        writer?.Close();
                }

                if (ms != null)
                {
                    try
                    {
                        System.IO.File.WriteAllBytes(@"E:\temp\sms\wram.bin", MasterSystem.Debugging.DumpMemory(ms, MasterSystem.Debugging.DumpRegion.WorkRam));
                        System.IO.File.WriteAllBytes(@"E:\temp\sms\vram.sms", MasterSystem.Debugging.DumpMemory(ms, MasterSystem.Debugging.DumpRegion.VideoRam));
                        System.IO.File.WriteAllBytes(@"E:\temp\sms\cram.bin", MasterSystem.Debugging.DumpMemory(ms, MasterSystem.Debugging.DumpRegion.ColorRam));
                    }
                    catch (System.IO.IOException) { /* just ignore this one, happens if I have any of these open in ex. a hexeditor */ }
                }
            });

            limitFPSToolStripMenuItem.CheckedChanged += ((s, ev) =>
            {
                if (ms == null) return;
                ms.LimitFPS = (s as ToolStripMenuItem).Checked;
            });

            logOpcodesToolStripMenuItem.CheckedChanged += ((s, ev) =>
            {
                if (ms == null) return;
                ms.DebugLogOpcodes = (s as ToolStripMenuItem).Checked;
            });

            Application.Idle += ((s, ev) => { pbTempDisplay.Invalidate(); pbTempPalette.Invalidate(); });
            pbTempDisplay.Paint += ((s, ev) =>
            {
                if (screenBitmap != null)
                {
                    ev.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    ev.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    ev.Graphics.DrawImage(screenBitmap, new Rectangle(0, 0, screenBitmap.Width * zoom, screenBitmap.Height * zoom), new Rectangle(0, 0, screenBitmap.Width, screenBitmap.Height), GraphicsUnit.Pixel);
                }
            });
            pbTempPalette.Paint += ((s, ev) => { if (paletteBitmap != null) ev.Graphics.DrawImageUnscaled(paletteBitmap, 0, 0); });

            Program.Log.OnLogUpdate += new Logger.LogUpdateHandler((s, ev) =>
            {
                if (IsHandleCreated && !Disposing)
                {
                    if (InvokeRequired)
                        Invoke(new Action(() => { writer.WriteLine(ev.Message); }));
                    else
                        writer.WriteLine(ev.Message);
                }
            });
            Program.Log.OnLogCleared += new EventHandler((s, ev) => { writer?.Flush(); });

            romFile = @"D:\ROMs\SMS\Hang-On_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\Sonic_the_Hedgehog_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Y's_-_The_Vanished_Omen_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\VDPTEST.sms";
            //romFile = @"D:\ROMs\SMS\[BIOS] Sega Master System (USA, Europe) (v1.3).sms";
            //romFile = @"D:\ROMs\SMS\Teddy_Boy_(UE)_[!].sms";

            DebugLoadRomShim();
        }

        private void LogRomInformation(MasterSystem ms, string romFile)
        {
            RomHeader header = ms.GetCartridgeHeader();

            Program.Log.WriteEvent("--- ROM INFORMATION ---");
            Program.Log.WriteEvent("Filename: {0}", System.IO.Path.GetFileName(romFile));
            Program.Log.WriteEvent("TMR SEGA string: '{0}'", header.TMRSEGAString);
            Program.Log.WriteEvent("Reserved: [0x{0:X2}, 0x{1:X2}]", header.Reserved[0], header.Reserved[1]);
            Program.Log.WriteEvent("Checksum: 0x{0:X4} (calculated 0x{1:X4}, {2})", header.Checksum, header.ChecksumCalculated, (header.Checksum == header.ChecksumCalculated ? "matches header" : "mismatch"));
            Program.Log.WriteEvent("Product code: {0}", header.ProductCode);
            Program.Log.WriteEvent("Version: {0}", header.Version);
            Program.Log.WriteEvent("Region: {0}", header.GetRegionName());
            Program.Log.WriteEvent("ROM size: {0} (file is {1} KB, {2})", header.GetRomSizeName(), (header.RomSizeCalculated / 1024), (header.IsRomSizeCorrect ? "matches header" : "mismatch"));
            Program.Log.WriteEvent(string.Empty);
        }

        private void openROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ofdOpenRom.InitialDirectory = System.IO.Path.GetDirectoryName(romFile);
            ofdOpenRom.FileName = System.IO.Path.GetFileName(romFile);

            if (ofdOpenRom.ShowDialog() == DialogResult.OK)
            {
                LoadRom(ofdOpenRom.FileName);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DebugLoadRomShim()
        {
            if (Environment.MachineName != "NANAMI-X") return;
            LoadRom(romFile);
        }

        private void LoadRom(string filename)
        {
            Text = Application.ProductName + " - " + System.IO.Path.GetFileName(filename);

            Program.Log.ClearEvents();

            ms = new MasterSystem(false, Emulation_OnRenderScreen);
            ms.LoadCartridge(filename);

            ms.DebugLogOpcodes = logOpcodesToolStripMenuItem.Checked;
            ms.LimitFPS = limitFPSToolStripMenuItem.Checked;

            LogRomInformation(ms, filename);

            Program.Log.WriteEvent("--- STARTING EMULATION ---");
            ms.Run();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ms?.Stop();
            Application.Exit();
        }

        private void Emulation_OnRenderScreen(object sender, RenderEventArgs e)
        {
            if (IsHandleCreated && !Disposing)
            {
                if (InvokeRequired)
                    Invoke(new Action<RenderEventArgs>(RenderScreen), e);
                else
                    RenderScreen(e);
            }
        }

        private void RenderScreen(RenderEventArgs e)
        {
            // TODO: make this much more safe

            if (screenBitmap == null || screenBitmap.Width != e.FrameWidth || screenBitmap.Height != e.FrameHeight)
            {
                screenBitmap?.Dispose();
                screenBitmap = new Bitmap(e.FrameWidth, e.FrameHeight, PixelFormat.Format32bppArgb);
            }

            BitmapData bmpData = screenBitmap.LockBits(new Rectangle(0, 0, screenBitmap.Width, screenBitmap.Height), ImageLockMode.WriteOnly, screenBitmap.PixelFormat);
            Marshal.Copy(e.FrameData, 0, bmpData.Scan0, (e.FrameWidth * e.FrameHeight * 4));
            screenBitmap.UnlockBits(bmpData);

            if (paletteBitmap == null) paletteBitmap = new Bitmap(128, 256);

            using (Graphics g = Graphics.FromImage(paletteBitmap))
            {
                for (int p = 0; p < 2; p++)
                {
                    for (int c = 0; c < 16; c++)
                    {
                        using (SolidBrush brush = new SolidBrush(MasterSystem.Debugging.GetPaletteColor(ms, p, c)))
                        {
                            g.FillRectangle(brush, p * 64, c * 16, 64, 16);
                        }
                    }
                }
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            byte keyBit = 0;
            switch (e.KeyCode)
            {
                case Keys.NumPad8: keyBit = (1 << 0); break;    //Up
                case Keys.NumPad2: keyBit = (1 << 1); break;    //Down
                case Keys.NumPad4: keyBit = (1 << 2); break;    //Left
                case Keys.NumPad6: keyBit = (1 << 3); break;    //Right
                case Keys.A: keyBit = (1 << 4); break;          //Button1
                case Keys.S: keyBit = (1 << 5); break;          //Button2
            }

            if (keyBit != 0)
                ms?.SetJoypadPressed(keyBit);
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            byte keyBit = 0;
            switch (e.KeyCode)
            {
                case Keys.NumPad8: keyBit = (1 << 0); break;    //Up
                case Keys.NumPad2: keyBit = (1 << 1); break;    //Down
                case Keys.NumPad4: keyBit = (1 << 2); break;    //Left
                case Keys.NumPad6: keyBit = (1 << 3); break;    //Right
                case Keys.A: keyBit = (1 << 4); break;          //Button1
                case Keys.S: keyBit = (1 << 5); break;          //Button2
            }
            if (keyBit != 0)
                ms?.SetJoypadReleased(keyBit);
        }
    }
}
