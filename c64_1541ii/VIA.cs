
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

using IO;

namespace DiskDrive
{
	public class VIA : Memory.MemoryMappedDevice, Clock.ClockOp, State.IDeviceState
	{
		public enum Registers
		{
			ORB, ORA, DDRB, DDRA,
			T1_CL, T1_CH, T1_LL, T1_LH, T2_CL, T2_CH,
			SR,
			ACR, FCR, IFR, IER,
			ORA_NH
		}

		enum FCR
		{
			CA1 = 0x01,
			CA2_MI = 0x02,
			CA2_OL = 0x02,
			CA2_TR = 0x04,
			CA2_OM = 0x04,
			CA2_IO = 0x08,
			CA2 = 0x0e,

			CB1 = 0x01,
			CB2_MI = 0x20,
			CB2_OL = 0x20,
			CB2_TR = 0x40,
			CB2_OM = 0x40,
			CB2_IO = 0x80,
			CB2 = 0xe0,
		}

		enum ACR
		{
			PA_L = 0x01,
			PB_L = 0x02,
			SRC = 0x1c,
			T2_IM = 0x20,
			T1_FM = 0x40,
			T1_OM = 0x80
		}

		enum IR
		{
			CA2 = 0x01,
			CA1 = 0x02,
			SR = 0x04,
			CB2 = 0x08,
			CB1 = 0x10,
			T2 = 0x20,
			T1 = 0x40,
			IRQ = 0x80
		}

		public VIA(ushort address, ushort size, Irq irqLine)
			: base(address, size) { _irqLine = irqLine; }

		private Irq _irqLine = null;

		private byte _functionControlRegister;
		private byte _auxiliaryControlRegister;

		private IOPort _portA = new IOPort(0);
		public IOPort PortA { get { return _portA; } }

		private bool _latchPortA;
		public bool LatchPortA { get { return _latchPortA; } }

		private byte _latchedValueA;
		public byte LatchedValueA { get { return _latchedValueA; } }

		private bool _ca1;
		public bool CA1
		{
			get { return _ca1; }
			set
			{
				if (_ca1 != value && ((_functionControlRegister & (byte)FCR.CA1) != 0) == value)
				{
					_latchedValueA = (byte)(_portA.Input | (_portA.Output & _portA.Direction));

					if ((_functionControlRegister & (byte)(FCR.CA2_IO | FCR.CA2_OM | FCR.CB2_OL)) == (byte)FCR.CA2_IO)
						_ca2 = false;

					SetInterrupt(IR.CA1);
				}

				_ca1 = value;
			}
		}

		private bool _ca2;
		public bool CA2
		{
			get { return _ca2; }
			set
			{
				if ((_functionControlRegister & (byte)FCR.CA2_IO) == 0)
				{
					if (_ca2 != value && ((_functionControlRegister & (byte)FCR.CA2_TR) != 0) == value)
						SetInterrupt(IR.CA2);

					_ca2 = value;
				}
			}
		}

		private IOPort _portB = new IOPort(0);
		public IOPort PortB { get { return _portB; } }

		private bool _latchPortB;
		public bool LatchPortB { get { return _latchPortB; } }

		private byte _latchedValueB;
		public byte LatchedValueB { get { return _latchedValueB; } }

		private bool _cb1;
		public bool CB1
		{
			get { return _cb1; }
			set
			{
				if (_cb1 != value && ((_functionControlRegister & (byte)FCR.CB1) != 0) == value)
				{
					_latchedValueB = (byte)(_portB.Input | (_portB.Output & _portB.Direction));

					if ((_functionControlRegister & (byte)(FCR.CB2_IO | FCR.CB2_OM | FCR.CB2_OL)) == (byte)FCR.CB2_IO)
						_cb2 = false;

					SetInterrupt(IR.CB1);
				}

				_cb1 = value;
			}
		}

		private bool _pulseCA2;

