using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MasterFudge.Emulation.CPU;

namespace MasterFudge.Emulation.Graphics
{
    public class VDP
    {
        /* TMS9918 video modes 0-3:
         * http://www.smspower.org/uploads/Development/tms9918a.txt 
         * http://problemkaputt.de/portar.htm#videodisplayprocessor
         */

        public const int NumScanlinesPAL = 313;
        public const int NumScanlinesNTSC = 262;

        public const int NumVisibleLinesLow = 192;
        public const int NumVisibleLinesMed = 224;
        public const int NumVisibleLinesHigh = 240;

        public const int NumPixelsPerLine = 256;

        public const double ClockDivider = 5.0;

        BaseUnitType baseUnitType;
        BaseUnitRegion baseUnitRegion;

        byte[] registers, vram, cramMS, cramGG;
        ushort cramLatchGG;

        bool isNtsc { get { return (baseUnitRegion == BaseUnitRegion.JapanNTSC || baseUnitRegion == BaseUnitRegion.ExportNTSC); } }
        int numScanlines { get { return (isNtsc ? NumScanlinesNTSC : NumScanlinesPAL); } }

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

        int currentScanline, cyclesInLine, vCounter, hCounter, screenHeight, nametableHeight, lineInterruptCounter, backgroundVScroll;

        /* Interrupt flags */
        public bool IrqLineAsserted { get; private set; }
        bool isLineInterruptEnabled { get { return PowerBase.IsBitSet(registers[0x00], 4); } }
        bool isFrameInterruptEnabled { get { return PowerBase.IsBitSet(registers[0x01], 5); } }
        bool isLineInterruptPending;

        /* Masking etc. flags */
        bool isDisplayBlanked { get { return !PowerBase.IsBitSet(registers[0x01], 6); } }
        bool isColumn0MaskEnabled { get { return PowerBase.IsBitSet(registers[0x00], 5); } }
        bool isVScrollPartiallyDisabled { get { return PowerBase.IsBitSet(registers[0x00], 7); } }   /* Columns 24-31, i.e. pixels 192-255 */
        bool isHScrollPartiallyDisabled { get { return PowerBase.IsBitSet(registers[0x00], 6); } }   /* Rows 0-1, i.e. pixels 0-15 */

        /* Display modes */
        bool isMode4 { get { return PowerBase.IsBitSet(registers[0x00], 2); } }      /* SMS VDP mode 4 */
        bool isMode3 { get { return PowerBase.IsBitSet(registers[0x01], 3); } }      /* TMS9918 mode 3 OR 240-line mode */
        bool isMode2 { get { return PowerBase.IsBitSet(registers[0x00], 1); } }      /* TMS9918 mode 2 */
        bool isMode1 { get { return PowerBase.IsBitSet(registers[0x01], 4); } }      /* TMS9918 mode 1 OR 224-line mode */
        bool isMode0 { get { return !(isMode1 || isMode2 || isMode3 || isMode4); } }

        bool isSMS240LineMode { get { return (isMode4 && isMode2 && isMode3); } }
        bool isSMS224LineMode { get { return (isMode4 && isMode2 && isMode1); } }
        bool isSMS192LineMode { get { return (isMode4 && isMode2); } }

        /* Sprite flags */
        bool isLargeSprites { get { return PowerBase.IsBitSet(registers[0x01], 1); } }
        bool isZoomedSprites { get { return PowerBase.IsBitSet(registers[0x01], 0); } }
        bool isSpriteShiftLeft8 { get { return PowerBase.IsBitSet(registers[0x00], 3); } }

        /* Addresses */
        ushort nametableBaseAddress
        {
            get
            {
                if (isMode4) return (ushort)((registers[0x02] & 0x0E) << 10);
                else return (ushort)((registers[0x02] & 0x0F) << 10);
            }
        }
        ushort spriteAttribTableBaseAddress { get { return (ushort)((registers[0x05] & 0x7E) << 7); } }
        ushort spritePatternGenBaseAddress
        {
            get
            {
                if (isMode4) return (ushort)((registers[0x06] & 0x04) << 11);
                else return (ushort)((registers[0x06] & 0x07) << 11);
            }
        }

        /* Colors, scrolling */
        int overscanBgColor { get { return (registers[0x07] & 0x0F); } }
        int backgroundHScroll { get { return registers[0x08]; } }

