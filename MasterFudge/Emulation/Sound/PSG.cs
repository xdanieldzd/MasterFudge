using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MasterFudge.Emulation.CPU;

namespace MasterFudge.Emulation.Sound
{
    public class PSG
    {
        /* http://www.smspower.org/Development/SN76489 */

        const int numChannels = 4;

        /* Channel registers */
        ushort[] volumeRegisters;       /* Channels 0-3: 4 bits */
        ushort[] toneRegisters;         /* Channels 0-2 (tone): 10 bits; channel 3 (noise): 3 bits */

        /* Channel counters */
        short[] channelCounters;        /* 10-bit counters, output bit in bit 14 */

        /* Volume attenuation table */
        double[] volumeTable;           /* 2dB change per volume register step */

        /* Latched channel/type */
        byte latchedChannel, latchedType;

        /* Clock-related values */
        bool isNtsc;
        double inputClock;

        /* Output wave provider (NAudio) */
        public WaveProvider WaveProvider { get; private set; }

        public PSG()
        {
            SetTVSystem(false);

            volumeRegisters = new ushort[numChannels];
            toneRegisters = new ushort[numChannels];

            channelCounters = new short[numChannels];

            double volume = 1024.0;
            volumeTable = new double[16];
            for (int i = 0; i < volumeTable.Length; i++)
            {
                volumeTable[i] = volume;
                volume *= 0.79432823;
            }
            volumeTable[15] = 0;

            Reset();

            // TODO: make this sample thingy do PSG!
            WaveProvider = new WaveProvider();
            WaveProvider.SetWaveFormat(44100, 1);
        }

        public void Reset()
        {
            latchedChannel = latchedType = 0x00;

            for (int i = 0; i < numChannels; i++)
            {
                volumeRegisters[i] = 0x0001;
                toneRegisters[i] = 0x0000;
            }
        }

        public void SetTVSystem(bool ntsc)
        {
            isNtsc = ntsc;
            inputClock = ((isNtsc ? MasterSystem.MasterClockNTSC : MasterSystem.MasterClockPAL) / Z80.ClockDivider);
        }

        public void Execute(int currentCycles)
        {
            double[] channelOutputs = new double[numChannels];

            /* Process channels */
            for (int ch = 0; ch < numChannels; ch++)
            {
                /* Check for counter underflow */
                if ((channelCounters[ch] & 0x03FF) > 0)
                    channelCounters[ch]--;

                /* Counter underflowed, reload and flip output bit */
                if ((channelCounters[ch] & 0x03FF) == 0)
                    channelCounters[ch] = (short)(((channelCounters[ch] & 0x4000) ^ 0x4000) | toneRegisters[ch] & 0x3FF);

                if (ch < 3)
                {
                    /* Tone channel */
                    channelOutputs[ch] = (inputClock / ((2 * toneRegisters[ch]) * 16));
                    //channelOutputs[ch] *= (((channelCounters[ch] & 0x4000) == 0x4000) ? -1 : 1);
                    channelOutputs[ch] *= volumeTable[volumeRegisters[ch]];

                    // TODO: verify and fix this, also... what do I do now? -.-
                }
                else
                {
                    /* Noise channel */
                }
            }

            //WaveProvider.AddSample((short)channelOutputs[0]);
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