		private bool _cb2;
		public bool CB2
		{
			get { return _cb2; }
			set
			{
				if ((_functionControlRegister & (byte)FCR.CB2_IO) == 0)
				{
					if (_cb2 != value && ((_functionControlRegister & (byte)FCR.CB2_TR) != 0) == value)
						SetInterrupt(IR.CB2);

					_cb2 = value;
				}
			}
		}

		private bool _pulseCB2;

		private ushort _t1Counter;
		private ushort _t1Latch;
		private bool _t1Count;
		private bool _t1OutPB;
		private bool _t1FreeRun;

		private ushort _t2Counter;
		private byte _t2Latch;
		private bool _t2Count;
		private bool _t2InPB;

		private byte _interruptFlagRegister;
		public byte InterruptFlagRegister { get { return _interruptFlagRegister; } }

		private byte _interruptEnabledRegister;
		public byte InterruptEnabledRegister { get { return _interruptEnabledRegister; } }

		public void Execute(Clock.Clock clock, byte cycle)
		{
			if (_ca2 && _pulseCA2)
				_ca2 = false;

			if (_cb2 && _pulseCB2)
				_cb2 = false;

			if (_t1Count && --_t1Counter == 0)
			{
				if (_t1FreeRun)
					_t1Counter = _t1Latch;
				else
					_t1Count = false;

				SetInterrupt(IR.T1);
			}

			if (_t2Count /*&& (!_t2InPB || (_portB.Pins & 0x40) == 0)*/)
			{
				if (--_t2Counter == 0)
				{
					_t2Count = false;
					SetInterrupt(IR.T2);
				}
			}
		}

		public void WriteToStateFile(C64Interfaces.IFile stateFile) { }

		public override byte Read(ushort address)
		{
			address &= 0x0f;
			switch (address)
			{
				case (ushort)Registers.ORB:
					return ReadPortB();

				case (ushort)Registers.ORA:
					return ReadPortA(true);

				case (ushort)Registers.DDRB:
					return _portB.Direction;

				case (ushort)Registers.DDRA:
					return _portA.Direction;

				case (ushort)Registers.T1_CL:
					ClearInterrupt(IR.T1);
					return (byte)_t1Counter;

				case (ushort)Registers.T1_CH:
					return (byte)(_t1Counter >> 8);

				case (ushort)Registers.T1_LL:
					return (byte)_t1Latch;

				case (ushort)Registers.T1_LH:
					return (byte)(_t1Latch >> 8);

				case (ushort)Registers.T2_CL:
					ClearInterrupt(IR.T2);
					return (byte)_t2Counter;

				case (ushort)Registers.T2_CH:
					return (byte)(_t2Counter >> 8);

				case (ushort)Registers.SR:
					break;

				case (ushort)Registers.ACR:
					return _auxiliaryControlRegister;

				case (ushort)Registers.FCR:
					return _functionControlRegister;

				case (ushort)Registers.IFR:
					return _interruptFlagRegister;

				case (ushort)Registers.IER:
					return _interruptEnabledRegister;

				case (ushort)Registers.ORA_NH:
					return ReadPortA(false);
			}

			return 0;
		}

