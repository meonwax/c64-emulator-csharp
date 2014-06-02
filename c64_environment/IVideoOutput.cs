
namespace C64Interfaces
{
	public interface IVideoOutput
	{
		void OutputPixel(uint pos, uint color);
		void Flush();
	}
}