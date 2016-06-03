﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using MasterFudge.Emulation.Memory;
using MasterFudge.Emulation.CPU;
using MasterFudge.Emulation.Cartridges;
using MasterFudge.Emulation.Graphics;
using MasterFudge.Emulation.Sound;

using NAudio.Wave;

namespace MasterFudge.Emulation
{
    public delegate void RenderScreenHandler(object sender, RenderEventArgs e);

    public class RenderEventArgs : EventArgs
    {
        public byte[] FrameData { get; private set; }

        public RenderEventArgs(byte[] data)
        {
            FrameData = data;
        }
    }

    public enum BaseUnitType
    {
        MasterSystem,
        GameGear
    }

    public enum BaseUnitRegion
    {
        JapanNTSC,
        ExportNTSC,
        ExportPAL
    }

    [Flags]
    public enum Buttons
    {
        None = 0,
        Up = (1 << 0),
        Down = (1 << 1),
        Left = (1 << 2),
        Right = (1 << 3),
        Button1 = (1 << 4),
        Button2 = (1 << 5),
        StartPause = (1 << 6),  /* Start on GG, Pause on SMS */
        Reset = (1 << 7)
    }

    [Flags]
    enum PortIoABButtons : byte
    {
        P1Up = (1 << 0),
        P1Down = (1 << 1),
        P1Left = (1 << 2),
        P1Right = (1 << 3),
        P1Button1 = (1 << 4),
        P1Button2 = (1 << 5),
        P2Up = (1 << 6),
        P2Down = (1 << 7)
    }

    [Flags]
    enum PortIoBMiscButtons : byte
    {
        P2Left = (1 << 0),
        P2Right = (1 << 1),
        P2Button1 = (1 << 2),
        P2Button2 = (1 << 3),
        Reset = (1 << 4)
    }

    [Flags]
    enum PortIoCButtons : byte
    {
        Start = (1 << 7)
    }

    public partial class PowerBase
    {
        public const double MasterClockPAL = 53203424;
        public const double MasterClockNTSC = 53693175;
        public const double FramesPerSecPAL = 49.701459;
        public const double FramesPerSecNTSC = 59.922743;

        BaseUnitType baseUnitType;
        BaseUnitRegion baseUnitRegion;

        MemoryMapper memoryMapper;

        Z80 cpu;
        WRAM wram;
        VDP vdp;
        PSG psg;
        BaseCartridge cartridge;

        byte portMemoryControl, portIoControl, portIoAB, portIoBMisc;
        byte lastHCounter;
        bool pausePressed;

        /* Game Gear-only */
        byte portIoC, portParallelData, portDataDirNMI, portTxBuffer, portRxBuffer, portSerialControl, portStereoControl;

        // TODO: actually make memory accesses depend on these
        public bool isExpansionSlotEnabled { get { return !IsBitSet(portMemoryControl, 7); } }
        public bool isCartridgeSlotEnabled { get { return !IsBitSet(portMemoryControl, 6); } }
        public bool isCardSlotEnabled { get { return !IsBitSet(portMemoryControl, 5); } }
        public bool isWorkRamEnabled { get { return !IsBitSet(portMemoryControl, 4); } }
        public bool isBiosRomEnabled { get { return !IsBitSet(portMemoryControl, 3); } }
        public bool isIOChipEnabled { get { return !IsBitSet(portMemoryControl, 2); } }

        bool isNtscSystem { get { return (baseUnitRegion == BaseUnitRegion.JapanNTSC || baseUnitRegion == BaseUnitRegion.ExportNTSC); } }
        bool isExportSystem { get { return (baseUnitRegion == BaseUnitRegion.ExportNTSC || baseUnitRegion == BaseUnitRegion.ExportPAL); } }

        public bool IsNtscSystem { get { return isNtscSystem; } }
        public bool IsPalSystem { get { return !isNtscSystem; } }
        public bool IsExportSystem { get { return isExportSystem; } }
        public bool IsJapaneseSystem { get { return !isExportSystem; } }

        public event RenderScreenHandler OnRenderScreen;

        Stopwatch stopWatch;
        bool isStopped;

        public bool LimitFPS { get; set; }
        public bool DebugLogOpcodes { get { return cpu.DebugLogOpcodes; } set { cpu.DebugLogOpcodes = value; } }
        public bool CartridgeLoaded { get { return (cartridge != null); } }
        public string CartridgeFilename { get; private set; }

