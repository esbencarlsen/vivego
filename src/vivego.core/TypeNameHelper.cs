using System;
using System.Collections.Concurrent;

namespace vivego.core
{
	public sealed class TypeNameHelper
	{
		private readonly ConcurrentDictionary<Type, string> _cache = new();
		private readonly ConcurrentDictionary<string, Type> _typeNameCache = new(StringComparer.Ordinal);

		public string GetTypeName<T>()
		{
			return GetTypeName(typeof(T));
		}

		public string GetTypeName(Type type)
		{
			if (type is null) throw new ArgumentNullException(nameof(type));
			if (!_cache.TryGetValue(type, out var typeName))
			{
				typeName = new ParsedAssemblyQualifiedName(type.FullName!).TypeName;
				_cache[type] = typeName;
			}

			return typeName;
		}

		public Type? GetTypeFromName(string typeName)
		{
			if (string.IsNullOrEmpty(typeName)) throw new ArgumentException("Value cannot be null or empty.", nameof(typeName));

			if (!_typeNameCache.TryGetValue(typeName, out var type))
			{
				Lazy<Type?> lazyType = new ParsedAssemblyQualifiedName(typeName).FoundType;
				if (lazyType.Value is not null)
				{
					type = lazyType.Value;
					_typeNameCache[typeName] = type;
				}
			}

			return type;
		}
	}
}
