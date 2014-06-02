
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

using System;

namespace CPU
{
	public abstract class IRegister<TYPE>
	{
		public abstract void Reset();

		public abstract TYPE Value
		{
			get;
			set;
		}
	}

	public abstract class Register<TYPE> : IRegister<TYPE>
	{
		protected TYPE _value;
		public override TYPE Value
		{
			get { return _value; }
			set { _value = value; }
		}
	}

	public class GpRegister : Register<byte>
	{
		public override void Reset() { _value = 0; }

		public bool IsNegative { get { return (_value & 0x80) != 0; } }
		public bool IsZero { get { return _value == 0; } }
	}

	public class StackRegister : Register<ushort>
	{
		public override void Reset() { _value = 0x1ff; }

		public override ushort Value { set { _value = (ushort)(value & 0xff | 0x100); } }
	}

	public class ProgramCounter : Register<ushort>
	{
		public override void Reset() { _value = 0; }

		public byte PCH { get { return (byte)(_value >> 8); } }
		public byte PCL { get { return (byte)(_value); } }

		public ushort Next() { return ++_value; }
	}

	public class StatusRegister : Register<byte>
	{
		public enum Bits
		{
			Negative = 7,
			Overflow = 6,
			Reserved = 5,
			BreakCmd = 4,
			Decimal = 3,
			IrqMask = 2,
			Zero = 1,
			Carry = 0
		}

		public override void Reset() { _value = (byte)Bits.IrqMask; }

		public bool Negative
		{
			get { return GetState(Bits.Negative); }
			set { SetState(Bits.Negative, value); }
		}

		public bool Overflow
		{
			get { return GetState(Bits.Overflow); }
			set { SetState(Bits.Overflow, value); }
		}

		public bool BreakCmd
		{
			get { return GetState(Bits.BreakCmd); }
			set { SetState(Bits.BreakCmd, value); }
		}

		public bool Decimal
		{
			get { return GetState(Bits.Decimal); }
			set { SetState(Bits.Decimal, value); }
		}

		public bool IrqMask
		{
			get { return GetState(Bits.IrqMask); }
			set { SetState(Bits.IrqMask, value); }
		}

		public bool Zero
		{
			get { return GetState(Bits.Zero); }
			set { SetState(Bits.Zero, value); }
		}

		public bool Carry
		{
			get { return GetState(Bits.Carry); }
			set { SetState(Bits.Carry, value); }
		}

		public byte CarryValue { get { return (byte)Convert.ToUInt16(Carry); } }

		private byte GetMask(Bits bit) { return (byte)(1 << (byte)bit); }
		private bool GetState(Bits bit) { return (_value & GetMask(bit)) != 0; }
		private void SetState(Bits bit, bool state) { _value = (byte)(_value & ~GetMask(bit) | ((byte)Convert.ToUInt16(state) << (byte)bit)); }
	}

	public class CPUState
	{
		private GpRegister _a = new GpRegister();
		public GpRegister A { get { return _a; } }

		private GpRegister _y = new GpRegister();
		public GpRegister Y { get { return _y; } }

		private GpRegister _x = new GpRegister();
		public GpRegister X { get { return _x; } }

		private ProgramCounter _pc = new ProgramCounter();
		public ProgramCounter PC { get { return _pc; } }

		private StackRegister _s = new StackRegister();
		public StackRegister S { get { return _s; } }

		private StatusRegister _p = new StatusRegister();
		public StatusRegister P { get { return _p; } }
	}

}