﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.CPU
{
    // TODO: uhhhh do this once switch-based stuff is working


    delegate void OpcodeDelegate(Z80 cpu);

    public partial class Z80
    {
        /* http://clrhome.org/table/
         * http://z80-heaven.wikidot.com/opcode-reference-chart
         */

        static OpcodeDelegate[] opcodeTableMain = new OpcodeDelegate[]
        {
            /* 0x00 */
            new OpcodeDelegate((c) => { /* nop */ }),
            new OpcodeDelegate((c) => { c.LoadRegisterImmediate16(ref c.bc.Word); }),
            new OpcodeDelegate((c) => { c.LoadMemory8(c.bc.Word, c.af.High); }),
            new OpcodeDelegate((c) => { c.IncrementRegister16(ref c.bc.Word); }),
            new OpcodeDelegate((c) => { c.IncrementRegister8(ref c.bc.High); }),
            new OpcodeDelegate((c) => { c.DecrementRegister8(ref c.bc.High); }),
            new OpcodeDelegate((c) => { c.LoadRegisterImmediate8(ref c.bc.High); }),
            new OpcodeDelegate((c) => { /* rlca */ }),
            new OpcodeDelegate((c) => { /* ex af,af' */ }),
            new OpcodeDelegate((c) => { /* add hl,bc */ }),
            new OpcodeDelegate((c) => { /* ld a,(bc)*/ }),
            new OpcodeDelegate((c) => { c.DecrementRegister16(ref c.bc.Word); }),
            new OpcodeDelegate((c) => { c.IncrementRegister8(ref c.bc.Low); }),
            new OpcodeDelegate((c) => { c.DecrementRegister8(ref c.bc.Low); }),
            new OpcodeDelegate((c) => { c.LoadRegisterImmediate8(ref c.bc.Low); }),
            new OpcodeDelegate((c) => { /* rrca */ }),
            /* 0x10 */
            new OpcodeDelegate((c) => { /* djnz * */ }),
            new OpcodeDelegate((c) => { c.LoadRegisterImmediate16(ref c.de.Word); }),
            new OpcodeDelegate((c) => { c.LoadMemory8(c.de.Word, c.af.High); }),
            new OpcodeDelegate((c) => { c.IncrementRegister16(ref c.de.Word); }),
            new OpcodeDelegate((c) => { c.IncrementRegister8(ref c.de.High); }),
            new OpcodeDelegate((c) => { c.DecrementRegister8(ref c.de.High); }),
            new OpcodeDelegate((c) => { c.LoadRegisterImmediate8(ref c.de.High); }),
            new OpcodeDelegate((c) => { /* rla */ }),
            new OpcodeDelegate((c) => { /* jr * */ }),
            new OpcodeDelegate((c) => { /* add hl,de */ }),
            new OpcodeDelegate((c) => { /* ld a,(de) */ }),
            new OpcodeDelegate((c) => { c.DecrementRegister16(ref c.de.Word); }),
            new OpcodeDelegate((c) => { c.IncrementRegister8(ref c.de.Low); }),
            new OpcodeDelegate((c) => { c.DecrementRegister8(ref c.de.Low); }),
            new OpcodeDelegate((c) => { c.LoadRegisterImmediate8(ref c.de.Low); }),
            new OpcodeDelegate((c) => { /* rra */ }),
            /* 0x20 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x30 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { c.LoadRegisterImmediate16(ref c.sp); }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x40 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x50 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x60 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x70 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x80 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x90 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xA0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xB0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xC0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { c.JumpConditional16(true); }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xD0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xE0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { opcodeTableED[c.memoryMapper.Read8(c.pc++)](c); }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xF0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { c.Push(c.af); }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { })
        };

        static OpcodeDelegate[] opcodeTableED = new OpcodeDelegate[]
        {
            /* 0x00 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x10 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x20 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x30 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x40 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { c.memoryMapper.Write16(c.memoryMapper.Read16(c.pc), c.bc.Word); c.pc += 2; }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x50 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { throw new Exception("meh"); }),
            /* 0x60 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x70 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { c.memoryMapper.Write16(c.memoryMapper.Read16(c.pc), c.sp); c.pc += 2; }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x80 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0x90 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xA0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xB0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xC0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xD0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xE0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            /* 0xF0 */
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { }),
            new OpcodeDelegate((c) => { })
        };
    }
}
