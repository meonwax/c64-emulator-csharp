
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

namespace Board
{

	public class Board : State.IDeviceState
	{
		private enum Map
		{
			CpuIOAddress = 0x0000, CpuIOSize = 0x0002,
			BasicRomAddress = 0xa000, BasicRomSize = 0x2000,
			VicRegistersAddress = 0xd000, VicRegistersSize = 0x0400,
			SidRegistersAddress = 0xd400, SidRegistersSize = 0x0400,
			ColorRamAddress = 0xd800, ColorRamSize = 0x0400,
			Cia1RegistersAddress = 0xdc00, Cia1RegistersSize = 0x0100,
			Cia2RegistersAddress = 0xdd00, Cia2RegistersSize = 0x0100,
			CharRomAddress = 0xd000, CharRomSize = 0x1000,
			KernelRomAddress = 0xe000, KernelRomSize = 0x2000,

			CharRomAddress_Vic1 = 0x1000,
			CharRomAddress_Vic2 = 0x9000,
		}

		private enum SerialPortPins
		{
			AtnOut = 0x08,
			ClockOut = 0x10,
			DataOut = 0x20,
			ClockIn = 0x40,
			DataIn = 0x80
		}

		private class BoardClock : Clock.Clock
		{
			public event PhaseEndDelegate OnTimeSlice;

			public BoardClock(byte phases) : base(phases) { }

			public override void Run()
			{
				while (true)
				{
					Running = true;

					while (Running)
					{
						ExecuteNoCombo(0);
						ExecuteWithCombo(1);
						ExecuteNoCombo(2);
						ExecuteNoCombo(3);

						RaiseOnPhaseEnd();

						//_clockCount++;
					}

					if (OnTimeSlice != null)
						OnTimeSlice();
				}
			}

			public override void Halt() { }

			public override void ReadDeviceState(C64Interfaces.IFile stateFile) { ReadPhaseFromDeviceState(stateFile, 1); }
			public override void WriteDeviceState(C64Interfaces.IFile stateFile) { WritePhaseToDeviceState(stateFile, 1); }
		}

		private BoardClock _systemClock = new BoardClock(4);
		public Clock.Clock SystemClock { get { return _systemClock; } }

		private byte _currentMap = 7;
		public Memory.MemoryMap Mem { get { return _memoryMaps[_currentMap]; } }

		private Memory.MemoryMap[] _memoryMaps = new Memory.MemoryMap[8]
		{
			 new Memory.MemoryMap(0x10000),
			 new Memory.MemoryMap(0x10000),
			 new Memory.MemoryMap(0x10000),
			 new Memory.MemoryMap(0x10000),
			 new Memory.MemoryMap(0x10000),
			 new Memory.MemoryMap(0x10000),
			 new Memory.MemoryMap(0x10000),
			 new Memory.MemoryMap(0x10000),
		};

		private Memory.RAM _systemRam;
		public Memory.RAM SysemRam { get { return _systemRam; } }

		private Memory.ROM _kernelRom;
		private Memory.ROM _basicRom;
		private Memory.ROM _charRom;

		private Memory.ROM _charRomVic1;
		private Memory.ROM _charRomVic2;

		private CPU.MOS6510 _systemCpu;
		public CPU.MOS6510 SystemCpu { get { return _systemCpu; } }

		private IO.CIA[] _systemCias;
		public IO.CIA[] SystemCias { get { return _systemCias; } }

		private Video.VIC _systemVic;
		public Video.VIC SystemVic { get { return _systemVic; } }

		private Audio.SID _systemSid;
		public Audio.SID SystemSid { get { return _systemSid; } }

		private IO.SerialPort _serial;
		public IO.SerialPort Serial { get { return _serial; } }

		private IO.SerialPort.BusLineConnection _sDataConn;
		private IO.SerialPort.BusLineConnection _sClockConn;

