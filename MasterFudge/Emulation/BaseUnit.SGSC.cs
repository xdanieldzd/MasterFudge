using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation
{
    public partial class BaseUnit
    {
        private byte ReadMemorySGSC(ushort address)
        {
            if (isCartridgeSlotEnabled && cartridge != null)
                return cartridge.ReadCartridge(address);
            else
                return (byte)(address >> 8);

            throw new Exception(string.Format("SG/SC: Unsupported read from address 0x{0:X4}", address));
        }

        private void WriteMemorySGSC(ushort address, byte value)
        {
            if (isCartridgeSlotEnabled && cartridge != null)
                cartridge.WriteCartridge(address, value);
            else
                throw new Exception(string.Format("SG/SC: Unsupported write to address 0x{0:X4}, value 0x{1:X2}", address, value));
        }

        private byte ReadIOPortSGSC(byte port)
        {
            // TODO: simplify like SG/SC writes?

            switch (port & 0xE0)
            {
                case 0x00:
                case 0x80:
                    // TODO: emulate the corrupted bits...? probably not
                    return ppi.ReadPort((byte)(port & 0x03));

                case 0x20:
                case 0xA0:
                    if ((port & 0x01) == 0)
                        return vdp.ReadDataPort();
                    else
                        return vdp.ReadControlPort();

                case 0x40:
                case 0xC0:
                    return ppi.ReadPort((byte)(port & 0x03));

                case 0x60:
                case 0xE0:
                    // TODO: "Instruction referenced by R" ??
                    return 0;

                default: throw new Exception(string.Format("SG/SC: Unsupported read from port 0x{0:X2}", port));
            }
        }

        private void WriteIOPortSGSC(byte port, byte value)
        {
            if ((port & 0x20) == 0)
            {
                ppi.WritePort((byte)(port & 0x03), value);
                keyboard.Refresh();
            }

            if ((port & 0x40) == 0)
                if ((port & 0x01) == 0)
                    vdp.WriteDataPort(value);
                else
                    vdp.WriteControlPort(value);

            if ((port & 0x80) == 0)
                psg.WriteData(value);
        }
    }
}
