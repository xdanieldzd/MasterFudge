using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation
{
    public enum DebugMemoryRegion
    {
        WorkRam,
        VideoRam,
        ColorRam,
    }

    public partial class MasterSystem
    {
        public byte[] DumpMemory(DebugMemoryRegion memoryRegion)
        {
            switch (memoryRegion)
            {
                case DebugMemoryRegion.WorkRam: return wram.DumpWorkRam();
                case DebugMemoryRegion.VideoRam: return vdp.DumpVideoRam();
                case DebugMemoryRegion.ColorRam: return vdp.DumpColorRam();
                default: throw new Exception("Invalid memory region for dumping");
            }
        }
    }
}
