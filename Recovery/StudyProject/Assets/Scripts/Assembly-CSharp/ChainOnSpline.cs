using UnityEngine;

[ExecuteAlways]
public class ChainOnSpline : MonoBehaviour
{
	public Mesh linkMesh;

	public Material linkMaterial;

	public int linkCount;

	public float radiusX;

	public float radiusZ;

	public float speed;

	public Vector3 linkScale;

	private float offset;

	private Matrix4x4[] matrices;

	private void Update()
	{
	}

	private void OnDrawGizmos()
	{
	}

	private void BuildMatrices()
	{
	}
}
