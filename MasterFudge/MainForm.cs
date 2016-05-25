using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MasterFudge.Emulation;
using MasterFudge.Emulation.Cartridges;

namespace MasterFudge
{
    public partial class MainForm : Form
    {
        MasterSystem ms;

        public MainForm()
        {
            if (false)
            {
                StringBuilder tmp = new StringBuilder();
                for (int i = 0; i < 256; i++)
                {
                    if ((i % 8) == 0) tmp.Append("            ");
                    tmp.AppendFormat("\".DB 0xED, 0x{0:X2}\",   ", i);
                    if (((i + 1) % 8) == 0) tmp.AppendFormat("/* 0x{0:X2} */\n", i - 7);
                }
                Clipboard.SetText(tmp.ToString());
            }

            if (false)
            {
                StringBuilder tmp = new StringBuilder();
                for (int i = 0; i < 256; i++)
                {
                    byte port = (byte)(i & 0xC1);
                    tmp.AppendFormat("raw 0x{0:X2}, masked 0x{1:X2}\n", i, port);
                }
                Clipboard.SetText(tmp.ToString());
            }

            InitializeComponent();

            Text = Application.ProductName;

            Program.Log.OnLogUpdate += new Logger.LogUpdateHandler((s, ev) =>
            {
                if (lbTempDisasm.IsHandleCreated)
                    lbTempDisasm.Invoke(new Action(() => { lbTempDisasm.Items.Add(ev.Message); lbTempDisasm.TopIndex = lbTempDisasm.Items.Count - 1; }));
            });

            Program.Log.OnLogCleared += new EventHandler((s, ev) =>
            {
                if (lbTempDisasm.IsHandleCreated)
                    lbTempDisasm.Invoke(new Action(() => lbTempDisasm.Items.Clear()));
            });
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

        private void btnTempRun_Click(object sender, EventArgs e)
        {
            Program.Log.ClearEvents();

            string romFile = @"D:\ROMs\SMS\Hang-On_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Sonic_the_Hedgehog_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Y's_-_The_Vanished_Omen_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\VDPTEST.sms";
            //romFile = @"D:\ROMs\SMS\[BIOS] Sega Master System (USA, Europe) (v1.3).sms";

            ms = new MasterSystem(false);
            ms.LoadCartridge(romFile);

            LogRomInformation(ms, romFile);

            Program.Log.WriteEvent("--- STARTING EMULATION ---");
            ms.Run();
        }

        private void btnTempPause_Click(object sender, EventArgs e)
        {
            if (!ms.IsPaused)
            {
                ms.Pause();
                (sender as Button).Text = "Resume";
            }
            else
            {
                ms.Resume();
                (sender as Button).Text = "Pause";
            }
        }
    }
}
