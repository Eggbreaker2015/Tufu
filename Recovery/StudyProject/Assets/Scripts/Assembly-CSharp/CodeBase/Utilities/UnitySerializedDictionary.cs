using System.Collections.Generic;
using UnityEngine;

namespace CodeBase.Utilities
{
	public abstract class UnitySerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField]
		private List<TKey> keyData = new List<TKey>();

		[SerializeField]
		private List<TValue> valueData = new List<TValue>();

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();
			for (int i = 0; i < keyData.Count && i < valueData.Count; i++) this[keyData[i]] = valueData[i];
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			keyData.Clear();
			valueData.Clear();
			foreach (var pair in this)
			{
				keyData.Add(pair.Key);
				valueData.Add(pair.Value);
			}
		}
	}
}
