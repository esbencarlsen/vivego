// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public sealed class AmazonS3Options
	{
		public string AccessKey { get; set; } = string.Empty;
		public string SecretKey { get; set; } = string.Empty;
		public string Endpoint { get; set; } = "USEast1";
		public string BucketName { get; set; } = string.Empty;
		public string RegionEndpoint { get; set; } = string.Empty;
	}
}
