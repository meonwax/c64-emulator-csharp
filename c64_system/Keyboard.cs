
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Input
{
	public class Keyboard
	{
		public enum Keys
		{
			KEY_0, KEY_1, KEY_2, KEY_3, KEY_4, KEY_5, KEY_6, KEY_7, KEY_8, KEY_9,
			KEY_A, KEY_B, KEY_C, KEY_D, KEY_E, KEY_F, KEY_G, KEY_H, KEY_I, KEY_J,
			KEY_K, KEY_L, KEY_M, KEY_N, KEY_O, KEY_P, KEY_Q, KEY_R, KEY_S, KEY_T,
			KEY_U, KEY_V, KEY_W, KEY_X, KEY_Y, KEY_Z,
			KEY_PL, KEY_MI, KEY_DOT, KEY_COL, KEY_AT, KEY_COM, KEY_PND, KEY_STAR, KEY_SCOL, KEY_EQ, KEY_SLASH, KEY_UP, KEY_LEFT,
			KEY_SP,
			KEY_F1, KEY_F3, KEY_F5, KEY_F7,
			KEY_HOR, KEY_VER,
			KEY_DEL, KEY_RET, KEY_HOME, KEY_LSH, KEY_RSH, KEY_CTRL, KEY_CMD, KEY_RUN,
			KEY_RES,
			KEY_SLCK,
			J1U, J1D, J1L, J1R, J1F, J2U, J2D, J2L, J2R, J2F
		}

		private byte[][] _keyCoords = new byte[][]
		{
			/*     0 */ new byte[] {  3,  4 }, /*     1 */ new byte[] {  0,  7 },
			/*     2 */ new byte[] {  3,  7 }, /*     3 */ new byte[] {  0,  1 },
			/*     4 */ new byte[] {  3,  1 }, /*     5 */ new byte[] {  0,  2 },
			/*     6 */ new byte[] {  3,  2 }, /*     7 */ new byte[] {  0,  3 },
			/*     8 */ new byte[] {  3,  3 }, /*     9 */ new byte[] {  0,  4 },
			/*     A */ new byte[] {  2,  1 }, /*     B */ new byte[] {  4,  3 },
			/*     C */ new byte[] {  4,  2 }, /*     D */ new byte[] {  2,  2 },
			/*     E */ new byte[] {  6,  1 }, /*     F */ new byte[] {  5,  2 },
			/*     G */ new byte[] {  2,  3 }, /*     H */ new byte[] {  5,  3 },
			/*     I */ new byte[] {  1,  4 }, /*     J */ new byte[] {  2,  4 },
			/*     K */ new byte[] {  5,  4 }, /*     L */ new byte[] {  2,  5 },
			/*     M */ new byte[] {  4,  4 }, /*     N */ new byte[] {  7,  4 },
			/*     O */ new byte[] {  6,  4 }, /*     P */ new byte[] {  1,  5 },
			/*     Q */ new byte[] {  6,  7 }, /*     R */ new byte[] {  1,  2 },
			/*     S */ new byte[] {  5,  1 }, /*     T */ new byte[] {  6,  2 },
			/*     U */ new byte[] {  6,  3 }, /*     V */ new byte[] {  7,  3 },
			/*     W */ new byte[] {  1,  1 }, /*     X */ new byte[] {  7,  2 },
			/*     Y */ new byte[] {  1,  3 }, /*     Z */ new byte[] {  4,  1 },
			/*    PL */ new byte[] {  0,  5 }, /*    MI */ new byte[] {  3,  5 },
			/*   DOT */ new byte[] {  4,  5 }, /*   COL */ new byte[] {  5,  5 },
			/*    AT */ new byte[] {  6,  5 }, /*   COM */ new byte[] {  7,  5 },
			/*   PND */ new byte[] {  0,  6 }, /*  STAR */ new byte[] {  1,  6 },
			/*  SCOL */ new byte[] {  2,  6 }, /*    EQ */ new byte[] {  5,  6 },
			/* SLASH */ new byte[] {  7,  6 }, /*    UP */ new byte[] {  6,  6 },
			/*  LEFT */ new byte[] {  1,  7 }, /*    SP */ new byte[] {  4,  7 },
			/*    F1 */ new byte[] {  4,  0 }, /*    F3 */ new byte[] {  5,  0 },
			/*    F5 */ new byte[] {  6,  0 }, /*    F7 */ new byte[] {  3,  0 },
			/*   HOR */ new byte[] {  2,  0 }, /*   VER */ new byte[] {  7,  0 },
			/*   DEL */ new byte[] {  0,  0 }, /*   RET */ new byte[] {  1,  0 },
			/*  HOME */ new byte[] {  3,  6 }, /*   LSH */ new byte[] {  7,  1 },
			/*   RSH */ new byte[] {  4,  6 }, /*  CTRL */ new byte[] {  2,  7 },
			/*   CMD */ new byte[] {  5,  7 }, /*   RUN */ new byte[] {  7,  7 },

			/*   RES */ new byte[] {  0,  0 }, /*  SLCK */ new byte[] {  0,  0 },

			/*   J1U */ new byte[] {  0,  0 }, /*   J1D */ new byte[] {  1,  0 },
			/*   J1L */ new byte[] {  2,  0 }, /*   J1R */ new byte[] {  3,  0 },
			/*   J1F */ new byte[] {  4,  0 }, /*   J2U */ new byte[] {  0,  1 },
			/*   J2D */ new byte[] {  1,  1 }, /*   J2L */ new byte[] {  2,  1 },
			/*   J2R */ new byte[] {  3,  1 }, /*   J2F */ new byte[] {  4,  1 },
		};

		private byte[] _matrix = new byte[8] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
		private byte[] _joystics = new byte[] { 0xff, 0xff };

		private byte _currentState;

		private IO.IOPort _rowSelectPort;
		private IO.IOPort _columnSelectPort;
		private IO.Nmi _nmiLine;

		public Keyboard(IO.IOPort rowSelectPort, IO.IOPort columnSelectPort, IO.Nmi nmiLine)
		{
			_rowSelectPort = rowSelectPort;
			_columnSelectPort = columnSelectPort;
			_nmiLine = nmiLine;

			_rowSelectPort.OnPortOut += new IO.IOPort.PortOutDelegate(RowSelectPort_OnPortOut);
		}

		private byte _currentRow = 0;

		private void RowSelectPort_OnPortOut(byte states)
		{
			_currentRow = states;
			_currentState = 0xff;

			states &= _joystics[0];

			for (byte i = 0; i < 8; i++, states >>= 1)
			{
				if ((states & 1) == 0)
					_currentState &= _matrix[i];
			}

			_columnSelectPort.Input = (byte)(_currentState & _joystics[1]);
		}

		public void KeyDown(Keys key)
		{
			byte row = _keyCoords[(byte)key][1], col = _keyCoords[(byte)key][0];

			if (key >= Keys.J1U)
			{
				_joystics[row] &= (byte)(~(1 << col));

				if (row == 0)
					_rowSelectPort.Input = _joystics[0];
			}
			else
			{
				_matrix[row] &= (byte)(~(1 << col));
			}

			byte cr = (byte)(_currentRow & _joystics[0]);
			if ((cr & (1 << row)) == 0)
				_currentState &= _matrix[row];

			_columnSelectPort.Input = (byte)(_currentState & _joystics[1]);
		}

		public void KeyUp(Keys key)
		{
			byte row = _keyCoords[(byte)key][1], col = _keyCoords[(byte)key][0];

			if (key >= Keys.J1U)
			{
				_joystics[row] |= (byte)(1 << col);

				if (row == 0)
					_rowSelectPort.Input = _joystics[0];
			}
			else
			{
				_matrix[row] |= (byte)(1 << col);
			}

			byte cr = (byte)(_currentRow & _joystics[0]);
			if ((_currentRow & (1 << row)) == 0)
			{
				_currentState = 0xff;

				for (byte i = 0; i < 8; i++, _currentRow >>= 1)
				{
					if ((_currentRow & 1) == 0)
						_currentState &= _matrix[i];
				}
			}

			_columnSelectPort.Input = (byte)(_currentState & _joystics[1]);
		}
	}

}
