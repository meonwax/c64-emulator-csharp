
namespace IO
{

	public class CIA : Memory.MemoryMappedDevice, Clock.ClockOp, State.IDeviceState
	{
		enum Registers
		{
			PA_D, PB_D, PA_DD, PB_DD,
			TA_L, TA_H, TB_L, TB_H,
			TOD_T, TOD_S, TOD_M, TOD_H,
			SERIAL,
			IR,
			TA_CR, TB_CR
		}

		enum TOD { TENTH, SECONDS, MINUTES, HOURS, AM_PM }

		enum TOD_R { CURRENT, ALARM, LATCH }

		enum TR { TA_C, TB_C, TA_L, TB_L, TA = 0, TB = 1 }

		enum TA_CR
		{
			ST = 0x01,
			PB = 0x02,
			OM = 0x04,
			RM = 0x08,
			LD = 0x10,
			IM = 0x20,
			SPM = 0x40,
			TDI = 0x80
		}

		enum TB_CR
		{
			ST = 0x01,
			PB = 0x02,
			OM = 0x04,
			RM = 0x08,
			LD = 0x10,
			IM = 0x60,
			TDA = 0x80
		}

		enum TIM
		{
			CLK = 0x00,
			CNT = 0x20,
			OWF = 0x40,
			OWF_CNT = 0x60
		}

		enum IR
		{
			TA = 0x01,
			TB = 0x02,
			TOD = 0x04,
			SERIAL = 0x08,
			FLAG = 0x10,
			REQ = 0x80,
			FILL = 0x80,
		}

		public CIA(ushort address, ushort size, Irq irqLine, byte writeOnlyPinsA, byte writeOnlyPinsB)
			: base(address, size)
		{
			_portA = new IOPort(writeOnlyPinsA);
			_portB = new IOPort(writeOnlyPinsB);

			_irqLine = irqLine;
		}

		private Irq _irqLine = null;

		private IOPort _portA;
		public IOPort PortA { get { return _portA; } }

		private IOPort _portB;
		public IOPort PortB { get { return _portB; } }

		private bool _pcState;
		public bool PCState { get { return _pcState; } }

		private bool _todPause = false;
		private bool _todLatch = false;
		private byte _todClkCnt = 0;
		private byte[] _todLimits = new byte[] { 1, 12, 60, 60, 10 };

		private byte[,] _tod = new byte[3, 5];

		private byte[] _interruptRegisters = new byte[2];

		private byte _serialRegister = 0;

		private bool _flag = false;

		private bool _cntState = false;
		private bool _cntEdge = false;

		public void RaiseCnt()
		{
			if (!_cntState)
				_cntEdge = true;

			_cntState = true;
		}

		public void LowerCnt() { _cntState = false; }

		public void RaiseFlag()
		{
			if (_flag)
				SetInterrupt(IR.FLAG);

			_flag = true;
		}

		public void LowerFlag() { _flag = false; }

		public void IncrementTod()
		{
			++_todClkCnt;

			byte clock = (byte)(_timerA._controlReg & (byte)TA_CR.TDI);
			if ((_todClkCnt == 5 && clock != 0) || (_todClkCnt == 6 && clock == 0))
			{
				_todClkCnt = 0;

				if (!_todPause)
				{
					bool alarm = true;
					byte lim = (byte)_todLimits.Length;
					for (byte carry = 1, i = 0; i < lim; i++)
					{
						byte value = _tod[(byte)TOD_R.CURRENT, i];

						value += carry;
						carry = 0;

						if (value == _todLimits[i])
						{
							value = 0;
							carry = 1;
						}

						_tod[(byte)TOD_R.CURRENT, i] = value;
						alarm = alarm && _tod[(byte)TOD_R.ALARM, i] == value;
					}

					if (alarm)
						SetInterrupt(IR.TOD);
				}
			}
		}

		private class TimerState : State.IDeviceState
		{
			public byte _controlReg;

			public bool _active;
			public byte _mode;
			public bool _oneTime;

			public byte _pin;
			public bool _output;
			public bool _pulsing;
			public bool _pulsed;

