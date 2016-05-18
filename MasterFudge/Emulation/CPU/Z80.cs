using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using MasterFudge.Emulation.Memory;

namespace MasterFudge.Emulation.CPU
{
    public partial class Z80
    {
        [Flags]
        enum Flags : byte
        {
            Carry = (1 << 0),
            Subtract = (1 << 1),
            Parity = (1 << 2),
            UnusedBit3 = (1 << 3),
            HalfCarry = (1 << 4),
            UnusedBit5 = (1 << 5),
            Zero = (1 << 6),
            Sign = (1 << 7)
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Register
        {
            [FieldOffset(0)]
            public byte Low;
            [FieldOffset(1)]
            public byte High;
            [FieldOffset(0)]
            public ushort Word;
        }

        Register af, bc, de, hl;
        Register afShadow, bcShadow, deShadow, hlShadow;
        Register ix, iy;
        byte i, r;
        ushort sp, pc;

        byte iff1, iff2, im;
        bool halted;

        MemoryMapper memoryMapper;

        public Z80(MemoryMapper memMap)
        {
            memoryMapper = memMap;

            af = bc = de = hl = new Register();
            afShadow = bcShadow = deShadow = hlShadow = new Register();
            ix = iy = new Register();
            i = r = 0;
            sp = pc = 0;

            Reset();
        }

        public void Reset()
        {
            af.Word = sp = 0xFFFF;

            pc = 0;
            iff1 = iff2 = im = 0;
        }

        public int Execute()
        {
            int cycles = 4;

            if (!halted)
            {
                byte op = memoryMapper.Read8(pc++);

                // TODO: rework cycle count stuff, its way too cumbersome right now

                if (op == 0xCB)
                    cycles = 4;
                else if (op == 0xDD)
                    cycles = 4;
                else if (op == 0xED)
                    cycles = cycleCountsED[memoryMapper.Read8(pc)];
                else if (op == 0xFD)
                    cycles = 4;
                else
                    cycles = cycleCountsMain[op];

                opcodeTableMain[op](this);
            }

            return cycles;
        }

        private void SetFlag(Flags flags)
        {
            af.Low |= (byte)flags;
        }

        private void ClearFlag(Flags flags)
        {
            af.Low ^= (byte)flags;
        }

        private bool IsFlagSet(Flags flags)
        {
            return (((Flags)af.Low & flags) == flags);
        }

        private void Pop(ref Register register)
        {
            register.Low = memoryMapper.Read8(sp++);
            register.High = memoryMapper.Read8(sp++);
        }

        private void Push(Register register)
        {
            memoryMapper.Write8(--sp, register.High);
            memoryMapper.Write8(--sp, register.Low);
        }
    }
}