		public override void Write(ushort address, byte value)
		{
			address &= 0x0f;
			byte clear;

			switch (address)
			{
				case (ushort)Registers.ORB:
					WritePortB(value);
					break;

				case (ushort)Registers.ORA:
					WritePortA(value, true);
					break;

				case (ushort)Registers.DDRB:
					_portB.Direction = value;
					break;

				case (ushort)Registers.DDRA:
					_portA.Direction = value;
					break;

				case (ushort)Registers.T1_CL:
					_t1Latch = (ushort)((_t1Latch & 0xff00) | value);
					break;

				case (ushort)Registers.T1_CH:
					_t1Latch = (ushort)((_t1Latch & 0xff) | (value << 8));
					_t1Counter = _t1Latch;
					_t1Count = true;

					ClearInterrupt(IR.T1);
					break;

				case (ushort)Registers.T1_LL:
					_t1Latch = (ushort)((_t1Latch & 0xff00) | value);
					break;

				case (ushort)Registers.T1_LH:
					_t1Latch = (ushort)((_t1Latch & 0xff) | (value << 8));
					ClearInterrupt(IR.T1);
					break;

				case (ushort)Registers.T2_CL:
					_t2Latch = value;
					ClearInterrupt(IR.T2);
					break;

				case (ushort)Registers.T2_CH:
					_t2Counter = (ushort)((value << 8) | _t1Latch);
					_t2Count = true;
					ClearInterrupt(IR.T2);
					break;

				case (ushort)Registers.SR:
					break;

				case (ushort)Registers.ACR:
					_auxiliaryControlRegister = value;

					_latchPortA = (value & (byte)ACR.PA_L) != 0;
					_latchPortB = (value & (byte)ACR.PB_L) != 0;

					_t1FreeRun = (value & (byte)ACR.T1_FM) != 0;
					_t1OutPB = (value & (byte)ACR.T1_OM) != 0;
					_t2InPB = (value & (byte)ACR.T2_IM) != 0;
					break;

				case (ushort)Registers.FCR:
					_functionControlRegister = value;

					if ((value & (byte)FCR.CA2_IO) == (byte)FCR.CA2_IO)
					{
						_pulseCA2 = (value & (byte)(FCR.CA2_OM | FCR.CA2_OL)) == (byte)FCR.CA2_OL;
						_ca2 = !_pulseCA2 && (value & (byte)(FCR.CA2_OM | FCR.CA2_OL)) == (byte)(FCR.CA2_OM | FCR.CA2_OL);
					}

					if ((value & (byte)FCR.CB2_IO) == (byte)FCR.CB2_IO)
					{
						_pulseCB2 = (value & (byte)(FCR.CB2_OM | FCR.CB2_OL)) == (byte)FCR.CB2_OL;
						_cb2 = !_pulseCB2 && (value & (byte)(FCR.CB2_OM | FCR.CB2_OL)) == (byte)(FCR.CB2_OM | FCR.CB2_OL);
					}

					break;

				case (ushort)Registers.IFR:
					clear = (byte)IR.IRQ;
					ClearInterrupt((IR)(value & ~clear));
					break;

				case (ushort)Registers.IER:

					if ((value & (byte)IR.IRQ) != 0)
					{
						clear = (byte)IR.IRQ;
						_interruptEnabledRegister |= (byte)(value & ~clear);
					}
					else
						_interruptEnabledRegister &= value;

					byte newInnteruptState = (byte)(_interruptFlagRegister & (_interruptEnabledRegister | (byte)IR.IRQ));

					if (newInnteruptState == (byte)IR.IRQ)
					{
						clear = (byte)IR.IRQ;
						_interruptFlagRegister &= (byte)~clear;

						_irqLine.Lower();
					}
					else if (newInnteruptState != 0 && (_interruptFlagRegister & (byte)IR.IRQ) == 0)
					{
						_interruptFlagRegister |= (byte)IR.IRQ;

						_irqLine.Raise();
					}

					break;

				case (ushort)Registers.ORA_NH:
					WritePortA(value, false);
					break;
			}
		}

		public Clock.ClockEntry CreateOps()
		{
			Clock.ClockEntry op = new Clock.ClockEntry(this);
			op.Next = op;

			return op;
		}

		private byte ReadPortA(bool handshake)
		{
			byte value = (_latchPortA && (_interruptFlagRegister & (byte)IR.CA1) != 0) ? _latchedValueA : (byte)(_portA.Input | (_portA.Output & _portA.Direction));

			if (handshake)
				ClearInterrupt(IR.CA1 | IR.CA2);

			return value;
		}

		private void WritePortA(byte value, bool handshake)
		{
			if (handshake)
			{
				ClearInterrupt(IR.CA1 | IR.CA2);

				if ((_functionControlRegister & (byte)(FCR.CA2_IO | FCR.CA2_OM)) == (byte)(FCR.CA2_IO | FCR.CA2_OM))
					_ca2 = true;
			}

			_portA.Output = value;
		}

