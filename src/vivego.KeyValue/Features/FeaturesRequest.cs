using MediatR;

namespace vivego.KeyValue.Features;

public readonly record struct FeaturesRequest : IRequest<KeyValueStoreFeatures>;
