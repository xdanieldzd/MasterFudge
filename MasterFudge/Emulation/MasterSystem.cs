using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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

        int cyclesPerFrame, numScanlines;

        MemoryMapper memoryMapper;

        Z80 cpu;
        WRAM wram;
        VDP vdp;
        BaseCartridge cartridge;

        byte portMemoryControl, portIoControl;

        // TODO: uuhhhh threading shit is actually broken and just runs however the fuck it wants, fix maybe?
        Thread mainThread;
        static ManualResetEvent threadReset;

        public bool IsPaused { get { return (mainThread?.ThreadState == ThreadState.Running || mainThread?.ThreadState == ThreadState.Background); } }

        public MasterSystem(bool isNtsc, RenderScreenHandler onRenderScreen)
        {
            OnRenderScreen = onRenderScreen;

            cyclesPerFrame = (int)((isNtsc ? MasterClockNTSC : MasterClockPAL) / 15.0 / (isNtsc ? FramesPerSecNTSC : FramesPerSecPAL));
            numScanlines = (int)(isNtsc ? VDP.NumScanlinesNTSC : VDP.NumScanlinesPAL);

            memoryMapper = new MemoryMapper();

            cpu = new Z80(memoryMapper, ReadIOPort, WriteIOPort);
            wram = new WRAM();
            vdp = new VDP(onRenderScreen);

            memoryMapper.AddMemoryArea(wram.GetMemoryAreaDescriptor());

            mainThread = new Thread(new ThreadStart(Execute)) { IsBackground = true, Name = "SMS" };
            threadReset = new ManualResetEvent(false);
        }

        ~MasterSystem()
        {
            mainThread.Join();
            while (mainThread.ThreadState != ThreadState.Stopped) { }
        }

        public static bool IsBitSet(byte value, int bit)
        {
            return ((value & (1 << bit)) != 0);
        }

        public void LoadCartridge(string filename)
        {
            cartridge = BaseCartridge.LoadCartridge<BaseCartridge>(filename);
            memoryMapper.AddMemoryArea(cartridge.GetMemoryAreaDescriptor());
        }

        public RomHeader GetCartridgeHeader()
        {
            return cartridge.Header;
        }

        public void Run()
        {
            mainThread.Start();
            threadReset.Set();
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
        }

        // BAH, JUNK, REMOVE AGAIN ASAP
        [Obsolete]
        public System.Drawing.Color GetPaletteColorDebug(int palette, int color)
        {
            return System.Drawing.Color.FromArgb(BitConverter.ToInt32(vdp.GetColorAsArgb8888(palette, color), 0));
        }

        private void Execute()
        {
            try
            {
                Reset();

                while (true)
                {
                    int totalCycles = 0;
                    while (totalCycles < cyclesPerFrame)
                    {
                        int currentCycles = cpu.Execute();

                        vdp.Execute(currentCycles, cyclesPerFrame, numScanlines);
                        //sound
                        //irqs

                        HandleInterrupts();

                        totalCycles += currentCycles;
                    }
                    threadReset.WaitOne();
                }
            }
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
                    break;

                case 0x80:
                    // VDP
                    if ((port & 0x01) == 0)
                        return vdp.ReadDataPort();      // Data port
                    else
                        return vdp.ReadControlPort();   // Status flags

                case 0xC0:
                    if ((port & 0x01) == 0)
                    {
                        // IO port A/B register
                    }
                    else
                    {
                        // IO port B/misc register
                    }
                    break;
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
