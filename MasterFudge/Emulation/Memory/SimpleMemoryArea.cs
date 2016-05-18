using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Memory
{
    public abstract class SimpleMemoryArea
    {
        public abstract ushort GetStartAddress();
        public abstract ushort GetEndAddress();

        public abstract byte Read8(ushort address);
        public abstract void Write8(ushort address, byte value);

        public MemoryAreaDescriptor GetMemoryAreaDescriptor()
        {
            return new MemoryAreaDescriptor(GetStartAddress(), GetEndAddress(), new MemoryReadDelegate(Read8), new MemoryWriteDelegate(Write8));
        }
    }
}
