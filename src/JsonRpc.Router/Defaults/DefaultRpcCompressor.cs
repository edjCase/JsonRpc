using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using JsonRpc.Router.Abstractions;
using Microsoft.Framework.Logging;

namespace JsonRpc.Router.Defaults
{
	public class DefaultRpcCompressor : IRpcCompressor
	{
		public ILogger Logger { get; set; }
		public DefaultRpcCompressor(ILogger logger = null)
		{
			this.Logger = logger;
		}

		public void CompressText(Stream outputStream, string text, Encoding encoding, CompressionType compressionType)
		{
			this.Logger?.LogVerbose($"Compressing the following text with the '{compressionType}' format: {text}");
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
			this.Logger?.LogVerbose("Compression successful");
		}
	}
}
