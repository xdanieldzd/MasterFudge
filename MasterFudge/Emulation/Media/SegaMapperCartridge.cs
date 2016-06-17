using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Media
{
    public class SegaMapperCartridge : BaseMedia
    {
        // TODO: the other FFFC/reg0 stuff? http://www.smspower.org/Development/Mappers?from=Development.Mapper

        byte[] pagingRegisters;
        byte[] ramData;
        byte bankMask;
        bool hasCartRam;

        bool isRamEnabled { get { return Utils.IsBitSet(pagingRegisters[0], 3); } }
        bool isRomWriteEnable { get { return Utils.IsBitSet(pagingRegisters[0], 7); } }
        int ramBank { get { return ((pagingRegisters[0] >> 2) & 0x01); } }
        int romBank0 { get { return pagingRegisters[1]; } }
        int romBank1 { get { return pagingRegisters[2]; } }
        int romBank2 { get { return pagingRegisters[3]; } }

        public SegaMapperCartridge(string filename, byte[] romData) : base(filename, romData)
        {
            pagingRegisters = new byte[0x04];
            pagingRegisters[0] = 0x00;  /* Mapper control */
            pagingRegisters[1] = 0x00;  /* Page 0 ROM bank */
            pagingRegisters[2] = 0x01;  /* Page 1 ROM bank */
            pagingRegisters[3] = 0x02;  /* Page 2 ROM bank */

            ramData = new byte[0x8000];

            bankMask = (byte)((romData.Length >> 14) - 1);
        }

        public override bool HasOnBoardRam()
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
            switch (address & 0xC000)
            {
                case 0x0000:
                    if (address < 0x400)
                        /* First 1kb is constant to preserve interrupt vectors */
                        return romData[address];
                    else
                        return romData[((romBank0 << 14) | (address & 0x3FFF))];

                case 0x4000:
                    return romData[((romBank1 << 14) | (address & 0x3FFF))];

                case 0x8000:
                    if (isRamEnabled)
                        return ramData[((ramBank << 14) | (address & 0x3FFF))];
                    else
                        return romData[((romBank2 << 14) | (address & 0x3FFF))];

                default:
                    throw new Exception(string.Format("Sega mapper: Cannot read from cartridge address 0x{0:X4}", address));
            }
        }

        public override void WriteCartridge(ushort address, byte value)
        {
            if (address >= 0xFFFC && address <= 0xFFFF)
            {
                /* Write to paging register */
                if ((address & 0x0003) != 0x00) value &= bankMask;
                pagingRegisters[address & 0x0003] = value;

                /* Check if RAM ever gets enabled; if it is, indicate that we'll need to save the RAM */
                if (!hasCartRam && isRamEnabled && (address & 0x0003) == 0x0000)
                    hasCartRam = true;
            }
            if (isRamEnabled && (address & 0xC000) == 0x8000)
            {
                /* Cartridge RAM */
                ramData[((ramBank << 14) | (address & 0x3FFF))] = value;
            }
            else if (isRomWriteEnable)
            {
                /* ROM write enabled...? */
            }

            /* Otherwise ignore writes to ROM, as some games seem to be doing that? (ex. Gunstar Heroes GG to 0000) */
        }
    }
}
