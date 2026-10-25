namespace Codebase.Core.Commands
{
	public readonly struct CommandHandlerInfo<TCommand> where TCommand : struct, ICommand
	{
		public readonly ICommandHandler<TCommand> Handler;

		public readonly string Name;

		public CommandHandlerInfo(ICommandHandler<TCommand> handler, string name)
		{
			Handler = handler;
			Name = name;
		}
	}
	public readonly struct CommandHandlerInfo<TCommand, TResult> where TCommand : struct, ICommand<TResult>
	{
		public readonly ICommandHandler<TCommand, TResult> Handler;

		public readonly string Name;

		public CommandHandlerInfo(ICommandHandler<TCommand, TResult> handler, string name)
		{
			Handler = handler;
			Name = name;
		}
	}
}
