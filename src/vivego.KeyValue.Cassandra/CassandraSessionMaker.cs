using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Cassandra;

using Microsoft.Extensions.Logging;

using Polly;

#pragma warning disable CA1308
namespace vivego.KeyValue.Cassandra;

internal sealed class CassandraSessionMaker
{
	public static CassandraSessionMaker Instance { get; } = new();

	private ConcurrentDictionary<string, Task<ISession>> Sessions { get; } = new(StringComparer.Ordinal);

	private CassandraSessionMaker()
	{
	}

	public Task<ISession> MakeSession(
		string connectionString,
		ILogger logger,
		Action<Builder>? action = default)
	{
		if (string.IsNullOrEmpty(connectionString))
		{
			throw new ArgumentException("Value cannot be null or empty.", nameof(connectionString));
		}

		if (!Sessions.TryGetValue(connectionString, out Task<ISession>? sessionTask))
		{
			sessionTask = Policy
				.Handle<Exception>(ex => ex is not OperationCanceledException)
				.WaitAndRetryAsync(int.MaxValue,
					_ => TimeSpan.FromSeconds(1),
					(exception, waitPeriod) =>
					{
						logger.LogError(exception, "Error while trying to make connection to Cassandra with connection string: {ConnectionString}; Waiting for {WaitPeriod} before trying again",
							connectionString,
							waitPeriod);
					})
				.ExecuteAsync(() =>
				{
					Builder clusterBuilder = Cluster.Builder()
						.WithPoolingOptions(PoolingOptions.Create()
							.SetCoreConnectionsPerHost(HostDistance.Local, 2)
							.SetCoreConnectionsPerHost(HostDistance.Remote, 2)
							.SetMaxConnectionsPerHost(HostDistance.Local, 20)
							.SetMaxConnectionsPerHost(HostDistance.Remote, 20)
							.SetMaxRequestsPerConnection(int.MaxValue))
						.WithConnectionString(connectionString)
						.WithQueryOptions(new QueryOptions()
							.SetConsistencyLevel(ConsistencyLevel.LocalQuorum));

					string[] options = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
					foreach (string option in options)
					{
						string[] parts = option.Split('=');
						if (parts.Length != 2)
						{
							throw new FormatException($"Invalid connection string option: {option}");
						}

						string key = parts[0].Trim().ToLowerInvariant();
						string value = parts[1].Trim();

						switch (key)
						{
							case "compression":
								if (Enum.TryParse(value, true, out CompressionType compressionType))
								{
									clusterBuilder.WithCompression(compressionType);
								}
								else
								{
									throw new FormatException($"Unknown compression type {value}");
								}

								break;

							case "ssl":
								if (bool.TryParse(value, out bool booleanValue)
									&& booleanValue)
								{
									clusterBuilder.WithSSL();
								}

								break;
						}
					}

					if (connectionString.IsAmazonKeyspacesConnection())
					{
						X509Certificate2Collection certCollection = new();
						X509Certificate2 amazoncert = new(Encoding.ASCII.GetBytes(AmazonKeyspacesCertificate));

						CassandraConnectionStringBuilder cassandraConnectionStringBuilder = new(connectionString);
						string userName = cassandraConnectionStringBuilder.Username;
						string pwd = cassandraConnectionStringBuilder.Password;
						certCollection.Add(amazoncert);
						clusterBuilder
							.WithAuthProvider(new PlainTextAuthProvider(userName, pwd))
							.WithSSL(new SSLOptions().SetCertificateCollection(certCollection));

						cassandraConnectionStringBuilder.Remove(nameof(cassandraConnectionStringBuilder.Username));
						cassandraConnectionStringBuilder.Remove(nameof(cassandraConnectionStringBuilder.Password));
						clusterBuilder.WithConnectionString(cassandraConnectionStringBuilder.ToString());
					}

					action?.Invoke(clusterBuilder);
					Cluster cluster = clusterBuilder.Build();
					return cluster.ConnectAsync();
				});
			Sessions.TryAdd(connectionString, sessionTask);
		}

		return sessionTask;
	}

	private const string AmazonKeyspacesCertificate = @"-----BEGIN CERTIFICATE-----
MIIEDzCCAvegAwIBAgIBADANBgkqhkiG9w0BAQUFADBoMQswCQYDVQQGEwJVUzEl
MCMGA1UEChMcU3RhcmZpZWxkIFRlY2hub2xvZ2llcywgSW5jLjEyMDAGA1UECxMp
U3RhcmZpZWxkIENsYXNzIDIgQ2VydGlmaWNhdGlvbiBBdXRob3JpdHkwHhcNMDQw
NjI5MTczOTE2WhcNMzQwNjI5MTczOTE2WjBoMQswCQYDVQQGEwJVUzElMCMGA1UE
ChMcU3RhcmZpZWxkIFRlY2hub2xvZ2llcywgSW5jLjEyMDAGA1UECxMpU3RhcmZp
ZWxkIENsYXNzIDIgQ2VydGlmaWNhdGlvbiBBdXRob3JpdHkwggEgMA0GCSqGSIb3
DQEBAQUAA4IBDQAwggEIAoIBAQC3Msj+6XGmBIWtDBFk385N78gDGIc/oav7PKaf
8MOh2tTYbitTkPskpD6E8J7oX+zlJ0T1KKY/e97gKvDIr1MvnsoFAZMej2YcOadN
+lq2cwQlZut3f+dZxkqZJRRU6ybH838Z1TBwj6+wRir/resp7defqgSHo9T5iaU0
X9tDkYI22WY8sbi5gv2cOj4QyDvvBmVmepsZGD3/cVE8MC5fvj13c7JdBmzDI1aa
K4UmkhynArPkPw2vCHmCuDY96pzTNbO8acr1zJ3o/WSNF4Azbl5KXZnJHoe0nRrA
1W4TNSNe35tfPe/W93bC6j67eA0cQmdrBNj41tpvi/JEoAGrAgEDo4HFMIHCMB0G
A1UdDgQWBBS/X7fRzt0fhvRbVazc1xDCDqmI5zCBkgYDVR0jBIGKMIGHgBS/X7fR
zt0fhvRbVazc1xDCDqmI56FspGowaDELMAkGA1UEBhMCVVMxJTAjBgNVBAoTHFN0
YXJmaWVsZCBUZWNobm9sb2dpZXMsIEluYy4xMjAwBgNVBAsTKVN0YXJmaWVsZCBD
bGFzcyAyIENlcnRpZmljYXRpb24gQXV0aG9yaXR5ggEAMAwGA1UdEwQFMAMBAf8w
DQYJKoZIhvcNAQEFBQADggEBAAWdP4id0ckaVaGsafPzWdqbAYcaT1epoXkJKtv3
L7IezMdeatiDh6GX70k1PncGQVhiv45YuApnP+yz3SFmH8lU+nLMPUxA2IGvd56D
eruix/U0F47ZEUD0/CwqTRV/p2JdLiXTAAsgGh1o+Re49L2L7ShZ3U0WixeDyLJl
xy16paq8U4Zt3VekyvggQQto8PT7dL5WXXp59fkdheMtlb71cZBDzI0fmgAKhynp
VSJYACPq4xJDKVtHCN2MQWplBqjlIapBtJUhlbl90TSrE9atvNziPTnNvT51cKEY
WQPJIrSPnNVeKtelttQKbfi3QBFGmh95DmK/D5fs4C8fF5Q=
-----END CERTIFICATE-----
";
}
