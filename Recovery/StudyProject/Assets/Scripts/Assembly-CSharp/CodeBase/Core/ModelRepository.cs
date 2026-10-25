using System.Collections.Generic;
using Multicast.UserData;
using UniMob;

namespace Codebase.Core
{
	public abstract class ModelRepository<TModel> : Model where TModel : Model
	{
		private readonly MutableAtom<List<TModel>> list;
		private readonly MutableAtom<int> version;
		protected List<TModel> values
		{
			get
			{
				version.Get();
				return list.Value;
			}
		}

		protected MutableAtom<int> Version
		{
			get
			{
				return version;
			}
		}

		protected ModelRepository(Lifetime lifetime) : base(lifetime)
		{
			list = Atom.Value(Lifetime, new List<TModel>());
			version = Atom.Value(Lifetime, 0);
		}

		protected void Add(TModel model)
		{
			if (model == null)
				throw new System.ArgumentNullException(nameof(model));
			list.Value.Add(model);
			version.Invalidate();
		}

		protected bool Remove(TModel model)
		{
			if (model == null || !list.Value.Remove(model))
				return false;
			version.Invalidate();
			return true;
		}

		protected void Clear(bool disposeModels = true)
		{
			if (list.Value.Count == 0)
				return;
			if (disposeModels)
				foreach (TModel model in list.Value)
					model.Dispose();
			list.Value.Clear();
			version.Invalidate();
		}

		public override void Dispose()
		{
			foreach (TModel model in list.Value)
				model.Dispose();
			list.Value.Clear();
		}
	}

	public abstract class ModelRepository<TModel, TData> : Model<TData> where TModel : Model where TData : UdObject
	{
		private readonly MutableAtom<List<TModel>> list;
		private readonly MutableAtom<int> version;
		protected List<TModel> values
		{
			get
			{
				version.Get();
				return list.Value;
			}
		}

		protected MutableAtom<int> Version
		{
			get
			{
				return version;
			}
		}

		protected ModelRepository(Lifetime lifetime) : base(lifetime)
		{
			list = Atom.Value(Lifetime, new List<TModel>());
			version = Atom.Value(Lifetime, 0);
		}

		protected void Add(TModel model)
		{
			if (model == null)
				throw new System.ArgumentNullException(nameof(model));
			list.Value.Add(model);
			version.Invalidate();
		}

		protected bool Remove(TModel model)
		{
			if (model == null || !list.Value.Remove(model))
				return false;
			version.Invalidate();
			return true;
		}

		protected void Clear(bool disposeModels = true)
		{
			if (list.Value.Count == 0)
				return;
			if (disposeModels)
				foreach (TModel model in list.Value)
					model.Dispose();
			list.Value.Clear();
			version.Invalidate();
		}

		public override void Dispose()
		{
			foreach (TModel model in list.Value)
				model.Dispose();
			list.Value.Clear();
		}
	}
}