        /* Non-mode 4 colors */
        static byte[][] legacyColorData = new byte[][]
        {
            /*             B    G    R    A */
            new byte[] {   0,   0,   0, 255 },  /* Transparent */
            new byte[] {   0,   0,   0, 255 },  /* Black */
            new byte[] {  66, 200,  33, 255 },  /* Medium green */
            new byte[] { 120, 220,  94, 255 },  /* Light green */
            new byte[] { 237,  85,  84, 255 },  /* Dark blue */
            new byte[] { 252, 118, 125, 255 },  /* Light blue */
            new byte[] {  77,  82, 212, 255 },  /* Dark red */
            new byte[] { 245, 235,  66, 255 },  /* Cyan */
            new byte[] {  84,  85, 252, 255 },  /* Medium red */
            new byte[] { 120, 121, 255, 255 },  /* Light red */
            new byte[] {  84, 193, 212, 255 },  /* Dark yellow */
            new byte[] { 128, 206, 230, 255 },  /* Light yellow */
            new byte[] {  59, 176,  33, 255 },  /* Dark green */
            new byte[] { 186,  91, 201, 255 },  /* Magenta */
            new byte[] { 204, 204, 204, 255 },  /* Gray */
            new byte[] { 255, 255, 255, 255 },  /* White */
        };

        /* For H-counter emulation */
        static byte[] hCounterTable = new byte[]
        {
            0x00, 0x01, 0x02, 0x02, 0x03, 0x04, 0x05, 0x05, 0x06, 0x07, 0x08, 0x08, 0x09, 0x0A, 0x0B, 0x0B,
            0x0C, 0x0D, 0x0E, 0x0E, 0x0F, 0x10, 0x11, 0x11, 0x12, 0x13, 0x14, 0x14, 0x15, 0x16, 0x17, 0x17,
            0x18, 0x19, 0x1A, 0x1A, 0x1B, 0x1C, 0x1D, 0x1D, 0x1E, 0x1F, 0x20, 0x20, 0x21, 0x22, 0x23, 0x23,
            0x24, 0x25, 0x26, 0x26, 0x27, 0x28, 0x29, 0x29, 0x2A, 0x2B, 0x2C, 0x2C, 0x2D, 0x2E, 0x2F, 0x2F,
            0x30, 0x31, 0x32, 0x32, 0x33, 0x34, 0x35, 0x35, 0x36, 0x37, 0x38, 0x38, 0x39, 0x3A, 0x3B, 0x3B,
            0x3C, 0x3D, 0x3E, 0x3E, 0x3F, 0x40, 0x41, 0x41, 0x42, 0x43, 0x44, 0x44, 0x45, 0x46, 0x47, 0x47,
            0x48, 0x49, 0x4A, 0x4A, 0x4B, 0x4C, 0x4D, 0x4D, 0x4E, 0x4F, 0x50, 0x50, 0x51, 0x52, 0x53, 0x53,
            0x54, 0x55, 0x56, 0x56, 0x57, 0x58, 0x59, 0x59, 0x5A, 0x5B, 0x5C, 0x5C, 0x5D, 0x5E, 0x5F, 0x5F,
            0x60, 0x61, 0x62, 0x62, 0x63, 0x64, 0x65, 0x65, 0x66, 0x67, 0x68, 0x68, 0x69, 0x6A, 0x6B, 0x6B,
            0x6C, 0x6D, 0x6E, 0x6E, 0x6F, 0x70, 0x71, 0x71, 0x72, 0x73, 0x74, 0x74, 0x75, 0x76, 0x77, 0x77,
            0x78, 0x79, 0x7A, 0x7A, 0x7B, 0x7C, 0x7D, 0x7D, 0x7E, 0x7F, 0x80, 0x80, 0x81, 0x82, 0x83, 0x83,
            0x84, 0x85, 0x86, 0x86, 0x87, 0x88, 0x89, 0x89, 0x8A, 0x8B, 0x8C, 0x8C, 0x8D, 0x8E, 0x8F, 0x8F,
            0x90, 0x91, 0x92, 0x92, 0x93,

            0xE9, 0xEA, 0xEA, 0xEB, 0xEC, 0xED, 0xED, 0xEE, 0xEF, 0xF0, 0xF0, 0xF1, 0xF2, 0xF3, 0xF3, 0xF4,
            0xF5, 0xF6, 0xF6, 0xF7, 0xF8, 0xF9, 0xF9, 0xFA, 0xFB, 0xFC, 0xFC, 0xFD, 0xFE, 0xFF, 0xFF,
        };

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
            SetTvSystem(PowerBase.DefaultBaseUnitRegion);

