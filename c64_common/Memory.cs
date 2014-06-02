
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

namespace Memory
{

	public class RAM : MemoryMappedDevice, State.IDeviceState
	{
		private byte[] _memory = null;

		public RAM(ushort address, uint size) : base(address, size) { _memory = new byte[size]; }

		public override byte Read(ushort address) { return _memory[address - _address]; }
		public override void Write(ushort address, byte value) { _memory[address - _address] = value; }

		public byte ReadDirect(ushort address) { return _memory[address]; }
		public virtual void WriteDirect(ushort address, byte value) { _memory[address] = value; }

		public void ReadDeviceState(C64Interfaces.IFile stateFile) { stateFile.ReadBytes(_memory); }
		public void WriteDeviceState(C64Interfaces.IFile stateFile) { stateFile.Write(_memory); }
	}

	public class ColorRAM : RAM
	{
		public ColorRAM(ushort address, uint size) : base(address, size) { }

		public override void Write(ushort address, byte value) { base.Write(address, (byte)(value & 0xf)); }
		public override void WriteDirect(ushort address, byte value) { base.WriteDirect(address, (byte)(value & 0xf)); }
	}

	public class ROM : MemoryMappedDevice
	{
		private byte[] _memory = null;

		public ROM(ushort address, ushort size, C64Interfaces.IFile file)
			: base(address, (uint)size)
		{
			_memory = new byte[size];
			file.Read(_memory, 0, size);
		}

		public override byte Read(ushort address) { return _memory[address - _address]; }
		public override void Write(ushort address, byte value) { throw new InvalidOperationException(); }

		public byte ReadDirect(ushort address) { return _memory[address]; }

		public void Patch(ushort address, byte value) { _memory[address - _address] = value; }
	}

}