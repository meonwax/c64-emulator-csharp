
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