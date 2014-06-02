
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

namespace Video
{

	public class RasterLine : State.IDeviceState
	{
		private VIC _vic;

		private byte _rasteCycle = 0;
		public byte RasteCycle
		{
			get { return _rasteCycle; }
			set { _rasteCycle = value; }
		}

		private delegate void MemReadOp(Clock.Clock clock);
		private delegate void GraphOutOp();

		private class VicNop : Clock.ClockOp
		{
			protected VIC _vic;

			public VicNop(VIC vic) { _vic = vic; }

			public virtual void Execute(Clock.Clock clock, byte cycle) { _vic.RasterLine.RasteCycle++; }

			public void WriteToStateFile(C64Interfaces.IFile stateFile) { }
		}

		private class VicGraphOp : Clock.ClockOp
		{
			protected VIC _vic;

			protected GraphOutOp _graphOp;

			public VicGraphOp(VIC vic, GraphOutOp graphOp)
			{
				_vic = vic;
				_graphOp = graphOp;
			}

			public virtual void Execute(Clock.Clock clock, byte cycle)
			{
				_graphOp();
				_vic.RasterLine.RasteCycle++;
			}

			public void WriteToStateFile(C64Interfaces.IFile stateFile) { }
		}

		private class VicReadOp : Clock.ClockOp
		{
			protected VIC _vic;

			protected MemReadOp _readOp;

			public VicReadOp(VIC vic, MemReadOp readOp)
			{
				_vic = vic;
				_readOp = readOp;
			}

			public virtual void Execute(Clock.Clock clock, byte cycle)
			{
				_readOp(clock);
				_vic.RasterLine.RasteCycle++;
			}

			public void WriteToStateFile(C64Interfaces.IFile stateFile) { }
		}

		private class VicGraphReadOp : Clock.ClockOp
		{
			protected VIC _vic;

			protected GraphOutOp _graphOp;

			protected MemReadOp _readOp;

			public VicGraphReadOp(VIC vic, MemReadOp readOp, GraphOutOp graphOp)
			{
				_vic = vic;
				_readOp = readOp;
				_graphOp = graphOp;
			}

			public virtual void Execute(Clock.Clock clock, byte cycle)
			{
				_readOp(clock);
				_graphOp();

				_vic.RasterLine.RasteCycle++;
			}

			public void WriteToStateFile(C64Interfaces.IFile stateFile) { }
		}

		private class VicReadOp62 : VicReadOp
		{
			public VicReadOp62(VIC vic, MemReadOp readOp)
				: base(vic, readOp) { }

			public override void Execute(Clock.Clock clock, byte cycle)
			{
				_readOp(clock);

				_vic.Raster++;

				if (_vic.Raster == VIC.Y_RESOLUTION)
				{
					_vic.OutputDevice.Flush();
					_vic.Raster = 0;

					clock.Running = false;
				}

				_vic.RasterLine.RasteCycle = 0;
			}
		}

		private void GraphReadOp()
		{
			if (_vic.DisplayState)
				_vic.VC++;
		}

		private void GraphReadOp15_53(Clock.Clock clock)
		{
			GraphReadOp();

			if (_vic.BadLine)
				_vic.WriteVideoMatrix(_vic.Memory.Read(_vic.GetVideoMemoryAddress(_vic.VC)), _vic.ColorRam.ReadDirect(_vic.VC));
		}

		private void GraphReadOp54(Clock.Clock clock)
		{
			for (byte i = 0; i < 8; i++)
			{
				if (_vic.GetSpriteExpY(i))
					_vic.YExpandFlip[i] = !_vic.YExpandFlip[i];
			}

			GraphReadOp();
		}

		private void IdleReadOp(Clock.Clock clock, byte first, byte last)
		{
			for (byte i = first; i <= last; i++)
			{
				if (_vic.GetSpriteEnabled(i) && (byte)_vic.GetSpritePosY(i) == (byte)_vic.Raster && !_vic.SpriteDMA[i])
				{
					_vic.SpriteDMA[i] = true;
					_vic.MCBase[i] = 0;

					if (_vic.GetSpriteExpY(i))
						_vic.YExpandFlip[i] = false;
				}
			}
		}

		private void IdleReadOp55(Clock.Clock clock) { IdleReadOp(clock, 0, 3); }

		private void IdleReadOp56(Clock.Clock clock) { IdleReadOp(clock, 4, 7); }

		private void RefreshReadOp13(Clock.Clock clock)
		{
			_vic.VC = _vic.VCBase;

			_vic.VMLIRead = _vic.VMLIWrite = 0;

			_vic.BadLine = _vic.Raster >= 0x30 && _vic.Raster <= 0xf7 && (_vic.Raster & 0x07) == _vic.YScroll && _vic.DisplayEnabled;

			if (_vic.BadLine)
			{
				_vic.DisplayState = true;
				_vic.RC = 0;
			}
		}

		private void RefreshReadOp14(Clock.Clock clock)
		{
			if (_vic.BadLine)
			{
				clock.Stall(40, 1);
				_vic.WriteVideoMatrix(_vic.Memory.Read(_vic.GetVideoMemoryAddress(_vic.VC)), _vic.ColorRam.ReadDirect(_vic.VC));
			}

			for (byte i = 0; i < 8; i++)
			{
				if (_vic.YExpandFlip[i] || !_vic.GetSpriteExpY(i))
				{
					_vic.MCBase[i] += 3;

					if (_vic.MCBase[i] > 63)
					{
						_vic.SDataBuffer[i].Clear();
						_vic.SpriteDMA[i] = false;
						_vic.SpriteDisplay[i] = false;
					}
				}
			}
		}

		private void SpriteReadOp(Clock.Clock clock, byte firstSprite, byte cycle)
		{
			byte sprite = (byte)(firstSprite + (cycle >> 1));

			if ((cycle & 1) == 0)
				_vic.SpritePointers[sprite] = (ushort)(_vic.Memory.Read(_vic.GetVideoMemoryAddress((ushort)(0x3f8 | sprite))) << 6);

			if (_vic.SpriteDMA[sprite])
			{
				if ((cycle & 1) == 0)
					clock.Stall(2, 1);

				for (byte reads = (byte)((cycle & 1) + 1); reads > 0; reads--)
				{
					_vic.SDataBuffer[sprite].Enqueue(_vic.Memory.Read((ushort)(_vic.GetRawMemoryAddress((ushort)(_vic.SpritePointers[sprite] + _vic.MC[sprite])))));
					_vic.MC[sprite]++;
				}
			}
		}

