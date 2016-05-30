using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

using MasterFudge.Emulation;
using MasterFudge.Emulation.Cartridges;
using MasterFudge.Emulation.Graphics;

namespace MasterFudge
{
    public partial class MainForm : Form
    {
        MasterSystem emulator;
        TaskWrapper taskWrapper;
        Bitmap screenBitmap;

        Version programVersion;
        bool logEnabled;
        TextWriter logWriter;

        public MainForm()
        {
            InitializeComponent();

            /* Create task wrapper & SMS instance */
            emulator = new MasterSystem();
            emulator.OnRenderScreen += Emulator_OnRenderScreen;
            emulator.SetRegion(true, true);
            taskWrapper = new TaskWrapper();
            taskWrapper.Start(emulator);
            screenBitmap = new Bitmap(VDP.NumPixelsPerLine, VDP.NumVisibleLinesHigh, PixelFormat.Format32bppArgb);

            /* Misc variables */
            programVersion = new Version(Application.ProductVersion);

            /* Misc UI stuff */
            nTSCToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsNtscSystem");
            pALToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsPalSystem");
            exportToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsExportSystem");
            japaneseToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsJapaneseSystem");
            limitFPSToolStripMenuItem.DataBindings.Add("Checked", emulator, "LimitFPS");

            SetFormTitle();
            tsslStatus.Text = "Ready";

            /* Logging stuff */
            logEnabled = false;
            logWriter = null;

            Program.Log.OnLogUpdate += new Logger.LogUpdateHandler((s, ev) =>
            {
                if (logWriter != null && !Disposing)
                {
                    if (InvokeRequired)
                        Invoke(new Action(() => { logWriter.WriteLine(ev.Message); }));
                    else
                        logWriter.WriteLine(ev.Message);
                }
            });
            Program.Log.OnLogCleared += new EventHandler((s, ev) => { logWriter?.Flush(); });

            /* Autostart ROM when debugging thingy */
            DebugLoadRomShim();
        }

        private void SetFormTitle()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} v{1}.{2}", Application.ProductName, programVersion.Major, programVersion.Minor);
            if (programVersion.Build != 0) builder.AppendFormat(".{0}", programVersion.Build);

            if (emulator.CartridgeLoaded)
                builder.AppendFormat(" - [{0}]", Path.GetFileName(emulator.CartridgeFilename));

