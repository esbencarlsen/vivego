using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Behaviours;

public sealed class ETagKeyValueStoreBehavior :
	IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>,
	IPipelineBehavior<SetRequest, string>,
	IPipelineBehavior<DeleteRequest, bool>
{
	private readonly IKeyValueStore _keyValueStore;

	public ETagKeyValueStoreBehavior(IKeyValueStore keyValueStore)
	{
		_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
	}

	public async Task<KeyValueStoreFeatures> Handle(FeaturesRequest request,
		CancellationToken cancellationToken,
		RequestHandlerDelegate<KeyValueStoreFeatures> next)
	{
		if (next is null) throw new ArgumentNullException(nameof(next));
		KeyValueStoreFeatures result = await next().ConfigureAwait(false);
		result.SupportsEtag = true;
		return result;
	}

	public async Task<string> Handle(SetRequest setRequest, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
	{
		if (next is null) throw new ArgumentNullException(nameof(next));
		if (setRequest.Entry is null) throw new ArgumentNullException(nameof(setRequest.Entry));
		if (string.IsNullOrEmpty(setRequest.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(setRequest.Entry.Key));

		if (!string.IsNullOrEmpty(setRequest.Entry.ETag))
		{
			string currentEtag = await GetEtag(setRequest.Entry.Key, cancellationToken).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(currentEtag)
				&& !setRequest.Entry.ETag.Equals(currentEtag, StringComparison.Ordinal))
			{
				return string.Empty;
			}
		}

		string newEtag = await next().ConfigureAwait(false);

		// avoid recursive calls
		if (!IsETagKey(setRequest.Entry.Key))
		{
			await _keyValueStore
				.Set(new SetKeyValueEntry
				{
					Key = GetKeyETagKey(setRequest.Entry.Key),
					ETag = string.Empty,
					ExpiresInSeconds = setRequest.Entry.ExpiresInSeconds,
					Value = Encoding.ASCII.GetBytes(newEtag).ToNullableBytes()
				}, cancellationToken)
				.ConfigureAwait(false);
		}

		return newEtag;
	}

	public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<bool> next)
	{
		if (next is null) throw new ArgumentNullException(nameof(next));
		if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
		if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

		if (string.IsNullOrEmpty(request.Entry.ETag))
		{
			return await next().ConfigureAwait(false);
		}

		string currentEtag = await GetEtag(request.Entry.Key, cancellationToken).ConfigureAwait(false);
		if (currentEtag.Equals(request.Entry.ETag, StringComparison.Ordinal))
		{
			return await next().ConfigureAwait(false);
		}

		return false;
	}

	private async Task<string> GetEtag(string key, CancellationToken cancellationToken)
	{
		string eTagKey = GetKeyETagKey(key);
		KeyValueEntry etagKeyValue = await _keyValueStore
			.Get(eTagKey, cancellationToken)
			.ConfigureAwait(false);

		return etagKeyValue.Value.IsNull()
			? string.Empty
			: Encoding.ASCII.GetString(etagKeyValue.Value.Data.ToByteArray());
	}

	private static string GetKeyETagKey(string key)
	{
		return $"{key}_etag";
	}

	private static bool IsETagKey(string key)
	{
		return key.EndsWith("_etag", StringComparison.Ordinal);
	}
}
