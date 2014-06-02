
/*
 * 
 * website: http://kataklinger.com/
 * e-mail: me[at]kataklinger.com
 * 
 */

/*
 * 
 * C#64 - Commodore 64 Emulator written in C#
 * Copyright (C) 2014  Mladen Janković
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

namespace CPU
{
	class DecodingTable
	{
		public class InstructionTiming
		{
			public byte _addressingTime;
			public byte _execTime;
			public byte _prolongOnPageCross;
			public sbyte _writeTime;

			public InstructionTiming()
			{
				_addressingTime = _execTime = _prolongOnPageCross = 0;
				_writeTime = -1;
			}

			public InstructionTiming(byte addressingTime, byte execTime)
			{
				_addressingTime = addressingTime;
				_execTime = execTime;
				_prolongOnPageCross = 0;
				_writeTime = -1;
			}

			public InstructionTiming(byte addressingTime, byte execTime, byte prolongOnPageCross)
			{
				_addressingTime = addressingTime;
				_execTime = execTime;
				_prolongOnPageCross = prolongOnPageCross;
				_writeTime = -1;
			}

			public InstructionTiming(byte addressingTime, byte execTime, byte prolongOnPageCross, sbyte writeTime)
			{
				_addressingTime = addressingTime;
				_execTime = execTime;
				_prolongOnPageCross = prolongOnPageCross;
				_writeTime = writeTime;
			}
		}

		public class Entry
		{
			public Instruction _instruction;
			public AddressingMode _addressing;
			public InstructionTiming _timing;

			public Entry()
			{
				_instruction = null;
				_addressing = null;
				_timing = new InstructionTiming();
			}

			public Entry(Instruction instruction, AddressingMode addressing, InstructionTiming timing)
			{
				_instruction = instruction;
				_addressing = addressing;
				_timing = timing;
			}
		}

		//public static int[] DecodingTimes = new int[256];
		//public static long LastCount;

		public static Entry[] Opcodes =
		{

			/*0x00*/ new Entry(new BRK(), new ImpAddressing(), new InstructionTiming(/*7*/ 0, 6)),
			/*0x01*/ new Entry(new ORA(), new IndXAddressing(), new InstructionTiming(/*6*/ 4, 1)),
			/*0x02*/ new Entry(new PseudoHLT(), new AbsAddressing(), new InstructionTiming(/*3*/ 2, 0)),
			/*0x03*/ new Entry(),
			/*0x04*/ new Entry(),
			/*0x05*/ new Entry(new ORA(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0x06*/ new Entry(new ASL(), new ZeroAddressing(), new InstructionTiming(/*6*/ 3, 1, 0, 1)),
			/*0x07*/ new Entry(),
			/*0x08*/ new Entry(new PHP(), new ImpAddressing(), new InstructionTiming(/*3*/ 0, 2)),
			/*0x09*/ new Entry(new ORA(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x0A*/ new Entry(new ASL(), new AccAddressing(), new InstructionTiming(/*2*/ 0, 1, 0, 0)),
			/*0x0B*/ new Entry(),
			/*0x0C*/ new Entry(),
			/*0x0D*/ new Entry(new ORA(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x0E*/ new Entry(new ASL(), new AbsAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0x0F*/ new Entry(),

			/*0x10*/ new Entry(new BPL(), new RelAddressing(), new InstructionTiming(/*2*/ 0, 1, 1)),
			/*0x11*/ new Entry(new ORA(), new IndYAddressing(), new InstructionTiming(/*5*/ 3, 1, 1)),
			/*0x12*/ new Entry(),
			/*0x13*/ new Entry(),
			/*0x14*/ new Entry(),
			/*0x15*/ new Entry(new ORA(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x16*/ new Entry(new ASL(), new ZeroXAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0x17*/ new Entry(),
			/*0x18*/ new Entry(new CLC(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x19*/ new Entry(new ORA(), new AbsYAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0x1A*/ new Entry(),
			/*0x1B*/ new Entry(),
			/*0x1C*/ new Entry(),
			/*0x1D*/ new Entry(new ORA(), new AbsXAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0x1E*/ new Entry(new ASL(), new AbsXAddressing(), new InstructionTiming(/*7*/ 2, 1, 0, 3)),
			/*0x1F*/ new Entry(),

			/*0x20*/ new Entry(new JSR(), new AbsAddressing(), new InstructionTiming(/*6*/ 2, 3)),
			/*0x21*/ new Entry(new AND(), new IndXAddressing(), new InstructionTiming(/*6*/ 4, 1)),
			/*0x22*/ new Entry(),
			/*0x23*/ new Entry(),
			/*0x24*/ new Entry(new BIT(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0x25*/ new Entry(new AND(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0x26*/ new Entry(new ROL(), new ZeroAddressing(), new InstructionTiming(/*5*/ 1, 1, 0, 2)),
			/*0x27*/ new Entry(),
			/*0x28*/ new Entry(new PLP(), new ImpAddressing(), new InstructionTiming(/*4*/ 0, 3)),
			/*0x29*/ new Entry(new AND(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x2A*/ new Entry(new ROL(), new AccAddressing(), new InstructionTiming(/*2*/ 0, 1, 0, 0)),
			/*0x2B*/ new Entry(),
			/*0x2C*/ new Entry(new BIT(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x2D*/ new Entry(new AND(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x2E*/ new Entry(new ROL(), new AbsAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0x2F*/ new Entry(),
			
			/*0x30*/ new Entry(new BMI(), new RelAddressing(), new InstructionTiming(/*2*/ 0, 1, 1)),
			/*0x31*/ new Entry(new AND(), new IndYAddressing(), new InstructionTiming(/*5*/ 3, 1, 1)),
			/*0x32*/ new Entry(),
			/*0x33*/ new Entry(),
			/*0x34*/ new Entry(),
			/*0x35*/ new Entry(new AND(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x36*/ new Entry(new ROL(), new ZeroXAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0x37*/ new Entry(),
			/*0x38*/ new Entry(new SEC(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x39*/ new Entry(new AND(), new AbsYAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0x3A*/ new Entry(),
			/*0x3B*/ new Entry(),
			/*0x3C*/ new Entry(),
			/*0x3D*/ new Entry(new AND(), new AbsXAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0x3E*/ new Entry(new ROL(), new AbsXAddressing(), new InstructionTiming(/*7*/ 2, 1, 0, 3)),
			/*0x3F*/ new Entry(),

			/*0x40*/ new Entry(new RTI(), new ImpAddressing(), new InstructionTiming(/*6*/ 0, 5)),
			/*0x41*/ new Entry(new EOR(), new IndXAddressing(), new InstructionTiming(/*6*/ 4, 1)),
			/*0x42*/ new Entry(),
			/*0x43*/ new Entry(),
			/*0x44*/ new Entry(),
			/*0x45*/ new Entry(new EOR(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0x46*/ new Entry(new LSR(), new ZeroAddressing(), new InstructionTiming(/*5*/ 1, 1, 0, 2)),
			/*0x47*/ new Entry(),
			/*0x48*/ new Entry(new PHA(), new ImpAddressing(), new InstructionTiming(/*3*/ 0, 2)),
			/*0x49*/ new Entry(new EOR(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x4A*/ new Entry(new LSR(), new AccAddressing(), new InstructionTiming(/*2*/ 0, 1, 0, 0)),
			/*0x4B*/ new Entry(),
			/*0x4C*/ new Entry(new JMP(), new AbsAddressing(), new InstructionTiming(/*3*/ 2, 0)),
			/*0x4D*/ new Entry(new EOR(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x4E*/ new Entry(new LSR(), new AbsAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0x4F*/ new Entry(),
			
			/*0x50*/ new Entry(new BVC(), new RelAddressing(), new InstructionTiming(/*2*/ 0, 1, 1)),
			/*0x51*/ new Entry(new EOR(), new IndYAddressing(), new InstructionTiming(/*5*/ 4, 1)),
			/*0x52*/ new Entry(),
			/*0x53*/ new Entry(),
			/*0x54*/ new Entry(),
			/*0x55*/ new Entry(new EOR(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x56*/ new Entry(new LSR(), new ZeroXAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0x57*/ new Entry(),
			/*0x58*/ new Entry(new CLI(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x59*/ new Entry(new EOR(), new AbsYAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0x5A*/ new Entry(),
			/*0x5B*/ new Entry(),
			/*0x5C*/ new Entry(),
			/*0x5D*/ new Entry(new EOR(), new AbsXAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0x5E*/ new Entry(new LSR(), new AbsXAddressing(), new InstructionTiming(/*7*/ 2, 1, 0, 3)),
			/*0x5F*/ new Entry(),

			/*0x60*/ new Entry(new RTS(), new ImpAddressing(), new InstructionTiming(/*6*/ 0, 5)),
			/*0x61*/ new Entry(new ADC(), new IndXAddressing(), new InstructionTiming(/*6*/ 4, 1)),
			/*0x62*/ new Entry(),
			/*0x63*/ new Entry(),
			/*0x64*/ new Entry(),
			/*0x65*/ new Entry(new ADC(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0x66*/ new Entry(new ROR(), new ZeroAddressing(), new InstructionTiming(/*5*/ 1, 1, 0, 2)),
			/*0x67*/ new Entry(),
			/*0x68*/ new Entry(new PLA(), new ImpAddressing(), new InstructionTiming(/*4*/ 0, 3)),
			/*0x69*/ new Entry(new ADC(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x6A*/ new Entry(new ROR(), new AccAddressing(), new InstructionTiming(/*2*/ 0, 1, 0, 0)),
			/*0x6B*/ new Entry(),
			/*0x6C*/ new Entry(new JMP(), new IndAddressing(), new InstructionTiming(/*5*/ 4, 0)),
			/*0x6D*/ new Entry(new ADC(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x6E*/ new Entry(new ROR(), new AbsAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0x6F*/ new Entry(),

			/*0x70*/ new Entry(new BVS(), new RelAddressing(),new InstructionTiming(/*2*/ 0, 1, 1)),
			/*0x71*/ new Entry(new ADC(), new IndYAddressing(), new InstructionTiming(/*5*/ 3, 1, 1)),
			/*0x72*/ new Entry(),
			/*0x73*/ new Entry(),
			/*0x74*/ new Entry(),
			/*0x75*/ new Entry(new ADC(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0x76*/ new Entry(new ROR(), new ZeroXAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0x77*/ new Entry(),
			/*0x78*/ new Entry(new SEI(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x79*/ new Entry(new ADC(), new AbsYAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0x7A*/ new Entry(),
			/*0x7B*/ new Entry(),
			/*0x7C*/ new Entry(),
			/*0x7D*/ new Entry(new ADC(), new AbsXAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0x7E*/ new Entry(new ROR(), new AbsXAddressing(), new InstructionTiming(/*7*/ 2, 1, 0, 3)),
			/*0x7F*/ new Entry(),

			/*0x80*/ new Entry(),
			/*0x81*/ new Entry(new STA(), new IndXAddressing(), new InstructionTiming(/*6*/ 4, 1, 0, 0)),
			/*0x82*/ new Entry(),
			/*0x83*/ new Entry(),
			/*0x84*/ new Entry(new STY(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1, 0, 0)),
			/*0x85*/ new Entry(new STA(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1, 0, 0)),
			/*0x86*/ new Entry(new STX(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1, 0, 0)),
			/*0x87*/ new Entry(),
			/*0x88*/ new Entry(new DEY(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x89*/ new Entry(),
			/*0x8A*/ new Entry(new TXA(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x8B*/ new Entry(),
			/*0x8C*/ new Entry(new STY(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1, 0, 0)),
			/*0x8D*/ new Entry(new STA(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1, 0, 0)),
			/*0x8E*/ new Entry(new STX(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1, 0, 0)),
			/*0x8F*/ new Entry(),

			/*0x90*/ new Entry(new BCC(), new RelAddressing(), new InstructionTiming(/*2*/ 0, 1, 1)),
			/*0x91*/ new Entry(new STA(), new IndYAddressing(), new InstructionTiming(/*6*/ 4, 1, 0, 0)),
			/*0x92*/ new Entry(),
			/*0x93*/ new Entry(),
			/*0x94*/ new Entry(new STY(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1, 0, 0)),
			/*0x95*/ new Entry(new STA(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1, 0, 0)),
			/*0x96*/ new Entry(new STX(), new ZeroYAddressing(), new InstructionTiming(/*4*/ 2, 1, 0, 0)),
			/*0x97*/ new Entry(),
			/*0x98*/ new Entry(new TYA(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x99*/ new Entry(new STA(), new AbsYAddressing(), new InstructionTiming(/*5*/ 2, 1, 0, 1)),
			/*0x9A*/ new Entry(new TXS(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0x9B*/ new Entry(),
			/*0x9C*/ new Entry(),
			/*0x9D*/ new Entry(new STA(), new AbsXAddressing(), new InstructionTiming(/*5*/ 2, 1, 0, 1)),
			/*0x9E*/ new Entry(),
			/*0x9F*/ new Entry(),

			/*0xA0*/ new Entry(new LDY(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xA1*/ new Entry(new LDA(), new IndXAddressing(), new InstructionTiming(/*6*/ 4, 1)),
			/*0xA2*/ new Entry(new LDX(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xA3*/ new Entry(),
			/*0xA4*/ new Entry(new LDY(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0xA5*/ new Entry(new LDA(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0xA6*/ new Entry(new LDX(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0xA7*/ new Entry(),
			/*0xA8*/ new Entry(new TAY(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xA9*/ new Entry(new LDA(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xAA*/ new Entry(new TAX(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xAB*/ new Entry(),
			/*0xAC*/ new Entry(new LDY(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xAD*/ new Entry(new LDA(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xAE*/ new Entry(new LDX(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xAF*/ new Entry(),

			/*0xB0*/ new Entry(new BCS(), new RelAddressing(), new InstructionTiming(/*2*/ 0, 1, 1)),
			/*0xB1*/ new Entry(new LDA(), new IndYAddressing(), new InstructionTiming(/*5*/ 3, 1, 1)),
			/*0xB2*/ new Entry(),
			/*0xB3*/ new Entry(),
			/*0xB4*/ new Entry(new LDY(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xB5*/ new Entry(new LDA(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xB6*/ new Entry(new LDX(), new ZeroYAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xB7*/ new Entry(),
			/*0xB8*/ new Entry(new CLV(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xB9*/ new Entry(new LDA(), new AbsYAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0xBA*/ new Entry(new TSX(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xBB*/ new Entry(),
			/*0xBC*/ new Entry(new LDY(), new AbsXAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0xBD*/ new Entry(new LDA(), new AbsXAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0xBE*/ new Entry(new LDX(), new AbsYAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0xBF*/ new Entry(),

			/*0xC0*/ new Entry(new CPY(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xC1*/ new Entry(new CMP(), new IndXAddressing(), new InstructionTiming(/*6*/ 4, 1)),
			/*0xC2*/ new Entry(),
			/*0xC3*/ new Entry(),
			/*0xC4*/ new Entry(new CPY(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0xC5*/ new Entry(new CMP(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0xC6*/ new Entry(new DEC(), new ZeroAddressing(), new InstructionTiming(/*5*/ 1, 1, 0, 2)),
			/*0xC7*/ new Entry(),
			/*0xC8*/ new Entry(new INY(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xC9*/ new Entry(new CMP(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xCA*/ new Entry(new DEX(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xCB*/ new Entry(),
			/*0xCC*/ new Entry(new CPY(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xCD*/ new Entry(new CMP(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xCE*/ new Entry(new DEC(), new AbsAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0xCF*/ new Entry(),

			/*0xD0*/ new Entry(new BNE(), new RelAddressing(), new InstructionTiming(/*2*/ 0, 1, 1)),
			/*0xD1*/ new Entry(new CMP(), new IndYAddressing(), new InstructionTiming(/*5*/ 3, 1, 1)),
			/*0xD2*/ new Entry(),
			/*0xD3*/ new Entry(),
			/*0xD4*/ new Entry(),
			/*0xD5*/ new Entry(new CMP(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xD6*/ new Entry(new DEC(), new ZeroXAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0xD7*/ new Entry(),
			/*0xD8*/ new Entry(new CLD(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xD9*/ new Entry(new CMP(), new AbsYAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0xDA*/ new Entry(),
			/*0xDB*/ new Entry(),
			/*0xDC*/ new Entry(),
			/*0xDD*/ new Entry(new CMP(), new AbsXAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0xDE*/ new Entry(new DEC(), new AbsXAddressing(), new InstructionTiming(/*7*/ 2, 1, 0, 3)),
			/*0xDF*/ new Entry(),

			/*0xE0*/ new Entry(new CPX(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xE1*/ new Entry(new SBC(), new IndXAddressing(), new InstructionTiming(/*6*/ 4, 1)),
			/*0xE2*/ new Entry(),
			/*0xE3*/ new Entry(),
			/*0xE4*/ new Entry(new CPX(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0xE5*/ new Entry(new SBC(), new ZeroAddressing(), new InstructionTiming(/*3*/ 1, 1)),
			/*0xE6*/ new Entry(new INC(), new ZeroAddressing(), new InstructionTiming(/*5*/ 1, 1, 0, 2)),
			/*0xE7*/ new Entry(),
			/*0xE8*/ new Entry(new INX(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xE9*/ new Entry(new SBC(), new ImmAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xEA*/ new Entry(new NOP(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xEB*/ new Entry(),
			/*0xEC*/ new Entry(new CPX(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xED*/ new Entry(new SBC(), new AbsAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xEE*/ new Entry(new INC(), new AbsAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0xEF*/ new Entry(),

			/*0xF0*/ new Entry(new BEQ(), new RelAddressing(), new InstructionTiming(/*2*/ 0, 1, 1)),
			/*0xF1*/ new Entry(new SBC(), new IndYAddressing(), new InstructionTiming(/*5*/ 3, 1, 1)),
			/*0xF2*/ new Entry(),
			/*0xF3*/ new Entry(),
			/*0xF4*/ new Entry(),
			/*0xF5*/ new Entry(new SBC(), new ZeroXAddressing(), new InstructionTiming(/*4*/ 2, 1)),
			/*0xF6*/ new Entry(new INC(), new ZeroXAddressing(), new InstructionTiming(/*6*/ 2, 1, 0, 2)),
			/*0xF7*/ new Entry(),
			/*0xF8*/ new Entry(new SED(), new ImpAddressing(), new InstructionTiming(/*2*/ 0, 1)),
			/*0xF9*/ new Entry(new SBC(), new AbsYAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0xFA*/ new Entry(),
			/*0xFB*/ new Entry(),
			/*0xFC*/ new Entry(),
			/*0xFD*/ new Entry(new SBC(), new AbsXAddressing(), new InstructionTiming(/*4*/ 2, 1, 1)),
			/*0xFE*/ new Entry(new INC(), new AbsXAddressing(), new InstructionTiming(/*7*/ 2, 1, 0, 3)),
			/*0xFF*/ new Entry()

		};
	}
}