		private void SpriteReadOp0(Clock.Clock clock)
		{
			if (_vic.Raster == 0)
				_vic.VCBase = 0;

			_vic.XCoord = VIC.FIRST_X_COORD - 1;

			if (_vic.Raster == _vic.RasterComparison)
				_vic.SetInterrupt(VIC.IR.RST);

			SpriteReadOp(clock, 3, _rasteCycle);
		}

		private void SpriteReadOp1_9(Clock.Clock clock) { SpriteReadOp(clock, 3, _rasteCycle); }

		private void SpriteReadOp57(Clock.Clock clock)
		{
			if (_vic.RC == 7)
			{
				_vic.DisplayState = false;
				_vic.VCBase = _vic.VC;
			}

			if (_vic.BadLine || _vic.DisplayState)
			{
				_vic.DisplayState = true;
				_vic.RC++;
			}

			for (byte i = 0; i < 8; i++)
			{
				_vic.MC[i] = _vic.MCBase[i];
				_vic.SpriteDisplay[i] = _vic.SpriteDMA[i] && (byte)_vic.Raster >= (byte)_vic.GetSpritePosY(i);
			}

			SpriteReadOp(clock, 0, 0);
		}

		private void SpriteReadOp58_61(Clock.Clock clock) { SpriteReadOp(clock, 0, (byte)(_rasteCycle - 57)); }

		private void SpriteReadOp62(Clock.Clock clock)
		{
			if (_vic.Raster == _vic.BottomBorder)
			{
				SetReplacableOperations(true, _vic.DisplayEnabled);
				_vic.AuxBorderFlip = true;
			}
			else if (_vic.Raster + 1 == _vic.TopBorder)
			{
				SetReplacableOperations(false, _vic.DisplayEnabled);
				_vic.AuxBorderFlip = false;
			}

			SpriteReadOp(clock, 0, 5);
		}

		private void SpriteGraphOp(ushort xLimit)
		{
			ushort y = (ushort)_vic.Raster;

			ushort right = _vic.RightBorder;
			ushort left = _vic.LeftBorder;
			bool vBorder = _vic.AuxBorderFlip;

			IVideoOutput output = _vic.OutputDevice;

			ushort[] collision = _vic.CollisionMatrix;
			bool[] enabled = _vic.SpriteDisplay;
			for (byte s = 7; s != 0xff; s--)
			{
				if (enabled[s] && xLimit >= _vic.GetSpritePosX(s) && _vic.XCoord <= _vic.GetSpritePosX(s) + 24)
				{
					short offset = (short)(_vic.GetSpritePosX(s) - _vic.XCoord);

					if (offset < 0)
						offset = 0;

					bool mc = _vic.GetSpriteMulticolor(s);
					uint c = Pallete.CvtColors[_vic.GetSpriteColor(s)];

					ushort x = (ushort)(_vic.XCoord + offset);
					uint pos = (uint)(y * VIC.X_RESOLUTION);

					for (; x < xLimit && !_vic.SDataBuffer[s].IsEmpty; x++)
					{
						byte bits;

						if (mc)
							_vic.SDataBuffer[s].Dequeue2(out bits);
						else
							_vic.SDataBuffer[s].Dequeue1(out bits);

						uint pc = (uint)(pos + x);

						if (bits != 0)
						{
							if (!vBorder && x > left && x < right && (_vic.GetSpritePriority(s) || (collision[pc] & (ushort)CollisionState.Foreground) == 0))
								output.OutputPixel(pc, !mc || (bits & 1) == 0 ? c : Pallete.CvtColors[_vic.GetSpriteMulitcolor((byte)(bits >> 1))]);

							byte mask = (byte)(1 << s);

							bool detected = false;
							ushort current = collision[pc];

							if ((current & (ushort)CollisionState.Foreground) != 0)
								_vic.SetSpriteDataCollision(mask);

							current &= (ushort)((ushort)CollisionState.Foreground - 1);

							for (byte sc = 0; current != 0; current >>= 1)
							{
								if ((current & 1) != 0)
								{
									_vic.SetSpriteSpriteCollision((byte)(1 << sc));
									detected = true;
								}
							}

							if (detected)
								_vic.SetSpriteSpriteCollision(mask);

							collision[pc] |= mask;
						}
					}
				}
			}
		}

		private void BorderGraphOp()
		{
			ushort y = (ushort)_vic.Raster;
			ushort x = _vic.XCoord;

			if (y > 15 && y < 285)
			{
				uint bColor = _vic.GetBorderColor();

				IVideoOutput output = _vic.OutputDevice;

				uint pos = (uint)(y * VIC.X_RESOLUTION + x);

				ushort[] collision = _vic.CollisionMatrix;
				for (uint lim = pos + 8; pos < lim; pos++)
				{
					output.OutputPixel(pos, bColor);
					collision[pos] = 0;
				}
			}

			x += 8;
			SpriteGraphOp(x);

			_vic.XCoord = x;
		}

		private void BorderWrapGraphOp()
		{
			ushort y = (ushort)_vic.Raster;

			uint bColor = _vic.GetBorderColor();

			IVideoOutput output = _vic.OutputDevice;

			uint pos = (uint)(y * VIC.X_RESOLUTION);
			ushort[] collision = _vic.CollisionMatrix;
			for (uint lim = pos + 4; pos < lim; pos++)
			{
				output.OutputPixel(pos, bColor);
				collision[pos] = 0;
			}

			SpriteGraphOp((ushort)(_vic.XCoord + 4));

			_vic.XCoord = 0;

			SpriteGraphOp(4);

			_vic.XCoord = 4;
		}

