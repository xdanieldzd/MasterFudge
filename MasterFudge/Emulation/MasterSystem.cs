using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using MasterFudge.Emulation.Memory;
using MasterFudge.Emulation.CPU;
using MasterFudge.Emulation.Cartridges;

namespace MasterFudge.Emulation
{
    public class MasterSystem
    {
        public const double MasterClockPAL = 53203424;
        public const double MasterClockNTSC = 53693175;
        public const double FramesPerSecPAL = 49.701459;
        public const double FramesPerSecNTSC = 59.922743;

        int cyclesPerFrame;

        MemoryMapper memoryMapper;

        Z80 cpu;
        WRAM wram;
        BaseCartridge cartridge;

        byte portMemoryControl, portIoControl;

        Thread mainThread;
        bool isPaused, isStopped;

        public MasterSystem(bool isNtsc)
        {
            memoryMapper = new MemoryMapper();

            cyclesPerFrame = (int)((isNtsc ? MasterClockNTSC : MasterClockPAL) / 15.0 / (isNtsc ? FramesPerSecNTSC : FramesPerSecPAL));
            cpu = new Z80(memoryMapper, ReadIOPort, WriteIOPort);
            wram = new WRAM();

            memoryMapper.AddMemoryArea(wram.GetMemoryAreaDescriptor());

            mainThread = new Thread(new ThreadStart(Execute)) { IsBackground = true, Name = "SMS" };
            isPaused = isStopped = true;

            Reset();
        }

        ~MasterSystem()
        {
            isStopped = true;
            while (mainThread.ThreadState != ThreadState.Stopped) { }
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
            if (mainThread.ThreadState == ThreadState.Running)
            {
                isPaused = false;
            }
            else
            {
                isPaused = isStopped = false;
                mainThread.Start();
            }
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Reset()
        {
            // TODO: more resetti things
            cpu.Reset();
            portMemoryControl = portIoControl = 0;
        }

        private void Execute()
        {
            try
            {
                while (!isStopped)
                {
                    if (!isPaused)
                    {
                        int currentCycles = 0;
                        while (currentCycles < cyclesPerFrame)
                        {
                            currentCycles += cpu.Execute();
                            //vdp
                            //sound
                            //irqs
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("paused");
                    }
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("Exception occured: {0}\n\nEmulation thread has been stopped.", ex.Message);
                System.Windows.Forms.MessageBox.Show(message);
            }
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
                    {
                        // V counter
                    }
                    else
                    {
                        // H counter
                    }
                    break;

                case 0x80:
                    // VDP
                    if ((port & 0x01) == 0)
                    {
                        // Data port
                    }
                    else
                    {
                        // Status flags
                    }
                    break;

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
                    {
                        // Memory control
                        portMemoryControl = value;
                    }
                    else
                    {
                        // I/O control
                        portIoControl = value;
                    }
                    break;

                case 0x40:
                    // PSG
                    break;

                case 0x80:
                    // VDP
                    if ((port & 0x01) == 0)
                    {
                        // Data port
                    }
                    else
                    {
                        // Control port
                    }
                    break;

                case 0xC0:
                    // No effect
                    break;
            }
        }
    }
}
