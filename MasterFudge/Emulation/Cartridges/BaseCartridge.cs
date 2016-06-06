using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MasterFudge.Emulation.Cartridges
{
    public abstract class BaseCartridge
    {
        protected byte[] romData;
        public RomHeader Header { get; private set; }

        protected BaseCartridge(byte[] romData)
        {
            this.romData = romData;
            Header = new RomHeader(this.romData);
        }

        public static T LoadCartridge<T>(string filename) where T : BaseCartridge
        {
            byte[] data = ReadRomData(filename);

            T cartridge = null;

            // TODO: non-standard mappers (Codemasters, "Korean" mapper)

            if (data.Length <= 0x80000)
                cartridge = (new SegaMapperCartridge(data) as T);
            else
                throw new Exception("Unhandled cartridge type");

            return cartridge;
        }

        public virtual bool HasCartridgeRam()
        {
            return false;
        }

        public virtual void SetRamData(byte[] data)
        {
            return;
        }

        public virtual byte[] GetRamData()
        {
            return new byte[0];
        }

        public abstract byte ReadCartridge(ushort address);
        public abstract void WriteCartridge(ushort address, byte value);
        public abstract void WriteMapper(ushort address, byte value);

        private static byte[] ReadRomData(string filename)
        {
            using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] data;

                if ((file.Length % 0x4000) == 0x200)
                {
                    // Copier(?) header
                    data = new byte[file.Length - (file.Length % 0x4000)];
                    file.Seek(file.Length % 0x4000, SeekOrigin.Begin);
                }
                else
                {
                    // Normal ROM
                    data = new byte[file.Length];
                }

                file.Read(data, 0, data.Length);
                return data;
            }
        }
    }
}
