using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MasterFudge.Emulation.Cartridges
{
    public enum KnownMapper
    {
        DefaultSega = 0,
        Codemasters = 1,
        Sega32kRAM = 2,
    }

    public abstract class BaseCartridge
    {
        static Dictionary<uint, CartridgeIdentity> cartridgeIdents = new Dictionary<uint, CartridgeIdentity>()
        {
            { 0x71DEBA5A, new CartridgeIdentity() { UnitRegion = BaseUnitRegion.JapanNTSC } },                                      /* Pop Breaker (GG) */
            { 0x29822980, new CartridgeIdentity() { Mapper = KnownMapper.Codemasters, UnitRegion = BaseUnitRegion.ExportPAL } },    /* Cosmic Spacehead (SMS) */
            { 0xB9664AE1, new CartridgeIdentity() { Mapper = KnownMapper.Codemasters, UnitRegion = BaseUnitRegion.ExportPAL } },    /* Fantastic Dizzy (SMS) */
            { 0xA577CE46, new CartridgeIdentity() { Mapper = KnownMapper.Codemasters, UnitRegion = BaseUnitRegion.ExportPAL } },    /* Micro Machines (SMS) */
            { 0xF691F9C7, new CartridgeIdentity() { Mapper = KnownMapper.Sega32kRAM, UnitType = BaseUnitType.SC3000 } },            /* Sega Basic Level 2 (SC-3000) */
        };

        protected byte[] romData;

        public RomHeader Header { get; private set; }

        public BaseUnitRegion RequestedUnitRegion { get; private set; }
        public BaseUnitType RequestedUnitType { get; private set; }

        protected BaseCartridge(byte[] romData)
        {
            this.romData = romData;

            Header = new RomHeader(this.romData);

            RequestedUnitRegion = BaseUnitRegion.Default;
            RequestedUnitType = BaseUnitType.Default;
        }

        public static T LoadCartridge<T>(string filename) where T : BaseCartridge
        {
            // TODO: "Korean" mapper

            byte[] data = ReadRomData(filename);
            uint crc = Utils.CalculateCrc32(data);

            T cartridge = null;

            /* Is cartridge known to need special care? */
            CartridgeIdentity cartIdent = (cartridgeIdents.ContainsKey(crc) ? cartridgeIdents[crc] : null);
            if (cartIdent != null)
            {
                /* Check mapper information */
                switch (cartIdent.Mapper)
                {
                    case KnownMapper.DefaultSega: cartridge = (new SegaMapperCartridge(data) as T); break;
                    case KnownMapper.Codemasters: cartridge = (new CodemastersMapperCartridge(data) as T); break;
                    case KnownMapper.Sega32kRAM: cartridge = (new Sega32kRAMCartridge(data) as T); break;
                    default: throw new Exception(string.Format("Unhandled cartridge type {0}", cartIdent.Mapper));
                }

                /* Force specified unit region/type */
                cartridge.RequestedUnitRegion = cartIdent.UnitRegion;
                cartridge.RequestedUnitType = cartIdent.UnitType;
            }
            else
            {
                /* Just assume default Sega mapper, no special treatment */
                cartridge = (new SegaMapperCartridge(data) as T);
            }

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
                    /* Copier header */
                    data = new byte[file.Length - (file.Length % 0x4000)];
                    file.Seek(file.Length % 0x4000, SeekOrigin.Begin);
                }
                else
                {
                    /* Normal ROM */
                    data = new byte[file.Length];
                }

                file.Read(data, 0, data.Length);
                return data;
            }
        }
    }
}
