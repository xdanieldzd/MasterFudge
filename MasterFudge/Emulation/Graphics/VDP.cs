using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Graphics
{
    public class VDP
    {
        public const double NumScanlinesPAL = 313.0;
        public const double NumScanlinesNTSC = 262.0;

        public const double NumVisibleLinesLow = 192.0;
        public const double NumVisibleLinesMed = 224.0;
        public const double NumVisibleLinesHigh = 240.0;

        public const double NumPixelsPerLine = 256.0;

        RenderScreenHandler renderScreen;

        byte[] registers, vram, cram;
        byte[] outputFramebuffer;

        bool isSecondControlWrite;
        ushort controlWord;
        byte readBuffer, statusFlags;

        byte codeRegister { get { return (byte)((controlWord >> 14) & 0x03); } }
        ushort addressRegister
        {
            get { return (ushort)(controlWord & 0x3FFF); }
            set { controlWord = (ushort)((codeRegister << 14) | ((addressRegister + 1) & 0x3FFF)); }
        }

        public bool InterruptPending { get; private set; }
        int currentScanline, hCounter, screenHeight, lineInterruptCounter;

        bool isLineInterruptEnabled { get { return MasterSystem.IsBitSet(registers[0x00], 4); } }
        bool isFrameInterruptEnabled { get { return MasterSystem.IsBitSet(registers[0x01], 5); } }
        bool isDisplayBlanked { get { return !MasterSystem.IsBitSet(registers[0x01], 6); } }
        bool isVScrollPartiallyDisabled { get { return MasterSystem.IsBitSet(registers[0x01], 5); } }   // columns 24-31, i.e. pixels 192-256(?)
        bool isHScrollPartiallyDisabled { get { return MasterSystem.IsBitSet(registers[0x01], 5); } }   // rows 0-1, i.e. pixels 0-16(?)

        int displayMode { get { return (((registers[0x01] >> 4) & 0x01) | (registers[0x00] & 0x02) | ((registers[0x01] >> 1) & 0x04) | ((registers[0x00] & 0x04) << 1)); } }

        ushort nametableBaseAddress { get { return (ushort)((registers[0x02] & 0x0E) << 10); } }
        ushort spriteAttibTableBaseAddress { get { return (ushort)((registers[0x05] & 0x7E) << 7); } }
        ushort spritePatternGenBaseAddress { get { return (ushort)((registers[0x06] & 0x04) << 11); } }

        int overscanBgColor { get { return (registers[0x07] & 0x0F); } }
        int backgroundXScroll { get { return registers[0x08]; } }
        int backgroundYScroll { get { return registers[0x09]; } }

        int[] backgroundXScrollCache;

        public VDP(RenderScreenHandler onRenderScreen)
        {
            renderScreen = onRenderScreen;

            registers = new byte[0x10];
            vram = new byte[0x4000];
            cram = new byte[0x20];

            outputFramebuffer = new byte[(int)(NumPixelsPerLine * NumVisibleLinesHigh) * 4];

            backgroundXScrollCache = new int[(int)NumScanlinesPAL];
        }

        public void Reset()
        {
            isSecondControlWrite = false;
            controlWord = 0x0000;
            readBuffer = statusFlags = 0;

            currentScanline = hCounter = 0;
            screenHeight = (int)NumVisibleLinesLow;
        }

        public void Execute(int currentCycles, int cyclesPerFrame, int numScanlines)
        {
            int cyclesPerLine = ((cyclesPerFrame / numScanlines) * 3);

            InterruptPending = (MasterSystem.IsBitSet(statusFlags, 7) && MasterSystem.IsBitSet(registers[0x01], 5));

            hCounter = ((hCounter + currentCycles) % (cyclesPerLine + 1));

            if ((hCounter + currentCycles) > cyclesPerLine)
            {
                backgroundXScrollCache[currentScanline] = backgroundXScroll;

                currentScanline++;
                hCounter = 0;

                if (currentScanline > (screenHeight + 1))
                    lineInterruptCounter = registers[0x0A];
                else
                {
                    lineInterruptCounter--;
                    if (lineInterruptCounter < 0)
                    {
                        lineInterruptCounter = registers[0x0A];
                        InterruptPending = (isLineInterruptEnabled);
                    }
                }

                if (currentScanline == (screenHeight + 1))
                {
                    // Frame interrupt
                    statusFlags |= 0x80;
                }
                else if (currentScanline == numScanlines)
                {
                    currentScanline = 0;
                    Render();
                }
            }
        }

        private void Render()
        {
            ClearFramebuffer();

            if (!isDisplayBlanked)
            {
                RenderBackground();

                // TODO: sprites, etc, etc
            }

            renderScreen?.Invoke(this, new RenderEventArgs((int)NumPixelsPerLine, screenHeight, outputFramebuffer));
        }

        private void ClearFramebuffer()
        {
            for (int i = 0; i < outputFramebuffer.Length; i += 4)
                Buffer.BlockCopy(GetColorAsArgb8888(1, overscanBgColor), 0, outputFramebuffer, i, 4);
        }

        private void RenderBackground()
        {
            // TODO: scrolling and whatever else...

            int numTilesPerLine = (int)(NumPixelsPerLine / 8);
            for (int line = 0; line < screenHeight; line++)
            {
                ushort nametableAddress = (ushort)(nametableBaseAddress + ((line / 8) * (numTilesPerLine * 2)));
                for (int tile = 0; tile < numTilesPerLine; tile++)
                {
                    ushort ntData = (ushort)((vram[nametableAddress + (tile * 2) + 1] << 8) | vram[nametableAddress + (tile * 2)]);

                    int tileIndex = (ntData & 0x01FF);
                    bool hFlip = ((ntData & 0x200) == 0x200);
                    bool vFlip = ((ntData & 0x400) == 0x400);
                    int palette = ((ntData & 0x800) >> 11);
                    bool priority = ((ntData & 0x1000) == 0x400);

                    ushort tileAddress = (ushort)((tileIndex * 0x20) + ((line % 8) * 4));
                    for (int pixel = 0; pixel < 8; pixel++)
                    {
                        int c = (((vram[tileAddress + 0] >> (7 - pixel)) & 0x1) << 0);
                        c |= (((vram[tileAddress + 1] >> (7 - pixel)) & 0x1) << 1);
                        c |= (((vram[tileAddress + 2] >> (7 - pixel)) & 0x1) << 2);
                        c |= (((vram[tileAddress + 3] >> (7 - pixel)) & 0x1) << 3);

                        int outputAddress = (int)(((line * NumPixelsPerLine) * 4) + (((tile * 8) + pixel) * 4));
                        Buffer.BlockCopy(GetColorAsArgb8888(palette, c), 0, outputFramebuffer, outputAddress, 4);
                    }
                }
            }
        }

        public byte[] DumpVideoRam()
        {
            return vram;
        }

        public byte[] DumpColorRam()
        {
            return cram;
        }

        public byte[] GetColorAsArgb8888(int palette, int color)
        {
            int offset = ((palette * 16) + color);

            byte r = (byte)(cram[offset] & 0x3);
            r |= (byte)((r << 6) | (r << 4) | (r << 2));
            byte g = (byte)((cram[offset] >> 2) & 0x3);
            g |= (byte)((g << 6) | (g << 4) | (g << 2));
            byte b = (byte)((cram[offset] >> 4) & 0x3);
            b |= (byte)((b << 6) | (b << 4) | (b << 2));

            return new byte[] { b, g, r, 0xFF };
        }

        public byte ReadVCounter()
        {
            return (byte)currentScanline;
        }

        public byte ReadHCounter()
        {
            return (byte)(hCounter >> 1);
        }

        public byte ReadControlPort()
        {
            isSecondControlWrite = false;

            // Unset frame interrupt flag
            statusFlags &= 0x7F;

            return statusFlags;
        }

        public void WriteControlPort(byte value)
        {
            if (!isSecondControlWrite)
                controlWord = (ushort)((controlWord & 0xFF00) | value);
            else
            {
                controlWord = (ushort)((controlWord & 0x00FF) | (value << 8));

                switch (codeRegister)
                {
                    case 0x00: readBuffer = vram[addressRegister++]; break;
                    case 0x01: break;
                    case 0x02: WriteRegister((byte)((controlWord >> 8) & 0x0F), (byte)(controlWord & 0xFF)); break;
                    case 0x03: break;
                }
            }

            isSecondControlWrite = !isSecondControlWrite;
        }

        public byte ReadDataPort()
        {
            isSecondControlWrite = false;
            statusFlags = 0;

            byte data = readBuffer;

            switch (codeRegister)
            {
                case 0x00:
                case 0x01:
                case 0x02:
                    readBuffer = vram[addressRegister];
                    break;
                case 0x03:
                    readBuffer = cram[(addressRegister & 0x001F)];
                    break;
            }

            addressRegister++;

            return data;
        }

        public void WriteDataPort(byte value)
        {
            isSecondControlWrite = false;

            readBuffer = value;

            switch (codeRegister)
            {
                case 0x00:
                case 0x01:
                case 0x02:
                    vram[addressRegister] = value;
                    break;
                case 0x03:
                    cram[(addressRegister & 0x001F)] = value;
                    break;
            }

            addressRegister++;
        }

        private void WriteRegister(byte register, byte value)
        {
            registers[register] = value;
        }
    }
}
