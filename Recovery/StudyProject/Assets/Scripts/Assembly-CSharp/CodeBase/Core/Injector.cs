using System;
using System.Collections.Generic;
using System.Reflection;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Core
{
	public static class Injector
	{
		private struct InjectionInfo
		{
			public FieldInfo Field;

			public Type DependencyType;

			public bool IsStash;

			public MethodInfo ResolverMethod;
		}

		private static readonly Dictionary<Type, InjectionInfo[]> TypeCache = new Dictionary<Type, InjectionInfo[]>(256);

		private static readonly MethodInfo GameContextGetMethod = typeof(GameContext).GetMethod("Get");

		private static readonly Type InjectAttributeType = typeof(InjectAttribute);

		private static readonly Type StashGenericType = typeof(Stash<>);

		public static void InjectScene()
		{
			MonoBehaviour[] targets = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			Dictionary<Type, object> stashCache = new Dictionary<Type, object>(64);
			Dictionary<Type, object> serviceCache = new Dictionary<Type, object>(64);
			foreach (MonoBehaviour target in targets)
			{
				InjectInto(target, stashCache, serviceCache);
			}
		}

		private static void InjectInto(object target, Dictionary<Type, object> stashCache, Dictionary<Type, object> serviceCache)
		{
			if (target == null)
			{
				return;
			}
			Type targetType = target.GetType();
			if (!TypeCache.TryGetValue(targetType, out InjectionInfo[] injectionInfos))
			{
				injectionInfos = BuildInjectionInfo(targetType);
				TypeCache[targetType] = injectionInfos;
			}
			foreach (InjectionInfo info in injectionInfos)
			{
				object dependency;
				if (info.IsStash)
				{
					if (!stashCache.TryGetValue(info.DependencyType, out dependency))
					{
						dependency = GetStash(info.DependencyType);
						if (dependency != null)
						{
							GameContext.Set(dependency);
							stashCache[info.DependencyType] = dependency;
						}
					}
				}
				else if (!serviceCache.TryGetValue(info.DependencyType, out dependency) && info.ResolverMethod != null)
				{
					dependency = info.ResolverMethod.Invoke(null, null);
					serviceCache[info.DependencyType] = dependency;
				}
				if (dependency == null)
				{
					Debug.LogError("[DI] Can't resolve " + info.DependencyType.Name + " for " + targetType.Name);
					continue;
				}
				info.Field.SetValue(target, dependency);
			}
		}

		private static InjectionInfo[] BuildInjectionInfo(Type type)
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			List<InjectionInfo> infos = new List<InjectionInfo>();
			foreach (FieldInfo field in fields)
			{
				if (!field.IsDefined(InjectAttributeType, false))
				{
					continue;
				}
				Type dependencyType = field.FieldType;
				bool isStash = dependencyType.IsGenericType && dependencyType.GetGenericTypeDefinition() == StashGenericType;
				infos.Add(new InjectionInfo
				{
					Field = field,
					DependencyType = dependencyType,
					IsStash = isStash,
					ResolverMethod = isStash ? null : GameContextGetMethod.MakeGenericMethod(dependencyType)
				});
			}
			return infos.Count == 0 ? Array.Empty<InjectionInfo>() : infos.ToArray();
		}

		private static object GetStash(Type type)
		{
			if (!type.IsGenericType || type.GetGenericTypeDefinition() != StashGenericType)
			{
				return null;
			}
			return World.Default.GetReflectionStash(type.GetGenericArguments()[0]);
		}

		public static void InjectInto(object target)
		{
			InjectInto(target, new Dictionary<Type, object>(), new Dictionary<Type, object>());
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			TypeCache.Clear();
		}
	}
}
