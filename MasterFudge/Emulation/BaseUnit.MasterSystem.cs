using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MasterFudge.Emulation.Cartridges;

namespace MasterFudge.Emulation
{
    public partial class BaseUnit
    {
        private byte ReadMemorySMS(ushort address)
        {
            if (address >= 0x0000 && address <= 0xBFFF)
            {
                if (isBootstrapRomEnabled && bootstrap != null)
                    return bootstrap.ReadCartridge(address);

                else if (isCartridgeSlotEnabled && cartridge != null)
                    return cartridge.ReadCartridge(address);

                else if (isCardSlotEnabled && card != null)
                    return card.ReadCartridge(address);

                else
                    /* For bootstrap, no usable media mapped */
                    return 0x00;
            }
            else if (address >= 0xC000 && address <= 0xFFFF)
            {
                if (isWorkRamEnabled)
                    return wram[address & 0x1FFF];
            }

            throw new Exception(string.Format("SMS: Unsupported read from address 0x{0:X4}", address));
        }

        private void WriteMemorySMS(ushort address, byte value)
        {
            if (address >= 0x0000 && address <= 0xBFFF)
            {
                if (isBootstrapRomEnabled) bootstrap?.WriteCartridge(address, value);
                if (isCartridgeSlotEnabled) cartridge?.WriteCartridge(address, value);
                if (isCardSlotEnabled) card?.WriteCartridge(address, value);
            }
            else if (address >= 0xC000 && address <= 0xFFFF)
            {
                if (isWorkRamEnabled)
                    wram[address & 0x1FFF] = value;

                // TODO: make just a bit smarter, in conjunction with CartridgeIdentity maybe?
                if (address >= 0xFFFC)
                {
                    if (isBootstrapRomEnabled && (bootstrap != null) && (bootstrap is SegaMapperCartridge)) bootstrap.WriteMapper(address, value);
                    if (isCartridgeSlotEnabled && (cartridge != null) && (cartridge is SegaMapperCartridge)) cartridge.WriteMapper(address, value);
                    if (isCardSlotEnabled && (card != null) && (card is SegaMapperCartridge)) card.WriteMapper(address, value);
                }
            }
            else
                throw new Exception(string.Format("SMS: Unsupported write to address 0x{0:X4}, value 0x{1:X2}", address, value));
        }

        private byte ReadIOPortSMS(byte port)
        {
            port = (byte)(port & 0xC1);

            switch (port & 0xF0)
            {
                case 0x00:
                    /* Uh, behave like SMS2 for now */
                    return 0xFF;

                case 0x40:
                    /* Counters */
                    if ((port & 0x01) == 0)
                        return vdp.ReadVCounter();      /* V counter */
                    else
                        return lastHCounter;            /* H counter */

                case 0x80:
                    /* VDP */
                    if ((port & 0x01) == 0)
                        return vdp.ReadDataPort();      /* Data port */
                    else
                        return vdp.ReadControlPort();   /* Status flags */

                case 0xC0:
                    if ((port & 0x01) == 0)
                        return portIoAB;                /* IO port A/B register */
                    else
                    {
                        /* IO port B/misc register */
                        if (isExportSystem)
                        {
                            if (portIoControl == 0xF5)
                                return (byte)(portIoBMisc | 0xC0);
                            else
                                return (byte)(portIoBMisc & 0x3F);
                        }
                        else
                            return portIoBMisc;
                    }

                default: throw new Exception(string.Format("SMS: Unsupported read from port 0x{0:X2}", port));
            }
        }

        public void WriteIOPortSMS(byte port, byte value)
        {
            port = (byte)(port & 0xC1);

            switch (port & 0xF0)
            {
                case 0x00:
                    /* System stuff */
                    if ((port & 0x01) == 0)
                        portMemoryControl = value;      /* Memory control */
                    else
                    {
                        /* I/O control */
                        if ((portIoControl & 0x0A) == 0x00 && ((value & 0x02) == 0x02 || (value & 0x08) == 0x08))
                            lastHCounter = vdp.ReadHCounter();
                        portIoControl = value;
                    }
                    break;

                case 0x40:
                    /* PSG */
                    psg.WriteData(value);
                    break;

                case 0x80:
                    /* VDP */
                    if ((port & 0x01) == 0)
                        vdp.WriteDataPort(value);       /* Data port */
                    else
                        vdp.WriteControlPort(value);    /* Control port */
                    break;

                case 0xC0:
                    /* No effect */
                    break;

                default: throw new Exception(string.Format("SMS: Unsupported write to port 0x{0:X2}, value 0x{1:X2}", port, value));
            }
        }
    }
}
