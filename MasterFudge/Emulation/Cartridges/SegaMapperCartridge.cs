﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Cartridges
{
    public class SegaMapperCartridge : BaseCartridge
    {
        // TODO: the other FFFC/reg0 stuff? http://www.smspower.org/Development/Mappers?from=Development.Mapper

        byte[] pagingRegisters;
        byte[] ramData;
        byte bankMask;
        bool hasCartRam;

        public SegaMapperCartridge(byte[] romData) : base(romData)
        {
            pagingRegisters = new byte[0x04];
            pagingRegisters[0] = 0x00;  /* RAM select */
            pagingRegisters[1] = 0x00;  /* Page 0 ROM bank */
            pagingRegisters[2] = 0x01;  /* Page 1 ROM bank */
            pagingRegisters[3] = 0x02;  /* Page 2 ROM/RAM bank */

            ramData = new byte[0x8000];

            bankMask = (byte)((romData.Length >> 14) - 1);

            // TODO: kludge for small SG-1000/SC-3000 games, eventually do proper memory mapping for them
            if (romData.Length <= 0x4000)
            {
                pagingRegisters[2] = 0x00;
                pagingRegisters[3] = 0x00;
            }
            else if (romData.Length <= 0x8000)
            {
                pagingRegisters[2] = 0x01;
                pagingRegisters[3] = 0x01;
            }
        }

        public override bool HasCartridgeRam()
        {
            return hasCartRam;
        }

        public override void SetRamData(byte[] data)
        {
            Buffer.BlockCopy(data, 0, ramData, 0, Math.Min(data.Length, ramData.Length));
        }

        public override byte[] GetRamData()
        {
            return ramData;
        }

        public override byte ReadCartridge(ushort address)
        {
            // TODO: appears to be working correctly now, wrt mirroring etc...

            switch (address & 0xC000)
            {
                case 0x0000:
                    if (address < 0x400)
                        return romData[address];
                    else
                        return romData[((pagingRegisters[1] << 14) | (address & 0x3FFF))];

                case 0x4000:
                    return romData[((pagingRegisters[2] << 14) | (address & 0x3FFF))];

                case 0x8000:
                    if (Utils.IsBitSet(pagingRegisters[0], 3))
                        return ramData[((pagingRegisters[0] >> 2) & 0x01) << 14 | (address & 0x3FFF)];
                    else
                        return romData[((pagingRegisters[3] << 14) | (address & 0x3FFF))];

                default:
                    throw new Exception(string.Format("Cannot read from cartridge address 0x{0:X4}", address));
            }
        }

        public override void WriteCartridge(ushort address, byte value)
        {
            if ((address & 0xC000) == 0x8000 && Utils.IsBitSet(pagingRegisters[0], 3))
            {
                /* Cartridge RAM */
                ramData[((pagingRegisters[0] >> 2) & 0x01) << 14 | (address & 0x3FFF)] = value;
            }
            else if (Utils.IsBitSet(pagingRegisters[0], 7))
            {
                /* ROM write enabled...? */
            }

            /* Otherwise ignore writes to ROM, as some games seem to be doing that? (ex. Gunstar Heroes GG to 0000) */
        }

        public override void WriteMapper(ushort address, byte value)
        {
            /* Write to paging register */
            if ((address & 0x0003) != 0x00) value &= bankMask;
            pagingRegisters[address & 0x0003] = value;

            /* Check if RAM ever gets enabled; if it is, indicate that we'll need to save the RAM */
            if (!hasCartRam && (address & 0x0003) == 0x0000 && Utils.IsBitSet(pagingRegisters[address & 0x0003], 3))
                hasCartRam = true;
        }
    }
}
