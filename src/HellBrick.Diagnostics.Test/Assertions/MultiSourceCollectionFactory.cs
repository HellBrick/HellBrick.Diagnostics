namespace HellBrick.Diagnostics.Assertions
{
	public readonly struct MultiSourceCollectionFactory : ISourceCollectionFactory<string[]>
	{
		public string[] CreateCollection( string[] sources ) => sources;
	}
}
