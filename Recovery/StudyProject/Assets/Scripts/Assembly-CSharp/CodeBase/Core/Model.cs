using Multicast.UserData;
using UniMob;

namespace Codebase.Core
{
	public class Model : ILifetimeScope
	{
		private readonly Lifetime lifetime;

		public Lifetime Lifetime => this.lifetime;

		public Model(Lifetime lifetime)
		{
			this.lifetime = lifetime;
		}

		public virtual void Initialize()
		{
		}

		public virtual void Dispose()
		{
		}
	}
	public class Model<TData> : Model where TData : UdObject
	{
		public TData Data { get; private set; }

		public Model(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public override void Initialize()
		{
			base.Initialize();
			Data = UserData.UserDataProvider.Resolve<TData>();
		}

		public override void Dispose()
		{
		}
	}
}
