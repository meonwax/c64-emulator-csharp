
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

using System.Collections.Generic;

namespace Clock
{

	public interface ClockOp
	{
		void Execute(Clock clock, byte cycle);
		void WriteToStateFile(C64Interfaces.IFile stateFile);
	}

	public interface ClockOpFactory
	{
		ClockOp CreateFromStateFile(C64Interfaces.IFile stateFile);
	}

	public class StallOp : ClockOp
	{
		public void Execute(Clock clock, byte cycle) { }
		public void WriteToStateFile(C64Interfaces.IFile stateFile) { stateFile.Write((byte)0); }
	}

	public class ClockEntry
	{
		protected ClockEntry _next;

		protected bool _comboNext;
		public bool ComboNext
		{
			get { return _comboNext; }
			set { _comboNext = value; }
		}

		protected ClockOp _op;
		public ClockOp Op
		{
			get { return _op; }
			set { _op = value; }
		}

		public ClockEntry(ClockOp op)
		{
			_op = op;
			_comboNext = false;
		}

		public ClockEntry(ClockOp op, bool comboNext)
		{
			_op = op;
			_comboNext = comboNext;
		}

		public ClockEntry(C64Interfaces.IFile stateFile, ClockOpFactory factory)
		{
			_op = factory.CreateFromStateFile(stateFile);
			_comboNext = stateFile.ReadBool();
		}

		public virtual bool Execute(Clock clock, out ClockEntry next)
		{
			_op.Execute(clock, 0);
			next = _next;

			return _comboNext;
		}

		public ClockEntry Next
		{
			get { return _next; }
			set { _next = value; }
		}

		public virtual void WriteToStateFile(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)0);

			_op.WriteToStateFile(stateFile);
			stateFile.Write(_comboNext);
		}
	}

	public class ClockEntryRep : ClockEntry
	{
		protected byte _length;
		public byte Length
		{
			get { return _length; }
			set { _length = value; }
		}

		protected byte _cycle;
		public byte Cycle
		{
			get { return _cycle; }
			set { _cycle = value; }
		}

		public ClockEntryRep(ClockOp op, byte length)
			: base(op)
		{
			_length = length;
			_cycle = 0;
		}

		public ClockEntryRep(ClockOp op, bool comboNext, byte length)
			: base(op, comboNext)
		{
			_length = length;
			_cycle = 0;
		}

		public ClockEntryRep(C64Interfaces.IFile stateFile, ClockOpFactory factory)
			: base(stateFile, factory)
		{
			_length = stateFile.ReadByte();
			_cycle = stateFile.ReadByte();
		}

		public override bool Execute(Clock clock, out ClockEntry next)
		{
			_op.Execute(clock, _cycle);

			_cycle = (byte)((_cycle + 1) % _length);
			if (_cycle == 0)
			{
				next = _next;
				return _comboNext;
			}
			else
			{
				next = this;
				return false;
			}
		}

		public override void WriteToStateFile(C64Interfaces.IFile stateFile)
		{
			stateFile.Write((byte)1);

			_op.WriteToStateFile(stateFile);

			stateFile.Write(_comboNext);
			stateFile.Write(_length);
			stateFile.Write(_cycle);
		}
	}

	public abstract class Clock : State.IDeviceState
	{
		//protected long _clockCount = 0;
		//public long ClockCount { get { return _clockCount; } }

		private ClockEntry[] _currentOps;

		private ClockOpFactory _opFactory;
		public ClockOpFactory OpFactory
		{
			get { return _opFactory; }
			set { _opFactory = value; }
		}

		public delegate void PhaseEndDelegate();
		public event PhaseEndDelegate OnPhaseEnd;
		protected void RaiseOnPhaseEnd()
		{
			if (OnPhaseEnd != null)
				OnPhaseEnd();
		}

		private bool _running;
		public bool Running
		{
			get { return _running; }
			set { _running = value; }
		}

		public Clock(byte phases) { _currentOps = new ClockEntry[phases]; }

		public abstract void Run();

		public abstract void Halt();

		public void QueueOpsStart(ClockEntry first, byte phase) { _currentOps[phase] = first; }

		public void QueueOps(ClockEntry ops, byte phase) { _currentOps[phase].Next = ops; }

		public void Stall(byte cycles, byte phase)
		{
			if (cycles > 0)
			{
				ClockEntry stall = cycles > 1 ? new ClockEntryRep(new StallOp(), cycles) : new ClockEntry(new StallOp());

				stall.Next = _currentOps[phase];
				_currentOps[phase] = stall;
			}
		}

		public void Prolong(byte cycles, byte phase)
		{
			if (cycles > 0)
			{
				ClockEntry stall = cycles > 1 ? new ClockEntryRep(new StallOp(), cycles) : new ClockEntry(new StallOp());

				stall.Next = _currentOps[phase].Next;
				_currentOps[phase].Next = stall;
			}
		}

		public abstract void ReadDeviceState(C64Interfaces.IFile stateFile);
		public abstract void WriteDeviceState(C64Interfaces.IFile stateFile);

		protected void ExecuteNoCombo(byte phase) { _currentOps[phase].Execute(this, out _currentOps[phase]); }
		protected void ExecuteWithCombo(byte phase)
		{
			while (_currentOps[phase].Execute(this, out _currentOps[phase]))
				;
		}

		protected void ReadPhaseFromDeviceState(C64Interfaces.IFile stateFile, byte phase)
		{
			ClockEntry previousOp = null;
			byte opCount = stateFile.ReadByte();
			for (byte i = 0; i < opCount; i++)
			{
				ClockEntry op = stateFile.ReadByte() == 0 ? new ClockEntry(stateFile, _opFactory) : new ClockEntryRep(stateFile, _opFactory);

				if (previousOp == null)
					_currentOps[phase] = op;
				else
					previousOp.Next = op;

				previousOp = op;
			}
		}

		protected void WritePhaseToDeviceState(C64Interfaces.IFile stateFile, byte phase)
		{
			byte opCount = 0;
			for (ClockEntry op = _currentOps[phase]; op != null; op = op.Next)
				opCount++;

			stateFile.Write(opCount);

			for (ClockEntry op = _currentOps[phase]; op != null; op = op.Next)
				op.WriteToStateFile(stateFile);
		}
	}

}