using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace vivego.core
{
	public sealed class ParsedAssemblyQualifiedName
	{
		public string AssemblyDescriptionString { get; }
		public string Culture { get; }
		public List<ParsedAssemblyQualifiedName> GenericParameters  { get; } = new();
		public string InternalKeyToken { get; }
		public string ShortAssemblyName { get; } = string.Empty;
		public string TypeName { get; }
		public string Version { get; }
		public Lazy<AssemblyName> AssemblyNameDescriptor { get; }
		public Lazy<Type?> FoundType { get; }

		public ParsedAssemblyQualifiedName(string assemblyQualifiedName)
		{
			if (string.IsNullOrEmpty(assemblyQualifiedName)) throw new ArgumentException("Value cannot be null or empty.", nameof(assemblyQualifiedName));

			int index = -1;
			Block rootBlock = new(this, default!);
			int bcount = 0;
			Block currentBlock = rootBlock;
			for (int i = 0; i < assemblyQualifiedName.Length; ++i)
			{
				char c = assemblyQualifiedName[i];
				if (c == '[')
				{
					if (assemblyQualifiedName[i + 1] == ']') // Array type.
					{
						i++;
					}
					else
					{
						++bcount;
						Block b = new(this, currentBlock)
						{
							Start = i + 1
						};
						currentBlock = b;
					}
				}
				else if (c == ']')
				{
					if (assemblyQualifiedName[currentBlock.Start] != '[')
					{
						currentBlock.ParsedAssemblyQualifiedName = new ParsedAssemblyQualifiedName(
							assemblyQualifiedName.Substring(currentBlock.Start, i - currentBlock.Start));
						if (bcount == 2)
						{
							GenericParameters.Add(currentBlock.ParsedAssemblyQualifiedName);
						}
					}

					currentBlock = currentBlock.ParentBlock;
					--bcount;
				}
				else if (bcount == 0 && c == ',')
				{
					index = i;
					break;
				}
			}

			TypeName = index > 0 ? assemblyQualifiedName.Substring(0, index) : assemblyQualifiedName;
			AssemblyDescriptionString = assemblyQualifiedName.Substring(index + 2);
			List<string> parts = AssemblyDescriptionString.Split(',')
				.Select(x => x.Trim())
				.ToList();
			Version = LookForPairThenRemove(parts, "Version");
			Culture = LookForPairThenRemove(parts, "Culture");
			InternalKeyToken = LookForPairThenRemove(parts, "internalKeyToken");
			if (parts.Count > 0)
			{
				ShortAssemblyName = parts[0];
			}

			AssemblyNameDescriptor = new Lazy<AssemblyName>(() => new AssemblyName(AssemblyDescriptionString));
			FoundType = new Lazy<Type?>(() => TypeFromAssemblyQualifiedName(assemblyQualifiedName));
		}

		private static Type? TypeFromAssemblyQualifiedName(string assemblyQualifiedName)
		{
			Type? searchedType = Type.GetType(assemblyQualifiedName, false);
			if (searchedType is not null)
			{
				return searchedType;
			}

			foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies())
			{
				searchedType = assem.GetType(assemblyQualifiedName);
				if (searchedType is not null)
				{
					return searchedType;
				}
			}

			return default; // Not found.
		}

		private string LanguageStyle(string prefix, string suffix)
		{
			if (GenericParameters.Count > 0)
			{
				StringBuilder sb = new(TypeName.Substring(0, TypeName.IndexOf('`', StringComparison.Ordinal)));
				sb.Append(prefix);
				bool pendingElement = false;
				foreach (ParsedAssemblyQualifiedName param in GenericParameters)
				{
					if (pendingElement)
					{
						sb.Append(", ");
					}

					sb.Append(param.LanguageStyle(prefix, suffix));
					pendingElement = true;
				}

				sb.Append(suffix);
				return sb.ToString();
			}

			return TypeName;
		}

		private static string LookForPairThenRemove(IList<string> strings, string name)
		{
			if (strings is null) throw new ArgumentNullException(nameof(strings));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			for (int istr = 0; istr < strings.Count; istr++)
			{
				string s = strings[istr];
				int i = s.IndexOf(name, StringComparison.Ordinal);
				if (i == 0)
				{
					int i2 = s.IndexOf('=', StringComparison.Ordinal);
					if (i2 > 0)
					{
						string ret = s.Substring(i2 + 1);
						strings.RemoveAt(istr);
						return ret;
					}
				}
			}

			return string.Empty;
		}

		public override string ToString() => LanguageStyle("<", ">");

		private class Block
		{
			public Block(ParsedAssemblyQualifiedName parsedAssemblyQualifiedName,
				Block parentBlock)
			{
				ParsedAssemblyQualifiedName = parsedAssemblyQualifiedName;
				ParentBlock = parentBlock;
			}

			public int Start { get; set; }
			public Block ParentBlock { get; }
			public ParsedAssemblyQualifiedName ParsedAssemblyQualifiedName { get; set; }
		}
	}
}
