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

        /* Control port stuff */
        bool isSecondControlWrite;
        ushort controlWord;
        byte readBuffer;

        byte codeRegister { get { return (byte)((controlWord >> 14) & 0x03); } }
        ushort addressRegister
        {
            get { return (ushort)(controlWord & 0x3FFF); }
            set { controlWord = (ushort)((codeRegister << 14) | (value & 0x3FFF)); }
        }

        /* Status flags */
        [Flags]
        enum StatusFlags : byte
        {
            None = 0,
            SpriteCollision = (1 << 5),
            SpriteOverflow = (1 << 6),
            FrameInterruptPending = (1 << 7)
        }
        StatusFlags statusFlags;
        bool isSpriteCollision
        {
            get { return ((statusFlags & StatusFlags.SpriteCollision) == StatusFlags.SpriteCollision); }
            set { statusFlags = ((statusFlags & ~StatusFlags.SpriteCollision) | (value ? StatusFlags.SpriteCollision : StatusFlags.None)); }
        }
        bool isSpriteOverflow
        {
            get { return ((statusFlags & StatusFlags.SpriteOverflow) == StatusFlags.SpriteOverflow); }
            set { statusFlags = ((statusFlags & ~StatusFlags.SpriteOverflow) | (value ? StatusFlags.SpriteOverflow : StatusFlags.None)); }
        }
        bool isFrameInterruptPending
        {
            get { return ((statusFlags & StatusFlags.FrameInterruptPending) == StatusFlags.FrameInterruptPending); }
            set { statusFlags = ((statusFlags & ~StatusFlags.FrameInterruptPending) | (value ? StatusFlags.FrameInterruptPending : StatusFlags.None)); }
        }

        public bool InterruptPending { get; private set; }

        int currentScanline, cyclesInScanline, vCounter, hCounter, screenHeight, nametableHeight, lineInterruptCounter, backgroundVScroll;

        /* Interrupt flags */
        bool isLineInterruptEnabled { get { return MasterSystem.IsBitSet(registers[0x00], 4); } }
        bool isFrameInterruptEnabled { get { return MasterSystem.IsBitSet(registers[0x01], 5); } }

        /* Masking etc. flags */
        bool isDisplayBlanked { get { return !MasterSystem.IsBitSet(registers[0x01], 6); } }
        bool isColumn0MaskEnabled { get { return MasterSystem.IsBitSet(registers[0x00], 5); } }
        bool isVScrollPartiallyDisabled { get { return MasterSystem.IsBitSet(registers[0x00], 7); } }   /* Columns 24-31, i.e. pixels 192-255 */
        bool isHScrollPartiallyDisabled { get { return MasterSystem.IsBitSet(registers[0x00], 6); } }   /* Rows 0-1, i.e. pixels 0-15 */

        /* Display modes */
        bool isMode4 { get { return MasterSystem.IsBitSet(registers[0x00], 2); } }      /* SMS VDP mode 4 */
        bool isMode3 { get { return MasterSystem.IsBitSet(registers[0x01], 3); } }      /* TMS9918 mode 3 OR 240-line mode */
        bool isMode2 { get { return MasterSystem.IsBitSet(registers[0x00], 1); } }      /* TMS9918 mode 2 */
        bool isMode1 { get { return MasterSystem.IsBitSet(registers[0x01], 4); } }      /* TMS9918 mode 1 OR 224-line mode */
        bool isMode0 { get { return !(isMode1 || isMode2 || isMode3 || isMode4); } }

        bool isSMS240LineMode { get { return (isMode4 && isMode2 && isMode3); } }
        bool isSMS224LineMode { get { return (isMode4 && isMode2 && isMode1); } }
        bool isSMS192LineMode { get { return (isMode4 && isMode2); } }

        /* Sprite flags */
        bool isLargeSprites { get { return MasterSystem.IsBitSet(registers[0x01], 1); } }
        bool isZoomedSprites { get { return MasterSystem.IsBitSet(registers[0x01], 0); } }
        bool isSpriteShiftLeft8 { get { return MasterSystem.IsBitSet(registers[0x00], 3); } }

        /* Addresses */
        ushort nametableBaseAddress { get { return (ushort)((registers[0x02] & 0x0E) << 10); } }
        ushort spriteAttribTableBaseAddress { get { return (ushort)((registers[0x05] & 0x7E) << 7); } }
        ushort spritePatternGenBaseAddress { get { return (ushort)((registers[0x06] & 0x04) << 11); } }

        /* Colors, scrolling */
        int overscanBgColor { get { return (registers[0x07] & 0x0F); } }
        int backgroundHScroll { get { return registers[0x08]; } }

        /* For checks during rendering (sprite collision, etc) */
        enum ScreenPixelUsage : byte
        {
            Empty = 0,
            HasBackgroundLowPriority = (1 << 0),
            HasBackgroundHighPriority = (1 << 1),
            HasSprite = (1 << 2)
        }
        ScreenPixelUsage[] screenPixelUsage;

        /* For rendering */
        byte[] overscanBgColorArgb;
        int outputFramebufferStartAddress { get { return (((NumVisibleLinesHigh - screenHeight) / 4) * NumPixelsPerLine) * 4; } }

        public byte[] OutputFramebuffer { get; private set; }

        public VDP()
        {
            SetTVSystem(true);

            registers = new byte[0x10];
            vram = new byte[0x4000];
            cram = new byte[0x20];

            screenPixelUsage = new ScreenPixelUsage[NumPixelsPerLine * NumVisibleLinesHigh];

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
            readBuffer = 0;

            statusFlags = StatusFlags.None;

            screenHeight = NumVisibleLinesLow;

            currentScanline = cyclesInScanline = vCounter = hCounter = 0;
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

            int currentHCounter = hCounter;
            bool nextLine = false;

            InterruptPending = (isFrameInterruptPending && isFrameInterruptEnabled);

            nextLine = ((currentHCounter + currentCycles) > GetVDPClockCyclesPerScanline(isNtsc));
            hCounter = ((hCounter + currentCycles) % (GetVDPClockCyclesPerScanline(isNtsc) + 1));

            if (nextLine)
            {
                /* Clear screen */
                if (currentScanline == 0)
                {
                    overscanBgColorArgb = GetColorAsArgb8888(1, overscanBgColor);
                    ClearFramebuffer();
                }

                /* Active screen area, render line */
                if (currentScanline < screenHeight)
                {
                    if (isMode4)
                    {
                        RenderBackgroundMode4(currentScanline);
                        RenderSpritesMode4(currentScanline);
                    }
                    else
                    {
                        // TODO: TMS9918 modes 0-3, used by "non-SMS" games (SG-1000, SC-3000) and F-16 Fighting Falcon; should I even bother?
                    }
                }

                /* Adjust counters */
                vCounter = AdjustVCounter(currentScanline);
                currentScanline++;
                hCounter = 0;
                cyclesInScanline = 0;

                if (currentScanline > (screenHeight + 1))
                {
                    lineInterruptCounter = registers[0x0A];
                    backgroundVScroll = registers[0x09];

                    if ((isSMS224LineMode && isSMS240LineMode) || isSMS192LineMode)
                    {
                        screenHeight = NumVisibleLinesLow;
                        nametableHeight = 224;
                    }
                    else if (isSMS240LineMode)
                    {
                        screenHeight = NumVisibleLinesHigh;
                        nametableHeight = 256;
                    }
                    else if (isSMS224LineMode)
                    {
                        screenHeight = NumVisibleLinesMed;
                        nametableHeight = 256;
                    }
                    else
                    {
                        // TODO: TMS9918 modes 0-3? Values below probably aren't correct
                        screenHeight = NumVisibleLinesLow;
                        nametableHeight = NumVisibleLinesLow;
                    }
                }
                else
                {
                    lineInterruptCounter--;
                    if (lineInterruptCounter < 0)
                    {
                        lineInterruptCounter = registers[0x0A];
                        InterruptPending = (isLineInterruptEnabled);
                    }
                }

                if (vCounter == (screenHeight + 1))
                {
                    isFrameInterruptPending = true;
                }
                else if (currentScanline == numScanlines)
                {
                    /* Mask column 0 with overscan color */
                    if (isColumn0MaskEnabled)
                    {
                        for (int i = 0; i < OutputFramebuffer.Length; i += (NumPixelsPerLine * 4))
                        {
                            for (int j = 0; j < (8 * 4); j += 4)
                                Buffer.BlockCopy(overscanBgColorArgb, 0, OutputFramebuffer, i + j, 4);
                        }
                    }

                    currentScanline = 0;
                    return true;
                }
            }

            return false;
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

        private void ClearFramebuffer()
        {
            for (int i = 0; i < screenPixelUsage.Length; i++)
                screenPixelUsage[i] = ScreenPixelUsage.Empty;

            for (int i = 0; i < OutputFramebuffer.Length; i += 4)
                Buffer.BlockCopy(overscanBgColorArgb, 0, OutputFramebuffer, i, 4);
        }

        private void RenderBackgroundMode4(int line)
        {
            if (!isDisplayBlanked)
            {
                int numTilesPerLine = (NumPixelsPerLine / 8);

                for (int tile = 0; tile < numTilesPerLine; tile++)
                {
                    /* Vertical scrolling */
                    int scrolledLine = line;
                    if (!(isVScrollPartiallyDisabled && tile >= 24))
                    {
                        scrolledLine = (line + backgroundVScroll);
                        if (scrolledLine >= nametableHeight) scrolledLine -= nametableHeight;
                    }

                    /* Horizontal scrolling */
                    int hScroll = (isHScrollPartiallyDisabled && line < 16 ? 0 : backgroundHScroll);

                    /* Get tile data */
                    ushort nametableAddress = (ushort)(nametableBaseAddress + ((scrolledLine / 8) * (numTilesPerLine * 2)));
                    ushort ntData = (ushort)((vram[nametableAddress + (tile * 2) + 1] << 8) | vram[nametableAddress + (tile * 2)]);

                    int tileIndex = (ntData & 0x01FF);
                    bool hFlip = ((ntData & 0x200) == 0x200);
                    bool vFlip = ((ntData & 0x400) == 0x400);
                    int palette = ((ntData & 0x800) >> 11);
                    bool priority = ((ntData & 0x1000) == 0x1000);

                    /* For vertical flip */
                    int tileLine = (vFlip ? ((scrolledLine / 8) * 8) + (-(scrolledLine % 8) + 7) : scrolledLine);

                    ushort tileAddress = (ushort)((tileIndex * 0x20) + ((tileLine % 8) * 4));
                    for (int pixel = 0; pixel < 8; pixel++)
                    {
                        /* For horizontal flip */
                        int hShift = (hFlip ? pixel : (7 - pixel));

                        int c = (((vram[tileAddress + 0] >> hShift) & 0x1) << 0);
                        c |= (((vram[tileAddress + 1] >> hShift) & 0x1) << 1);
                        c |= (((vram[tileAddress + 2] >> hShift) & 0x1) << 2);
                        c |= (((vram[tileAddress + 3] >> hShift) & 0x1) << 3);

                        int outputY = ((line % screenHeight) * NumPixelsPerLine);
                        int outputX = ((hScroll + (tile * 8) + pixel) % NumPixelsPerLine);

                        if (screenPixelUsage[outputY + outputX] == ScreenPixelUsage.Empty)
                        {
                            screenPixelUsage[outputY + outputX] |= ((c != 0 && priority) ? ScreenPixelUsage.HasBackgroundHighPriority : ScreenPixelUsage.HasBackgroundLowPriority);
                            int outputAddress = outputFramebufferStartAddress + ((outputY + outputX) * 4);
                            Buffer.BlockCopy(GetColorAsArgb8888(palette, c), 0, OutputFramebuffer, outputAddress, 4);
                        }
                    }
                }
            }
        }

        private void RenderSpritesMode4(int line)
        {
            int spriteSize = (isLargeSprites ? 16 : 8);

            int numSprites = 0;
            for (int sprite = 0; sprite < 64; sprite++)
            {
                int yCoordinate = vram[spriteAttribTableBaseAddress + sprite];

                /* Ignore following if Y coord is 208 in 192-line mode */
                if (yCoordinate == 208 && screenHeight == NumVisibleLinesLow) break;

                /* Modify Y coord as needed */
                yCoordinate++;
                if (yCoordinate > screenHeight)
                    yCoordinate -= 256;

                /* Ignore this sprite if on incorrect lines */
                if (line < yCoordinate || line >= (yCoordinate + spriteSize)) continue;

                /* Check for sprite overflow */
                numSprites++;
                if (numSprites > 8)
                {
                    isSpriteOverflow = true;
                    break;
                }

                /* If display isn't blanked, draw line */
                if (!isDisplayBlanked)
                {
                    int xCoordinate = vram[spriteAttribTableBaseAddress + 0x80 + (sprite * 2)];
                    int tileIndex = vram[spriteAttribTableBaseAddress + 0x80 + (sprite * 2) + 1];

                    /* Adjust according to registers */
                    if (isSpriteShiftLeft8) xCoordinate -= 8;
                    if (isLargeSprites) tileIndex &= ~0x01;

                    ushort tileAddress = (ushort)(spritePatternGenBaseAddress + (tileIndex * 0x20) + (((line - yCoordinate) % spriteSize) * 4));

                    /* Draw sprite line */
                    for (int pixel = 0; pixel < 8; pixel++)
                    {
                        int c = (((vram[tileAddress + 0] >> (7 - pixel)) & 0x1) << 0);
                        c |= (((vram[tileAddress + 1] >> (7 - pixel)) & 0x1) << 1);
                        c |= (((vram[tileAddress + 2] >> (7 - pixel)) & 0x1) << 2);
                        c |= (((vram[tileAddress + 3] >> (7 - pixel)) & 0x1) << 3);

                        if (c == 0 || xCoordinate + pixel >= NumPixelsPerLine) continue;

                        int outputY = ((line % screenHeight) * NumPixelsPerLine);
                        int outputX = ((xCoordinate + pixel) % NumPixelsPerLine);

                        if ((screenPixelUsage[outputY + outputX] & ScreenPixelUsage.HasSprite) == ScreenPixelUsage.HasSprite)
                        {
                            /* Set sprite collision flag */
                            isSpriteCollision = true;
                        }
                        else if ((screenPixelUsage[outputY + outputX] & ScreenPixelUsage.HasBackgroundHighPriority) != ScreenPixelUsage.HasBackgroundHighPriority)
                        {
                            /* Draw if pixel isn't occupied by high-priority BG */
                            int outputAddress = outputFramebufferStartAddress + ((outputY + outputX) * 4);
                            Buffer.BlockCopy(GetColorAsArgb8888(1, c), 0, OutputFramebuffer, outputAddress, 4);
                        }

                        /* Note that there is a sprite here regardless */
                        screenPixelUsage[outputY + outputX] |= ScreenPixelUsage.HasSprite;
                    }
                }
            }
        }

        private int AdjustVCounter(int scanline)
        {
            int counter = scanline;

            // TODO: odd thing, verify this is correct http://www.smspower.org/Development/ScanlineCounter
            if (isNtsc)
            {
                if (screenHeight == NumVisibleLinesHigh)
                {
                    /* Invalid on NTSC */
                    if (scanline > 0xFF)
                        counter = (scanline - 0x100);
                }
                else if (screenHeight == NumVisibleLinesMed)
                {
                    if (scanline > 0xEA)
                        counter = (scanline - 0x06);
                }
                else
                {
                    if (scanline > 0xDA)
                        counter = (scanline - 0x06);
                }
            }
            else
            {
                if (screenHeight == NumVisibleLinesHigh)
                {
                    if (scanline > 0xFF && scanline < 0xFF + 0x0A)
                        counter = (scanline - 0x100);
                    else
                        counter = (scanline - 0x38);
                }
                else if (screenHeight == NumVisibleLinesMed)
                {
                    if (scanline > 0xFF && scanline < 0xFF + 0x02)
                        counter = (scanline - 0x100);
                    else
                        counter = (scanline - 0x38);
                }
                else
                {
                    if (scanline > 0xF2)
                        counter = (scanline - 0x39);
                }
            }

            return counter;
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
            byte statusCurrent = (byte)statusFlags;

            statusFlags = StatusFlags.None;

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
            readBuffer = vram[addressRegister];
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

        public byte[] DumpVideoRam()
        {
            return vram;
        }

        public byte[] DumpColorRam()
        {
            return cram;
        }
    }
}