		public Board(C64Interfaces.IVideoOutput video, C64Interfaces.IFile kernel, C64Interfaces.IFile basic, C64Interfaces.IFile charGen)
		{
			_systemCpu = new CPU.MOS6510((ushort)Map.CpuIOAddress, (ushort)Map.CpuIOSize, _memoryMaps[_currentMap], 1);
			_systemClock.OpFactory = _systemCpu.OpFactory;

			_systemCpu.Port.IOPort.OnPortOut += new IO.IOPort.PortOutDelegate(CpuPort_OnMemoryMapChanged);

			_systemCias = new IO.CIA[]
			{
				new IO.CIA((ushort)Map.Cia1RegistersAddress, (ushort)Map.Cia1RegistersSize, _systemCpu.IRQ, 0x00, 0x00),
				new IO.CIA((ushort)Map.Cia2RegistersAddress, (ushort)Map.Cia2RegistersSize, _systemCpu.IRQ, 0x3f, 0x06)
			};

			_systemVic = new Video.VIC((ushort)Map.VicRegistersAddress, (ushort)Map.VicRegistersSize, (ushort)Map.ColorRamAddress, (ushort)Map.ColorRamSize,
				_systemCpu.IRQ, video);

			_systemSid = new Audio.SID((ushort)Map.SidRegistersAddress, (ushort)Map.SidRegistersSize);

			_systemCias[1].PortA.OnPortOut += new IO.IOPort.PortOutDelegate(Cia1_PortA_OnPortOut);

			_serial = new IO.SerialPort();
			_sDataConn = new IO.SerialPort.BusLineConnection(_serial.DataLine);
			_sClockConn = new IO.SerialPort.BusLineConnection(_serial.ClockLine);

			_serial.ClockLine.OnLineChanged += new IO.SerialPort.LineChangedDelegate(ClockLine_OnLineChanged);
			_serial.DataLine.OnLineChanged += new IO.SerialPort.LineChangedDelegate(DataLine_OnLineChanged);

			_systemRam = new Memory.RAM(0, 0x10000);

			_kernelRom = new Memory.ROM((ushort)Map.KernelRomAddress, (ushort)Map.KernelRomSize, kernel);
			_basicRom = new Memory.ROM((ushort)Map.BasicRomAddress, (ushort)Map.BasicRomSize, basic);
			_charRom = new Memory.ROM((ushort)Map.CharRomAddress, (ushort)Map.CharRomSize, charGen);

			_charRomVic1 = new Memory.ROM((ushort)Map.CharRomAddress_Vic1, (ushort)Map.CharRomSize, charGen);
			_charRomVic2 = new Memory.ROM((ushort)Map.CharRomAddress_Vic2, (ushort)Map.CharRomSize, charGen);

			for (int i = 0; i < _memoryMaps.Length; i++)
			{
				_memoryMaps[i].Map(_systemRam, true);
				_memoryMaps[i].Map(_systemCpu.Port, true);

				if ((i & 2) != 0)
				{
					_memoryMaps[i].Map(_kernelRom, Memory.MemoryMapEntry.AccessType.Read, true);

					if ((i & 1) != 0)
						_memoryMaps[i].Map(_basicRom, Memory.MemoryMapEntry.AccessType.Read, true);
				}

				if ((i & 3) != 0)
				{
					if ((i & 4) != 0)
					{
						_memoryMaps[i].Map(_systemVic, true);
						_memoryMaps[i].Map(_systemSid, true);
						_memoryMaps[i].Map(_systemVic.ColorRam, true);
						_memoryMaps[i].Map(_systemCias[0], true);
						_memoryMaps[i].Map(_systemCias[1], true);
					}
					else
						_memoryMaps[i].Map(_charRom, Memory.MemoryMapEntry.AccessType.Read, true);
				}
			}

			_systemVic.Memory.Map(_systemRam, true);
			_systemVic.Memory.Map(_charRomVic1, Memory.MemoryMapEntry.AccessType.Read, true);
			_systemVic.Memory.Map(_charRomVic2, Memory.MemoryMapEntry.AccessType.Read, true);

			_systemClock.QueueOpsStart(_systemVic.RasterLine.CreateOps(), 0);
			_systemCpu.Restart(_systemClock, 1);
			_systemClock.QueueOpsStart(_systemCias[0].CreateOps(), 2);
			_systemClock.QueueOpsStart(_systemCias[1].CreateOps(), 3);

			_systemClock.OnTimeSlice += new Clock.Clock.PhaseEndDelegate(_systemCias[0].IncrementTod);
			_systemClock.OnTimeSlice += new Clock.Clock.PhaseEndDelegate(_systemCias[1].IncrementTod);

			_systemClock.OnTimeSlice += new Clock.Clock.PhaseEndDelegate(_checkPendingStateOperations_OnTimeSlice);
		}

