using UnityEngine;

namespace EpicToonFX
{
	public class ETFXLightFade : MonoBehaviour
	{
		public enum OnLifeEnd
		{
			DoNothing = 0,
			Disable = 1,
			Destroy = 2
		}

		[Header("Seconds to dim the light")]
		public float life;

		public OnLifeEnd onLifeEnd;

		private Light li;

		private float initIntensity;

		private void Start()
		{
		}

		private void Update()
		{
		}
	}
}
