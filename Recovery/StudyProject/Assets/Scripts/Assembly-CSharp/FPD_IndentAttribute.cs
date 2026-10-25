using UnityEngine;

public class FPD_IndentAttribute : PropertyAttribute
{
	public int IndentCount;

	public int LabelsWidth;

	public int SpaceAfter;

	public FPD_IndentAttribute(int indent = 1, int labelsWidth = 0, int spaceAfter = 0)
	{
	}
}
