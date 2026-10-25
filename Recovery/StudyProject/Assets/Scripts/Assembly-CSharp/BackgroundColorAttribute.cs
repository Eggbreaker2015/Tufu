using UnityEngine;

public class BackgroundColorAttribute : PropertyAttribute
{
	public float r;

	public float g;

	public float b;

	public float a;

	public Color Color => default;

	public BackgroundColorAttribute()
	{
	}

	public BackgroundColorAttribute(float aR, float aG, float aB, float aA)
	{
	}
}
