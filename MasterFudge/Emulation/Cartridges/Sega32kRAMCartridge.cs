using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Cartridges
{
    public class Sega32kRAMCartridge : BaseCartridge
    {
        byte[] ramData;

        public Sega32kRAMCartridge(byte[] romData) : base(romData)
        {
            ramData = new byte[0x8000];
        }

        public override byte ReadCartridge(ushort address)
        {
            if ((address & 0x8000) == 0x8000)
                return ramData[address & 0x7FFF];
            else
                return romData[address & (romData.Length - 1)];
        }

        public override void WriteCartridge(ushort address, byte value)
        {
            if ((address & 0x8000) == 0x8000)
                ramData[address & 0x7FFF] = value;
            else
                throw new Exception(string.Format("Sega 32k RAM mapper: Cannot write to cartridge address 0x{0:X4}", address));
        }

        public override void WriteMapper(ushort address, byte value)
        {
            /* Not needed */
            return;
        }
    }
}
