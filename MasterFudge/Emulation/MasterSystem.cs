using System;
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

    public partial class MasterSystem
    {
        public const double MasterClockPAL = 53203424;
        public const double MasterClockNTSC = 53693175;
        public const double FramesPerSecPAL = 49.701459;
        public const double FramesPerSecNTSC = 59.922743;

        MemoryMapper memoryMapper;

        Z80 cpu;
        WRAM wram;
        VDP vdp;
        PSG psg;
        BaseCartridge cartridge;

        byte portMemoryControl, portIoControl, portIoAB, portIoBMisc;
        byte lastHCounter;

        bool isNtscSystem, isExportSystem;

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

        public MasterSystem()
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

            SetRegion(true, true);
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

        public void SetRegion(bool isNtsc, bool isExport)
        {
            isNtscSystem = isNtsc;
            isExportSystem = isExport;

            vdp?.SetTVSystem(isNtsc);
            psg?.SetTVSystem(isNtsc);
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
        public void SetJoypadPressed(byte keyBit)
        {
            // TODO: player 2 buttons
            portIoAB &= (byte)~keyBit;
        }

        public void SetJoypadReleased(byte keyBit)
        {
            // TODO: player 2 buttons
            portIoAB |= keyBit;
        }

        public void Reset()
        {
            // TODO: more resetti things
            cpu.Reset();
            vdp.Reset();
            portMemoryControl = 0x00;
            portIoControl = portIoAB = portIoBMisc = 0xFF;
            lastHCounter = 0x00;
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

                    int totalCycles = 0, cycleDiff = 0;
                    while (totalCycles < Z80.GetCPUClockCyclesPerFrame(isNtscSystem))
                    {
                        int cyclesInLine = cycleDiff;
                        while (cyclesInLine < Z80.GetCPUClockCyclesPerScanline(isNtscSystem))
                        {
                            int currentCycles = cpu.Execute();

                            HandleInterrupts();

                            if (vdp.Execute(currentCycles))
                            {
                                if (!isStopped)
                                    OnRenderScreen?.Invoke(this, new RenderEventArgs(vdp.OutputFramebuffer));
                            }

                            // TODO: verify
                            psg.Execute((int)(currentCycles * (Z80.ClockDivider / VDP.ClockDivider)));

                            cyclesInLine += currentCycles;
                        }

                        cycleDiff = (cyclesInLine - Z80.GetCPUClockCyclesPerScanline(isNtscSystem));
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
            // TODO: pause button NMI

            if (vdp.IrqLineAsserted && cpu.IFF1 && cpu.InterruptMode == 0x01)
                cpu.ServiceInterrupt(0x0038);
        }

        // TODO: all the IO port stuff

        // 0xC1 mask via http://www.smspower.org/uploads/Development/smstech-20021112.txt, ch3 I/O - A7,A6,A0
        private byte ReadIOPort(byte port)
        {
            port = (byte)(port & 0xC1);

            switch (port & 0xF0)
            {
                case 0x00:
                    // Uh, behave like SMS2 for now
                    return 0xFF;

                case 0x40:
                    // Counters
                    if ((port & 0x01) == 0)
                        return vdp.ReadVCounter();      // V counter
                    else
                        return lastHCounter;            // H counter

                case 0x80:
                    // VDP
                    if ((port & 0x01) == 0)
                        return vdp.ReadDataPort();      // Data port
                    else
                        return vdp.ReadControlPort();   // Status flags

                case 0xC0:
                    if ((port & 0x01) == 0)
                        return portIoAB;                // IO port A/B register
                    else
                    {
                        // IO port B/misc register
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
            port = (byte)(port & 0xC1);

            switch (port & 0xF0)
            {
                case 0x00:
                    // System stuff
                    if ((port & 0x01) == 0)
                        portMemoryControl = value;      // Memory control
                    else
                    {
                        // I/O control
                        if ((portIoControl & 0x0A) == 0x00 && ((value & 0x02) == 0x02 || (value & 0x08) == 0x08))
                            lastHCounter = vdp.ReadHCounter();
                        portIoControl = value;
                    }
                    break;

                case 0x40:
                    // PSG
                    psg.WriteData(value);
                    break;

                case 0x80:
                    // VDP
                    if ((port & 0x01) == 0)
                        vdp.WriteDataPort(value);       // Data port
                    else
                        vdp.WriteControlPort(value);    // Control port
                    break;

                case 0xC0:
                    // No effect
                    break;
            }
        }
    }
}
