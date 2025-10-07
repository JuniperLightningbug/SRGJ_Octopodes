using UnityEngine;

public static class PrimitiveQuadBuilder
{
	public static Mesh BuildQuad( float halfSize, float boundingSizeMultiplier )
	{
		Mesh outMesh = new Mesh();

		outMesh.vertices = new Vector3[]
		{
			new Vector3( -halfSize, -halfSize, 0 ),
			new Vector3( halfSize, -halfSize, 0 ),
			new Vector3( halfSize, halfSize, 0 ),
			new Vector3( -halfSize, halfSize, 0 ),
		};

		outMesh.triangles = new int[]
		{
			0, 1, 2,
			2, 3, 0,
		};

		outMesh.uv = new Vector2[]
		{
			new Vector2( 0.0f, 0.0f ),
			new Vector2( 1.0f, 0.0f ),
			new Vector2( 1.0f, 1.0f ),
			new Vector2( 0.0f, 1.0f ),
		};

		outMesh.RecalculateBounds();
		Vector3 boundsExtents = outMesh.bounds.extents;
		boundsExtents *= boundingSizeMultiplier;
		outMesh.bounds = new Bounds( outMesh.bounds.center, boundsExtents );

		return outMesh;
	}
}
