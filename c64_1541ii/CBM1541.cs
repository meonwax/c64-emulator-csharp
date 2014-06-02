
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

	public class CBM1541 : State.IDeviceState
	{
		private enum Map
		{
			RamAddress = 0x0000, RamSize = 0x0f00,
			RomAddress = 0xc000, RomSize = 0x4000,
			Via1RegistersAddress = 0x1800, Via1RegistersSize = 0x0010,
			Via2RegistersAddress = 0x1c00, Via2RegistersSize = 0x0010,
		}

		private enum SerialPortPins
		{
			DataIn = 0x01,
			DataOut = 0x02,
			ClockIn = 0x04,
			ClockOut = 0x08,
			AtnaOut = 0x10,
			AtnIn = 0x80
		}

		private const ushort IDLE_TRAP_ADDRES = 0xec9b;
		private const byte IDLE_TRAP_OPCODE = 0x02;
		private const byte NOP_OPCODE = 0xea;
		private static readonly ushort[] PATCH_MAP = { 0xeae4, 0xeae5, 0xeae8, 0xeae9 };

		private class BoardClock : Clock.Clock
		{
			private bool _halted = false;

			public BoardClock(byte phases) : base(phases) { }

			public override void Run()
			{
				if (!_halted)
				{
					ExecuteWithCombo(0);
					ExecuteNoCombo(1);
					ExecuteNoCombo(2);
					ExecuteNoCombo(3);
				}
			}

			public override void Halt() { _halted = true; }
			public void Wake() { _halted = false; }

			public override void ReadDeviceState(C64Interfaces.IFile stateFile)
			{
				_halted = stateFile.ReadBool();
				ReadPhaseFromDeviceState(stateFile, 0);
			}

			public override void WriteDeviceState(C64Interfaces.IFile stateFile)
			{
				stateFile.Write(_halted);
				WritePhaseToDeviceState(stateFile, 0);
			}
		}

		private BoardClock _driveClock = new BoardClock(4);
		public Clock.Clock DriveClock { get { return _driveClock; } }

		private CPU.MOS6502 _driveCpu;
		public CPU.MOS6502 DriveCpu { get { return _driveCpu; } }

		private VIA[] _driveVias;
		public VIA[] DriveVIAs { get { return _driveVias; } }

		private Drive _drive;
		public Drive Drive { get { return _drive; } }

		private Memory.MemoryMap _memory = new Memory.MemoryMap(0x10000);
		public Memory.MemoryMap Mem { get { return _memory; } }

		private Memory.RAM _ram;
		private Memory.ROM _rom;

		private IO.SerialPort _serial;

		private IO.SerialPort.BusLineConnection _sAtnaConn;
		private IO.SerialPort.BusLineConnection _sDataConn;
		private IO.SerialPort.BusLineConnection _sClockConn;

		public CBM1541(C64Interfaces.IFile kernel, IO.SerialPort serial)
		{
			_driveCpu = new CPU.MOS6502(_memory, 0);
			_driveClock.OpFactory = _driveCpu.OpFactory;

			_driveVias = new VIA[2]
			{
				new VIA((ushort)Map.Via1RegistersAddress, (ushort)Map.Via1RegistersSize, _driveCpu.IRQ),
				new VIA((ushort)Map.Via2RegistersAddress, (ushort)Map.Via2RegistersSize, _driveCpu.IRQ)
			};

			_drive = new DiskDrive.Drive(_driveVias[1]);
			_drive.OnDataReady += new DiskDrive.Drive.DateReadyDelegate(drive_OnDataReady);

			_ram = new Memory.RAM((ushort)Map.RamAddress, (ushort)Map.RamSize);
			_rom = new Memory.ROM((ushort)Map.RomAddress, (ushort)Map.RomSize, kernel);

			_rom.Patch(IDLE_TRAP_ADDRES, IDLE_TRAP_OPCODE);
			for (int i = 0; i < PATCH_MAP.Length; i++)
				_rom.Patch(PATCH_MAP[i], NOP_OPCODE);

			_memory.Map(_ram, true);
			_memory.Map(_driveVias[0], true);
			_memory.Map(_driveVias[1], true);
			_memory.Map(_rom, Memory.MemoryMapEntry.AccessType.Read, true);

			_serial = serial;

			_sAtnaConn = new IO.SerialPort.BusLineConnection(_serial.DataLine);
			_sDataConn = new IO.SerialPort.BusLineConnection(_serial.DataLine);
			_sClockConn = new IO.SerialPort.BusLineConnection(_serial.ClockLine);

			_serial.OnAtnLineChanged += new IO.SerialPort.LineChangedDelegate(serial_OnAtnLineChanged);
			_serial.ClockLine.OnLineChanged += new IO.SerialPort.LineChangedDelegate(ClockLine_OnLineChanged);
			_serial.DataLine.OnLineChanged += new IO.SerialPort.LineChangedDelegate(DataLine_OnLineChanged);

			_driveVias[0].PortB.OnPortOut += new IO.IOPort.PortOutDelegate(PortB_OnPortOut);

			_driveCpu.Restart(_driveClock, 0);
			_driveClock.QueueOpsStart(_driveVias[0].CreateOps(), 1);
			_driveClock.QueueOpsStart(_driveVias[1].CreateOps(), 2);
			_driveClock.QueueOpsStart(_drive.CreateOps(), 3);
		}

		private void drive_OnDataReady() { _driveCpu.State.P.Overflow = true; }

		private bool _atnIn;
		private bool _atnaOut;

		private void serial_OnAtnLineChanged(bool state)
		{
			_atnIn = state;
			_sAtnaConn.LocalState = _atnaOut != state;

			_driveVias[0].PortB.SetSingleInputFast((byte)SerialPortPins.AtnIn, !state);
			_driveVias[0].CA1 = !state;

			_driveClock.Wake();
		}

		private void DataLine_OnLineChanged(bool state)
		{
			_driveVias[0].PortB.SetSingleInputFast((byte)SerialPortPins.DataIn, !state);

			_driveClock.Wake();
		}

		private void ClockLine_OnLineChanged(bool state)
		{
			_driveVias[0].PortB.SetSingleInputFast((byte)SerialPortPins.ClockIn, !state);

			_driveClock.Wake();
		}

		private void PortB_OnPortOut(byte states)
		{
			_atnaOut = (states & (byte)SerialPortPins.AtnaOut) != 0;
			_sAtnaConn.LocalState = _atnaOut != _atnIn;

			_sDataConn.LocalState = (states & (byte)SerialPortPins.DataOut) == 0;
			_sClockConn.LocalState = (states & (byte)SerialPortPins.ClockOut) == 0;
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_ram.ReadDeviceState(stateFile);
			_driveCpu.ReadDeviceState(stateFile);
			_driveVias[0].ReadDeviceState(stateFile);
			_driveVias[1].ReadDeviceState(stateFile);
			_drive.ReadDeviceState(stateFile);
			_sAtnaConn.ReadDeviceState(stateFile);
			_sClockConn.ReadDeviceState(stateFile);
			_sDataConn.ReadDeviceState(stateFile);

			_driveClock.ReadDeviceState(stateFile);
		}

		public void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			_ram.WriteDeviceState(stateFile);
			_driveCpu.WriteDeviceState(stateFile);
			_driveVias[0].WriteDeviceState(stateFile);
			_driveVias[1].WriteDeviceState(stateFile);
			_drive.WriteDeviceState(stateFile);
			_sAtnaConn.WriteDeviceState(stateFile);
			_sClockConn.WriteDeviceState(stateFile);
			_sDataConn.WriteDeviceState(stateFile);

			_driveClock.WriteDeviceState(stateFile);
		}
	}

}
