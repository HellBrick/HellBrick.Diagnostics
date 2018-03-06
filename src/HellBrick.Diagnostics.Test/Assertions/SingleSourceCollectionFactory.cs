namespace HellBrick.Diagnostics.Assertions
{
	public readonly struct SingleSourceCollectionFactory : ISourceCollectionFactory<string>
	{
		public string[] CreateCollection( string sources ) => new[] { sources };
	}
}
