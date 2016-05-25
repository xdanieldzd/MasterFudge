﻿using System;
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
        /* http://clrhome.org/table/
         * http://z80-heaven.wikidot.com/opcode-reference-chart
         */

        public delegate byte IOPortReadDelegate(byte port);
        public delegate void IOPortWriteDelegate(byte port, byte value);

        const int AddCyclesJumpCond8Taken = 5;
        const int AddCyclesRetCondTaken = 6;
        const int AddCyclesCallCondTaken = 7;
        const int AddCyclesRepeatByteOps = 5;   // EDBx

        // Flag notes http://stackoverflow.com/a/30411377

        [Flags]
        enum Flags : byte
        {
            C = (1 << 0),   /* Carry */
            N = (1 << 1),   /* Subtract */
            PV = (1 << 2),  /* Parity or Overflow */
            UB3 = (1 << 3), /* Unused bit 3 */
            H = (1 << 4),   /* Half Carry */
            UB5 = (1 << 5), /* Unused bit 5 */
            Z = (1 << 6),   /* Zero */
            S = (1 << 7),   /* Sign */
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

        bool iff1, iff2, eiDelay, halted;
        byte interruptMode;

        int currentCycles;

        MemoryMapper memoryMapper;
        IOPortReadDelegate ioReadDelegate;
        IOPortWriteDelegate ioWriteDelegate;

        public Z80(MemoryMapper memMap, IOPortReadDelegate ioRead, IOPortWriteDelegate ioWrite)
        {
            memoryMapper = memMap;
            ioReadDelegate = ioRead;
            ioWriteDelegate = ioWrite;

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
            iff1 = iff2 = eiDelay = halted = false;
            interruptMode = 0;
        }

        const bool DIE_ON_UNIMPLEMENTED_PREFIXES = false;

        // TODO: undocumented opcodes (ugh)

        public int Execute()
        {
            currentCycles = 0;

            Program.Log.WriteEvent(DisassembleOpcode(pc));

            if (!halted)
            {
                byte op = memoryMapper.Read8(pc++);
                switch (op)
                {
                    case 0xCB: ExecuteOpCB(); break;
                    case 0xDD: if (DIE_ON_UNIMPLEMENTED_PREFIXES) throw new Exception(string.Format("Unimplemented opcode prefix 0xDD")); break;
                    case 0xED: ExecuteOpED(); break;
                    case 0xFD: if (DIE_ON_UNIMPLEMENTED_PREFIXES) throw new Exception(string.Format("Unimplemented opcode prefix 0xFD")); break;
                    default:
                        currentCycles += cycleCountsMain[op];
                        ExecuteOp(op);
                        break;
                }

                if (op != 0xFB && eiDelay)
                {
                    eiDelay = false;
                    iff1 = iff2 = true;
                }
            }
            else
                currentCycles += 4;

            return currentCycles;
        }

        private void ExecuteOpED()
        {
            byte edOp = memoryMapper.Read8(pc++);
            currentCycles += cycleCountsED[edOp];
            ExecuteOpED(edOp);
        }

        private void ExecuteOpCB()
        {
            byte cbOp = memoryMapper.Read8(pc++);
            currentCycles += cycleCountsCB[cbOp];
            ExecuteOpCB(cbOp);
        }

        private void ExecuteOp(byte op)
        {
            switch (op)
            {
                case 0x00: break;
                case 0x01: LoadRegisterImmediate16(ref bc.Word); break;
                case 0x02: LoadMemory8(bc.Word, af.High); break;
                case 0x03: Increment16(ref bc.Word); break;
                case 0x04: Increment8(ref bc.High); break;
                case 0x05: Decrement8(ref bc.High); break;
                case 0x06: LoadRegisterImmediate8(ref bc.High); break;
                case 0x07: RotateLeftAccumulator(true); break;
                case 0x08: ExchangeRegisters16(ref af, ref afShadow); break;
                case 0x09: Add16(ref hl, bc.Word, false); break;
                case 0x0A: LoadRegisterFromMemory8(ref af.High, bc.Word); break;
                case 0x0B: Decrement16(ref bc.Word); break;
                case 0x0C: Increment8(ref bc.Low); break;
                case 0x0D: Decrement8(ref bc.Low); break;
                case 0x0E: LoadRegisterImmediate8(ref bc.Low); break;
                case 0x0F: RotateRightAccumulator(true); break;
                case 0x10: DecrementJumpNonZero(); break;
                case 0x11: LoadRegisterImmediate16(ref de.Word); break;
                case 0x12: LoadMemory8(de.Word, af.High); break;
                case 0x13: Increment16(ref de.Word); break;
                case 0x14: Increment8(ref de.High); break;
                case 0x15: Decrement8(ref de.High); break;
                case 0x16: LoadRegisterImmediate8(ref de.High); break;
                case 0x17: RotateLeftAccumulator(false); break;
                case 0x18: Jump8(); break;
                case 0x19: Add16(ref hl, de.Word, false); break;
                case 0x1A: LoadRegisterFromMemory8(ref af.High, de.Word); break;
                case 0x1B: Decrement16(ref de.Word); break;
                case 0x1C: Increment8(ref de.Low); break;
                case 0x1D: Decrement8(ref de.Low); break;
                case 0x1E: LoadRegisterImmediate8(ref de.Low); break;
                case 0x1F: RotateRightAccumulator(false); break;
                case 0x20: JumpConditional8(!IsFlagSet(Flags.Z)); break;
                case 0x21: LoadRegisterImmediate16(ref hl.Word); break;
                case 0x22: LoadMemory16(memoryMapper.Read16(pc), hl.Word); pc += 2; break;
                case 0x23: Increment16(ref hl.Word); break;
                case 0x24: Increment8(ref hl.High); break;
                case 0x25: Decrement8(ref hl.High); break;
                case 0x26: LoadRegisterImmediate8(ref hl.High); break;
                case 0x27: DecimalAdjustAccumulator(); break;
                case 0x28: JumpConditional8(IsFlagSet(Flags.Z)); break;
                case 0x29: Add16(ref hl, hl.Word, false); break;
                case 0x2A: LoadRegister16(ref hl.Word, memoryMapper.Read16(memoryMapper.Read16(pc))); pc += 2; break;
                case 0x2B: Decrement16(ref hl.Word); break;
                case 0x2C: Increment8(ref hl.Low); break;
                case 0x2D: Decrement8(ref hl.Low); break;
                case 0x2E: LoadRegisterImmediate8(ref hl.Low); break;
                case 0x2F: af.High ^= 0xFF; SetFlag(Flags.N | Flags.H); break;
                case 0x30: JumpConditional8(!IsFlagSet(Flags.C)); break;
                case 0x31: LoadRegisterImmediate16(ref sp); break;
                case 0x32: LoadMemory8(memoryMapper.Read16(pc), af.High); pc += 2; break;
                case 0x33: Increment16(ref sp); break;
                case 0x34: IncrementMemory8(hl.Word); break;
                case 0x35: DecrementMemory8(hl.Word); break;
                case 0x36: LoadMemory8(hl.Word, memoryMapper.Read8(pc++)); break;
                case 0x37: SetFlag(Flags.C); ClearFlag(Flags.N | Flags.H); break;
                case 0x38: JumpConditional8(IsFlagSet(Flags.C)); break;
                case 0x39: Add16(ref hl, sp, false); break;
                case 0x3A: LoadRegisterFromMemory8(ref af.High, memoryMapper.Read16(pc)); pc += 2; break;
                case 0x3B: Decrement16(ref sp); break;
                case 0x3C: Increment8(ref af.High); break;
                case 0x3D: Decrement8(ref af.High); break;
                case 0x3E: LoadRegisterImmediate8(ref af.High); break;
                case 0x3F: ClearFlag(Flags.N); SetClearFlagConditional(Flags.C, !IsFlagSet(Flags.C)); break;
                case 0x40: LoadRegister8(ref bc.High, bc.High); break;
                case 0x41: LoadRegister8(ref bc.High, bc.Low); break;
                case 0x42: LoadRegister8(ref bc.High, de.High); break;
                case 0x43: LoadRegister8(ref bc.High, de.Low); break;
                case 0x44: LoadRegister8(ref bc.High, hl.High); break;
                case 0x45: LoadRegister8(ref bc.High, hl.Low); break;
                case 0x46: bc.High = memoryMapper.Read8(hl.Word); break;
                case 0x47: LoadRegister8(ref bc.High, af.High); break;
                case 0x48: LoadRegister8(ref bc.Low, bc.High); break;
                case 0x49: LoadRegister8(ref bc.Low, bc.Low); break;
                case 0x4A: LoadRegister8(ref bc.Low, de.High); break;
                case 0x4B: LoadRegister8(ref bc.Low, de.Low); break;
                case 0x4C: LoadRegister8(ref bc.Low, hl.High); break;
                case 0x4D: LoadRegister8(ref bc.Low, hl.Low); break;
                case 0x4E: bc.Low = memoryMapper.Read8(hl.Word); break;
                case 0x4F: LoadRegister8(ref bc.Low, af.High); break;
                case 0x50: LoadRegister8(ref de.High, bc.High); break;
                case 0x51: LoadRegister8(ref de.High, bc.Low); break;
                case 0x52: LoadRegister8(ref de.High, de.High); break;
                case 0x53: LoadRegister8(ref de.High, de.Low); break;
                case 0x54: LoadRegister8(ref de.High, hl.High); break;
                case 0x55: LoadRegister8(ref de.High, hl.Low); break;
                case 0x56: de.High = memoryMapper.Read8(hl.Word); break;
                case 0x57: LoadRegister8(ref de.High, af.High); break;
                case 0x58: LoadRegister8(ref de.Low, bc.High); break;
                case 0x59: LoadRegister8(ref de.Low, bc.Low); break;
                case 0x5A: LoadRegister8(ref de.Low, de.High); break;
                case 0x5B: LoadRegister8(ref de.Low, de.Low); break;
                case 0x5C: LoadRegister8(ref de.Low, hl.High); break;
                case 0x5D: LoadRegister8(ref de.Low, hl.Low); break;
                case 0x5E: de.Low = memoryMapper.Read8(hl.Word); break;
                case 0x5F: LoadRegister8(ref de.Low, af.High); break;
                case 0x60: LoadRegister8(ref hl.High, bc.High); break;
                case 0x61: LoadRegister8(ref hl.High, bc.Low); break;
                case 0x62: LoadRegister8(ref hl.High, de.High); break;
                case 0x63: LoadRegister8(ref hl.High, de.Low); break;
                case 0x64: LoadRegister8(ref hl.High, hl.High); break;
                case 0x65: LoadRegister8(ref hl.High, hl.Low); break;
                case 0x66: hl.High = memoryMapper.Read8(hl.Word); break;
                case 0x67: LoadRegister8(ref hl.High, af.High); break;
                case 0x68: LoadRegister8(ref hl.Low, bc.High); break;
                case 0x69: LoadRegister8(ref hl.Low, bc.Low); break;
                case 0x6A: LoadRegister8(ref hl.Low, de.High); break;
                case 0x6B: LoadRegister8(ref hl.Low, de.Low); break;
                case 0x6C: LoadRegister8(ref hl.Low, hl.High); break;
                case 0x6D: LoadRegister8(ref hl.Low, hl.Low); break;
                case 0x6E: hl.Low = memoryMapper.Read8(hl.Word); break;
                case 0x6F: LoadRegister8(ref hl.Low, af.High); break;
                case 0x70: LoadMemory8(hl.Word, bc.High); break;
                case 0x71: LoadMemory8(hl.Word, bc.Low); break;
                case 0x72: LoadMemory8(hl.Word, de.High); break;
                case 0x73: LoadMemory8(hl.Word, de.Low); break;
                case 0x74: LoadMemory8(hl.Word, hl.High); break;
                case 0x75: LoadMemory8(hl.Word, hl.Low); break;
                case 0x76: halted = true; break;
                case 0x77: LoadMemory8(hl.Word, af.High); break;
                case 0x78: LoadRegister8(ref af.High, bc.High); break;
                case 0x79: LoadRegister8(ref af.High, bc.Low); break;
                case 0x7A: LoadRegister8(ref af.High, de.High); break;
                case 0x7B: LoadRegister8(ref af.High, de.Low); break;
                case 0x7C: LoadRegister8(ref af.High, hl.High); break;
                case 0x7D: LoadRegister8(ref af.High, hl.Low); break;
                case 0x7E: af.High = memoryMapper.Read8(hl.Word); break;
                case 0x7F: LoadRegister8(ref af.High, af.High); break;
                case 0x80: Add8(bc.High, false); break;
                case 0x81: Add8(bc.Low, false); break;
                case 0x82: Add8(de.High, false); break;
                case 0x83: Add8(de.Low, false); break;
                case 0x84: Add8(hl.High, false); break;
                case 0x85: Add8(hl.Low, false); break;
                case 0x86: Add8(memoryMapper.Read8(hl.Word), false); break;
                case 0x87: Add8(af.High, false); break;
                case 0x88: Add8(bc.High, true); break;
                case 0x89: Add8(bc.Low, true); break;
                case 0x8A: Add8(de.High, true); break;
                case 0x8B: Add8(de.Low, true); break;
                case 0x8C: Add8(hl.High, true); break;
                case 0x8D: Add8(hl.Low, true); break;
                case 0x8E: Add8(memoryMapper.Read8(hl.Word), true); break;
                case 0x8F: Add8(af.High, true); break;
                case 0x90: Subtract8(bc.High, false); break;
                case 0x91: Subtract8(bc.Low, false); break;
                case 0x92: Subtract8(de.High, false); break;
                case 0x93: Subtract8(de.Low, false); break;
                case 0x94: Subtract8(hl.High, false); break;
                case 0x95: Subtract8(hl.Low, false); break;
                case 0x96: Subtract8(memoryMapper.Read8(hl.Word), false); break;
                case 0x97: Subtract8(af.High, false); break;
                case 0x98: Subtract8(bc.High, true); break;
                case 0x99: Subtract8(bc.Low, true); break;
                case 0x9A: Subtract8(de.High, true); break;
                case 0x9B: Subtract8(de.Low, true); break;
                case 0x9C: Subtract8(hl.High, true); break;
                case 0x9D: Subtract8(hl.Low, true); break;
                case 0x9E: Subtract8(memoryMapper.Read8(hl.Word), true); break;
                case 0x9F: Subtract8(af.High, true); break;
                case 0xA0: And8(bc.High); break;
                case 0xA1: And8(bc.Low); break;
                case 0xA2: And8(de.High); break;
                case 0xA3: And8(de.Low); break;
                case 0xA4: And8(hl.High); break;
                case 0xA5: And8(hl.Low); break;
                case 0xA6: And8(memoryMapper.Read8(hl.Word)); break;
                case 0xA7: And8(af.High); break;
                case 0xA8: Xor8(bc.High); break;
                case 0xA9: Xor8(bc.Low); break;
                case 0xAA: Xor8(de.High); break;
                case 0xAB: Xor8(de.Low); break;
                case 0xAC: Xor8(hl.High); break;
                case 0xAD: Xor8(hl.Low); break;
                case 0xAE: Xor8(memoryMapper.Read8(hl.Word)); break;
                case 0xAF: Xor8(af.High); break;
                case 0xB0: Or8(bc.High); break;
                case 0xB1: Or8(bc.Low); break;
                case 0xB2: Or8(de.High); break;
                case 0xB3: Or8(de.Low); break;
                case 0xB4: Or8(hl.High); break;
                case 0xB5: Or8(hl.Low); break;
                case 0xB6: Or8(memoryMapper.Read8(hl.Word)); break;
                case 0xB7: Or8(af.High); break;
                case 0xB8: Cp8(bc.High); break;
                case 0xB9: Cp8(bc.Low); break;
                case 0xBA: Cp8(de.High); break;
                case 0xBB: Cp8(de.Low); break;
                case 0xBC: Cp8(hl.High); break;
                case 0xBD: Cp8(hl.Low); break;
                case 0xBE: Cp8(memoryMapper.Read8(hl.Word)); break;
                case 0xBF: Cp8(af.High); break;
                case 0xC0: ReturnConditional(!IsFlagSet(Flags.Z)); break;
                case 0xC1: Pop(ref bc); break;
                case 0xC2: JumpConditional16(!IsFlagSet(Flags.Z)); break;
                case 0xC3: JumpConditional16(true); break;
                case 0xC4: CallConditional16(!IsFlagSet(Flags.Z)); break;
                case 0xC5: Push(bc); break;
                case 0xC6: Add8(memoryMapper.Read8(pc++), false); break;
                case 0xC7: Rst(0x0000); break;
                case 0xC8: ReturnConditional(IsFlagSet(Flags.Z)); break;
                case 0xC9: Return(); break;
                case 0xCA: JumpConditional16(IsFlagSet(Flags.Z)); break;
                // CB
                case 0xCC: CallConditional16(IsFlagSet(Flags.Z)); break;
                case 0xCD: Call16(); break;
                case 0xCE: Add8(memoryMapper.Read8(pc++), true); break;
                case 0xCF: Rst(0x0008); break;
                case 0xD0: ReturnConditional(!IsFlagSet(Flags.C)); break;
                case 0xD1: Pop(ref de); break;
                case 0xD2: JumpConditional16(!IsFlagSet(Flags.C)); break;
                case 0xD3: ioWriteDelegate(memoryMapper.Read8(pc++), af.High); break;
                case 0xD4: CallConditional16(!IsFlagSet(Flags.C)); break;
                case 0xD5: Push(de); break;
                case 0xD6: Subtract8(memoryMapper.Read8(pc++), false); break;
                case 0xD7: Rst(0x0010); break;
                case 0xD8: ReturnConditional(IsFlagSet(Flags.C)); break;
                case 0xD9: ExchangeRegisters16(ref bc, ref bcShadow); ExchangeRegisters16(ref de, ref deShadow); ExchangeRegisters16(ref hl, ref hlShadow); break;
                case 0xDA: JumpConditional16(IsFlagSet(Flags.C)); break;
                case 0xDB: af.High = ioReadDelegate(memoryMapper.Read8(pc++)); break;
                case 0xDC: CallConditional16(IsFlagSet(Flags.C)); break;
                // DD
                case 0xDE: Subtract8(memoryMapper.Read8(pc++), true); break;
                case 0xDF: Rst(0x0018); break;
                case 0xE0: ReturnConditional(!IsFlagSet(Flags.PV)); break;
                case 0xE1: Pop(ref hl); break;
                case 0xE2: JumpConditional16(!IsFlagSet(Flags.PV)); break;
                case 0xE3: ExchangeStackRegister16(ref hl); break;
                case 0xE4: CallConditional16(!IsFlagSet(Flags.PV)); break;
                case 0xE5: Push(hl); break;
                case 0xE6: And8(memoryMapper.Read8(pc++)); break;
                case 0xE7: Rst(0x0020); break;
                case 0xE8: ReturnConditional(IsFlagSet(Flags.PV)); break;
                case 0xE9: pc = hl.Word; break;
                case 0xEA: JumpConditional16(IsFlagSet(Flags.PV)); break;
                case 0xEB: ExchangeRegisters16(ref de, ref hl); break;
                case 0xEC: CallConditional16(IsFlagSet(Flags.PV)); break;
                // ED
                case 0xEE: Xor8(memoryMapper.Read8(pc++)); break;
                case 0xEF: Rst(0x0028); break;
                case 0xF0: ReturnConditional(!IsFlagSet(Flags.S)); break;
                case 0xF1: Pop(ref af); break;
                case 0xF2: JumpConditional16(!IsFlagSet(Flags.S)); break;
                case 0xF3: iff1 = iff2 = false; break;
                case 0xF4: CallConditional16(!IsFlagSet(Flags.S)); break;
                case 0xF5: Push(af); break;
                case 0xF6: Or8(memoryMapper.Read8(pc++)); break;
                case 0xF7: Rst(0x0030); break;
                case 0xF8: ReturnConditional(IsFlagSet(Flags.S)); break;
                case 0xF9: sp = hl.Word; break;
                case 0xFA: JumpConditional16(IsFlagSet(Flags.S)); break;
                case 0xFB: eiDelay = true; break;
                case 0xFC: CallConditional16(IsFlagSet(Flags.S)); break;
                // FD
                case 0xFE: Cp8(memoryMapper.Read8(pc++)); break;
                case 0xFF: Rst(0x0038); break;

                default: throw new Exception(MakeUnimplementedOpcodeString((ushort)(pc - 1)));
            }
        }

        private void ExecuteOpED(byte op)
        {
            switch (op)
            {
                //00-3F - nothing
                case 0x40: PortRead(ref bc.High, bc.Low); break;
                case 0x41: ioWriteDelegate(bc.Low, bc.High); break;
                case 0x42: Subtract16(ref hl, bc.Word, true); break;
                case 0x43: LoadMemory16(memoryMapper.Read16(pc), bc.Word); pc += 2; break;
                case 0x44: Negate(); break;
                case 0x45: iff1 = iff2; Return(); break;
                case 0x46: interruptMode = 0; break;
                case 0x47: i = af.High; break;
                case 0x48: PortRead(ref bc.Low, bc.Low); break;
                case 0x49: ioWriteDelegate(bc.Low, bc.Low); break;
                case 0x4A: Add16(ref hl, bc.Word, true); break;
                case 0x4B: LoadRegister16(ref bc.Word, memoryMapper.Read16(memoryMapper.Read16(pc))); pc += 2; break;
                //4C - undocumented
                case 0x4D: Return(); break; // TODO: really just a return for our purposes? no IFF1=IFF2?
                //4E - undocumented
                case 0x4F: r = af.High; break;
                case 0x50: PortRead(ref de.High, bc.Low); break;
                case 0x51: ioWriteDelegate(bc.Low, de.High); break;
                case 0x52: Subtract16(ref hl, de.Word, true); break;
                case 0x53: LoadMemory16(memoryMapper.Read16(pc), de.Word); pc += 2; break;
                //54 - undocumented
                //55 - undocumented
                case 0x56: interruptMode = 1; break;
                case 0x57: LoadRegister8(ref af.High, i); break;
                case 0x58: PortRead(ref de.Low, bc.Low); break;
                case 0x59: ioWriteDelegate(bc.Low, de.Low); break;
                case 0x5A: Add16(ref hl, de.Word, true); break;
                case 0x5B: LoadRegister16(ref de.Word, memoryMapper.Read16(memoryMapper.Read16(pc))); pc += 2; break;
                //5C - undocumented
                //5D - undocumented
                case 0x5E: interruptMode = 2; break;
                case 0x5F: LoadRegister8(ref af.High, r); break;
                case 0x60: PortRead(ref hl.High, bc.Low); break;
                case 0x61: ioWriteDelegate(bc.Low, hl.High); break;
                case 0x62: Subtract16(ref hl, hl.Word, true); break;
                case 0x63: LoadMemory16(memoryMapper.Read16(pc), hl.Word); pc += 2; break;
                //64 - undocumented
                //65 - undocumented
                //66 - undocumented
                case 0x67: RotateRight4B(); break;
                case 0x68: PortRead(ref hl.Low, bc.Low); break;
                case 0x69: ioWriteDelegate(bc.Low, hl.Low); break;
                case 0x6A: Add16(ref hl, hl.Word, true); break;
                case 0x6B: LoadRegister16(ref hl.Word, memoryMapper.Read16(memoryMapper.Read16(pc))); pc += 2; break;
                //6C - undocumented
                //6D - undocumented
                //6E - undocumented
                case 0x6F: RotateLeft4B(); break;
                //70 - undocumented
                //71 - undocumented
                case 0x72: Subtract16(ref hl, sp, true); break;
                case 0x73: LoadMemory16(memoryMapper.Read16(pc), sp); pc += 2; break;
                //74 - undocumented
                //75 - undocumented
                //76 - undocumented
                //77 - undocumented
                case 0x78: PortRead(ref af.High, bc.Low); break;
                case 0x79: ioWriteDelegate(bc.Low, af.High); break;
                case 0x7A: Add16(ref hl, sp, true); break;
                case 0x7B: LoadRegister16(ref sp, memoryMapper.Read16(memoryMapper.Read16(pc))); pc += 2; break;
                //7C - undocumented
                //7D - undocumented
                //7E - undocumented
                //7F - undocumented
                //80-9F - nothing
                case 0xA0: LoadIncrement(); break;
                case 0xA1: CompareIncrement(); break;
                case 0xA2: InputIncrement(); break;
                case 0xA3: OutputIncrement(); break;
                //A4-A7 - nothing
                case 0xA8: LoadDecrement(); break;
                case 0xA9: CompareDecrement(); break;
                case 0xAA: InputDecrement(); break;
                case 0xAB: OutputDecrement(); break;
                //AC-AF - nothing
                case 0xB0: LoadIncrementRepeat(); break;
                case 0xB1: CompareIncrementRepeat(); break;
                case 0xB2: InputIncrementRepeat(); break;
                case 0xB3: OutputIncrementRepeat(); break;
                //B4-B7 - nothing
                case 0xB8: LoadDecrementRepeat(); break;
                case 0xB9: CompareDecrementRepeat(); break;
                case 0xBA: InputDecrementRepeat(); break;
                case 0xBB: OutputDecrementRepeat(); break;
                //BC-FF - nothing

                default: throw new Exception(MakeUnimplementedOpcodeString((ushort)(pc - 2)));
            }
        }

        private void ExecuteOpCB(byte op)
        {
            switch (op)
            {
                case 0x00: RotateLeftCircular(ref bc.High); break;
                case 0x01: RotateLeftCircular(ref bc.Low); break;
                case 0x02: RotateLeftCircular(ref de.High); break;
                case 0x03: RotateLeftCircular(ref de.Low); break;
                case 0x04: RotateLeftCircular(ref hl.High); break;
                case 0x05: RotateLeftCircular(ref hl.Low); break;
                case 0x06: RotateLeftCircular(hl.Word); break;
                case 0x07: RotateLeftCircular(ref af.High); break;
                case 0x08: RotateRightCircular(ref bc.High); break;
                case 0x09: RotateRightCircular(ref bc.Low); break;
                case 0x0A: RotateRightCircular(ref de.High); break;
                case 0x0B: RotateRightCircular(ref de.Low); break;
                case 0x0C: RotateRightCircular(ref hl.High); break;
                case 0x0D: RotateRightCircular(ref hl.Low); break;
                case 0x0E: RotateRightCircular(hl.Word); break;
                case 0x0F: RotateRightCircular(ref af.High); break;
                case 0x10: RotateLeft(ref bc.High); break;
                case 0x11: RotateLeft(ref bc.Low); break;
                case 0x12: RotateLeft(ref de.High); break;
                case 0x13: RotateLeft(ref de.Low); break;
                case 0x14: RotateLeft(ref hl.High); break;
                case 0x15: RotateLeft(ref hl.Low); break;
                case 0x16: RotateLeft(hl.Word); break;
                case 0x17: RotateLeft(ref af.High); break;
                case 0x18: RotateRight(ref bc.High); break;
                case 0x19: RotateRight(ref bc.Low); break;
                case 0x1A: RotateRight(ref de.High); break;
                case 0x1B: RotateRight(ref de.Low); break;
                case 0x1C: RotateRight(ref hl.High); break;
                case 0x1D: RotateRight(ref hl.Low); break;
                case 0x1E: RotateRight(hl.Word); break;
                case 0x1F: RotateRight(ref af.High); break;
                case 0x20: ShiftLeftArithmetic(ref bc.High); break;
                case 0x21: ShiftLeftArithmetic(ref bc.Low); break;
                case 0x22: ShiftLeftArithmetic(ref de.High); break;
                case 0x23: ShiftLeftArithmetic(ref de.Low); break;
                case 0x24: ShiftLeftArithmetic(ref hl.High); break;
                case 0x25: ShiftLeftArithmetic(ref hl.Low); break;
                case 0x26: ShiftLeftArithmetic(hl.Word); break;
                case 0x27: ShiftLeftArithmetic(ref af.High); break;
                case 0x28: ShiftRightArithmetic(ref bc.High); break;
                case 0x29: ShiftRightArithmetic(ref bc.Low); break;
                case 0x2A: ShiftRightArithmetic(ref de.High); break;
                case 0x2B: ShiftRightArithmetic(ref de.Low); break;
                case 0x2C: ShiftRightArithmetic(ref hl.High); break;
                case 0x2D: ShiftRightArithmetic(ref hl.Low); break;
                case 0x2E: ShiftRightArithmetic(hl.Word); break;
                case 0x2F: ShiftRightArithmetic(ref af.High); break;
                //30-37 - undocumented
                case 0x38: ShiftRightLogical(ref bc.High); break;
                case 0x39: ShiftRightLogical(ref bc.Low); break;
                case 0x3A: ShiftRightLogical(ref de.High); break;
                case 0x3B: ShiftRightLogical(ref de.Low); break;
                case 0x3C: ShiftRightLogical(ref hl.High); break;
                case 0x3D: ShiftRightLogical(ref hl.Low); break;
                case 0x3E: ShiftRightLogical(hl.Word); break;
                case 0x3F: ShiftRightLogical(ref af.High); break;
                case 0x40: TestBit(bc.High, 0); break;
                case 0x41: TestBit(bc.Low, 0); break;
                case 0x42: TestBit(de.High, 0); break;
                case 0x43: TestBit(de.Low, 0); break;
                case 0x44: TestBit(hl.High, 0); break;
                case 0x45: TestBit(hl.Low, 0); break;
                case 0x46: TestBit(memoryMapper.Read8(hl.Word), 0); break;
                case 0x47: TestBit(af.High, 0); break;
                case 0x48: TestBit(bc.High, 1); break;
                case 0x49: TestBit(bc.Low, 1); break;
                case 0x4A: TestBit(de.High, 1); break;
                case 0x4B: TestBit(de.Low, 1); break;
                case 0x4C: TestBit(hl.High, 1); break;
                case 0x4D: TestBit(hl.Low, 1); break;
                case 0x4E: TestBit(memoryMapper.Read8(hl.Word), 1); break;
                case 0x4F: TestBit(af.High, 1); break;
                case 0x50: TestBit(bc.High, 2); break;
                case 0x51: TestBit(bc.Low, 2); break;
                case 0x52: TestBit(de.High, 2); break;
                case 0x53: TestBit(de.Low, 2); break;
                case 0x54: TestBit(hl.High, 2); break;
                case 0x55: TestBit(hl.Low, 2); break;
                case 0x56: TestBit(memoryMapper.Read8(hl.Word), 2); break;
                case 0x57: TestBit(af.High, 2); break;
                case 0x58: TestBit(bc.High, 3); break;
                case 0x59: TestBit(bc.Low, 3); break;
                case 0x5A: TestBit(de.High, 3); break;
                case 0x5B: TestBit(de.Low, 3); break;
                case 0x5C: TestBit(hl.High, 3); break;
                case 0x5D: TestBit(hl.Low, 3); break;
                case 0x5E: TestBit(memoryMapper.Read8(hl.Word), 3); break;
                case 0x5F: TestBit(af.High, 3); break;
                case 0x60: TestBit(bc.High, 4); break;
                case 0x61: TestBit(bc.Low, 4); break;
                case 0x62: TestBit(de.High, 4); break;
                case 0x63: TestBit(de.Low, 4); break;
                case 0x64: TestBit(hl.High, 4); break;
                case 0x65: TestBit(hl.Low, 4); break;
                case 0x66: TestBit(memoryMapper.Read8(hl.Word), 4); break;
                case 0x67: TestBit(af.High, 4); break;
                case 0x68: TestBit(bc.High, 5); break;
                case 0x69: TestBit(bc.Low, 5); break;
                case 0x6A: TestBit(de.High, 5); break;
                case 0x6B: TestBit(de.Low, 5); break;
                case 0x6C: TestBit(hl.High, 5); break;
                case 0x6D: TestBit(hl.Low, 5); break;
                case 0x6E: TestBit(memoryMapper.Read8(hl.Word), 5); break;
                case 0x6F: TestBit(af.High, 5); break;
                case 0x70: TestBit(bc.High, 6); break;
                case 0x71: TestBit(bc.Low, 6); break;
                case 0x72: TestBit(de.High, 6); break;
                case 0x73: TestBit(de.Low, 6); break;
                case 0x74: TestBit(hl.High, 6); break;
                case 0x75: TestBit(hl.Low, 6); break;
                case 0x76: TestBit(memoryMapper.Read8(hl.Word), 6); break;
                case 0x77: TestBit(af.High, 6); break;
                case 0x78: TestBit(bc.High, 7); break;
                case 0x79: TestBit(bc.Low, 7); break;
                case 0x7A: TestBit(de.High, 7); break;
                case 0x7B: TestBit(de.Low, 7); break;
                case 0x7C: TestBit(hl.High, 7); break;
                case 0x7D: TestBit(hl.Low, 7); break;
                case 0x7E: TestBit(memoryMapper.Read8(hl.Word), 7); break;
                case 0x7F: TestBit(af.High, 7); break;
                case 0x80: ResetBit(ref bc.High, 0); break;
                case 0x81: ResetBit(ref bc.Low, 0); break;
                case 0x82: ResetBit(ref de.High, 0); break;
                case 0x83: ResetBit(ref de.Low, 0); break;
                case 0x84: ResetBit(ref hl.High, 0); break;
                case 0x85: ResetBit(ref hl.Low, 0); break;
                case 0x86: ResetBit(hl.Word, 0); break;
                case 0x87: ResetBit(ref af.High, 0); break;
                case 0x88: ResetBit(ref bc.High, 1); break;
                case 0x89: ResetBit(ref bc.Low, 1); break;
                case 0x8A: ResetBit(ref de.High, 1); break;
                case 0x8B: ResetBit(ref de.Low, 1); break;
                case 0x8C: ResetBit(ref hl.High, 1); break;
                case 0x8D: ResetBit(ref hl.Low, 1); break;
                case 0x8E: ResetBit(hl.Word, 1); break;
                case 0x8F: ResetBit(ref af.High, 1); break;
                case 0x90: ResetBit(ref bc.High, 2); break;
                case 0x91: ResetBit(ref bc.Low, 2); break;
                case 0x92: ResetBit(ref de.High, 2); break;
                case 0x93: ResetBit(ref de.Low, 2); break;
                case 0x94: ResetBit(ref hl.High, 2); break;
                case 0x95: ResetBit(ref hl.Low, 2); break;
                case 0x96: ResetBit(hl.Word, 2); break;
                case 0x97: ResetBit(ref af.High, 2); break;
                case 0x98: ResetBit(ref bc.High, 3); break;
                case 0x99: ResetBit(ref bc.Low, 3); break;
                case 0x9A: ResetBit(ref de.High, 3); break;
                case 0x9B: ResetBit(ref de.Low, 3); break;
                case 0x9C: ResetBit(ref hl.High, 3); break;
                case 0x9D: ResetBit(ref hl.Low, 3); break;
                case 0x9E: ResetBit(hl.Word, 3); break;
                case 0x9F: ResetBit(ref af.High, 3); break;
                case 0xA0: ResetBit(ref bc.High, 4); break;
                case 0xA1: ResetBit(ref bc.Low, 4); break;
                case 0xA2: ResetBit(ref de.High, 4); break;
                case 0xA3: ResetBit(ref de.Low, 4); break;
                case 0xA4: ResetBit(ref hl.High, 4); break;
                case 0xA5: ResetBit(ref hl.Low, 4); break;
                case 0xA6: ResetBit(hl.Word, 4); break;
                case 0xA7: ResetBit(ref af.High, 4); break;
                case 0xA8: ResetBit(ref bc.High, 5); break;
                case 0xA9: ResetBit(ref bc.Low, 5); break;
                case 0xAA: ResetBit(ref de.High, 5); break;
                case 0xAB: ResetBit(ref de.Low, 5); break;
                case 0xAC: ResetBit(ref hl.High, 5); break;
                case 0xAD: ResetBit(ref hl.Low, 5); break;
                case 0xAE: ResetBit(hl.Word, 5); break;
                case 0xAF: ResetBit(ref af.High, 5); break;
                case 0xB0: ResetBit(ref bc.High, 6); break;
                case 0xB1: ResetBit(ref bc.Low, 6); break;
                case 0xB2: ResetBit(ref de.High, 6); break;
                case 0xB3: ResetBit(ref de.Low, 6); break;
                case 0xB4: ResetBit(ref hl.High, 6); break;
                case 0xB5: ResetBit(ref hl.Low, 6); break;
                case 0xB6: ResetBit(hl.Word, 6); break;
                case 0xB7: ResetBit(ref af.High, 6); break;
                case 0xB8: ResetBit(ref bc.High, 7); break;
                case 0xB9: ResetBit(ref bc.Low, 7); break;
                case 0xBA: ResetBit(ref de.High, 7); break;
                case 0xBB: ResetBit(ref de.Low, 7); break;
                case 0xBC: ResetBit(ref hl.High, 7); break;
                case 0xBD: ResetBit(ref hl.Low, 7); break;
                case 0xBE: ResetBit(hl.Word, 7); break;
                case 0xBF: ResetBit(ref af.High, 7); break;
                case 0xC0: SetBit(ref bc.High, 0); break;
                case 0xC1: SetBit(ref bc.Low, 0); break;
                case 0xC2: SetBit(ref de.High, 0); break;
                case 0xC3: SetBit(ref de.Low, 0); break;
                case 0xC4: SetBit(ref hl.High, 0); break;
                case 0xC5: SetBit(ref hl.Low, 0); break;
                case 0xC6: SetBit(hl.Word, 0); break;
                case 0xC7: SetBit(ref af.High, 0); break;
                case 0xC8: SetBit(ref bc.High, 1); break;
                case 0xC9: SetBit(ref bc.Low, 1); break;
                case 0xCA: SetBit(ref de.High, 1); break;
                case 0xCB: SetBit(ref de.Low, 1); break;
                case 0xCC: SetBit(ref hl.High, 1); break;
                case 0xCD: SetBit(ref hl.Low, 1); break;
                case 0xCE: SetBit(hl.Word, 1); break;
                case 0xCF: SetBit(ref af.High, 1); break;
                case 0xD0: SetBit(ref bc.High, 2); break;
                case 0xD1: SetBit(ref bc.Low, 2); break;
                case 0xD2: SetBit(ref de.High, 2); break;
                case 0xD3: SetBit(ref de.Low, 2); break;
                case 0xD4: SetBit(ref hl.High, 2); break;
                case 0xD5: SetBit(ref hl.Low, 2); break;
                case 0xD6: SetBit(hl.Word, 2); break;
                case 0xD7: SetBit(ref af.High, 2); break;
                case 0xD8: SetBit(ref bc.High, 3); break;
                case 0xD9: SetBit(ref bc.Low, 3); break;
                case 0xDA: SetBit(ref de.High, 3); break;
                case 0xDB: SetBit(ref de.Low, 3); break;
                case 0xDC: SetBit(ref hl.High, 3); break;
                case 0xDD: SetBit(ref hl.Low, 3); break;
                case 0xDE: SetBit(hl.Word, 3); break;
                case 0xDF: SetBit(ref af.High, 3); break;
                case 0xE0: SetBit(ref bc.High, 4); break;
                case 0xE1: SetBit(ref bc.Low, 4); break;
                case 0xE2: SetBit(ref de.High, 4); break;
                case 0xE3: SetBit(ref de.Low, 4); break;
                case 0xE4: SetBit(ref hl.High, 4); break;
                case 0xE5: SetBit(ref hl.Low, 4); break;
                case 0xE6: SetBit(hl.Word, 4); break;
                case 0xE7: SetBit(ref af.High, 4); break;
                case 0xE8: SetBit(ref bc.High, 5); break;
                case 0xE9: SetBit(ref bc.Low, 5); break;
                case 0xEA: SetBit(ref de.High, 5); break;
                case 0xEB: SetBit(ref de.Low, 5); break;
                case 0xEC: SetBit(ref hl.High, 5); break;
                case 0xED: SetBit(ref hl.Low, 5); break;
                case 0xEE: SetBit(hl.Word, 5); break;
                case 0xEF: SetBit(ref af.High, 5); break;
                case 0xF0: SetBit(ref bc.High, 6); break;
                case 0xF1: SetBit(ref bc.Low, 6); break;
                case 0xF2: SetBit(ref de.High, 6); break;
                case 0xF3: SetBit(ref de.Low, 6); break;
                case 0xF4: SetBit(ref hl.High, 6); break;
                case 0xF5: SetBit(ref hl.Low, 6); break;
                case 0xF6: SetBit(hl.Word, 6); break;
                case 0xF7: SetBit(ref af.High, 6); break;
                case 0xF8: SetBit(ref bc.High, 7); break;
                case 0xF9: SetBit(ref bc.Low, 7); break;
                case 0xFA: SetBit(ref de.High, 7); break;
                case 0xFB: SetBit(ref de.Low, 7); break;
                case 0xFC: SetBit(ref hl.High, 7); break;
                case 0xFD: SetBit(ref hl.Low, 7); break;
                case 0xFE: SetBit(hl.Word, 7); break;
                case 0xFF: SetBit(ref af.High, 7); break;

                default: throw new Exception(MakeUnimplementedOpcodeString((ushort)(pc - 2)));
            }
        }

        private string MakeUnimplementedOpcodeString(ushort address)
        {
            byte[] opcode = DisassembleGetOpcodeBytes(address);
            return string.Format("Unimplemented opcode {0} ({1})", DisassembleMakeByteString(opcode), DisassembleMakeMnemonicString(opcode));
        }

        private void SetFlag(Flags flags)
        {
            af.Low |= (byte)flags;
        }

        private void ClearFlag(Flags flags)
        {
            af.Low ^= (byte)flags;
        }

        private void SetClearFlagConditional(Flags flags, bool condition)
        {
            if (condition)
                af.Low |= (byte)flags;
            else
                af.Low ^= (byte)flags;
        }

        private bool IsFlagSet(Flags flags)
        {
            return (((Flags)af.Low & flags) == flags);
        }

        private void CalculateAndSetParity(byte value)
        {
            int bitsSet = 0;
            for (int i = 0; i < 8; i++)
                if (IsBitSet(value, i))
                    bitsSet++;

            SetClearFlagConditional(Flags.PV, (bitsSet == 0 || (bitsSet % 2) == 0));
        }

        private bool IsBitSet(byte value, int bit)
        {
            return ((value & (1 << bit)) != 0);
        }

        // TODO: verify naming of these functions, wrt addressing modes and shit
        // TODO: reorder however it makes sense

        private void RotateLeft(ref byte value)
        {
            bool isCarrySet = IsFlagSet(Flags.C);
            bool isMsbSet = IsBitSet(value, 7);
            value <<= 1;
            if (isCarrySet) SetBit(ref value, 0);

            SetClearFlagConditional(Flags.C, isMsbSet);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (value == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(value, 7));
            CalculateAndSetParity(value);
        }

        private void RotateLeft(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            RotateLeft(ref value);
            memoryMapper.Write8(address, value);
        }

        private void RotateLeftCircular(ref byte value)
        {
            bool isMsbSet = IsBitSet(value, 7);
            value <<= 1;
            if (isMsbSet) SetBit(ref value, 0);

            SetClearFlagConditional(Flags.C, isMsbSet);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (value == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(value, 7));
            CalculateAndSetParity(value);
        }

        private void RotateLeftCircular(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            RotateLeftCircular(ref value);
            memoryMapper.Write8(address, value);
        }

        private void RotateRight(ref byte value)
        {
            bool isCarrySet = IsFlagSet(Flags.C);
            bool isLsbSet = IsBitSet(value, 0);
            value >>= 1;
            if (isCarrySet) SetBit(ref value, 7);

            SetClearFlagConditional(Flags.C, isLsbSet);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (value == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(value, 7));
            CalculateAndSetParity(value);
        }

        private void RotateRight(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            RotateRight(ref value);
            memoryMapper.Write8(address, value);
        }

        private void RotateRightCircular(ref byte value)
        {
            bool isLsbSet = IsBitSet(value, 0);
            value >>= 1;
            if (isLsbSet) SetBit(ref value, 7);

            SetClearFlagConditional(Flags.C, isLsbSet);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (value == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(value, 7));
            CalculateAndSetParity(value);
        }

        private void RotateRightCircular(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            RotateRightCircular(ref value);
            memoryMapper.Write8(address, value);
        }

        private void ShiftLeftArithmetic(ref byte value)
        {
            bool isMsbSet = IsBitSet(value, 7);
            value <<= 1;

            SetClearFlagConditional(Flags.C, isMsbSet);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (value == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(value, 7));
            CalculateAndSetParity(value);
        }

        private void ShiftLeftArithmetic(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            ShiftLeftArithmetic(ref value);
            memoryMapper.Write8(address, value);
        }

        private void ShiftRightArithmetic(ref byte value)
        {
            bool isLsbSet = IsBitSet(value, 0);
            bool isMsbSet = IsBitSet(value, 7);
            value >>= 1;
            if (isMsbSet) SetBit(ref value, 7);

            SetClearFlagConditional(Flags.C, isLsbSet);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (value == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(value, 7));
            CalculateAndSetParity(value);
        }

        private void ShiftRightArithmetic(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            ShiftRightArithmetic(ref value);
            memoryMapper.Write8(address, value);
        }

        private void ShiftRightLogical(ref byte value)
        {
            bool isLsbSet = IsBitSet(value, 0);
            value >>= 1;

            SetClearFlagConditional(Flags.C, isLsbSet);

            ClearFlag(Flags.N | Flags.H | Flags.S);
            SetClearFlagConditional(Flags.Z, (value == 0));
            CalculateAndSetParity(value);
        }

        private void ShiftRightLogical(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            ShiftRightLogical(ref value);
            memoryMapper.Write8(address, value);
        }

        private void TestBit(byte value, int bit)
        {
            SetClearFlagConditional(Flags.Z, !IsBitSet(value, bit));
            ClearFlag(Flags.N);
            SetFlag(Flags.H);
        }

        private void ResetBit(ref byte value, int bit)
        {
            value ^= (byte)(1 << bit);
        }

        private void ResetBit(ushort address, int bit)
        {
            byte value = memoryMapper.Read8(address);
            ResetBit(ref value, bit);
            memoryMapper.Write8(address, value);
        }

        private void SetBit(ref byte value, int bit)
        {
            value |= (byte)(1 << bit);
        }

        private void SetBit(ushort address, int bit)
        {
            byte value = memoryMapper.Read8(address);
            SetBit(ref value, bit);
            memoryMapper.Write8(address, value);
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

        private void Rst(ushort address)
        {
            memoryMapper.Write8(--sp, (byte)((pc + 1) >> 8));
            memoryMapper.Write8(--sp, (byte)((pc + 1) & 0xFF));
            pc = address;
        }

        private void RotateLeftAccumulator(bool circular)
        {
            bool bit7Set = IsBitSet(af.High, 7);
            if (!circular)
            {
                af.High <<= 1;
                if (IsFlagSet(Flags.C)) af.High |= 0x01;
            }
            else
            {
                af.High = (byte)((af.High << 1) | (af.High >> 7));
            }

            SetClearFlagConditional(Flags.C, bit7Set);
            ClearFlag(Flags.N | Flags.H);
        }

        private void RotateRightAccumulator(bool circular)
        {
            bool bit0Set = IsBitSet(af.High, 0);
            if (!circular)
            {
                af.High >>= 1;
                if (IsFlagSet(Flags.C)) af.High |= 0x80;
            }
            else
            {
                af.High = (byte)((af.High << 7) | (af.High >> 1));
            }

            SetClearFlagConditional(Flags.C, bit0Set);
            ClearFlag(Flags.N | Flags.H);
        }

        private void RotateRight4B()
        {
            byte hlValue = memoryMapper.Read8(hl.Word);

            // A=WX  (HL)=YZ
            // A=WZ  (HL)=XY
            byte a1 = (byte)(af.High >> 4);     //W
            byte a2 = (byte)(af.High & 0xF);    //X
            byte hl1 = (byte)(hlValue >> 4);    //Y
            byte hl2 = (byte)(hlValue & 0xF);   //Z

            af.High = (byte)((a1 << 4) | hl2);
            hlValue = (byte)((a2 << 4) | hl1);

            memoryMapper.Write8(hl.Word, hlValue);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (af.High == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(af.High, 7));
            CalculateAndSetParity(af.High);
        }

        private void RotateLeft4B()
        {
            byte hlValue = memoryMapper.Read8(hl.Word);

            // A=WX  (HL)=YZ
            // A=WY  (HL)=ZX
            byte a1 = (byte)(af.High >> 4);     //W
            byte a2 = (byte)(af.High & 0xF);    //X
            byte hl1 = (byte)(hlValue >> 4);    //Y
            byte hl2 = (byte)(hlValue & 0xF);   //Z

            af.High = (byte)((a1 << 4) | hl1);
            hlValue = (byte)((hl2 << 4) | a2);

            memoryMapper.Write8(hl.Word, hlValue);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (af.High == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(af.High, 7));
            CalculateAndSetParity(af.High);
        }

        private void DecimalAdjustAccumulator()
        {
            int value = af.High;

            if (!IsFlagSet(Flags.N))
            {
                if (IsFlagSet(Flags.H) || ((value & 0x0F) > 9)) value += 6;
                if (IsFlagSet(Flags.C) || value > 0x9F) value += 0x60;
            }
            else
            {
                if (IsFlagSet(Flags.H)) value = (value - 6) & 0xFF;
                if (IsFlagSet(Flags.C)) value -= 0x60;
            }

            ClearFlag(Flags.H | Flags.Z);

            SetClearFlagConditional(Flags.C, ((value & 0x100) != 0));
            SetClearFlagConditional(Flags.Z, ((value & 0xFF) == 0));

            af.High = (byte)value;
        }

        private void ExchangeRegisters16(ref Register reg1, ref Register reg2)
        {
            ushort tmp = reg1.Word;
            reg1.Word = reg2.Word;
            reg2.Word = tmp;
        }

        private void ExchangeStackRegister16(ref Register reg)
        {
            byte sl = memoryMapper.Read8(sp);
            byte sh = memoryMapper.Read8((ushort)(sp + 1));

            memoryMapper.Write8(sp, reg.Low);
            memoryMapper.Write8((ushort)(sp + 1), reg.High);

            reg.Low = sl;
            reg.High = sh;
        }

        private void PortRead(ref byte dest, byte port)
        {
            dest = ioReadDelegate(port);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (dest == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(dest, 7));
            CalculateAndSetParity(dest);
        }

        private void DecrementJumpNonZero()
        {
            bc.High--;
            JumpConditional8(bc.High != 0);
        }

        private void LoadIncrement()
        {
            byte hlValue = memoryMapper.Read8(hl.Word);
            memoryMapper.Write8(de.Word, hlValue);
            Increment16(ref de.Word);
            Increment16(ref hl.Word);
            Decrement16(ref bc.Word);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.PV, (bc.Word != 0));
        }

        private void LoadIncrementRepeat()
        {
            LoadIncrement();
            if (bc.Word != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc--;
            }
        }

        private void LoadDecrement()
        {
            byte hlValue = memoryMapper.Read8(hl.Word);
            memoryMapper.Write8(de.Word, hlValue);
            Decrement16(ref de.Word);
            Decrement16(ref hl.Word);
            Decrement16(ref bc.Word);

            ClearFlag(Flags.N | Flags.H);
            SetClearFlagConditional(Flags.PV, (bc.Word != 0));
        }

        private void LoadDecrementRepeat()
        {
            LoadDecrement();
            if (bc.Word != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc--;
            }
        }

        private void CompareIncrement()
        {
            Cp8(memoryMapper.Read8(hl.Word));
            Increment16(ref hl.Word);
            Decrement16(ref bc.Word);

            SetFlag(Flags.N);
        }

        private void CompareIncrementRepeat()
        {
            CompareIncrement();
            if (bc.Word != 0 && !IsFlagSet(Flags.Z))
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc--;
            }
        }

        private void CompareDecrement()
        {
            Cp8(memoryMapper.Read8(hl.Word));
            Decrement16(ref hl.Word);
            Decrement16(ref bc.Word);

            SetFlag(Flags.N);
        }

        private void CompareDecrementRepeat()
        {
            CompareDecrement();
            if (bc.Word != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc--;
            }
        }

        private void InputIncrement()
        {
            memoryMapper.Write8(hl.Word, ioReadDelegate(bc.Low));
            Increment16(ref hl.Word);
            Decrement8(ref bc.High);

            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.Z, (bc.High == 0));
        }

        private void InputIncrementRepeat()
        {
            InputIncrement();
            if (bc.High != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc--;
            }
        }

        private void InputDecrement()
        {
            memoryMapper.Write8(hl.Word, ioReadDelegate(bc.Low));
            Decrement16(ref hl.Word);
            Decrement8(ref bc.High);

            SetFlag(Flags.N);
            SetClearFlagConditional(Flags.Z, (bc.High == 0));
        }

        private void InputDecrementRepeat()
        {
            InputDecrement();
            if (bc.High != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc--;
            }
        }

        private void OutputIncrement()
        {
            ioWriteDelegate(bc.Low, memoryMapper.Read8(hl.Word));
            Increment16(ref hl.Word);
            Decrement8(ref bc.High);

            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.Z, (bc.High == 0));
        }

        private void OutputIncrementRepeat()
        {
            OutputIncrement();
            if (bc.High != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc--;
            }
        }

        private void OutputDecrement()
        {
            ioWriteDelegate(bc.Low, memoryMapper.Read8(hl.Word));
            Decrement16(ref hl.Word);
            Decrement8(ref bc.High);

            SetFlag(Flags.N);
            SetClearFlagConditional(Flags.Z, (bc.High == 0));
        }

        private void OutputDecrementRepeat()
        {
            OutputDecrement();
            if (bc.High != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc--;
            }
        }

        private void Add8(byte operand, bool withCarry)
        {
            int result = (af.High + (sbyte)operand + (withCarry && IsFlagSet(Flags.C) ? 1 : 0));

            ClearFlag(Flags.N);

            // http://www.smspower.org/forums/14413-PVFlagAndSub#76353, TODO: confirm I did this right
            SetClearFlagConditional(Flags.PV, ((((af.High ^ operand) & 0x80) == 0) && ((af.High ^ result) & 0x80) != 0));

            SetClearFlagConditional(Flags.H, ((result & 0x0F) == 0));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            SetClearFlagConditional(Flags.S, IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.C, (result >= 0x100));

            af.High = (byte)result;
        }

        private void Subtract8(byte operand, bool withCarry)
        {
            int result = (af.High - (sbyte)operand - (withCarry && IsFlagSet(Flags.C) ? 1 : 0));

            ClearFlag(Flags.N);

            // http://www.smspower.org/forums/14413-PVFlagAndSub#76353, TODO: confirm I did this right
            SetClearFlagConditional(Flags.PV, ((((af.High ^ operand) & 0x80) != 0) && ((operand ^ result) & 0x80) == 0));

            SetClearFlagConditional(Flags.H, ((result & 0x0F) == 0));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            SetClearFlagConditional(Flags.S, IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.C, (result >= 0x100));

            af.High = (byte)result;
        }

        private void And8(byte operand)
        {
            af.High &= operand;

            ClearFlag(Flags.C | Flags.N);
            SetFlag(Flags.H);
            SetClearFlagConditional(Flags.Z, (af.High == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(af.High, 7));
            CalculateAndSetParity(af.High);
        }

        private void Xor8(byte operand)
        {
            af.High ^= operand;

            ClearFlag(Flags.C | Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (af.High == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(af.High, 7));
            CalculateAndSetParity(af.High);
        }

        private void Or8(byte operand)
        {
            af.High |= operand;

            ClearFlag(Flags.C | Flags.N | Flags.H);
            SetClearFlagConditional(Flags.Z, (af.High == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(af.High, 7));
            CalculateAndSetParity(af.High);
        }

        private void Cp8(byte operand)
        {
            int result = (af.High - operand);

            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.PV, ((((af.High ^ operand) & 0x80) != 0) && ((operand ^ result) & 0x80) == 0));
            SetClearFlagConditional(Flags.H, ((result & 0x0F) == 0));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            SetClearFlagConditional(Flags.S, IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.C, (result >= 0x100));
        }

        private void Add16(ref Register dest, ushort operand, bool withCarry)
        {
            int result = (dest.Word + (short)operand + (withCarry && IsFlagSet(Flags.C) ? 1 : 0));

            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.H, ((result & 0x00FF) == 0));
            SetClearFlagConditional(Flags.C, (result >= 0x10000));

            if (withCarry)
            {
                SetClearFlagConditional(Flags.PV, ((((dest.Word ^ operand) & 0x8000) == 0) && ((dest.Word ^ result) & 0x8000) != 0));
                SetClearFlagConditional(Flags.Z, ((result & 0xFFFF) == 0));
                SetClearFlagConditional(Flags.S, IsBitSet((byte)result, 15));
            }

            dest.Word = (ushort)result;
        }

        private void Subtract16(ref Register dest, ushort operand, bool withCarry)
        {
            int result = (dest.High - (sbyte)operand - (withCarry && IsFlagSet(Flags.C) ? 1 : 0));

            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.PV, ((((dest.Word ^ operand) & 0x8000) == 0) && ((dest.Word ^ result) & 0x8000) != 0));
            SetClearFlagConditional(Flags.H, ((result & 0x00FF) == 0));
            SetClearFlagConditional(Flags.Z, ((result & 0xFFFF) == 0));
            SetClearFlagConditional(Flags.S, IsBitSet((byte)result, 15));
            SetClearFlagConditional(Flags.C, (result >= 0x10000));

            dest.High = (byte)result;
        }

        private void Negate()
        {
            int result = (0 - af.High);

            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.PV, ((((af.High ^ 0) & 0x80) != 0) && ((0 ^ result) & 0x80) == 0));   // TODO: ?????
            SetClearFlagConditional(Flags.H, ((result & 0x0F) == 0));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            SetClearFlagConditional(Flags.S, IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.C, (result >= 0x100));

            af.High = (byte)result;
        }

        private void LoadRegisterFromMemory8(ref byte register, ushort address)
        {
            LoadRegister8(ref register, memoryMapper.Read8(address));
        }

        private void LoadRegisterImmediate8(ref byte register)
        {
            LoadRegister8(ref register, memoryMapper.Read8(pc++));
        }

        private void LoadRegister8(ref byte register, byte value)
        {
            register = value;
        }

        private void LoadRegisterImmediate16(ref ushort register)
        {
            LoadRegister16(ref register, memoryMapper.Read16(pc));
            pc += 2;
        }

        private void LoadRegister16(ref ushort register, ushort value)
        {
            register = value;
        }

        private void LoadMemory8(ushort address, byte value)
        {
            memoryMapper.Write8(address, value);
        }

        private void LoadMemory16(ushort address, ushort value)
        {
            memoryMapper.Write16(address, value);
        }

        private void Increment8(ref byte register)
        {
            register++;

            // todo: check flags
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.PV, (register == 0x80));    // http://www.smspower.org/forums/1469-Z80INCInstructionsAndFlagAffection#6921
            SetClearFlagConditional(Flags.H, ((register & 0x0F) == 0));
            SetClearFlagConditional(Flags.Z, (register == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(register, 7));
        }

        private void Increment16(ref ushort register)
        {
            register++;
        }

        private void IncrementMemory8(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            Increment8(ref value);
            memoryMapper.Write8(address, value);
        }

        private void Decrement8(ref byte register)
        {
            register--;

            // todo: check flags
            SetFlag(Flags.N);
            SetClearFlagConditional(Flags.PV, (register == 0x7F));    // http://www.smspower.org/forums/1469-Z80INCInstructionsAndFlagAffection#6921
            SetClearFlagConditional(Flags.H, ((register & 0x0F) == 0x0F));
            SetClearFlagConditional(Flags.Z, (register == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(register, 7));
        }

        private void Decrement16(ref ushort register)
        {
            register--;
        }

        private void DecrementMemory8(ushort address)
        {
            byte value = memoryMapper.Read8(address);
            Decrement8(ref value);
            memoryMapper.Write8(address, value);
        }

        private void Jump8()
        {
            pc += (ushort)((sbyte)(memoryMapper.Read8(pc) + 1));
        }

        private void JumpConditional8(bool condition)
        {
            if (condition)
            {
                Jump8();
                currentCycles += AddCyclesJumpCond8Taken;
            }
            else
                pc++;
        }

        private void JumpConditional16(bool condition)
        {
            if (condition)
                pc = memoryMapper.Read16(pc);
            else
                pc += 2;
        }

        private void Call16()
        {
            memoryMapper.Write8(--sp, (byte)((pc + 2) >> 8));
            memoryMapper.Write8(--sp, (byte)((pc + 2) & 0xFF));
            pc = memoryMapper.Read16(pc);
        }

        private void CallConditional16(bool condition)
        {
            if (condition)
            {
                Call16();
                currentCycles += AddCyclesCallCondTaken;
            }
            else
                pc += 2;
        }

        private void Return()
        {
            pc = memoryMapper.Read16(sp);
            sp += 2;
        }

        private void ReturnConditional(bool condition)
        {
            if (condition)
            {
                Return();
                currentCycles += AddCyclesRetCondTaken;
            }
        }
    }
}