		private byte ReadPortB()
		{
			byte value = (_latchPortB && (_interruptFlagRegister & (byte)IR.CB1) != 0) ? _latchedValueB : (byte)(_portB.Input | (_portB.Output & _portB.Direction));

			ClearInterrupt(IR.CB1 | IR.CB2);

			return value;
		}

		private void WritePortB(byte value)
		{
			ClearInterrupt(IR.CB1 | IR.CB2);

			if ((_functionControlRegister & (byte)(FCR.CB2_IO | FCR.CB2_OM)) == (byte)(FCR.CB2_IO | FCR.CB2_OM))
				_cb2 = true;

			_portB.Output = value;
		}

		private void SetInterrupt(IR flag)
		{
			_interruptFlagRegister |= (byte)flag;

			if ((_interruptEnabledRegister & (byte)flag) != 0)
			{
				if ((_interruptFlagRegister & (byte)IR.IRQ) == 0)
				{
					_interruptFlagRegister |= (byte)IR.IRQ;

					_irqLine.Raise();
				}
			}
		}

		private void ClearInterrupt(IR flag)
		{
			_interruptFlagRegister &= (byte)~flag;

			if ((_interruptFlagRegister & (_interruptEnabledRegister | (byte)IR.IRQ)) == (byte)IR.IRQ)
			{
				byte clear = (byte)IR.IRQ;
				_interruptFlagRegister &= (byte)~clear;

				_irqLine.Lower();
			}
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_portA.ReadDeviceState(stateFile);
			_portB.ReadDeviceState(stateFile);

			_latchPortA = stateFile.ReadBool();
			_latchedValueA = stateFile.ReadByte();
			_ca1 = stateFile.ReadBool();
			_ca2 = stateFile.ReadBool();
			_latchPortB = stateFile.ReadBool();
			_latchedValueB = stateFile.ReadByte();
			_cb1 = stateFile.ReadBool();
			_pulseCA2 = stateFile.ReadBool();
			_cb2 = stateFile.ReadBool();
			_pulseCB2 = stateFile.ReadBool();

			_t1Counter = stateFile.ReadWord();
			_t1Latch = stateFile.ReadWord();
			_t1Count = stateFile.ReadBool();
			_t1OutPB = stateFile.ReadBool();
			_t1FreeRun = stateFile.ReadBool();
			_t2Counter = stateFile.ReadWord();
			_t2Latch = stateFile.ReadByte();
			_t2Count = stateFile.ReadBool();
			_t2InPB = stateFile.ReadBool();

			_functionControlRegister = stateFile.ReadByte();
			_auxiliaryControlRegister = stateFile.ReadByte();
			_interruptFlagRegister = stateFile.ReadByte();
			_interruptEnabledRegister = stateFile.ReadByte();
		}

		public void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			_portA.WriteDeviceState(stateFile);
			_portB.WriteDeviceState(stateFile);

			stateFile.Write(_latchPortA);
			stateFile.Write(_latchedValueA);
			stateFile.Write(_ca1);
			stateFile.Write(_ca2);
			stateFile.Write(_latchPortB);
			stateFile.Write(_latchedValueB);
			stateFile.Write(_cb1);
			stateFile.Write(_pulseCA2);
			stateFile.Write(_cb2);
			stateFile.Write(_pulseCB2);

			stateFile.Write(_t1Counter);
			stateFile.Write(_t1Latch);
			stateFile.Write(_t1Count);
			stateFile.Write(_t1OutPB);
			stateFile.Write(_t1FreeRun);
			stateFile.Write(_t2Counter);
			stateFile.Write(_t2Latch);
			stateFile.Write(_t2Count);
			stateFile.Write(_t2InPB);

			stateFile.Write(_functionControlRegister);
			stateFile.Write(_auxiliaryControlRegister);
			stateFile.Write(_interruptFlagRegister);
			stateFile.Write(_interruptEnabledRegister);
		}
	}

}
