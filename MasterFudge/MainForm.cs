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
            InitializeComponent();

            string romFile = @"D:\ROMs\SMS\Hang-On_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\Sonic_the_Hedgehog_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\Y's_-_The_Vanished_Omen_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\VDPTEST.sms";

            ms = new MasterSystem(false);
            ms.LoadCartridge(romFile);
            ms.Run();

            //ShowRomInformation(ms, romFile);
        }

        private void ShowRomInformation(MasterSystem ms, string romFile)
        {
            RomHeader header = ms.GetCartridgeHeader();

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}\n\n", System.IO.Path.GetFileName(romFile));
            sb.AppendFormat("TMR SEGA string: '{0}'\n", header.TMRSEGAString);
            sb.AppendFormat("Reserved: [0x{0:X2}, 0x{1:X2}]\n", header.Reserved[0], header.Reserved[1]);
            sb.AppendFormat("Checksum: 0x{0:X4} (calculated 0x{1:X4}, {2})\n", header.Checksum, header.ChecksumCalculated, (header.Checksum == header.ChecksumCalculated ? "matches header" : "mismatch"));
            sb.AppendFormat("Product code: {0}\n", header.ProductCode);
            sb.AppendFormat("Version: {0}\n", header.Version);
            sb.AppendFormat("Region: {0}\n", header.GetRegionName());
            sb.AppendFormat("ROM size: {0} (file is {1} KB, {2})\n", header.GetRomSizeName(), (header.RomSizeCalculated / 1024), (header.IsRomSizeCorrect ? "matches header" : "mismatch"));
            MessageBox.Show(sb.ToString(), "ROM Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