			public ushort _current;
			public ushort _latch;

			public IR _interruptFlag;

			public TimerState(byte pin, IR interruptFlag)
			{
				_pin = pin;
				_interruptFlag = interruptFlag;
			}

			public void ReadDeviceState(C64Interfaces.IFile stateFile)
			{
				_controlReg = stateFile.ReadByte();
				_active = stateFile.ReadBool();
				_mode = stateFile.ReadByte();
				_oneTime = stateFile.ReadBool();

				_output = stateFile.ReadBool();
				_pulsing = stateFile.ReadBool();
				_pulsed = stateFile.ReadBool();

				_current = stateFile.ReadWord();
				_latch = stateFile.ReadWord();
			}

			public void WriteDeviceState(C64Interfaces.IFile stateFile)
			{
				stateFile.Write(_controlReg);
				stateFile.Write(_active);
				stateFile.Write(_mode);
				stateFile.Write(_oneTime);

				stateFile.Write(_output);
				stateFile.Write(_pulsing);
				stateFile.Write(_pulsed);

				stateFile.Write(_current);
				stateFile.Write(_latch);
			}
		}

		private TimerState _timerA = new TimerState(0x40, IR.TA);
		private TimerState _timerB = new TimerState(0x80, IR.TB);

		private bool Timer(TimerState timer, bool timerOverflow, bool edge)
		{
			//if (timer._pulsed && timer._pulsing)
			//{
			//    _portB.LowerInput(timer._pin);
			//    timer._pulsed = false;
			//}

			if (timer._active)
			{
				byte mode = timer._mode;

				if (mode == (byte)TIM.CLK ||
					//mode == (byte)TIM.CNT && edge ||
					mode == (byte)TIM.OWF && timerOverflow)// ||
					//mode == (byte)TIM.OWF_CNT && timerOverflow && _cntState)
					timer._current--;

				if (timer._current == 0)
				{
					timerOverflow = true;

					if (timer._oneTime)
					{
						byte mask = (byte)TA_CR.ST;
						timer._controlReg &= (byte)~mask;

						timer._active = false;
					}

					//if (timer._output)
					//{
					//    if (timer._pulsing)
					//    {
					//        _portB.RaiseInput(timer._pin);
					//        timer._pulsed = true;
					//    }
					//    else
					//    {
					//        if (timer._pulsed)
					//            _portB.LowerInput(timer._pin);
					//        else
					//            _portB.RaiseInput(timer._pin);

					//        timer._pulsed = !timer._pulsed;
					//    }
					//}

					SetInterrupt(timer._interruptFlag);

					timer._current = timer._latch;
				}
			}

			return timerOverflow;
		}

		public virtual void Execute(Clock.Clock clock, byte cycle)
		{
			bool edge = _cntEdge;

			//if (_cntEdge)
			//    _cntEdge = false;

			//if (_pcState)
			//    _pcState = false;

			Timer(_timerB, Timer(_timerA, false, edge), edge);
		}

		public void WriteToStateFile(C64Interfaces.IFile stateFile) { }

		public override byte Read(ushort address)
		{
			address &= 0x3f;

			switch (address)
			{
				case (ushort)Registers.PA_D:
					return (byte)((_portA.Input | _portA.WriteOnly) & (_portA.Output | ~_portA.Direction));

				case (ushort)Registers.PB_D:
					_pcState = true;
					return (byte)((_portB.Input | _portB.WriteOnly) & (_portB.Output | ~_portB.Direction));

				case (ushort)Registers.PA_DD:
					return _portA.Direction;

				case (ushort)Registers.PB_DD:
					return _portB.Direction;

				case (ushort)Registers.TA_L:
					return (byte)_timerA._current;

				case (ushort)Registers.TA_H:
					return (byte)(_timerA._current >> 8);

				case (ushort)Registers.TB_L:
					return (byte)_timerB._current;

				case (ushort)Registers.TB_H:
					return (byte)(_timerB._current >> 8);

				case (ushort)Registers.TOD_T:
					return ReadTOD(TOD.TENTH);

				case (ushort)Registers.TOD_S:
					return ReadTOD(TOD.SECONDS);

				case (ushort)Registers.TOD_M:
					return ReadTOD(TOD.MINUTES);

				case (ushort)Registers.TOD_H:
					return ReadTOD(TOD.HOURS);

				case (ushort)Registers.SERIAL:
					return _serialRegister;

				case (ushort)Registers.IR:
					byte status = _interruptRegisters[0];
					_interruptRegisters[0] = 0;

					if ((status & (byte)IR.REQ) != 0)
						_irqLine.Lower();

					return status;

				case (ushort)Registers.TA_CR:
					return _timerA._controlReg;

				case (ushort)Registers.TB_CR:
					return _timerB._controlReg;
			}

			return 0;
		}

