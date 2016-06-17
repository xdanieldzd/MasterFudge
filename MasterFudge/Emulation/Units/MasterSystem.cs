using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MasterFudge.Emulation.CPU;
using MasterFudge.Emulation.Media;
using MasterFudge.Emulation.Graphics;
using MasterFudge.Emulation.Sound;
using MasterFudge.Emulation.IO;

using NAudio.Wave;

namespace MasterFudge.Emulation.Units
{
    public class MasterSystem : BaseUnit
    {
        public const double MasterClockPAL = 53203424;
        public const double MasterClockNTSC = 53693175;
        public const double FramesPerSecPAL = 49.701459;
        public const double FramesPerSecNTSC = 59.922743;

        Z80 cpu;
        byte[] wram;
        VDP vdp;
        PSG psg;
        BaseMedia bootstrap;

        byte portMemoryControl, portIoControl, portIoAB, portIoBMisc;
        byte lastHCounter;
        bool pausePressed;

        public bool isExpansionSlotEnabled { get { return !Utils.IsBitSet(portMemoryControl, 7); } }
        public bool isCartridgeSlotEnabled { get { return !Utils.IsBitSet(portMemoryControl, 6); } }
        public bool isCardSlotEnabled { get { return !Utils.IsBitSet(portMemoryControl, 5); } }
        public bool isWorkRamEnabled { get { return !Utils.IsBitSet(portMemoryControl, 4); } }
        public bool isBootstrapRomEnabled { get { return !Utils.IsBitSet(portMemoryControl, 3); } }
        public bool isIoChipEnabled { get { return !Utils.IsBitSet(portMemoryControl, 2); } }

        public MasterSystem() : base()
        {
            cpu = new Z80(ReadMemory, WriteMemory, ReadPort, WritePort);
            wram = new byte[0x2000];
            vdp = new VDP();
            psg = new PSG();

            bootstrap = null;
        }

        public override void SetRegion(BaseUnitRegion unitRegion)
        {
            base.SetRegion(unitRegion);

            vdp?.SetTvSystem(unitRegion);
            psg?.SetTvSystem(unitRegion);
        }

        public override double GetFrameRate()
        {
            return (GetRegion() == BaseUnitRegion.ExportPAL ? FramesPerSecPAL : FramesPerSecNTSC);
        }

        public override void Reset()
        {
            cpu.Reset();
            vdp.Reset();
            psg.Reset();

            bootstrap = null;
            if (Configuration.BootstrapEnabled)
                bootstrap = BaseMedia.LoadMedia(Configuration.MasterSystemBootstrapPath);

            portMemoryControl = (byte)(bootstrap != null ? 0xE3 : 0x00);
            portIoControl = portIoAB = portIoBMisc = 0xFF;
            lastHCounter = 0x00;
        }

        public override void ExecuteFrame()
        {
            int cyclesPerFrame = Z80.GetCPUClockCyclesPerFrame(IsNtscSystem);
            int cyclesPerLine = Z80.GetCPUClockCyclesPerScanline(IsNtscSystem);

            int totalCycles = 0, cycleDiff = 0;
            while (totalCycles < cyclesPerFrame)
            {
                int cyclesInLine = cycleDiff;
                while (cyclesInLine < cyclesPerLine)
                {
                    int currentCycles = cpu.Execute();

                    HandleInterrupts();

                    if (vdp.Execute(currentCycles))
                        OnRenderScreen(new RenderEventArgs(vdp.OutputFramebuffer));

                    // TODO: verify, fix, whatever, I hate sound
                    psg.Execute(currentCycles);

                    cyclesInLine += currentCycles;
                }

                cycleDiff = (cyclesInLine - cyclesPerLine);
                totalCycles += cyclesInLine;
            }
        }

        private void HandleInterrupts()
        {
            if (pausePressed)
            {
                pausePressed = false;
                cpu.ServiceNonMaskableInterrupt(0x0066);
            }

            if (vdp.IrqLineAsserted)
                cpu.ServiceInterrupt(0x0038);
        }

        public override byte ReadMemory(ushort address)
        {
            if (address >= 0x0000 && address <= 0xBFFF)
            {
                if (isBootstrapRomEnabled && bootstrap != null)
                    return bootstrap.ReadCartridge(address);

                else if (isCartridgeSlotEnabled && CurrentMediaType == MediaType.Cartridge)
                    return CurrentMedia.ReadCartridge(address);

                else if (isCardSlotEnabled && CurrentMediaType == MediaType.Card)
                    return CurrentMedia.ReadCartridge(address);

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

        public override void WriteMemory(ushort address, byte value)
        {
            if (isBootstrapRomEnabled) bootstrap?.WriteCartridge(address, value);
            if (isCartridgeSlotEnabled && CurrentMediaType == MediaType.Cartridge) CurrentMedia?.WriteCartridge(address, value);
            if (isCardSlotEnabled && CurrentMediaType == MediaType.Card) CurrentMedia?.WriteCartridge(address, value);

            if (isWorkRamEnabled && address >= 0xC000 && address <= 0xFFFF)
                wram[address & 0x1FFF] = value;
        }

        public override byte ReadPort(byte port)
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
                        if (IsExportSystem)
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

        public override void WritePort(byte port, byte value)
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
