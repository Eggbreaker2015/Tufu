using System;
using System.Collections.Generic;
using System.Globalization;
using UniMob;
using UnityEngine;

namespace Codebase.Core
{
	public class DataVar<T>
	{
		[Serializable]
		private class Wrapper<U>
		{
			public U Data;

			public Wrapper(U data)
			{
				Data = data;
			}
		}

		private readonly string key;

		private MutableAtom<T> value;

		public T Value
		{
			get
			{
				return value.Value;
			}
			set
			{
				this.value.Value = value;
				SaveValue();
			}
		}

		public DataVar(string key, T defaultValue = default(T))
		{
			this.key = key;
			value = Atom.Value(PlayerPrefs.HasKey(key) ? LoadValue(defaultValue) : defaultValue);
		}

		public void SaveValue()
		{
			if (typeof(T) == typeof(int)) PlayerPrefs.SetInt(key, Convert.ToInt32(Value));
			else if (typeof(T) == typeof(float)) PlayerPrefs.SetFloat(key, Convert.ToSingle(Value));
			else if (typeof(T) == typeof(string)) PlayerPrefs.SetString(key, (string)(object)Value);
			else if (IsListOrArray()) PlayerPrefs.SetString(key, JsonUtility.ToJson(new Wrapper<T>(Value)));
			else if (typeof(T) == typeof(bool)) PlayerPrefs.SetInt(key, (bool)(object)Value ? 1 : 0);
			else if (typeof(T) == typeof(double)) PlayerPrefs.SetString(key, ((double)(object)Value).ToString(CultureInfo.InvariantCulture));
			else PlayerPrefs.SetString(key, JsonUtility.ToJson(Value));
			PlayerPrefs.Save();
		}

		private T LoadValue(T defaultValue)
		{
			Debug.Log("Loaded value " + key);
			if (typeof(T) == typeof(int)) return (T)(object)PlayerPrefs.GetInt(key, Convert.ToInt32(defaultValue));
			if (typeof(T) == typeof(float)) return (T)(object)PlayerPrefs.GetFloat(key, Convert.ToSingle(defaultValue));
			if (typeof(T) == typeof(string)) return (T)(object)PlayerPrefs.GetString(key, (string)(object)defaultValue);
			if (typeof(T) == typeof(bool))
			{
				if (PlayerPrefs.HasKey(key))
				{
					int stored = PlayerPrefs.GetInt(key, -1);
					if (stored == 0 || stored == 1) return (T)(object)(stored == 1);
				}
				return (T)(object)bool.Parse(PlayerPrefs.GetString(key, defaultValue?.ToString() ?? "False"));
			}
			if (typeof(T) == typeof(double)) return (T)(object)double.Parse(PlayerPrefs.GetString(key, Convert.ToDouble(defaultValue).ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
			string json = PlayerPrefs.GetString(key, string.Empty);
			if (string.IsNullOrEmpty(json)) return defaultValue;
			if (!IsListOrArray()) return JsonUtility.FromJson<T>(json);
			string trimmed = json.TrimStart();
			if (trimmed.StartsWith("[")) json = "{\"Data\":" + trimmed + "}";
			Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
			return wrapper != null ? wrapper.Data : defaultValue;
		}

		private static bool IsListOrArray()
		{
			return typeof(T).IsArray || typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>);
		}
	}
}
