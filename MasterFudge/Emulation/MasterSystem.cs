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

    public partial class MasterSystem
    {
        public event RenderScreenHandler OnRenderScreen;

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

        // TODO: uuhhhh threading shit is actually broken and just runs however the fuck it wants, fix maybe?
        Thread mainThread;

        bool isStopped;
        public bool LimitFPS { get; set; }
        public bool DebugLogOpcodes { get { return cpu.DebugLogOpcodes; } set { cpu.DebugLogOpcodes = value; } }

        public MasterSystem(bool isNtsc, RenderScreenHandler onRenderScreen)
        {
            OnRenderScreen = onRenderScreen;

            framesPerSecond = (isNtsc ? FramesPerSecNTSC : FramesPerSecPAL);
            cyclesPerFrame = (int)((isNtsc ? MasterClockNTSC : MasterClockPAL) / 15.0 / framesPerSecond);

            memoryMapper = new MemoryMapper();

            cpu = new Z80(memoryMapper, ReadIOPort, WriteIOPort);
            //cpu.DebugLogOpcodes = true;
            wram = new WRAM();
            vdp = new VDP(isNtsc, onRenderScreen);

            memoryMapper.AddMemoryArea(wram.GetMemoryAreaDescriptor());

            mainThread = new Thread(new ThreadStart(Execute)) { Priority = ThreadPriority.AboveNormal, Name = "SMS" };

            isStopped = false;
            LimitFPS = false;
        }

        ~MasterSystem()
        {
            mainThread.Join();
            while (mainThread.ThreadState != System.Threading.ThreadState.Stopped) { }
        }

        public static bool IsBitSet(byte value, int bit)
        {
            return ((value & (1 << bit)) != 0);
        }

        public void LoadCartridge(string filename)
        {
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

        public void Run()
        {
            mainThread.Start();
        }

        public void Stop()
        {
            mainThread.Abort();
        }

        public void Reset()
        {
            // TODO: more resetti things
            cpu.Reset();
            vdp.Reset();
            portMemoryControl = 0x00;
            portIoControl = portIoAB = portIoBMisc = 0xFF;
        }

        private void Execute()
        {
            try
            {
                Reset();

                Stopwatch sw = new Stopwatch();
                sw.Start();

                // TODO: fix timing and frame limiter...

                while (!isStopped)
                {
                    long startTime = sw.ElapsedMilliseconds;
                    long interval = (long)TimeSpan.FromSeconds(1.0 / framesPerSecond).TotalMilliseconds;

                    int totalCycles = 0;
                    while (totalCycles < cyclesPerFrame)
                    {
                        double currentCycles = cpu.Execute();

                        currentCycles *= 3.0;

                        vdp.Execute((int)(currentCycles / 2.0), cyclesPerFrame);
                        HandleInterrupts();
                        // TODO: sound stuff here, too!

                        totalCycles += (int)currentCycles;
                    }

                    while (LimitFPS && sw.ElapsedMilliseconds - startTime < (interval / 3.0) / 3.0)
                        Thread.Sleep(1);
                }
            }
            catch (ThreadAbortException) { /* probably not good practice, but what do I care */ }
            catch (Exception ex)
            {
                string message = string.Format("Exception occured: {0}\n\nEmulation thread has been stopped.", ex.Message);
                System.Windows.Forms.MessageBox.Show(message);
            }
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
                        // Check if export SMS
                        if (false)
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

    public class RenderEventArgs : EventArgs
    {
        public int FrameWidth { get; private set; }
        public int FrameHeight { get; private set; }
        public byte[] FrameData { get; private set; }

        public RenderEventArgs(int width, int height, byte[] data)
        {
            FrameWidth = width;
            FrameHeight = height;
            FrameData = data;
        }
    }
}
