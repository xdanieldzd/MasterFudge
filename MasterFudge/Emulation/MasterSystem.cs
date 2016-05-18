using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MasterFudge.Emulation.Memory;
using MasterFudge.Emulation.Cartridges;

namespace MasterFudge.Emulation
{
    public class MasterSystem
    {
        MemoryMapper memoryMapper;

        Z80 cpu;
        WRAM wram;
        BaseCartridge cartridge;

        public MasterSystem()
        {
            memoryMapper = new MemoryMapper();

            cpu = new Z80();
            wram = new WRAM();

            memoryMapper.AddMemoryArea(wram.GetMemoryAreaDescriptor());
        }

        public void LoadCartridge(string filename)
        {
            cartridge = BaseCartridge.LoadCartridge<BaseCartridge>(filename);
            memoryMapper.AddMemoryArea(cartridge.GetMemoryAreaDescriptor());
        }

        public RomHeader GetCartridgeHeader()
        {
            return cartridge.Header;
        }
    }
}
