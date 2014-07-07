
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

namespace CPU
{

	public class DecodeOpcodeOp : Clock.ClockOp
	{
		private MOS6502 _cpu;

		public DecodeOpcodeOp(MOS6502 cpu) { _cpu = cpu; }

		public virtual void Execute(Clock.Clock clock, byte cycle)
		{
			Clock.ClockEntry first = null;
			Clock.ClockEntry next = null;

			if (_cpu.NMI.Check())
				first = next = new Clock.ClockEntryRep(new InterruptOp(_cpu, 0xfffa), 7);
			else if (_cpu.IRQ.IsRaised && !_cpu.State.P.IrqMask)
				first = next = new Clock.ClockEntryRep(new InterruptOp(_cpu, 0xfffe), 7);
			else
			{
				byte lastOpcode = _cpu.Opcode;

				_cpu.Opcode = _cpu.Memory.Read(_cpu.State.PC.Value);
				_cpu.State.PC.Next();

				DecodingTable.Entry decoded = DecodingTable.Opcodes[_cpu.Opcode];
				DecodeAddressOp addrOp = new DecodeAddressOp(_cpu, decoded._addressing);
				ExecuteOpcodeOp execOp = new ExecuteOpcodeOp(_cpu, decoded._instruction, decoded._timing._prolongOnPageCross);

				byte addrTime = decoded._timing._addressingTime;
				byte execTime = decoded._timing._execTime;

				first = addrTime < 2 ? new Clock.ClockEntry(addrOp, true) : new Clock.ClockEntryRep(addrOp, addrTime);
				first.ComboNext = execTime == 0 || addrTime == 0;

				next = first.Next = execTime < 2 ? new Clock.ClockEntry(execOp) : new Clock.ClockEntryRep(execOp, execTime);

				sbyte writeCycles = decoded._timing._writeTime;
				if (writeCycles >= 0)
				{
					if (writeCycles < 2)
					{
						next.ComboNext = writeCycles == 0;
						next = next.Next = new Clock.ClockEntry(new WriteResultOp(_cpu));
					}
					else
					{
						next = next.Next = new Clock.ClockEntryRep(new Clock.StallOp(), (byte)(writeCycles - 1));
						next = next.Next = new Clock.ClockEntry(new WriteResultOp(_cpu));
					}
				}
			}

			next.Next = new Clock.ClockEntry(new DecodeOpcodeOp(_cpu));
			clock.QueueOps(first, _cpu.Phase);
		}

		public void WriteToStateFile(C64Interfaces.IFile stateFile) { stateFile.Write((byte)MOS6502_OpFactory.Ops.DecodeOpcode); }

	}

	public class DecodeAddressOp : Clock.ClockOp
	{
		private MOS6502 _cpu;

		private AddressingMode _addressing;

		public DecodeAddressOp(MOS6502 cpu, AddressingMode addressing)
		{
			_cpu = cpu;
			_addressing = addressing;
		}

		public DecodeAddressOp(MOS6502 cpu, C64Interfaces.IFile stateFile)
			: this(cpu, (AddressingMode)null) { _addressing = DecodingTable.Opcodes[cpu.Opcode]._addressing; }

		public virtual void Execute(Clock.Clock clock, byte cycle) { _addressing.Decode(_cpu, cycle); }

		public void WriteToStateFile(C64Interfaces.IFile stateFile) { stateFile.Write((byte)MOS6502_OpFactory.Ops.DecodeAddressing); }
	}

	public class ExecuteOpcodeOp : Clock.ClockOp
	{
		private MOS6502 _cpu;

		private Instruction _instruction;

		private byte _pageCrossProlong;

		public ExecuteOpcodeOp(MOS6502 cpu, Instruction instruction, byte pageCrossProlong)
		{
			_cpu = cpu;
			_instruction = instruction;
			_pageCrossProlong = pageCrossProlong;
		}

