
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
using System.Diagnostics;

namespace c64_win_gdi
{
	public partial class C64EmuForm : Form
	{
		C64Emulator _emulator;

		public C64EmuForm()
		{
			InitializeComponent();

			_emulator = new C64Emulator(panel1);
		}

		private void C64EmuForm_Load(object sender, EventArgs e)
		{
			_emulator.Start();
		}

		private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			_emulator.KeyPressed(e.KeyCode);
		}

		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			_emulator.KeyReleased(e.KeyCode);
		}

		private void _tbSwapJoystick_Click(object sender, EventArgs e)
		{
			_emulator.SwapJoystick();
		}

		private void _tbLoadState_Click(object sender, EventArgs e)
		{
			if (_dlgOpenState.ShowDialog() == DialogResult.OK)
			{
				_emulator.LoadState(_dlgOpenState.FileName);
			}
		}

		private void _tbSaveState_Click(object sender, EventArgs e)
		{
			if (_dlgSaveState.ShowDialog() == DialogResult.OK)
			{
				_emulator.SaveState(_dlgOpenState.FileName);
			}
		}

		private void _tbAttachDiskImage_Click(object sender, EventArgs e)
		{
			if (_dlgAttachDiskImage.ShowDialog() == DialogResult.OK)
			{
				_emulator.AttachImage(_dlgAttachDiskImage.FileName);
			}
		}

		private void _tbRestartEmulator_Click(object sender, EventArgs e)
		{
			_emulator.Restart();
		}

		private void C64EmuForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			_emulator.Stop();
		}
	}
}
