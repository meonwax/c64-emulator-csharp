
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
	public interface Instruction
	{
		void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle);
	}

	class PseudoHLT : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.PC.Value = cpu.Target.Address;
			clock.Halt();
		}
	}

	class ADC : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte mem = cpu.Target.Read();
			byte a = cpu.State.A.Value;
			int tmp = 0, vCheck = 0;

			if (cpu.State.P.Decimal)
			{
				tmp = (a & 0x0f) + (mem & 0x0f) + cpu.State.P.CarryValue;
				if (tmp > 0x09)
					tmp += 0x06;

				tmp += (a & 0xf0) + (mem & 0xf0);
				vCheck = tmp;

				if ((tmp & 0x1f0) > 0x90)
					tmp += 0x60;

				cpu.State.P.Carry = (tmp & 0xff0) > 0xf0;
			}
			else
			{
				vCheck = tmp = a + mem + cpu.State.P.CarryValue;
				cpu.State.P.Carry = (tmp & 0xff00) != 0;
			}

			cpu.State.A.Value = (byte)tmp;

			cpu.State.P.Overflow = ((a ^ mem) & 0x80) == 0 && ((a ^ vCheck) & 0x80) != 0; //(mem & 0x80) == (a & 0x80) && (vCheck  & 0x80) != (a & 0x80);
			cpu.State.P.Zero = cpu.State.A.IsZero;
			cpu.State.P.Negative = cpu.State.A.IsNegative;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class AND : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.A.Value &= cpu.Target.Read();

			cpu.State.P.Zero = cpu.State.A.IsZero;
			cpu.State.P.Negative = cpu.State.A.IsNegative;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class ASL : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			int tmp = cpu.Target.Read() << 1;
			cpu.Result = (byte)tmp;

			cpu.State.P.Carry = (tmp & 0xff00) != 0;
			cpu.State.P.Zero = (tmp & 0xff) == 0;
			cpu.State.P.Negative = (tmp & 0x80) != 0;
		}
	}

	class BCC : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte prolong = 0;
			if (!cpu.State.P.Carry)
			{
				prolong++;
				ushort newAddress = cpu.Target.Address;
				if ((newAddress & 0xff00) != (cpu.State.PC.Value & 0xff00))
					prolong++;

				cpu.State.PC.Value = newAddress;
			}

			clock.Prolong(prolong, cpu.Phase);
		}
	}

	class BCS : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte prolong = 0;
			if (cpu.State.P.Carry)
			{
				prolong++;
				ushort newAddress = cpu.Target.Address;
				if ((newAddress & 0xff00) != (cpu.State.PC.Value & 0xff00))
					prolong++;

				cpu.State.PC.Value = newAddress;
			}

			clock.Prolong(prolong, cpu.Phase);
		}
	}

	class BEQ : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte prolong = 0;
			if (cpu.State.P.Zero)
			{
				prolong++;
				ushort newAddress = cpu.Target.Address;
				if ((newAddress & 0xff00) != (cpu.State.PC.Value & 0xff00))
					prolong++;

				cpu.State.PC.Value = newAddress;
			}

			clock.Prolong(prolong, cpu.Phase);
		}
	}

	class BIT : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte mem = cpu.Target.Read();
			int tmp = cpu.State.A.Value & mem;

			cpu.State.P.Zero = (tmp & 0xff) == 0;
			cpu.State.P.Negative = (mem & 0x80) != 0;
			cpu.State.P.Overflow = (mem & 0x40) != 0;
		}
	}

	class BMI : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte prolong = 0;
			if (cpu.State.P.Negative)
			{
				prolong++;
				ushort newAddress = cpu.Target.Address;
				if ((newAddress & 0xff00) != (cpu.State.PC.Value & 0xff00))
					prolong++;

				cpu.State.PC.Value = newAddress;
			}

			clock.Prolong(prolong, cpu.Phase);
		}
	}

	class BNE : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte prolong = 0;
			if (!cpu.State.P.Zero)
			{
				prolong++;
				ushort newAddress = cpu.Target.Address;
				if ((newAddress & 0xff00) != (cpu.State.PC.Value & 0xff00))
					prolong++;

				cpu.State.PC.Value = newAddress;
			}

			clock.Prolong(prolong, cpu.Phase);
		}
	}

	class BPL : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte prolong = 0;
			if (!cpu.State.P.Negative)
			{
				prolong++;
				ushort newAddress = cpu.Target.Address;
				if ((newAddress & 0xff00) != (cpu.State.PC.Value & 0xff00))
					prolong++;

				cpu.State.PC.Value = newAddress;
			}

			clock.Prolong(prolong, cpu.Phase);
		}
	}

	class BRK : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Memory.Write(cpu.State.S.Value, cpu.State.PC.PCH);
				cpu.State.S.Value--;
			}
			else if (cycle == 1)
			{
				cpu.Memory.Write(cpu.State.S.Value, cpu.State.PC.PCL);
				cpu.State.S.Value--;
			}
			else if (cycle == 2)
			{
				cpu.Memory.Write(cpu.State.S.Value, cpu.State.P.Value);
				cpu.State.S.Value--;
			}
			else if (cycle == 3)
				cpu.Result = cpu.Memory.Read(0xfffe);
			else if (cycle == 4)
			{
				cpu.State.PC.Value = (ushort)((cpu.Memory.Read(0xffff) << 8) | cpu.Result);
				cpu.State.P.BreakCmd = true;
			}
		}
	}

	class BVC : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte prolong = 0;
			if (!cpu.State.P.Overflow)
			{
				prolong++;
				ushort newAddress = cpu.Target.Address;
				if ((newAddress & 0xff00) != (cpu.State.PC.Value & 0xff00))
					prolong++;

				cpu.State.PC.Value = newAddress;
			}

			clock.Prolong(prolong, cpu.Phase);
		}
	}

	class BVS : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte prolong = 0;
			if (cpu.State.P.Overflow)
			{
				prolong++;
				ushort newAddress = cpu.Target.Address;
				if ((newAddress & 0xff00) != (cpu.State.PC.Value & 0xff00))
					prolong++;

				cpu.State.PC.Value = newAddress;
			}

			clock.Prolong(prolong, cpu.Phase);
		}
	}

	class CLC : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.P.Carry = false;
		}
	}

	class CLD : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.P.Decimal = false;
		}
	}

	class CLI : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.P.IrqMask = false;
		}
	}

	class CLV : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.P.Overflow = false;
		}
	}

	class CMP : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			uint tmp = (uint)(cpu.State.A.Value - cpu.Target.Read());

			cpu.State.P.Carry = (tmp & 0xff00) == 0;
			cpu.State.P.Zero = (tmp & 0xff) == 0;
			cpu.State.P.Negative = (tmp & 0x80) != 0;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class CPX : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			int tmp = cpu.State.X.Value - cpu.Target.Read();

			cpu.State.P.Carry = (tmp & 0xff00) == 0;
			cpu.State.P.Zero = (tmp & 0xff) == 0;
			cpu.State.P.Negative = (tmp & 0x80) != 0;
		}
	}

	class CPY : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			int tmp = cpu.State.Y.Value - cpu.Target.Read();

			cpu.State.P.Carry = (tmp & 0xff00) == 0;
			cpu.State.P.Zero = (tmp & 0xff) == 0;
			cpu.State.P.Negative = (tmp & 0x80) != 0;
		}
	}

	class DEC : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte tmp = (byte)(cpu.Target.Read() - 1);
			cpu.Result = tmp;

			cpu.State.P.Zero = tmp == 0;
			cpu.State.P.Negative = (tmp & 0x80) != 0;
		}
	}

	class DEX : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.X.Value--;

			cpu.State.P.Zero = cpu.State.X.IsZero;
			cpu.State.P.Negative = cpu.State.X.IsNegative;
		}
	}

	class DEY : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.Y.Value--;

			cpu.State.P.Zero = cpu.State.Y.IsZero;
			cpu.State.P.Negative = cpu.State.Y.IsNegative;
		}
	}

	class EOR : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.A.Value ^= cpu.Target.Read();

			cpu.State.P.Zero = cpu.State.A.IsZero;
			cpu.State.P.Negative = cpu.State.A.IsNegative;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class INC : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte tmp = (byte)(cpu.Target.Read() + 1);
			cpu.Result = tmp;

			cpu.State.P.Zero = tmp == 0;
			cpu.State.P.Negative = (tmp & 0x80) != 0;
		}
	}

	class INX : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.X.Value++;

			cpu.State.P.Zero = cpu.State.X.IsZero;
			cpu.State.P.Negative = cpu.State.X.IsNegative;
		}
	}

	class INY : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.Y.Value++;

			cpu.State.P.Zero = cpu.State.Y.IsZero;
			cpu.State.P.Negative = cpu.State.Y.IsNegative;
		}
	}

	class JMP : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.PC.Value = cpu.Target.Address;
		}
	}

	class JSR : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Memory.Write(cpu.State.S.Value, (byte)((cpu.State.PC.Value - 1) >> 8));
				cpu.State.S.Value--;
			}
			else if (cycle == 1)
			{
				cpu.Memory.Write(cpu.State.S.Value, (byte)((cpu.State.PC.Value - 1)));
				cpu.State.S.Value--;
			}
			else
				cpu.State.PC.Value = cpu.Target.Address;
		}
	}

	class LDA : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.A.Value = cpu.Target.Read();

			cpu.State.P.Zero = cpu.State.A.IsZero;
			cpu.State.P.Negative = cpu.State.A.IsNegative;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class LDX : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.X.Value = cpu.Target.Read();

			cpu.State.P.Zero = cpu.State.X.IsZero;
			cpu.State.P.Negative = cpu.State.X.IsNegative;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class LDY : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.Y.Value = cpu.Target.Read();

			cpu.State.P.Zero = cpu.State.Y.IsZero;
			cpu.State.P.Negative = cpu.State.Y.IsNegative;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class LSR : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte value = cpu.Target.Read();

			cpu.State.P.Carry = (value & 0x01) != 0;

			value >>= 1;
			cpu.Result = value;

			cpu.State.P.Zero = (value & 0xff) == 0;
			cpu.State.P.Negative = (value & 0x80) != 0;
		}
	}

	class NOP : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
		}
	}

	class ORA : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			int tmp = cpu.State.A.Value | cpu.Target.Read();
			cpu.State.A.Value = (byte)tmp;

			cpu.State.P.Zero = cpu.State.A.IsZero;
			cpu.State.P.Negative = cpu.State.A.IsNegative;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class PHA : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			if (cycle == 1)
			{
				cpu.Memory.Write(cpu.State.S.Value, cpu.State.A.Value);
				cpu.State.S.Value--;
			}
		}
	}

	class PHP : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			if (cycle == 1)
			{
				cpu.Memory.Write(cpu.State.S.Value, cpu.State.P.Value);
				cpu.State.S.Value--;
			}
		}
	}

	class PLA : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.State.S.Value++;
				cpu.State.A.Value = cpu.Memory.Read(cpu.State.S.Value);

				cpu.State.P.Zero = cpu.State.A.IsZero;
				cpu.State.P.Negative = cpu.State.A.IsNegative;
			}
		}
	}

	class PLP : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.State.S.Value++;
				cpu.State.P.Value = cpu.Memory.Read(cpu.State.S.Value);
			}
		}
	}

	class ROL : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte carry = cpu.State.P.CarryValue;

			int tmp = (cpu.Target.Read() << 1) | carry;
			cpu.Result = (byte)tmp;

			cpu.State.P.Carry = (tmp & 0xff00) != 0;
			cpu.State.P.Zero = (tmp & 0xff) == 0;
			cpu.State.P.Negative = (tmp & 0x80) != 0;
		}
	}

	class ROR : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte carry = cpu.State.P.CarryValue;

			byte tmp = cpu.Target.Read();

			cpu.State.P.Carry = (tmp & 0x01) != 0;

			tmp = (byte)((tmp >> 1) | (carry << 7));
			cpu.Result = (byte)tmp;

			cpu.State.P.Zero = (tmp & 0xff) == 0;
			cpu.State.P.Negative = (tmp & 0x80) != 0;
		}
	}

	class RTI : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.State.S.Value++;
				cpu.State.P.Value = cpu.Memory.Read(cpu.State.S.Value);
			}
			else if (cycle == 1)
			{
				cpu.State.S.Value++;
				cpu.Result = cpu.Memory.Read(cpu.State.S.Value);
			}
			else if (cycle == 2)
			{
				cpu.State.S.Value++;
				cpu.State.PC.Value = (ushort)(cpu.Result | (cpu.Memory.Read(cpu.State.S.Value) << 8));
			}
		}
	}

	class RTS : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			if (cycle == 1)
			{
				cpu.State.S.Value++;
				cpu.Result = cpu.Memory.Read(cpu.State.S.Value);
			}
			else if (cycle == 2)
			{
				cpu.State.S.Value++;
				cpu.State.PC.Value = (ushort)((cpu.Result | (cpu.Memory.Read(cpu.State.S.Value) << 8)) + 1);
			}
		}
	}

	class SBC : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			byte mem = cpu.Target.Read();
			byte a = cpu.State.A.Value;
			int tmp = 0, vCheck = 0;

			if (cpu.State.P.Decimal)
			{
				tmp = (a & 0x0f) - (mem & 0x0f) - (1 - cpu.State.P.CarryValue);
				tmp = (tmp & 0x10) != 0 ? ((tmp - 6) & 0x0f) | ((a & 0xf0) - (mem & 0xf0) - 0x10) : tmp | ((a & 0xf0) - (mem & 0xf0));

				vCheck = tmp;

				if ((tmp & 0xff00) != 0)
					tmp -= 0x60;

				cpu.State.P.Carry = (tmp & 0xff00) == 0;
			}
			else
			{
				vCheck = tmp = a - mem - (1 - cpu.State.P.CarryValue);
				cpu.State.P.Carry = (tmp & 0xff00) == 0;
			}

			cpu.State.A.Value = (byte)tmp;

			cpu.State.P.Overflow = ((a ^ mem) & 0x80) != 0 && ((a ^ vCheck) & 0x80) != 0; //(mem & 0x80) != (a & 0x80) && (tmp & 0x80) != (a & 0x80);
			cpu.State.P.Zero = cpu.State.A.IsZero;
			cpu.State.P.Negative = cpu.State.A.IsNegative;

			if (cpu.Target.IsPageCrossed(cpu.State.PC.Value))
				clock.Prolong(pageCrossProlong, cpu.Phase);
		}
	}

	class SEC : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.P.Carry = true;
		}
	}

	class SED : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.P.Decimal = true;
		}
	}

	class SEI : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.P.IrqMask = true;
		}
	}

	class STA : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.Result = cpu.State.A.Value;
		}
	}

	class STX : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.Result = cpu.State.X.Value;
		}
	}

	class STY : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.Result = cpu.State.Y.Value;
		}
	}

	class TAX : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.X.Value = cpu.State.A.Value;

			cpu.State.P.Zero = cpu.State.X.IsZero;
			cpu.State.P.Negative = cpu.State.X.IsNegative;
		}
	}

	class TAY : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.Y.Value = cpu.State.A.Value;

			cpu.State.P.Zero = cpu.State.Y.IsZero;
			cpu.State.P.Negative = cpu.State.Y.IsNegative;
		}
	}

	class TSX : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.X.Value = (byte)cpu.State.S.Value;

			cpu.State.P.Zero = cpu.State.X.IsZero;
			cpu.State.P.Negative = cpu.State.X.IsNegative;
		}
	}

	class TXA : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.A.Value = cpu.State.X.Value;

			cpu.State.P.Zero = cpu.State.A.IsZero;
			cpu.State.P.Negative = cpu.State.A.IsNegative;
		}
	}

	class TXS : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.S.Value = cpu.State.X.Value;
		}
	}

	class TYA : Instruction
	{
		public virtual void Execute(Clock.Clock clock, MOS6502 cpu, byte pageCrossProlong, byte cycle)
		{
			cpu.State.A.Value = cpu.State.Y.Value;

			cpu.State.P.Zero = cpu.State.A.IsZero;
			cpu.State.P.Negative = cpu.State.A.IsNegative;
		}
	}

}