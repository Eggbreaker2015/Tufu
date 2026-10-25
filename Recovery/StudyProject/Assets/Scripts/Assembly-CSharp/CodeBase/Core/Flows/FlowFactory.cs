using System;
using System.Collections.Generic;

namespace Codebase.Core.Flows
{
    public class FlowFactory
    {
        private readonly Dictionary<Type, Type> flowHandlersDict = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, IFlowHandlerBase> flowsCache = new Dictionary<Type, IFlowHandlerBase>();

        public FlowFactory()
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
                        if (definition != typeof(IFlowRunner<>) && definition != typeof(IFlowRunner<,>))
                            continue;
                        var argument = implemented.GetGenericArguments()[0];
                        if (typeof(IFlow).IsAssignableFrom(argument) && !flowHandlersDict.ContainsKey(argument))
                            flowHandlersDict.Add(argument, type);
                    }
                }
            }
        }

        public IFlowRunner<TFlow> Create<TFlow>() where TFlow : struct, IFlow
        {
            var handlerType = flowHandlersDict[typeof(TFlow)];
            if (!flowsCache.TryGetValue(handlerType, out var handler))
            {
                handler = (IFlowHandlerBase)Activator.CreateInstance(handlerType);
                flowsCache[handlerType] = handler;
            }
            Injector.InjectInto(handler);
            return (IFlowRunner<TFlow>)handler;
        }

        public IFlowRunner<TFlow, TResult> Create<TFlow, TResult>() where TFlow : struct, IFlow<TResult>
        {
            var handlerType = flowHandlersDict[typeof(TFlow)];
            if (!flowsCache.TryGetValue(handlerType, out var handler))
            {
                handler = (IFlowHandlerBase)Activator.CreateInstance(handlerType);
                flowsCache[handlerType] = handler;
            }
            Injector.InjectInto(handler);
            return (IFlowRunner<TFlow, TResult>)handler;
        }
    }
}
