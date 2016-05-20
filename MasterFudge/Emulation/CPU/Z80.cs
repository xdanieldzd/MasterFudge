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

        byte iff1, iff2, im;
        bool halted;

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
            iff1 = iff2 = im = 0;
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

                case 0x0B: DecrementRegister16(ref bc.Word); break;
                case 0x0C: IncrementRegister8(ref bc.Low); break;
                case 0x0D: DecrementRegister8(ref bc.Low); break;
                case 0x0E: LoadRegisterImmediate8(ref bc.Low); break;

                case 0x11: LoadRegisterImmediate16(ref de.Word); break;
                case 0x12: LoadMemory8(de.Word, af.High); break;
                case 0x13: IncrementRegister16(ref de.Word); break;
                case 0x14: IncrementRegister8(ref de.High); break;
                case 0x15: DecrementRegister8(ref de.High); break;
                case 0x16: LoadRegisterImmediate8(ref de.High); break;

                case 0x1B: DecrementRegister16(ref de.Word); break;
                case 0x1C: IncrementRegister8(ref de.Low); break;
                case 0x1D: DecrementRegister8(ref de.Low); break;
                case 0x1E: LoadRegisterImmediate8(ref de.Low); break;

                case 0x31: LoadRegisterImmediate16(ref sp); break;
                case 0xC3: JumpConditional16(true); break;
                case 0xF5: Push(af); break;

                default: throw new Exception(MakeUnimplementedOpcodeString((ushort)(pc - 1)));
            }
        }

        private void ExecuteOpED_BigSwitchThing(byte op)
        {
            // TODO: everything
            switch (op)
            {
                case 0x43: memoryMapper.Write16(memoryMapper.Read16(pc), bc.Word); pc += 2; break;
                case 0x73: memoryMapper.Write16(memoryMapper.Read16(pc), sp); pc += 2; break;

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

        private void JumpConditional8(bool condition)
        {
            if (condition)
                pc += (ushort)((sbyte)memoryMapper.Read8(pc));
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
    }
}
