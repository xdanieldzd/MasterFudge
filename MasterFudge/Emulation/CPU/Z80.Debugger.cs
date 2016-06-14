﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.CPU
{
    public partial class Z80
    {
        public bool DebugLogOpcodes { get; set; }

        // TODO: add undocumented ops if implemented in the core

        static readonly string[] opcodeMnemonic_Main = new string[]
        {
            "NOP",                  "LD BC, 0x{0:X4}",      "LD (BC), A",           "INC BC",               "INC B",                "DEC B",                "LD B, 0x{0:X2}",       "RLCA",                 /* 0x00 */
            "EX AF, AF'",           "ADD HL, BC",           "LD A, (BC)",           "DEC BC",               "INC C",                "DEC C",                "LD C, 0x{0:X2}",       "RRCA",                 /* 0x08 */
            "DJNZ 0x{0:X2}",        "LD DE, 0x{0:X4}",      "LD (DE), A",           "INC DE",               "INC D",                "DEC D",                "LD D, 0x{0:X2}",       "RLA",                  /* 0x10 */
            "JR 0x{0:X2}",          "ADD HL, DE",           "LD A, (DE)",           "DEC DE",               "INC E",                "DEC E",                "LD E, 0x{0:X2}",       "RRA",                  /* 0x18 */
            "JR NZ, 0x{0:X2}",      "LD HL, 0x{0:X4}",      "LD (0x{0:X4}), HL",    "INC HL",               "INC H",                "DEC H",                "LD H, 0x{0:X2}",       "DAA",                  /* 0x20 */
            "JR Z, 0x{0:X2}",       "ADD HL, HL",           "LD HL, (0x{0:X4})",    "DEC HL",               "INC L",                "DEC L",                "LD L, 0x{0:X2}",       "CPL",                  /* 0x28 */
            "JR NC, 0x{0:X2}",      "LD SP, 0x{0:X4}",      "LD (0x{0:X4}), A",     "INC SP",               "INC (HL)",             "DEC (HL)",             "LD (HL), 0x{0:X2}",    "SCF",                  /* 0x30 */
            "JR C, 0x{0:X2}",       "ADD HL, SP",           "LD A, (0x{0:X4})",     "DEC SP",               "INC A",                "DEC A",                "LD A, 0x{0:X2}",       "CCF",                  /* 0x38 */
            "LD B, B",              "LD B, C",              "LD B, D",              "LD B, E",              "LD B, H",              "LD B, L",              "LD B, (HL)",           "LD B, A",              /* 0x40 */
            "LD C, B",              "LD C, C",              "LD C, D",              "LD C, E",              "LD C, H",              "LD C, L",              "LD C, (HL)",           "LD C, A",              /* 0x48 */
            "LD D, B",              "LD D, C",              "LD D, D",              "LD D, E",              "LD D, H",              "LD D, L",              "LD D, (HL)",           "LD D, A",              /* 0x50 */
            "LD E, B",              "LD E, C",              "LD E, D",              "LD E, E",              "LD E, H",              "LD E, L",              "LD E, (HL)",           "LD E, A",              /* 0x58 */
            "LD H, B",              "LD H, C",              "LD H, D",              "LD H, E",              "LD H, H",              "LD H, L",              "LD H, (HL)",           "LD H, A",              /* 0x60 */
            "LD L, B",              "LD L, C",              "LD L, D",              "LD L, E",              "LD L, H",              "LD L, L",              "LD L, (HL)",           "LD L, A",              /* 0x68 */
            "LD (HL), B",           "LD (HL), C",           "LD (HL), D",           "LD (HL), E",           "LD (HL), H",           "LD (HL), L",           "HALT",                 "LD (HL), A",           /* 0x70 */
            "LD A, B",              "LD A, C",              "LD A, D",              "LD A, E",              "LD A, H",              "LD A, L",              "LD A, (HL)",           "LD A, A",              /* 0x78 */
            "ADD A, B",             "ADD A, C",             "ADD A, D",             "ADD A, E",             "ADD A, H",             "ADD A, L",             "ADD A, (HL)",          "ADD A, A",             /* 0x80 */
            "ADC A, B",             "ADC A, C",             "ADC A, D",             "ADC A, E",             "ADC A, H",             "ADC A, L",             "ADC A, (HL)",          "ADC A, A",             /* 0x88 */
            "SUB B",                "SUB C",                "SUB D",                "SUB E",                "SUB H",                "SUB L",                "SUB (HL)",             "SUB A",                /* 0x90 */
            "SBC B",                "SBC C",                "SBC D",                "SBC E",                "SBC H",                "SBC L",                "SBC (HL)",             "SBC A",                /* 0x98 */
            "AND B",                "AND C",                "AND D",                "AND E",                "AND H",                "AND L",                "AND (HL)",             "AND A",                /* 0xA0 */
            "XOR B",                "XOR C",                "XOR D",                "XOR E",                "XOR H",                "XOR L",                "XOR (HL)",             "XOR A",                /* 0xA8 */
            "OR B",                 "OR C",                 "OR D",                 "OR E",                 "OR H",                 "OR L",                 "OR (HL)",              "OR A",                 /* 0xA0 */
            "CP B",                 "CP C",                 "CP D",                 "CP E",                 "CP H",                 "CP L",                 "CP (HL)",              "CP A",                 /* 0xB8 */
            "RET NZ",               "POP BC",               "JP NZ, 0x{0:X4}",      "JP 0x{0:X4}",          "CALL NZ, 0x{0:X4}",    "PUSH BC",              "ADD A, 0x{0:X2}",      "RST 00",               /* 0xC0 */
            "RET Z",                "RET",                  "JP Z, 0x{0:X4}",       string.Empty,           "CALL Z, 0x{0:X4}",     "CALL 0x{0:X4}",        "ADC A, 0x{0:X2}",      "RST 08",               /* 0xC8 */
            "RET NC",               "POP DE",               "JP NC, 0x{0:X4}",      "OUT 0x{0:X2}, A",      "CALL NC, 0x{0:X4}",    "PUSH DE",              "SUB 0x{0:X2}",         "RST 10",               /* 0xD0 */
            "RET C",                "EXX",                  "JP C, 0x{0:X4}",       "IN A, 0x{0:X2}",       "CALL C, 0x{0:X4}",     string.Empty,           "SBC 0x{0:X2}",         "RST 18",               /* 0xD8 */
            "RET PO",               "POP HL",               "JP PO, 0x{0:X4}",      "EX (SP), HL",          "CALL PO, 0x{0:X4}",    "PUSH HL",              "AND 0x{0:X2}",         "RST 20",               /* 0xE0 */
            "RET PE",               "JP (HL)",              "JP PE, 0x{0:X4}",      "EX DE, HL",            "CALL PE, 0x{0:X4}",    string.Empty,           "XOR 0x{0:X2}",         "RST 28",               /* 0xE8 */
            "RET P",                "POP AF",               "JP P, 0x{0:X4}",       "DI",                   "CALL P, 0x{0:X4}",     "PUSH AF",              "OR 0x{0:X2}",          "RST 30",               /* 0xF0 */
            "RET M",                "LD SP, HL",            "JP M, 0x{0:X4}",       "EI",                   "CALL M, 0x{0:X4}",     string.Empty,           "CP 0x{0:X2}",          "RST 38"                /* 0xF0 */
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
            ".DB 0xED, 0x00",       ".DB 0xED, 0x01",       ".DB 0xED, 0x02",       ".DB 0xED, 0x03",       ".DB 0xED, 0x04",       ".DB 0xED, 0x05",       ".DB 0xED, 0x06",       ".DB 0xED, 0x07",       /* 0x00 */
            ".DB 0xED, 0x08",       ".DB 0xED, 0x09",       ".DB 0xED, 0x0A",       ".DB 0xED, 0x0B",       ".DB 0xED, 0x0C",       ".DB 0xED, 0x0D",       ".DB 0xED, 0x0E",       ".DB 0xED, 0x0F",       /* 0x08 */
            ".DB 0xED, 0x10",       ".DB 0xED, 0x11",       ".DB 0xED, 0x12",       ".DB 0xED, 0x13",       ".DB 0xED, 0x14",       ".DB 0xED, 0x15",       ".DB 0xED, 0x16",       ".DB 0xED, 0x17",       /* 0x10 */
            ".DB 0xED, 0x18",       ".DB 0xED, 0x19",       ".DB 0xED, 0x1A",       ".DB 0xED, 0x1B",       ".DB 0xED, 0x1C",       ".DB 0xED, 0x1D",       ".DB 0xED, 0x1E",       ".DB 0xED, 0x1F",       /* 0x18 */
            ".DB 0xED, 0x20",       ".DB 0xED, 0x21",       ".DB 0xED, 0x22",       ".DB 0xED, 0x23",       ".DB 0xED, 0x24",       ".DB 0xED, 0x25",       ".DB 0xED, 0x26",       ".DB 0xED, 0x27",       /* 0x20 */
            ".DB 0xED, 0x28",       ".DB 0xED, 0x29",       ".DB 0xED, 0x2A",       ".DB 0xED, 0x2B",       ".DB 0xED, 0x2C",       ".DB 0xED, 0x2D",       ".DB 0xED, 0x2E",       ".DB 0xED, 0x2F",       /* 0x28 */
            ".DB 0xED, 0x30",       ".DB 0xED, 0x31",       ".DB 0xED, 0x32",       ".DB 0xED, 0x33",       ".DB 0xED, 0x34",       ".DB 0xED, 0x35",       ".DB 0xED, 0x36",       ".DB 0xED, 0x37",       /* 0x30 */
            ".DB 0xED, 0x38",       ".DB 0xED, 0x39",       ".DB 0xED, 0x3A",       ".DB 0xED, 0x3B",       ".DB 0xED, 0x3C",       ".DB 0xED, 0x3D",       ".DB 0xED, 0x3E",       ".DB 0xED, 0x3F",       /* 0x38 */
            "IN B, (C)",            "OUT (C), B",           "SBC HL, BC",           "LD (0x{0:X4}), BC",    "NEG",                  "RETN",                 "IM 0",                 "LD I, A",              /* 0x40 */
            "IN C, (C)",            "OUT (C), C",           "ADC HL, BC",           "LD BC, (0x{0:X4})",    ".DB 0xED, 0x4C",       "RETI",                 ".DB 0xED, 0x4E",       "LD R, A",              /* 0x48 */
            "IN D, (C)",            "OUT (C), D",           "SBC HL, DE",           "LD (0x{0:X4}), DE",    ".DB 0xED, 0x54",       ".DB 0xED, 0x55",       "IM 1",                 "LD A, I",              /* 0x50 */
            "IN E, (C)",            "OUT (C), E",           "ADC HL, DE",           "LD DE, (0x{0:X4})",    ".DB 0xED, 0x5C",       ".DB 0xED, 0x5D",       "IM 2",                 "LD A, R",              /* 0x58 */
            "IN H, (C)",            "OUT (C), H",           "SBC HL, HL",           ".DB 0xED, 0x63",       ".DB 0xED, 0x64",       ".DB 0xED, 0x65",       ".DB 0xED, 0x66",       "RRD",                  /* 0x60 */
            "IN L, (C)",            "OUT (C), L",           "ADC HL, HL",           ".DB 0xED, 0x6B",       ".DB 0xED, 0x6C",       ".DB 0xED, 0x6D",       ".DB 0xED, 0x6E",       "RLD",                  /* 0x68 */
            ".DB 0xED, 0x70",       ".DB 0xED, 0x71",       "SBC HL, SP",           "LD (0x{0:X4}), SP",    ".DB 0xED, 0x74",       ".DB 0xED, 0x75",       ".DB 0xED, 0x76",       ".DB 0xED, 0x77",       /* 0x70 */
            "IN A, (C)",            "OUT (C), A",           "ADC HL, SP",           "LD SP, (0x{0:X4})",    ".DB 0xED, 0x7C",       ".DB 0xED, 0x7D",       ".DB 0xED, 0x7E",       ".DB 0xED, 0x7F",       /* 0x78 */
            ".DB 0xED, 0x80",       ".DB 0xED, 0x81",       ".DB 0xED, 0x82",       ".DB 0xED, 0x83",       ".DB 0xED, 0x84",       ".DB 0xED, 0x85",       ".DB 0xED, 0x86",       ".DB 0xED, 0x87",       /* 0x80 */
            ".DB 0xED, 0x88",       ".DB 0xED, 0x89",       ".DB 0xED, 0x8A",       ".DB 0xED, 0x8B",       ".DB 0xED, 0x8C",       ".DB 0xED, 0x8D",       ".DB 0xED, 0x8E",       ".DB 0xED, 0x8F",       /* 0x88 */
            ".DB 0xED, 0x90",       ".DB 0xED, 0x91",       ".DB 0xED, 0x92",       ".DB 0xED, 0x93",       ".DB 0xED, 0x94",       ".DB 0xED, 0x95",       ".DB 0xED, 0x96",       ".DB 0xED, 0x97",       /* 0x90 */
            ".DB 0xED, 0x98",       ".DB 0xED, 0x99",       ".DB 0xED, 0x9A",       ".DB 0xED, 0x9B",       ".DB 0xED, 0x9C",       ".DB 0xED, 0x9D",       ".DB 0xED, 0x9E",       ".DB 0xED, 0x9F",       /* 0x98 */
            "LDI",                  "CPI",                  "INI",                  "OUTI",                 ".DB 0xED, 0xA4",       ".DB 0xED, 0xA5",       ".DB 0xED, 0xA6",       ".DB 0xED, 0xA7",       /* 0xA0 */
            "LDD",                  "CPD",                  "IND",                  "OUTD",                 ".DB 0xED, 0xAC",       ".DB 0xED, 0xAD",       ".DB 0xED, 0xAE",       ".DB 0xED, 0xAF",       /* 0xA8 */
            "LDIR",                 "CPIR",                 "INIR",                 "OTIR",                 ".DB 0xED, 0xB4",       ".DB 0xED, 0xB5",       ".DB 0xED, 0xB6",       ".DB 0xED, 0xB7",       /* 0xB0 */
            "LDDR",                 "CPDR",                 "INDR",                 "OTDR",                 ".DB 0xED, 0xBC",       ".DB 0xED, 0xBD",       ".DB 0xED, 0xBE",       ".DB 0xED, 0xBF",       /* 0xB8 */
            ".DB 0xED, 0xC0",       ".DB 0xED, 0xC1",       ".DB 0xED, 0xC2",       ".DB 0xED, 0xC3",       ".DB 0xED, 0xC4",       ".DB 0xED, 0xC5",       ".DB 0xED, 0xC6",       ".DB 0xED, 0xC7",       /* 0xC0 */
            ".DB 0xED, 0xC8",       ".DB 0xED, 0xC9",       ".DB 0xED, 0xCA",       ".DB 0xED, 0xCB",       ".DB 0xED, 0xCC",       ".DB 0xED, 0xCD",       ".DB 0xED, 0xCE",       ".DB 0xED, 0xCF",       /* 0xC8 */
            ".DB 0xED, 0xD0",       ".DB 0xED, 0xD1",       ".DB 0xED, 0xD2",       ".DB 0xED, 0xD3",       ".DB 0xED, 0xD4",       ".DB 0xED, 0xD5",       ".DB 0xED, 0xD6",       ".DB 0xED, 0xD7",       /* 0xD0 */
            ".DB 0xED, 0xD8",       ".DB 0xED, 0xD9",       ".DB 0xED, 0xDA",       ".DB 0xED, 0xDB",       ".DB 0xED, 0xDC",       ".DB 0xED, 0xDD",       ".DB 0xED, 0xDE",       ".DB 0xED, 0xDF",       /* 0xD8 */
            ".DB 0xED, 0xE0",       ".DB 0xED, 0xE1",       ".DB 0xED, 0xE2",       ".DB 0xED, 0xE3",       ".DB 0xED, 0xE4",       ".DB 0xED, 0xE5",       ".DB 0xED, 0xE6",       ".DB 0xED, 0xE7",       /* 0xE0 */
            ".DB 0xED, 0xE8",       ".DB 0xED, 0xE9",       ".DB 0xED, 0xEA",       ".DB 0xED, 0xEB",       ".DB 0xED, 0xEC",       ".DB 0xED, 0xED",       ".DB 0xED, 0xEE",       ".DB 0xED, 0xEF",       /* 0xE8 */
            ".DB 0xED, 0xF0",       ".DB 0xED, 0xF1",       ".DB 0xED, 0xF2",       ".DB 0xED, 0xF3",       ".DB 0xED, 0xF4",       ".DB 0xED, 0xF5",       ".DB 0xED, 0xF6",       ".DB 0xED, 0xF7",       /* 0xF0 */
            ".DB 0xED, 0xF8",       ".DB 0xED, 0xF9",       ".DB 0xED, 0xFA",       ".DB 0xED, 0xFB",       ".DB 0xED, 0xFC",       ".DB 0xED, 0xFD",       ".DB 0xED, 0xFE",       ".DB 0xED, 0xFF"        /* 0xF8 */
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

        static readonly string[] opcodeMnemonic_CB = new string[]
        {
            "RLC B",                "RLC C",                "RLC D",                "RLC E",                "RLC H",                "RLC L",                "RLC (HL)",             "RLC A",                /* 0x00 */
            "RRC B",                "RRC C",                "RRC D",                "RRC E",                "RRC H",                "RRC L",                "RRC (HL)",             "RRC A",                /* 0x08 */
            "RL B",                 "RL C",                 "RL D",                 "RL E",                 "RL H",                 "RL L",                 "RL (HL)",              "RL A",                 /* 0x10 */
            "RR B",                 "RR C",                 "RR D",                 "RR E",                 "RR H",                 "RR L",                 "RR (HL)",              "RR A",                 /* 0x18 */
            "SLA B",                "SLA C",                "SLA D",                "SLA E",                "SLA H",                "SLA L",                "SLA (HL)",             "SLA A",                /* 0x20 */
            "SRA B",                "SRA C",                "SRA D",                "SRA E",                "SRA H",                "SRA L",                "SRA (HL)",             "SRA A",                /* 0x28 */
            "SLL B",                "SLL C",                "SLL D",                "SLL E",                "SLL H",                "SLL L",                "SLL (HL)",             "SLL A",                /* 0x30 */
            "SRL B",                "SRL C",                "SRL D",                "SRL E",                "SRL H",                "SRL L",                "SRL (HL)",             "SRL A",                /* 0x38 */
            "BIT 0, B",             "BIT 0, C",             "BIT 0, D",             "BIT 0, E",             "BIT 0, H",             "BIT 0, L",             "BIT 0, (HL)",          "BIT 0, A",             /* 0x40 */
            "BIT 1, B",             "BIT 1, C",             "BIT 1, D",             "BIT 1, E",             "BIT 1, H",             "BIT 1, L",             "BIT 1, (HL)",          "BIT 1, A",             /* 0x48 */
            "BIT 2, B",             "BIT 2, C",             "BIT 2, D",             "BIT 2, E",             "BIT 2, H",             "BIT 2, L",             "BIT 2, (HL)",          "BIT 2, A",             /* 0x50 */
            "BIT 3, B",             "BIT 3, C",             "BIT 3, D",             "BIT 3, E",             "BIT 3, H",             "BIT 3, L",             "BIT 3, (HL)",          "BIT 3, A",             /* 0x58 */
            "BIT 4, B",             "BIT 4, C",             "BIT 4, D",             "BIT 4, E",             "BIT 4, H",             "BIT 4, L",             "BIT 4, (HL)",          "BIT 4, A",             /* 0x60 */
            "BIT 5, B",             "BIT 5, C",             "BIT 5, D",             "BIT 5, E",             "BIT 5, H",             "BIT 5, L",             "BIT 5, (HL)",          "BIT 5, A",             /* 0x68 */
            "BIT 6, B",             "BIT 6, C",             "BIT 6, D",             "BIT 6, E",             "BIT 6, H",             "BIT 6, L",             "BIT 6, (HL)",          "BIT 6, A",             /* 0x70 */
            "BIT 7, B",             "BIT 7, C",             "BIT 7, D",             "BIT 7, E",             "BIT 7, H",             "BIT 7, L",             "BIT 7, (HL)",          "BIT 7, A",             /* 0x78 */
            "RES 0, B",             "RES 0, C",             "RES 0, D",             "RES 0, E",             "RES 0, H",             "RES 0, L",             "RES 0, (HL)",          "RES 0, A",             /* 0x80 */
            "RES 1, B",             "RES 1, C",             "RES 1, D",             "RES 1, E",             "RES 1, H",             "RES 1, L",             "RES 1, (HL)",          "RES 1, A",             /* 0x88 */
            "RES 2, B",             "RES 2, C",             "RES 2, D",             "RES 2, E",             "RES 2, H",             "RES 2, L",             "RES 2, (HL)",          "RES 2, A",             /* 0x90 */
            "RES 3, B",             "RES 3, C",             "RES 3, D",             "RES 3, E",             "RES 3, H",             "RES 3, L",             "RES 3, (HL)",          "RES 3, A",             /* 0x98 */
            "RES 4, B",             "RES 4, C",             "RES 4, D",             "RES 4, E",             "RES 4, H",             "RES 4, L",             "RES 4, (HL)",          "RES 4, A",             /* 0xA0 */
            "RES 5, B",             "RES 5, C",             "RES 5, D",             "RES 5, E",             "RES 5, H",             "RES 5, L",             "RES 5, (HL)",          "RES 5, A",             /* 0xA8 */
            "RES 6, B",             "RES 6, C",             "RES 6, D",             "RES 6, E",             "RES 6, H",             "RES 6, L",             "RES 6, (HL)",          "RES 6, A",             /* 0xB0 */
            "RES 7, B",             "RES 7, C",             "RES 7, D",             "RES 7, E",             "RES 7, H",             "RES 7, L",             "RES 7, (HL)",          "RES 7, A",             /* 0xB8 */
            "SET 0, B",             "SET 0, C",             "SET 0, D",             "SET 0, E",             "SET 0, H",             "SET 0, L",             "SET 0, (HL)",          "SET 0, A",             /* 0xC0 */
            "SET 1, B",             "SET 1, C",             "SET 1, D",             "SET 1, E",             "SET 1, H",             "SET 1, L",             "SET 1, (HL)",          "SET 1, A",             /* 0xC8 */
            "SET 2, B",             "SET 2, C",             "SET 2, D",             "SET 2, E",             "SET 2, H",             "SET 2, L",             "SET 2, (HL)",          "SET 2, A",             /* 0xD0 */
            "SET 3, B",             "SET 3, C",             "SET 3, D",             "SET 3, E",             "SET 3, H",             "SET 3, L",             "SET 3, (HL)",          "SET 3, A",             /* 0xD8 */
            "SET 4, B",             "SET 4, C",             "SET 4, D",             "SET 4, E",             "SET 4, H",             "SET 4, L",             "SET 4, (HL)",          "SET 4, A",             /* 0xE0 */
            "SET 5, B",             "SET 5, C",             "SET 5, D",             "SET 5, E",             "SET 5, H",             "SET 5, L",             "SET 5, (HL)",          "SET 5, A",             /* 0xE8 */
            "SET 6, B",             "SET 6, C",             "SET 6, D",             "SET 6, E",             "SET 6, H",             "SET 6, L",             "SET 6, (HL)",          "SET 6, A",             /* 0xF0 */
            "SET 7, B",             "SET 7, C",             "SET 7, D",             "SET 7, E",             "SET 7, H",             "SET 7, L",             "SET 7, (HL)",          "SET 7, A"              /* 0xF8 */
        };

        static readonly int[] opcodeLength_CB = new int[]
        {
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        };

        static readonly string[] opcodeMnemonic_DD = new string[]
        {
            ".DB 0xDD, 0x00",       ".DB 0xDD, 0x01",       ".DB 0xDD, 0x02",       ".DB 0xDD, 0x03",       ".DB 0xDD, 0x04",       ".DB 0xDD, 0x05",       ".DB 0xDD, 0x06",       ".DB 0xDD, 0x07",       /* 0x00 */
            ".DB 0xDD, 0x08",       "ADD IX, BC",           ".DB 0xDD, 0x0A",       ".DB 0xDD, 0x0B",       ".DB 0xDD, 0x0C",       ".DB 0xDD, 0x0D",       ".DB 0xDD, 0x0E",       ".DB 0xDD, 0x0F",       /* 0x08 */
            ".DB 0xDD, 0x10",       ".DB 0xDD, 0x11",       ".DB 0xDD, 0x12",       ".DB 0xDD, 0x13",       ".DB 0xDD, 0x14",       ".DB 0xDD, 0x15",       ".DB 0xDD, 0x16",       ".DB 0xDD, 0x17",       /* 0x10 */
            ".DB 0xDD, 0x18",       "ADD IX, DE",           ".DB 0xDD, 0x1A",       ".DB 0xDD, 0x1B",       ".DB 0xDD, 0x1C",       ".DB 0xDD, 0x1D",       ".DB 0xDD, 0x1E",       ".DB 0xDD, 0x1F",       /* 0x18 */
            ".DB 0xDD, 0x20",       "LD IX, 0x{0:X4}",      "LD (0x{0:X4}), IX",    "INC IX",               "INC IXH",              "DEC IXH",              "LD IXH, 0x{0:X2}",     ".DB 0xDD, 0x27",       /* 0x20 */
            ".DB 0xDD, 0x28",       "ADD IX, IX",           "LD IX, (0x{0:X4})",    "DEC IX",               "INC IXL",              "DEC IXL",              "LD IXL, 0x{0:X2}",     ".DB 0xDD, 0x2F",       /* 0x28 */
            ".DB 0xDD, 0x30",       ".DB 0xDD, 0x31",       ".DB 0xDD, 0x32",       ".DB 0xDD, 0x33",       "INC (IX+0x{0:X2})",    "DEC (IX+0x{0:X2})",    "LD (IX+0x{0:X2}), 0x{0:X2}",   ".DB 0xDD, 0x37",   /* 0x30 */
            ".DB 0xDD, 0x38",       "ADD IX, SP",           ".DB 0xDD, 0x3A",       ".DB 0xDD, 0x3B",       ".DB 0xDD, 0x3C",       ".DB 0xDD, 0x3D",       ".DB 0xDD, 0x3E",       ".DB 0xDD, 0x3F",       /* 0x38 */
            ".DB 0xDD, 0x40",       ".DB 0xDD, 0x41",       ".DB 0xDD, 0x42",       ".DB 0xDD, 0x43",       ".DB 0xDD, 0x44",       ".DB 0xDD, 0x45",       "LD B, (IX+0x{0:X2})",  ".DB 0xDD, 0x47",       /* 0x40 */
            ".DB 0xDD, 0x48",       ".DB 0xDD, 0x49",       ".DB 0xDD, 0x4A",       ".DB 0xDD, 0x4B",       ".DB 0xDD, 0x4C",       ".DB 0xDD, 0x4D",       "LD C, (IX+0x{0:X2})",  ".DB 0xDD, 0x4F",       /* 0x48 */
            ".DB 0xDD, 0x50",       ".DB 0xDD, 0x51",       ".DB 0xDD, 0x52",       ".DB 0xDD, 0x53",       ".DB 0xDD, 0x54",       ".DB 0xDD, 0x55",       "LD D, (IX+0x{0:X2})",  ".DB 0xDD, 0x57",       /* 0x50 */
            ".DB 0xDD, 0x58",       ".DB 0xDD, 0x59",       ".DB 0xDD, 0x5A",       ".DB 0xDD, 0x5B",       ".DB 0xDD, 0x5C",       ".DB 0xDD, 0x5D",       "LD E, (IX+0x{0:X2})",  ".DB 0xDD, 0x5F",       /* 0x58 */
            ".DB 0xDD, 0x60",       ".DB 0xDD, 0x61",       ".DB 0xDD, 0x62",       ".DB 0xDD, 0x63",       ".DB 0xDD, 0x64",       ".DB 0xDD, 0x65",       "LD H, (IX+0x{0:X2})",  ".DB 0xDD, 0x67",       /* 0x60 */
            ".DB 0xDD, 0x68",       ".DB 0xDD, 0x69",       ".DB 0xDD, 0x6A",       ".DB 0xDD, 0x6B",       ".DB 0xDD, 0x6C",       ".DB 0xDD, 0x6D",       "LD L, (IX+0x{0:X2})",  ".DB 0xDD, 0x6F",       /* 0x68 */
            "LD (IX+0x{0:X2}), B",  "LD (IX+0x{0:X2}), C",  "LD (IX+0x{0:X2}), D",  "LD (IX+0x{0:X2}), E",  "LD (IX+0x{0:X2}), H",  "LD (IX+0x{0:X2}), L",  ".DB 0xDD, 0x76",       "LD (IX+0x{0:X2}), A",  /* 0x70 */
            ".DB 0xDD, 0x78",       ".DB 0xDD, 0x79",       ".DB 0xDD, 0x7A",       ".DB 0xDD, 0x7B",       ".DB 0xDD, 0x7C",       ".DB 0xDD, 0x7D",       "LD A, (IX+0x{0:X2})",  ".DB 0xDD, 0x7F",       /* 0x78 */
            ".DB 0xDD, 0x80",       ".DB 0xDD, 0x81",       ".DB 0xDD, 0x82",       ".DB 0xDD, 0x83",       ".DB 0xDD, 0x84",       ".DB 0xDD, 0x85",       "ADD A, (IX+0x{0:X2})", ".DB 0xDD, 0x87",       /* 0x80 */
            ".DB 0xDD, 0x88",       ".DB 0xDD, 0x89",       ".DB 0xDD, 0x8A",       ".DB 0xDD, 0x8B",       ".DB 0xDD, 0x8C",       ".DB 0xDD, 0x8D",       "ADC A, (IX+0x{0:X2})", ".DB 0xDD, 0x8F",       /* 0x88 */
            ".DB 0xDD, 0x90",       ".DB 0xDD, 0x91",       ".DB 0xDD, 0x92",       ".DB 0xDD, 0x93",       ".DB 0xDD, 0x94",       ".DB 0xDD, 0x95",       "SUB (IX+0x{0:X2})",    ".DB 0xDD, 0x97",       /* 0x90 */
            ".DB 0xDD, 0x98",       ".DB 0xDD, 0x99",       ".DB 0xDD, 0x9A",       ".DB 0xDD, 0x9B",       ".DB 0xDD, 0x9C",       ".DB 0xDD, 0x9D",       "SBC (IX+0x{0:X2})",    ".DB 0xDD, 0x9F",       /* 0x98 */
            ".DB 0xDD, 0xA0",       ".DB 0xDD, 0xA1",       ".DB 0xDD, 0xA2",       ".DB 0xDD, 0xA3",       ".DB 0xDD, 0xA4",       ".DB 0xDD, 0xA5",       "AND (IX+0x{0:X2})",    ".DB 0xDD, 0xA7",       /* 0xA0 */
            ".DB 0xDD, 0xA8",       ".DB 0xDD, 0xA9",       ".DB 0xDD, 0xAA",       ".DB 0xDD, 0xAB",       ".DB 0xDD, 0xAC",       ".DB 0xDD, 0xAD",       "XOR (IX+0x{0:X2})",    ".DB 0xDD, 0xAF",       /* 0xA8 */
            ".DB 0xDD, 0xB0",       ".DB 0xDD, 0xB1",       ".DB 0xDD, 0xB2",       ".DB 0xDD, 0xB3",       ".DB 0xDD, 0xB4",       ".DB 0xDD, 0xB5",       "OR (IX+0x{0:X2})",     ".DB 0xDD, 0xB7",       /* 0xB0 */
            ".DB 0xDD, 0xB8",       ".DB 0xDD, 0xB9",       ".DB 0xDD, 0xBA",       ".DB 0xDD, 0xBB",       ".DB 0xDD, 0xBC",       ".DB 0xDD, 0xBD",       "CP (IX+0x{0:X2})",     ".DB 0xDD, 0xBF",       /* 0xB8 */
            ".DB 0xDD, 0xC0",       ".DB 0xDD, 0xC1",       ".DB 0xDD, 0xC2",       ".DB 0xDD, 0xC3",       ".DB 0xDD, 0xC4",       ".DB 0xDD, 0xC5",       ".DB 0xDD, 0xC6",       ".DB 0xDD, 0xC7",       /* 0xC0 */
            ".DB 0xDD, 0xC8",       ".DB 0xDD, 0xC9",       ".DB 0xDD, 0xCA",       string.Empty,           ".DB 0xDD, 0xCC",       ".DB 0xDD, 0xCD",       ".DB 0xDD, 0xCE",       ".DB 0xDD, 0xCF",       /* 0xC8 */
            ".DB 0xDD, 0xD0",       ".DB 0xDD, 0xD1",       ".DB 0xDD, 0xD2",       ".DB 0xDD, 0xD3",       ".DB 0xDD, 0xD4",       ".DB 0xDD, 0xD5",       ".DB 0xDD, 0xD6",       ".DB 0xDD, 0xD7",       /* 0xD0 */
            ".DB 0xDD, 0xD8",       ".DB 0xDD, 0xD9",       ".DB 0xDD, 0xDA",       ".DB 0xDD, 0xDB",       ".DB 0xDD, 0xDC",       ".DB 0xDD, 0xDD",       ".DB 0xDD, 0xDE",       ".DB 0xDD, 0xDF",       /* 0xD8 */
            ".DB 0xED, 0xE0",       "POP IX",               ".DB 0xED, 0xE2",       "EX (SP), IX",          ".DB 0xED, 0xE4",       "PUSH IX",              ".DB 0xED, 0xE6",       ".DB 0xED, 0xE7",       /* 0xE0 */
            ".DB 0xED, 0xE8",       "JP (IX)",              ".DB 0xED, 0xEA",       ".DB 0xED, 0xEB",       ".DB 0xED, 0xEC",       ".DB 0xED, 0xED",       ".DB 0xED, 0xEE",       ".DB 0xED, 0xEF",       /* 0xE8 */
            ".DB 0xFD, 0xF0",       ".DB 0xFD, 0xF1",       ".DB 0xFD, 0xF2",       ".DB 0xFD, 0xF3",       ".DB 0xFD, 0xF4",       ".DB 0xFD, 0xF5",       ".DB 0xFD, 0xF6",       ".DB 0xFD, 0xF7",       /* 0xF0 */
            ".DB 0xFD, 0xF8",       "LD SP, IX",            ".DB 0xFD, 0xFA",       ".DB 0xFD, 0xFB",       ".DB 0xFD, 0xFC",       ".DB 0xFD, 0xFD",       ".DB 0xFD, 0xFE",       ".DB 0xFD, 0xFF"        /* 0xF8 */
        };

        static readonly string[] opcodeMnemonic_FD = new string[]
        {
            ".DB 0xFD, 0x00",       ".DB 0xFD, 0x01",       ".DB 0xFD, 0x02",       ".DB 0xFD, 0x03",       ".DB 0xFD, 0x04",       ".DB 0xFD, 0x05",       ".DB 0xFD, 0x06",       ".DB 0xFD, 0x07",       /* 0x00 */
            ".DB 0xFD, 0x08",       "ADD IY, BC",           ".DB 0xFD, 0x0A",       ".DB 0xFD, 0x0B",       ".DB 0xFD, 0x0C",       ".DB 0xFD, 0x0D",       ".DB 0xFD, 0x0E",       ".DB 0xFD, 0x0F",       /* 0x08 */
            ".DB 0xFD, 0x10",       ".DB 0xFD, 0x11",       ".DB 0xFD, 0x12",       ".DB 0xFD, 0x13",       ".DB 0xFD, 0x14",       ".DB 0xFD, 0x15",       ".DB 0xFD, 0x16",       ".DB 0xFD, 0x17",       /* 0x10 */
            ".DB 0xFD, 0x18",       "ADD IY, DE",           ".DB 0xFD, 0x1A",       ".DB 0xFD, 0x1B",       ".DB 0xFD, 0x1C",       ".DB 0xFD, 0x1D",       ".DB 0xFD, 0x1E",       ".DB 0xFD, 0x1F",       /* 0x18 */
            ".DB 0xFD, 0x20",       "LD IY, 0x{0:X4}",      "LD (0x{0:X4}), IY",    "INC IY",               ".DB 0xFD, 0x24",       ".DB 0xFD, 0x25",       ".DB 0xFD, 0x26",       ".DB 0xFD, 0x27",       /* 0x20 */
            ".DB 0xFD, 0x28",       "ADD IY, IY",           "LD IY, (0x{0:X4})",    "DEC IY",               ".DB 0xFD, 0x2C",       ".DB 0xFD, 0x2D",       ".DB 0xFD, 0x2E",       ".DB 0xFD, 0x2F",       /* 0x28 */
            ".DB 0xFD, 0x30",       ".DB 0xFD, 0x31",       ".DB 0xFD, 0x32",       ".DB 0xFD, 0x33",       "INC (IY+0x{0:X2})",    "DEC (IY+0x{0:X2})",    "LD (IY+0x{0:X2}), 0x{0:X2}",   ".DB 0xFD, 0x37",   /* 0x30 */
            ".DB 0xFD, 0x38",       "ADD IY, SP",           ".DB 0xFD, 0x3A",       ".DB 0xFD, 0x3B",       ".DB 0xFD, 0x3C",       ".DB 0xFD, 0x3D",       ".DB 0xFD, 0x3E",       ".DB 0xFD, 0x3F",       /* 0x38 */
            ".DB 0xFD, 0x40",       ".DB 0xFD, 0x41",       ".DB 0xFD, 0x42",       ".DB 0xFD, 0x43",       ".DB 0xFD, 0x44",       ".DB 0xFD, 0x45",       "LD B, (IY+0x{0:X2})",  ".DB 0xFD, 0x47",       /* 0x40 */
            ".DB 0xFD, 0x48",       ".DB 0xFD, 0x49",       ".DB 0xFD, 0x4A",       ".DB 0xFD, 0x4B",       ".DB 0xFD, 0x4C",       ".DB 0xFD, 0x4D",       "LD C, (IY+0x{0:X2})",  ".DB 0xFD, 0x4F",       /* 0x48 */
            ".DB 0xFD, 0x50",       ".DB 0xFD, 0x51",       ".DB 0xFD, 0x52",       ".DB 0xFD, 0x53",       ".DB 0xFD, 0x54",       ".DB 0xFD, 0x55",       "LD D, (IY+0x{0:X2})",  ".DB 0xFD, 0x57",       /* 0x50 */
            ".DB 0xFD, 0x58",       ".DB 0xFD, 0x59",       ".DB 0xFD, 0x5A",       ".DB 0xFD, 0x5B",       ".DB 0xFD, 0x5C",       ".DB 0xFD, 0x5D",       "LD E, (IY+0x{0:X2})",  ".DB 0xFD, 0x5F",       /* 0x58 */
            ".DB 0xFD, 0x60",       ".DB 0xFD, 0x61",       ".DB 0xFD, 0x62",       ".DB 0xFD, 0x63",       ".DB 0xFD, 0x64",       ".DB 0xFD, 0x65",       "LD H, (IY+0x{0:X2})",  ".DB 0xFD, 0x67",       /* 0x60 */
            ".DB 0xFD, 0x68",       ".DB 0xFD, 0x69",       ".DB 0xFD, 0x6A",       ".DB 0xFD, 0x6B",       ".DB 0xFD, 0x6C",       ".DB 0xFD, 0x6D",       "LD L, (IY+0x{0:X2})",  ".DB 0xFD, 0x6F",       /* 0x68 */
            "LD (IY+0x{0:X2}), B",  "LD (IY+0x{0:X2}), C",  "LD (IY+0x{0:X2}), D",  "LD (IY+0x{0:X2}), E",  "LD (IY+0x{0:X2}), H",  "LD (IY+0x{0:X2}), L",  ".DB 0xFD, 0x76",       "LD (IY+0x{0:X2}), A",  /* 0x70 */
            ".DB 0xFD, 0x78",       ".DB 0xFD, 0x79",       ".DB 0xFD, 0x7A",       ".DB 0xFD, 0x7B",       ".DB 0xFD, 0x7C",       ".DB 0xFD, 0x7D",       "LD A, (IY+0x{0:X2})",  ".DB 0xFD, 0x7F",       /* 0x78 */
            ".DB 0xFD, 0x80",       ".DB 0xFD, 0x81",       ".DB 0xFD, 0x82",       ".DB 0xFD, 0x83",       ".DB 0xFD, 0x84",       ".DB 0xFD, 0x85",       "ADD A, (IY+0x{0:X2})", ".DB 0xFD, 0x87",       /* 0x80 */
            ".DB 0xFD, 0x88",       ".DB 0xFD, 0x89",       ".DB 0xFD, 0x8A",       ".DB 0xFD, 0x8B",       ".DB 0xFD, 0x8C",       ".DB 0xFD, 0x8D",       "ADC A, (IY+0x{0:X2})", ".DB 0xFD, 0x8F",       /* 0x88 */
            ".DB 0xFD, 0x90",       ".DB 0xFD, 0x91",       ".DB 0xFD, 0x92",       ".DB 0xFD, 0x93",       ".DB 0xFD, 0x94",       ".DB 0xFD, 0x95",       "SUB (IY+0x{0:X2})",    ".DB 0xFD, 0x97",       /* 0x90 */
            ".DB 0xFD, 0x98",       ".DB 0xFD, 0x99",       ".DB 0xFD, 0x9A",       ".DB 0xFD, 0x9B",       ".DB 0xFD, 0x9C",       ".DB 0xFD, 0x9D",       "SBC (IY+0x{0:X2})",    ".DB 0xFD, 0x9F",       /* 0x98 */
            ".DB 0xFD, 0xA0",       ".DB 0xFD, 0xA1",       ".DB 0xFD, 0xA2",       ".DB 0xFD, 0xA3",       ".DB 0xFD, 0xA4",       ".DB 0xFD, 0xA5",       "AND (IY+0x{0:X2})",    ".DB 0xFD, 0xA7",       /* 0xA0 */
            ".DB 0xFD, 0xA8",       ".DB 0xFD, 0xA9",       ".DB 0xFD, 0xAA",       ".DB 0xFD, 0xAB",       ".DB 0xFD, 0xAC",       ".DB 0xFD, 0xAD",       "XOR (IY+0x{0:X2})",    ".DB 0xFD, 0xAF",       /* 0xA8 */
            ".DB 0xFD, 0xB0",       ".DB 0xFD, 0xB1",       ".DB 0xFD, 0xB2",       ".DB 0xFD, 0xB3",       ".DB 0xFD, 0xB4",       ".DB 0xFD, 0xB5",       "OR (IY+0x{0:X2})",     ".DB 0xFD, 0xB7",       /* 0xB0 */
            ".DB 0xFD, 0xB8",       ".DB 0xFD, 0xB9",       ".DB 0xFD, 0xBA",       ".DB 0xFD, 0xBB",       ".DB 0xFD, 0xBC",       ".DB 0xFD, 0xBD",       "CP (IY+0x{0:X2})",     ".DB 0xFD, 0xBF",       /* 0xB8 */
            ".DB 0xFD, 0xC0",       ".DB 0xFD, 0xC1",       ".DB 0xFD, 0xC2",       ".DB 0xFD, 0xC3",       ".DB 0xFD, 0xC4",       ".DB 0xFD, 0xC5",       ".DB 0xFD, 0xC6",       ".DB 0xFD, 0xC7",       /* 0xC0 */
            ".DB 0xFD, 0xC8",       ".DB 0xFD, 0xC9",       ".DB 0xFD, 0xCA",       string.Empty,           ".DB 0xFD, 0xCC",       ".DB 0xFD, 0xCD",       ".DB 0xFD, 0xCE",       ".DB 0xFD, 0xCF",       /* 0xC8 */
            ".DB 0xFD, 0xD0",       ".DB 0xFD, 0xD1",       ".DB 0xFD, 0xD2",       ".DB 0xFD, 0xD3",       ".DB 0xFD, 0xD4",       ".DB 0xFD, 0xD5",       ".DB 0xFD, 0xD6",       ".DB 0xFD, 0xD7",       /* 0xD0 */
            ".DB 0xFD, 0xD8",       ".DB 0xFD, 0xD9",       ".DB 0xFD, 0xDA",       ".DB 0xFD, 0xDB",       ".DB 0xFD, 0xDC",       ".DB 0xFD, 0xFD",       ".DB 0xFD, 0xDE",       ".DB 0xFD, 0xDF",       /* 0xD8 */
            ".DB 0xED, 0xE0",       "POP IY",               ".DB 0xED, 0xE2",       "EX (SP), IY",          ".DB 0xED, 0xE4",       "PUSH IY",              ".DB 0xED, 0xE6",       ".DB 0xED, 0xE7",       /* 0xE0 */
            ".DB 0xED, 0xE8",       "JP (IY)",              ".DB 0xED, 0xEA",       ".DB 0xED, 0xEB",       ".DB 0xED, 0xEC",       ".DB 0xED, 0xED",       ".DB 0xED, 0xEE",       ".DB 0xED, 0xEF",       /* 0xE8 */
            ".DB 0xFD, 0xF0",       ".DB 0xFD, 0xF1",       ".DB 0xFD, 0xF2",       ".DB 0xFD, 0xF3",       ".DB 0xFD, 0xF4",       ".DB 0xFD, 0xF5",       ".DB 0xFD, 0xF6",       ".DB 0xFD, 0xF7",       /* 0xF0 */
            ".DB 0xFD, 0xF8",       "LD SP, IY",            ".DB 0xFD, 0xFA",       ".DB 0xFD, 0xFB",       ".DB 0xFD, 0xFC",       ".DB 0xFD, 0xFD",       ".DB 0xFD, 0xFE",       ".DB 0xFD, 0xFF"        /* 0xF8 */
        };

        static readonly string[] opcodeMnemonic_DDCB = new string[]
        {
            ".DB 0xDD, 0xCB, 0x00", ".DB 0xDD, 0xCB, 0x01", ".DB 0xDD, 0xCB, 0x02", ".DB 0xDD, 0xCB, 0x03", ".DB 0xDD, 0xCB, 0x04", ".DB 0xDD, 0xCB, 0x05", "RLC (IX+0x{0:X2})",    ".DB 0xDD, 0xCB, 0x07", /* 0x00 */
            ".DB 0xDD, 0xCB, 0x08", ".DB 0xDD, 0xCB, 0x09", ".DB 0xDD, 0xCB, 0x0A", ".DB 0xDD, 0xCB, 0x0B", ".DB 0xDD, 0xCB, 0x0C", ".DB 0xDD, 0xCB, 0x0D", "RRC (IX+0x{0:X2})",    ".DB 0xDD, 0xCB, 0x0F", /* 0x08 */
            ".DB 0xDD, 0xCB, 0x10", ".DB 0xDD, 0xCB, 0x11", ".DB 0xDD, 0xCB, 0x12", ".DB 0xDD, 0xCB, 0x13", ".DB 0xDD, 0xCB, 0x14", ".DB 0xDD, 0xCB, 0x15", "RL (IX+0x{0:X2})",     ".DB 0xDD, 0xCB, 0x17", /* 0x10 */
            ".DB 0xDD, 0xCB, 0x18", ".DB 0xDD, 0xCB, 0x19", ".DB 0xDD, 0xCB, 0x1A", ".DB 0xDD, 0xCB, 0x1B", ".DB 0xDD, 0xCB, 0x1C", ".DB 0xDD, 0xCB, 0x1D", "RR (IX+0x{0:X2})",     ".DB 0xDD, 0xCB, 0x1F", /* 0x18 */
            ".DB 0xDD, 0xCB, 0x20", ".DB 0xDD, 0xCB, 0x21", ".DB 0xDD, 0xCB, 0x22", ".DB 0xDD, 0xCB, 0x23", ".DB 0xDD, 0xCB, 0x24", ".DB 0xDD, 0xCB, 0x25", "SLA (IX+0x{0:X2})",    ".DB 0xDD, 0xCB, 0x27", /* 0x20 */
            ".DB 0xDD, 0xCB, 0x28", ".DB 0xDD, 0xCB, 0x29", ".DB 0xDD, 0xCB, 0x2A", ".DB 0xDD, 0xCB, 0x2B", ".DB 0xDD, 0xCB, 0x2C", ".DB 0xDD, 0xCB, 0x2D", "SRA (IX+0x{0:X2})",    ".DB 0xDD, 0xCB, 0x2F", /* 0x28 */
            ".DB 0xDD, 0xCB, 0x30", ".DB 0xDD, 0xCB, 0x31", ".DB 0xDD, 0xCB, 0x32", ".DB 0xDD, 0xCB, 0x33", ".DB 0xDD, 0xCB, 0x34", ".DB 0xDD, 0xCB, 0x35", "SLL (IX+0x{0:X2})",    ".DB 0xDD, 0xCB, 0x37", /* 0x30 */
            ".DB 0xDD, 0xCB, 0x38", ".DB 0xDD, 0xCB, 0x39", ".DB 0xDD, 0xCB, 0x3A", ".DB 0xDD, 0xCB, 0x3B", ".DB 0xDD, 0xCB, 0x3C", ".DB 0xDD, 0xCB, 0x3D", "SRL (IX+0x{0:X2})",    ".DB 0xDD, 0xCB, 0x3F", /* 0x38 */
            ".DB 0xDD, 0xCB, 0x40", ".DB 0xDD, 0xCB, 0x41", ".DB 0xDD, 0xCB, 0x42", ".DB 0xDD, 0xCB, 0x43", ".DB 0xDD, 0xCB, 0x44", ".DB 0xDD, 0xCB, 0x45", "BIT 0, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x47", /* 0x40 */
            ".DB 0xDD, 0xCB, 0x48", ".DB 0xDD, 0xCB, 0x49", ".DB 0xDD, 0xCB, 0x4A", ".DB 0xDD, 0xCB, 0x4B", ".DB 0xDD, 0xCB, 0x4C", ".DB 0xDD, 0xCB, 0x4D", "BIT 1, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x4F", /* 0x48 */
            ".DB 0xDD, 0xCB, 0x50", ".DB 0xDD, 0xCB, 0x51", ".DB 0xDD, 0xCB, 0x52", ".DB 0xDD, 0xCB, 0x53", ".DB 0xDD, 0xCB, 0x54", ".DB 0xDD, 0xCB, 0x55", "BIT 2, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x57", /* 0x50 */
            ".DB 0xDD, 0xCB, 0x58", ".DB 0xDD, 0xCB, 0x59", ".DB 0xDD, 0xCB, 0x5A", ".DB 0xDD, 0xCB, 0x5B", ".DB 0xDD, 0xCB, 0x5C", ".DB 0xDD, 0xCB, 0x5D", "BIT 3, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x5F", /* 0x58 */
            ".DB 0xDD, 0xCB, 0x60", ".DB 0xDD, 0xCB, 0x61", ".DB 0xDD, 0xCB, 0x62", ".DB 0xDD, 0xCB, 0x63", ".DB 0xDD, 0xCB, 0x64", ".DB 0xDD, 0xCB, 0x65", "BIT 4, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x67", /* 0x60 */
            ".DB 0xDD, 0xCB, 0x68", ".DB 0xDD, 0xCB, 0x69", ".DB 0xDD, 0xCB, 0x6A", ".DB 0xDD, 0xCB, 0x6B", ".DB 0xDD, 0xCB, 0x6C", ".DB 0xDD, 0xCB, 0x6D", "BIT 5, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x6F", /* 0x68 */
            ".DB 0xDD, 0xCB, 0x70", ".DB 0xDD, 0xCB, 0x71", ".DB 0xDD, 0xCB, 0x72", ".DB 0xDD, 0xCB, 0x73", ".DB 0xDD, 0xCB, 0x74", ".DB 0xDD, 0xCB, 0x75", "BIT 6, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x77", /* 0x70 */
            ".DB 0xDD, 0xCB, 0x78", ".DB 0xDD, 0xCB, 0x79", ".DB 0xDD, 0xCB, 0x7A", ".DB 0xDD, 0xCB, 0x7B", ".DB 0xDD, 0xCB, 0x7C", ".DB 0xDD, 0xCB, 0x7D", "BIT 7, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x7F", /* 0x78 */
            ".DB 0xDD, 0xCB, 0x80", ".DB 0xDD, 0xCB, 0x81", ".DB 0xDD, 0xCB, 0x82", ".DB 0xDD, 0xCB, 0x83", ".DB 0xDD, 0xCB, 0x84", ".DB 0xDD, 0xCB, 0x85", "RES 0, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x87", /* 0x80 */
            ".DB 0xDD, 0xCB, 0x88", ".DB 0xDD, 0xCB, 0x89", ".DB 0xDD, 0xCB, 0x8A", ".DB 0xDD, 0xCB, 0x8B", ".DB 0xDD, 0xCB, 0x8C", ".DB 0xDD, 0xCB, 0x8D", "RES 1, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x8F", /* 0x88 */
            ".DB 0xDD, 0xCB, 0x90", ".DB 0xDD, 0xCB, 0x91", ".DB 0xDD, 0xCB, 0x92", ".DB 0xDD, 0xCB, 0x93", ".DB 0xDD, 0xCB, 0x94", ".DB 0xDD, 0xCB, 0x95", "RES 2, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x97", /* 0x90 */
            ".DB 0xDD, 0xCB, 0x98", ".DB 0xDD, 0xCB, 0x99", ".DB 0xDD, 0xCB, 0x9A", ".DB 0xDD, 0xCB, 0x9B", ".DB 0xDD, 0xCB, 0x9C", ".DB 0xDD, 0xCB, 0x9D", "RES 3, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0x9F", /* 0x98 */
            ".DB 0xDD, 0xCB, 0xA0", ".DB 0xDD, 0xCB, 0xA1", ".DB 0xDD, 0xCB, 0xA2", ".DB 0xDD, 0xCB, 0xA3", ".DB 0xDD, 0xCB, 0xA4", ".DB 0xDD, 0xCB, 0xA5", "RES 4, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0xA7", /* 0xA0 */
            ".DB 0xDD, 0xCB, 0xA8", ".DB 0xDD, 0xCB, 0xA9", ".DB 0xDD, 0xCB, 0xAA", ".DB 0xDD, 0xCB, 0xAB", ".DB 0xDD, 0xCB, 0xAC", ".DB 0xDD, 0xCB, 0xAD", "RES 5, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0xAF", /* 0xA8 */
            ".DB 0xDD, 0xCB, 0xB0", ".DB 0xDD, 0xCB, 0xB1", ".DB 0xDD, 0xCB, 0xB2", ".DB 0xDD, 0xCB, 0xB3", ".DB 0xDD, 0xCB, 0xB4", ".DB 0xDD, 0xCB, 0xB5", "RES 6, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0xB7", /* 0xB0 */
            ".DB 0xDD, 0xCB, 0xB8", ".DB 0xDD, 0xCB, 0xB9", ".DB 0xDD, 0xCB, 0xBA", ".DB 0xDD, 0xCB, 0xBB", ".DB 0xDD, 0xCB, 0xBC", ".DB 0xDD, 0xCB, 0xBD", "RES 7, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0xBF", /* 0xB8 */
            ".DB 0xDD, 0xCB, 0xC0", ".DB 0xDD, 0xCB, 0xC1", ".DB 0xDD, 0xCB, 0xC2", ".DB 0xDD, 0xCB, 0xC3", ".DB 0xDD, 0xCB, 0xC4", ".DB 0xDD, 0xCB, 0xC5", "SET 0, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0xC7", /* 0xC0 */
            ".DB 0xDD, 0xCB, 0xC8", ".DB 0xDD, 0xCB, 0xC9", ".DB 0xDD, 0xCB, 0xCA", ".DB 0xDD, 0xCB, 0xCB", ".DB 0xDD, 0xCB, 0xCC", ".DB 0xDD, 0xCB, 0xCD", "SET 1, (IX+0x{0:X2})", ".DB 0xDD, 0xCB, 0xCF", /* 0xC8 */
            ".DB 0xDD, 0xDB, 0xD0", ".DB 0xDD, 0xDB, 0xD1", ".DB 0xDD, 0xDB, 0xD2", ".DB 0xDD, 0xDB, 0xD3", ".DB 0xDD, 0xDB, 0xD4", ".DB 0xDD, 0xDB, 0xD5", "SET 2, (IX+0x{0:X2})", ".DB 0xDD, 0xDB, 0xD7", /* 0xD0 */
            ".DB 0xDD, 0xDB, 0xD8", ".DB 0xDD, 0xDB, 0xD9", ".DB 0xDD, 0xDB, 0xDA", ".DB 0xDD, 0xDB, 0xDB", ".DB 0xDD, 0xDB, 0xDC", ".DB 0xDD, 0xDB, 0xDD", "SET 3, (IX+0x{0:X2})", ".DB 0xDD, 0xDB, 0xDF", /* 0xD8 */
            ".DB 0xDD, 0xEB, 0xE0", ".DB 0xDD, 0xEB, 0xE1", ".DB 0xDD, 0xEB, 0xE2", ".DB 0xDD, 0xEB, 0xE3", ".DB 0xDD, 0xEB, 0xE4", ".DB 0xDD, 0xEB, 0xE5", "SET 4, (IX+0x{0:X2})", ".DB 0xDD, 0xEB, 0xE7", /* 0xE0 */
            ".DB 0xDD, 0xEB, 0xE8", ".DB 0xDD, 0xEB, 0xE9", ".DB 0xDD, 0xEB, 0xEA", ".DB 0xDD, 0xEB, 0xEB", ".DB 0xDD, 0xEB, 0xEC", ".DB 0xDD, 0xEB, 0xED", "SET 5, (IX+0x{0:X2})", ".DB 0xDD, 0xEB, 0xEF", /* 0xE8 */
            ".DB 0xDD, 0xFB, 0xF0", ".DB 0xDD, 0xFB, 0xF1", ".DB 0xDD, 0xFB, 0xF2", ".DB 0xDD, 0xFB, 0xF3", ".DB 0xDD, 0xFB, 0xF4", ".DB 0xDD, 0xFB, 0xF5", "SET 6, (IX+0x{0:X2})", ".DB 0xDD, 0xFB, 0xF7", /* 0xF0 */
            ".DB 0xDD, 0xFB, 0xF8", ".DB 0xDD, 0xFB, 0xF9", ".DB 0xDD, 0xFB, 0xFA", ".DB 0xDD, 0xFB, 0xFB", ".DB 0xDD, 0xFB, 0xFC", ".DB 0xDD, 0xFB, 0xFD", "SET 7, (IX+0x{0:X2})", ".DB 0xDD, 0xFB, 0xFF"  /* 0xF8 */
        };

        static readonly string[] opcodeMnemonic_FDCB = new string[]
        {
            ".DB 0xFD, 0xCB, 0x00", ".DB 0xFD, 0xCB, 0x01", ".DB 0xFD, 0xCB, 0x02", ".DB 0xFD, 0xCB, 0x03", ".DB 0xFD, 0xCB, 0x04", ".DB 0xFD, 0xCB, 0x05", "RLC (IX+0x{0:X2})",    ".DB 0xFD, 0xCB, 0x07", /* 0x00 */
            ".DB 0xFD, 0xCB, 0x08", ".DB 0xFD, 0xCB, 0x09", ".DB 0xFD, 0xCB, 0x0A", ".DB 0xFD, 0xCB, 0x0B", ".DB 0xFD, 0xCB, 0x0C", ".DB 0xFD, 0xCB, 0x0D", "RRC (IX+0x{0:X2})",    ".DB 0xFD, 0xCB, 0x0F", /* 0x08 */
            ".DB 0xFD, 0xCB, 0x10", ".DB 0xFD, 0xCB, 0x11", ".DB 0xFD, 0xCB, 0x12", ".DB 0xFD, 0xCB, 0x13", ".DB 0xFD, 0xCB, 0x14", ".DB 0xFD, 0xCB, 0x15", "RL (IX+0x{0:X2})",     ".DB 0xFD, 0xCB, 0x17", /* 0x10 */
            ".DB 0xFD, 0xCB, 0x18", ".DB 0xFD, 0xCB, 0x19", ".DB 0xFD, 0xCB, 0x1A", ".DB 0xFD, 0xCB, 0x1B", ".DB 0xFD, 0xCB, 0x1C", ".DB 0xFD, 0xCB, 0x1D", "RR (IX+0x{0:X2})",     ".DB 0xFD, 0xCB, 0x1F", /* 0x18 */
            ".DB 0xFD, 0xCB, 0x20", ".DB 0xFD, 0xCB, 0x21", ".DB 0xFD, 0xCB, 0x22", ".DB 0xFD, 0xCB, 0x23", ".DB 0xFD, 0xCB, 0x24", ".DB 0xFD, 0xCB, 0x25", "SLA (IX+0x{0:X2})",    ".DB 0xFD, 0xCB, 0x27", /* 0x20 */
            ".DB 0xFD, 0xCB, 0x28", ".DB 0xFD, 0xCB, 0x29", ".DB 0xFD, 0xCB, 0x2A", ".DB 0xFD, 0xCB, 0x2B", ".DB 0xFD, 0xCB, 0x2C", ".DB 0xFD, 0xCB, 0x2D", "SRA (IX+0x{0:X2})",    ".DB 0xFD, 0xCB, 0x2F", /* 0x28 */
            ".DB 0xFD, 0xCB, 0x30", ".DB 0xFD, 0xCB, 0x31", ".DB 0xFD, 0xCB, 0x32", ".DB 0xFD, 0xCB, 0x33", ".DB 0xFD, 0xCB, 0x34", ".DB 0xFD, 0xCB, 0x35", ".DB 0xFD, 0xCB, 0x36", ".DB 0xFD, 0xCB, 0x37", /* 0x30 */
            ".DB 0xFD, 0xCB, 0x38", ".DB 0xFD, 0xCB, 0x39", ".DB 0xFD, 0xCB, 0x3A", ".DB 0xFD, 0xCB, 0x3B", ".DB 0xFD, 0xCB, 0x3C", ".DB 0xFD, 0xCB, 0x3D", "SRL (IX+0x{0:X2})",    ".DB 0xFD, 0xCB, 0x3F", /* 0x38 */
            ".DB 0xFD, 0xCB, 0x40", ".DB 0xFD, 0xCB, 0x41", ".DB 0xFD, 0xCB, 0x42", ".DB 0xFD, 0xCB, 0x43", ".DB 0xFD, 0xCB, 0x44", ".DB 0xFD, 0xCB, 0x45", "BIT 0, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x47", /* 0x40 */
            ".DB 0xFD, 0xCB, 0x48", ".DB 0xFD, 0xCB, 0x49", ".DB 0xFD, 0xCB, 0x4A", ".DB 0xFD, 0xCB, 0x4B", ".DB 0xFD, 0xCB, 0x4C", ".DB 0xFD, 0xCB, 0x4D", "BIT 1, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x4F", /* 0x48 */
            ".DB 0xFD, 0xCB, 0x50", ".DB 0xFD, 0xCB, 0x51", ".DB 0xFD, 0xCB, 0x52", ".DB 0xFD, 0xCB, 0x53", ".DB 0xFD, 0xCB, 0x54", ".DB 0xFD, 0xCB, 0x55", "BIT 2, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x57", /* 0x50 */
            ".DB 0xFD, 0xCB, 0x58", ".DB 0xFD, 0xCB, 0x59", ".DB 0xFD, 0xCB, 0x5A", ".DB 0xFD, 0xCB, 0x5B", ".DB 0xFD, 0xCB, 0x5C", ".DB 0xFD, 0xCB, 0x5D", "BIT 3, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x5F", /* 0x58 */
            ".DB 0xFD, 0xCB, 0x60", ".DB 0xFD, 0xCB, 0x61", ".DB 0xFD, 0xCB, 0x62", ".DB 0xFD, 0xCB, 0x63", ".DB 0xFD, 0xCB, 0x64", ".DB 0xFD, 0xCB, 0x65", "BIT 4, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x67", /* 0x60 */
            ".DB 0xFD, 0xCB, 0x68", ".DB 0xFD, 0xCB, 0x69", ".DB 0xFD, 0xCB, 0x6A", ".DB 0xFD, 0xCB, 0x6B", ".DB 0xFD, 0xCB, 0x6C", ".DB 0xFD, 0xCB, 0x6D", "BIT 5, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x6F", /* 0x68 */
            ".DB 0xFD, 0xCB, 0x70", ".DB 0xFD, 0xCB, 0x71", ".DB 0xFD, 0xCB, 0x72", ".DB 0xFD, 0xCB, 0x73", ".DB 0xFD, 0xCB, 0x74", ".DB 0xFD, 0xCB, 0x75", "BIT 6, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x77", /* 0x70 */
            ".DB 0xFD, 0xCB, 0x78", ".DB 0xFD, 0xCB, 0x79", ".DB 0xFD, 0xCB, 0x7A", ".DB 0xFD, 0xCB, 0x7B", ".DB 0xFD, 0xCB, 0x7C", ".DB 0xFD, 0xCB, 0x7D", "BIT 7, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x7F", /* 0x78 */
            ".DB 0xFD, 0xCB, 0x80", ".DB 0xFD, 0xCB, 0x81", ".DB 0xFD, 0xCB, 0x82", ".DB 0xFD, 0xCB, 0x83", ".DB 0xFD, 0xCB, 0x84", ".DB 0xFD, 0xCB, 0x85", "RES 0, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x87", /* 0x80 */
            ".DB 0xFD, 0xCB, 0x88", ".DB 0xFD, 0xCB, 0x89", ".DB 0xFD, 0xCB, 0x8A", ".DB 0xFD, 0xCB, 0x8B", ".DB 0xFD, 0xCB, 0x8C", ".DB 0xFD, 0xCB, 0x8D", "RES 1, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x8F", /* 0x88 */
            ".DB 0xFD, 0xCB, 0x90", ".DB 0xFD, 0xCB, 0x91", ".DB 0xFD, 0xCB, 0x92", ".DB 0xFD, 0xCB, 0x93", ".DB 0xFD, 0xCB, 0x94", ".DB 0xFD, 0xCB, 0x95", "RES 2, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x97", /* 0x90 */
            ".DB 0xFD, 0xCB, 0x98", ".DB 0xFD, 0xCB, 0x99", ".DB 0xFD, 0xCB, 0x9A", ".DB 0xFD, 0xCB, 0x9B", ".DB 0xFD, 0xCB, 0x9C", ".DB 0xFD, 0xCB, 0x9D", "RES 3, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0x9F", /* 0x98 */
            ".DB 0xFD, 0xCB, 0xA0", ".DB 0xFD, 0xCB, 0xA1", ".DB 0xFD, 0xCB, 0xA2", ".DB 0xFD, 0xCB, 0xA3", ".DB 0xFD, 0xCB, 0xA4", ".DB 0xFD, 0xCB, 0xA5", "RES 4, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0xA7", /* 0xA0 */
            ".DB 0xFD, 0xCB, 0xA8", ".DB 0xFD, 0xCB, 0xA9", ".DB 0xFD, 0xCB, 0xAA", ".DB 0xFD, 0xCB, 0xAB", ".DB 0xFD, 0xCB, 0xAC", ".DB 0xFD, 0xCB, 0xAD", "RES 5, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0xAF", /* 0xA8 */
            ".DB 0xFD, 0xCB, 0xB0", ".DB 0xFD, 0xCB, 0xB1", ".DB 0xFD, 0xCB, 0xB2", ".DB 0xFD, 0xCB, 0xB3", ".DB 0xFD, 0xCB, 0xB4", ".DB 0xFD, 0xCB, 0xB5", "RES 6, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0xB7", /* 0xB0 */
            ".DB 0xFD, 0xCB, 0xB8", ".DB 0xFD, 0xCB, 0xB9", ".DB 0xFD, 0xCB, 0xBA", ".DB 0xFD, 0xCB, 0xBB", ".DB 0xFD, 0xCB, 0xBC", ".DB 0xFD, 0xCB, 0xBD", "RES 7, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0xBF", /* 0xB8 */
            ".DB 0xFD, 0xCB, 0xC0", ".DB 0xFD, 0xCB, 0xC1", ".DB 0xFD, 0xCB, 0xC2", ".DB 0xFD, 0xCB, 0xC3", ".DB 0xFD, 0xCB, 0xC4", ".DB 0xFD, 0xCB, 0xC5", "SET 0, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0xC7", /* 0xC0 */
            ".DB 0xFD, 0xCB, 0xC8", ".DB 0xFD, 0xCB, 0xC9", ".DB 0xFD, 0xCB, 0xCA", ".DB 0xFD, 0xCB, 0xCB", ".DB 0xFD, 0xCB, 0xCC", ".DB 0xFD, 0xCB, 0xCD", "SET 1, (IX+0x{0:X2})", ".DB 0xFD, 0xCB, 0xCF", /* 0xC8 */
            ".DB 0xFD, 0xDB, 0xD0", ".DB 0xFD, 0xDB, 0xD1", ".DB 0xFD, 0xDB, 0xD2", ".DB 0xFD, 0xDB, 0xD3", ".DB 0xFD, 0xDB, 0xD4", ".DB 0xFD, 0xDB, 0xD5", "SET 2, (IX+0x{0:X2})", ".DB 0xFD, 0xDB, 0xD7", /* 0xD0 */
            ".DB 0xFD, 0xDB, 0xD8", ".DB 0xFD, 0xDB, 0xD9", ".DB 0xFD, 0xDB, 0xDA", ".DB 0xFD, 0xDB, 0xDB", ".DB 0xFD, 0xDB, 0xDC", ".DB 0xFD, 0xDB, 0xFD", "SET 3, (IX+0x{0:X2})", ".DB 0xFD, 0xDB, 0xDF", /* 0xD8 */
            ".DB 0xFD, 0xEB, 0xE0", ".DB 0xFD, 0xEB, 0xE1", ".DB 0xFD, 0xEB, 0xE2", ".DB 0xFD, 0xEB, 0xE3", ".DB 0xFD, 0xEB, 0xE4", ".DB 0xFD, 0xEB, 0xE5", "SET 4, (IX+0x{0:X2})", ".DB 0xFD, 0xEB, 0xE7", /* 0xE0 */
            ".DB 0xFD, 0xEB, 0xE8", ".DB 0xFD, 0xEB, 0xE9", ".DB 0xFD, 0xEB, 0xEA", ".DB 0xFD, 0xEB, 0xEB", ".DB 0xFD, 0xEB, 0xEC", ".DB 0xFD, 0xEB, 0xED", "SET 5, (IX+0x{0:X2})", ".DB 0xFD, 0xEB, 0xEF", /* 0xE8 */
            ".DB 0xFD, 0xFB, 0xF0", ".DB 0xFD, 0xFB, 0xF1", ".DB 0xFD, 0xFB, 0xF2", ".DB 0xFD, 0xFB, 0xF3", ".DB 0xFD, 0xFB, 0xF4", ".DB 0xFD, 0xFB, 0xF5", "SET 6, (IX+0x{0:X2})", ".DB 0xFD, 0xFB, 0xF7", /* 0xF0 */
            ".DB 0xFD, 0xFB, 0xF8", ".DB 0xFD, 0xFB, 0xF9", ".DB 0xFD, 0xFB, 0xFA", ".DB 0xFD, 0xFB, 0xFB", ".DB 0xFD, 0xFB, 0xFC", ".DB 0xFD, 0xFB, 0xFD", "SET 7, (IX+0x{0:X2})", ".DB 0xFD, 0xFB, 0xFF"  /* 0xF8 */
        };

        static readonly int[] opcodeLength_DDFD = new int[]
        {
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 4, 4, 2, 2, 2, 2, 2, 2, 2, 4, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 3, 3, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 3, 2,
            2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 3, 2,
            2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 3, 2,
            3, 3, 3, 3, 3, 3, 2, 3, 2, 2, 2, 2, 2, 2, 3, 2,
            2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 3, 2,
            2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 3, 2,
            2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 3, 2,
            2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 3, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        };

        static readonly int[] opcodeLength_DDFDCB = new int[]
        {
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        };

        public static string PrintRegisters(Z80 cpu)
        {
            return string.Format("AF:{0:X4} BC:{1:X4} DE:{2:X4} HL:{3:X4} IX:{4:X4} IY:{5:X4} SP:{6:X4}", cpu.af.Word, cpu.bc.Word, cpu.de.Word, cpu.hl.Word, cpu.ix.Word, cpu.iy.Word, cpu.sp);
        }

        public static string PrintFlags(Z80 cpu)
        {
            return string.Format("[{7}{6}{5}{4}{3}{2}{1}{0}]",
                cpu.IsFlagSet(Flags.C) ? "C" : "-",
                cpu.IsFlagSet(Flags.N) ? "N" : "-",
                cpu.IsFlagSet(Flags.PV) ? "P" : "-",
                cpu.IsFlagSet(Flags.UB3) ? "3" : "-",
                cpu.IsFlagSet(Flags.H) ? "H" : "-",
                cpu.IsFlagSet(Flags.UB5) ? "5" : "-",
                cpu.IsFlagSet(Flags.Z) ? "Z" : "-",
                cpu.IsFlagSet(Flags.S) ? "S" : "-");
        }

        public static string DisassembleOpcode(Z80 cpu, ushort address)
        {
            byte[] opcode = DisassembleGetOpcodeBytes(cpu, address);
            return string.Format("{0:X4} | {1} | {2}", address, DisassembleMakeByteString(cpu, opcode).PadRight(15), DisassembleMakeMnemonicString(cpu, opcode));
        }

        public static byte[] DisassembleGetOpcodeBytes(Z80 cpu, ushort address)
        {
            byte[] opcode = new byte[5];
            for (int i = 0; i < opcode.Length; i++)
                opcode[i] = (address + i <= 0xFFFF ? cpu.ReadMemory8((ushort)(address + i)) : (byte)0);
            return opcode;
        }

        public static int DisassembleGetOpcodeLen(Z80 cpu, byte[] opcode)
        {
            switch (opcode[0])
            {
                case 0xCB: return opcodeLength_CB[opcode[1]];
                case 0xED: return opcodeLength_ED[opcode[1]];

                case 0xDD:
                case 0xFD:
                    if (opcode[1] == 0xCB)
                        return opcodeLength_DDFDCB[opcode[3]];
                    else
                        return opcodeLength_DDFD[opcode[1]];

                default: return opcodeLength_Main[opcode[0]];
            }
        }

        public static string DisassembleMakeByteString(Z80 cpu, byte[] opcode)
        {
            return string.Join(" ", opcode.Select(x => string.Format("{0:X2}", x)).Take(DisassembleGetOpcodeLen(cpu, opcode)));
        }

        public static string DisassembleMakeMnemonicString(Z80 cpu, byte[] opcode)
        {
            int len = DisassembleGetOpcodeLen(cpu, opcode);

            int start = 0;
            string[] mnemonics = opcodeMnemonic_Main;
            bool isDDFDCB = false;

            switch (opcode[0])
            {
                case 0xCB: start = 1; mnemonics = opcodeMnemonic_CB; break;
                case 0xED: start = 1; mnemonics = opcodeMnemonic_ED; break;

                case 0xDD:
                    if (opcode[1] == 0xCB)
                    {
                        mnemonics = opcodeMnemonic_DDCB;
                        isDDFDCB = true;
                    }
                    else
                    {
                        start = 1;
                        mnemonics = opcodeMnemonic_DD;
                    }
                    break;

                case 0xFD:
                    if (opcode[1] == 0xCB)
                    {
                        mnemonics = opcodeMnemonic_FDCB;
                        isDDFDCB = true;
                    }
                    else
                    {
                        start = 1;
                        mnemonics = opcodeMnemonic_FD;
                    }
                    break;
            }

            if (mnemonics == null) return "(unimplemented)";

            if (!isDDFDCB)
            {
                switch (len - start)
                {
                    case 1: return mnemonics[opcode[start]];
                    case 2: return string.Format(mnemonics[opcode[start]], opcode[start + 1]);
                    case 3: return string.Format(mnemonics[opcode[start]], (opcode[start + 2] << 8 | opcode[start + 1]));
                    default: return string.Empty;
                }
            }
            else
            {
                return string.Format(mnemonics[opcode[3]], opcode[2]);
            }
        }

        public class CpuDebugSnapshot
        {
            BaseUnit.CoreDebugSnapshot parent;

            public Register AF { get; private set; }
            public Register BC { get; private set; }
            public Register DE { get; private set; }
            public Register HL { get; private set; }
            public Register AFShadow { get; private set; }
            public Register BCShadow { get; private set; }
            public Register DEShadow { get; private set; }
            public Register HLShadow { get; private set; }

            public Register IX { get; private set; }
            public Register IY { get; private set; }

            public byte I { get; private set; }
            public byte R { get; private set; }

            public ushort SP { get; private set; }
            public ushort PC { get; private set; }

            public bool IFF1 { get; private set; }
            public bool IFF2 { get; private set; }
            public bool Halted { get; private set; }
            public byte InterruptMode { get; private set; }

            public CpuDebugSnapshot(BaseUnit.CoreDebugSnapshot parent, Z80 cpu)
            {
                this.parent = parent;

                AF = new Register() { Word = cpu.af.Word };
                BC = new Register() { Word = cpu.bc.Word };
                DE = new Register() { Word = cpu.de.Word };
                HL = new Register() { Word = cpu.hl.Word };
                AFShadow = new Register() { Word = cpu.afShadow.Word };
                BCShadow = new Register() { Word = cpu.bcShadow.Word };
                DEShadow = new Register() { Word = cpu.deShadow.Word };
                HLShadow = new Register() { Word = cpu.hlShadow.Word };

                IX = new Register() { Word = cpu.ix.Word };
                IY = new Register() { Word = cpu.iy.Word };

                I = cpu.i;
                R = cpu.r;

                SP = cpu.sp;
                PC = cpu.pc;

                IFF1 = cpu.iff1;
                IFF2 = cpu.iff2;
                Halted = cpu.halted;
                InterruptMode = cpu.interruptMode;
            }

            public bool IsFlagSet(Flags flags)
            {
                return (((Flags)AF.Low & flags) == flags);
            }

            public string GetFlagsString()
            {
                return string.Format("[{7}{6}{5}{4}{3}{2}{1}{0}]",
                    IsFlagSet(Flags.C) ? "C" : "-",
                    IsFlagSet(Flags.N) ? "N" : "-",
                    IsFlagSet(Flags.PV) ? "P" : "-",
                    IsFlagSet(Flags.UB3) ? "3" : "-",
                    IsFlagSet(Flags.H) ? "H" : "-",
                    IsFlagSet(Flags.UB5) ? "5" : "-",
                    IsFlagSet(Flags.Z) ? "Z" : "-",
                    IsFlagSet(Flags.S) ? "S" : "-");
            }

            public byte[] GetOpcodeBytes(ushort address)
            {
                byte[] opcodeBytes = new byte[5];
                for (int i = 0; i < opcodeBytes.Length; i++)
                    opcodeBytes[i] = ((address + i) < parent.MemoryMap.Length ? parent.MemoryMap[address + i] : (byte)0);

                int length = GetOpcodeLength(opcodeBytes);
                return opcodeBytes.Take(length).ToArray();
            }

            private int GetOpcodeLength(byte[] opcodeBytes)
            {
                switch (opcodeBytes[0])
                {
                    case 0xCB: return opcodeLength_CB[opcodeBytes[1]];
                    case 0xED: return opcodeLength_ED[opcodeBytes[1]];

                    case 0xDD:
                    case 0xFD:
                        if (opcodeBytes[1] == 0xCB)
                            return opcodeLength_DDFDCB[opcodeBytes[3]];
                        else
                            return opcodeLength_DDFD[opcodeBytes[1]];

                    default: return opcodeLength_Main[opcodeBytes[0]];
                }
            }

            public string GetOpcodeBytesString(byte[] opcodeBytes)
            {
                return string.Join(" ", opcodeBytes.Select(x => string.Format("{0:X2}", x)));
            }

            public string GetOpcodesMnemonicString(byte[] opcodeBytes)
            {
                int start = 0;
                string[] mnemonics = opcodeMnemonic_Main;
                bool isDDFDCB = false;

                switch (opcodeBytes[0])
                {
                    case 0xCB: start = 1; mnemonics = opcodeMnemonic_CB; break;
                    case 0xED: start = 1; mnemonics = opcodeMnemonic_ED; break;

                    case 0xDD:
                        if (opcodeBytes[1] == 0xCB)
                        {
                            mnemonics = opcodeMnemonic_DDCB;
                            isDDFDCB = true;
                        }
                        else
                        {
                            start = 1;
                            mnemonics = opcodeMnemonic_DD;
                        }
                        break;

                    case 0xFD:
                        if (opcodeBytes[1] == 0xCB)
                        {
                            mnemonics = opcodeMnemonic_FDCB;
                            isDDFDCB = true;
                        }
                        else
                        {
                            start = 1;
                            mnemonics = opcodeMnemonic_FD;
                        }
                        break;
                }

                if (mnemonics == null) return "(unimplemented)";

                if (!isDDFDCB)
                {
                    switch (opcodeBytes.Length - start)
                    {
                        case 1: return mnemonics[opcodeBytes[start]];
                        case 2: return string.Format(mnemonics[opcodeBytes[start]], opcodeBytes[start + 1]);
                        case 3: return string.Format(mnemonics[opcodeBytes[start]], (opcodeBytes[start + 2] << 8 | opcodeBytes[start + 1]));
                        default: return string.Empty;
                    }
                }
                else
                {
                    return string.Format(mnemonics[opcodeBytes[3]], opcodeBytes[2]);
                }
            }
        }
    }
}