		private void CheckedActiveGraphLeftOp()
		{
			IVideoOutput output = _vic.OutputDevice;
			ushort[] collision = _vic.CollisionMatrix;

			uint bColor = _vic.GetBorderColor();

			ushort x = _vic.XCoord;
			ushort y = _vic.Raster;
			ushort left = _vic.LeftBorder;

			uint pos = (uint)(y * VIC.X_RESOLUTION + x);
			for (uint lim = (uint)(pos + ((x + 8) < left ? 8 : (left - x))); pos < lim; pos++, x++)
			{
				collision[pos] = 0;
				output.OutputPixel(pos, bColor);
			}

			if (x > left)
				x = _vic.GraphicMode.Render(_vic, x, 0);
			else if (x == left)
				x = left == VIC.MAIN_BORDER[1, 0] ? _vic.GraphicMode.Render(_vic, (ushort)(x + _vic.XScroll), 0) : _vic.GraphicMode.Render(_vic, x, (byte)(7 - _vic.XScroll));

			SpriteGraphOp(x);

			_vic.XCoord = x;
		}

		private void CheckedActiveGraphRight1Op()
		{
			IVideoOutput output = _vic.OutputDevice;
			ushort[] collision = _vic.CollisionMatrix;

			_vic.GraphicMode.Render(_vic, _vic.XCoord, 0);

			ushort right = _vic.RightBorder;
			ushort x = VIC.MAIN_BORDER[0, 1];

			if (x >= right)
			{
				uint bColor = _vic.GetBorderColor();

				ushort y = _vic.Raster;
				uint pos = (uint)(y * VIC.X_RESOLUTION + x);
				uint lim = pos + 6;

				for (; pos < lim; pos++)
				{
					collision[pos] = 0;
					output.OutputPixel(pos, bColor);
				}
			}

			x += 5;

			SpriteGraphOp(x);

			_vic.XCoord = x;
		}

		private void CheckedActiveGraphRight2Op()
		{
			IVideoOutput output = _vic.OutputDevice;
			ushort[] collision = _vic.CollisionMatrix;

			ushort right = _vic.RightBorder;
			uint bColor = _vic.GetBorderColor();

			ushort x = _vic.XCoord;
			ushort y = _vic.Raster;
			uint pos = (uint)(y * VIC.X_RESOLUTION + x);
			uint lim = pos + 8;

			if (x < right)
				pos += 4;

			for (; pos < lim; pos++)
			{
				collision[pos] = 0;
				output.OutputPixel(pos, bColor);
			}

			x += 8;

			SpriteGraphOp(x);

			_vic.XCoord = x;
		}

		private void UncheckedActiveGraphOp()
		{
			ushort x = _vic.GraphicMode.Render(_vic, _vic.XCoord, 0);

			SpriteGraphOp(x);

			_vic.XCoord = x;
		}

		public RasterLine(VIC vic) { _vic = vic; }

		private Clock.ClockEntry[] _replacableOps;
		private Clock.ClockOp[] _activeOps;
		private Clock.ClockOp[] _borderOps;

		public Clock.ClockEntry CreateOps()
		{
			_activeOps = new Clock.ClockOp[]
			{
				new VicGraphReadOp(_vic, GraphReadOp15_53, CheckedActiveGraphLeftOp), 
				new VicGraphReadOp(_vic, GraphReadOp15_53, UncheckedActiveGraphOp),
				new VicGraphReadOp(_vic, GraphReadOp54, CheckedActiveGraphRight1Op),
				new VicGraphReadOp(_vic, IdleReadOp55, CheckedActiveGraphRight2Op)
			};

			_borderOps = new Clock.ClockOp[]
			{
				new VicGraphReadOp(_vic, GraphReadOp15_53, BorderGraphOp), 
				new VicGraphReadOp(_vic, GraphReadOp15_53, BorderGraphOp),
				new VicGraphReadOp(_vic, GraphReadOp54, BorderGraphOp),
				new VicGraphReadOp(_vic, IdleReadOp55, BorderGraphOp)
			};

			_replacableOps = new Clock.ClockEntry[]
			{
				new Clock.ClockEntryRep(_activeOps[0], 2),
				new Clock.ClockEntryRep(_activeOps[1], 37),
				new Clock.ClockEntry(_activeOps[2]),
				new Clock.ClockEntry(_activeOps[3]),
			};

			Clock.ClockEntry first = new Clock.ClockEntry(new VicReadOp(_vic, SpriteReadOp0));
			Clock.ClockEntry next = first.Next = new Clock.ClockEntryRep(new VicReadOp(_vic, SpriteReadOp1_9), 9);
			next = next.Next = new Clock.ClockEntryRep(new VicNop(_vic), 2);
			next = next.Next = new Clock.ClockEntry(new VicGraphOp(_vic, BorderWrapGraphOp));
			next = next.Next = new Clock.ClockEntry(new VicGraphReadOp(_vic, RefreshReadOp13, BorderGraphOp));
			next = next.Next = new Clock.ClockEntry(new VicGraphReadOp(_vic, RefreshReadOp14, BorderGraphOp));
			next = next.Next = _replacableOps[0];
			next = next.Next = _replacableOps[1];
			next = next.Next = _replacableOps[2];
			next = next.Next = _replacableOps[3];
			next = next.Next = new Clock.ClockEntry(new VicGraphReadOp(_vic, IdleReadOp56, BorderGraphOp));
			next = next.Next = new Clock.ClockEntry(new VicGraphReadOp(_vic, SpriteReadOp57, BorderGraphOp));
			next = next.Next = new Clock.ClockEntryRep(new VicReadOp(_vic, SpriteReadOp58_61), 4);
			next = next.Next = new Clock.ClockEntry(new VicReadOp62(_vic, SpriteReadOp62));
			next.Next = first;

			return first;
		}

		public void SetReplacableOperations(bool borderFlip, bool den)
		{
			if (!borderFlip && den)
			{
				for (int i = 0; i < _replacableOps.Length; i++)
					_replacableOps[i].Op = _activeOps[i];
			}
			else
			{
				for (int i = 0; i < _replacableOps.Length; i++)
					_replacableOps[i].Op = _borderOps[i];
			}
		}

		public void ReadDeviceState(IFile stateFile) { _rasteCycle = stateFile.ReadByte(); }

		public void WriteDeviceState(IFile stateFile) { stateFile.Write(_rasteCycle); }
	}

	public interface GraphicMode
	{
		ushort Render(VIC vic, ushort x, byte shift);
	}

