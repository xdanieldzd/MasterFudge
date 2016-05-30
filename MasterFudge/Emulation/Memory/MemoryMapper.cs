using System;
using System.Collections.Generic;

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
            for (int i = 0; i < readMap.Length; i++) readMap[i] = DummyRead;

            writeMap = new MemoryWriteDelegate[memoryUpperBound + 1];
            for (int i = 0; i < writeMap.Length; i++) writeMap[i] = DummyWrite;
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

        private byte DummyRead(ushort address)
        {
            throw new Exception(string.Format("Unsupported read from address 0x{0:X4}", address));
        }

        private void DummyWrite(ushort address, byte value)
        {
            throw new Exception(string.Format("Unsupported write to address 0x{0:X4}, value 0x{1:X2}", address, value));
        }

        public byte Read8(ushort address)
        {
            return readMap[address](address);
        }

        public ushort Read16(ushort address)
        {
            byte low = readMap[address](address);
            byte high = readMap[address + 1]((ushort)(address + 1));
            return (ushort)((high << 8) | low);
        }

        public void Write8(ushort address, byte value)
        {
            writeMap[address](address, value);
        }

        public void Write16(ushort address, ushort value)
        {
            writeMap[address](address, (byte)(value & 0xFF));
            writeMap[address + 1]((ushort)(address + 1), (byte)(value >> 8));
        }
    }
}
