using System;

using Orleans;
using Orleans.Runtime;

namespace vivego.Orleans.KeyValueProvider.Storage
{
	public sealed class KeyValueStoreGrainStorageOptionsValidator : IConfigurationValidator
	{
		private readonly KeyValueStoreStorageOptions _options;
		private readonly string _name;

		/// <summary>Constructor</summary>
		/// <param name="options">The option to be validated.</param>
		/// <param name="name">The option name to be validated.</param>
		public KeyValueStoreGrainStorageOptionsValidator(KeyValueStoreStorageOptions options, string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_name = name;
		}

		public void ValidateConfiguration()
		{
			if (_options.Ttl.HasValue && _options.Ttl.Value < TimeSpan.FromSeconds(1))
			{
				throw new OrleansConfigurationException(
					$"Configuration for {nameof(KeyValueStoreStorageOptions)} {_name} is invalid. {nameof(_options.Ttl)} must be >= 1 second.");
			}
		}
	}
}
