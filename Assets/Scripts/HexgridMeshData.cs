using System;
using System.Collections.Generic;
using MM;
using NaughtyAttributes;
using UnityEngine;

[System.Serializable]
public class HexgridMeshData
{
	// Hex grid on a sphere - all faces have either 5 or 6 vertices
	public const int kFaceVertexCountMax = 6; // We use this value to pack the face:vertex array
	public const int kFaceVertexCountMin = 5;

	// Input data
	[SerializeField, ReadOnly] public Mesh _mesh; // Show which mesh was last baked & skip if it hasn't changed
	
	// Validation
	[SerializeField, ReadOnly] public bool _bInitialised = false;

	// Cached mesh data
	[SerializeField, ReadOnly] public Vector3[] _vertices;
	[SerializeField, ReadOnly] public int[] _triangles;
	[SerializeField, ReadOnly] public Vector3[] _normals;

	// Interpreted mesh data
	[SerializeField, ReadOnly] public Vector3[] _faceNormals;
	[SerializeField, ReadOnly] public Vector3[] _faceCentres;

	// Two-way map our vertex indices to an array of unique normals (i.e. group vertices that share faces)
	[SerializeField, ReadOnly] public int[] _faceIdxToVertexIdxs; // Packed 1d array for serialisation and fast iteration
	[SerializeField, ReadOnly] public int[] _vertexIdxToFaceIdx;

	[SerializeField, ReadOnly, TextArea( 1, 50 )] private string _bakeOutput;

	public void InitialiseFromMesh( Mesh inMesh )
	{
		if( inMesh == null )
		{
			Clear();
		}
		else if( inMesh != _mesh )
		{
			_mesh = inMesh;
			Refresh();
		}
	}

	public void Refresh()
	{
		if( _mesh == null )
		{
			Clear();
			return;
		}
		
		_bakeOutput = "";
		
		_vertices = _mesh.vertices;
		_triangles = _mesh.triangles;
		_normals = _mesh.normals;

		_vertexIdxToFaceIdx = new int[_vertices.Length];

		_bInitialised = CalculateMeshFaces();
	}

	public void Clear()
	{
		_mesh = null;
		
		_vertices = Array.Empty<Vector3>();
		_triangles = Array.Empty<int>();
		_normals = Array.Empty<Vector3>();
		
		_faceNormals = Array.Empty<Vector3>();
		_faceCentres = Array.Empty<Vector3>();
		
		_faceIdxToVertexIdxs = Array.Empty<int>();
		_vertexIdxToFaceIdx = new int[_vertices.Length];
		
		_bakeOutput = "";
		
		_bInitialised = false;
	}

