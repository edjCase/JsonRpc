using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonRpc.Router.Abstractions;

namespace JsonRpc.Router
{
	public class DefaultRpcCompressor : IRpcCompressor
	{
		public void CompressText(Stream outputStream, string text, Encoding encoding, CompressionType compressionType)
		{
			switch (compressionType)
			{
				case CompressionType.Gzip:
					using (GZipStream gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
					{
						using (StreamWriter streamWriter = new StreamWriter(gZipStream))
						{
							streamWriter.Write(text);
							streamWriter.Flush();
						}
					}
					break;
				case CompressionType.Deflate:
					using (DeflateStream deflateStream = new DeflateStream(outputStream, CompressionMode.Compress))
					{
						using (StreamWriter streamWriter = new StreamWriter(deflateStream))
						{
							streamWriter.Write(text);
							streamWriter.Flush();
						}
					}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null);
			}
		}
	}
}
