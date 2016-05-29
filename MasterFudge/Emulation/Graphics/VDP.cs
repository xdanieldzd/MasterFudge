using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Graphics
{
    public class VDP
    {
        public const int NumScanlinesPAL = 313;
        public const int NumScanlinesNTSC = 262;

        public const int NumVisibleLinesLow = 192;
        public const int NumVisibleLinesMed = 224;
        public const int NumVisibleLinesHigh = 240;

        public const int NumPixelsPerLine = 256;

        public const double ClockDivider = 5.0;

        byte[] registers, vram, cram;

        bool isNtsc;
        int numScanlines;

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

        int currentScanline, vCounter, hCounter, screenHeight, nametableHeight, lineInterruptCounter, backgroundVScroll;

        bool isLineInterruptEnabled { get { return MasterSystem.IsBitSet(registers[0x00], 4); } }
        bool isFrameInterruptEnabled { get { return MasterSystem.IsBitSet(registers[0x01], 5); } }
        bool isDisplayBlanked { get { return !MasterSystem.IsBitSet(registers[0x01], 6); } }
        bool isVScrollPartiallyDisabled { get { return MasterSystem.IsBitSet(registers[0x01], 5); } }   // columns 24-31, i.e. pixels 192-256(?)
        bool isHScrollPartiallyDisabled { get { return MasterSystem.IsBitSet(registers[0x01], 5); } }   // rows 0-1, i.e. pixels 0-16(?)
        bool isLargeSprites { get { return MasterSystem.IsBitSet(registers[0x01], 1); } }
        bool isZoomedSprites { get { return MasterSystem.IsBitSet(registers[0x01], 0); } }

        bool isMode4 { get { return MasterSystem.IsBitSet(registers[0x00], 2); } }

        ushort nametableBaseAddress { get { return (ushort)((registers[0x02] & 0x0E) << 10); } }
        ushort spriteAttribTableBaseAddress { get { return (ushort)((registers[0x05] & 0x7E) << 7); } }
        ushort spritePatternGenBaseAddress { get { return (ushort)((registers[0x06] & 0x04) << 11); } }

        int overscanBgColor { get { return (registers[0x07] & 0x0F); } }
        int backgroundHScroll { get { return registers[0x08]; } }

        byte[] overscanBgColorArgb;

        int[] backgroundXScrollCache;
        int[] spriteBuffer;

        enum PixelDrawn : byte
        {
            None = 0,
            BackgroundLowPriority = (1 << 0),
            BackgroundHighPriority = (1 << 1),
            Sprite = (1 << 2)
        }
        PixelDrawn[] screenDrawnPixels;

        public byte[] OutputFramebuffer { get; private set; }
        int outputFramebufferStartAddress { get { return (((NumVisibleLinesHigh - screenHeight) / 4) * NumPixelsPerLine) * 4; } }

        public VDP()
        {
            SetTVSystem(false);

            registers = new byte[0x10];
            vram = new byte[0x4000];
            cram = new byte[0x20];

            backgroundXScrollCache = new int[NumVisibleLinesHigh];
            spriteBuffer = new int[8];
            screenDrawnPixels = new PixelDrawn[NumPixelsPerLine * NumVisibleLinesHigh];

            OutputFramebuffer = new byte[(NumPixelsPerLine * NumVisibleLinesHigh) * 4];

            Reset();
        }

        public static int GetVDPClockCyclesPerFrame(bool isNtsc)
        {
            return (int)(MasterSystem.GetMasterClockCyclesPerFrame(isNtsc) / ClockDivider);
        }

        public static int GetVDPClockCyclesPerScanline(bool isNtsc)
        {
            return (int)(MasterSystem.GetMasterClockCyclesPerScanline(isNtsc) / ClockDivider);
        }

        public void Reset()
        {
            isSecondControlWrite = false;
            controlWord = 0x0000;
            readBuffer = statusFlags = 0;

            screenHeight = NumVisibleLinesLow;

            currentScanline = vCounter = hCounter = 0;
            nametableHeight = 224;
            lineInterruptCounter = 255;
            backgroundVScroll = 0;
        }

        public void SetTVSystem(bool ntsc)
        {
            isNtsc = ntsc;
            numScanlines = (isNtsc ? NumScanlinesNTSC : NumScanlinesPAL);
        }

        public bool Execute(int currentCycles)
        {
            // TODO: all the timing!

            AdjustVCounter();

            int currentHCounter = hCounter;
            bool nextLine = false;

            InterruptPending = (MasterSystem.IsBitSet(statusFlags, 7) && MasterSystem.IsBitSet(registers[0x01], 5));

            nextLine = ((currentHCounter + currentCycles) > GetVDPClockCyclesPerScanline(isNtsc));
            hCounter = ((hCounter + currentCycles) % (GetVDPClockCyclesPerScanline(isNtsc) + 1));

            if (nextLine)
            {
                currentScanline++;
                hCounter = 0;

                if (currentScanline > (screenHeight + 1))
                {
                    lineInterruptCounter = registers[0x0A];
                    backgroundVScroll = registers[0x09];

                    if (isMode4)
                    {
                        if (MasterSystem.IsBitSet(registers[0x01], 3))
                        {
                            // 240-line mode
                            screenHeight = NumVisibleLinesHigh;
                            nametableHeight = 256;
                        }
                        else if (MasterSystem.IsBitSet(registers[0x01], 4))
                        {
                            // 224-line mode
                            screenHeight = NumVisibleLinesMed;
                            nametableHeight = 256;
                        }
                        else
                        {
                            // 192-line mode
                            screenHeight = NumVisibleLinesLow;
                            nametableHeight = 224;
                        }
                    }
                    else
                    {
                        // TODO: Modes 0-3, used by "non-SMS" games (SG-1000, SC-3000) ?
                    }
                }
                else
                {
                    backgroundXScrollCache[currentScanline] = backgroundHScroll;

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

                    return true;
                }
            }

            return false;
        }

        private void AdjustVCounter()
        {
            vCounter = currentScanline;

            // TODO: odd thing, verify this is correct http://www.smspower.org/Development/ScanlineCounter
            if (isNtsc)
            {
                if (screenHeight == NumVisibleLinesHigh)
                {
                    // Invalid on NTSC?
                    if (currentScanline > 0xFF)
                        vCounter = (currentScanline - 0xFF);
                }
                else if (screenHeight == NumVisibleLinesMed)
                {
                    if (currentScanline > 0xEA)
                        vCounter = (currentScanline - 0x05);
                }
                else
                {
                    if (currentScanline > 0xDA)
                        vCounter = (currentScanline - 0x05);
                }
            }
            else
            {
                if (screenHeight == NumVisibleLinesHigh)
                {
                    if (currentScanline > 0xFF && currentScanline < 0xFF + 0x0A)
                        vCounter = (currentScanline - 0xFF);
                    else
                        vCounter = (currentScanline - 0x37);
                }
                else if (screenHeight == NumVisibleLinesMed)
                {
                    if (currentScanline > 0xFF && currentScanline < 0xFF + 0x02)
                        vCounter = (currentScanline - 0xFF);
                    else
                        vCounter = (currentScanline - 0x37);
                }
                else
                {
                    if (currentScanline > 0xF2)
                        vCounter = (currentScanline - 0x38);
                }
            }
        }

        private void Render()
        {
            overscanBgColorArgb = GetColorAsArgb8888(1, overscanBgColor);

            ClearFramebuffer();

            if (!isDisplayBlanked)
            {
                if (isMode4)
                {
                    RenderBackgroundMode4();
                    RenderSpritesMode2();
                }
                else
                {
                    // TODO: Modes 0-3 rendering
                }

                // Mask column 0 with overscan color
                if (MasterSystem.IsBitSet(registers[0x00], 5))
                {
                    for (int i = 0; i < OutputFramebuffer.Length; i += (NumPixelsPerLine * 4))
                    {
                        for (int j = 0; j < (8 * 4); j += 4)
                            Buffer.BlockCopy(overscanBgColorArgb, 0, OutputFramebuffer, i + j, 4);
                    }
                }
            }
        }

        private void ClearFramebuffer()
        {
            for (int i = 0; i < screenDrawnPixels.Length; i++)
                screenDrawnPixels[i] = 0x00;

            for (int i = 0; i < OutputFramebuffer.Length; i += 4)
                Buffer.BlockCopy(overscanBgColorArgb, 0, OutputFramebuffer, i, 4);
        }

        private void RenderBackgroundMode4()
        {
            // TODO: scrolling is kinda wrong, especially vertically, but really both

            int numTilesPerLine = (NumPixelsPerLine / 8);

            for (int line = 0; line < screenHeight; line++)
            {
                for (int tile = 0; tile < numTilesPerLine; tile++)
                {
                    int scrolledLine = line;
                    if (!(MasterSystem.IsBitSet(registers[0x00], 7) && tile >= 24))
                    {
                        scrolledLine = (line + backgroundVScroll);
                        if (scrolledLine >= nametableHeight) scrolledLine -= nametableHeight;
                    }

                    int hScroll = (MasterSystem.IsBitSet(registers[0x00], 6) && line < 16 ? 0 : backgroundXScrollCache[line]);

                    ushort nametableAddress = (ushort)(nametableBaseAddress + ((scrolledLine / 8) * (numTilesPerLine * 2)));
                    ushort ntData = (ushort)((vram[nametableAddress + (tile * 2) + 1] << 8) | vram[nametableAddress + (tile * 2)]);

                    int tileIndex = (ntData & 0x01FF);
                    bool hFlip = ((ntData & 0x200) == 0x200);
                    bool vFlip = ((ntData & 0x400) == 0x400);
                    int palette = ((ntData & 0x800) >> 11);
                    bool priority = ((ntData & 0x1000) == 0x1000);

                    int tileLine = (vFlip ? ((scrolledLine / 8) * 8) + (-(scrolledLine % 8) + 7) : scrolledLine);

                    ushort tileAddress = (ushort)((tileIndex * 0x20) + ((tileLine % 8) * 4));
                    for (int pixel = 0; pixel < 8; pixel++)
                    {
                        int hShift = (hFlip ? pixel : (7 - pixel));

                        int c = (((vram[tileAddress + 0] >> hShift) & 0x1) << 0);
                        c |= (((vram[tileAddress + 1] >> hShift) & 0x1) << 1);
                        c |= (((vram[tileAddress + 2] >> hShift) & 0x1) << 2);
                        c |= (((vram[tileAddress + 3] >> hShift) & 0x1) << 3);

                        int outputY = ((line % screenHeight) * NumPixelsPerLine);
                        int outputX = ((hScroll + (tile * 8) + pixel) % NumPixelsPerLine);

                        if (screenDrawnPixels[outputY + outputX] == 0x00)
                        {
                            screenDrawnPixels[outputY + outputX] |= ((c != 0 && priority) ? PixelDrawn.BackgroundHighPriority : PixelDrawn.BackgroundLowPriority);
                            int outputAddress = outputFramebufferStartAddress + ((outputY + outputX) * 4);
                            Buffer.BlockCopy(GetColorAsArgb8888(palette, c), 0, OutputFramebuffer, outputAddress, 4);
                        }
                    }
                }
            }
        }

        private void RenderSpritesMode2()
        {
            int spriteSize = (isLargeSprites ? 16 : 8);

            for (int line = 0; line < screenHeight; line++)
            {
                for (int i = 0; i < spriteBuffer.Length; i++)
                    spriteBuffer[i] = 0;

                int numSprites = 0;
                for (int sprite = 0; sprite < 64; sprite++)
                {
                    int yCoordinate = vram[spriteAttribTableBaseAddress + sprite];

                    // Ignore following if Y coord is 208 in 192-line mode
                    if (yCoordinate == 208 && screenHeight == NumVisibleLinesLow) break;

                    // Y coord equals current scanline
                    if (yCoordinate + 1 == line)
                    {
                        if (numSprites < spriteBuffer.Length)
                            spriteBuffer[numSprites] = sprite;
                        numSprites++;
                    }
                }

                // Sprite overflow
                if (numSprites > spriteBuffer.Length)
                {
                    statusFlags |= 0x40;
                    numSprites = spriteBuffer.Length;
                }

                for (int i = 0; i < numSprites; i++)
                {
                    int sprite = spriteBuffer[i];
                    int xCoordinate = vram[spriteAttribTableBaseAddress + 0x80 + (sprite * 2)];
                    int tileIndex = vram[spriteAttribTableBaseAddress + 0x80 + (sprite * 2) + 1];

                    if (MasterSystem.IsBitSet(registers[0x00], 3))
                        xCoordinate -= 8;

                    if (MasterSystem.IsBitSet(registers[0x06], 2))
                        tileIndex |= 0x100;

                    for (int y = line; y < line + spriteSize; y++)
                    {
                        if (y >= screenHeight) continue;

                        ushort tileAddress = (ushort)((tileIndex * 0x20) + (((y - line) % spriteSize) * 4));

                        for (int pixel = 0; pixel < 8; pixel++)
                        {
                            int c = (((vram[tileAddress + 0] >> (7 - pixel)) & 0x1) << 0);
                            c |= (((vram[tileAddress + 1] >> (7 - pixel)) & 0x1) << 1);
                            c |= (((vram[tileAddress + 2] >> (7 - pixel)) & 0x1) << 2);
                            c |= (((vram[tileAddress + 3] >> (7 - pixel)) & 0x1) << 3);

                            if (c == 0 || xCoordinate + pixel >= NumPixelsPerLine) continue;

                            int outputY = ((y % screenHeight) * NumPixelsPerLine);
                            int outputX = ((xCoordinate + pixel) % NumPixelsPerLine);

                            if ((screenDrawnPixels[outputY + outputX] & PixelDrawn.Sprite) == PixelDrawn.Sprite)
                            {
                                // Sprite collision
                                statusFlags |= 0x20;
                            }
                            else if ((screenDrawnPixels[outputY + outputX] & PixelDrawn.BackgroundHighPriority) != PixelDrawn.BackgroundHighPriority)
                            {
                                // Draw if pixel isn't occupied by high-priority BG
                                screenDrawnPixels[outputY + outputX] |= PixelDrawn.Sprite;
                                int outputAddress = outputFramebufferStartAddress + ((outputY + outputX) * 4);
                                Buffer.BlockCopy(GetColorAsArgb8888(1, c), 0, OutputFramebuffer, outputAddress, 4);
                            }
                        }
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
            return (byte)vCounter;
        }

        public byte ReadHCounter()
        {
            return (byte)(hCounter >> 1);
        }

        public byte ReadControlPort()
        {
            byte statusCurrent = statusFlags;

            statusFlags = 0;

            isSecondControlWrite = false;
            InterruptPending = false;

            return statusCurrent;
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
