

using System;

namespace Unity.Tiny
{
	internal struct TinyId : IEquatable<TinyId>
	{
		private readonly Guid m_Guid;
		
		public static readonly TinyId Empty = new TinyId(Guid.Empty);
		
		public TinyId(Guid guid)
		{
			m_Guid = guid;
		}
        
		public TinyId(string guid)
		{
			m_Guid = string.IsNullOrEmpty(guid) ? Guid.Empty : new Guid(guid);
		}

		public static TinyId New()
		{
			return new TinyId(Guid.NewGuid());
		}

		public static TinyId Generate(string name)
		{
			using (var provider = System.Security.Cryptography.MD5.Create())
			{
				var hash = provider.ComputeHash(System.Text.Encoding.UTF8.GetBytes(name));
				var guid = new Guid(hash);
				return new TinyId(guid);
			}
		}

		public static bool operator ==(TinyId a, TinyId b)
		{
			return a.m_Guid.Equals(b.m_Guid);
		}

		public static bool operator !=(TinyId a, TinyId b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is TinyId))
			{
				return false;
			}

			var typeReference = (TinyId) obj;
			return (this == typeReference);
		}

		public override int GetHashCode()
		{
			return m_Guid.GetHashCode();
		}

		public Guid ToGuid()
		{
			return m_Guid;
		}

		public override string ToString()
		{
			return m_Guid.ToString("N");
		}

		public bool Equals(TinyId other)
		{
			return m_Guid.Equals(other.m_Guid);
		}
	}
}

