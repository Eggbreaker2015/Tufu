using UnityEngine;

namespace Codebase.Core.UserData.Generated
{
	public static class UserDataResolverInitializer
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			Multicast.UserData.UdRoot.RegisterResolver(UserDataResolver.Instance);
		}
	}
}
