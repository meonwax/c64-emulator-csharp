
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

using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System;

namespace c64_win_gdi
{

	class File : C64Interfaces.IFile
	{
		private FileStream _stream;
		private ulong _size;

		public File(FileInfo fileInfo)
		{
			_stream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			_size = (ulong)fileInfo.Length;
		}

		public ulong Size { get { return _size; } }
		public ulong Pos { get { return (ulong)_stream.Position; } }


		public void Read(byte[] memory, int offset, ushort size)
		{
			_stream.Seek(offset, SeekOrigin.Begin);
			_stream.Read(memory, 0, (int)size);
		}

		public byte ReadByte() { return (byte)_stream.ReadByte(); }
		public ushort ReadWord()
		{
			byte[] arr = new byte[2];
			_stream.Read(arr, 0, arr.Length);
			return BitConverter.ToUInt16(arr, 0);
		}
		public uint ReadDWord()
		{
			byte[] arr = new byte[4];
			_stream.Read(arr, 0, arr.Length);
			return BitConverter.ToUInt32(arr, 0);
		}
		public ulong ReadQWord()
		{
			byte[] arr = new byte[8];
			_stream.Read(arr, 0, arr.Length);
			return BitConverter.ToUInt64(arr, 0);
		}
		public bool ReadBool() { return ReadByte() != 0; }

		public void ReadBytes(byte[] data) { _stream.Read(data, 0, data.Length); }
		public void ReadWords(ushort[] data)
		{
			byte[] arr = new byte[2];
			for (int i = 0; i < data.Length; i++)
			{
				_stream.Read(arr, 0, arr.Length);
				data[i] = BitConverter.ToUInt16(arr, 0);
			}
		}
		public void ReadDWords(uint[] data)
		{
			byte[] arr = new byte[4];
			for (int i = 0; i < data.Length; i++)
			{
				_stream.Read(arr, 0, arr.Length);
				data[i] = BitConverter.ToUInt32(arr, 0);
			}
		}
		public void ReadBools(bool[] data)
		{
			for (int i = 0; i < data.Length; i++)
				data[i] = _stream.ReadByte() != 0;
		}

		public void Write(byte data) { _stream.WriteByte(data); }
		public void Write(ushort data)
		{
			byte[] arr = BitConverter.GetBytes(data);
			_stream.Write(arr, 0, arr.Length);
		}
		public void Write(uint data)
		{
			byte[] arr = BitConverter.GetBytes(data);
			_stream.Write(arr, 0, arr.Length);
		}
		public void Write(ulong data)
		{
			byte[] arr = BitConverter.GetBytes(data);
			_stream.Write(arr, 0, arr.Length);
		}
		public void Write(bool data) { Write(data ? (byte)1 : (byte)0); }

		public void Write(byte[] data) { _stream.Write(data, 0, data.Length); }
		public void Write(ushort[] data)
		{
			for (int i = 0; i < data.Length; i++)
				Write(BitConverter.GetBytes(data[i]));
		}
		public void Write(uint[] data)
		{
			for (int i = 0; i < data.Length; i++)
				Write(BitConverter.GetBytes(data[i]));
		}
		public void Write(bool[] data)
		{
			for (int i = 0; i < data.Length; i++)
				Write(data[i] ? (byte)1 : (byte)0);
		}

		public void Close() { _stream.Close(); }
	}

	unsafe class GdiVideo : C64Interfaces.IVideoOutput
	{
		private Panel _panel;

		private Rectangle _bitmapRect = new Rectangle(0, 0, Video.VIC.X_RESOLUTION, Video.VIC.Y_RESOLUTION);
		private Bitmap _bitmap = new Bitmap(Video.VIC.X_RESOLUTION, Video.VIC.Y_RESOLUTION, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
		private System.Drawing.Imaging.BitmapData _bitmapData = null;
		uint* _bitmapPtr;

		public unsafe GdiVideo(Panel panel)
		{
			_panel = panel;
			_bitmapData = _bitmap.LockBits(_bitmapRect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
			_bitmapPtr = (uint*)_bitmapData.Scan0.ToPointer();
			_bitmap.UnlockBits(_bitmapData);

			for (int i = 0; i < Video.Pallete.CvtColors.Length; i++)
			{
				byte[] c = Video.Pallete.Colors[i];
				Video.Pallete.CvtColors[i] = (uint)((c[0] << 16) | (c[1] << 8) | c[2]);
			}
		}

		public unsafe void OutputPixel(uint pos, uint color)
		{
			_bitmapPtr[pos] = color;
		}

		public unsafe void Flush()
		{
			//_panel.BeginInvoke(new DrawDelegate(DrawAsync), _bitmap);
			_panel.Invoke(new DrawDelegate(Draw), _bitmap);

			_frame++;

			DateTime now = DateTime.Now;
			long diff = (now - _last).Ticks;

			if (diff >= TimeSpan.TicksPerSecond)
			{
				_fps = TimeSpan.TicksPerSecond * _frame / diff;
				_last = now;
				_frame = 0;
			}
		}

		Font _font = new Font(FontFamily.GenericMonospace, 10);
		Brush _brush = new SolidBrush(Color.Black);
		Brush _clearBrush = new SolidBrush(Color.White);

		uint _frame = 0;
		long _fps = 0;
		DateTime _last;

		private void DrawAsync(Bitmap bmp)
		{
			_panel.Invoke(new DrawDelegate(Draw), bmp);
		}

		private delegate void DrawDelegate(Bitmap bmp);
		private void Draw(Bitmap bmp)
		{
			Graphics g = _panel.CreateGraphics();
			g.DrawImage(bmp, 0, 0);

			g.FillRectangle(_clearBrush, 0, 400, 50, 50);
			g.DrawString(_fps.ToString(), _font, _brush, 0, 400);
		}
	}

}