        public PowerBase()
        {
            memoryMapper = new MemoryMapper();

            cpu = new Z80(memoryMapper, ReadIOPort, WriteIOPort);
            wram = new WRAM();
            vdp = new VDP();
            psg = new PSG();

            memoryMapper.AddMemoryArea(wram.GetMemoryAreaDescriptor());

            stopWatch = new Stopwatch();
            stopWatch.Start();

            isStopped = true;
            LimitFPS = true;

            SetRegion(BaseUnitRegion.ExportNTSC);
        }

        public static double GetFrameRate(bool isNtsc)
        {
            return (isNtsc ? FramesPerSecNTSC : FramesPerSecPAL);
        }

        public static int GetMasterClockCyclesPerFrame(bool isNtsc)
        {
            return (int)((isNtsc ? MasterClockNTSC : MasterClockPAL) / GetFrameRate(isNtsc));
        }

        public static int GetMasterClockCyclesPerScanline(bool isNtsc)
        {
            return (GetMasterClockCyclesPerFrame(isNtsc) / (isNtsc ? VDP.NumScanlinesNTSC : VDP.NumScanlinesPAL));
        }

        public void SetUnitType(BaseUnitType unitType)
        {
            baseUnitType = unitType;

            vdp?.SetUnitType(baseUnitType);
            psg?.SetUnitType(baseUnitType);
        }

        public void SetRegion(BaseUnitRegion unitRegion)
        {
            baseUnitRegion = unitRegion;

            vdp?.SetTvSystem(baseUnitRegion);
            psg?.SetTvSystem(baseUnitRegion);
        }

        public static bool IsBitSet(byte value, int bit)
        {
            return ((value & (1 << bit)) != 0);
        }

        public void LoadCartridge(string filename)
        {
            CartridgeFilename = filename;

            cartridge = BaseCartridge.LoadCartridge<BaseCartridge>(filename);
            memoryMapper.AddMemoryArea(cartridge.GetMemoryAreaDescriptor());

            foreach (MemoryAreaDescriptor areaDescriptor in cartridge.GetAdditionalMemoryAreaDescriptors())
                memoryMapper.AddMemoryArea(areaDescriptor);
        }

        public RomHeader GetCartridgeHeader()
        {
            return cartridge.Header;
        }

        public IWaveProvider GetPSGWaveProvider()
        {
            return (psg as IWaveProvider);
        }

        // TODO: IO port control (the active high/low stuff)
        public void SetButtonData(Buttons buttons, int player, bool pressed)
        {
            byte maskAB = 0, maskBMisc = 0, maskC = 0;

            if (player == 0)
            {
                /* Player 1 */
                if (buttons.HasFlag(Buttons.Up)) maskAB |= (byte)PortIoABButtons.P1Up;
                if (buttons.HasFlag(Buttons.Down)) maskAB |= (byte)PortIoABButtons.P1Down;
                if (buttons.HasFlag(Buttons.Left)) maskAB |= (byte)PortIoABButtons.P1Left;
                if (buttons.HasFlag(Buttons.Right)) maskAB |= (byte)PortIoABButtons.P1Right;
                if (buttons.HasFlag(Buttons.Button1)) maskAB |= (byte)PortIoABButtons.P1Button1;
                if (buttons.HasFlag(Buttons.Button2)) maskAB |= (byte)PortIoABButtons.P1Button2;
            }
            else if (player == 1)
            {
                /* Player 2 */
                if (buttons.HasFlag(Buttons.Up)) maskAB |= (byte)PortIoABButtons.P2Up;
                if (buttons.HasFlag(Buttons.Down)) maskAB |= (byte)PortIoABButtons.P2Down;
                if (buttons.HasFlag(Buttons.Left)) maskBMisc |= (byte)PortIoBMiscButtons.P2Left;
                if (buttons.HasFlag(Buttons.Right)) maskBMisc |= (byte)PortIoBMiscButtons.P2Right;
                if (buttons.HasFlag(Buttons.Button1)) maskBMisc |= (byte)PortIoBMiscButtons.P2Button1;
                if (buttons.HasFlag(Buttons.Button2)) maskBMisc |= (byte)PortIoBMiscButtons.P2Button2;
            }

            if (buttons.HasFlag(Buttons.StartPause))
            {
                if (baseUnitType == BaseUnitType.GameGear)
                    maskC |= (byte)PortIoCButtons.Start;
                else
                    pausePressed = pressed;
            }
            if (buttons.HasFlag(Buttons.Reset)) maskBMisc |= (byte)PortIoBMiscButtons.Reset;

            if (pressed)
            {
                portIoAB &= (byte)~maskAB;
                portIoBMisc &= (byte)~maskBMisc;
                portIoC &= (byte)~maskC;
            }
            else
            {
                portIoAB |= maskAB;
                portIoBMisc |= maskBMisc;
                portIoC |= maskC;
            }
        }