		public override void Write(ushort address, byte value)
		{
			address &= 0x3f;

			switch (address)
			{
				case (ushort)Registers.PA_D:
					_portA.Output = value;
					break;

				case (ushort)Registers.PB_D:
					_pcState = true;
					_portB.Output = value;
					break;

				case (ushort)Registers.PA_DD:
					_portA.Direction = value;
					break;

				case (ushort)Registers.PB_DD:
					_portB.Direction = value;
					break;

				case (ushort)Registers.TA_L:
					_timerA._latch = (ushort)((_timerA._latch & 0xff00) | value);
					break;

				case (ushort)Registers.TA_H:
					_timerA._latch = (ushort)((_timerA._latch & 0x00ff) | (value << 8));
					break;

				case (ushort)Registers.TB_L:
					_timerB._latch = (ushort)((_timerB._latch & 0xff00) | value);
					break;

				case (ushort)Registers.TB_H:
					_timerB._latch = (ushort)((_timerB._latch & 0x00ff) | (value << 8));
					break;

				case (ushort)Registers.TOD_T:
					WriteTOD(TOD.TENTH, value);
					break;

				case (ushort)Registers.TOD_S:
					WriteTOD(TOD.SECONDS, value);
					break;

				case (ushort)Registers.TOD_M:
					WriteTOD(TOD.MINUTES, value);
					break;

				case (ushort)Registers.TOD_H:
					WriteTOD(TOD.HOURS, value);
					break;

				case (ushort)Registers.SERIAL:
					break;

				case (ushort)Registers.IR:
					if ((value & 0x80) != 0)
					{
						_interruptRegisters[1] |= (byte)(value & 0x7f);

						if ((_interruptRegisters[0] & _interruptRegisters[1]) != 0 && (_interruptRegisters[0] & (byte)IR.REQ) == 0)
						{
							_interruptRegisters[0] |= (byte)IR.REQ;
							_irqLine.Raise();
						}
					}
					else
					{
						_interruptRegisters[1] &= (byte)(~value & 0x7f);

						if ((_interruptRegisters[0] & _interruptRegisters[1]) == 0 && (_interruptRegisters[0] & (byte)IR.REQ) != 0)
						{
							byte mask = (byte)IR.REQ;
							_interruptRegisters[0] &= (byte)~mask;
							_irqLine.Lower();
						}
					}
					break;

				case (ushort)Registers.TA_CR:
					_timerA._controlReg = value;

					_timerA._active = (value & (byte)TA_CR.ST) != 0;
					_timerA._mode = (byte)(value & 0x20);
					_timerA._oneTime = (value & (byte)TA_CR.RM) != 0;

					_timerA._output = (value & (byte)TA_CR.PB) == 0;
					_timerA._pulsing = (value & (byte)TA_CR.OM) == 0;

					if ((value & (byte)TA_CR.LD) != 0)
						_timerA._current = _timerA._latch;
					break;

				case (ushort)Registers.TB_CR:
					_timerB._controlReg = value;

					_timerB._active = (value & (byte)TA_CR.ST) != 0;
					_timerB._mode = (byte)(value & 0x80);
					_timerB._oneTime = (value & (byte)TA_CR.RM) != 0;

					_timerB._output = (value & (byte)TA_CR.PB) == 0;
					_timerB._pulsing = (value & (byte)TA_CR.OM) == 0;


					if ((value & (byte)TB_CR.LD) != 0)
						_timerA._current = _timerA._latch;
					break;
			}
		}

