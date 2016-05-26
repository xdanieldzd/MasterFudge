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
        int currentScanline, hCounter, screenHeight;

        public VDP(RenderScreenHandler onRenderScreen)
        {
            renderScreen = onRenderScreen;

            registers = new byte[0x10];
            vram = new byte[0x4000];
            cram = new byte[0x20];
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

            if (MasterSystem.IsBitSet(statusFlags, 7) && MasterSystem.IsBitSet(registers[0x01], 5))
                InterruptPending = true;

            if ((hCounter + currentCycles) > cyclesPerLine)
            {
                currentScanline++;
                hCounter = 0;

                if (currentScanline == screenHeight)
                {
                    //
                }
                else if (currentScanline == numScanlines)
                {
                    currentScanline = 0;
                    Render();
                }
            }

            // TODO: probably wrong...?
            hCounter = ((hCounter + currentCycles) % cyclesPerLine);
        }

        private void Render()
        {
            // TODO: actually render shit instead of an empty screen

            byte[] frameBuffer = new byte[(int)NumPixelsPerLine * screenHeight * 4];

            for (int i = 0; i < frameBuffer.Length; i += 4)
                Buffer.BlockCopy(GetColorAsArgb8888(0, 8), 0, frameBuffer, i, 4);

            renderScreen?.Invoke(this, new RenderEventArgs((int)NumPixelsPerLine, screenHeight, frameBuffer));
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

            if (register == 0x01 && MasterSystem.IsBitSet(registers[register], 5))
            {
                // Frame interrupt
                InterruptPending = true;
            }
        }
    }
}
