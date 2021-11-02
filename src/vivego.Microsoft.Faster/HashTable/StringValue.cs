namespace vivego.Microsoft.Faster
{
	public sealed class StringValue
	{
		public string Value { get; set; }
		public StringValue(string first) => Value = first;

#pragma warning disable CA2225 // Operator overloads have named alternates
		public static implicit operator StringValue(string first)
#pragma warning restore CA2225 // Operator overloads have named alternates
		{
			return new(first);
		}

		public override string ToString() => $"{nameof(Value)}: {Value}";
	}
}
