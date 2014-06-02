using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace State
{

	public interface IDeviceState
	{
		void ReadDeviceState(C64Interfaces.IFile stateFile);
		void WriteDeviceState(C64Interfaces.IFile stateFile);
	}

}
