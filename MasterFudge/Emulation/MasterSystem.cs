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

    [Flags]
    public enum JoypadInput
    {
        None = 0,
        Up = (1 << 0),
        Down = (1 << 1),
        Left = (1 << 2),
        Right = (1 << 3),
        Button1 = (1 << 4),
        Button2 = (1 << 5),
        ResetButton = (1 << 6)
    }

    public partial class MasterSystem
    {
        public event RenderScreenHandler OnRenderScreen;

        public const double MasterClockPAL = 53203424;
        public const double MasterClockNTSC = 53693175;
        public const double FramesPerSecPAL = 49.701459;
        public const double FramesPerSecNTSC = 59.922743;

        double framesPerSecond;
        int cyclesPerFrame, numScanlines;

        MemoryMapper memoryMapper;

        Z80 cpu;
        WRAM wram;
        VDP vdp;
        BaseCartridge cartridge;

        byte portMemoryControl, portIoControl, portIoAB, portIoBMisc;

        // TODO: uuhhhh threading shit is actually broken and just runs however the fuck it wants, fix maybe?
        Thread mainThread;
        static ManualResetEvent threadReset;

        public bool IsPaused { get { return (mainThread?.ThreadState == System.Threading.ThreadState.Running || mainThread?.ThreadState == System.Threading.ThreadState.Background); } }
        public bool LimitFPS { get; set; }
        public bool DebugLogOpcodes { get { return cpu.DebugLogOpcodes; } set { cpu.DebugLogOpcodes = value; } }

        public MasterSystem(bool isNtsc, RenderScreenHandler onRenderScreen)
        {
            OnRenderScreen = onRenderScreen;

            framesPerSecond = (isNtsc ? FramesPerSecNTSC : FramesPerSecPAL);
            cyclesPerFrame = (int)((isNtsc ? MasterClockNTSC : MasterClockPAL) / 15.0 / framesPerSecond);
            numScanlines = (int)(isNtsc ? VDP.NumScanlinesNTSC : VDP.NumScanlinesPAL);

            memoryMapper = new MemoryMapper();

            cpu = new Z80(memoryMapper, ReadIOPort, WriteIOPort);
            //cpu.DebugLogOpcodes = true;
            wram = new WRAM();
            vdp = new VDP(onRenderScreen);

            memoryMapper.AddMemoryArea(wram.GetMemoryAreaDescriptor());

            mainThread = new Thread(new ThreadStart(Execute)) { IsBackground = true, Name = "SMS" };
            threadReset = new ManualResetEvent(false);

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

        public void SetJoypadInput(JoypadInput p1, JoypadInput p2)
        {
            // TODO: IO port control (the active high/low stuff)

            int data1 = 0xFF, data2 = 0xFF;

            if (p1.HasFlag(JoypadInput.Up)) data1 &= ~(1 << 0);
            if (p1.HasFlag(JoypadInput.Down)) data1 &= ~(1 << 1);
            if (p1.HasFlag(JoypadInput.Left)) data1 &= ~(1 << 2);
            if (p1.HasFlag(JoypadInput.Right)) data1 &= ~(1 << 3);
            if (p1.HasFlag(JoypadInput.Button1)) data1 &= ~(1 << 4);
            if (p1.HasFlag(JoypadInput.Button2)) data1 &= ~(1 << 5);

            if (p2.HasFlag(JoypadInput.Up)) data1 &= ~(1 << 6);
            if (p2.HasFlag(JoypadInput.Down)) data1 &= ~(1 << 7);
            if (p2.HasFlag(JoypadInput.Left)) data2 &= ~(1 << 0);
            if (p2.HasFlag(JoypadInput.Right)) data2 &= ~(1 << 1);
            if (p2.HasFlag(JoypadInput.Button1)) data2 &= ~(1 << 2);
            if (p2.HasFlag(JoypadInput.Button2)) data2 &= ~(1 << 3);

            if (p1.HasFlag(JoypadInput.ResetButton) || p2.HasFlag(JoypadInput.ResetButton)) data2 &= ~(1 << 4);

            portIoAB = (byte)data1;
            portIoBMisc = (byte)data2;
        }

        public void Run()
        {
            mainThread.Start();
            threadReset.Set();
        }

        public void Stop()
        {
            mainThread.Abort();
            threadReset.Reset();
        }

        public void Pause()
        {
            threadReset.Reset();
        }

        public void Resume()
        {
            threadReset.Set();
        }

        public void Reset()
        {
            // TODO: more resetti things
            cpu.Reset();
            vdp.Reset();
            portMemoryControl = portIoControl = 0;
            portIoAB = portIoBMisc = 0xFF;
        }

        private void Execute()
        {
            try
            {
                Reset();

                Stopwatch sw = new Stopwatch();
                sw.Start();

                while (true)
                {
                    long startTime = sw.ElapsedMilliseconds;
                    long interval = (long)TimeSpan.FromSeconds(1.0 / framesPerSecond).TotalMilliseconds;

                    int totalCycles = 0;
                    while (totalCycles < cyclesPerFrame)
                    {
                        int currentCycles = cpu.Execute();

                        vdp.Execute(currentCycles, cyclesPerFrame, numScanlines);
                        HandleInterrupts();
                        // TODO: sound stuff here, too!

                        totalCycles += currentCycles;
                    }

                    threadReset.WaitOne();

                    while (LimitFPS && sw.ElapsedMilliseconds - startTime < interval)
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
