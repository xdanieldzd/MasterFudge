using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.CPU
{
    public partial class Z80
    {
        // TODO: verify again, also add all the other crap (CB/DD/FD/etc)

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

        static readonly string[] opcodeMnemonic_ED = new string[]
        {
            ".DB 0xED, 0x00",   ".DB 0xED, 0x01",   ".DB 0xED, 0x02",   ".DB 0xED, 0x03",   ".DB 0xED, 0x04",   ".DB 0xED, 0x05",   ".DB 0xED, 0x06",   ".DB 0xED, 0x07",   /* 0x00 */
            ".DB 0xED, 0x08",   ".DB 0xED, 0x09",   ".DB 0xED, 0x0A",   ".DB 0xED, 0x0B",   ".DB 0xED, 0x0C",   ".DB 0xED, 0x0D",   ".DB 0xED, 0x0E",   ".DB 0xED, 0x0F",   /* 0x08 */
            ".DB 0xED, 0x10",   ".DB 0xED, 0x11",   ".DB 0xED, 0x12",   ".DB 0xED, 0x13",   ".DB 0xED, 0x14",   ".DB 0xED, 0x15",   ".DB 0xED, 0x16",   ".DB 0xED, 0x17",   /* 0x10 */
            ".DB 0xED, 0x18",   ".DB 0xED, 0x19",   ".DB 0xED, 0x1A",   ".DB 0xED, 0x1B",   ".DB 0xED, 0x1C",   ".DB 0xED, 0x1D",   ".DB 0xED, 0x1E",   ".DB 0xED, 0x1F",   /* 0x18 */
            ".DB 0xED, 0x20",   ".DB 0xED, 0x21",   ".DB 0xED, 0x22",   ".DB 0xED, 0x23",   ".DB 0xED, 0x24",   ".DB 0xED, 0x25",   ".DB 0xED, 0x26",   ".DB 0xED, 0x27",   /* 0x20 */
            ".DB 0xED, 0x28",   ".DB 0xED, 0x29",   ".DB 0xED, 0x2A",   ".DB 0xED, 0x2B",   ".DB 0xED, 0x2C",   ".DB 0xED, 0x2D",   ".DB 0xED, 0x2E",   ".DB 0xED, 0x2F",   /* 0x28 */
            ".DB 0xED, 0x30",   ".DB 0xED, 0x31",   ".DB 0xED, 0x32",   ".DB 0xED, 0x33",   ".DB 0xED, 0x34",   ".DB 0xED, 0x35",   ".DB 0xED, 0x36",   ".DB 0xED, 0x37",   /* 0x30 */
            ".DB 0xED, 0x38",   ".DB 0xED, 0x39",   ".DB 0xED, 0x3A",   ".DB 0xED, 0x3B",   ".DB 0xED, 0x3C",   ".DB 0xED, 0x3D",   ".DB 0xED, 0x3E",   ".DB 0xED, 0x3F",   /* 0x38 */
            "IN B, (C)",        "OUT (C), B",       "SBC HL, BC",       "LD (0x{0:X4}), BC","NEG",              "RETN",             "IM 0",             "LD I, A",          /* 0x40 */
            "IN C, (C)",        "OUT (C), C",       "ADC HL, BC",       "LD BC, (0x{0:X4})",".DB 0xED, 0x4C",   "RETI",             ".DB 0xED, 0x4E",   "LD R, A",          /* 0x48 */
            "IN D, (C)",        "OUT (C), D",       "SBC HL, DE",       "LD (0x{0:X4}), DE",".DB 0xED, 0x54",   ".DB 0xED, 0x55",   "IM 1",             "LD A, I",          /* 0x50 */
            "IN E, (C)",        "OUT (C), E",       "ADC HL, DE",       "LD DE, (0x{0:X4})",".DB 0xED, 0x5C",   ".DB 0xED, 0x5D",   "IM 2",             "LD A, R",          /* 0x58 */
            "IN H, (C)",        "OUT (C), H",       "SBC HL, HL",       ".DB 0xED, 0x63",   ".DB 0xED, 0x64",   ".DB 0xED, 0x65",   ".DB 0xED, 0x66",   "RRD",              /* 0x60 */
            "IN L, (C)",        "OUT (C), L",       "ADC HL, HL",       ".DB 0xED, 0x6B",   ".DB 0xED, 0x6C",   ".DB 0xED, 0x6D",   ".DB 0xED, 0x6E",   "RLD",              /* 0x68 */
            ".DB 0xED, 0x70",   ".DB 0xED, 0x71",   "SBC HL, SP",       "LD (0x{0:X4}), SP",".DB 0xED, 0x74",   ".DB 0xED, 0x75",   ".DB 0xED, 0x76",   ".DB 0xED, 0x77",   /* 0x70 */
            "IN A, (C)",        "OUT (C), A",       "ADC HL, SP",       "LD SP, (0x{0:X4})",".DB 0xED, 0x7C",   ".DB 0xED, 0x7D",   ".DB 0xED, 0x7E",   ".DB 0xED, 0x7F",   /* 0x78 */
            ".DB 0xED, 0x80",   ".DB 0xED, 0x81",   ".DB 0xED, 0x82",   ".DB 0xED, 0x83",   ".DB 0xED, 0x84",   ".DB 0xED, 0x85",   ".DB 0xED, 0x86",   ".DB 0xED, 0x87",   /* 0x80 */
            ".DB 0xED, 0x88",   ".DB 0xED, 0x89",   ".DB 0xED, 0x8A",   ".DB 0xED, 0x8B",   ".DB 0xED, 0x8C",   ".DB 0xED, 0x8D",   ".DB 0xED, 0x8E",   ".DB 0xED, 0x8F",   /* 0x88 */
            ".DB 0xED, 0x90",   ".DB 0xED, 0x91",   ".DB 0xED, 0x92",   ".DB 0xED, 0x93",   ".DB 0xED, 0x94",   ".DB 0xED, 0x95",   ".DB 0xED, 0x96",   ".DB 0xED, 0x97",   /* 0x90 */
            ".DB 0xED, 0x98",   ".DB 0xED, 0x99",   ".DB 0xED, 0x9A",   ".DB 0xED, 0x9B",   ".DB 0xED, 0x9C",   ".DB 0xED, 0x9D",   ".DB 0xED, 0x9E",   ".DB 0xED, 0x9F",   /* 0x98 */
            "LDI",              "CPI",              "INI",              "OUTI",             ".DB 0xED, 0xA4",   ".DB 0xED, 0xA5",   ".DB 0xED, 0xA6",   ".DB 0xED, 0xA7",   /* 0xA0 */
            "LDD",              "CPD",              "IND",              "OUTD",             ".DB 0xED, 0xAC",   ".DB 0xED, 0xAD",   ".DB 0xED, 0xAE",   ".DB 0xED, 0xAF",   /* 0xA8 */
            "LDIR",             "CPIR",             "INIR",             "OTIR",             ".DB 0xED, 0xB4",   ".DB 0xED, 0xB5",   ".DB 0xED, 0xB6",   ".DB 0xED, 0xB7",   /* 0xB0 */
            "LDDR",             "CPDR",             "INDR",             "OTDR",             ".DB 0xED, 0xBC",   ".DB 0xED, 0xBD",   ".DB 0xED, 0xBE",   ".DB 0xED, 0xBF",   /* 0xB8 */
            ".DB 0xED, 0xC0",   ".DB 0xED, 0xC1",   ".DB 0xED, 0xC2",   ".DB 0xED, 0xC3",   ".DB 0xED, 0xC4",   ".DB 0xED, 0xC5",   ".DB 0xED, 0xC6",   ".DB 0xED, 0xC7",   /* 0xC0 */
            ".DB 0xED, 0xC8",   ".DB 0xED, 0xC9",   ".DB 0xED, 0xCA",   ".DB 0xED, 0xCB",   ".DB 0xED, 0xCC",   ".DB 0xED, 0xCD",   ".DB 0xED, 0xCE",   ".DB 0xED, 0xCF",   /* 0xC8 */
            ".DB 0xED, 0xD0",   ".DB 0xED, 0xD1",   ".DB 0xED, 0xD2",   ".DB 0xED, 0xD3",   ".DB 0xED, 0xD4",   ".DB 0xED, 0xD5",   ".DB 0xED, 0xD6",   ".DB 0xED, 0xD7",   /* 0xD0 */
            ".DB 0xED, 0xD8",   ".DB 0xED, 0xD9",   ".DB 0xED, 0xDA",   ".DB 0xED, 0xDB",   ".DB 0xED, 0xDC",   ".DB 0xED, 0xDD",   ".DB 0xED, 0xDE",   ".DB 0xED, 0xDF",   /* 0xD8 */
            ".DB 0xED, 0xE0",   ".DB 0xED, 0xE1",   ".DB 0xED, 0xE2",   ".DB 0xED, 0xE3",   ".DB 0xED, 0xE4",   ".DB 0xED, 0xE5",   ".DB 0xED, 0xE6",   ".DB 0xED, 0xE7",   /* 0xE0 */
            ".DB 0xED, 0xE8",   ".DB 0xED, 0xE9",   ".DB 0xED, 0xEA",   ".DB 0xED, 0xEB",   ".DB 0xED, 0xEC",   ".DB 0xED, 0xED",   ".DB 0xED, 0xEE",   ".DB 0xED, 0xEF",   /* 0xE8 */
            ".DB 0xED, 0xF0",   ".DB 0xED, 0xF1",   ".DB 0xED, 0xF2",   ".DB 0xED, 0xF3",   ".DB 0xED, 0xF4",   ".DB 0xED, 0xF5",   ".DB 0xED, 0xF6",   ".DB 0xED, 0xF7",   /* 0xF0 */
            ".DB 0xED, 0xF8",   ".DB 0xED, 0xF9",   ".DB 0xED, 0xFA",   ".DB 0xED, 0xFB",   ".DB 0xED, 0xFC",   ".DB 0xED, 0xFD",   ".DB 0xED, 0xFE",   ".DB 0xED, 0xFF",   /* 0xF8 */
        };

        static readonly int[] opcodeLength_ED = new int[]
        {
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 4, 2, 2, 2, 2, 2, 2, 2, 4, 2, 2, 2, 2,
            2, 2, 2, 4, 2, 2, 2, 2, 2, 2, 2, 4, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 4, 2, 2, 2, 2, 2, 2, 2, 4, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        };

        private string DisassembleOpcode(ushort address)
        {
            // TODO: make nicer?

            byte[] opcode = DisassembleGetOpcodeBytes(address);
            return string.Format("{0:X4} | {1} | {2}", address, DisassembleMakeByteString(opcode).PadRight(15), DisassembleMakeMnemonicString(opcode));
        }

        private byte[] DisassembleGetOpcodeBytes(ushort address)
        {
            byte[] opcode = new byte[5];
            for (int i = 0; i < opcode.Length; i++)
                opcode[i] = (address + i <= 0xFFFF ? memoryMapper.Read8((ushort)(address + i)) : (byte)0);
            return opcode;
        }

        private int DisassembleGetOpcodeLen(byte[] opcode)
        {
            // TODO: handle CB/DD/FD/etc

            switch (opcode[0])
            {
                case 0xCB: return 1;
                case 0xDD: return 1;
                case 0xED: return opcodeLength_ED[opcode[1]];
                case 0xFD: return 1;
                default: return opcodeLength_Main[opcode[0]];
            }
        }

        private string DisassembleMakeByteString(byte[] opcode)
        {
            return string.Join(" ", opcode.Select(x => string.Format("{0:X2}", x)).Take(DisassembleGetOpcodeLen(opcode)));
        }

        private string DisassembleMakeMnemonicString(byte[] opcode)
        {
            // TODO: handle CB/DD/FD/etc

            int len = DisassembleGetOpcodeLen(opcode);

            int start = 0;
            string[] mnemonics = opcodeMnemonic_Main;

            switch (opcode[0])
            {
                case 0xCB: start = 1; mnemonics = null; break;
                case 0xDD: start = 1; mnemonics = null; break;
                case 0xED: start = 1; mnemonics = opcodeMnemonic_ED; break;
                case 0xFD: start = 1; mnemonics = null; break;
            }

            if (mnemonics == null) return string.Empty;

            switch (len - start)
            {
                case 1: return mnemonics[opcode[start]];
                case 2: return string.Format(mnemonics[opcode[start]], opcode[start + 1]);
                case 3: return string.Format(mnemonics[opcode[start]], (opcode[start + 2] << 8 | opcode[start + 1]));
                default: return string.Empty;
            }
        }
    }
}
