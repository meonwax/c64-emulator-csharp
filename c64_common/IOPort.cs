
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