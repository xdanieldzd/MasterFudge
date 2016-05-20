using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Cartridges
{
    public class RomHeader
    {
        public static readonly Dictionary<byte, string> RegionNames = new Dictionary<byte, string>()
        {
            { 0x03, "SMS (Japan)" },
            { 0x04, "SMS (Export)" },
            { 0x05, "GG (Japan)" },
            { 0x06, "GG (Export)" },
            { 0x07, "GG (International)" }
        };

        public static readonly Dictionary<byte, string> SizeNames = new Dictionary<byte, string>()
        {
            { 0x0A, "8 KB (unused)" },
            { 0x0B, "16 KB (unused)" },
            { 0x0C, "32 KB" },
            { 0x0D, "48 KB (unused, buggy)" },
            { 0x0E, "64 KB (rarely used)" },
            { 0x0F, "128 KB" },
            { 0x00, "256 KB" },
            { 0x01, "512 KB (rarely used)" },
            { 0x02, "1 MB (unused, buggy)" },
        };

        public static readonly Dictionary<byte, uint> SizeValues = new Dictionary<byte, uint>()
        {
            { 0x0A, 0x2000 },
            { 0x0B, 0x4000 },
            { 0x0C, 0x8000 },
            { 0x0D, 0xC000 },
            { 0x0E, 0x10000 },
            { 0x0F, 0x20000 },
            { 0x00, 0x40000 },
            { 0x01, 0x80000 },
            { 0x02, 0x100000 },
        };

        public string TMRSEGAString { get; private set; }
        public byte[] Reserved { get; private set; }
        public ushort Checksum { get; private set; }
        public uint ProductCode { get; private set; }
        public byte Version { get; private set; }
        public byte Region { get; private set; }
        public byte RomSizeType { get; private set; }

        public uint RomSizeCalculated { get; private set; }
        public bool IsRomSizeCorrect { get; private set; }
        public ushort ChecksumCalculated { get; private set; }

        public RomHeader(byte[] romData)
        {
            int headerOffset = (romData.Length <= 0x8000 ? romData.Length - 0x10 : 0x7FF0);

            TMRSEGAString = Encoding.ASCII.GetString(romData, headerOffset, 8);
            Reserved = new byte[] { romData[headerOffset + 8], romData[headerOffset + 9] };
            Checksum = BitConverter.ToUInt16(romData, headerOffset + 10);
            ProductCode = (uint)BCDToDecimal(new byte[] { romData[headerOffset + 12], romData[headerOffset + 13], (byte)(romData[headerOffset + 14] >> 4) });
            Version = (byte)(romData[headerOffset + 14] & 0x0F);
            Region = (byte)(romData[headerOffset + 15] >> 4);
            RomSizeType = (byte)(romData[headerOffset + 15] & 0xF);

            RomSizeCalculated = (uint)romData.Length;
            IsRomSizeCorrect = (romData.Length == GetRomSizeValue());
            ChecksumCalculated = CalculateChecksum(romData);
        }

        public string GetRegionName()
        {
            if (RegionNames.ContainsKey(Region)) return RegionNames[Region];
            else return "unknown region";
        }

        public string GetRomSizeName()
        {
            if (SizeNames.ContainsKey(RomSizeType)) return SizeNames[RomSizeType];
            else return "unknown size";
        }

        public uint GetRomSizeValue()
        {
            if (SizeValues.ContainsKey(RomSizeType)) return SizeValues[RomSizeType];
            else return 0;
        }

        private ulong BCDToDecimal(byte[] bcdInput)
        {
            ulong decOutput = 0;
            for (int i = bcdInput.Length - 1; i >= 0; i--)
            {
                decOutput *= 100;
                byte decDigits = (byte)((bcdInput[i] >> 4) * 10 + (bcdInput[i] & 0x0F));
                decOutput += decDigits;
            }
            return decOutput;
        }

        private ushort CalculateChecksum(byte[] romData)
        {
            ushort checksum = 0;

            for (int i = 0; i < (romData.Length < 0x8000 ? romData.Length - 0x10 : 0x7FF0); i++)
                checksum += romData[i];

            if ((IsRomSizeCorrect ? GetRomSizeValue() : RomSizeCalculated) >= 0x10000)
                for (int i = 0x8000; i < GetRomSizeValue(); i++)
                    checksum += romData[i];

            return checksum;
        }
    }
}
