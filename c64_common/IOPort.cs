
using System;

namespace IO
{
	public class IOPort : State.IDeviceState
	{
		private byte _writeOnly = 0;

		private byte _stateOut = 0;
		private byte _stateIn = 0;

		private byte _direction = 0;

		public delegate void PortOutDelegate(byte states);
		public event PortOutDelegate OnPortOut;
		private void RaisePortOut(byte states)
		{
			if (OnPortOut != null)
				OnPortOut(states);
		}

		public IOPort(byte writeOnly) { _writeOnly = writeOnly; }

		public void SetSingleInputFast(byte pin, bool state)
		{
			if (state)
				_stateIn |= pin;
			else
				_stateIn &= (byte)~pin;
		}

		public byte Input
		{
			get { return _stateIn; }
			set { _stateIn = value; }
		}

		public byte Output
		{
			get { return _stateOut; }
			set
			{
				_stateOut = value;
				RaisePortOut((byte)(_stateOut & _direction));
			}
		}

		public byte Direction
		{
			get { return _direction; }
			set
			{
				_direction = value;
				RaisePortOut((byte)(_stateOut & _direction));
			}
		}

		public byte WriteOnly { get { return _writeOnly; } }

		public void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_stateOut = stateFile.ReadByte();
			_stateIn = stateFile.ReadByte();
			_direction = stateFile.ReadByte();
		}

		public void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			stateFile.Write(_stateOut);
			stateFile.Write(_stateIn);
			stateFile.Write(_direction);
		}
	}

}