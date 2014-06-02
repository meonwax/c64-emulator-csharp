
namespace C64Interfaces
{
	public interface IFile
	{
		ulong Size { get; }
		ulong Pos { get; }

		void Read(byte[] memory, int offset, ushort size);

		byte ReadByte();
		ushort ReadWord();
		uint ReadDWord();
		ulong ReadQWord();
		bool ReadBool();
		void ReadBytes(byte[] data);
		void ReadWords(ushort[] data);
		void ReadDWords(uint[] data);
		void ReadBools(bool[] data);

		void Write(byte data);
		void Write(ushort data);
		void Write(uint data);
		void Write(ulong data);
		void Write(bool data);
		void Write(byte[] data);
		void Write(ushort[] data);
		void Write(uint[] data);
		void Write(bool[] data);

		void Close();
	}
}
