using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Memory
{
    public class MemoryAreaDescriptor
    {
        public ushort StartAddress { get; private set; }
        public ushort EndAddress { get; private set; }
        public MemoryReadDelegate Read { get; private set; }
        public MemoryWriteDelegate Write { get; private set; }

        public ushort AreaSize { get { return (ushort)(EndAddress - StartAddress); } }

        public MemoryAreaDescriptor(ushort startAddress, ushort endAddress, MemoryReadDelegate readHandler, MemoryWriteDelegate writeHandler)
        {
            StartAddress = startAddress;
            EndAddress = endAddress;

            Read = readHandler;
            Write = writeHandler;
        }
    }
}
