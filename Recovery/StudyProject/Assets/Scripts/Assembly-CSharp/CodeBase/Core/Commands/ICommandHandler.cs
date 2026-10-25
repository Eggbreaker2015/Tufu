namespace Codebase.Core.Commands
{
	public interface ICommandHandler<in T> where T : struct, ICommand
	{
		void Execute(T command);
	}
	public interface ICommandHandler<in T, TResult> where T : struct, ICommand<TResult>
	{
		TResult Execute(T command);
	}
}
