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

        double framesPerSecond;
        int cyclesPerFrame;

        MemoryMapper memoryMapper;

        Z80 cpu;
        WRAM wram;
        VDP vdp;
        BaseCartridge cartridge;

        byte portMemoryControl, portIoControl, portIoAB, portIoBMisc;
        bool isNtscSystem, isExportSystem;

        public event RenderScreenHandler OnRenderScreen;

        Stopwatch stopWatch;
        bool isStopped;

        public bool LimitFPS { get; set; }
        public bool DebugLogOpcodes { get { return cpu.DebugLogOpcodes; } set { cpu.DebugLogOpcodes = value; } }
        public bool CartridgeLoaded { get { return (cartridge != null); } }
        public string CartridgeFilename { get; private set; }

        public MasterSystem()
        {
            SetRegion(false, true);

            memoryMapper = new MemoryMapper();

            cpu = new Z80(memoryMapper, ReadIOPort, WriteIOPort);
            wram = new WRAM();
            vdp = new VDP();

            memoryMapper.AddMemoryArea(wram.GetMemoryAreaDescriptor());

            stopWatch = new Stopwatch();
            stopWatch.Start();

            isStopped = true;
            LimitFPS = false;
        }

        public void SetRegion(bool isNtsc, bool isExport)
        {
            isNtscSystem = isNtsc;
            isExportSystem = isExport;

            framesPerSecond = (isNtsc ? FramesPerSecNTSC : FramesPerSecPAL);
            cyclesPerFrame = (int)((isNtsc ? MasterClockNTSC : MasterClockPAL) / 15.0 / framesPerSecond);

            vdp?.SetTVSystem(isNtsc);
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
            memoryMapper.AddMemoryArea(cartridge.GetMappingRegisterAreaDescriptor());
        }

        public RomHeader GetCartridgeHeader()
        {
            return cartridge.Header;
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
            //try
            {
                // TODO: fix timing and frame limiter...

                while (!isStopped)
                {
                    long startTime = stopWatch.ElapsedMilliseconds;
                    long interval = (long)TimeSpan.FromSeconds(1.0 / framesPerSecond).TotalMilliseconds;

                    int totalCycles = 0;
                    while (totalCycles < cyclesPerFrame)
                    {
                        double currentCycles = cpu.Execute();

                        HandleInterrupts();

                        currentCycles *= 3.0;

                        if (vdp.Execute((int)(currentCycles / 2.0), cyclesPerFrame))
                        {
                            if (!isStopped)
                                OnRenderScreen?.Invoke(this, new RenderEventArgs(vdp.OutputFramebuffer));
                        }

                        // TODO: sound stuff here, too!

                        totalCycles += (int)currentCycles;
                    }

                    while (LimitFPS && stopWatch.ElapsedMilliseconds - startTime < (interval / 3.0) / 4.0)
                        Thread.Sleep(1);
                }
            }
            /*catch (Exception ex)
            {
                string message = string.Format("Exception occured: {0}\n\nEmulation thread has been stopped.", ex.Message);
                System.Windows.Forms.MessageBox.Show(message);

                isStopped = true;
            }*/
        }

        private void HandleInterrupts()
        {
            // TODO: pause button NMI

            if (vdp.InterruptPending && cpu.IFF1 && cpu.InterruptMode == 0x01)
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
                        return vdp.ReadHCounter();      // H counter

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
                        portIoControl = value;          // I/O control
                    break;

                case 0x40:
                    // PSG
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