        public void Reset()
        {
            // TODO: more resetti things
            cpu.Reset();
            vdp.Reset();

            portMemoryControl = 0x00;
            portIoControl = portIoAB = portIoBMisc = 0xFF;
            lastHCounter = 0x00;

            portIoC = 0xC0;
            portParallelData = 0x7F;
            portDataDirNMI = 0xFF;
            portTxBuffer = 0x00;
            portRxBuffer = 0xFF;
            portSerialControl = 0x00;
            portStereoControl = 0xFF;
        }

        public void PowerOn()
        {
            Reset();

            isStopped = false;
        }

        public void PowerOff()
        {
            isStopped = true;
        }

        public void Execute()
        {
#if !DEBUG
            try
#endif
            {
                // TODO: fix timing

                while (!isStopped)
                {
                    long startTime = stopWatch.ElapsedMilliseconds;
                    long interval = (long)TimeSpan.FromSeconds(1.0 / GetFrameRate(isNtscSystem)).TotalMilliseconds;

                    int cyclesPerFrame = Z80.GetCPUClockCyclesPerFrame(isNtscSystem);
                    int cyclesPerLine = Z80.GetCPUClockCyclesPerScanline(isNtscSystem);

                    int totalCycles = 0, cycleDiff = 0;
                    while (totalCycles < cyclesPerFrame)
                    {
                        int cyclesInLine = cycleDiff;
                        while (cyclesInLine < cyclesPerLine)
                        {
                            int currentCycles = cpu.Execute();

                            HandleInterrupts();

                            if (vdp.Execute(currentCycles))
                                OnRenderScreen?.Invoke(this, new RenderEventArgs(vdp.OutputFramebuffer));

                            // TODO: verify, fix, whatever, I hate sound
                            psg.Execute((int)(currentCycles * (Z80.ClockDivider / VDP.ClockDivider)));

                            cyclesInLine += currentCycles;
                        }

                        cycleDiff = (cyclesInLine - cyclesPerLine);
                        totalCycles += cyclesInLine;
                    }

                    while (LimitFPS && stopWatch.ElapsedMilliseconds - startTime < interval)
                        Thread.Sleep(1);
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string message = string.Format("Exception occured: {0}\n\nEmulation thread has been stopped.", ex.Message);
                System.Windows.Forms.MessageBox.Show(message);

                isStopped = true;
            }
#endif
        }

        private void HandleInterrupts()
        {
            if (pausePressed && cpu.IFF1 && cpu.InterruptMode == 0x01)
            {
                pausePressed = false;
                cpu.ServiceInterrupt(0x0066);
            }

            if (vdp.IrqLineAsserted && cpu.IFF1 && cpu.InterruptMode == 0x01)
                cpu.ServiceInterrupt(0x0038);
        }

        private byte ReadIOPort(byte port)
        {
            if (baseUnitType == BaseUnitType.GameGear)
            {
                switch (port)
                {
                    case 0x00: return portIoC;
                    case 0x01: return portParallelData;
                    case 0x02: return portDataDirNMI;
                    case 0x03: return portTxBuffer;
                    case 0x04: return portRxBuffer;
                    case 0x05: return portSerialControl;
                    case 0x06: return portStereoControl;
                }
            }

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
            }

            return 0xAA;
        }

        private void WriteIOPort(byte port, byte value)
        {
            byte maskedPort = (byte)(port & 0xC1);

            switch (maskedPort & 0xF0)
            {
                case 0x00:
                    if (baseUnitType == BaseUnitType.GameGear && port >= 0x00 && port < 0x07)
                    {
                        /* Game Gear-only stuff */
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
            }
        }
    }
}