	public class StandardTextMode : GraphicMode
	{
		public ushort Render(VIC vic, ushort x, byte shift)
		{
			IVideoOutput output = vic.OutputDevice;
			ushort[] collision = vic.CollisionMatrix;

			byte b1, b2;
			vic.ReadVideoMatrix(out b1, out b2);

			uint bgColor = vic.GetBackgroundColor(0);
			uint color = Pallete.CvtColors[b2];
			uint bits = (uint)(vic.Memory.Read(vic.GetCharGenMemoryAddress((ushort)((b1 << 3) | vic.RC))) << shift);

			ushort y = vic.Raster;
			uint pos = (uint)(y * VIC.X_RESOLUTION + x);
			for (uint lim = pos + 8; pos < lim; pos++)
			{
				bits <<= 1;
				uint pixel = bits & 0x100;

				output.OutputPixel(pos, pixel == 0 ? bgColor : color);
				collision[pos] = (ushort)pixel;
			}

			return (ushort)(x + 8 - shift);
		}
	}

	public class MulticolorTextMode : GraphicMode
	{
		public ushort Render(VIC vic, ushort x, byte shift)
		{
			IVideoOutput output = vic.OutputDevice;
			ushort[] collision = vic.CollisionMatrix;

			byte b1, b2;
			vic.ReadVideoMatrix(out b1, out b2);

			uint bgColor = vic.GetBackgroundColor(0);
			uint color = Pallete.CvtColors[b2 & 0x7];
			bool mc = (b2 & 0x8) != 0;

			ushort y = vic.Raster;
			uint pos = (uint)(y * VIC.X_RESOLUTION + x);

			if (mc)
			{
				uint bits = (uint)(vic.Memory.Read(vic.GetCharGenMemoryAddress((ushort)((b1 << 3) | vic.RC))) << (shift & ~1));

				bits <<= 2;
				uint pixel = (bits & 0x300) >> 8;

				bool next = (shift & 1) != 0;
				for (uint lim = pos + 8 - shift; pos < lim; pos++)
				{
					output.OutputPixel(pos, pixel < 3 ? vic.GetBackgroundColor((byte)pixel) : color);
					collision[pos] = (pixel & 2) != 0 ? (ushort)CollisionState.Foreground : (ushort)0;

					if (next)
					{
						bits <<= 2;
						pixel = (bits & 0x300) >> 8;
					}

					next = !next;
				}
			}
			else
			{
				uint bits = (uint)(vic.Memory.Read(vic.GetCharGenMemoryAddress((ushort)((b1 << 3) | vic.RC))) << shift);

				for (uint lim = pos + 8; pos < lim; pos++)
				{
					bits <<= 1;
					uint pixel = bits & 0x100;

					output.OutputPixel(pos, pixel == 0 ? bgColor : color);
					collision[pos] = (ushort)pixel;
				}
			}

			return (ushort)(x + 8 - shift);
		}
	}

	public class StandardBitmapMode : GraphicMode
	{
		public ushort Render(VIC vic, ushort x, byte shift)
		{
			IVideoOutput output = vic.OutputDevice;
			ushort[] collision = vic.CollisionMatrix;

			byte b1, b2;
			vic.ReadVideoMatrix(out b1, out b2);

			uint color0 = Pallete.CvtColors[b1 & 0x0f];
			uint color1 = Pallete.CvtColors[b1 >> 4];
			uint bits = (uint)(vic.Memory.Read(vic.GetBitmapMemoryAddress((ushort)((vic.VC << 3) | vic.RC))) << shift);

			ushort y = vic.Raster;
			uint pos = (uint)(y * VIC.X_RESOLUTION + x);
			for (uint lim = pos + 8; pos < lim; pos++)
			{
				bits <<= 1;
				uint pixel = bits & 0x100;

				output.OutputPixel(pos, pixel == 0 ? color0 : color1);
				collision[pos] = (ushort)pixel;
			}

			return (ushort)(x + 8 - shift);
		}
	}

	public class MulticolorBitmapMode : GraphicMode
	{
		static private uint[] _colors = new uint[4];

		public ushort Render(VIC vic, ushort x, byte shift)
		{
			IVideoOutput output = vic.OutputDevice;
			ushort[] collision = vic.CollisionMatrix;

			byte b1, b2;
			vic.ReadVideoMatrix(out b1, out b2);

			_colors[0] = vic.GetBackgroundColor(0);
			_colors[1] = Pallete.CvtColors[b1 >> 4];
			_colors[2] = Pallete.CvtColors[b1 & 0x0f];
			_colors[3] = Pallete.CvtColors[b2];

			ushort y = vic.Raster;
			uint pos = (uint)(y * VIC.X_RESOLUTION + x);

			uint bits = (uint)(vic.Memory.Read(vic.GetBitmapMemoryAddress((ushort)((vic.VC << 3) | vic.RC))) << (shift & ~1));

			bits <<= 2;
			uint pixel = pixel = (bits & 0x300) >> 8;

			bool next = (shift & 1) != 0;
			for (uint lim = pos + 8 - shift; pos < lim; pos++)
			{
				output.OutputPixel(pos, _colors[pixel]);
				collision[pos] = (bits & 2) != 0 ? (ushort)CollisionState.Foreground : (ushort)0;

				if (next)
				{
					bits <<= 2;
					pixel = (bits & 0x300) >> 8;
				}

				next = !next;
			}

			return (ushort)(x + 8 - shift);
		}
	}

	public class ExtendedColorTextMode : GraphicMode
	{
		public ushort Render(VIC vic, ushort x, byte shift)
		{
			IVideoOutput output = vic.OutputDevice;
			ushort[] collision = vic.CollisionMatrix;

			byte b1, b2;
			vic.ReadVideoMatrix(out b1, out b2);

			byte bgColor = (byte)(b1 >> 6);
			b1 &= 0x3f;

			uint color = Pallete.CvtColors[b2];
			uint bits = (uint)(vic.Memory.Read(vic.GetCharGenMemoryAddress((ushort)((b1 << 3) | vic.RC))) << shift);

			ushort y = vic.Raster;
			uint pos = (uint)(y * VIC.X_RESOLUTION + x);
			for (uint lim = pos + 8; pos < lim; pos++)
			{
				bits <<= 1;
				uint pixel = bits & 0x100;

				output.OutputPixel(pos, pixel == 0 ? vic.GetBackgroundColor(bgColor) : color);
				collision[pos] = (ushort)pixel;
			}

			return (ushort)(x + 8 - shift);
		}
	}

