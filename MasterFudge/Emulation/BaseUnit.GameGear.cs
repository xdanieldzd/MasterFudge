using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation
{
    public partial class BaseUnit
    {
        private byte ReadMemoryGG(ushort address)
        {
            if (address >= 0x0000 && address <= 0xBFFF)
            {
                if (isBootstrapRomEnabled && bootstrap != null)
                    return bootstrap.ReadCartridge(address);

                else if (isCartridgeSlotEnabled && cartridge != null)
                    return cartridge.ReadCartridge(address);

                else
                    /* For bootstrap, no usable media mapped */
                    return 0x00;
            }
            else if (address >= 0xC000 && address <= 0xFFFF)
            {
                if (isWorkRamEnabled)
                    return wram[address & 0x1FFF];
            }

            throw new Exception(string.Format("GG: Unsupported read from address 0x{0:X4}", address));
        }

        private void WriteMemoryGG(ushort address, byte value)
        {
            if (isBootstrapRomEnabled) bootstrap?.WriteCartridge(address, value);
            if (isCartridgeSlotEnabled) cartridge?.WriteCartridge(address, value);

            if (isWorkRamEnabled && address >= 0xC000 && address <= 0xFFFF)
                wram[address & 0x1FFF] = value;
        }

        private byte ReadIOPortGG(byte port)
        {
            /* GG-specific ports */
            switch (port)
            {
                case 0x00: return (byte)((portIoC & 0xBF) | (isExportSystem ? 0x40 : 0x00));
                case 0x01: return portParallelData;
                case 0x02: return portDataDirNMI;
                case 0x03: return portTxBuffer;
                case 0x04: return portRxBuffer;
                case 0x05: return portSerialControl;
                case 0x06: return portStereoControl;
            }

            port = (byte)(port & 0xC1);

            switch (port & 0xF0)
            {
                case 0x00:
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
                        return portIoBMisc;             /* IO port B/misc register */

                default: throw new Exception(string.Format("GG: Unsupported read from port 0x{0:X2}", port));
            }
        }

        private void WriteIOPortGG(byte port, byte value)
        {
            byte maskedPort = (byte)(port & 0xC1);

            switch (maskedPort & 0xF0)
            {
                case 0x00:
                    if (port >= 0x00 && port < 0x07)
                    {
                        switch (port)
                        {
                            case 0x00: /* Read-only */ break;
                            case 0x01: portParallelData = value; break;
                            case 0x02: portDataDirNMI = value; break;
                            case 0x03: portTxBuffer = value; break;
                            case 0x04: /* Read-only? */; break;
                            case 0x05: portSerialControl = (byte)(value & 0xF8); break;
                            case 0x06: portStereoControl = value; break;    // TODO: write to PSG
                        }
                    }
                    else
                    {
                        /* System stuff */
                        if ((maskedPort & 0x01) == 0)
                            portMemoryControl = value;  /* Memory control */
                        else
                        {
                            /* I/O control */
                            if ((portIoControl & 0x0A) == 0x00 && ((value & 0x02) == 0x02 || (value & 0x08) == 0x08))
                                lastHCounter = vdp.ReadHCounter();
                            portIoControl = value;
                        }
                    }
                    break;

                case 0x40:
                    /* PSG */
                    psg.WriteData(value);
                    break;

                case 0x80:
                    /* VDP */
                    if ((maskedPort & 0x01) == 0)
                        vdp.WriteDataPort(value);       /* Data port */
                    else
                        vdp.WriteControlPort(value);    /* Control port */
                    break;

                case 0xC0:
                    /* No effect */
                    break;

                default: throw new Exception(string.Format("GG: Unsupported write to port 0x{0:X2}, value 0x{1:X2}", port, value));
            }
        }
    }
}
