using Microsoft.Extensions.DependencyInjection;

using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.Client.Http;

ServiceCollection collection = new();
collection.AddHttpMessageBroker();
ServiceProvider sp = collection.BuildServiceProvider();

IMessageBroker httpMessageBroker = sp.GetRequiredService<IMessageBroker>();
await httpMessageBroker
	.Subscribe("AA", SubscriptionType.Glob, "*")
	.ConfigureAwait(false);

await foreach (MessageBrokerEvent brokerEvent in httpMessageBroker
	.StreamingGet("AA", 0)
	.ConfigureAwait(false))
{
	Console.Out.WriteLine(brokerEvent);
}