		public Clock.ClockEntry CreateOps()
		{
			Clock.ClockEntry op = new Clock.ClockEntry(this);
			op.Next = op;

			return op;
		}

		private void WriteTOD(TOD register, byte value)
		{
			byte pm = (byte)(value >> 7);
			if (register == TOD.HOURS)
				value &= 0x7f;

			value = FromBCD(value);

			if ((_timerB._controlReg & (byte)TB_CR.TDA) == 0)
			{
				if (register == TOD.TENTH)
					_todPause = false;
				else if (register == TOD.HOURS)
				{
					_todPause = true;
					_tod[(byte)TOD_R.CURRENT, (byte)TOD.AM_PM] = pm;
				}

				_tod[(byte)TOD_R.CURRENT, (byte)register] = value;
			}
			else
			{
				if (register == TOD.HOURS)
					_tod[(byte)TOD_R.ALARM, (byte)TOD.AM_PM] = pm;

				_tod[(byte)TOD_R.ALARM, (byte)register] = value;
			}
		}

		private byte ReadTOD(TOD register)
		{
			bool latch = _todLatch;

			if (register == TOD.TENTH)
				_todLatch = false;
			else if (register == TOD.HOURS)
			{
				for (sbyte i = (sbyte)(_tod.GetLength(1) - 1); i >= 0; i--)
					_tod[(byte)TOD_R.LATCH, i] = _tod[(byte)TOD_R.CURRENT, i];

				_todLatch = true;
			}

			byte value = ToBCD(_tod[latch ? (byte)TOD_R.CURRENT : (byte)TOD_R.LATCH, (byte)register]);
			if (register == TOD.HOURS)
				value |= (byte)(value << 7);

			return value;
		}

		private static byte FromBCD(byte value) { return (byte)((value & 0xf) + (value >> 4)); }
		private static byte ToBCD(byte value) { return (byte)((value % 10) | (value / 10 << 4)); }

		private void SetInterrupt(IR interrupt)
		{
			_interruptRegisters[0] |= (byte)interrupt;

			if ((_interruptRegisters[1] & (byte)interrupt) != 0 && (_interruptRegisters[0] & (byte)IR.REQ) == 0)
			{
				_interruptRegisters[0] |= (byte)IR.REQ;
				_irqLine.Raise();
			}
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_portA.ReadDeviceState(stateFile);
			_portB.ReadDeviceState(stateFile);

			_pcState = stateFile.ReadBool();
			_todPause = stateFile.ReadBool();
			_todLatch = stateFile.ReadBool();
			_todClkCnt = stateFile.ReadByte();
			stateFile.ReadBytes(_todLimits);

			for (int i = 0; i < _tod.GetLength(0); i++)
			{
				for (int j = 0; j < _tod.GetLength(1); j++)
					_tod[i, j] = stateFile.ReadByte();
			}

			stateFile.ReadBytes(_interruptRegisters);
			_serialRegister = stateFile.ReadByte();
			_flag = stateFile.ReadBool();
			_cntState = stateFile.ReadBool();
			_cntEdge = stateFile.ReadBool();

			_timerA.ReadDeviceState(stateFile);
			_timerB.ReadDeviceState(stateFile);
		}

		public void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			_portA.WriteDeviceState(stateFile);
			_portB.WriteDeviceState(stateFile);

			stateFile.Write(_pcState);
			stateFile.Write(_todPause);
			stateFile.Write(_todLatch);
			stateFile.Write(_todClkCnt);
			stateFile.Write(_todLimits);

			for (int i = 0; i < _tod.GetLength(0); i++)
			{
				for (int j = 0; j < _tod.GetLength(1); j++)
					stateFile.Write(_tod[i, j]);
			}

			stateFile.Write(_interruptRegisters);
			stateFile.Write(_serialRegister);
			stateFile.Write(_flag);
			stateFile.Write(_cntState);
			stateFile.Write(_cntEdge);

			_timerA.WriteDeviceState(stateFile);
			_timerB.WriteDeviceState(stateFile);
		}
	}

}