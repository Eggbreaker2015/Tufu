namespace Codebase.ABTests
{
	public interface IABTestDataGroup
	{
		string TestName { get; }

		void ApplyRemote(string json);

		void ValidateCohorts(string cohortsJson);

		void SetValuesUntyped(object dictionary);

		void Reset();
	}
}
