using System.Threading.Tasks;

using Orleans;

namespace vivego.Orleans.KeyValueProvider.Tests
{
	public interface ITestGrain : IGrainWithStringKey
	{
		Task Set(string value);
		Task Clear();
		Task<string> Get();
	}

	public sealed class TestGrain : Grain<TestGrainState>, ITestGrain
	{
		public Task Set(string value)
		{
			State = new TestGrainState
			{
				Value = value
			};
			return WriteStateAsync();
		}

		public Task Clear() => ClearStateAsync();

		public Task<string> Get() => Task.FromResult(State.Value);
	}

	public sealed class TestGrainState
	{
		public string Value { get; set; } = string.Empty;
	}
}
