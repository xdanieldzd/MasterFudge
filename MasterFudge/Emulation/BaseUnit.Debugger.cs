using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using MasterFudge.Emulation.CPU;

namespace MasterFudge.Emulation
{
    public partial class BaseUnit
    {
        // TODO: more sensible or neater placement of this stuff?

        public partial class Debugging
        {
            public enum DumpRegion
            {
                WorkRam,
                VideoRam,
                ColorRam,
            }

            public static byte[] DumpMemory(BaseUnit emulator, DumpRegion memoryRegion)
            {
                switch (memoryRegion)
                {
                    case DumpRegion.WorkRam: return emulator.wram;
                    case DumpRegion.VideoRam: return emulator.vdp.DumpVideoRam();
                    case DumpRegion.ColorRam: return emulator.vdp.DumpColorRam();
                    default: throw new Exception("Invalid memory region for dumping");
                }
            }

            public static Color GetPaletteColor(BaseUnit emulator, int palette, int color)
            {
                return Color.FromArgb(BitConverter.ToInt32(emulator.vdp.ConvertMasterSystemColor(palette, color), 0));
            }

            public static Z80 GetCPUInstance(BaseUnit emulator)
            {
                return emulator.cpu;
            }
        }
    }
}
