using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue;

public interface IKeyValueStoreRequestHandler :
	IRequestHandler<SetRequest, string>,
	IRequestHandler<GetRequest, KeyValueEntry>,
	IRequestHandler<DeleteRequest, bool>,
	IRequestHandler<FeaturesRequest, KeyValueStoreFeatures>,
	IRequestHandler<ClearRequest>
{
}
