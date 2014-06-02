
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