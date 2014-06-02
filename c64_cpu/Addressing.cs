
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

namespace CPU
{

	public interface AddressedTarget
	{
		void Write(byte value);
		byte Read();

		bool HasAddress { get; }
		ushort Address { get; }

		void BuildAddress(byte part);
		void MoveAddress(ushort offset);

		bool IsPageCrossed(ushort pc);

		void WriteToStateFile(C64Interfaces.IFile stateFile);
	}

	class ReadableTarget : AddressedTarget
	{
		protected MOS6502 _cpu;

		protected byte _part = 0;

		protected ushort _address;

		public ReadableTarget(MOS6502 cpu) : this(cpu, 0) { }
		public ReadableTarget(MOS6502 cpu, ushort address)
		{
			_cpu = cpu;
			_address = address;
		}
		public ReadableTarget(MOS6502 cpu, C64Interfaces.IFile stateFile)
			: this(cpu, stateFile.ReadWord()) { _part = stateFile.ReadByte(); }

		public virtual void Write(byte value) { throw new InvalidOperationException(); }
		public virtual byte Read() { return _cpu.Memory.Read(_address); }

		public virtual bool HasAddress { get { return true; } }
		public virtual ushort Address { get { return _address; } }

		public virtual void BuildAddress(byte part)
		{
			_address |= _part == 0 ? part : (ushort)(part << 8);
			_part++;
		}

		public virtual void MoveAddress(ushort offset) { _address += offset; }

		public virtual bool IsPageCrossed(ushort pc) { return (_address & 0xff00) != (pc & 0xff00); }

		public virtual void WriteToStateFile(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)TargetFactory.TargetTypes.ReadableTarget);

