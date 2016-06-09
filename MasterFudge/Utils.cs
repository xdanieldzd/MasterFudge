using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge
{
    public static class Utils
    {
        static readonly uint[] crcTable;
        static readonly uint crcPolynomial = 0xEDB88320;
        static readonly uint crcSeed = 0xFFFFFFFF;

        static Utils()
        {
            crcTable = new uint[256];

            for (int i = 0; i < 256; i++)
            {
                uint entry = (uint)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 0x00000001) == 0x00000001)
                        entry = (entry >> 1) ^ crcPolynomial;
                    else
                        entry = (entry >> 1);
                }
                crcTable[i] = entry;
            }
        }

        public static uint CalculateCrc32(byte[] data)
        {
            return CalculateCrc32(data, 0, data.Length);
        }

        public static uint CalculateCrc32(byte[] data, int start, int length)
        {
            uint crc = crcSeed;
            for (int i = start; i < (start + length); i++)
                crc = ((crc >> 8) ^ crcTable[data[i] ^ (crc & 0x000000FF)]);
            return ~crc;
        }

        // TODO: more bit operations and make the rest of the code use them

        public static bool IsBitSet(byte value, int bit)
        {
            return ((value & (1 << bit)) != 0);
        }
    }
}
