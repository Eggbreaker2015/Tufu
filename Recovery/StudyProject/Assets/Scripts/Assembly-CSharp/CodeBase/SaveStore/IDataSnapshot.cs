namespace Codebase.SaveStore
{
	public interface IDataSnapshot
	{
		string Name { get; }

		string CreatedAtIso { get; }

		string Details { get; }
	}
}
