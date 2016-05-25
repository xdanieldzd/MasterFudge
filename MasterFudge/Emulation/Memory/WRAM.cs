using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Memory
{
    public class WRAM : SimpleMemoryArea
    {
        byte[] workRam;

        public WRAM()
        {
            workRam = new byte[0x2000];
        }

        public override ushort GetStartAddress()
        {
            return 0xC000;
        }

        public override ushort GetEndAddress()
        {
            return 0xFFFF;
        }

        public override byte Read8(ushort address)
        {
            return workRam[address & 0x1FFF];
        }

        public override void Write8(ushort address, byte value)
        {
            workRam[address & 0x1FFF] = value;
        }

        public byte[] DumpWorkRam()
        {
            return workRam;
        }
    }
}