            registers = new byte[0x10];
            vram = new byte[0x4000];
            cramMS = new byte[0x20];
            cramGG = new byte[0x40];

            screenPixelUsage = new ScreenPixelUsage[NumPixelsPerLine * NumVisibleLinesHigh];

            OutputFramebuffer = new byte[(NumPixelsPerLine * NumVisibleLinesHigh) * 4];

            Reset();
        }

        public static int GetVDPClockCyclesPerFrame(bool isNtsc)
        {
            return (int)(PowerBase.GetMasterClockCyclesPerFrame(isNtsc) / ClockDivider);
        }

        public static int GetVDPClockCyclesPerScanline(bool isNtsc)
        {
            return (int)(PowerBase.GetMasterClockCyclesPerScanline(isNtsc) / ClockDivider);
        }

        public void Reset()
        {
            WriteRegister(0x00, 0x36);
            WriteRegister(0x01, 0x80);
            WriteRegister(0x02, 0xFF);
            WriteRegister(0x03, 0xFF);
            WriteRegister(0x04, 0xFF);
            WriteRegister(0x05, 0xFF);
            WriteRegister(0x06, 0xFF);

            for (int i = 0; i < vram.Length; i++) vram[i] = 0;
            for (int i = 0; i < cramMS.Length; i++) cramMS[i] = 0;
            for (int i = 0; i < cramGG.Length; i++) cramGG[i] = 0;

            cramLatchGG = 0x0000;

            isSecondControlWrite = false;
            controlWord = 0x0000;
            readBuffer = 0;

            statusFlags = StatusFlags.None;

            screenHeight = NumVisibleLinesLow;

            currentScanline = cyclesInLine = vCounter = hCounter = 0;
            nametableHeight = 224;
            lineInterruptCounter = 255;
            backgroundVScroll = 0;
        }

        public void SetUnitType(BaseUnitType unitType)
        {
            baseUnitType = unitType;
        }

        public void SetTvSystem(BaseUnitRegion unitRegion)
        {
            baseUnitRegion = unitRegion;
        }

        public bool Execute(int currentCycles)
        {
            // TODO: all the timing!

            bool drawLine = ((cyclesInLine + currentCycles) >= Z80.GetCPUClockCyclesPerScanline(isNtsc));

            cyclesInLine = ((cyclesInLine + currentCycles) % Z80.GetCPUClockCyclesPerScanline(isNtsc));

            hCounter = hCounterTable[cyclesInLine % Z80.GetCPUClockCyclesPerScanline(isNtsc)];

            IrqLineAsserted = ((isFrameInterruptEnabled && isFrameInterruptPending) || (isLineInterruptEnabled && isLineInterruptPending));

            if (drawLine)
            {
                /* Clear screen */
                if (currentScanline == 0)
                {
                    overscanBgColorArgb = (baseUnitType == BaseUnitType.GameGear ? ConvertGameGearColor(1, overscanBgColor) : ConvertMasterSystemColor(1, overscanBgColor));
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
                    else if (isMode2)
                    {
                        RenderBackgroundMode2(currentScanline);
                        RenderSpritesMode2(currentScanline);
                    }
                    else
                    {
                        // TODO: TMS9918 modes 0, 1 and 3, used by "non-SMS" games (SG-1000, SC-3000); should I even bother?
                    }
                }

                /* Adjust counters */
                vCounter = AdjustVCounter(currentScanline);
                currentScanline++;

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
                        if (isMode2)
                        {
                            screenHeight = NumVisibleLinesLow;
                            nametableHeight = 192;
                        }
                        else
                        {
                            // TODO: verify remaining TMS9918 modes, these are probably not correct
                            screenHeight = NumVisibleLinesLow;
                            nametableHeight = NumVisibleLinesLow;
                        }
                    }
                }

