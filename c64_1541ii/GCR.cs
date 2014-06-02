
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

	public class GCRImage : State.IDeviceState
	{
		public static readonly byte[] TO_GCR = new byte[]
		{
			0x0a, 0x0b, 0x12, 0x13, 0x0e, 0x0f, 0x16, 0x17,
			0x09, 0x19, 0x1a, 0x1b, 0x0d, 0x1d, 0x1e, 0x15
		};

		public static readonly byte[] TO_DATA = new byte[]
		{
			0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
			0x0, 0x8, 0x0, 0x1, 0x0, 0xc, 0x4, 0x5,
			0x0, 0x0, 0x2, 0x3, 0x0, 0xf, 0x6, 0x7,
			0x0, 0x9, 0xa, 0xb, 0x0, 0xd, 0xe, 0x0
		};

		public static readonly byte[] SPT = new byte[]
		{
			/*  1 - 17 */ 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 
			/* 18 - 24 */ 19, 19, 19, 19, 19, 19, 19, 
			/* 25 - 30 */ 18, 18, 18, 18, 18, 18, 
			/* 31 - 40 */ 17, 17, 17, 17, 17, 17, 17, 17, 17, 17
		};

		public static readonly byte[] DEN = new byte[] { 26, 28, 30, 32 };

		public const byte TRACK_COUNT = 80;

		private const ushort GCR_SYNC_LEN = 5;
		private const ushort GCR_HEADER_LEN = 10;
		private const ushort GCR_HEADER_GAP_LEN = 9;
		private const ushort GCR_DATA_LEN = 325;
		private const ushort GCR_SECTOR_GAP_LEN = 8;
		private const ushort GCR_SECTOR_LEN = GCR_SYNC_LEN + GCR_HEADER_LEN +
			GCR_HEADER_GAP_LEN + GCR_SYNC_LEN + GCR_DATA_LEN + GCR_SECTOR_GAP_LEN;

		private const ushort RAW_SECT_LEN = 256;
		private const ushort PRE_GCR_SECT_LEN = 260;

		public const byte GCR_SYNC_BYTE = 0xff;
		public const byte GCR_GAP_BYTE = 0x55;

		private byte[][] _tracks;
		public byte[][] Tracks { get { return _tracks; } }

		public GCRImage(C64Interfaces.IFile diskImage)
		{
			ushort id = 0;

			byte[] sector = new byte[RAW_SECT_LEN];
			int offset = 0;

			_tracks = new byte[TRACK_COUNT][];
			for (byte i = 0; i < TRACK_COUNT; i++)
			{
				byte track = (byte)(i >> 1);
				_tracks[i] = new byte[SPT[track] * GCR_SECTOR_LEN];

				if ((i & 1) == 0)
				{
					if ((ulong)offset < diskImage.Size)
					{
						ushort start = 0;
						for (byte j = 0; j < SPT[track]; j++)
						{
							diskImage.Read(sector, offset, RAW_SECT_LEN);
							ConvertSectorToGCR(sector, _tracks[i], ref start, j, (byte)(track + 1), id);

							offset += RAW_SECT_LEN;
						}
					}
				}
				else
				{
					ushort start = 0;

					for (byte j = 0; j < SPT[track]; j++)
						ConvertSectorToGCR(sector, _tracks[i], ref start, j, (byte)(track + 1), id);
				}
			}
		}

		private static void ConvertSectorToGCR(byte[] data, byte[] encoded, ref ushort encodedStart, byte sector, byte track, ushort id)
		{
			WriteToGCRStream(encoded, ref encodedStart, GCR_SYNC_BYTE, GCR_SYNC_LEN);
			EncodeGCR(CreateSectorHeader(sector, track, id), encoded, ref encodedStart);
			WriteToGCRStream(encoded, ref encodedStart, GCR_GAP_BYTE, GCR_HEADER_GAP_LEN);

			WriteToGCRStream(encoded, ref encodedStart, GCR_SYNC_BYTE, GCR_SYNC_LEN);
			EncodeGCR(CreateSectorData(data), encoded, ref encodedStart);
			WriteToGCRStream(encoded, ref encodedStart, GCR_GAP_BYTE, GCR_SECTOR_GAP_LEN);
		}

		private static void WriteToGCRStream(byte[] encoded, ref ushort encodedStart, byte data, ushort length)
		{
			for (int i = 0; i < length; i++)
				encoded[encodedStart++] = data;
		}

		private static void EncodeGCR(byte[] data, byte[] encoded, ref ushort encodeStart)
		{
			byte shift = 63 - 4;
			ulong e = 0;

			for (int i = 0, j = encodeStart; i < data.Length; i++)
			{
				byte b = data[i];
				b = (byte)((b >> 4) | (b << 4));

				for (int k = 2; k > 0; k--)
				{
					e |= (ulong)TO_GCR[b & 0x0f] << shift;
					b >>= 4;
					shift -= 5;
				}

				if (i % 4 == 3)
				{
					byte[] buffer = System.BitConverter.GetBytes(e);
					for (short k = 7; k > 2; k--)
						encoded[encodeStart++] = buffer[k];

					shift = 63 - 4;
					e = 0;
				}
			}
		}

		private static byte[] CreateSectorHeader(byte sector, byte track, ushort id)
		{
			byte[] header = new byte[8];

			header[0] = 0x08;

			header[2] = sector;
			header[3] = track;

			header[4] = (byte)id;
			header[5] = (byte)(id >> 8);

			header[1] = (byte)(header[2] ^ header[3] ^ header[4] ^ header[5]);
			header[6] = header[7] = 0x0f;

			return header;
		}

		private static byte[] CreateSectorData(byte[] data)
		{
			byte[] sector = new byte[PRE_GCR_SECT_LEN];

			sector[0] = 0x07;

			byte checksum = 0;

			for (int i = 0; i < data.Length; i++)
			{
				sector[i + 1] = data[i];
				checksum ^= data[i];
			}

			sector[0x101] = checksum;
			sector[0x102] = sector[0x103] = 0;

			return sector;
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			byte trackCount = stateFile.ReadByte();

			for (int i = 0; i < _tracks.Length; i++)
			{
				ushort sectoirCount = stateFile.ReadWord();
				stateFile.ReadBytes(_tracks[i]);
			}
		}

		public void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)_tracks.Length);

			for (int i = 0; i < _tracks.Length; i++)
			{
				stateFile.Write((ushort)_tracks[i].Length);
				stateFile.Write(_tracks[i]);
			}
		}
	}

}
