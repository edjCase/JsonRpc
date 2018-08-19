using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace EdjCase.JsonRpc.Core.Tools
{
	public interface IStreamCompressor
	{
		void Compress(Stream inputStream, Stream outputStream, CompressionType compressionType);
		void Decompress(Stream inputStream, Stream outputStream, CompressionType compressionType);
	}

	public class DefaultStreamCompressor : IStreamCompressor
	{
		/// <summary>
		/// Decompresses the input stream to the output stream.
		/// </summary>
		/// <param name="inputStream">The input stream to decompress.</param>
		/// <param name="inputStream">The output stream to write to.</param>
		/// <param name="compressionType">Type of the compression.</param>
		/// <returns></returns>
		public void Decompress(Stream inputStream, Stream outputStream, CompressionType compressionType)
		{
			Stream compressionStream = null;
			try
			{
				switch (compressionType)
				{
					case CompressionType.Gzip:
						compressionStream = new GZipStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
						break;
					case CompressionType.Deflate:
						compressionStream = new DeflateStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null);
				}
				compressionStream.CopyTo(outputStream);
				outputStream.Position = 0;
			}
			finally
			{
				compressionStream?.Dispose();
			}
		}

		/// <summary>
		/// Compresses the input stream to the output stream.
		/// </summary>
		/// <param name="inputStream">The input stream to compress.</param>
		/// <param name="inputStream">The output stream to write to.</param>
		/// <param name="compressionType">Type of the compression.</param>
		/// <returns></returns>
		public void Compress(Stream inputStream, Stream outputStream, CompressionType compressionType)
		{
			long intialPosition = inputStream.Position;
			Stream compressionStream = null;
			try
			{
				switch (compressionType)
				{
					case CompressionType.Gzip:
						compressionStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true);
						break;
					case CompressionType.Deflate:
						compressionStream = new DeflateStream(outputStream, CompressionMode.Compress, leaveOpen: true);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null);
				}
				inputStream.CopyTo(compressionStream);
			}
			finally
			{
				compressionStream?.Dispose();
				inputStream.Position = intialPosition;
			}
		}
	}
}
