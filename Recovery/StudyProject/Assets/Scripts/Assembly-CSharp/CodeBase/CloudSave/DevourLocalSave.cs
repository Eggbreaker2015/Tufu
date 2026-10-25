using Codebase.Core.UserData;
using Multicast.CloudSave;

namespace Codebase.CloudSave
{
	public sealed class DevourLocalSave : ILocalSave
	{
		private const string PRE_CLOUD_SAVE_FILE_NAME = "game_data_pre_cloud.bin";

		private readonly GameDataService gameDataService;

		public DevourLocalSave(GameDataService gameDataService)
		{
		}

		public LocalSaveReadStatus TryRead(out byte[] bytes)
		{
			bytes = null;
			return LocalSaveReadStatus.Missing;
		}

		public bool TryWrite(byte[] bytes)
		{
			return false;
		}

		public bool TryBackup(byte[] bytes)
		{
			return false;
		}
	}
}
