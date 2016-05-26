using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Memory
{
    public delegate byte MemoryReadDelegate(ushort address);
    public delegate void MemoryWriteDelegate(ushort address, byte value);

    public class MemoryMapper
    {
        const uint memoryUpperBound = 0xFFFF;

        List<MemoryAreaDescriptor> memoryAreas;
        MemoryReadDelegate[] readMap;
        MemoryWriteDelegate[] writeMap;

        public MemoryMapper()
        {
            memoryAreas = new List<MemoryAreaDescriptor>();
            readMap = new MemoryReadDelegate[memoryUpperBound + 1];
            writeMap = new MemoryWriteDelegate[memoryUpperBound + 1];
        }

        public void AddMemoryArea(ushort startAddress, ushort endAddress, MemoryReadDelegate readHandler, MemoryWriteDelegate writeHandler)
        {
            AddMemoryArea(new MemoryAreaDescriptor(startAddress, endAddress, readHandler, writeHandler));
        }

        public void AddMemoryArea(MemoryAreaDescriptor area)
        {
            if (area == null) return;

            memoryAreas.Add(area);
            for (int i = area.StartAddress; i <= area.EndAddress; i++)
            {
                readMap[i] = area.Read;
                writeMap[i] = area.Write;
            }
        }

        public void RemoveMemoryArea(ushort startAddress, ushort endAddress)
        {
            RemoveMemoryArea(new MemoryAreaDescriptor(startAddress, endAddress, null, null));
        }

        public void RemoveMemoryArea(MemoryAreaDescriptor area)
        {
            if (area == null) return;

            for (int i = area.StartAddress; i <= area.EndAddress; i++)
            {
                readMap[i] = null;
                writeMap[i] = null;
            }
            memoryAreas.RemoveAll(x => x.StartAddress == area.StartAddress && x.EndAddress == area.EndAddress);
        }

        public byte Read8(ushort address)
        {
            if (readMap[address] == null)
                throw new Exception(string.Format("Unsupported 8-bit read from address 0x{0:X4}", address));
            else
                return readMap[address](address);
        }

        public ushort Read16(ushort address)
        {
            if (readMap[address] == null || readMap[address + 1] == null)
                throw new Exception(string.Format("Unsupported 16-bit read from address 0x{0:X4}", address));
            else
                return (ushort)((readMap[address + 1]((ushort)(address + 1)) << 8) + readMap[address](address));
        }

        public void Write8(ushort address, byte value)
        {
            if (writeMap[address] == null)
                throw new Exception(string.Format("Unsupported 8-bit write to address 0x{0:X4}, value 0x{1:X2}", address, value));
            else
                writeMap[address](address, value);
        }

        public void Write16(ushort address, ushort value)
        {
            if (writeMap[address] == null || writeMap[address + 1] == null)
                throw new Exception(string.Format("Unsupported 16-bit write to address 0x{0:X4}, value 0x{1:X4}", address, value));
            else
            {
                writeMap[address](address, (byte)(value & 0xFF));
                writeMap[address + 1]((ushort)(address + 1), (byte)(value >> 8));
            }
        }
    }
}
