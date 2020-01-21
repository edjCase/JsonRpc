
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Common.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Client
{
	public interface IHttpAuthHeaderFactory
	{
		Task<AuthenticationHeaderValue?> CreateAuthHeader();
	}

	public class DefaultHttpAuthHeaderFactory : IHttpAuthHeaderFactory
	{
		private Func<Task<AuthenticationHeaderValue?>> authHeaderFunc { get; }
		public DefaultHttpAuthHeaderFactory(Func<Task<AuthenticationHeaderValue?>>? authHeaderFunc = null)
		{
			this.authHeaderFunc = authHeaderFunc ?? (() => Task.FromResult<AuthenticationHeaderValue?>(null));
		}
		public DefaultHttpAuthHeaderFactory(AuthenticationHeaderValue? authHeader)
		{
			this.authHeaderFunc = () => Task.FromResult(authHeader);
		}

		public Task<AuthenticationHeaderValue?> CreateAuthHeader()
		{
			return this.authHeaderFunc();
		}
	}
}