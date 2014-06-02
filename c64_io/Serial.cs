
namespace IO
{

	public class SerialPort : State.IDeviceState
	{
		public delegate void LineChangedDelegate(bool state);

		public event LineChangedDelegate OnAtnLineChanged;

		public class BusLine : State.IDeviceState
		{
			public event LineChangedDelegate OnLineChanged;

			private byte _state = 0;
			public bool State
			{
				get { return _state == 0; }
				set
				{
					if (value)
					{
						_state--;
						if (_state == 0 && OnLineChanged != null)
							OnLineChanged(true);
					}
					else
					{
						_state++;
						if (_state == 1 && OnLineChanged != null)
							OnLineChanged(false);
					}
				}
			}

			public void Attach(bool localState)
			{
				if (!localState)
					_state++;
			}

			public void ReadDeviceState(C64Interfaces.IFile stateFile) { _state = stateFile.ReadByte(); }

			public void WriteDeviceState(C64Interfaces.IFile stateFile) { stateFile.Write(_state); }
		}

		public class BusLineConnection : State.IDeviceState
		{
			private BusLine _line;

			private bool _localState = false;
			public bool LocalState
			{
				get { return _localState; }
				set
				{
					if (value != _localState)
						_line.State = _localState = value;
				}
			}

			public BusLineConnection(BusLine line)
			{
				_line = line;
				_line.Attach(_localState);
			}

			public void ReadDeviceState(C64Interfaces.IFile stateFile) { _localState = stateFile.ReadBool(); }

			public void WriteDeviceState(C64Interfaces.IFile stateFile) { stateFile.Write(_localState); }
		}

		public bool _atnLine = false;
		public bool AtnLine
		{
			get { return _atnLine; }
			set
			{
				if (_atnLine != value && OnAtnLineChanged != null)
				{
					_atnLine = value;
					OnAtnLineChanged(value);
				}
			}
		}

		private BusLine _dataLine = new BusLine();
		public BusLine DataLine { get { return _dataLine; } }

		private BusLine _clockLine = new BusLine();
		public BusLine ClockLine { get { return _clockLine; } }

		public void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_atnLine = stateFile.ReadBool();
			_dataLine.ReadDeviceState(stateFile);
			_clockLine.ReadDeviceState(stateFile);
		}

		public void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			stateFile.Write(_atnLine);
			_dataLine.WriteDeviceState(stateFile);
			_clockLine.WriteDeviceState(stateFile);
		}
	}

}
