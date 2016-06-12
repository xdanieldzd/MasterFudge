﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Cartridges
{
    public class RomOnlyCartridge : BaseCartridge
    {
        public RomOnlyCartridge(byte[] romData) : base(romData) { }

        public override byte ReadCartridge(ushort address)
        {
            return romData[address & (romData.Length - 1)];
        }

        public override void WriteCartridge(ushort address, byte value)
        {
            /* Cannot write to cartridge */
            return;
        }
    }
}
