﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

using NAudio.Wave;
using NAudio.Utils;

using MasterFudge.Emulation;
using MasterFudge.Emulation.Cartridges;
using MasterFudge.Emulation.Graphics;

namespace MasterFudge
{
    public partial class MainForm : Form
    {
        Emulation.PowerBase emulator;
        TaskWrapper taskWrapper;
        Bitmap screenBitmap;
        WaveOut waveOut;

        Version programVersion;
        bool logEnabled;
        TextWriter logWriter;

        const float defaultVolume = 0.5f;

        public bool soundEnabled
        {
            get { return (waveOut?.Volume == defaultVolume); }
            set { waveOut.Volume = (value ? defaultVolume : 0.0f); }
        }

        public MainForm()
        {
            InitializeComponent();

            /* Create emulator instance & task wrapper */
            emulator = new PowerBase();
            emulator.OnRenderScreen += Emulator_OnRenderScreen;
            emulator.SetRegion(Properties.Settings.Default.BaseUnitRegion);
            emulator.LimitFPS = Properties.Settings.Default.LimitFPS;
            taskWrapper = new TaskWrapper();
            taskWrapper.Start(emulator);

            /* Create output instances */
            screenBitmap = new Bitmap(VDP.NumPixelsPerLine, VDP.NumVisibleLinesHigh, PixelFormat.Format32bppArgb);
            waveOut = new WaveOut();
            waveOut.Init(emulator.GetPSGWaveProvider());
            waveOut.Play();
            soundEnabled = Properties.Settings.Default.SoundEnabled;

            /* Misc variables */
            programVersion = new Version(Application.ProductVersion);

            /* Misc UI stuff */
            nTSCToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsNtscSystem");
            pALToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsPalSystem");
            exportToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsExportSystem");
            japaneseToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsJapaneseSystem");
            limitFPSToolStripMenuItem.DataBindings.Add("Checked", emulator, "LimitFPS");
            enableSoundToolStripMenuItem.DataBindings.Add("Checked", this, "soundEnabled");

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

            Properties.Settings.Default.Save();

            logWriter?.Close();

            // TODO: make menu options for dumps, I guess
            if (emulator != null)
            {
                try
                {
                    File.WriteAllBytes(@"E:\temp\sms\wram.bin", Emulation.PowerBase.Debugging.DumpMemory(emulator, Emulation.PowerBase.Debugging.DumpRegion.WorkRam));
                    File.WriteAllBytes(@"E:\temp\sms\vram.sms", Emulation.PowerBase.Debugging.DumpMemory(emulator, Emulation.PowerBase.Debugging.DumpRegion.VideoRam));
                    File.WriteAllBytes(@"E:\temp\sms\cram.bin", Emulation.PowerBase.Debugging.DumpMemory(emulator, Emulation.PowerBase.Debugging.DumpRegion.ColorRam));
                }
                catch (IOException) { /* just ignore this one, happens if I have any of these open in ex. a hexeditor */ }
            }
        }

        private void SetRegion(bool isNtsc, bool isExport)
        {
            BaseUnitRegion regionToSet;
            if (isExport)
            {
                if (isNtsc)
                    regionToSet = BaseUnitRegion.ExportNTSC;
                else
                    regionToSet = BaseUnitRegion.ExportPAL;
            }
            else
                regionToSet = BaseUnitRegion.JapanNTSC;

            emulator.SetRegion(regionToSet);
            Properties.Settings.Default.BaseUnitRegion = regionToSet;
        }

        private void LoadCartridge(string filename)
        {
            emulator.PowerOff();
            Program.Log.ClearEvents();

            emulator.LoadCartridge(filename);
            LogCartridgeInformation(emulator, filename);

            // TODO: make this better again, but SMS Chase HQ is set to GG in header...?
            if (Path.GetExtension(filename) == ".gg")
                emulator.SetUnitType(BaseUnitType.GameGear);
            else
                emulator.SetUnitType(BaseUnitType.MasterSystem);

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

            soundEnabled = false;

            string romFile = @"D:\ROMs\SMS\Hang-On_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\Sonic_the_Hedgehog_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Y's_-_The_Vanished_Omen_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\VDPTEST.sms";
            //romFile = @"D:\ROMs\SMS\[BIOS] Sega Master System (USA, Europe) (v1.3).sms";
            //romFile = @"D:\ROMs\SMS\Teddy_Boy_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\R-Type_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Alex_Kidd_in_Miracle_World_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Psycho_Fox_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\SMS Sound Test 1.1.sms";

            romFile = @"D:\ROMs\GG\Sonic_the_Hedgehog_(JUE).gg";
            romFile = @"D:\ROMs\GG\Gunstar_Heroes_(J).gg";

            LoadCartridge(romFile);
        }

        private void LogCartridgeInformation(Emulation.PowerBase ms, string romFile)
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
            Buttons button = Buttons.None;
            switch (e.KeyCode)
            {
                case Keys.Up: button = Buttons.Up; break;
                case Keys.Down: button = Buttons.Down; break;
                case Keys.Left: button = Buttons.Left; break;
                case Keys.Right: button = Buttons.Right; break;
                case Keys.A: button = Buttons.Button1; break;
                case Keys.S: button = Buttons.Button2; break;
                case Keys.Enter: button = Buttons.StartPause; break;
                case Keys.Back: button = Buttons.Reset; break;
            }
            emulator?.SetButtonData(button, 0, true);
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            Buttons button = Buttons.None;
            switch (e.KeyCode)
            {
                case Keys.Up: button = Buttons.Up; break;
                case Keys.Down: button = Buttons.Down; break;
                case Keys.Left: button = Buttons.Left; break;
                case Keys.Right: button = Buttons.Right; break;
                case Keys.A: button = Buttons.Button1; break;
                case Keys.S: button = Buttons.Button2; break;
                case Keys.Enter: button = Buttons.StartPause; break;
                case Keys.Back: button = Buttons.Reset; break;
            }
            emulator?.SetButtonData(button, 0, false);
        }

        private void nTSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRegion((sender as ToolStripMenuItem).Checked, emulator.IsExportSystem);
        }

        private void pALToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRegion(!(sender as ToolStripMenuItem).Checked, emulator.IsExportSystem);
        }

        private void japaneseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRegion(emulator.IsNtscSystem, !(sender as ToolStripMenuItem).Checked);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRegion(emulator.IsNtscSystem, (sender as ToolStripMenuItem).Checked);
        }

        private void limitFPSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.LimitFPS = emulator.LimitFPS = (sender as ToolStripMenuItem).Checked;
        }

        private void enableSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.SoundEnabled = soundEnabled = (sender as ToolStripMenuItem).Checked;
        }
    }
}