            Text = builder.ToString();
        }

        private void ResizeWindowByOutput()
        {
            ClientSize = new Size(pbRenderOutput.Width * 2, (pbRenderOutput.Height * 2) + menuStrip.Height);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            emulator.OnRenderScreen -= Emulator_OnRenderScreen;

            emulator?.PowerOff();
            taskWrapper.Stop();

            logWriter?.Close();

            // TODO: make menu options for dumps, I guess
            if (emulator != null)
            {
                try
                {
                    File.WriteAllBytes(@"E:\temp\sms\wram.bin", MasterSystem.Debugging.DumpMemory(emulator, MasterSystem.Debugging.DumpRegion.WorkRam));
                    File.WriteAllBytes(@"E:\temp\sms\vram.sms", MasterSystem.Debugging.DumpMemory(emulator, MasterSystem.Debugging.DumpRegion.VideoRam));
                    File.WriteAllBytes(@"E:\temp\sms\cram.bin", MasterSystem.Debugging.DumpMemory(emulator, MasterSystem.Debugging.DumpRegion.ColorRam));
                }
                catch (IOException) { /* just ignore this one, happens if I have any of these open in ex. a hexeditor */ }
            }
        }

        private void LoadCartridge(string filename)
        {
            Program.Log.ClearEvents();

            emulator.PowerOff();
            emulator.LoadCartridge(filename);
            LogCartridgeInformation(emulator, filename);

            SetFormTitle();
            tsslStatus.Text = string.Format("Cartridge '{0}' loaded", Path.GetFileName(filename));
            cartridgeInformationToolStripMenuItem.Enabled = true;

            Program.Log.WriteEvent("--- STARTING EMULATION ---");
            emulator.PowerOn();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DebugLoadRomShim()
        {
            if (Environment.MachineName != "NANAMI-X") return;

            string romFile = @"D:\ROMs\SMS\Hang-On_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Sonic_the_Hedgehog_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Y's_-_The_Vanished_Omen_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\VDPTEST.sms";
            //romFile = @"D:\ROMs\SMS\[BIOS] Sega Master System (USA, Europe) (v1.3).sms";
            //romFile = @"D:\ROMs\SMS\Teddy_Boy_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\R-Type_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Alex_Kidd_in_Miracle_World_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\Psycho_Fox_(UE)_[!].sms";

            LoadCartridge(romFile);
        }

        private void LogCartridgeInformation(MasterSystem ms, string romFile)
        {
            Program.Log.WriteEvent("--- ROM INFORMATION ---");

            foreach (string line in GetCartridgeInformation())
                Program.Log.WriteEvent(line);
        }

        private string[] GetCartridgeInformation()
        {
            RomHeader header = emulator.GetCartridgeHeader();

            List<string> lines = new List<string>();
            lines.Add(string.Format("Filename: {0}", Path.GetFileName(emulator.CartridgeFilename)));
            lines.Add(string.Format("TMR SEGA string: '{0}'", header.TMRSEGAString));
            lines.Add(string.Format("Reserved: [0x{0:X2}, 0x{1:X2}]", header.Reserved[0], header.Reserved[1]));
            lines.Add(string.Format("Checksum: 0x{0:X4} (calculated 0x{1:X4}, {2})", header.Checksum, header.ChecksumCalculated, (header.Checksum == header.ChecksumCalculated ? "matches header" : "mismatch")));
            lines.Add(string.Format("Product code: {0}", header.ProductCode));
            lines.Add(string.Format("Version: {0}", header.Version));
            lines.Add(string.Format("Region: {0}", header.GetRegionName()));
            lines.Add(string.Format("ROM size: {0} (file is {1} KB, {2})", header.GetRomSizeName(), (header.RomSizeCalculated / 1024), (header.IsRomSizeCorrect ? "matches header" : "mismatch")));
            return lines.ToArray();
        }

        private void Emulator_OnRenderScreen(object sender, RenderEventArgs e)
        {
            try
            {
                if (IsHandleCreated && !Disposing)
                {
                    if (InvokeRequired)
                        Invoke(new Action<RenderEventArgs>(RenderScreen), e);
                    else
                        RenderScreen(e);
                }
            }
            catch (ObjectDisposedException) { /* meh, maybe fix later */ }
        }

        private void RenderScreen(RenderEventArgs e)
        {
            BitmapData bmpData = screenBitmap.LockBits(new Rectangle(0, 0, screenBitmap.Width, screenBitmap.Height), ImageLockMode.WriteOnly, screenBitmap.PixelFormat);

            byte[] pixelData = new byte[bmpData.Stride * bmpData.Height];
            Buffer.BlockCopy(e.FrameData, 0, pixelData, 0, pixelData.Length);

            Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);

            screenBitmap.UnlockBits(bmpData);

            pbRenderOutput.Invalidate();
        }

        private void openCartridgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ofdOpenCartridge.InitialDirectory = Path.GetDirectoryName(emulator.CartridgeFilename);
            ofdOpenCartridge.FileName = Path.GetFileName(emulator.CartridgeFilename);

            if (ofdOpenCartridge.ShowDialog() == DialogResult.OK)
            {
                LoadCartridge(ofdOpenCartridge.FileName);
            }
        }

        private void cartridgeInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string line in GetCartridgeInformation()) builder.AppendLine(line);

            MessageBox.Show(builder.ToString(), "Cartridge Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            taskWrapper.Stop();
            Application.Exit();
        }

        private void enableLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logEnabled = (sender as ToolStripMenuItem).Checked;

            if (logEnabled && logWriter == null)
            {
                // TODO: path selection? or just dump into executable or ROM folder?
                logWriter = new StreamWriter(@"E:\temp\sms\log.txt", false);
            }
        }

        private void logOpcodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulator.DebugLogOpcodes = (sender as ToolStripMenuItem).Checked;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();

            Version programVersion = new Version(Application.ProductVersion);
            builder.AppendFormat("{0} v{1}.{2}", Application.ProductName, programVersion.Major, programVersion.Minor);
            if (programVersion.Build != 0) builder.AppendFormat(".{0}", programVersion.Build);
            builder.Append(" - ");
            builder.AppendLine((Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute).Description);
            builder.AppendLine();
            builder.AppendLine((Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute).Copyright);

            MessageBox.Show(builder.ToString(), "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void pbRenderOutput_Paint(object sender, PaintEventArgs e)
        {
            if (screenBitmap != null)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                e.Graphics.DrawImage(screenBitmap, (sender as PictureBox).ClientRectangle, new Rectangle(0, 0, screenBitmap.Width, screenBitmap.Height), GraphicsUnit.Pixel);
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            byte keyBit = 0;
            switch (e.KeyCode)
            {
                case Keys.Up: keyBit = (1 << 0); break;         //Up
                case Keys.Down: keyBit = (1 << 1); break;       //Down
                case Keys.Left: keyBit = (1 << 2); break;       //Left
                case Keys.Right: keyBit = (1 << 3); break;      //Right
                case Keys.A: keyBit = (1 << 4); break;          //Button1
                case Keys.S: keyBit = (1 << 5); break;          //Button2
            }

            if (keyBit != 0)
                emulator?.SetJoypadPressed(keyBit);
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            byte keyBit = 0;
            switch (e.KeyCode)
            {
                case Keys.Up: keyBit = (1 << 0); break;         //Up
                case Keys.Down: keyBit = (1 << 1); break;       //Down
                case Keys.Left: keyBit = (1 << 2); break;       //Left
                case Keys.Right: keyBit = (1 << 3); break;      //Right
                case Keys.A: keyBit = (1 << 4); break;          //Button1
                case Keys.S: keyBit = (1 << 5); break;          //Button2
            }
            if (keyBit != 0)
                emulator?.SetJoypadReleased(keyBit);
        }

        private void nTSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulator.SetRegion((sender as ToolStripMenuItem).Checked, emulator.IsExportSystem);
        }

        private void pALToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulator.SetRegion(!(sender as ToolStripMenuItem).Checked, emulator.IsExportSystem);
        }

        private void japaneseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulator.SetRegion(emulator.IsNtscSystem, !(sender as ToolStripMenuItem).Checked);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulator.SetRegion(emulator.IsNtscSystem, (sender as ToolStripMenuItem).Checked);
        }
    }
}