	public class InvalidTextMode : GraphicMode
	{
		public ushort Render(VIC vic, ushort x, byte shift) { return (ushort)(x + 8 - shift); }
	}

	public class InvalidBitmapMode : GraphicMode
	{
		public ushort Render(VIC vic, ushort x, byte shift) { return (ushort)(x + 8 - shift); }
	}

	public class InvalidMulticolorBitmapMode : GraphicMode
	{
		public ushort Render(VIC vic, ushort x, byte shift) { return (ushort)(x + 8 - shift); }
	}

	public enum CollisionState
	{
		Sprite0 = 0x01,
		Sprite1 = 0x02,
		Sprite2 = 0x04,
		Sprite3 = 0x08,
		Sprite4 = 0x10,
		Sprite5 = 0x20,
		Sprite6 = 0x40,
		Sprite7 = 0x80,
		SpriteX = 0xff,
		Foreground = 0x100
	}

	public class Pallete
	{
		public enum Names
		{
			Black,
			White,
			Red,
			Cyan,
			Purple,
			Green,
			Blue,
			Yellow,
			Orange,
			Brown,
			LightRed,
			DarkGrey,
			Grey,
			LightGreen,
			LightBlue,
			LightGrey
		}

		public static readonly byte[][] Colors = new byte[][]
		{
		    new byte[] {0x00, 0x00, 0x00},
		    new byte[] {0xFF, 0xFF, 0xFF},
		    new byte[] {0x68, 0x37, 0x2B},
		    new byte[] {0x70, 0xA4, 0xB2},
		    new byte[] {0x6F, 0x3D, 0x86},
		    new byte[] {0x58, 0x8D, 0x43},
		    new byte[] {0x35, 0x28, 0x79},
		    new byte[] {0xB8, 0xC7, 0x6F},
		    new byte[] {0x6F, 0x4F, 0x25},
		    new byte[] {0x43, 0x39, 0x00},
		    new byte[] {0x9A, 0x67, 0x59},
		    new byte[] {0x44, 0x44, 0x44},
		    new byte[] {0x6C, 0x6C, 0x6C},
		    new byte[] {0x9A, 0xD2, 0x84},
		    new byte[] {0x6C, 0x5E, 0xB5},
		    new byte[] {0x95, 0x95, 0x95}
		};

		public static uint[] CvtColors = new uint[16];

		//public static byte[] Lookup(byte color) { return Colors[color & 0x0f]; }
		//public static byte[] Lookup(Names color) { return Colors[(byte)color]; }
	}

	public class DataBuffer : State.IDeviceState
	{
		private uint _bits = 0;
		private byte _bitCount = 0;

		private bool _emptyMultiplier = true;
		private byte _multiplierData = 0;

		public void Clear()
		{
			_bits = _bitCount = 0;
			_emptyMultiplier = true;
			_multiplierData = 0;
		}

		public void Enqueue(byte bits)
		{
			_bits |= (uint)bits << (24 - _bitCount);
			_bitCount += 8;
		}

		public void Dequeue1(out byte bit)
		{
			bit = (byte)(_bits >> 31);

			_bits <<= 1;
			_bitCount--;
		}

		public void Dequeue2(out byte bits)
		{
			if (_emptyMultiplier)
			{
				_multiplierData = (byte)(_bits >> 30);

				_bits <<= 2;
				_bitCount -= 2;
			}

			_emptyMultiplier = !_emptyMultiplier;
			bits = _multiplierData;
		}

		public bool IsEmpty { get { return _bitCount == 0 && _emptyMultiplier; } }

		public void ReadDeviceState(IFile stateFile)
		{
			_bits = stateFile.ReadDWord();
			_bitCount = stateFile.ReadByte();
			_emptyMultiplier = stateFile.ReadBool();
			_multiplierData = stateFile.ReadByte();
		}

		public void WriteDeviceState(IFile stateFile)
		{
			stateFile.Write(_bits);
			stateFile.Write(_bitCount);
			stateFile.Write(_emptyMultiplier);
			stateFile.Write(_multiplierData);
		}
	}

	public class VIC : Memory.MemoryMappedDevice, State.IDeviceState
	{
		public const uint VIC_MEMORY_MAP_SIZE = 0x10000;

		public const byte VIC_GRAPHIC_MODES = 8;

		public const byte VIC_VIDEO_MATRIX_SIZE = 40;

		public const ushort X_RESOLUTION = 504;
		public const ushort Y_RESOLUTION = 312;

		public const ushort FIRST_X_COORD = 404;

		public static readonly ushort[,] AUX_BORDER = new ushort[,] { { 55, 246 }, { 51, 250 } };
		public static readonly ushort[,] MAIN_BORDER = new ushort[,] { { 31, 335 }, { 24, 343 } };

		public const int REGISTERS_COUNT = 64;
		private byte[] _registers = new byte[REGISTERS_COUNT];

		public enum Registers
		{
			M0_X, M0_Y, M1_X, M1_Y, M2_X, M2_Y, M3_X, M3_Y, M4_X, M4_Y, M5_X, M5_Y, M6_X, M6_Y, M7_X, M7_Y, Mx_X,
			CR_1, RASTER, LP_X, LP_Y, Mx_E, CR_2, Mx_YE, ME_P, IN_R, IN_E, Mx_P, Mx_MC, Mx_XE, Mx_CM, Mx_CD,
			EC, BC_0, BC_1, BC_2, BC_3,
			MM_0, MM_1, M0_C, M1_C, M2_C, M3_C, M4_C, M5_C, M6_C, M7_C
		}

		public enum CR1
		{
			YSCROLL = 0x07,
			RSEL = 0x08,
			DEN = 0x10,
			BMM = 0x20,
			ECM = 0x40,
			RST8 = 0x80
		}

		public enum CR2
		{
			XSCROLL = 0x07,
			CSEL = 0x08,
			MCM = 0x10,
			RES = 0x20
		}