		public void Start() { _systemClock.Run(); }

		public delegate void StateOperationDelegate(C64Interfaces.IFile stateFile);
		public event StateOperationDelegate OnLoadState;
		public event StateOperationDelegate OnSaveState;

		public void LoadState(C64Interfaces.IFile stateFile)
		{
			lock (_systemClock)
			{
				if (_currentStateFile != null)
					throw new System.InvalidOperationException();

				_currentStateFile = stateFile;
				_pendingStateOperation = PendingStateOperations.Load;
			}
		}

		public void SaveState(C64Interfaces.IFile stateFile)
		{
			lock (_systemClock)
			{
				if (_currentStateFile != null)
					throw new System.InvalidOperationException();

				_currentStateFile = stateFile;
				_pendingStateOperation = PendingStateOperations.Save;
			}
		}

		private enum PendingStateOperations { Load, Save }
		private PendingStateOperations _pendingStateOperation;

		private C64Interfaces.IFile _currentStateFile = null;

		private void _checkPendingStateOperations_OnTimeSlice()
		{
			lock (_systemClock)
			{
				if (_currentStateFile != null)
				{
					switch (_pendingStateOperation)
					{
						case PendingStateOperations.Load:

							ReadDeviceState(_currentStateFile);

							if (OnLoadState != null)
								OnLoadState(_currentStateFile);

							_currentStateFile.Close();
							_currentStateFile = null;

							break;

						case PendingStateOperations.Save:

							WriteDeviceState(_currentStateFile);

							if (OnSaveState != null)
								OnSaveState(_currentStateFile);

							_currentStateFile.Close();
							_currentStateFile = null;

							break;
					}
				}
			}
		}

		private void ClockLine_OnLineChanged(bool state) { _systemCias[1].PortA.SetSingleInputFast((byte)SerialPortPins.ClockIn, state); }
		private void DataLine_OnLineChanged(bool state) { _systemCias[1].PortA.SetSingleInputFast((byte)SerialPortPins.DataIn, state); }

		private void Cia1_PortA_OnPortOut(byte states)
		{
			_systemVic.MemoryBank = (ushort)((~states & 3) << 14);

			_sDataConn.LocalState = (states & (byte)SerialPortPins.DataOut) == 0;
			_sClockConn.LocalState = (states & (byte)SerialPortPins.ClockOut) == 0;
			_serial.AtnLine = (states & (byte)SerialPortPins.AtnOut) == 0;
		}

		private void CpuPort_OnMemoryMapChanged(byte map)
		{
			_systemCpu.Memory = _memoryMaps[map & 7];
			_currentMap = map;
		}

		public void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_systemRam.ReadDeviceState(stateFile);
			_systemCpu.ReadDeviceState(stateFile);
			_systemVic.ReadDeviceState(stateFile);
			_systemCias[0].ReadDeviceState(stateFile);
			_systemCias[1].ReadDeviceState(stateFile);
			_serial.ReadDeviceState(stateFile);
			_sDataConn.ReadDeviceState(stateFile);
			_sClockConn.ReadDeviceState(stateFile);

			CpuPort_OnMemoryMapChanged(stateFile.ReadByte());

			_systemClock.ReadDeviceState(stateFile);
		}

		public void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			_systemRam.WriteDeviceState(stateFile);
			_systemCpu.WriteDeviceState(stateFile);
			_systemVic.WriteDeviceState(stateFile);
			_systemCias[0].WriteDeviceState(stateFile);
			_systemCias[1].WriteDeviceState(stateFile);
			_serial.WriteDeviceState(stateFile);
			_sDataConn.WriteDeviceState(stateFile);
			_sClockConn.WriteDeviceState(stateFile);

			stateFile.Write(_currentMap);

			_systemClock.WriteDeviceState(stateFile);
		}
	}

}