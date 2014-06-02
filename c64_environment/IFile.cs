
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

namespace C64Interfaces
{
	public interface IFile
	{
		ulong Size { get; }
		ulong Pos { get; }

		void Read(byte[] memory, int offset, ushort size);

		byte ReadByte();
		ushort ReadWord();
		uint ReadDWord();
		ulong ReadQWord();
		bool ReadBool();
		void ReadBytes(byte[] data);
		void ReadWords(ushort[] data);
		void ReadDWords(uint[] data);
		void ReadBools(bool[] data);

		void Write(byte data);
		void Write(ushort data);
		void Write(uint data);
		void Write(ulong data);
		void Write(bool data);
		void Write(byte[] data);
		void Write(ushort[] data);
		void Write(uint[] data);
		void Write(bool[] data);

		void Close();
	}
}
