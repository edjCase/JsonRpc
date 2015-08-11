using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router
{
	public struct RpcPath : IEquatable<RpcPath>
	{
		public static RpcPath Default => new RpcPath();

		private string[] components { get; }

		private RpcPath(string path)
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				this.components = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			}
			else
			{
				this.components = new string[0];
			}
		}

		private RpcPath(string[] components)
		{
			if (components == null)
			{
				throw new ArgumentNullException(nameof(components));
			}
			this.components = components;
		}

		public static bool operator ==(RpcPath path1, RpcPath path2)
		{
			return path1.Equals(path2);
		}

		public static bool operator !=(RpcPath path1, RpcPath path2)
		{
			return !path1.Equals(path2);
		}

		public bool Equals(RpcPath other)
		{
			if (other.components == null)
			{
				return this.components == null;
			}
			if (other.components.Count() != this.components.Count())
			{
				return false;
			}
			for (int i = 0; i < this.components.Length; i++)
			{
				string component = this.components[i];
				string otherComponent = other.components[i];
				if (!string.Equals(component, otherComponent, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			return true;
		}
		

		public override bool Equals(object obj)
		{
			if (obj is RpcPath)
			{
				return this.Equals((RpcPath) obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			int hash = 1337;
			if (this.components == null)
			{
				return 0;
			}
			foreach (string component in this.components)
			{
				hash = (hash * 7) + component.GetHashCode();
			}
			return hash;
		}

		public static RpcPath Parse(string path)
		{
			return new RpcPath(path);
		}

		public RpcPath Add(RpcPath other)
		{
			int componentCount = this.components.Length + other.components.Length;
			string[] newComponents = new string[componentCount];
			this.components.CopyTo(newComponents, 0);
			other.components.CopyTo(newComponents, this.components.Length);
			return new RpcPath(newComponents);
		}
	}
}
