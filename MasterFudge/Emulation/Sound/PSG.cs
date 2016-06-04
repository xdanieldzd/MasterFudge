using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Sound
{
    public class PSG
    {
        /* http://www.smspower.org/Development/SN76489 */

        const int numChannels = 4;

        /* Base unit stuff */
        BaseUnitType baseUnitType;
        BaseUnitRegion baseUnitRegion;
        bool isNtsc { get { return (baseUnitRegion == BaseUnitRegion.JapanNTSC || baseUnitRegion == BaseUnitRegion.ExportNTSC); } }

        /* Channel registers */
        ushort[] volumeRegisters;       /* Channels 0-3: 4 bits */
        ushort[] toneRegisters;         /* Channels 0-2 (tone): 10 bits; channel 3 (noise): 3 bits */

        /* Channel counters */
        short[] channelCounters;        /* 10-bit counters, output bit in bit 14 */

        /* Volume attenuation table */
        short[] volumeTable;            /* 2dB change per volume register step */

        /* Latched channel/type */
        byte latchedChannel, latchedType;

        /* Clock-related values */
        int cyclesInLine;

        /* Sound output stuff */
        public short[] Samples { get; private set; }
        int currentSamplePosition;
        int updateCounter;

        public PSG()
        {
            SetTvSystem(PowerBase.DefaultBaseUnitRegion);

            volumeRegisters = new ushort[numChannels];
            toneRegisters = new ushort[numChannels];

            channelCounters = new short[numChannels];

            double volume = 8191.0;
            volumeTable = new short[16];
            for (int i = 0; i < volumeTable.Length; i++)
            {
                volumeTable[i] = (short)volume;
                volume *= 0.79432823;
            }
            volumeTable[15] = 0;

            Reset();
        }

        public void Reset()
        {
            latchedChannel = latchedType = 0x00;

            for (int i = 0; i < numChannels; i++)
            {
                volumeRegisters[i] = 0x0001;
                toneRegisters[i] = 0x0000;
            }

            Samples = new short[2047];
            currentSamplePosition = 0;
            updateCounter = 0;
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
            // TODO: timing is garbage, I guess

            int cyclesPerLine = (CPU.Z80.GetCPUClockCyclesPerScanline(isNtsc) / 6);

            cyclesInLine += currentCycles;

            if (cyclesInLine > cyclesPerLine)
            {
                cyclesInLine = 0;

                short[] channelOutputs = new short[numChannels];

                /* Process channels */
                for (int ch = 0; ch < numChannels; ch++)
                {
                    /* Check for counter underflow */
                    if ((channelCounters[ch] & 0x03FF) > 0)
                        channelCounters[ch]--;

                    /* Counter underflowed, reload and flip output bit */
                    if ((channelCounters[ch] & 0x03FF) == 0)
                        channelCounters[ch] = (short)(((channelCounters[ch] & 0x4000) ^ 0x4000) | ((toneRegisters[ch] & 0x3FF) / 2));

                    if (ch < 3)
                    {
                        /* Tone channel */
                        channelOutputs[ch] = (short)(volumeTable[volumeRegisters[ch]] * (((channelCounters[ch] & 0x4000) == 0x4000) ? 1 : -1));
                    }
                    else
                    {
                        /* Noise channel */
                    }
                }

                /* Mix output together */
                if (Samples != null)
                {
                    short mixed = 0;
                    for (int i = 0; i < numChannels; i++)
                        mixed += channelOutputs[i];

                    if (currentSamplePosition < Samples.Length)
                        Samples[currentSamplePosition++] = mixed;
                    else
                    {
                        currentSamplePosition = 0;
                        updateCounter++;
                        if (updateCounter == 2)
                        {
                            updateCounter = 0;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void WriteData(byte data)
        {
            if (PowerBase.IsBitSet(data, 7))
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
                    toneRegisters[latchedChannel] = (ushort)((toneRegisters[latchedChannel] & 0x03F0) | data);
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
                    /* Data is tone/noise */
                    if (latchedChannel == 3)
                    {
                        /* Target is channel 3 noise, mask off excess bits and write to low bits of register */
                        toneRegisters[latchedChannel] = (ushort)(data & 0x07);
                    }
                    else
                    {
                        /* Target is not channel 3 noise, write to high bits of register */
                        toneRegisters[latchedChannel] = (ushort)((toneRegisters[latchedChannel] & 0x000F) | (data << 4));
                    }
                }
                else
                {
                    /* Data is volume; mask off excess bits and write to low bits of register */
                    volumeRegisters[latchedChannel] = (ushort)(data & 0x0F);
                }
            }
        }
    }
}
