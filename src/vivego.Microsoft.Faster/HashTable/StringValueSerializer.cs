using System;

using FASTER.core;

namespace vivego.Microsoft.Faster
{
	public sealed class StringValueSerializer : BinaryObjectSerializer<StringValue>
	{
		public override void Deserialize(out StringValue obj)
		{
			obj = new StringValue(reader.ReadString());
		}

		public override void Serialize(ref StringValue obj)
		{
			if (obj is null) throw new ArgumentNullException(nameof(obj));
			writer.Write(obj.Value);
		}
	}
}
