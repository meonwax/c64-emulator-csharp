
namespace CPU
{

	public class CPUPort : Memory.MemoryMappedDevice, State.IDeviceState
	{
		private IO.IOPort _ioPort = new IO.IOPort(0x07);
		public IO.IOPort IOPort { get { return _ioPort; } }

		public CPUPort(ushort ioAddress, ushort ioSize) : base(ioAddress, ioSize) { _ioPort.Direction = 0x2f; }

		public override byte Read(ushort address) { return address == 0 ? _ioPort.Direction : (byte)(_ioPort.Input | (_ioPort.Output & _ioPort.Direction)); }

		public override void Write(ushort address, byte value)
		{
			if (address == 0)
				_ioPort.Direction = value;
			else
				_ioPort.Output = value;
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile) { _ioPort.ReadDeviceState(stateFile); }

		public void WriteDeviceState(C64Interfaces.IFile stateFile) { _ioPort.WriteDeviceState(stateFile); }
	}

	public class MOS6510 : MOS6502
	{
		private CPUPort _port;
		public CPUPort Port { get { return _port; } }

		public MOS6510(ushort ioAddress, ushort ioSize, Memory.MemoryMap memory, byte phase)
			: base(memory, phase) { _port = new CPUPort(ioAddress, ioSize); }

		public override void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_port.ReadDeviceState(stateFile);
			base.ReadDeviceState(stateFile);
		}

		public override void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			_port.WriteDeviceState(stateFile);
			base.WriteDeviceState(stateFile);
		}
	}

}