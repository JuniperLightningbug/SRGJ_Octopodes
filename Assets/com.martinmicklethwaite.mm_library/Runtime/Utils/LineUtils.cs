using System.Collections.Generic;
using UnityEngine;

namespace MM
{
	public static class LineUtils
	{
		public static List<Vector3> GetArcPolylinePoints(
			Vector3 centre,
			float radius,
			int fullCircleLineSegments,
			float minAngle = 0.0f,
			float maxAngle = 2 * Mathf.PI )
		{
			float arcAngle = maxAngle - minAngle;
			if( arcAngle < 0.0f )
			{
				arcAngle += (2.0f * Mathf.PI);
			}

			List<Vector3> outVertices;

			if( Mathf.Approximately( arcAngle, 0.0f ) )
			{
				outVertices = new List<Vector3>();
			}
			else if( Mathf.Approximately( radius, 0.0f ) )
			{
				outVertices = new List<Vector3>()
				{
					centre
				};
			}
			else
			{
				float rotationRatio = arcAngle / (2 * Mathf.PI);
				int numSegments = Mathf.Max( Mathf.CeilToInt( fullCircleLineSegments * rotationRatio ), 1 );
				float segmentAngle =
					arcAngle /
					(float)numSegments; // This won't necessarily be exactly the same as 2PI/fullCircleLineSegments

				outVertices = new List<Vector3>( numSegments );

				for( int i = 0; i < numSegments; ++i )
				{
					float pointAngle = minAngle + segmentAngle * i;
					outVertices.Add( centre + new Vector3(
						Mathf.Cos( pointAngle ) * radius,
						0.0f,
						Mathf.Sin( pointAngle ) * radius ) );
				}
			}

			return outVertices;
		}

		public static List<Vector3> GetRoundedPolygonPolylinePoints(
			Vector3[] vertices,
			float radius,
			int fullCircleLineSegments )
		{

			List<Vector3> outOutlineLineVertices = new List<Vector3>();

			if( vertices != null )
			{
				if( vertices.Length == 1 )
				{
					// Single vertex - a sphere (simple case)
					outOutlineLineVertices = GetArcPolylinePoints(
						vertices[0],
						radius,
						fullCircleLineSegments );
				}
				else if( vertices.Length > 1 )
				{
					// Multiple vertices - a convex hull
					// (NOTE: Vertices need to be ordered)
					int numVertices = vertices.Length;
					for( int i = 0; i < numVertices; ++i )
					{
						int prevIdx = Mathf.Abs( (i + numVertices - 1) % numVertices );
						int nextIdx = Mathf.Abs( (i + numVertices + 1) % numVertices );
						Vector3 fromPrev = vertices[i] - vertices[prevIdx];
						Vector3 fromPrevOrthogonal = Vector3.Cross( Vector3.up, fromPrev );
						fromPrevOrthogonal.y = 0.0f;
						fromPrevOrthogonal.Normalize();
						Vector3 toNext = vertices[nextIdx] - vertices[i];
						Vector3 toNextOrthogonal = Vector3.Cross( Vector3.up, toNext );
						toNextOrthogonal.y = 0.0f;
						toNextOrthogonal.Normalize();

						float angleFrom = Mathf.Acos( fromPrevOrthogonal.x ) * Mathf.Sign( fromPrevOrthogonal.z );
						float angleTo = Mathf.Acos( toNextOrthogonal.x ) * Mathf.Sign( toNextOrthogonal.z );

						outOutlineLineVertices.AddRange(
							GetArcPolylinePoints(
								vertices[i],
								radius,
								fullCircleLineSegments,
								angleFrom,
								angleTo ) );
					}
				}
			}

			return outOutlineLineVertices;
		}
	}
}
