using System.Buffers;
using System.IO;
using System.Text;

namespace EdjCase.JsonRpc.Router.Utilities
{
	internal class StreamUtil
	{
		internal static MemoryStream GetStreamFromUtf8String(string utf8Text)
		{
			byte[] bytes = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(utf8Text));
			int byteCount = Encoding.UTF8.GetBytes(utf8Text, 0, utf8Text.Length, bytes, 0);
			return new MemoryStream(bytes, 0, byteCount);
		}
	}
}