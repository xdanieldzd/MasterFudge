using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;

using MasterFudge.Emulation.CPU;
using MasterFudge.Emulation.Media;
using MasterFudge.Emulation.Graphics;
using MasterFudge.Emulation.Sound;
using MasterFudge.Emulation.IO;
using MasterFudge.Emulation.Units;

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

    [Flags]
    public enum Buttons
    {
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

    public partial class BaseUnitOld
    {
        public const double MasterClockPAL = 53203424;
        public const double MasterClockNTSC = 53693175;
        public const double FramesPerSecPAL = 49.701459;
        public const double FramesPerSecNTSC = 59.922743;

        public const BaseUnitRegion DefaultBaseUnitRegion = BaseUnitRegion.ExportNTSC;

        BaseUnitType baseUnitType;
        BaseUnitRegion baseUnitRegion;

        Z80 cpu;
        byte[] wram;
        VDP vdp;
        PSG psg;
        BaseMedia cartridge, card, bootstrap;

        byte portMemoryControl, portIoControl, portIoAB, portIoBMisc;
        byte lastHCounter;
        bool pausePressed;

        /* Game Gear only */
        byte portIoC, portParallelData, portDataDirNMI, portTxBuffer, portRxBuffer, portSerialControl, portStereoControl;

        /* SC-3000 only */
        i8255PPI ppi;
        SCKeyboard keyboard;

        // TODO: actually make memory accesses depend on these
        public bool isExpansionSlotEnabled { get { return !Utils.IsBitSet(portMemoryControl, 7); } }
        public bool isCartridgeSlotEnabled { get { return !Utils.IsBitSet(portMemoryControl, 6); } }
        public bool isCardSlotEnabled { get { return !Utils.IsBitSet(portMemoryControl, 5); } }
        public bool isWorkRamEnabled { get { return !Utils.IsBitSet(portMemoryControl, 4); } }
        public bool isBootstrapRomEnabled { get { return !Utils.IsBitSet(portMemoryControl, 3); } }
        public bool isIoChipEnabled { get { return !Utils.IsBitSet(portMemoryControl, 2); } }

        bool isNtscSystem { get { return (baseUnitRegion == BaseUnitRegion.JapanNTSC || baseUnitRegion == BaseUnitRegion.ExportNTSC); } }
        bool isExportSystem { get { return (baseUnitRegion == BaseUnitRegion.ExportNTSC || baseUnitRegion == BaseUnitRegion.ExportPAL); } }

        public bool IsNtscSystem { get { return isNtscSystem; } }
        public bool IsPalSystem { get { return !isNtscSystem; } }
        public bool IsExportSystem { get { return isExportSystem; } }
        public bool IsJapaneseSystem { get { return !isExportSystem; } }

        public event RenderScreenHandler OnRenderScreen;

        public bool IsStopped { get; private set; }
        public bool IsPaused { get; private set; }

        Stopwatch stopWatch;
        long startTime;
        int frameCounter;
        public double FramesPerSecond { get; private set; }

        public bool LimitFPS { get; set; }
        public bool DebugLogOpcodes { get { return cpu.DebugLogOpcodes; } set { cpu.DebugLogOpcodes = value; } }
        public bool CartridgeLoaded { get { return (cartridge != null); } }
        public string CartridgeFilename { get; private set; }

        public BaseUnitOld()
        {
            cpu = new Z80(ReadMemory, WriteMemory, ReadIOPort, WriteIOPort);
            wram = new byte[0x2000];
            vdp = new VDP();
            psg = new PSG();

            cartridge = null;
            card = null;
            bootstrap = null;

            ppi = new i8255PPI();
            keyboard = new SCKeyboard(ppi);

            IsStopped = true;
            IsPaused = false;

            stopWatch = new Stopwatch();
            stopWatch.Start();

            frameCounter = 0;
            FramesPerSecond = 0.0;

            LimitFPS = true;

            SetRegion(DefaultBaseUnitRegion);
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

        public BaseUnitType GetUnitType()
        {
            return baseUnitType;
        }

        public void LoadCartridge(string filename)
        {
            /*CartridgeFilename = filename;

            cartridge = BaseMedia.LoadMedia(filename);

            if (cartridge.RequestedUnitRegion != BaseUnitRegion.Default)
                SetRegion(cartridge.RequestedUnitRegion);

            if (cartridge.RequestedUnitType != BaseUnitType.Default)
                SetUnitType(cartridge.RequestedUnitType);
            else if (cartridge.Header.IsGameGear)
                SetUnitType(BaseUnitType.GameGear);
            else
                SetUnitType(BaseUnitType.MasterSystem);*/
        }

        public void LoadCartridgeRam(string filename)
        {
            if (!File.Exists(filename)) return;

            using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] data = new byte[file.Length];
                file.Read(data, 0, data.Length);
                cartridge.SetRamData(data);
            }
        }

        public void SaveCartridgeRam(string filename)
        {
            if (cartridge.HasOnBoardRam())
            {
                byte[] cartRam = cartridge.GetRamData();
                using (FileStream file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    file.Write(cartRam, 0, cartRam.Length);
                }
            }
        }

        public RomHeader GetCartridgeHeader()
        {
            //return cartridge.Header;
            return null;
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

        public void SetKeyboardData(KeyboardKeys keys, bool pressed)
        {
            keyboard.SetKeys(keys, pressed);
        }

        public void Reset()
        {
            // TODO: more resetti things
            cpu.Reset();
            vdp.Reset();
            psg.Reset();

            bootstrap = null;
            if (Configuration.BootstrapEnabled)
            {
                if (baseUnitType == BaseUnitType.MasterSystem)
                    bootstrap = BaseMedia.LoadMedia(Configuration.MasterSystemBootstrapPath);
                else if (baseUnitType == BaseUnitType.GameGear)
                    bootstrap = BaseMedia.LoadMedia(Configuration.GameGearBootstrapPath);
            }

            portMemoryControl = (byte)(bootstrap != null ? (baseUnitType == BaseUnitType.GameGear ? 0xA3 : 0xE3) : 0x00);
            portIoControl = portIoAB = portIoBMisc = 0xFF;
            lastHCounter = 0x00;

            portIoC = 0xC0;
            portParallelData = 0x7F;
            portDataDirNMI = 0xFF;
            portTxBuffer = 0x00;
            portRxBuffer = 0xFF;
            portSerialControl = 0x00;
            portStereoControl = 0xFF;

            ppi.Reset();
            keyboard.Reset();
        }

        public void PowerOn()
        {
            Reset();

            IsStopped = false;

            IsPaused = false;
        }

        public void PowerOff()
        {
            IsStopped = true;

            IsPaused = false;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        public void Execute()
        {
#if !DEBUG
            try
#endif
            {
                // TODO: fix timing

                while (!IsStopped)
                {
                    startTime = stopWatch.ElapsedMilliseconds;
                    long interval = (long)TimeSpan.FromSeconds(1.0 / GetFrameRate(isNtscSystem)).TotalMilliseconds;

                    if (!IsPaused)
                    {
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
                                psg.Execute(currentCycles);

                                cyclesInLine += currentCycles;
                            }

                            cycleDiff = (cyclesInLine - cyclesPerLine);
                            totalCycles += cyclesInLine;
                        }
                    }

                    while (LimitFPS && stopWatch.ElapsedMilliseconds - startTime < interval)
                        Thread.Sleep(1);

                    frameCounter++;
                    double timeDifference = (stopWatch.ElapsedMilliseconds - startTime);
                    if (timeDifference >= 1.0)
                    {
                        FramesPerSecond = (frameCounter / (timeDifference / 1000));
                        frameCounter = 0;
                    }
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string message = string.Format("Exception occured: {0}\n\nEmulation thread has been stopped.", ex.Message);
                System.Windows.Forms.MessageBox.Show(message);

                IsStopped = true;
            }
#endif
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

        private byte ReadMemory(ushort address)
        {
            switch (baseUnitType)
            {
                case BaseUnitType.MasterSystem: return ReadMemorySMS(address);
                case BaseUnitType.GameGear: return ReadMemoryGG(address);
                case BaseUnitType.SC3000: return ReadMemorySGSC(address);

                default: throw new NotImplementedException(string.Format("Memory read not implemented for {0}", baseUnitType));
            }
        }

        private void WriteMemory(ushort address, byte value)
        {
            switch (baseUnitType)
            {
                case BaseUnitType.MasterSystem: WriteMemorySMS(address, value); break;
                case BaseUnitType.GameGear: WriteMemoryGG(address, value); break;
                case BaseUnitType.SC3000: WriteMemorySGSC(address, value); break;

                default: throw new NotImplementedException(string.Format("Memory write not implemented for {0}", baseUnitType));
            }
        }

        private byte ReadIOPort(byte port)
        {
            switch (baseUnitType)
            {
                case BaseUnitType.MasterSystem: return ReadIOPortSMS(port);
                case BaseUnitType.GameGear: return ReadIOPortGG(port);
                case BaseUnitType.SC3000: return ReadIOPortSGSC(port);

                default: throw new NotImplementedException(string.Format("Port read not implemented for {0}", baseUnitType));
            }
        }

        private void WriteIOPort(byte port, byte value)
        {
            switch (baseUnitType)
            {
                case BaseUnitType.MasterSystem: WriteIOPortSMS(port, value); break;
                case BaseUnitType.GameGear: WriteIOPortGG(port, value); break;
                case BaseUnitType.SC3000: WriteIOPortSGSC(port, value); break;

                default: throw new NotImplementedException(string.Format("Port write not implemented for {0}", baseUnitType));
            }
        }
    }
}
