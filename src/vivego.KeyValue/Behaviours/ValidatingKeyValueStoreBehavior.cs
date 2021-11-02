using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Behaviours;

public sealed class ValidatingKeyValueStoreBehavior :
	IPipelineBehavior<SetRequest, string>,
	IPipelineBehavior<GetRequest, KeyValueEntry>,
	IPipelineBehavior<DeleteRequest, bool>,
	IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>
{
	private readonly IKeyValueStore _keyValueStore;
	private KeyValueStoreFeatures? _features;

	public ValidatingKeyValueStoreBehavior(IKeyValueStore keyValueStore)
	{
		_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
	}

	public async Task<KeyValueStoreFeatures> Handle(FeaturesRequest request,
		CancellationToken cancellationToken,
		RequestHandlerDelegate<KeyValueStoreFeatures> next)
	{
		if (next is null) throw new ArgumentNullException(nameof(next));

		KeyValueStoreFeatures features = await next().ConfigureAwait(false);
		if (features.MaximumKeyLength <= 1)
		{
			throw new ArgumentException($"{nameof(features.MaximumKeyLength)} must be larger than 1, but was: {features.MaximumKeyLength}");
		}

		if (features.MaximumDataSize <= 1)
		{
			throw new ArgumentException($"{nameof(features.MaximumDataSize)} must be larger than 1, but was: {features.MaximumDataSize}");
		}

		return features;
	}

	public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
	{
		if (next is null) throw new ArgumentNullException(nameof(next));
		if (request.Entry.Value is null) throw new ArgumentNullException(nameof(request.Entry.Value));
		await ValidateKey(request.Entry.Key, cancellationToken).ConfigureAwait(false);
		switch (request.Entry.Value.KindCase)
		{
			case NullableBytes.KindOneofCase.None:
			case NullableBytes.KindOneofCase.Null:
				break;
			case NullableBytes.KindOneofCase.Data:
				_features ??= await _keyValueStore.GetFeatures(cancellationToken).ConfigureAwait(false);
				if (request.Entry.Value.Data is not null && request.Entry.Value.Data.Length > _features.MaximumDataSize)
				{
					string message = $"Data cannot be longer than {_features.MaximumDataSize} but was {request.Entry.Value.Data.Length}.";
					throw new ArgumentException(message, nameof(request.Entry.Value));
				}

				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		return await next().ConfigureAwait(false);
	}

	public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<KeyValueEntry> next)
	{
		if (next is null) throw new ArgumentNullException(nameof(next));
		await ValidateKey(request.Key, cancellationToken).ConfigureAwait(false);
		return await next().ConfigureAwait(false);
	}

	public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<bool> next)
	{
		if (next is null) throw new ArgumentNullException(nameof(next));
		await ValidateKey(request.Entry.Key, cancellationToken).ConfigureAwait(false);
		return await next().ConfigureAwait(false);
	}

	private async Task ValidateKey(string key, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));
		_features ??= await _keyValueStore.GetFeatures(cancellationToken).ConfigureAwait(false);
		if (key.Length > _features.MaximumKeyLength)
		{
			throw new ArgumentException($"Key cannot be longer than {_features.MaximumKeyLength} but was {key.Length}.", nameof(key));
		}
	}
}
