using System;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;
using vivego.MediatR;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddIsolatedKeyValueStoreBehaviour(this IServiceBuilder builder)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			builder.AddSingleThreadedPipelineBehaviour<SetRequest, string>(request => request.Entry.Key);
			builder.AddSingleThreadedPipelineBehaviour<GetRequest, KeyValueEntry>(request => request.Key);
			builder.AddSingleThreadedPipelineBehaviour<DeleteRequest, bool>(request => request.Entry.Key);

			return builder;
		}
	}
}