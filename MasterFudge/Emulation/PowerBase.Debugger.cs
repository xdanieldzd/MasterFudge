using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace MasterFudge.Emulation
{
    public partial class PowerBase
    {
        // TODO: more sensible or neater placement of this stuff?

        public class Debugging
        {
            public enum DumpRegion
            {
                WorkRam,
                VideoRam,
                ColorRam,
            }

            public static byte[] DumpMemory(PowerBase ms, DumpRegion memoryRegion)
            {
                switch (memoryRegion)
                {
                    case DumpRegion.WorkRam: return ms.wram.DumpWorkRam();
                    case DumpRegion.VideoRam: return ms.vdp.DumpVideoRam();
                    case DumpRegion.ColorRam: return ms.vdp.DumpColorRam();
                    default: throw new Exception("Invalid memory region for dumping");
                }
            }

            public static Color GetPaletteColor(PowerBase ms, int palette, int color)
            {
                return Color.FromArgb(BitConverter.ToInt32(ms.vdp.ConvertMasterSystemColor(palette, color), 0));
            }
        }
    }
}
