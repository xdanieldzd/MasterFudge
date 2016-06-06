using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MasterFudge.Emulation.CPU
{
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

    public partial class Z80
    {
        /* http://clrhome.org/table/
         * http://z80-heaven.wikidot.com/opcode-reference-chart
         */

        public delegate byte MemoryReadDelegate(ushort address);
        public delegate void MemoryWriteDelegate(ushort address, byte value);

        public delegate byte IOPortReadDelegate(byte port);
        public delegate void IOPortWriteDelegate(byte port, byte value);

        public const double ClockDivider = 15.0;

        const int AddCyclesJumpCond8Taken = 5;
        const int AddCyclesRetCondTaken = 6;
        const int AddCyclesCallCondTaken = 7;
        const int AddCyclesRepeatByteOps = 5;   // EDBx
        const int AddCyclesDDFDCBOps = 8;

        // Flag notes
        // http://stackoverflow.com/a/30411377
        // http://www.retrogames.com/cgi-bin/wwwthreads/showpost.pl?Board=retroemuprog&Number=3997&page=&view=&mode=flat&sb=

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

        Register af, bc, de, hl;
        Register afShadow, bcShadow, deShadow, hlShadow;
        Register ix, iy;
        byte i, r;
        ushort sp, pc;

        public bool IFF1 { get; private set; }
        public byte InterruptMode { get; private set; }
        bool iff2, eiDelay, halted;

        int currentCycles;

        MemoryReadDelegate memoryReadDelegate;
        MemoryWriteDelegate memoryWriteDelegate;
        IOPortReadDelegate ioReadDelegate;
        IOPortWriteDelegate ioWriteDelegate;

        Random random;

        protected Z80()
        {
            DebugLogOpcodes = false;

            random = new Random();
        }

        public Z80(MemoryReadDelegate memRead, MemoryWriteDelegate memWrite, IOPortReadDelegate ioRead, IOPortWriteDelegate ioWrite) : this()
        {
            memoryReadDelegate = memRead;
            memoryWriteDelegate = memWrite;
            ioReadDelegate = ioRead;
            ioWriteDelegate = ioWrite;

            af = bc = de = hl = new Register();
            afShadow = bcShadow = deShadow = hlShadow = new Register();
            ix = iy = new Register();

            i = r = 0;
            sp = pc = 0;

            Reset();
        }

        public static int GetCPUClockCyclesPerFrame(bool isNtsc)
        {
            return (int)(BaseUnit.GetMasterClockCyclesPerFrame(isNtsc) / ClockDivider);
        }

        public static int GetCPUClockCyclesPerScanline(bool isNtsc)
        {
            return (int)(BaseUnit.GetMasterClockCyclesPerScanline(isNtsc) / ClockDivider);
        }

        public void Reset()
        {
            af.Word = bc.Word = de.Word = hl.Word = 0x0000;
            afShadow.Word = bcShadow.Word = deShadow.Word = hlShadow.Word = 0x0000;
            ix.Word = iy.Word = 0x0000;

            i = 0;
            r = (byte)(random.Next() & 0x7F);

            sp = 0xDFF0;
            pc = 0;

            IFF1 = iff2 = eiDelay = halted = false;
            InterruptMode = 0;
        }

        public void ServiceInterrupt(ushort address)
        {
            Rst(address);
            IFF1 = iff2 = false;
            halted = false;
        }

        private byte ReadMemory8(ushort address)
        {
            return memoryReadDelegate.Invoke(address);
        }

        private ushort ReadMemory16(ushort address)
        {
            byte low = memoryReadDelegate.Invoke(address);
            byte high = memoryReadDelegate.Invoke((ushort)(address + 1));
            return (ushort)((high << 8) | low);
        }

        public void WriteMemory8(ushort address, byte value)
        {
            memoryWriteDelegate(address, value);
        }

        public void WriteMemory16(ushort address, ushort value)
        {
            WriteMemory8(address, (byte)(value & 0xFF));
            WriteMemory8((ushort)(address + 1), (byte)(value >> 8));
        }

        public int Execute()
        {
            currentCycles = 0;

            if (!halted)
            {
                if (DebugLogOpcodes)
                    Program.Log.WriteEvent(string.Format("{0} | {1} | {2}", DisassembleOpcode(pc).PadRight(48), PrintRegisters(), PrintFlags()));

                r = (byte)((r + random.Next()) & 0x7F);

                byte op = ReadMemory8(pc++);
                switch (op)
                {
                    case 0xCB: ExecuteOpCB(); break;
                    case 0xDD: ExecuteOpDD(); break;
                    case 0xED: ExecuteOpED(); break;
                    case 0xFD: ExecuteOpFD(); break;
                    default:
                        currentCycles += cycleCountsMain[op];
                        opcodeTable_Main[op](this);
                        break;
                }

                if (op != 0xFB && eiDelay)
                {
                    eiDelay = false;
                    IFF1 = iff2 = true;
                }
            }
            else
                currentCycles += 4;

            return currentCycles;
        }

        private void ExecuteOpED()
        {
            byte edOp = ReadMemory8(pc++);
            currentCycles += cycleCountsED[edOp];
            opcodeTable_ED[edOp](this);
        }

        private void ExecuteOpCB()
        {
            byte cbOp = ReadMemory8(pc++);
            currentCycles += cycleCountsCB[cbOp];
            opcodeTable_CB[cbOp](this);
        }

        private void ExecuteOpDD()
        {
            byte ddOp = ReadMemory8(pc++);
            currentCycles += cycleCountsDDFD[ddOp];
            opcodeTable_DDFD[ddOp](this, ref ix);
        }

        private void ExecuteOpFD()
        {
            byte fdOp = ReadMemory8(pc++);
            currentCycles += cycleCountsDDFD[fdOp];
            opcodeTable_DDFD[fdOp](this, ref iy);
        }

        private ushort CalculateIXIYAddress(Register register)
        {
            return (ushort)(register.Word + (sbyte)ReadMemory8(pc++));
        }

        private void ExecuteOpDDFDCB(byte op, ref Register register)
        {
            currentCycles += (cycleCountsCB[op] + AddCyclesDDFDCBOps);

            sbyte operand = (sbyte)ReadMemory8(pc);
            ushort address = (ushort)(register.Word + operand);
            pc += 2;

            opcodeTable_DDFDCB[op](this, ref register, address);
        }

        private string MakeUnimplementedOpcodeString(string prefix, ushort address)
        {
            byte[] opcode = DisassembleGetOpcodeBytes(address);
            return string.Format("Unimplemented {0}opcode {1} ({2})", (prefix != string.Empty ? prefix + " " : prefix), DisassembleMakeByteString(opcode), DisassembleMakeMnemonicString(opcode));
        }

        private void SetFlag(Flags flags)
        {
            af.Low |= (byte)flags;
        }

        private void ClearFlag(Flags flags)
        {
            af.Low &= (byte)~flags;
        }

        private void SetClearFlagConditional(Flags flags, bool condition)
        {
            if (condition)
                af.Low |= (byte)flags;
            else
                af.Low &= (byte)~flags;
        }

        private bool IsFlagSet(Flags flags)
        {
            return (((Flags)af.Low & flags) == flags);
        }

        private void CalculateAndSetParity(byte value)
        {
            int bitsSet = 0;
            for (int i = 0; i < 8; i++)
                if (Utils.IsBitSet(value, i))
                    bitsSet++;

            SetClearFlagConditional(Flags.PV, (bitsSet == 0 || (bitsSet % 2) == 0));
        }

        // TODO: verify naming of these functions, wrt addressing modes and shit
        // TODO: reorder however it makes sense

        private void RotateLeft(ref byte value)
        {
            bool isCarrySet = IsFlagSet(Flags.C);
            bool isMsbSet = Utils.IsBitSet(value, 7);
            value <<= 1;
            if (isCarrySet) SetBit(ref value, 0);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(value, 7));
            SetClearFlagConditional(Flags.Z, (value == 0));
            ClearFlag(Flags.H);
            CalculateAndSetParity(value);
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isMsbSet);
        }

        private void RotateLeft(ushort address)
        {
            byte value = ReadMemory8(address);
            RotateLeft(ref value);
            WriteMemory8(address, value);
        }

        private void RotateLeftCircular(ref byte value)
        {
            bool isMsbSet = Utils.IsBitSet(value, 7);
            value <<= 1;
            if (isMsbSet) SetBit(ref value, 0);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(value, 7));
            SetClearFlagConditional(Flags.Z, (value == 0));
            ClearFlag(Flags.H);
            CalculateAndSetParity(value);
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isMsbSet);
        }

        private void RotateLeftCircular(ushort address)
        {
            byte value = ReadMemory8(address);
            RotateLeftCircular(ref value);
            WriteMemory8(address, value);
        }

        private void RotateRight(ref byte value)
        {
            bool isCarrySet = IsFlagSet(Flags.C);
            bool isLsbSet = Utils.IsBitSet(value, 0);
            value >>= 1;
            if (isCarrySet) SetBit(ref value, 7);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(value, 7));
            SetClearFlagConditional(Flags.Z, (value == 0));
            ClearFlag(Flags.H);
            CalculateAndSetParity(value);
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isLsbSet);
        }

        private void RotateRight(ushort address)
        {
            byte value = ReadMemory8(address);
            RotateRight(ref value);
            WriteMemory8(address, value);
        }

        private void RotateRightCircular(ref byte value)
        {
            bool isLsbSet = Utils.IsBitSet(value, 0);
            value >>= 1;
            if (isLsbSet) SetBit(ref value, 7);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(value, 7));
            SetClearFlagConditional(Flags.Z, (value == 0x00));
            ClearFlag(Flags.H);
            CalculateAndSetParity(value);
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isLsbSet);
        }

        private void RotateRightCircular(ushort address)
        {
            byte value = ReadMemory8(address);
            RotateRightCircular(ref value);
            WriteMemory8(address, value);
        }

        private void ShiftLeftArithmetic(ref byte value)
        {
            bool isMsbSet = Utils.IsBitSet(value, 7);
            value <<= 1;

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(value, 7));
            SetClearFlagConditional(Flags.Z, (value == 0x00));
            ClearFlag(Flags.H);
            CalculateAndSetParity(value);
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isMsbSet);
        }

        private void ShiftLeftArithmetic(ushort address)
        {
            byte value = ReadMemory8(address);
            ShiftLeftArithmetic(ref value);
            WriteMemory8(address, value);
        }

        private void ShiftRightArithmetic(ref byte value)
        {
            bool isLsbSet = Utils.IsBitSet(value, 0);
            bool isMsbSet = Utils.IsBitSet(value, 7);
            value >>= 1;
            if (isMsbSet) SetBit(ref value, 7);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(value, 7));
            SetClearFlagConditional(Flags.Z, (value == 0x00));
            ClearFlag(Flags.H);
            CalculateAndSetParity(value);
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isLsbSet);
        }

        private void ShiftRightArithmetic(ushort address)
        {
            byte value = ReadMemory8(address);
            ShiftRightArithmetic(ref value);
            WriteMemory8(address, value);
        }

        private void ShiftLeftLogical(ref byte value)
        {
            bool isMsbSet = Utils.IsBitSet(value, 7);
            value <<= 1;
            value |= 0x01;

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(value, 7));
            SetClearFlagConditional(Flags.Z, (value == 0x00));
            ClearFlag(Flags.H);
            CalculateAndSetParity(value);
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isMsbSet);
        }

        private void ShiftLeftLogical(ushort address)
        {
            byte value = ReadMemory8(address);
            ShiftLeftLogical(ref value);
            WriteMemory8(address, value);
        }

        private void ShiftRightLogical(ref byte value)
        {
            bool isLsbSet = Utils.IsBitSet(value, 0);
            value >>= 1;

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(value, 7));
            SetClearFlagConditional(Flags.Z, (value == 0x00));
            ClearFlag(Flags.H);
            CalculateAndSetParity(value);
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isLsbSet);
        }

        private void ShiftRightLogical(ushort address)
        {
            byte value = ReadMemory8(address);
            ShiftRightLogical(ref value);
            WriteMemory8(address, value);
        }

        private void TestBit(byte value, int bit)
        {
            bool isBitSet = Utils.IsBitSet(value, bit);

            SetClearFlagConditional(Flags.S, (bit == 7 && isBitSet));
            SetClearFlagConditional(Flags.Z, !isBitSet);
            SetFlag(Flags.H);
            SetClearFlagConditional(Flags.PV, !isBitSet);
            ClearFlag(Flags.N);
            // C
        }

        private void ResetBit(ref byte value, int bit)
        {
            value &= (byte)~(1 << bit);
        }

        private void ResetBit(ushort address, int bit)
        {
            byte value = ReadMemory8(address);
            ResetBit(ref value, bit);
            WriteMemory8(address, value);
        }

        private void SetBit(ref byte value, int bit)
        {
            value |= (byte)(1 << bit);
        }

        private void SetBit(ushort address, int bit)
        {
            byte value = ReadMemory8(address);
            SetBit(ref value, bit);
            WriteMemory8(address, value);
        }

        private void Pop(ref Register register)
        {
            register.Low = ReadMemory8(sp++);
            register.High = ReadMemory8(sp++);
        }

        private void Push(Register register)
        {
            WriteMemory8(--sp, register.High);
            WriteMemory8(--sp, register.Low);
        }

        private void Rst(ushort address)
        {
            WriteMemory8(--sp, (byte)(pc >> 8));
            WriteMemory8(--sp, (byte)(pc & 0xFF));
            pc = address;
        }

        private void RotateLeftAccumulator()
        {
            bool isCarrySet = IsFlagSet(Flags.C);
            bool isMsbSet = Utils.IsBitSet(af.High, 7);
            af.High <<= 1;
            if (isCarrySet) SetBit(ref af.High, 0);

            // S
            // Z
            ClearFlag(Flags.H);
            // PV
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isMsbSet);
        }

        private void RotateLeftAccumulatorCircular()
        {
            bool isMsbSet = Utils.IsBitSet(af.High, 7);
            af.High <<= 1;
            if (isMsbSet) SetBit(ref af.High, 0);

            // S
            // Z
            ClearFlag(Flags.H);
            // PV
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isMsbSet);
        }

        private void RotateRightAccumulator()
        {
            bool isCarrySet = IsFlagSet(Flags.C);
            bool isLsbSet = Utils.IsBitSet(af.High, 0);
            af.High >>= 1;
            if (isCarrySet) SetBit(ref af.High, 7);

            // S
            // Z
            ClearFlag(Flags.H);
            // PV
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isLsbSet);
        }

        private void RotateRightAccumulatorCircular()
        {
            bool isLsbSet = Utils.IsBitSet(af.High, 0);
            af.High >>= 1;
            if (isLsbSet) SetBit(ref af.High, 7);

            // S
            // Z
            ClearFlag(Flags.H);
            // PV
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, isLsbSet);
        }

        private void RotateRight4B()
        {
            byte hlValue = ReadMemory8(hl.Word);

            // A=WX  (HL)=YZ
            // A=WZ  (HL)=XY
            byte a1 = (byte)(af.High >> 4);     //W
            byte a2 = (byte)(af.High & 0xF);    //X
            byte hl1 = (byte)(hlValue >> 4);    //Y
            byte hl2 = (byte)(hlValue & 0xF);   //Z

            af.High = (byte)((a1 << 4) | hl2);
            hlValue = (byte)((a2 << 4) | hl1);

            WriteMemory8(hl.Word, hlValue);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(af.High, 7));
            SetClearFlagConditional(Flags.Z, (af.High == 0x00));
            ClearFlag(Flags.H);
            CalculateAndSetParity(af.High);
            ClearFlag(Flags.N);
            // C
        }

        private void RotateLeft4B()
        {
            byte hlValue = ReadMemory8(hl.Word);

            // A=WX  (HL)=YZ
            // A=WY  (HL)=ZX
            byte a1 = (byte)(af.High >> 4);     //W
            byte a2 = (byte)(af.High & 0xF);    //X
            byte hl1 = (byte)(hlValue >> 4);    //Y
            byte hl2 = (byte)(hlValue & 0xF);   //Z

            af.High = (byte)((a1 << 4) | hl1);
            hlValue = (byte)((hl2 << 4) | a2);

            WriteMemory8(hl.Word, hlValue);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(af.High, 7));
            SetClearFlagConditional(Flags.Z, (af.High == 0x00));
            ClearFlag(Flags.H);
            CalculateAndSetParity(af.High);
            ClearFlag(Flags.N);
            // C
        }

        private void DecimalAdjustAccumulator()
        {
            // http://www.worldofspectrum.org/faq/reference/z80reference.htm

            byte before = af.High, factor = 0;
            int result = 0;

            if ((af.High > 0x99) || IsFlagSet(Flags.C))
            {
                factor |= 0x60;
                SetFlag(Flags.C);
            }
            else
            {
                factor |= 0x00;
                ClearFlag(Flags.C);
            }

            if (((af.High & 0x0F) > 0x09) || IsFlagSet(Flags.H))
                factor |= 0x06;
            else
                factor |= 0x00;

            if (!IsFlagSet(Flags.N))
                result = (af.High + factor);
            else
                result = (af.High - factor);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, ((byte)result == 0x00));
            SetClearFlagConditional(Flags.H, (((before ^ (byte)result) & 0x10) != 0));
            CalculateAndSetParity(af.High);
            // N
            // C (set above)

            af.High = (byte)result;
        }

        private void ExchangeRegisters16(ref Register reg1, ref Register reg2)
        {
            ushort tmp = reg1.Word;
            reg1.Word = reg2.Word;
            reg2.Word = tmp;
        }

        private void ExchangeStackRegister16(ref Register reg)
        {
            byte sl = ReadMemory8(sp);
            byte sh = ReadMemory8((ushort)(sp + 1));

            WriteMemory8(sp, reg.Low);
            WriteMemory8((ushort)(sp + 1), reg.High);

            reg.Low = sl;
            reg.High = sh;
        }

        private void PortRead(ref byte dest, byte port)
        {
            dest = ioReadDelegate(port);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(dest, 7));
            SetClearFlagConditional(Flags.Z, (dest == 0x00));
            ClearFlag(Flags.H);
            CalculateAndSetParity(dest);
            ClearFlag(Flags.N);
            // C
        }

        private void PortReadFlagsOnly(byte port)
        {
            byte temp = 0;

            PortRead(ref temp, port);
        }

        private void DecrementJumpNonZero()
        {
            bc.High--;
            JumpConditional8(bc.High != 0);
        }

        private void LoadIncrement()
        {
            byte hlValue = ReadMemory8(hl.Word);
            WriteMemory8(de.Word, hlValue);
            Increment16(ref de.Word);
            Increment16(ref hl.Word);
            Decrement16(ref bc.Word);

            // S
            // Z
            ClearFlag(Flags.H);
            SetClearFlagConditional(Flags.PV, (bc.Word != 0));
            ClearFlag(Flags.N);
            // C
        }

        private void LoadIncrementRepeat()
        {
            LoadIncrement();

            // S
            // Z
            ClearFlag(Flags.H);
            ClearFlag(Flags.PV);
            ClearFlag(Flags.N);
            // C

            if (bc.Word != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc -= 2;
            }
        }

        private void LoadDecrement()
        {
            byte hlValue = ReadMemory8(hl.Word);
            WriteMemory8(de.Word, hlValue);
            Decrement16(ref de.Word);
            Decrement16(ref hl.Word);
            Decrement16(ref bc.Word);

            // S
            // Z
            ClearFlag(Flags.H);
            SetClearFlagConditional(Flags.PV, (bc.Word != 0));
            ClearFlag(Flags.N);
            // C
        }

        private void LoadDecrementRepeat()
        {
            LoadDecrement();

            // S
            // Z
            ClearFlag(Flags.H);
            ClearFlag(Flags.PV);
            ClearFlag(Flags.N);
            // C

            if (bc.Word != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc -= 2;
            }
        }

        private void CompareIncrement()
        {
            byte operand = ReadMemory8(hl.Word);
            int result = (af.High - (sbyte)operand);

            hl.Word++;
            bc.Word--;

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, (af.High == operand));
            SetClearFlagConditional(Flags.H, (((af.High ^ result ^ operand) & 0x10) != 0));
            SetClearFlagConditional(Flags.PV, (bc.Word != 0));
            SetFlag(Flags.N);
            // C
        }

        private void CompareIncrementRepeat()
        {
            CompareIncrement();

            if (bc.Word != 0 && !IsFlagSet(Flags.Z))
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc -= 2;
            }
        }

        private void CompareDecrement()
        {
            byte operand = ReadMemory8(hl.Word);
            int result = (af.High - (sbyte)operand);

            hl.Word--;
            bc.Word--;

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, (af.High == operand));
            SetClearFlagConditional(Flags.H, (((af.High ^ result ^ operand) & 0x10) != 0));
            SetClearFlagConditional(Flags.PV, (bc.Word != 0));
            SetFlag(Flags.N);
            // C
        }

        private void CompareDecrementRepeat()
        {
            CompareDecrement();

            if (bc.Word != 0 && !IsFlagSet(Flags.Z))
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc -= 2;
            }
        }

        private void InputIncrement()
        {
            WriteMemory8(hl.Word, ioReadDelegate(bc.Low));
            Increment16(ref hl.Word);
            Decrement8(ref bc.High);

            // S
            SetClearFlagConditional(Flags.Z, (bc.High == 0));
            // H
            // PV
            SetFlag(Flags.N);
            // C
        }

        private void InputIncrementRepeat()
        {
            InputIncrement();

            if (bc.High != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc -= 2;
            }
            else
            {
                // S
                SetFlag(Flags.Z);
                // H
                // PV
                SetFlag(Flags.N);
                // C
            }
        }

        private void InputDecrement()
        {
            WriteMemory8(hl.Word, ioReadDelegate(bc.Low));
            Decrement16(ref hl.Word);
            Decrement8(ref bc.High);

            // S
            SetClearFlagConditional(Flags.Z, (bc.High == 0));
            // H
            // PV
            SetFlag(Flags.N);
            // C
        }

        private void InputDecrementRepeat()
        {
            InputDecrement();

            if (bc.High != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc -= 2;
            }
            else
            {
                // S
                SetFlag(Flags.Z);
                // H
                // PV
                SetFlag(Flags.N);
                // C
            }
        }

        private void OutputIncrement()
        {
            byte value = ReadMemory8(hl.Word);
            ioWriteDelegate(bc.Low, value);
            Increment16(ref hl.Word);
            Decrement8(ref bc.High);

            // S
            SetClearFlagConditional(Flags.Z, (bc.High == 0));
            // H
            // PV
            SetFlag(Flags.N);
            // C
        }

        private void OutputIncrementRepeat()
        {
            OutputIncrement();

            if (bc.High != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc -= 2;
            }
            else
            {
                // S
                SetFlag(Flags.Z);
                // H
                // PV
                SetFlag(Flags.N);
                // C
            }
        }

        private void OutputDecrement()
        {
            ioWriteDelegate(bc.Low, ReadMemory8(hl.Word));
            Decrement16(ref hl.Word);
            Decrement8(ref bc.High);

            // S
            SetClearFlagConditional(Flags.Z, (bc.High == 0));
            // H
            // PV
            SetFlag(Flags.N);
            // C
        }

        private void OutputDecrementRepeat()
        {
            OutputDecrement();

            if (bc.High != 0)
            {
                currentCycles += AddCyclesRepeatByteOps;
                pc -= 2;
            }
            else
            {
                // S
                SetFlag(Flags.Z);
                // H
                // PV
                SetFlag(Flags.N);
                // C
            }
        }

        private void Add8(byte operand, bool withCarry)
        {
            int operandWithCarry = (operand + (withCarry && IsFlagSet(Flags.C) ? 1 : 0));
            int result = (af.High + operandWithCarry);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            SetClearFlagConditional(Flags.H, (((af.High ^ result ^ operand) & 0x10) != 0));
            SetClearFlagConditional(Flags.PV, (((operand ^ af.High ^ 0x80) & (af.High ^ result) & 0x80) != 0));
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, (result > 0xFF));

            af.High = (byte)result;
        }

        private void Subtract8(byte operand, bool withCarry)
        {
            int operandWithCarry = (operand + (withCarry && IsFlagSet(Flags.C) ? 1 : 0));
            int result = (af.High - operandWithCarry);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            SetClearFlagConditional(Flags.H, (((af.High ^ result ^ operand) & 0x10) != 0));
            SetClearFlagConditional(Flags.PV, (((operand ^ af.High) & (af.High ^ result) & 0x80) != 0));
            SetFlag(Flags.N);
            SetClearFlagConditional(Flags.C, (af.High < operandWithCarry));

            af.High = (byte)result;
        }

        private void And8(byte operand)
        {
            int result = (af.High & operand);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            SetFlag(Flags.H);
            CalculateAndSetParity((byte)result);
            ClearFlag(Flags.N);
            ClearFlag(Flags.C);

            af.High = (byte)result;
        }

        private void Xor8(byte operand)
        {
            int result = (af.High ^ operand);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            ClearFlag(Flags.H);
            CalculateAndSetParity((byte)result);
            ClearFlag(Flags.N);
            ClearFlag(Flags.C);

            af.High = (byte)result;
        }

        private void Or8(byte operand)
        {
            int result = (af.High | operand);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            ClearFlag(Flags.H);
            CalculateAndSetParity((byte)result);
            ClearFlag(Flags.N);
            ClearFlag(Flags.C);

            af.High = (byte)result;
        }

        private void Cp8(byte operand)
        {
            int result = (af.High - operand);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet((byte)result, 7));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0));
            SetClearFlagConditional(Flags.H, (((af.High ^ result ^ operand) & 0x10) != 0));
            SetClearFlagConditional(Flags.PV, (((operand ^ af.High) & (af.High ^ result) & 0x80) != 0));
            SetFlag(Flags.N);
            SetClearFlagConditional(Flags.C, (af.High < operand));
        }

        private void Add16(ref Register dest, ushort operand, bool withCarry)
        {
            int operandWithCarry = ((short)operand + (withCarry && IsFlagSet(Flags.C) ? 1 : 0));
            int result = (dest.Word + operandWithCarry);

            // S
            // Z
            SetClearFlagConditional(Flags.H, (((dest.Word & 0x0FFF) + (operandWithCarry & 0x0FFF)) > 0x0FFF));
            // PV
            ClearFlag(Flags.N);
            SetClearFlagConditional(Flags.C, (((dest.Word & 0xFFFF) + (operandWithCarry & 0xFFFF)) > 0xFFFF));

            if (withCarry)
            {
                SetClearFlagConditional(Flags.S, ((result & 0x8000) != 0));
                SetClearFlagConditional(Flags.Z, ((result & 0xFFFF) == 0));
                SetClearFlagConditional(Flags.PV, (((dest.Word ^ operandWithCarry) & 0x8000) == 0 && ((dest.Word ^ (result & 0xFFFF)) & 0x8000) != 0));
            }

            dest.Word = (ushort)result;
        }

        private void Subtract16(ref Register dest, ushort operand, bool withCarry)
        {
            int result = (dest.Word - operand - (withCarry && IsFlagSet(Flags.C) ? 1 : 0));

            SetClearFlagConditional(Flags.S, ((result & 0x8000) != 0));
            SetClearFlagConditional(Flags.Z, ((result & 0xFFFF) == 0));
            SetClearFlagConditional(Flags.H, ((((dest.Word ^ result ^ operand) >> 8) & 0x10) != 0));
            SetClearFlagConditional(Flags.PV, (((operand ^ dest.Word) & (dest.Word ^ result) & 0x8000) != 0));
            SetFlag(Flags.N);
            SetClearFlagConditional(Flags.C, ((result & 0x10000) != 0));

            dest.Word = (ushort)result;
        }

        private void Negate()
        {
            int result = (0 - af.High);

            SetClearFlagConditional(Flags.S, ((result & 0xFF) >= 0x80));
            SetClearFlagConditional(Flags.Z, ((result & 0xFF) == 0x00));
            SetClearFlagConditional(Flags.H, ((0 - (af.High & 0x0F)) < 0));
            SetClearFlagConditional(Flags.PV, (af.High == 0x80));
            SetFlag(Flags.N);
            SetClearFlagConditional(Flags.C, (af.High != 0x00));

            af.High = (byte)result;
        }

        private void LoadRegisterFromMemory8(ref byte register, ushort address, bool specialRegs)
        {
            LoadRegister8(ref register, ReadMemory8(address), specialRegs);
        }

        private void LoadRegisterImmediate8(ref byte register, bool specialRegs)
        {
            LoadRegister8(ref register, ReadMemory8(pc++), specialRegs);
        }

        private void LoadRegister8(ref byte register, byte value, bool specialRegs)
        {
            register = value;

            // Register is I or R?
            if (specialRegs)
            {
                SetClearFlagConditional(Flags.S, Utils.IsBitSet(register, 7));
                SetClearFlagConditional(Flags.Z, (register == 0));
                ClearFlag(Flags.H);
                SetClearFlagConditional(Flags.PV, (iff2));
                ClearFlag(Flags.N);
                // C
            }
        }

        private void LoadRegisterImmediate16(ref ushort register)
        {
            LoadRegister16(ref register, ReadMemory16(pc));
            pc += 2;
        }

        private void LoadRegister16(ref ushort register, ushort value)
        {
            register = value;
        }

        private void LoadMemory8(ushort address, byte value)
        {
            WriteMemory8(address, value);
        }

        private void LoadMemory16(ushort address, ushort value)
        {
            WriteMemory16(address, value);
        }

        private void Increment8(ref byte register)
        {
            byte result = (byte)(register + 1);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(result, 7));
            SetClearFlagConditional(Flags.Z, (result == 0x00));
            SetClearFlagConditional(Flags.H, ((register & 0x0F) == 0x0F));
            SetClearFlagConditional(Flags.PV, (register == 0x7F));
            ClearFlag(Flags.N);
            // C

            register = result;
        }

        private void Increment16(ref ushort register)
        {
            register++;
        }

        private void IncrementMemory8(ushort address)
        {
            byte value = ReadMemory8(address);
            Increment8(ref value);
            WriteMemory8(address, value);
        }

        private void Decrement8(ref byte register)
        {
            byte result = (byte)(register - 1);

            SetClearFlagConditional(Flags.S, Utils.IsBitSet(result, 7));
            SetClearFlagConditional(Flags.Z, (result == 0x00));
            SetClearFlagConditional(Flags.H, ((register & 0x0F) == 0x00));
            SetClearFlagConditional(Flags.PV, (register == 0x80));
            SetFlag(Flags.N);
            // C

            register = result;
        }

        private void Decrement16(ref ushort register)
        {
            register--;
        }

        private void DecrementMemory8(ushort address)
        {
            byte value = ReadMemory8(address);
            Decrement8(ref value);
            WriteMemory8(address, value);
        }

        private void Jump8()
        {
            pc += (ushort)((sbyte)(ReadMemory8(pc) + 1));
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
                pc = ReadMemory16(pc);
            else
                pc += 2;
        }

        private void Call16()
        {
            WriteMemory8(--sp, (byte)((pc + 2) >> 8));
            WriteMemory8(--sp, (byte)((pc + 2) & 0xFF));
            pc = ReadMemory16(pc);
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
            pc = ReadMemory16(sp);
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
