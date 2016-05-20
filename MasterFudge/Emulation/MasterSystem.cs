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

        Thread mainThread;
        bool isPaused, isStopped;

        public MasterSystem(bool isNtsc)
        {
            memoryMapper = new MemoryMapper();

            cyclesPerFrame = (int)((isNtsc ? MasterClockNTSC : MasterClockPAL) / 15.0 / (isNtsc ? FramesPerSecNTSC : FramesPerSecPAL));
            cpu = new Z80(memoryMapper);
            wram = new WRAM();

            memoryMapper.AddMemoryArea(wram.GetMemoryAreaDescriptor());

            mainThread = new Thread(new ThreadStart(Execute)) { IsBackground = true, Name = "SMS" };
            isPaused = isStopped = true;
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
    }
}
