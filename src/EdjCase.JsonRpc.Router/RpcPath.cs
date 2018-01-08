using EdjCase.JsonRpc.Core;
using System;
using System.Linq;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Represents the url path for Rpc routing purposes
	/// </summary>
	public struct RpcPath : IEquatable<RpcPath>
	{
		/// <summary>
		/// Default/Empty path
		/// </summary>
		public static RpcPath Default => new RpcPath();

		/// <summary>
		/// Path components split on forward slashes
		/// </summary>
		private readonly string[] componentsValue;

		private string[] components
		{
			get
			{
				if (this.componentsValue == null)
				{
					return new string[0];
				}
				return this.componentsValue;
			}
		}



		/// <param name="path">Url/route path</param>
		private RpcPath(string path)
		{
			this.componentsValue = !string.IsNullOrWhiteSpace(path)
				? path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				: new string[0];
		}

		/// <param name="components">Uri components for the path</param>
		private RpcPath(params string[] components)
		{
			this.componentsValue = components ?? throw new ArgumentNullException(nameof(components));
		}

		public static bool operator ==(RpcPath path1, RpcPath path2)
		{
			return path1.Equals(path2);
		}

		public static bool operator !=(RpcPath path1, RpcPath path2)
		{
			return !path1.Equals(path2);
		}

		public bool StartsWith(RpcPath other)
		{
			if (other.components.Count() > this.components.Count())
			{
				return false;
			}
			return this.StartsWithInternal(other);
		}

		public bool Equals(RpcPath other)
		{
			if (other.components.Count() != this.components.Count())
			{
				return false;
			}
			return this.StartsWithInternal(other);
		}

		private bool StartsWithInternal(RpcPath other)
		{
			for (int i = 0; i < other.components.Length; i++)
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
				return this.Equals((RpcPath)obj);
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

		/// <summary>
		/// Creates a <see cref="RpcPath"/> based on the string form of the path
		/// </summary>
		/// <param name="path">Uri/route path</param>
		/// <returns>Rpc path based on the path string</returns>
		public static RpcPath Parse(string path, string basePath = null)
		{
			if (!string.IsNullOrWhiteSpace(basePath))
			{
				if (!string.IsNullOrWhiteSpace(path))
				{
					return new RpcPath(basePath, path);
				}
				return new RpcPath(basePath);
			}
			return new RpcPath(path);
		}

		/// <summary>
		/// Removes the base path path from this path
		/// </summary>
		/// <param name="basePath">Base path to remove</param>
		/// <returns>A new path that is the full path without the base path</returns>
		public RpcPath RemoveBasePath(RpcPath basePath)
		{
			if(!this.TryRemoveBasePath(basePath, out RpcPath path))
			{
				throw new RpcParseException($"Count not remove path '{basePath}' from path '{this}'.");
			}
			return path;
		}

		/// <summary>
		/// Tries to remove the base path path from this path
		/// </summary>
		/// <param name="basePath">Base path to remove</param>
		/// <returns>True if removed the base path. Otherwise false</returns>
		public bool TryRemoveBasePath(RpcPath basePath, out RpcPath path)
		{
			if (basePath == default)
			{
				path = this;
				return true;
			}
			if (!this.StartsWith(basePath))
			{
				path = default;
				return false;
			}
			var newComponents = new string[this.components.Length - basePath.components.Length];
			if(newComponents.Length > 0)
			{
				Array.Copy(this.components, basePath.components.Length, newComponents, 0, 1);
			}
			path = new RpcPath(newComponents);
			return true;
		}

		/// <summary>
		/// Merges the two paths to create a new Rpc path that is the combination of the two
		/// </summary>
		/// <param name="other">Other path to add to the end of the current path</param>
		/// <returns>A new path that is the combination of the two paths</returns>
		public RpcPath Add(RpcPath other)
		{
			int componentCount = this.components.Length + other.components.Length;
			string[] newComponents = new string[componentCount];
			this.components.CopyTo(newComponents, 0);
			other.components.CopyTo(newComponents, this.components.Length);
			return new RpcPath(newComponents);
		}

		public override string ToString()
		{
			return "/" + string.Join("/", this.components);
		}

		public static implicit operator string(RpcPath path)
		{
			return path.ToString();
		}

		public static implicit operator RpcPath(string s)
		{
			return RpcPath.Parse(s);
		}
	}
}
