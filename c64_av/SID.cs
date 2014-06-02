
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

using C64Interfaces;

namespace Audio
{

	public class SID : Memory.MemoryMappedDevice, State.IDeviceState
	{
		public SID(ushort sidAddress, ushort sidSize)
			: base(sidAddress, sidSize)
		{
		}

		public override byte Read(ushort address)
		{
			return 0;
		}

		public override void Write(ushort address, byte value)
		{
		}

		void State.IDeviceState.ReadDeviceState(IFile stateFile)
		{
		}

		void State.IDeviceState.WriteDeviceState(IFile stateFile)
		{
		}
	}

}