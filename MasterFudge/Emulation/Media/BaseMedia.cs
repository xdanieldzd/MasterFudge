using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using MasterFudge.Emulation.Units;

namespace MasterFudge.Emulation.Media
{
    public enum MediaType
    {
        None,
        Bootstrap,
        Cartridge,
        Card
    }

    public enum KnownMapper
    {
        DefaultSega = 0,
        Codemasters = 1,
        Sega32kRAM = 2,
    }

    public abstract class BaseMedia
    {
        static Dictionary<uint, MediaIdentity> mediaIdents = new Dictionary<uint, MediaIdentity>()
        {
            { 0x71DEBA5A, new MediaIdentity() { UnitRegion = BaseUnitRegion.JapanNTSC } },                                      /* Pop Breaker (GG) */
            { 0x29822980, new MediaIdentity() { Mapper = KnownMapper.Codemasters, UnitRegion = BaseUnitRegion.ExportPAL } },    /* Cosmic Spacehead (SMS) */
            { 0xB9664AE1, new MediaIdentity() { Mapper = KnownMapper.Codemasters, UnitRegion = BaseUnitRegion.ExportPAL } },    /* Fantastic Dizzy (SMS) */
            { 0xA577CE46, new MediaIdentity() { Mapper = KnownMapper.Codemasters, UnitRegion = BaseUnitRegion.ExportPAL } },    /* Micro Machines (SMS) */
            { 0xF691F9C7, new MediaIdentity() { Mapper = KnownMapper.Sega32kRAM, UnitType = BaseUnitType.SC3000 } },            /* Sega Basic Level 2 (SC-3000) */
            { 0x5D9F11CA, new MediaIdentity() { Mapper = KnownMapper.Sega32kRAM, UnitType = BaseUnitType.SC3000 } },            /* Sega Basic Level 3 V1 (SC-3000) */
        };

        protected byte[] romData;

        public string Filename { get; private set; }

        public BaseUnitRegion RequestedUnitRegion { get; private set; }
        public BaseUnitType RequestedUnitType { get; private set; }

        protected BaseMedia(string filename, byte[] romData)
        {
            this.romData = romData;

            Filename = filename;

            RequestedUnitRegion = BaseUnitRegion.Invalid;
            RequestedUnitType = BaseUnitType.Invalid;
        }

        public static BaseMedia LoadMedia(string filename)
        {
            // TODO: "Korean" mapper

            byte[] data = ReadRomData(filename);
            uint crc = Utils.CalculateCrc32(data);

            BaseMedia media = null;

            /* Is media known to need special care? */
            MediaIdentity mediaIdent = (mediaIdents.ContainsKey(crc) ? mediaIdents[crc] : null);
            if (mediaIdent != null)
            {
                /* Check mapper information */
                switch (mediaIdent.Mapper)
                {
                    case KnownMapper.DefaultSega: media = new SegaMapperCartridge(filename, data); break;
                    case KnownMapper.Codemasters: media = new CodemastersMapperCartridge(filename, data); break;
                    case KnownMapper.Sega32kRAM: media = new Sega32kRamCartridge(filename, data); break;
                    default: throw new Exception(string.Format("Unhandled cartridge type {0}", mediaIdent.Mapper));
                }

                /* Force specified unit region/type */
                media.RequestedUnitRegion = mediaIdent.UnitRegion;
                media.RequestedUnitType = mediaIdent.UnitType;
            }
            else if (data.Length <= 0xC000)
            {
                /* Size is 48k max, assume ROM only mapper */
                media = new RomOnlyCartridge(filename, data);
            }
            else
            {
                /* No special treatment and bigger than 48k, assume default Sega mapper */
                media = new SegaMapperCartridge(filename, data);
            }

            /* If not already set, try to determine unit region/type now */
            if (media.RequestedUnitRegion == BaseUnitRegion.Invalid && media.RequestedUnitType == BaseUnitType.Invalid)
            {
                RomHeader header = new RomHeader(data);
                if (header.TMRSEGAString == RomHeader.ExpectedTMRSEGAString)
                {
                    /* Valid header */
                    if (header.IsGameGear)
                    {
                        media.RequestedUnitType = BaseUnitType.GameGear;
                        if (header.IsExport)
                            media.RequestedUnitRegion = BaseUnitRegion.ExportNTSC;
                        else
                            media.RequestedUnitRegion = BaseUnitRegion.JapanNTSC;
                    }
                    else
                    {
                        media.RequestedUnitType = BaseUnitType.MasterSystem;
                        if (header.IsExport)
                            media.RequestedUnitRegion = BaseUnitRegion.ExportNTSC;
                        else
                            media.RequestedUnitRegion = BaseUnitRegion.JapanNTSC;

                        //TODO: PAL!
                    }
                }
                else
                {
                    // TODO: SG1000/SC3000
                    media.RequestedUnitType = BaseUnitType.SC3000;
                    media.RequestedUnitRegion = BaseUnitRegion.JapanNTSC;
                }
            }

            return media;
        }

        public virtual bool HasOnBoardRam()
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
