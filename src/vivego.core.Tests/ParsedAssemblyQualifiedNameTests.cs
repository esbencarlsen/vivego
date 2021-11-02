using System;

using Xunit;

namespace vivego.core.Tests
{
	public sealed class ParsedAssemblyQualifiedNameTests
	{
		[Fact]
		public void SimpleType()
		{
			ParsedAssemblyQualifiedName parsedAssemblyQualifiedName =
				new(typeof(int).AssemblyQualifiedName ?? string.Empty);
			Assert.Equal(typeof(int), parsedAssemblyQualifiedName.FoundType.Value);
		}

		[Fact]
		public void GenericType()
		{
			Type type = typeof(GenericType<GenericType<string>>);
			ParsedAssemblyQualifiedName parsedAssemblyQualifiedName =
				new(type.AssemblyQualifiedName ?? string.Empty);
			Assert.Equal(type, parsedAssemblyQualifiedName.FoundType.Value);
		}

		[Fact]
		public void GenericTypeFromOldVersion()
		{
			Type type = typeof(GenericType<GenericType<string>>);
			ParsedAssemblyQualifiedName parsedAssemblyQualifiedName =
				new("vivego.core.Tests.GenericType`1[[vivego.core.Tests.GenericType`1[[System.String, System.Private.CoreLib, Version=4.0.0.1, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], vivego.core.Tests, Version=1.0.0.2, Culture=neutral, PublicKeyToken=null]], vivego.core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
			Assert.Equal(type, parsedAssemblyQualifiedName.FoundType.Value);
		}
	}

	public sealed class GenericType<T>
	{
		public T Value { get; }

		public GenericType(T value)
		{
			Value = value;
		}
	}
}
