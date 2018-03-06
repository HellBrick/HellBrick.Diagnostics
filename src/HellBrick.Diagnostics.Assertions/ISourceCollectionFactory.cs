namespace HellBrick.Diagnostics.Assertions
{
	public interface ISourceCollectionFactory<TSource>
	{
		string[] CreateCollection( TSource sources );
	}
}
