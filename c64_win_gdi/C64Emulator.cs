
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace c64_win_gdi
{
	public class C64Emulator
	{
		private delegate void HandleKeyboardMethod(Input.Keyboard.Keys key);

		private object _syncRoot = new object();

		private File _kernel = new File(new FileInfo(@".\kernal.rom"));
		private File _basic = new File(new FileInfo(@".\basic.rom"));
		private File _charGen = new File(new FileInfo(@".\chargen.rom"));
		private File _driveKernel = new File(new FileInfo(@".\d1541.rom"));

		private Board.Board _board;
		private DiskDrive.CBM1541 _drive;

		private Input.Keyboard _keyboard;

		private byte _currentJoystic;
		private bool _emulatorRunning;

		private Panel _videoOutput;

		private Thread _thread;

		public C64Emulator(Panel videoOutput)
		{
			_videoOutput = videoOutput;
			CreateEmulator();
		}

		public void Start()
		{
			_thread = new Thread(new ThreadStart(EmulateFrame));
			_thread.Start();
		}

		public void Restart()
		{
			CreateEmulator();
		}

		public void Stop()
		{
			_emulatorRunning = false;
			_thread.Join();
		}

		public void LoadState(string fileName)
		{
			_board.LoadState(new File(new FileInfo(fileName)));
		}

		public void SaveState(string fileName)
		{
			_board.SaveState(new File(new FileInfo(fileName)));
		}

		public void AttachImage(string fileName)
		{
			_drive.Drive.Attach(new File(new FileInfo(fileName)));
		}

		public void SwapJoystick()
		{
			_currentJoystic = _currentJoystic == 0 ? (byte)5 : (byte)0;
		}

		public void KeyPressed(Keys keyCode)
		{
			KeyboardEvent(keyCode, _keyboard.KeyDown);
		}

		public void KeyReleased(Keys keyCode)
		{
			KeyboardEvent(keyCode, _keyboard.KeyUp);
		}

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

		private void CreateEmulator()
		{
			lock (_syncRoot)
			{
				DestoryEmulator();

				_board = new Board.Board(new GdiVideo(_videoOutput), _kernel, _basic, _charGen);

				_drive = new DiskDrive.CBM1541(_driveKernel, _board.Serial);

				_board.SystemClock.OnPhaseEnd += _drive.DriveClock.Run;
				_board.OnLoadState += _drive.ReadDeviceState;
				_board.OnSaveState += _drive.WriteDeviceState;

				_keyboard = new Input.Keyboard(_board.SystemCias[0].PortA, _board.SystemCias[0].PortB, null);

				_emulatorRunning = true;
			}
		}

		private void DestoryEmulator()
		{
			if (_board != null)
			{
				_board.SystemClock.OnPhaseEnd -= _drive.DriveClock.Run;
				_board.OnLoadState -= _drive.ReadDeviceState;
				_board.OnSaveState -= _drive.WriteDeviceState;

				_board = null;
				_drive = null;
				_keyboard = null;
			}
		}

		private void EmulateFrame()
		{
			Stopwatch timer = new Stopwatch();

			while (_emulatorRunning)
			{
				lock (_syncRoot)
				{
					timer.Restart();

					_board.Start();

					long elapsed = timer.ElapsedMilliseconds;
					if (elapsed > 15)
					{
						Thread.Sleep(0);
					}

					while (timer.ElapsedMilliseconds < 20)
						;
				}
			}
		}
	}
}
