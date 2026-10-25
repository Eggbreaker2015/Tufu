using System.Collections.Generic;
using UnityEngine;

public class PolygonGenerator : MonoBehaviour
{
	private Mesh mesh;

	public Vector3[] polygonPoints;

	public int[] polygonTriangles;

	public bool isFilled;

	public int polygonSides;

	public float polygonRadius;

	public float centerRadius;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void DrawFilled(int sides, float radius)
	{
	}

	private void DrawHollow(int sides, float outerRadius, float innerRadius)
	{
	}

	private int[] DrawHollowTriangles(Vector3[] points)
	{
		return null;
	}

	private List<Vector3> GetCircumferencePoints(int sides, float radius)
	{
		return null;
	}

	private int[] DrawFilledTriangles(Vector3[] points)
	{
		return null;
	}
}
