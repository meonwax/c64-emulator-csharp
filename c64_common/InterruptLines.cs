
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