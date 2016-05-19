using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.CPU
{
    public partial class Z80
    {
        // TODO: verify again, also add all the other crap

        static readonly string[] opcodeMnemonic_Main = new string[]
        {
            "NOP",              "LD BC, 0x{0:X4}",  "LD (BC), A",       "INC BC",           "INC B",            "DEC B",            "LD B, 0x{0:X2}",   "RLCA",             /* 0x00 */
            "EX AF, AF'",       "ADD HL, BC",       "LD A, (BC)",       "DEC BC",           "INC C",            "DEC C",            "LD C, 0x{0:X2}",   "RRCA",             /* 0x08 */
            "DJNZ",             "LD DE, 0x{0:X4}",  "LD (DE), A",       "INC DE",           "INC D",            "DEC D",            "LD D, 0x{0:X2}",   "RLA",              /* 0x10 */
            "JR 0x{0:X2}",      "ADD HL, DE",       "LD A, (DE)",       "DEC DE",           "INC E",            "DEC E",            "LD E, 0x{0:X2}",   "RRA",              /* 0x18 */
            "JR NZ, 0x{0:X2}",  "LD HL, 0x{0:X4}",  "LD (0x{0:X4}), HL","INC HL",           "INC H",            "DEC H",            "LD H, 0x{0:X2}",   "DAA",              /* 0x20 */
            "JR Z, 0x{0:X2}",   "ADD HL, HL",       "LD HL, (0x{0:X4})","DEC HL",           "INC L",            "DEC L",            "LD L, 0x{0:X2}",   "CPL",              /* 0x28 */
            "JR NC, 0x{0:X2}",  "LD SP, 0x{0:X4}",  "LD (0x{0:X4}), A", "INC SP",           "INC (HL)",         "DEC (HL)",         "LD (HL), 0x{0:X2}","SCF",              /* 0x30 */
            "JR C, 0x{0:X2}",   "ADD HL, SP",       "LD A, (0x{0:X4})", "DEC SP",           "INC A",            "DEC A",            "LD A, 0x{0:X2}",   "CCF",              /* 0x38 */
            "LD B, B",          "LD B, C",          "LD B, D",          "LD B, E",          "LD B, H",          "LD B, L",          "LD B, (HL)",       "LD B, A",          /* 0x40 */
            "LD C, B",          "LD C, C",          "LD C, D",          "LD C, E",          "LD C, H",          "LD C, L",          "LD C, (HL)",       "LD C, A",          /* 0x48 */
            "LD D, B",          "LD D, C",          "LD D, D",          "LD D, E",          "LD D, H",          "LD D, L",          "LD D, (HL)",       "LD D, A",          /* 0x50 */
            "LD E, B",          "LD E, C",          "LD E, D",          "LD E, E",          "LD E, H",          "LD E, L",          "LD E, (HL)",       "LD E, A",          /* 0x58 */
            "LD H, B",          "LD H, C",          "LD H, D",          "LD H, E",          "LD H, H",          "LD H, L",          "LD H, (HL)",       "LD H, A",          /* 0x60 */
            "LD L, B",          "LD L, C",          "LD L, D",          "LD L, E",          "LD L, H",          "LD L, L",          "LD L, (HL)",       "LD L, A",          /* 0x68 */
            "LD (HL), B",       "LD (HL), C",       "LD (HL), D",       "LD (HL), E",       "LD (HL), H",       "LD (HL), L",       "HALT",             "LD (HL), A",       /* 0x70 */
            "LD A, B",          "LD A, C",          "LD A, D",          "LD A, E",          "LD A, H",          "LD A, L",          "LD A, (HL)",       "LD A, A",          /* 0x78 */
            "ADD A, B",         "ADD A, C",         "ADD A, D",         "ADD A, E",         "ADD A, H",         "ADD A, L",         "ADD A, (HL)",      "ADD A, A",         /* 0x80 */
            "ADC A, B",         "ADC A, C",         "ADC A, D",         "ADC A, E",         "ADC A, H",         "ADC A, L",         "ADC A, (HL)",      "ADC A, A",         /* 0x88 */
            "SUB B",            "SUB C",            "SUB D",            "SUB E",            "SUB H",            "SUB L",            "SUB (HL)",         "SUB A",            /* 0x90 */
            "SBC B",            "SBC C",            "SBC D",            "SBC E",            "SBC H",            "SBC L",            "SBC (HL)",         "SBC A",            /* 0x98 */
            "AND B",            "AND C",            "AND D",            "AND E",            "AND H",            "AND L",            "AND (HL)",         "AND A",            /* 0xA0 */
            "XOR B",            "XOR C",            "XOR D",            "XOR E",            "XOR H",            "XOR L",            "XOR (HL)",         "XOR A",            /* 0xA8 */
            "OR B",             "OR C",             "OR D",             "OR E",             "OR H",             "OR L",             "OR (HL)",          "OR A",             /* 0xA0 */
            "CP B",             "CP C",             "CP D",             "CP E",             "CP H",             "CP L",             "CP (HL)",          "CP A",             /* 0xB8 */
            "RET NZ",           "POP BC",           "JP NZ, 0x{0:X4}",  "JP 0x{0:X4}",      "CALL NZ, 0x{0:X4}","PUSH BC",          "ADD A, 0x{0:X2}",  "RST 00",           /* 0xC0 */
            "RET Z",            "RET",              "JP Z, 0x{0:X4}",   string.Empty,       "CALL Z, 0x{0:X4}", "CALL 0x{0:X4}",    "ADC A, 0x{0:X2}",  "RST 08",           /* 0xC8 */
            "RET NC",           "POP DE",           "JP NC, 0x{0:X4}",  "OUT 0x{0:X2}, A",  "CALL NC, 0x{0:X4}","PUSH DE",          "SUB 0x{0:X2}",     "RST 10",           /* 0xD0 */
            "RET C",            "EXX",              "JP C, 0x{0:X4}",   "IN A, 0x{0:X2}",   "CALL C, 0x{0:X4}", string.Empty,       "SBC 0x{0:X2}",     "RST 18",           /* 0xD8 */
            "RET PO",           "POP HL",           "JP PO, 0x{0:X4}",  "EX (SP), HL",      "CALL PO, 0x{0:X4}","PUSH HL",          "AND 0x{0:X2}",     "RST 20",           /* 0xE0 */
            "RET PE",           "JP (HL)",          "JP PE, 0x{0:X4}",  "EX DE, HL",        "CALL PE, 0x{0:X4}",string.Empty,       "XOR 0x{0:X2}",     "RST 28",           /* 0xE8 */
            "RET P",            "POP AF",           "JP P, 0x{0:X4}",   "DI",               "CALL P, 0x{0:X4}", "PUSH AF",          "OR 0x{0:X2}",      "RST 30",           /* 0xF0 */
            "RET M",            "LD SP, HL",        "JP M, 0x{0:X4}",   "EI",               "CALL M, 0x{0:X4}", string.Empty,       "CP 0x{0:X2}",      "RST 38",           /* 0xF0 */
        };

        static readonly int[] opcodeLength_Main = new int[]
        {
            1, 3, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1,
            2, 3, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 2, 1,
            2, 3, 3, 1, 1, 1, 2, 1, 2, 1, 3, 1, 1, 1, 2, 1,
            2, 3, 3, 1, 1, 1, 2, 1, 2, 1, 3, 1, 1, 1, 2, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 3, 3, 3, 1, 2, 1, 1, 1, 3, -1, 3, 3, 2, 1,
            1, 1, 3, 2, 3, 1, 2, 1, 1, 1, 3, 2, 3, -1, 2, 1,
            1, 1, 3, 1, 3, 1, 2, 1, 1, 1, 3, 1, 3, -1, 2, 1,
            1, 1, 3, 1, 3, 1, 2, 1, 1, 1, 3, 1, 3, -1, 2, 1,
        };

        private string DisassembleOpcode(ushort address)
        {
            // TODO: less shitty, more CB/DD/ED/FD/etc

            byte[] opcode = new byte[]
            {
                memoryMapper.Read8(address),
                (address + 1 <= 0xFFFF ? memoryMapper.Read8((ushort)(address + 1)) : (byte)0),
                (address + 2 <= 0xFFFF ? memoryMapper.Read8((ushort)(address + 2)) : (byte)0)
            };
            int len = opcodeLength_Main[opcode[0]];

            string bytes = string.Empty;
            string mnemonic = string.Empty;

            switch (opcode[0])
            {
                case 0xCB: bytes = "CB ??"; mnemonic = "(can't disasm)"; break;
                case 0xDD: bytes = "DD ??"; mnemonic = "(can't disasm)"; break;
                case 0xED: bytes = "ED ??"; mnemonic = "(can't disasm)"; break;
                case 0xFD: bytes = "FD ??"; mnemonic = "(can't disasm)"; break;
                default:
                    bytes = string.Join(" ", opcode.Select(x => string.Format("{0:X2}", x)));
                    switch (len)
                    {
                        case 1:
                            mnemonic = opcodeMnemonic_Main[opcode[0]];
                            break;
                        case 2:
                            mnemonic = string.Format(opcodeMnemonic_Main[opcode[0]], opcode[1]);
                            break;
                        case 3:
                            mnemonic = string.Format(opcodeMnemonic_Main[opcode[0]], (opcode[2] << 8 | opcode[1]));
                            break;
                    }
                    break;
            }

            return string.Format("{0:X4} | {1} | {2}", address, bytes.PadRight(9), mnemonic);
        }
    }
}