                if (vCounter < (screenHeight + 1))
                {
                    lineInterruptCounter--;
                    if (lineInterruptCounter < 0)
                    {
                        lineInterruptCounter = registers[0x0A];
                        isLineInterruptPending = true;
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

                    /* Apply Game Gear mask, if applicable */
                    if (baseUnitType == BaseUnitType.GameGear)
                    {
                        // TODO: make this not shitty
                        byte[] maskColor = new byte[] { 0x00, 0x00, 0x00, 0xFF };

                        for (int i = 0; i < outputFramebufferStartAddress; i += 4)
                            Buffer.BlockCopy(maskColor, 0, OutputFramebuffer, i, 4);

                        for (int y = 0; y < screenHeight; y++)
                        {
                            for (int x = 0; x < NumPixelsPerLine; x++)
                            {
                                if ((y >= 24 && y < 144 + 24) && (x >= 48 && x < 160 + 48)) continue;

                                int outputY = (y * NumPixelsPerLine);
                                int outputX = (x % NumPixelsPerLine);

                                Buffer.BlockCopy(maskColor, 0, OutputFramebuffer, outputFramebufferStartAddress + ((outputY + outputX) * 4), 4);
                            }
                        }

                        for (int i = outputFramebufferStartAddress + ((screenHeight * NumPixelsPerLine) * 4); i < OutputFramebuffer.Length; i += 4)
                            Buffer.BlockCopy(maskColor, 0, OutputFramebuffer, i, 4);
                    }

                    currentScanline = 0;
                    return true;
                }
            }