	public bool CalculateMeshFaces()
	{
		List<string> bakeOutputErrorList = new List<string>();
		
		
		// +++++ Pass 1 (triangles): Calculate faces: Group indices by their normal directions +++++

		// Work with an array of IndexedHashSets for the face:vertex map as an intermediate. This helps with
		// allocation, bookkeeping, etc. We'll convert it to a packed 1d array for serialisation at the end.
		List<IndexedHashSet<int>> faceIdxToVertexIdxSets = new List<IndexedHashSet<int>>();
		List<Vector3> faceNormalsList = new List<Vector3>();

		/*
		 * Iterate by triangle instead of vertex because:
		 * 1. This means fewer Vector3 comparisons: # triangles < # vertices for hexagon/pentagon groups
		 * 2. We keep count of each vertex usage in triangles, letting us deduce the central vertices for each face
		 */
		for( int triangleIdx = 0; triangleIdx < _triangles.Length; triangleIdx += 3 )
		{
			/*
			 * Note: within this loop, _faceNormals and faceIdxToVertexIdxSets are in lockstep.
			 * The indices ("face index") are shared. When we add to one, we add to both.
			 * _faceCentres needs a second pass, and will sync to the same "face indices" later.
			 */

			int thisTriangleFaceIdx = -1;

			// Assume the triangle normals aren't smoothed
			Vector3 triangleNormal = _normals[_triangles[triangleIdx]];

			/*
			 * Look for a previous normal vector that matches this one.
			 *
			 * Note: can't reliably compare hashed Vector3, so dictionary or hashset lookups don't work here.
			 * Ensure uniqueness manually.
			 * 
			 * Small optimisation: iterate backwards
			 * Triangles are likely to be grouped together spatially - the most recent results are the most relevant.
			 */
			for( int faceIdx = faceNormalsList.Count - 1; faceIdx >= 0; --faceIdx )
			{
				// Note: can't reliably compare hashed Vector3 - dictionary/hashset doesn't ensure uniqueness
				if( faceNormalsList[faceIdx] == triangleNormal )
				{
					// We've already created a face index for this normal direction. Try-add these triangle
					// vertices to the face:vertex map.
					thisTriangleFaceIdx = faceIdx;
					faceIdxToVertexIdxSets[faceIdx].Add( _triangles[triangleIdx] );
					faceIdxToVertexIdxSets[faceIdx].Add( _triangles[triangleIdx + 1] );
					faceIdxToVertexIdxSets[faceIdx].Add( _triangles[triangleIdx + 2] );
					break;
				}
			}

			if( thisTriangleFaceIdx < 0 )
			{
				// This is a new face. Create a new face idx by appending to the face normals and face:vertex map.
				faceNormalsList.Add( triangleNormal );
				faceIdxToVertexIdxSets.Add( new IndexedHashSet<int>()
				{
					_triangles[triangleIdx],
					_triangles[triangleIdx + 1],
					_triangles[triangleIdx + 2],
				} );
				thisTriangleFaceIdx = faceNormalsList.Count - 1;
			}

			// Here, 'thisTriangleFaceIdx' is guaranteed to be valid for 'faceNormalsList' and 'faceIdxToVertexIdxSets'
			_vertexIdxToFaceIdx[_triangles[triangleIdx]] = thisTriangleFaceIdx;
			_vertexIdxToFaceIdx[_triangles[triangleIdx + 1]] = thisTriangleFaceIdx;
			_vertexIdxToFaceIdx[_triangles[triangleIdx + 2]] = thisTriangleFaceIdx;
		}

		_faceNormals = faceNormalsList.ToArray();
		

		// +++ Pass 2 (faces): serialise the face:vertex map and calculate the centre positions +++

		int faceCount = faceIdxToVertexIdxSets.Count;
		_faceIdxToVertexIdxs = new int[faceCount * kFaceVertexCountMax];
		for( int i = 0; i < _faceIdxToVertexIdxs.Length; ++i )
		{
			// Populate packed array with empty flagged entries
			_faceIdxToVertexIdxs[i] = -1;
		}

		_faceCentres = new Vector3[faceCount];
		int debugReverseMappedVertexCount = 0;

		for( int faceIdx = 0; faceIdx < faceCount; ++faceIdx )
		{
			// Everything is a regular pentagon/hexagon. Take the average position of the vertices as the centre.
			Vector3 vertexSum = Vector3.zero;
			int faceVertexCount = faceIdxToVertexIdxSets[faceIdx].Count;
			if( faceVertexCount > kFaceVertexCountMax || faceVertexCount < kFaceVertexCountMin )
			{
				bakeOutputErrorList.Add( $"Face [{faceIdx}] has [{faceVertexCount}] vertices. Expected between [{kFaceVertexCountMin}] and [{kFaceVertexCountMax}]!" );
			}
			for( int vertexSetIdx = 0; vertexSetIdx < faceVertexCount && vertexSetIdx < kFaceVertexCountMax; ++vertexSetIdx )
			{
				int vertexIdx = faceIdxToVertexIdxSets[faceIdx][vertexSetIdx];
				_faceIdxToVertexIdxs[faceIdx * kFaceVertexCountMax + vertexSetIdx] = vertexIdx;

				vertexSum += _vertices[vertexIdx];
			}

			_faceCentres[faceIdx] = vertexSum / (float)faceVertexCount;

			debugReverseMappedVertexCount += faceVertexCount;
		}

		// Output validation info
		if( _vertexIdxToFaceIdx.Length != debugReverseMappedVertexCount )
		{
			bakeOutputErrorList.Add(
				$"\tBake did not complete successfully. {_vertexIdxToFaceIdx.Length} vertices in Vertex:Face map; {debugReverseMappedVertexCount} vertices in Face:Vertex map." );
		}
		if( _vertexIdxToFaceIdx.Length != _vertices.Length )
		{
			bakeOutputErrorList.Add(
				$"\tBake did not complete successfully. {_vertexIdxToFaceIdx.Length} vertices in input mesh; {_vertexIdxToFaceIdx.Length} vertices parsed." );
		}
		string bakeOutputErrors = $"\t{string.Join( "\n\t", bakeOutputErrorList )}";
		string bakeOutputSummary = string.Format(
			"Mesh: {0} vertices, {1} normals, {2} triangles.\nResult: {3} vertices mapped ({4} reverse-mapped) to {5} distinct faces.",
			_vertices.Length,
			_normals.Length,
			_triangles.Length,
			_vertexIdxToFaceIdx.Length,
			debugReverseMappedVertexCount,
			_faceNormals.Length );
		_bakeOutput =
			$"Hexgrid face data for [{_mesh.name}]:\n{bakeOutputSummary}\nErrors: {bakeOutputErrorList.Count}\n{bakeOutputErrors}";
		Debug.Log( bakeOutputSummary );

		return bakeOutputErrorList.Count == 0;
	}

	public Vector3[] GetDebugFaceNormals( float lineLength )
	{
		Vector3[] outFaceNormalLines = new Vector3[_faceNormals.Length * 2];
		for( int i = 0; i < _faceNormals.Length; ++i )
		{
			outFaceNormalLines[i * 2] = _faceCentres[i];
			outFaceNormalLines[i * 2 + 1] = _faceCentres[i] + lineLength * _faceNormals[i];
		}

		return outFaceNormalLines;
	}
}