		public enum IR
		{
			RST = 0x01,
			MBC = 0x02,
			MMC = 0x04,
			LP = 0x08,
			IRQ = 0x80
		}

		public enum MP
		{
			BB = 0x08,
			CB = 0x0e,
			VM = 0xf0
		}

		public VIC(ushort vicAddress, ushort vicSize, ushort cRamAddress, ushort cRamSize, IO.Irq irqLine, C64Interfaces.IVideoOutput outputDevice)
			: base(vicAddress, vicSize)
		{
			_irqLine = irqLine;
			_outputDevice = outputDevice;

			_colorRam = new Memory.ColorRAM(cRamAddress, cRamSize);

			_rasterLine = new RasterLine(this);
			_graphicMode = _graphicModes[0];

			for (int i = _sDataBuffer.Length - 1; i >= 0; i--)
				_sDataBuffer[i] = new DataBuffer();
		}

		private C64Interfaces.IVideoOutput _outputDevice = null;
		public C64Interfaces.IVideoOutput OutputDevice { get { return _outputDevice; } }

		private ushort _memoryBank;
		public ushort MemoryBank
		{
			get { return _memoryBank; }
			set { _memoryBank = value; }
		}

		private IO.Irq _irqLine = null;

		private Memory.ColorRAM _colorRam;
		public Memory.ColorRAM ColorRam { get { return _colorRam; } }

		private Memory.MemoryMap _memory = new Memory.MemoryMap(VIC_MEMORY_MAP_SIZE);
		public Memory.MemoryMap Memory { get { return _memory; } }

		private RasterLine _rasterLine;
		public RasterLine RasterLine { get { return _rasterLine; } }

		private ushort[] _collisionMatrix = new ushort[X_RESOLUTION * Y_RESOLUTION];
		public ushort[] CollisionMatrix { get { return _collisionMatrix; } }

		private GraphicMode[] _graphicModes = new GraphicMode[VIC_GRAPHIC_MODES]
		{
			new StandardTextMode(),
			new MulticolorTextMode(),
			new StandardBitmapMode(),
			new MulticolorBitmapMode(),
			new ExtendedColorTextMode(),
			new InvalidTextMode(),
			new InvalidBitmapMode(), 
			new InvalidMulticolorBitmapMode()
		};

		private GraphicMode _graphicMode = null;
		public GraphicMode GraphicMode { get { return _graphicMode; } }

		private ushort[] _spritePointers = new ushort[8];
		public ushort[] SpritePointers { get { return _spritePointers; } }

		private DataBuffer[] _sDataBuffer = new DataBuffer[8];
		public DataBuffer[] SDataBuffer { get { return _sDataBuffer; } }

		private byte[] _videoMatrix = new byte[2 * VIC_VIDEO_MATRIX_SIZE];

		private ushort _xCoord;
		public ushort XCoord
		{
			get { return _xCoord; }
			set { _xCoord = value; }
		}

		private byte _vmliRead = 0;
		public byte VMLIRead
		{
			get { return _vmliRead; }
			set { _vmliRead = value; }
		}

		private byte _vmliWrite = 0;
		public byte VMLIWrite
		{
			get { return _vmliWrite; }
			set { _vmliWrite = value; }
		}

		private ushort _vc = 0;
		public ushort VC
		{
			get { return _vc; }
			set { _vc = value; }
		}

		private ushort _vcBase = 0;
		public ushort VCBase
		{
			get { return _vcBase; }
			set { _vcBase = value; }
		}

		private byte _rc = 0;
		public byte RC
		{
			get { return _rc; }
			set { _rc = value; }
		}

		private byte[] _mc = new byte[8];
		public byte[] MC { get { return _mc; } }

		private byte[] _mcBase = new byte[8];
		public byte[] MCBase { get { return _mcBase; } }

		private bool[] _yExpandFlip = new bool[8];
		public bool[] YExpandFlip { get { return _yExpandFlip; } }

		private bool[] _spriteDMA = new bool[8];
		public bool[] SpriteDMA { get { return _spriteDMA; } }

		private bool[] _spriteDisplay = new bool[8];
		public bool[] SpriteDisplay { get { return _spriteDisplay; } }

		private bool _auxBorderFlip = true;
		public bool AuxBorderFlip
		{
			get { return _auxBorderFlip; }
			set { _auxBorderFlip = value; }
		}

		public void ReadVideoMatrix(out byte chr, out byte color)
		{
			chr = _videoMatrix[_vmliRead++];
			color = _videoMatrix[_vmliRead++];
		}

		public void WriteVideoMatrix(byte chr, byte color)
		{
			_videoMatrix[_vmliWrite++] = chr;
			_videoMatrix[_vmliWrite++] = color;
		}

		private bool _displayState = false;
		public bool DisplayState
		{
			get { return _displayState; }
			set { _displayState = value; }
		}

		private bool _badLine = false;
		public bool BadLine
		{
			get { return _badLine; }
			set { _badLine = value; }
		}

		private ushort _rasterComparison;
		public ushort RasterComparison { get { return _rasterComparison; } }

		private ushort _raster = 0;
		public ushort Raster
		{
			get { return _raster; }
			set
			{
				_raster = value;

				_registers[(byte)Registers.RASTER] = (byte)value;
				_registers[(byte)Registers.CR_1] = (byte)((_registers[(byte)Registers.CR_1] & ~(byte)CR1.RST8) | ((value >> 1) & (byte)CR1.RST8));

				if (_rasterComparison == value)
					SetInterrupt(IR.RST);
			}
		}

		public byte XScroll { get { return (byte)(_registers[(byte)Registers.CR_2] & (byte)CR2.XSCROLL); } }
		public byte YScroll { get { return (byte)(_registers[(byte)Registers.CR_1] & (byte)CR1.YSCROLL); } }

		private ushort _topBorder = AUX_BORDER[1, 0];
		private ushort _bottomBorder = AUX_BORDER[1, 1];
		private ushort _leftBorder = MAIN_BORDER[1, 0];
		private ushort _rightBorder = MAIN_BORDER[1, 1];

