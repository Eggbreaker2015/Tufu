using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Codebase.Utilities
{
	public static class LinqExtensions
	{
		[PublicAPI]
		public static bool AllNonAlloc<T, TPayload>(this List<T> source, Func<T, TPayload, bool> predicate, TPayload payload)
		{
			return false;
		}

		[PublicAPI]
		public static T FirstOrDefaultNonAlloc<T, TPayload>(this List<T> source, Func<T, TPayload, bool> predicate, TPayload payload, T defaultValue = default(T))
		{
			foreach (T item in source)
                if (predicate(item, payload)) return item;
            return defaultValue;
		}
	}
}
