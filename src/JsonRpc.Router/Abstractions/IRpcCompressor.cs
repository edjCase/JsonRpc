using System.IO;
using System.Text;

namespace edjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcCompressor
	{
		void CompressText(Stream outputStream, string text, Encoding encoding, CompressionType compressionType);
	}

	public enum CompressionType
	{
		Gzip,
		Deflate
	}
}
