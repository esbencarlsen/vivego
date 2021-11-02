namespace vivego.KeyValue;

public sealed class KeyValueStoreFeatures
{
	public int MaximumKeyLength { get; set; } = 1024;
	public long MaximumDataSize { get; set; } = 1024 * 1024;
	public bool SupportsTtl { get; set; }
	public bool SupportsEtag { get; set; }
}
