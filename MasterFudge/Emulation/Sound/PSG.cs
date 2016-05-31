using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Sound
{
    public class PSG
    {
        ushort[] volumeRegisters;       /* Channels 0-3: 4 bits */
        ushort[] toneRegisters;         /* Channels 0-2 (tone): 10 bits; channel 3 (noise): 3 bits */

        byte latchedChannel, latchedType;

        public WaveProvider WaveProvider { get; private set; }

        public PSG()
        {
            volumeRegisters = new ushort[4];
            toneRegisters = new ushort[4];

            latchedChannel = 0x00;

            // TODO: make this sample thingy do PSG!
            WaveProvider = new WaveProvider();
            WaveProvider.SetWaveFormat(16000, 1);
            WaveProvider.Frequency = 1000;
            WaveProvider.Amplitude = 0.25f;
        }

        public void Execute(int currentCycles)
        {
            // TODO: everything!
            WaveProvider.Frequency = (toneRegisters[0] ^ 0x3FF) & 0x3FF;
        }

        public void WriteData(byte data)
        {
            if (MasterSystem.IsBitSet(data, 7))
            {
                /* LATCH/DATA byte; get channel (0-3) and type (0 is tone/noise, 1 is volume) */
                latchedChannel = (byte)((data >> 5) & 0x03);
                latchedType = (byte)((data >> 4) & 0x01);

                /* Mask off non-data bits */
                data &= 0x0F;

                /* If target is channel 3 noise (3 bits), mask off highest bit */
                if (latchedChannel == 3 && latchedType == 0)
                    data &= 0x07;

                /* Write to register */
                if (latchedType == 0)
                {
                    /* Data is tone/noise */
                    toneRegisters[latchedChannel] = data;
                }
                else
                {
                    /* Data is volume */
                    volumeRegisters[latchedChannel] = data;
                }
            }
            else
            {
                /* DATA byte; mask off non-data bits */
                data &= 0x3F;

                /* Write to register */
                if (latchedType == 0)
                {
                    /* Data is tone/noise; write to high bits of register */
                    toneRegisters[latchedChannel] = (ushort)((toneRegisters[latchedChannel] & 0x0F) | (data << 4));
                }
                else
                {
                    /* Data is volume; mask off excess bits, then write to low bits of register */
                    if (latchedChannel == 3) data &= 0x07;
                    else data &= 0x0F;
                    volumeRegisters[latchedChannel] = data;
                }
            }
        }
    }
}