		public ushort TopBorder { get { return _topBorder; } }
		public ushort BottomBorder { get { return _bottomBorder; } }
		public ushort LeftBorder { get { return _leftBorder; } }
		public ushort RightBorder { get { return _rightBorder; } }

		public bool ExtendedColorMode { get { return (_registers[(byte)Registers.CR_1] & (byte)CR1.ECM) != 0; } }
		public bool BitmapMode { get { return (_registers[(byte)Registers.CR_1] & (byte)CR1.BMM) != 0; } }
		public bool MulticolorMode { get { return (_registers[(byte)Registers.CR_2] & (byte)CR2.MCM) != 0; } }

		public bool DisplayEnabled { get { return (_registers[(byte)Registers.CR_1] & (byte)CR1.DEN) != 0; } }

		public byte LightPanX { get { return _registers[(byte)Registers.LP_X]; } }
		public byte LightPanY { get { return _registers[(byte)Registers.LP_Y]; } }

		public bool GetSpriteEnabled(byte sprite) { return ((_registers[(byte)Registers.Mx_E] >> sprite) & 1) == 1; }

		public ushort GetSpritePosX(byte sprite) { return (ushort)(_registers[(byte)Registers.M0_X + sprite * 2] + (((_registers[(byte)Registers.Mx_X] >> sprite) & 1) << 8)); }
		public ushort GetSpritePosY(byte sprite) { return _registers[(byte)Registers.M0_Y + sprite * 2]; }

		public bool GetSpriteExpX(byte sprite) { return ((_registers[(byte)Registers.Mx_XE] >> sprite) & 1) == 1; }
		public bool GetSpriteExpY(byte sprite) { return ((_registers[(byte)Registers.Mx_YE] >> sprite) & 1) == 1; }

		public bool GetSpritePriority(byte sprite) { return ((_registers[(byte)Registers.Mx_P] >> sprite) & 1) == 0; }
		public bool GetSpriteMulticolor(byte sprite) { return ((_registers[(byte)Registers.Mx_MC] >> sprite) & 1) == 1; }

		public bool GetSpriteSpriteCollision(byte sprite) { return ((_registers[(byte)Registers.Mx_CM] >> sprite) & 1) == 1; }
		public bool GetSpriteDataCollision(byte sprite) { return ((_registers[(byte)Registers.Mx_CD] >> sprite) & 1) == 1; }

		public byte GetSpriteSpriteCollision() { return _registers[(byte)Registers.Mx_CM]; }
		public byte GetSpriteDataCollision() { return _registers[(byte)Registers.Mx_CD]; }

		public void SetSpriteSpriteCollision(byte spriteMask)
		{
			bool raiseInterrupt = _registers[(byte)Registers.Mx_CM] == 0;
			_registers[(byte)Registers.Mx_CM] |= spriteMask;

			if (raiseInterrupt)
				SetInterrupt(IR.MMC);
		}

		public void SetSpriteDataCollision(byte spriteMask)
		{
			bool raiseInterrupt = _registers[(byte)Registers.Mx_CM] == 0;
			_registers[(byte)Registers.Mx_CD] |= spriteMask;

			if (raiseInterrupt)
				SetInterrupt(IR.MBC);
		}

		public bool GetInterruptEnabled(IR type) { return (_registers[(byte)Registers.IN_E] & (byte)type) != 0; }

		public void SetInterrupt(IR type)
		{
			_registers[(byte)Registers.IN_R] |= (byte)type;

			if (GetInterruptEnabled(type) && (_registers[(byte)Registers.IN_R] & (byte)IR.IRQ) == 0)
			{
				_registers[(byte)Registers.IN_R] |= (byte)IR.IRQ;
				_irqLine.Raise();
			}
		}

		public byte GetSpriteMulitcolor(byte color) { return _registers[(byte)Registers.MM_0 + color]; }
		public byte GetSpriteColor(byte sprite) { return _registers[(byte)Registers.M0_C + sprite]; }

		private uint _borderColor;
		private uint[] _backGroundColor = new uint[4];

		public uint GetBorderColor() { return _borderColor; }
		public uint GetBackgroundColor(byte color) { return _backGroundColor[color]; }

		private ushort _videoMemoryBase;
		private ushort _charGenMemoryBase;
		private ushort _bitmapMemoryBase;

		public ushort GetRawMemoryAddress(ushort baseAddress) { return (ushort)((baseAddress & 0x3fff) | _memoryBank); }
		public ushort GetVideoMemoryAddress(ushort baseAddress) { return (ushort)(_videoMemoryBase | (baseAddress & 0x3ff) | _memoryBank); }
		public ushort GetCharGenMemoryAddress(ushort baseAddress) { return (ushort)(_charGenMemoryBase | (baseAddress & 0x7ff) | _memoryBank); }
		public ushort GetBitmapMemoryAddress(ushort baseAddress) { return (ushort)(_bitmapMemoryBase | (baseAddress & 0x7ff) | _memoryBank); }

		public override byte Read(ushort address)
		{
			address &= 0x3f;
			byte value = _registers[address];

			if (address == (byte)Registers.Mx_CM || address == (byte)Registers.Mx_CD)
				_registers[address] = 0;

			return value;
		}