            return false;
        }

        public byte[] ConvertMasterSystemColor(int palette, int color)
        {
            int offset = ((palette * 16) + color);

            byte r = (byte)(cramMS[offset] & 0x3);
            r |= (byte)((r << 6) | (r << 4) | (r << 2));
            byte g = (byte)((cramMS[offset] >> 2) & 0x3);
            g |= (byte)((g << 6) | (g << 4) | (g << 2));
            byte b = (byte)((cramMS[offset] >> 4) & 0x3);
            b |= (byte)((b << 6) | (b << 4) | (b << 2));

            return new byte[] { b, g, r, 0xFF };
        }

        public byte[] ConvertGameGearColor(int palette, int color)
        {
            int offset = ((palette * 32) + (color * 2));

            byte r = (byte)((cramGG[offset]) & 0x0F);
            r |= (byte)(r << 4);
            byte g = (byte)((cramGG[offset] >> 4) & 0x0F);
            g |= (byte)(g << 4);
            byte b = (byte)((cramGG[offset + 1]) & 0x0F);
            b |= (byte)(b << 4);

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

                            byte[] colorData = (baseUnitType == BaseUnitType.GameGear ? ConvertGameGearColor(palette, c) : ConvertMasterSystemColor(palette, c));
                            Buffer.BlockCopy(colorData, 0, OutputFramebuffer, outputAddress, 4);
                        }
                    }
                }
            }
        }

        private void RenderSpritesMode4(int line)
        {
            /* Determine sprite size */
            int spriteWidth = 8;
            int spriteHeight = (isLargeSprites ? 16 : 8);

            /* Check and adjust for zoomed sprites */
            if (isZoomedSprites)
            {
                spriteWidth *= 2;
                spriteHeight *= 2;
            }

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
                if (line < yCoordinate || line >= (yCoordinate + spriteHeight)) continue;

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
                    int zoomShift = (isZoomedSprites ? 1 : 0);

                    /* Adjust according to registers */
                    if (isSpriteShiftLeft8) xCoordinate -= 8;
                    if (isLargeSprites) tileIndex &= ~0x01;

                    ushort tileAddress = (ushort)(spritePatternGenBaseAddress + (tileIndex * 0x20) + ((((line - yCoordinate) >> zoomShift) % spriteHeight) * 4));

                    /* Draw sprite line */
                    for (int pixel = 0; pixel < spriteWidth; pixel++)
                    {
                        int c = (((vram[tileAddress + 0] >> (7 - (pixel >> zoomShift))) & 0x1) << 0);
                        c |= (((vram[tileAddress + 1] >> (7 - (pixel >> zoomShift))) & 0x1) << 1);
                        c |= (((vram[tileAddress + 2] >> (7 - (pixel >> zoomShift))) & 0x1) << 2);
                        c |= (((vram[tileAddress + 3] >> (7 - (pixel >> zoomShift))) & 0x1) << 3);

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

                            byte[] colorData = (baseUnitType == BaseUnitType.GameGear ? ConvertGameGearColor(1, c) : ConvertMasterSystemColor(1, c));
                            Buffer.BlockCopy(colorData, 0, OutputFramebuffer, outputAddress, 4);
                        }

                        /* Note that there is a sprite here regardless */
                        screenPixelUsage[outputY + outputX] |= ScreenPixelUsage.HasSprite;
                    }
                }
            }
        }

        private void RenderBackgroundMode2(int line)
        {
            if (!isDisplayBlanked)
            {
                int numTilesPerLine = (NumPixelsPerLine / 8);

                /* Calculate some base addresses */
                ushort patternGeneratorBaseAddress = (ushort)((registers[0x04] & 0x04) << 11);
                ushort colorTableBaseAddress = (ushort)((registers[0x03] & 0x80) << 6);

                for (int tile = 0; tile < numTilesPerLine; tile++)
                {
                    /* Calculate nametable address, fetch data byte */
                    ushort nametableAddress = (ushort)(nametableBaseAddress + ((line / 8) * numTilesPerLine) + tile);

                    byte patternNametableData = vram[nametableAddress];

                    /* Calculate character number and masks */
                    ushort characterNumber = (ushort)(((line / 64) << 8) | vram[nametableAddress]);
                    ushort characterNumberDataMask = (ushort)(((registers[0x04] & 0x03) << 8) | 0xFF);
                    ushort characterNumberColorMask = (ushort)(((registers[0x03] & 0x7F) << 3) | 0x07);

                    /* Fetch pixel and color data for current pixel line (1 byte, 8 pixels) */
                    byte pixelLineData = vram[patternGeneratorBaseAddress + ((characterNumber & characterNumberDataMask) * 8) + (line % 8)];
                    byte pixelLineColor = vram[colorTableBaseAddress + ((characterNumber & characterNumberColorMask) * 8) + (line % 8)];

                    /* Extract background and foreground color indices */
                    byte[] colorIndicesBackgroundForeground = new byte[2];
                    colorIndicesBackgroundForeground[0] = (byte)(pixelLineColor & 0x0F);
                    colorIndicesBackgroundForeground[1] = (byte)(pixelLineColor >> 4);

                    /* Draw pixels */
                    for (int pixel = 0; pixel < 8; pixel++)
                    {
                        /* Fetch color index for current pixel (bit clear means background, bit set means foreground color) */
                        byte c = colorIndicesBackgroundForeground[((pixelLineData >> (7 - pixel)) & 0x01)];
                        /* Color index 0 is transparent */
                        if (c == 0) continue;

                        /* Calculate output framebuffer location, get BGRA values from legacy color table, write to framebuffer */
                        int outputY = ((line % screenHeight) * NumPixelsPerLine);
                        int outputX = (((tile * 8) + pixel) % NumPixelsPerLine);

                        int outputAddress = outputFramebufferStartAddress + ((outputY + outputX) * 4);
                        Buffer.BlockCopy(legacyColorData[c & 0x0F], 0, OutputFramebuffer, outputAddress, 4);
                    }
                }
            }
        }

        // TODO: illegal sprites and stuff
        private void RenderSpritesMode2(int line)
        {
            /* Clear last illegal sprite number (if any) and overflow flag from status register */
            statusFlags &= (StatusFlags.FrameInterruptPending | StatusFlags.SpriteCollision);

            /* Determine sprite size */
            int spriteSize = (isLargeSprites ? 16 : 8);

            /* Check and adjust for zoomed sprites */
            if (isZoomedSprites) spriteSize *= 2;
            int zoomShift = (isZoomedSprites ? 1 : 0);

            int numSprites = 0;
            for (int sprite = 0; sprite < 32; sprite++)
            {
                int yCoordinate = vram[spriteAttribTableBaseAddress + (sprite * 4)];

                /* Ignore following if Y coord is 208 */
                if (yCoordinate == 208)
                {
                    /* Store first "illegal sprite" number in status register */
                    statusFlags |= (StatusFlags)sprite;
                    return;
                }

                /* Modify Y coord as needed */
                yCoordinate++;
                if (yCoordinate > screenHeight)
                    yCoordinate -= 256;

                /* Ignore this sprite if on incorrect lines */
                if (line < yCoordinate || line >= (yCoordinate + spriteSize)) continue;

                /* Check for sprite overflow */
                numSprites++;
                if (numSprites > 4)
                {
                    isSpriteOverflow = true;
                    /* Store sprite number in status register */
                    statusFlags |= (StatusFlags)sprite;
                    return;
                }

                /* If display isn't blanked, draw line */
                if (!isDisplayBlanked)
                {
                    int xCoordinate = vram[spriteAttribTableBaseAddress + (sprite * 4) + 1];
                    int characterNumber = vram[spriteAttribTableBaseAddress + (sprite * 4) + 2];
                    int attributes = vram[spriteAttribTableBaseAddress + (sprite * 4) + 3];

                    /* Adjust according to registers/attributes, get sprite color */
                    if ((attributes & 0x80) == 0x80) xCoordinate -= 32;
                    if (isLargeSprites) characterNumber &= 0xFC;
                    int spriteColor = (attributes & 0x0F);

                    ushort spritePatternAddress = (ushort)(spritePatternGenBaseAddress + (characterNumber * 8) + (((line - yCoordinate) >> zoomShift) % spriteSize));
                    if (isLargeSprites)
                    {
                        RenderSpriteTileMode2(spritePatternAddress, xCoordinate, (line - (yCoordinate % 8)), spriteColor, zoomShift);
                        RenderSpriteTileMode2((ushort)(spritePatternAddress + 16), (xCoordinate + 8), (line - (yCoordinate % 8)), spriteColor, zoomShift);
                    }
                    else
                    {
                        RenderSpriteTileMode2(spritePatternAddress, xCoordinate, (line - (yCoordinate % 8)), spriteColor, zoomShift);
                    }
                }
            }

            /* Because we didn't bow out before already, store total number of sprites in status register */
            statusFlags |= (StatusFlags)31;
        }

        private void RenderSpriteTileMode2(ushort spritePatternAddress, int xCoordinate, int yCoordinate, int spriteColor, int zoomShift)
        {
            /* Fetch pixel and color data */
            byte pixelLineData = vram[spritePatternAddress];
            byte[] outputColorData = legacyColorData[spriteColor];

            /* Draw pixels */
            for (int pixel = 0; pixel < 8; pixel++)
            {
                /* Check if a pixel needs to be drawn, and if we've crossed the right screen edge */
                if (spriteColor == 0 || ((pixelLineData >> (7 - (pixel >> zoomShift))) & 0x01) == 0x00 || xCoordinate + pixel >= NumPixelsPerLine) continue;

                /* Calculate output framebuffer coordinates */
                int outputY = ((yCoordinate % screenHeight) * NumPixelsPerLine);
                int outputX = ((xCoordinate + pixel) % NumPixelsPerLine);

                if ((screenPixelUsage[outputY + outputX] & ScreenPixelUsage.HasSprite) == ScreenPixelUsage.HasSprite)
                {
                    /* Set sprite collision flag */
                    isSpriteCollision = true;
                }
                else
                {
                    /* Get BGRA values from legacy color table, write to framebuffer */
                    int outputAddress = outputFramebufferStartAddress + ((outputY + outputX) * 4);
                    Buffer.BlockCopy(outputColorData, 0, OutputFramebuffer, outputAddress, 4);
                }

                /* Note that there is a sprite here regardless */
                screenPixelUsage[outputY + outputX] |= ScreenPixelUsage.HasSprite;
            }
        }

        private int AdjustVCounter(int scanline)
        {
            int counter = scanline;

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
            return (byte)hCounter;
        }

        public byte ReadControlPort()
        {
            byte statusCurrent = (byte)statusFlags;

            statusFlags = StatusFlags.None;

            isSecondControlWrite = false;
            isLineInterruptPending = false;

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
                    if (baseUnitType == BaseUnitType.GameGear)
                    {
                        if ((addressRegister & 0x0001) != 0)
                        {
                            cramLatchGG = (ushort)((cramLatchGG & 0x00FF) | (value << 8));
                            cramGG[(addressRegister & 0x003E) + 0] = (byte)(cramLatchGG & 0xFF);
                            cramGG[(addressRegister & 0x003E) + 1] = (byte)((cramLatchGG >> 8) & 0xFF);
                        }
                        else
                            cramLatchGG = (ushort)((cramLatchGG & 0xFF00) | value);
                    }
                    else
                        cramMS[(addressRegister & 0x001F)] = value;
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
            if (baseUnitType == BaseUnitType.GameGear)
                return cramGG;
            else
                return cramMS;
        }
    }
}
