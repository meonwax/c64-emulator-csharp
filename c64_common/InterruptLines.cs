
namespace IO
{
	public class Irq : State.IDeviceState
	{
		private byte _sourceCount = 0;

		public bool IsRaised { get { return _sourceCount != 0; } }
		public void Raise() { _sourceCount++; }
		public void Lower()
		{
			if (_sourceCount > 0)
				_sourceCount--;
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile) { _sourceCount = stateFile.ReadByte(); }
		public void WriteDeviceState(C64Interfaces.IFile stateFile) { stateFile.Write(_sourceCount); }
	}

	public class Nmi
	{
		private bool _level = false;

		public void Raise() { _level = true; }
		public bool Check()
		{
			bool state = _level;
			_level = false;

			return state;
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile) { _level = stateFile.ReadBool(); }
		public void WriteDeviceState(C64Interfaces.IFile stateFile) { stateFile.Write(_level); }
	}

}