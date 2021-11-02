using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue;

public sealed class DefaultKeyValueStore : IKeyValueStore
{
	private readonly IMediator _mediator;

	public DefaultKeyValueStore(string name,
		IMediator mediator)
	{
		if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
		Name = name;
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
	}

	public string Name { get; }

	public async ValueTask<KeyValueStoreFeatures> GetFeatures(CancellationToken cancellationToken = default)
	{
		return await _mediator.Send(new FeaturesRequest(), cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<string> Set(SetKeyValueEntry setKeyValueEntry, CancellationToken cancellationToken = default)
	{
		string result = await _mediator
			.Send(new SetRequest(setKeyValueEntry), cancellationToken)
			.ConfigureAwait(false);
		await _mediator
			.Publish(new SetKeyValueEntryNotification(setKeyValueEntry), cancellationToken)
			.ConfigureAwait(false);
		return result;
	}

	public async ValueTask<KeyValueEntry> Get(string key, CancellationToken cancellationToken = default)
	{
		return await _mediator.Send(new GetRequest(key), cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<bool> Delete(DeleteKeyValueEntry deleteKeyValueEntry, CancellationToken cancellationToken = default)
	{
		return await _mediator.Send(new DeleteRequest(deleteKeyValueEntry), cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask Clear(CancellationToken cancellationToken = default) => await _mediator.Send(new ClearRequest(), cancellationToken).ConfigureAwait(false);
}
