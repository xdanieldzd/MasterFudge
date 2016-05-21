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
        const int AddCyclesJumpCond8Taken = 5;
        const int AddCyclesRetCondTaken = 6;
        const int AddCyclesCallCondTaken = 7;
        const int AddCyclesRepeatByteOps = 5;   // EDBx

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
            iff1 = iff2 = eiDelay = halted = false;
            interruptMode = 0;
        }

        public int Execute()
        {
            currentCycles = 0;

            Program.Log.WriteEvent(DisassembleOpcode(pc));

            if (!halted)
            {
                byte op = memoryMapper.Read8(pc++);
                switch (op)
                {
                    case 0xCB: break;
                    case 0xDD: break;
                    case 0xED: ExecuteOpED(); break;
                    case 0xFD: break;
                    default:
                        currentCycles += cycleCountsMain[op];
                        //opcodeTableMain[op](this);
                        ExecuteOp_BigSwitchThing(op);
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
            //opcodeTableED[edOp](this);
            ExecuteOpED_BigSwitchThing(edOp);
        }

        private void ExecuteOp_BigSwitchThing(byte op)
        {
            switch (op)
            {
                case 0x00: break;
                case 0x01: LoadRegisterImmediate16(ref bc.Word); break;
                case 0x02: LoadMemory8(bc.Word, af.High); break;
                case 0x03: IncrementRegister16(ref bc.Word); break;
                case 0x04: IncrementRegister8(ref bc.High); break;
                case 0x05: DecrementRegister8(ref bc.High); break;
                case 0x06: LoadRegisterImmediate8(ref bc.High); break;
                case 0x07: RotateLeftAccumulator(true); break;
                case 0x08: ExchangeRegisters16(ref af, ref afShadow); break;

                case 0x0B: DecrementRegister16(ref bc.Word); break;
                case 0x0C: IncrementRegister8(ref bc.Low); break;
                case 0x0D: DecrementRegister8(ref bc.Low); break;
                case 0x0E: LoadRegisterImmediate8(ref bc.Low); break;
                case 0x0F: RotateRightAccumulator(true); break;

                case 0x11: LoadRegisterImmediate16(ref de.Word); break;
                case 0x12: LoadMemory8(de.Word, af.High); break;
                case 0x13: IncrementRegister16(ref de.Word); break;
                case 0x14: IncrementRegister8(ref de.High); break;
                case 0x15: DecrementRegister8(ref de.High); break;
                case 0x16: LoadRegisterImmediate8(ref de.High); break;
                case 0x17: RotateLeftAccumulator(false); break;
                case 0x18: Jump8(); break;

                case 0x1B: DecrementRegister16(ref de.Word); break;
                case 0x1C: IncrementRegister8(ref de.Low); break;
                case 0x1D: DecrementRegister8(ref de.Low); break;
                case 0x1E: LoadRegisterImmediate8(ref de.Low); break;
                case 0x1F: RotateRightAccumulator(false); break;
                case 0x20: JumpConditional8(!IsFlagSet(Flags.Z)); break;

                case 0x27: DecimalAdjustAccumulator(); break;
                case 0x28: JumpConditional8(IsFlagSet(Flags.Z)); break;

                case 0x30: JumpConditional8(!IsFlagSet(Flags.C)); break;
                case 0x31: LoadRegisterImmediate16(ref sp); break;

                case 0x38: JumpConditional8(IsFlagSet(Flags.C)); break;

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

                case 0xC0: ReturnConditional(!IsFlagSet(Flags.Z)); break;

                case 0xC3: JumpConditional16(true); break;
                case 0xC4: CallConditional16(!IsFlagSet(Flags.Z)); break;

                case 0xC8: ReturnConditional(IsFlagSet(Flags.Z)); break;
                case 0xC9: Return(); break;

                case 0xCC: CallConditional16(IsFlagSet(Flags.Z)); break;
                case 0xCD: Call16(); break;

                case 0xD0: ReturnConditional(!IsFlagSet(Flags.C)); break;

                case 0xD4: CallConditional16(!IsFlagSet(Flags.C)); break;

                case 0xD8: ReturnConditional(IsFlagSet(Flags.C)); break;
                case 0xD9: ExchangeRegisters16(ref bc, ref bcShadow); ExchangeRegisters16(ref de, ref deShadow); ExchangeRegisters16(ref hl, ref hlShadow); break;
                case 0xDC: CallConditional16(IsFlagSet(Flags.C)); break;

                case 0xE3: ExchangeStackRegister16(ref hl); break;

                case 0xEB: ExchangeRegisters16(ref de, ref hl); break;

                case 0xF3: iff1 = iff2 = false; break;

                case 0xF5: Push(af); break;

                case 0xFB: eiDelay = true; break;

                default: throw new Exception(MakeUnimplementedOpcodeString((ushort)(pc - 1)));
            }
        }

        private void ExecuteOpED_BigSwitchThing(byte op)
        {
            // TODO: everything
            switch (op)
            {
                case 0x43: LoadMemory16(memoryMapper.Read16(pc), bc.Word); pc += 2; break;

                case 0x46: interruptMode = 0; break;

                case 0x53: LoadMemory16(memoryMapper.Read16(pc), de.Word); pc += 2; break;

                case 0x56: interruptMode = 1; break;

                case 0x5E: interruptMode = 2; break;

                case 0x73: LoadMemory16(memoryMapper.Read16(pc), sp); pc += 2; break;

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

        private bool IsBitSet(byte value, int bit)
        {
            return ((value & (1 << bit)) != 0);
        }

        // TODO: verify naming of these functions, wrt addressing modes and shit
        // TODO: reorder however it makes sense

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

        private void LoadRegister8(ref byte register, byte value)
        {
            register = value;
        }

        private void LoadRegisterImmediate8(ref byte register)
        {
            register = memoryMapper.Read8(pc);
            pc++;
        }

        private void LoadRegisterImmediate16(ref ushort register)
        {
            register = memoryMapper.Read16(pc);
            pc += 2;
        }

        private void LoadMemory8(ushort address, byte value)
        {
            memoryMapper.Write8(address, value);
        }

        private void LoadMemory16(ushort address, ushort value)
        {
            memoryMapper.Write16(address, value);
        }

        private void IncrementRegister8(ref byte register)
        {
            register++;

            // todo: check flags
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.PV, (register == 0x80));    // http://www.smspower.org/forums/1469-Z80INCInstructionsAndFlagAffection#6921
            SetClearFlagConditional(Flags.H, ((register & 0x0F) == 0));
            SetClearFlagConditional(Flags.Z, (register == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(register, 7));
        }

        private void IncrementRegister16(ref ushort register)
        {
            register++;
        }

        private void DecrementRegister8(ref byte register)
        {
            register--;

            // todo: check flags
            SetFlag(Flags.N);
            SetClearFlagConditional(Flags.PV, (register == 0x7F));    // http://www.smspower.org/forums/1469-Z80INCInstructionsAndFlagAffection#6921
            SetClearFlagConditional(Flags.H, ((register & 0x0F) == 0x0F));
            SetClearFlagConditional(Flags.Z, (register == 0));
            SetClearFlagConditional(Flags.S, IsBitSet(register, 7));
        }

        private void DecrementRegister16(ref ushort register)
        {
            register--;
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
