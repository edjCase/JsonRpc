using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace JsonRpc.Router.Abstractions
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
