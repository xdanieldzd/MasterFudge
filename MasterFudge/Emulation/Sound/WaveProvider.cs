using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

namespace MasterFudge.Emulation.Sound
{
    // TODO: nope, this seems all wrong...

    public class WaveProvider : WaveProvider16
    {
        short[] samples;
        int position;

        public WaveProvider()
        {
            samples = new short[32768];
            position = 0;
        }

        public void AddSample(short sample)
        {
            samples[position] = sample;
            position++;
            if (position >= samples.Length) position = 0;
        }

        public override int Read(short[] buffer, int offset, int sampleCount)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                buffer[i + offset] = samples[i % samples.Length];
            }
            return samples.Length;
        }
    }
}
