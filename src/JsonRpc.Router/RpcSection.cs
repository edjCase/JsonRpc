using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router
{
	public class RpcRoute
	{
		public string Name { get; }
		public List<Type> Types { get; } = new List<Type>();

		public RpcRoute(string name = null)
		{
			this.Name = name;
		}
	}

	public class RpcRouteCollection : ICollection<RpcRoute>
	{
		private List<RpcRoute> SectionList { get; } = new List<RpcRoute>();

		public RpcRouteCollection()
		{
		}

		public int Count => this.SectionList.Count;

		public bool IsReadOnly => false;
		
		public RpcRoute GetByName(string sectionName)
		{
			if (string.IsNullOrWhiteSpace(sectionName))
			{
				return this.SectionList.FirstOrDefault(s => string.IsNullOrWhiteSpace(s.Name));
			}
			return this.SectionList.FirstOrDefault(s => string.Equals(s.Name, sectionName, StringComparison.OrdinalIgnoreCase));
		}

		public void Add(RpcRoute section)
		{
			if(section == null)
			{
				throw new ArgumentNullException(nameof(section));
			}
			RpcRoute duplicateSection = this.GetByName(section.Name);
			if(duplicateSection != null)
			{
				throw new ArgumentException($"Section with the name '{section.Name}' already exists");
			}
			this.SectionList.Add(section);
		} 

		public void Clear()
		{
			this.SectionList.Clear();
		}

		public bool Contains(RpcRoute section)
		{
			return this.SectionList.Contains(section);
		}

		public void CopyTo(RpcRoute[] array, int arrayIndex)
		{
			this.SectionList.CopyTo(array, arrayIndex);
		}

		public bool Remove(RpcRoute section)
		{
			return this.SectionList.Remove(section);
		}

		public IEnumerator<RpcRoute> GetEnumerator()
		{
			return this.SectionList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.SectionList.GetEnumerator();
		}
	}
}
