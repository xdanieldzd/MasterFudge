using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Cartridges
{
    public class Basic32kCartridge : BaseCartridge
    {
        public Basic32kCartridge(byte[] romData) : base(romData) { }

        public override ushort GetStartAddress()
        {
            return 0x0000;
        }

        public override ushort GetEndAddress()
        {
            return 0x7FFF;
        }

        public override byte Read8(ushort address)
        {
            return romData[address & 0x7FFF];
        }

        public override void Write8(ushort address, byte value)
        {
            throw new Exception("Cannot write to ROM");
        }
    }
}
