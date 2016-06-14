using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MasterFudge.Emulation.CPU;

namespace MasterFudge.Emulation
{
    public partial class BaseUnit
    {
        // TODO: expand into savestate functionality?

        public partial class Debugging
        {
            public static CoreDebugSnapshot GetCoreDebugSnapshot(BaseUnit emulator)
            {
                return new CoreDebugSnapshot(emulator);
            }
        }

        public class CoreDebugSnapshot
        {
            public byte[] MemoryMap { get; private set; }
            public Z80.CpuDebugSnapshot CPU { get; private set; }

            public CoreDebugSnapshot(BaseUnit emulator)
            {
                emulator.IsPaused = true;

                MemoryMap = new byte[0x10000];
                for (int i = 0; i < MemoryMap.Length; i++) MemoryMap[i] = emulator.ReadMemory((ushort)i);

                CPU = new Z80.CpuDebugSnapshot(this, emulator.cpu);

                emulator.IsPaused = false;
            }

            public byte GetMemory8(ushort address)
            {
                return MemoryMap[address & 0xFFFF];
            }

            public ushort GetMemory16(ushort address)
            {
                byte low = GetMemory8(address);
                byte high = GetMemory8((ushort)(address + 1));
                return (ushort)((high << 8) | low);
            }
        }
    }
}
