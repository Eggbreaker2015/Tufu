public static class FEasing
{
	public enum EFease
	{
		EaseInCubic = 0,
		EaseOutCubic = 1,
		EaseInOutCubic = 2,
		EaseInOutElastic = 3,
		EaseInElastic = 4,
		EaseOutElastic = 5,
		EaseInExpo = 6,
		EaseOutExpo = 7,
		EaseInOutExpo = 8,
		Linear = 9
	}

	public delegate float Function(float s, float e, float v, float extraParameter = 1f);

	public static float EaseInCubic(float start, float end, float value, float ignore = 1f)
	{
		return 0f;
	}

	public static float EaseOutCubic(float start, float end, float value, float ignore = 1f)
	{
		return 0f;
	}

	public static float EaseInOutCubic(float start, float end, float value, float ignore = 1f)
	{
		return 0f;
	}

	public static float EaseOutElastic(float start, float end, float value, float rangeMul = 1f)
	{
		return 0f;
	}

	public static float EaseInElastic(float start, float end, float value, float rangeMul = 1f)
	{
		return 0f;
	}

	public static float EaseInOutElastic(float start, float end, float value, float rangeMul = 1f)
	{
		return 0f;
	}

	public static float EaseInExpo(float start, float end, float value, float ignore = 1f)
	{
		return 0f;
	}

	public static float EaseOutExpo(float start, float end, float value, float ignore = 1f)
	{
		return 0f;
	}

	public static float EaseInOutExpo(float start, float end, float value, float ignore = 1f)
	{
		return 0f;
	}

	public static float Linear(float start, float end, float value, float ignore = 1f)
	{
		return 0f;
	}

	public static Function GetEasingFunction(EFease easingFunction)
	{
		return null;
	}
}