			stateFile.Write(_address);
			stateFile.Write(_part);
		}
	}

	class WritableTarget : ReadableTarget
	{
		public WritableTarget(MOS6502 cpu) : base(cpu) { }
		public WritableTarget(MOS6502 cpu, ushort address) : base(cpu, address) { }
		public WritableTarget(MOS6502 cpu, C64Interfaces.IFile stateFile)
			: base(cpu, stateFile) { }

		public override void Write(byte value) { _cpu.Memory.Write(_address, value); }

		public override void WriteToStateFile(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)TargetFactory.TargetTypes.WritableTarget);

			stateFile.Write(_address);
			stateFile.Write(_part);
		}
	}

	class IndirectTarget : WritableTarget
	{
		protected byte _tempPart = 0;

		protected ushort _tempAddress;
		public ushort TempAddress
		{
			get { return _tempAddress; }
			set { _tempAddress = value; }
		}

		public IndirectTarget(MOS6502 cpu) : base(cpu) { }
		public IndirectTarget(MOS6502 cpu, ushort tempAddress) : base(cpu) { _tempAddress = tempAddress; }
		public IndirectTarget(MOS6502 cpu, C64Interfaces.IFile stateFile)
			: base(cpu, stateFile)
		{
			_tempAddress = stateFile.ReadWord();
			_tempPart = stateFile.ReadByte();
		}

		public void BuildTempAddress(byte part)
		{
			_tempAddress |= _tempPart == 0 ? part : (ushort)(part << 8);
			_tempPart++;
		}

		public override void WriteToStateFile(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)TargetFactory.TargetTypes.IndirectTarget);

			stateFile.Write(_address);
			stateFile.Write(_part);

			stateFile.Write(_tempAddress);
			stateFile.Write(_tempPart);
		}
	}

	public interface AddressingMode
	{
		void Decode(MOS6502 cpu, byte cycle);
	}

	class AccAddressing : AddressingMode
	{
		public class Target : AddressedTarget
		{
			private CPUState _state;

			public Target(MOS6502 cpu) { _state = cpu.State; }

			public virtual void Write(byte value) { _state.A.Value = value; }

			public virtual byte Read() { return _state.A.Value; }

			public virtual bool HasAddress { get { return false; } }
			public virtual ushort Address { get { throw new InvalidOperationException(); } }

			public virtual void BuildAddress(byte part) { throw new InvalidOperationException(); }
			public virtual void MoveAddress(ushort offset) { throw new InvalidOperationException(); }

			public virtual bool IsPageCrossed(ushort pc) { return false; }

			public void WriteToStateFile(C64Interfaces.IFile stateFile) { stateFile.Write((byte)TargetFactory.TargetTypes.AccumulatorTarget); }
		}

		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
				cpu.Target = new Target(cpu);
		}
	}

	class ImmAddressing : AddressingMode
	{
		public class Target : ReadableTarget
		{
			public Target(MOS6502 cpu) : base(cpu, cpu.State.PC.Value) { _cpu.State.PC.Next(); }
			public Target(MOS6502 cpu, C64Interfaces.IFile stateFile) : base(cpu, stateFile) { }

			public override void Write(byte value) { throw new InvalidOperationException(); }

			public override bool IsPageCrossed(ushort pc) { return false; }

			public override void WriteToStateFile(C64Interfaces.IFile stateFile)
			{
				stateFile.Write((byte)TargetFactory.TargetTypes.ImmediateTarget);

				stateFile.Write(_address);
				stateFile.Write(_part);
			}
		}

		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
				cpu.Target = new Target(cpu);
		}
	}

	class AbsAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
				cpu.Target = new WritableTarget(cpu);

			if (cycle < 2)
			{
				cpu.Target.BuildAddress(cpu.Memory.Read(cpu.State.PC.Value));
				cpu.State.PC.Next();
			}
		}
	}

	class ZeroAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Target = new WritableTarget(cpu, cpu.Memory.Read(cpu.State.PC.Value));
				cpu.State.PC.Next();
			}
		}
	}

	class ZeroXAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Target = new WritableTarget(cpu, (byte)((cpu.Memory.Read(cpu.State.PC.Value) + cpu.State.X.Value) & 0xff));
				cpu.State.PC.Next();
			}
		}
	}

	class ZeroYAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Target = new WritableTarget(cpu, (ushort)((cpu.Memory.Read(cpu.State.PC.Value) + cpu.State.Y.Value) & 0xff));
				cpu.State.PC.Next();
			}
		}
	}

	class AbsXAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Target = new WritableTarget(cpu);

				cpu.Target.BuildAddress(cpu.Memory.Read(cpu.State.PC.Value));
				cpu.State.PC.Next();
			}
			else if (cycle == 1)
			{
				cpu.Target.BuildAddress(cpu.Memory.Read(cpu.State.PC.Value));
				cpu.State.PC.Next();

				cpu.Target.MoveAddress(cpu.State.X.Value);
			}
		}
	}

	class AbsYAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Target = new WritableTarget(cpu);

				cpu.Target.BuildAddress(cpu.Memory.Read(cpu.State.PC.Value));
				cpu.State.PC.Next();
			}
			else if (cycle == 1)
			{
				cpu.Target.BuildAddress(cpu.Memory.Read(cpu.State.PC.Value));
				cpu.State.PC.Next();

				cpu.Target.MoveAddress(cpu.State.Y.Value);
			}
		}
	}

	class ImpAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle) { }
	}

	class RelAddressing : AddressingMode
	{
		public virtual byte WriteTime { get { return 0; } }

		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
			{
				ushort offset = cpu.Memory.Read(cpu.State.PC.Value);
				cpu.State.PC.Next();

				cpu.Target = new WritableTarget(cpu, (ushort)(cpu.State.PC.Value + (sbyte)offset));
			}
		}
	}

	class IndAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle < 2)
			{
				if (cycle == 0)
					cpu.Target = new IndirectTarget(cpu);

				((IndirectTarget)cpu.Target).BuildTempAddress(cpu.Memory.Read(cpu.State.PC.Value));
				cpu.State.PC.Next();
			}
			else if (cycle < 4)
				cpu.Target.BuildAddress(cpu.Memory.Read(((IndirectTarget)cpu.Target).TempAddress++));
		}
	}

	class IndXAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Target = new IndirectTarget(cpu, (byte)(cpu.Memory.Read(cpu.State.PC.Value) + cpu.State.X.Value));
				cpu.State.PC.Next();
			}
			else if (cycle < 3)
				cpu.Target.BuildAddress(cpu.Memory.Read(((IndirectTarget)cpu.Target).TempAddress++));
		}
	}

	class IndYAddressing : AddressingMode
	{
		public virtual void Decode(MOS6502 cpu, byte cycle)
		{
			if (cycle == 0)
			{
				cpu.Target = new IndirectTarget(cpu, cpu.Memory.Read(cpu.State.PC.Value));
				cpu.State.PC.Next();
			}
			else if (cycle < 3)
			{
				cpu.Target.BuildAddress(cpu.Memory.Read(((IndirectTarget)cpu.Target).TempAddress++));

				if (cycle == 2)
					cpu.Target.MoveAddress(cpu.State.Y.Value);
			}
		}
	}

	public class TargetFactory
	{
		public enum TargetTypes
		{
			ReadableTarget,
			WritableTarget,
			IndirectTarget,
			AccumulatorTarget,
			ImmediateTarget
		}

		public static AddressedTarget ReadTargetFromStateFile(MOS6502 cpu, C64Interfaces.IFile stateFile)
		{
			switch (stateFile.ReadByte())
			{
				case (byte)TargetTypes.ReadableTarget:
					return new ReadableTarget(cpu, stateFile);

				case (byte)TargetTypes.WritableTarget:
					return new WritableTarget(cpu, stateFile);

				case (byte)TargetTypes.IndirectTarget:
					return new IndirectTarget(cpu, stateFile);

				case (byte)TargetTypes.AccumulatorTarget:
					return new AccAddressing.Target(cpu);

				case (byte)TargetTypes.ImmediateTarget:
					return new ImmAddressing.Target(cpu, stateFile);
			}

			return null;
		}
	}

}