		public ExecuteOpcodeOp(MOS6502 cpu, C64Interfaces.IFile stateFile)
			: this(cpu, null, stateFile.ReadByte()) { _instruction = DecodingTable.Opcodes[cpu.Opcode]._instruction; }

		public virtual void Execute(Clock.Clock clock, byte cycle) { _instruction.Execute(clock, _cpu, _pageCrossProlong, cycle); }

		public void WriteToStateFile(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)MOS6502_OpFactory.Ops.ExecuteOpcode);
			stateFile.Write(_pageCrossProlong);
		}
	}

	public class WriteResultOp : Clock.ClockOp
	{
		private MOS6502 _cpu;

		public WriteResultOp(MOS6502 cpu) { _cpu = cpu; }

		public virtual void Execute(Clock.Clock clock, byte cycle) { _cpu.Target.Write(_cpu.Result); }

		public void WriteToStateFile(C64Interfaces.IFile stateFile) { stateFile.Write((byte)MOS6502_OpFactory.Ops.WriteResult); }
	}

	public class InterruptOp : Clock.ClockOp
	{
		private MOS6502 _cpu;

		ushort _isrPointer;
		byte _readBuffer = 0;

		public InterruptOp(MOS6502 cpu, ushort isrPointer)
		{
			_cpu = cpu;
			_isrPointer = isrPointer;
		}

		public InterruptOp(MOS6502 cpu, C64Interfaces.IFile stateFile)
			: this(cpu, stateFile.ReadWord()) { _readBuffer = stateFile.ReadByte(); }

		public virtual void Execute(Clock.Clock clock, byte cycle)
		{
			if (cycle == 0)
			{
				_cpu.Memory.Write(_cpu.State.S.Value, _cpu.State.PC.PCH);
				_cpu.State.S.Value--;
			}
			else if (cycle == 1)
			{
				_cpu.Memory.Write(_cpu.State.S.Value, _cpu.State.PC.PCL);
				_cpu.State.S.Value--;
			}
			else if (cycle == 2)
			{
				_cpu.Memory.Write(_cpu.State.S.Value, _cpu.State.P.Value);
				_cpu.State.S.Value--;
			}
			else if (cycle == 3)
			{
				_readBuffer = _cpu.Memory.Read(_isrPointer);
			}
			else if (cycle == 4)
			{
				_cpu.State.PC.Value = (ushort)((_cpu.Memory.Read((ushort)(_isrPointer + 1)) << 8) | _readBuffer);
				_cpu.State.P.IrqMask = true;
			}
		}

		public void WriteToStateFile(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)MOS6502_OpFactory.Ops.Interrupt);
			stateFile.Write(_isrPointer);
			stateFile.Write(_readBuffer);
		}
	}

	public class ResetOp : Clock.ClockOp
	{
		private MOS6502 _cpu;

		byte _readBuffer = 0;

		public ResetOp(MOS6502 cpu) { _cpu = cpu; }

		public ResetOp(MOS6502 cpu, C64Interfaces.IFile stateFile)
			: this(cpu) { _readBuffer = stateFile.ReadByte(); }

		public virtual void Execute(Clock.Clock clock, byte cycle)
		{
			if (cycle == 0)
			{
				_cpu.State.A.Reset();
				_cpu.State.X.Reset();
				_cpu.State.Y.Reset();
				_cpu.State.S.Reset();
				_cpu.State.P.Reset();
			}
			else if (cycle == 1)
				_readBuffer = _cpu.Memory.Read(0xfffc);
			else if (cycle == 2)
				_cpu.State.PC.Value = (ushort)(_readBuffer | (_cpu.Memory.Read(0xfffd) << 8));
		}

		public void WriteToStateFile(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)MOS6502_OpFactory.Ops.Reset);
			stateFile.Write(_readBuffer);
		}
	}

	public class MOS6502_OpFactory : Clock.ClockOpFactory
	{
		public enum Ops
		{
			Stall = 0,
			DecodeOpcode,
			DecodeAddressing,
			ExecuteOpcode,
			WriteResult,
			Interrupt,
			Reset
		}

		private MOS6502 _cpu;
		public MOS6502_OpFactory(MOS6502 cpu) { _cpu = cpu; }

		public Clock.ClockOp CreateFromStateFile(C64Interfaces.IFile stateFile)
		{
			switch (stateFile.ReadByte())
			{
				case (byte)Ops.Stall:
					return new Clock.StallOp();

				case (byte)Ops.DecodeOpcode:
					return new DecodeOpcodeOp(_cpu);

				case (byte)Ops.DecodeAddressing:
					return new DecodeAddressOp(_cpu, stateFile);

				case (byte)Ops.ExecuteOpcode:
					return new ExecuteOpcodeOp(_cpu, stateFile);

				case (byte)Ops.WriteResult:
					return new WriteResultOp(_cpu);

				case (byte)Ops.Interrupt:
					return new InterruptOp(_cpu, stateFile);

				case (byte)Ops.Reset:
					return new ResetOp(_cpu, stateFile);
			}

			return null;
		}
	}

	public class MOS6502 : State.IDeviceState
	{
		private MOS6502_OpFactory _opFactory;
		public MOS6502_OpFactory OpFactory { get { return _opFactory; } }

		private CPUState _state = new CPUState();
		public CPUState State { get { return _state; } }

		private Memory.MemoryMap _memory;
		public Memory.MemoryMap Memory
		{
			get { return _memory; }
			set { _memory = value; }
		}

		private byte _phase;
		public byte Phase { get { return _phase; } }

		private byte _opcode;
		public byte Opcode
		{
			get { return _opcode; }
			set { _opcode = value; }
		}

		private AddressedTarget _target;
		public AddressedTarget Target
		{
			get { return _target; }
			set { _target = value; }
		}

		private byte _result;
		public byte Result
		{
			get { return _result; }
			set { _result = value; }
		}

		public MOS6502(Memory.MemoryMap memory, byte phase)
		{
			_opFactory = new MOS6502_OpFactory(this);

			_memory = memory;
			_phase = phase;
		}

		public void Restart(Clock.Clock clock, byte phase)
		{
			Clock.ClockEntry first = new Clock.ClockEntryRep(new ResetOp(this), 3);
			first.Next = new Clock.ClockEntry(new DecodeOpcodeOp(this));

			clock.QueueOpsStart(first, phase);
		}

		private IO.Irq _irq = new IO.Irq();
		public IO.Irq IRQ { get { return _irq; } }

		private IO.Nmi _nmi = new IO.Nmi();
		public IO.Nmi NMI { get { return _nmi; } }

		public virtual void ReadDeviceState(C64Interfaces.IFile stateFile)
		{
			_state.A.Value = stateFile.ReadByte();
			_state.X.Value = stateFile.ReadByte();
			_state.Y.Value = stateFile.ReadByte();
			_state.PC.Value = stateFile.ReadWord();
			_state.S.Value = stateFile.ReadWord();
			_state.P.Value = stateFile.ReadByte();

			_opcode = stateFile.ReadByte();
			_result = stateFile.ReadByte();

			_target = TargetFactory.ReadTargetFromStateFile(this, stateFile);

			_irq.ReadDeviceState(stateFile);
			_nmi.ReadDeviceState(stateFile);
		}

		public virtual void WriteDeviceState(C64Interfaces.IFile stateFile)
		{
			stateFile.Write(_state.A.Value);
			stateFile.Write(_state.X.Value);
			stateFile.Write(_state.Y.Value);
			stateFile.Write(_state.PC.Value);
			stateFile.Write(_state.S.Value);
			stateFile.Write(_state.P.Value);

			stateFile.Write(_opcode);
			stateFile.Write(_result);

			_target.WriteToStateFile(stateFile);

			_irq.WriteDeviceState(stateFile);
			_nmi.WriteDeviceState(stateFile);
		}
	}

}