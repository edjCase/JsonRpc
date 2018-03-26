using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Logging;

namespace EdjCase.JsonRpc.Router.Defaults
{
	/// <summary>
	/// Rpc compressor that uses <see cref="System.IO.Compression"/>
	/// </summary>
	public class DefaultRpcCompressor : IRpcCompressor
	{
		/// <summary>
		/// Logger for logging the compression requests
		/// </summary>
		private ILogger<DefaultRpcCompressor> logger { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger">Optional logger for logging the compression requests</param>
		public DefaultRpcCompressor(ILogger<DefaultRpcCompressor> logger)
		{
			this.logger = logger;
		}


		/// <summary>
		/// Compressor that takes the <paramref name="text"/> and puts it in the <paramref name="outputStream"/> in a compressed format
		/// </summary>
		/// <param name="outputStream">Stream to write the compressed value to</param>
		/// <param name="text">Text to compress</param>
		/// <param name="encoding">Encoding to be used when compressing</param>
		/// <param name="compressionType">Type of compression to be used when compressing</param>
		public void CompressText(Stream outputStream, string text, Encoding encoding, CompressionType compressionType)
		{
			this.logger?.LogDebug($"Compressing the following text with the '{compressionType}' format: {text}");
			switch (compressionType)
			{
				case CompressionType.Gzip:
					using (GZipStream gZipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
					{
						StreamWriter streamWriter = new StreamWriter(gZipStream);
						streamWriter.Write(text);
						streamWriter.Flush();

					}
					break;
				case CompressionType.Deflate:
					using (DeflateStream deflateStream = new DeflateStream(outputStream, CompressionMode.Compress, leaveOpen: true))
					{
						StreamWriter streamWriter = new StreamWriter(deflateStream);
						streamWriter.Write(text);
						streamWriter.Flush();

					}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null);
			}
			this.logger?.LogDebug("Compression successful");
		}
	}
}
