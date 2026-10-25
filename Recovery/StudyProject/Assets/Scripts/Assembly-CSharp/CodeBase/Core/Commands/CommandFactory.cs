using System;
using System.Collections.Generic;

namespace Codebase.Core.Commands
{
    public class CommandFactory
    {
        private readonly Dictionary<Type, Type> commandHandlersDict = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, object> commandHandlersCache = new Dictionary<Type, object>();

        public CommandFactory()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.FullName;
                if (name.StartsWith("Unity") || name.StartsWith("System") ||
                    name.StartsWith("mscorlib") || name.StartsWith("netstandard") || name.StartsWith("Mono."))
                    continue;
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                        continue;
                    foreach (var implemented in type.GetInterfaces())
                    {
                        if (!implemented.IsGenericType)
                            continue;
                        var definition = implemented.GetGenericTypeDefinition();
                        if (definition != typeof(ICommandHandler<>) && definition != typeof(ICommandHandler<,>))
                            continue;
                        var argument = implemented.GetGenericArguments()[0];
                        if (typeof(ICommand).IsAssignableFrom(argument) && !commandHandlersDict.ContainsKey(argument))
                            commandHandlersDict.Add(argument, type);
                    }
                }
            }
        }

        public CommandHandlerInfo<TCommand> Create<TCommand>() where TCommand : struct, ICommand
        {
            var handlerType = commandHandlersDict[typeof(TCommand)];
            if (!commandHandlersCache.TryGetValue(handlerType, out var handler))
            {
                handler = Activator.CreateInstance(handlerType);
                commandHandlersCache[handlerType] = handler;
            }
            Injector.InjectInto(handler);
            return new CommandHandlerInfo<TCommand>((ICommandHandler<TCommand>)handler, typeof(TCommand).Name);
        }

        public CommandHandlerInfo<TCommand, TResult> Create<TCommand, TResult>() where TCommand : struct, ICommand<TResult>
        {
            var handlerType = commandHandlersDict[typeof(TCommand)];
            if (!commandHandlersCache.TryGetValue(handlerType, out var handler))
            {
                handler = Activator.CreateInstance(handlerType);
                commandHandlersCache[handlerType] = handler;
            }
            Injector.InjectInto(handler);
            return new CommandHandlerInfo<TCommand, TResult>((ICommandHandler<TCommand, TResult>)handler, typeof(TCommand).Name);
        }
    }
}
