
using C64Interfaces;

namespace Audio
{

	public class SID : Memory.MemoryMappedDevice, State.IDeviceState
	{
		public SID(ushort sidAddress, ushort sidSize)
			: base(sidAddress, sidSize)
		{
		}

		public override byte Read(ushort address)
		{
			return 0;
		}

		public override void Write(ushort address, byte value)
		{
		}

		void State.IDeviceState.ReadDeviceState(IFile stateFile)
		{
		}

		void State.IDeviceState.WriteDeviceState(IFile stateFile)
		{
		}
	}

}