		public override void Write(ushort address, byte value)
		{
			bool write = true;

			address &= 0x3f;

			switch (address)
			{
				case (ushort)Registers.RASTER:

					_rasterComparison = (byte)((_rasterComparison & 0xff00) | value);
					break;

				case (ushort)Registers.CR_1:

					_rasterComparison = (ushort)((_rasterComparison & 0xff) | ((value & (byte)CR1.RST8) << 1));

					_graphicMode = _graphicModes[((_registers[(ushort)Registers.CR_2] & (byte)CR2.MCM) | (value & (byte)(CR1.ECM | CR1.BMM))) >> 4];

					_rasterLine.SetReplacableOperations(_auxBorderFlip, (value & (byte)CR1.DEN) != 0);

					_topBorder = AUX_BORDER[(value & (byte)CR1.RSEL) >> 3, 0];
					_bottomBorder = AUX_BORDER[(value & (byte)CR1.RSEL) >> 3, 1];
					break;

				case (ushort)Registers.CR_2:

					_graphicMode = _graphicModes[((value & (byte)CR2.MCM) | (_registers[(ushort)Registers.CR_1] & (byte)(CR1.ECM | CR1.BMM))) >> 4];

					_leftBorder = MAIN_BORDER[(value & (byte)CR2.CSEL) >> 3, 0];
					_rightBorder = MAIN_BORDER[(value & (byte)CR2.CSEL) >> 3, 1];
					break;

				case (ushort)Registers.IN_R:

					_registers[address] &= (byte)((~value) | (byte)IR.IRQ);

					if ((_registers[address] & _registers[(byte)Registers.IN_E]) == 0 && (_registers[address] & (byte)IR.IRQ) != 0)
					{
						byte clear = (byte)IR.IRQ;
						_registers[address] &= (byte)~clear;
						_irqLine.Lower();
					}

					write = false;
					break;

				case (ushort)Registers.IN_E:

					if ((value & _registers[(byte)Registers.IN_R]) != 0 && (_registers[(byte)Registers.IN_R] & (byte)IR.IRQ) == 0)
					{
						_registers[(byte)Registers.IN_R] |= (byte)IR.IRQ;
						_irqLine.Raise();
					}
					else if ((value & _registers[(byte)Registers.IN_R]) == 0 && (_registers[(byte)Registers.IN_R] & (byte)IR.IRQ) != 0)
					{
						byte clear = (byte)IR.IRQ;
						_registers[(byte)Registers.IN_R] &= (byte)~clear;
						_irqLine.Lower();
					}
					break;

				case (ushort)Registers.ME_P:
					_videoMemoryBase = (ushort)((value & (byte)MP.VM) << 6);
					_charGenMemoryBase = (ushort)((value & (byte)MP.CB) << 10);
					_bitmapMemoryBase = (ushort)((value & (byte)MP.BB) << 10);
					break;

				case (ushort)Registers.EC:
					_borderColor = Pallete.CvtColors[value & 0x0f];
					break;

				case (ushort)Registers.BC_0:
					_backGroundColor[0] = Pallete.CvtColors[value & 0x0f];
					break;

				case (ushort)Registers.BC_1:
					_backGroundColor[1] = Pallete.CvtColors[value & 0x0f];
					break;

				case (ushort)Registers.BC_2:
					_backGroundColor[2] = Pallete.CvtColors[value & 0x0f];
					break;

				case (ushort)Registers.BC_3:
					_backGroundColor[3] = Pallete.CvtColors[value & 0x0f];
					break;

				default:

					if (address >= 0x20)
						value &= 0x0f;

					break;
			}

			if (write)
				_registers[address] = value;
		}

		public void ReadDeviceState(IFile stateFile)
		{
			stateFile.ReadBytes(_registers);
			_colorRam.ReadDeviceState(stateFile);
			_rasterLine.ReadDeviceState(stateFile);
			stateFile.ReadWords(_collisionMatrix);
			stateFile.ReadWords(_spritePointers);

			for (int i = 0; i < _sDataBuffer.Length; i++)
				_sDataBuffer[i].ReadDeviceState(stateFile);

			stateFile.ReadBytes(_videoMatrix);
			_xCoord = stateFile.ReadWord();
			_vmliRead = stateFile.ReadByte();
			_vmliWrite = stateFile.ReadByte();
			_vc = stateFile.ReadWord();
			_vcBase = stateFile.ReadWord();
			_rc = stateFile.ReadByte();
			stateFile.ReadBytes(_mc);
			stateFile.ReadBytes(_mcBase);

			stateFile.ReadBools(_yExpandFlip);
			stateFile.ReadBools(_spriteDMA);
			stateFile.ReadBools(_spriteDisplay);

			_auxBorderFlip = stateFile.ReadBool();
			_displayState = stateFile.ReadBool();
			_badLine = stateFile.ReadBool();
			_rasterComparison = stateFile.ReadWord();
			_raster = stateFile.ReadWord();

			_borderColor = stateFile.ReadDWord();
			stateFile.ReadDWords(_backGroundColor);

			_topBorder = stateFile.ReadWord();
			_bottomBorder = stateFile.ReadWord();
			_leftBorder = stateFile.ReadWord();
			_rightBorder = stateFile.ReadWord();

			_memoryBank = stateFile.ReadWord();
			_videoMemoryBase = stateFile.ReadWord();
			_charGenMemoryBase = stateFile.ReadWord();
			_bitmapMemoryBase = stateFile.ReadWord();

			_graphicMode = _graphicModes[((_registers[(ushort)Registers.CR_2] & (byte)CR2.MCM) | (_registers[(ushort)Registers.CR_1] & (byte)(CR1.ECM | CR1.BMM))) >> 4];
		}

		public void WriteDeviceState(IFile stateFile)
		{
			stateFile.Write(_registers);
			_colorRam.WriteDeviceState(stateFile);
			_rasterLine.WriteDeviceState(stateFile);
			stateFile.Write(_collisionMatrix);
			stateFile.Write(_spritePointers);

			for (int i = 0; i < _sDataBuffer.Length; i++)
				_sDataBuffer[i].WriteDeviceState(stateFile);

			stateFile.Write(_videoMatrix);
			stateFile.Write(_xCoord);
			stateFile.Write(_vmliRead);
			stateFile.Write(_vmliWrite);
			stateFile.Write(_vc);
			stateFile.Write(_vcBase);
			stateFile.Write(_rc);
			stateFile.Write(_mc);
			stateFile.Write(_mcBase);

			stateFile.Write(_yExpandFlip);
			stateFile.Write(_spriteDMA);
			stateFile.Write(_spriteDisplay);

			stateFile.Write(_auxBorderFlip);
			stateFile.Write(_displayState);
			stateFile.Write(_badLine);
			stateFile.Write(_rasterComparison);
			stateFile.Write(_raster);

			stateFile.Write(_borderColor);
			stateFile.Write(_backGroundColor);

			stateFile.Write(_topBorder);
			stateFile.Write(_bottomBorder);
			stateFile.Write(_leftBorder);
			stateFile.Write(_rightBorder);

			stateFile.Write(_memoryBank);
			stateFile.Write(_videoMemoryBase);
			stateFile.Write(_charGenMemoryBase);
			stateFile.Write(_bitmapMemoryBase);
		}
	}
}