using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace EdjCase.JsonRpc.Common.Tools
{
	internal interface IStreamCompressor
	{
		bool TryGetCompressionStream(Stream uncompressedStream, string encoding, CompressionMode mode, out Stream compressedStream);

	}

	internal class DefaultStreamCompressor : IStreamCompressor
	{

		/// <summary>
		/// Decompresses the input stream to the output stream.
		/// </summary>
		/// <param name="uncompressedStream">The uncompressed stream.</param>
		/// <param name="compressionType">Type of the compression.</param>
		/// <param name="mode">Specifies compression or decompression</param>
		/// <returns></returns>
		public bool TryGetCompressionStream(Stream uncompressedStream, string encoding, CompressionMode mode, out Stream compressedStream)
		{
			switch (encoding)
			{
				case "gzip":
					compressedStream = new GZipStream(uncompressedStream, mode, leaveOpen: false);
					return true;
				case "deflate":
					compressedStream = new DeflateStream(uncompressedStream, mode, leaveOpen: false);
					return true;
				default:
					compressedStream = uncompressedStream;
					return false;
			}
		}
	}
}
