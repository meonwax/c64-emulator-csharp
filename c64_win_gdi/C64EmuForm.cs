
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace c64_win_gdi
{
	public partial class C64EmuForm : Form
	{
		File _kernel = new File(new FileInfo(@".\kernal.rom"));
		File _basic = new File(new FileInfo(@".\basic.rom"));
		File _charGen = new File(new FileInfo(@".\chargen.rom"));
		File _driveKernel = new File(new FileInfo(@".\d1541.rom"));

		private Board.Board _board;
		private DiskDrive.CBM1541 _drive;

		private Input.Keyboard _keyboard;

		private byte _currentJoystic;

		public C64EmuForm()
		{
			InitializeComponent();

			_board = new Board.Board(new GdiVideo(panel1), _kernel, _basic, _charGen);

			_drive = new DiskDrive.CBM1541(_driveKernel, _board.Serial);

			_board.SystemClock.OnPhaseEnd += new Clock.Clock.PhaseEndDelegate(_drive.DriveClock.Run);
			_board.OnLoadState += new Board.Board.StateOperationDelegate(_drive.ReadDeviceState);
			_board.OnSaveState += new Board.Board.StateOperationDelegate(_drive.WriteDeviceState);

			_keyboard = new Input.Keyboard(_board.SystemCias[0].PortA, _board.SystemCias[0].PortB, null);

			Thread thread = new Thread(new ThreadStart(_board.Start));
			thread.Start();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			//_keyboard.KeyUp(Input.Keyboard.Keys.KEY_RET);

			//if (msg.Msg == 256 && keyData == System.Windows.Forms.Keys.Enter)
			//    _keyboard.KeyDown(Input.Keyboard.Keys.KEY_RET);

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			KeyboardEvent(e.KeyCode, _keyboard.KeyDown);
		}

		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			KeyboardEvent(e.KeyCode, _keyboard.KeyUp);
		}

		delegate void HandleKeyboardMethod(Input.Keyboard.Keys key);

		private void KeyboardEvent(Keys key, HandleKeyboardMethod handle)
		{
			if (key >= Keys.A && key <= Keys.Z)
				handle(Input.Keyboard.Keys.KEY_A + (key - Keys.A));
			else if (key >= Keys.D0 && key <= Keys.D9)
				handle(Input.Keyboard.Keys.KEY_0 + (key - Keys.D0));
			else if (key == Keys.Space)
				handle(Input.Keyboard.Keys.KEY_SP);
			else if (key >= Keys.F1 && key <= Keys.F7)
			{
				int fkey = key - Keys.F1;
				if ((fkey & 1) == 1)
					handle(Input.Keyboard.Keys.KEY_LSH);

				handle(Input.Keyboard.Keys.KEY_F1 + (fkey >> 1));
			}
			else if ((ushort)key == 192)
				handle(Input.Keyboard.Keys.KEY_LEFT);
			else if ((ushort)key == 187)
				handle(Input.Keyboard.Keys.KEY_PL);
			else if ((ushort)key == 189)
				handle(Input.Keyboard.Keys.KEY_MI);
			//else if ((ushort)key == 222)
			//    handle(Input.Keyboard.Keys.KEY_PND);
			else if ((ushort)key == 219)
				handle(Input.Keyboard.Keys.KEY_AT);
			else if ((ushort)key == 221)
				handle(Input.Keyboard.Keys.KEY_STAR);
			else if ((ushort)key == 220)
				handle(Input.Keyboard.Keys.KEY_UP);
			else if ((ushort)key == 186)
				handle(Input.Keyboard.Keys.KEY_COL);
			else if ((ushort)key == 222)
				handle(Input.Keyboard.Keys.KEY_SCOL);
			//else if ((ushort)key == 222)
			//    handle(Input.Keyboard.Keys.KEY_EQ);
			else if ((ushort)key == 188)
				handle(Input.Keyboard.Keys.KEY_COM);
			else if ((ushort)key == 190)
				handle(Input.Keyboard.Keys.KEY_DOT);
			else if ((ushort)key == 191)
				handle(Input.Keyboard.Keys.KEY_SLASH);
			else if (key == Keys.Left)
			{
				handle(Input.Keyboard.Keys.KEY_LSH);
				handle(Input.Keyboard.Keys.KEY_HOR);
			}
			else if (key == Keys.Right)
				handle(Input.Keyboard.Keys.KEY_HOR);
			else if (key == Keys.Up)
			{
				handle(Input.Keyboard.Keys.KEY_LSH);
				handle(Input.Keyboard.Keys.KEY_VER);
			}
			else if (key == Keys.Down)
				handle(Input.Keyboard.Keys.KEY_VER);
			else if (key == Keys.Back)
				handle(Input.Keyboard.Keys.KEY_DEL);
			else if (key == Keys.Enter)
				handle(Input.Keyboard.Keys.KEY_RET);
			else if (key == Keys.Home)
				handle(Input.Keyboard.Keys.KEY_HOME);
			else if (key == Keys.ShiftKey)
				handle(Input.Keyboard.Keys.KEY_LSH);
			//else if (key == Keys.Shift)
			//    handle(Input.Keyboard.Keys.KEY_RSH);
			else if ((ushort)key == 17)
				handle(Input.Keyboard.Keys.KEY_CTRL);
			else if ((ushort)key == 18)
				handle(Input.Keyboard.Keys.KEY_CMD);
			else if (key == Keys.End)
				handle(Input.Keyboard.Keys.KEY_RUN);
			else if (key == Keys.Escape)
			{
				// handle RESTORE KEY
			}
			if (key == Keys.NumPad8)
				handle(Input.Keyboard.Keys.J1U + _currentJoystic);
			else if (key == Keys.NumPad5)
				handle(Input.Keyboard.Keys.J1D + _currentJoystic);
			else if (key == Keys.NumPad4)
				handle(Input.Keyboard.Keys.J1L + _currentJoystic);
			else if (key == Keys.NumPad6)
				handle(Input.Keyboard.Keys.J1R + _currentJoystic);
			else if (key == Keys.NumPad0)
				handle(Input.Keyboard.Keys.J1F + _currentJoystic);
		}

		private void _tbSwapJoystick_Click(object sender, EventArgs e)
		{
			_currentJoystic = _currentJoystic == 0 ? (byte)5 : (byte)0;
		}

		private void _tbLoadState_Click(object sender, EventArgs e)
		{
			if (_dlgOpenState.ShowDialog() == DialogResult.OK)
			{
				_board.LoadState(new File(new FileInfo(_dlgOpenState.FileName)));
			}
		}

		private void _tbSaveState_Click(object sender, EventArgs e)
		{
			if (_dlgSaveState.ShowDialog() == DialogResult.OK)
			{
				_board.SaveState(new File(new FileInfo(_dlgOpenState.FileName)));
			}
		}

		private void _tbAttachDiskImage_Click(object sender, EventArgs e)
		{
			if (_dlgAttachDiskImage.ShowDialog() == DialogResult.OK)
			{
				_drive.Drive.Attach(new File(new FileInfo(_dlgAttachDiskImage.FileName)));
			}
		}
	}
}
