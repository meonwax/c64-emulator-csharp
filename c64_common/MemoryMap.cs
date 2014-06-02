
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

	public abstract class MemoryMappedDevice
	{
		protected ushort _address;
		public ushort Address { get { return _address; } }

		protected uint _size;
		public uint Size { get { return _size; } }

		public MemoryMappedDevice(ushort address, uint size)
		{
			_address = address;
			_size = size;
		}

		public abstract byte Read(ushort address);
		public abstract void Write(ushort address, byte value);

		public ushort GetOffset(ushort address) { return (ushort)(address - _address); }
	}

	public class MemoryMapEntry
	{
		public enum AccessType
		{
			Read,
			Write
		}

		private MemoryMappedDevice[] _devices = new MemoryMappedDevice[2];

		public MemoryMapEntry() { _devices[0] = _devices[1] = null; }

		public MemoryMappedDevice this[AccessType accessType]
		{
			get { return _devices[(int)accessType]; }
			set { _devices[(int)accessType] = value; }
		}
	}

	public class MemoryMap
	{
		private MemoryMapEntry[] _memoryMap = null;

		public MemoryMap(uint mapSize)
		{
			_memoryMap = new MemoryMapEntry[mapSize];
			for (int i = 0; i < _memoryMap.Length; i++)
				_memoryMap[i] = new MemoryMapEntry();
		}

		public void Map(MemoryMappedDevice device, bool overwrite)
		{
			Map(device, MemoryMapEntry.AccessType.Read, overwrite);
			Map(device, MemoryMapEntry.AccessType.Write, overwrite);
		}

		public void Map(MemoryMappedDevice device, MemoryMapEntry.AccessType accessType, bool overwrite) { Map(device, device.Address, device.Size, accessType, overwrite); }

		public void Map(MemoryMappedDevice device, ushort address, uint size, bool overwrite)
		{
			Map(device, address, size, MemoryMapEntry.AccessType.Read, overwrite);
			Map(device, address, size, MemoryMapEntry.AccessType.Write, overwrite);
		}

		public void Map(MemoryMappedDevice device, ushort address, uint size, MemoryMapEntry.AccessType accessType, bool overwrite)
		{
			if (!overwrite)
			{
				for (uint i = 0; i < size; i++)
				{
					if (_memoryMap[address + i][accessType] != null)
						throw new ArgumentException();
				}
			}

			for (uint i = 0; i < size; i++)
				_memoryMap[address + i][accessType] = device;
		}

		public void Unmap(MemoryMappedDevice device)
		{
			Unmap(device, MemoryMapEntry.AccessType.Read);
			Unmap(device, MemoryMapEntry.AccessType.Write);
		}

		public void Unmap(MemoryMappedDevice device, MemoryMapEntry.AccessType accessType) { Unmap(device, device.Address, device.Size, accessType); }

		public void Unmap(MemoryMappedDevice device, ushort address, uint size)
		{
			Unmap(device, address, size, MemoryMapEntry.AccessType.Read);
			Unmap(device, address, size, MemoryMapEntry.AccessType.Write);
		}

		public void Unmap(MemoryMappedDevice device, ushort address, uint size, MemoryMapEntry.AccessType accessType)
		{
			for (uint i = 0; i < size; i++)
			{
				if (_memoryMap[address + i][accessType] == device)
					_memoryMap[address + i][accessType] = null;
			}
		}

		public byte Read(ushort address) { return _memoryMap[address][MemoryMapEntry.AccessType.Read].Read(address); }
		public void Write(ushort address, byte value) { _memoryMap[address][MemoryMapEntry.AccessType.Write].Write(address, value); }
	}

}  