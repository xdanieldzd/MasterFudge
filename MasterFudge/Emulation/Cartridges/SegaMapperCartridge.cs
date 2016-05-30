using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MasterFudge.Emulation.Memory;

namespace MasterFudge.Emulation.Cartridges
{
    public class SegaMapperCartridge : BaseCartridge
    {
        // TODO: the other FFFC/reg0 stuff? http://www.smspower.org/Development/Mappers?from=Development.Mapper

        // TODO: remove the mirror kludge somehow? registerMirror is basically WRAM, but in *here* for the whole (DFFC-DFFF == FFFC-FFFF) mirroring...

        byte[] pagingRegisters, wramPagingRegistersMirror;
        byte[][] ramData;
        byte bankMask;

        public SegaMapperCartridge(byte[] romData) : base(romData)
        {
            pagingRegisters = new byte[0x04];
            pagingRegisters[0] = 0x00;  /* RAM select */
            pagingRegisters[1] = 0x00;  /* Page 0 ROM bank */
            pagingRegisters[2] = 0x01;  /* Page 1 ROM bank */
            pagingRegisters[3] = 0x02;  /* Page 2 ROM/RAM bank */

            wramPagingRegistersMirror = new byte[] { pagingRegisters[0], pagingRegisters[1], pagingRegisters[2], pagingRegisters[3] };

            ramData = new byte[0x02][];
            for (int i = 0; i < ramData.Length; i++) ramData[i] = new byte[0x4000];

            bankMask = (byte)((romData.Length >> 14) - 1);
        }

        public override ushort GetStartAddress()
        {
            return 0x0000;
        }

        public override ushort GetEndAddress()
        {
            return 0xBFFF;
        }

        public override byte Read8(ushort address)
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
                    if (MasterSystem.IsBitSet(pagingRegisters[0], 3))
                        return ramData[((pagingRegisters[0] >> 2) & 0x01)][(address & 0x3FFF)];
                    else
                        return romData[((pagingRegisters[3] << 14) | (address & 0x3FFF))];

                default:
                    throw new Exception(string.Format("Cannot read from cartridge address 0x{0:X4}", address));
            }
        }

        public override void Write8(ushort address, byte value)
        {
            if ((address & 0xC000) == 0x8000 && MasterSystem.IsBitSet(pagingRegisters[0], 3))
            {
                /* Cartridge RAM */
                ramData[((pagingRegisters[0] >> 2) & 0x01)][(address & 0x3FFF)] = value;
            }
            else if (MasterSystem.IsBitSet(pagingRegisters[0], 7))
            {
                /* ROM write enabled...? */
            }
            else
                throw new Exception(string.Format("Cannot write to cartridge address 0x{0:X4}", address));
        }

        public override MemoryAreaDescriptor[] GetAdditionalMemoryAreaDescriptors()
        {
            return new MemoryAreaDescriptor[]
            {
                 new MemoryAreaDescriptor(0xFFFC, 0xFFFF, ReadRegister, WriteRegister),
                 new MemoryAreaDescriptor(0xDFFC, 0xDFFF, ReadRegisterMirror, WriteRegisterMirror)
            };
        }

        private byte ReadRegister(ushort address)
        {
            /* Read from paging register */
            return pagingRegisters[address & 0x0003];
        }

        private byte ReadRegisterMirror(ushort address)
        {
            /* Read from "WRAM" */
            return wramPagingRegistersMirror[address & 0x0003];
        }

        private void WriteRegister(ushort address, byte value)
        {
            /* Write to paging register AND "WRAM" */
            wramPagingRegistersMirror[address & 0x0003] = value;
            if ((address & 0x0003) != 0x00) value &= bankMask;
            pagingRegisters[address & 0x0003] = value;
        }

        private void WriteRegisterMirror(ushort address, byte value)
        {
            /* Write ONLY to "WRAM" (does not affect paging registers) */
            wramPagingRegistersMirror[address & 0x0003] = value;
        }
    }
}
