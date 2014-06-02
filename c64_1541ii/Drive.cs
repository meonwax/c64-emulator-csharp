
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

namespace DiskDrive
{
	public class Drive : Clock.ClockOp, State.IDeviceState
	{
		public delegate void DateReadyDelegate();

		private VIA _controller;

		private GCRImage _attachedImage;

		private byte _headTrackPos;
		private ushort _headSectorPos;

		private byte _lastHeadDirection;

		private bool _spinning;

		private byte _density;

		private byte _cycleCount;

		private byte _lastData;

		public Drive(VIA controller)
		{
			_controller = controller;
			_controller.PortB.OnPortOut += new IO.IOPort.PortOutDelegate(ControlPort_OnPortOut);

			_density = GCRImage.DEN[0];
		}

		public void Execute(Clock.Clock clock, byte cycle)
		{
			_controller.CA1 = true;

			_cycleCount++;
			if (_cycleCount >= _density)
			{
				if (_controller.CB2)
				{
					if (_attachedImage != null)
					{
						byte data = _attachedImage.Tracks[_headTrackPos][_headSectorPos];
						bool sync = _lastData == GCRImage.GCR_SYNC_BYTE && data == GCRImage.GCR_SYNC_BYTE;
						_controller.PortB.Input = !sync ? (byte)0x80 : (byte)0x00;

						_lastData = data;
						_controller.PortA.Input = data;

						if (!sync && _controller.CA2)
						{
							_controller.CA1 = false;
							RaiseOnDataReady();
						}
					}
				}
				else
				{
					// write
				}

				if (_attachedImage != null)
				{
					_headSectorPos++;
					if (_headSectorPos >= _attachedImage.Tracks[_headTrackPos].Length)
						_headSectorPos = 0;
				}

				_cycleCount = 0;
			}
		}

		public void WriteToStateFile(C64Interfaces.IFile stateFile) { }

		public void Attach(C64Interfaces.IFile diskImage)
		{
			DetachImage();
			_attachedImage = new GCRImage(diskImage);
		}

		public void DetachImage()
		{
			_attachedImage = null;

			_headTrackPos = 0;
			_headSectorPos = 0;

			_spinning = false;
			_lastHeadDirection = _cycleCount = _lastData = 0;

			_density = GCRImage.DEN[0];
		}

		public Clock.ClockEntry CreateOps()
		{
			Clock.ClockEntry op = new Clock.ClockEntry(this);
			op.Next = op;

			return op;
		}

		public event DateReadyDelegate OnDataReady;

		private void RaiseOnDataReady()
		{
			if (OnDataReady != null)
				OnDataReady();
		}

		private void ControlPort_OnPortOut(byte states)
		{
			MoveHead((byte)(states & 3));
			_density = GCRImage.DEN[(byte)((states >> 5) & 3)];
			_spinning = (states & 4) != 0;
		}

		private void MoveHead(byte headDirection)
		{
			if (_lastHeadDirection != headDirection)
			{
				if (((_lastHeadDirection - 1) & 3) == headDirection)
				{
					if (_headTrackPos > 0)
						_headTrackPos--;
				}
				else if (((_lastHeadDirection + 1) & 3) == headDirection)
				{
					if (_headTrackPos < GCRImage.TRACK_COUNT - 1)
						_headTrackPos++;
				}

				_headSectorPos %= (ushort)_attachedImage.Tracks[_headTrackPos].Length;
				_lastHeadDirection = headDirection;
			}
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_headTrackPos = stateFile.ReadByte();
			_headSectorPos = stateFile.ReadWord();
			_lastHeadDirection = stateFile.ReadByte();
			_spinning = stateFile.ReadBool();
			_density = stateFile.ReadByte();
			_cycleCount = stateFile.ReadByte();
			_lastData = stateFile.ReadByte();
		}

		public void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			stateFile.Write(_headTrackPos);
			stateFile.Write(_headSectorPos);
			stateFile.Write(_lastHeadDirection);
			stateFile.Write(_spinning);
			stateFile.Write(_density);
			stateFile.Write(_cycleCount);
			stateFile.Write(_lastData);
		}
	}

}
