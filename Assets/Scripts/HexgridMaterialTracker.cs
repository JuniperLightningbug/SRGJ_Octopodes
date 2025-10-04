using System.Collections.Generic;
using MM;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class HexgridMaterialTracker : StandaloneSingletonBase<HexgridMaterialTracker>
{
	// Note: we're assuming that the mesh does not smooth normals between hexagons
	[SerializeField] private MeshFilter _hexgridMeshFilter;
	private Transform _hexgridTransform;
	private Mesh _meshTemplate;
	private Mesh _mesh; // Active copy of the mesh

	/**
	 * Cached and interpreted data for a mesh asset. TODO: Move to an SO for initialisation in edit time.
	 */
	[System.Serializable]
	public class EditTimeHexgridMeshData
	{
		// Cached mesh data (can be calculated in edit-time)
		public readonly Vector3[] _vertices;
		public readonly int[] _triangles;
		public readonly Vector3[] _normals;

		// Interpreted mesh data (can be calculated in edit-time if we use serialisable types)
		public Vector3[] _faceNormals;
		public Vector3[] _faceCentres;

		// Two-way map our vertex indices to an array of unique normals (i.e. group vertices that share faces)
		public const int _kFaceVertexCountMax = 6; // Hex grid on a sphere - we have faces of either 5 or 6 verts
		public int[] _faceIdxToVertexIdxs; // Packed 1d array for serialisation and fast iteration
		public int[] _vertexIdxToFaceIdx;

		public EditTimeHexgridMeshData( Mesh mesh )
		{
			_vertices = mesh.vertices;
			_triangles = mesh.triangles;
			_normals = mesh.normals;
			
			_vertexIdxToFaceIdx = new int[_vertices.Length];

			CalculateMeshFaces();
		}

		public bool CalculateMeshFaces()
		{
			// Note: can't reliably use dictionaries or hash sets with the normals as keys. Need to iterate to compare.

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

				// Normals for the triangles are shared - we only need to look at one
				Vector3 triangleNormal = _normals[_triangles[triangleIdx]];

				// Small optimisation: iterate backwards
				// The triangles are very likely to be grouped together spatially, so the most likely place to find an
				// overlap is with the most recent entry
				for( int faceIdx = faceNormalsList.Count - 1; faceIdx >= 0; --faceIdx )
				{
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

				// At this point, 'thisTriangleFaceIdx' is guaranteed to be valid
				_vertexIdxToFaceIdx[_triangles[triangleIdx]] = thisTriangleFaceIdx;
				_vertexIdxToFaceIdx[_triangles[triangleIdx + 1]] = thisTriangleFaceIdx;
				_vertexIdxToFaceIdx[_triangles[triangleIdx + 2]] = thisTriangleFaceIdx;
			}
			
			_faceNormals = faceNormalsList.ToArray();
			
			// +++ Pass 2 (faces): serialise the face:vertex map and calculate the centre positions +++
			
			int faceCount = faceIdxToVertexIdxSets.Count;
			_faceIdxToVertexIdxs = new int[faceCount * _kFaceVertexCountMax];
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
				for( int vertexSetIdx = 0; vertexSetIdx < faceVertexCount; ++vertexSetIdx )
				{
					int vertexIdx = faceIdxToVertexIdxSets[faceIdx][vertexSetIdx];
					if( vertexSetIdx > _kFaceVertexCountMax )
					{
						Debug.LogErrorFormat( "Face {0} has {1} vertices (more than the maximum of {2}!)",
							faceIdx, vertexSetIdx + 1, _kFaceVertexCountMax );
					}
					else
					{
						_faceIdxToVertexIdxs[faceIdx * _kFaceVertexCountMax + vertexSetIdx] = vertexIdx;
					}
					vertexSum += _vertices[vertexIdx];
				}
				_faceCentres[faceIdx] = vertexSum / (float)faceVertexCount;
				
				debugReverseMappedVertexCount += faceVertexCount;
			}
			
			Debug.LogFormat(
				"Mesh: {0} vertices, {1} normals, {2} triangles.\nResult: {3} vertices mapped ({4} reverse-mapped) to {5} distinct faces.",
				_vertices.Length,
				_normals.Length,
				_triangles.Length,
				_vertexIdxToFaceIdx.Length,
				debugReverseMappedVertexCount,
				_faceNormals.Length );

			return debugReverseMappedVertexCount == _vertexIdxToFaceIdx.Length &&
			       debugReverseMappedVertexCount == _vertices.Length;
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

	[SerializeField] private EditTimeHexgridMeshData _editTimeMeshData;
	private Vector3[] _faceCentresWS;
	private Vector3[] _faceNormalsWS;
	private Color[] _vertexColours;

	[SerializeField] private Transform _debugTestProximityTransform;
	[SerializeField] private float _debugTestProximityRadius;

	// Time in seconds until alpha 1 becomes alpha 0
	[SerializeField] private float _fadeOutTime = 1.0f;
	[SerializeField] private bool _bDoFadeOut = false;
	
	[SerializeField] private List<Transform> _trackedTransforms = new List<Transform>();
	
	[Button( "Set Transparent" )]
	void InspectorEventSetTransparent()
	{
		if( Application.isPlaying && _editTimeMeshData != null )
		{
			for( int i = 0; i < _vertexColours.Length; ++i )
			{
				_vertexColours[i] = Color.white;
				_vertexColours[i].a = 0.0f;
			}
			_mesh.SetColors( _vertexColours );
		}
	}
	
	protected override void Initialise()
	{
		InitialiseEditTimeMeshData(); // TODO move to edit time depending on the performance hit
		InitialiseRuntimeMeshData();
	}

	private void InitialiseEditTimeMeshData()
	{
		_meshTemplate = _hexgridMeshFilter?.mesh;
		if( _meshTemplate )
		{
			_editTimeMeshData = new EditTimeHexgridMeshData( _meshTemplate );
		}
	}

	private void InitialiseRuntimeMeshData()
	{
		if( _editTimeMeshData != null && _hexgridMeshFilter && _meshTemplate )
		{
			// Make a copy of the mesh for edit-time changes
			_mesh = Instantiate( _meshTemplate );
			_mesh.MarkDynamic();
			_hexgridMeshFilter.mesh = _mesh;

			_vertexColours = new Color[_editTimeMeshData._vertices.Length];
			
			_hexgridTransform = _hexgridMeshFilter.transform;
			_faceCentresWS = new Vector3[_editTimeMeshData._faceCentres.Length];
			_faceNormalsWS = new Vector3[_editTimeMeshData._faceNormals.Length];
			_hexgridTransform.TransformPoints( _editTimeMeshData._faceCentres, _faceCentresWS );
			_hexgridTransform.TransformDirections( _editTimeMeshData._faceNormals, _faceNormalsWS );
		}
	}

	public void TrackTransform( Transform inTransform )
	{
		// tODO bookkeeping
		_trackedTransforms.Add( inTransform );
	}
	
	private void OnDrawGizmosSelected()
	{
		DebugMeshDataGizmos();
	}

	private void OnDrawGizmos()
	{
		DebugAxisGizmos();
		SatelliteGizmos();
	}

	private void DebugMeshDataGizmos()
	{
		if( _editTimeMeshData != null )
		{
			Vector3[] debugMeshNormals = _editTimeMeshData.GetDebugFaceNormals( 0.1f );
			Gizmos.color = Color.yellow;
			Gizmos.DrawLineList( debugMeshNormals );
		}
	}

	private void DebugAxisGizmos()
	{
		if( _hexgridMeshFilter != null )
		{
			Gizmos.color = Color.grey;
			Gizmos.DrawLine( _hexgridMeshFilter.transform.position - _hexgridMeshFilter.transform.up * 2.0f,
				_hexgridMeshFilter.transform.position);
			Gizmos.color = Color.white;
			Gizmos.DrawLine( _hexgridMeshFilter.transform.position,
				_hexgridMeshFilter.transform.position + _hexgridMeshFilter.transform.up * 2.0f);
		}
	}

	private void SatelliteGizmos()
	{
		if( _mesh && _editTimeMeshData != null && _debugTestProximityTransform )
		{
			Vector3 testProximityPositionWS = _hexgridTransform.position + Vector3.Normalize(_debugTestProximityTransform.transform.position - _hexgridTransform.position) * _hexgridTransform.lossyScale.x;
			
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere( testProximityPositionWS, _debugTestProximityRadius );

		}
	}

	void Update()
	{
		if( Keyboard.current.enterKey.wasPressedThisFrame )
		{
			DebugTestRandomColours();
		}
		
		DebugTestProximity();
		
		if( _bDoFadeOut && _mesh && _editTimeMeshData != null )
		{
			FadeOut( Time.deltaTime );
		}

		if( _mesh && _editTimeMeshData != null )
		{
			PaintFromTrackedPositions();
		}
	}

	private void PaintFromTrackedPositions()
	{
		for( int trackedTransformIdx = 0; trackedTransformIdx < _trackedTransforms.Count; ++trackedTransformIdx )
		{
			// Project onto out 2unit sphere
			Vector3 testProximityPositionWS = _hexgridTransform.position + (_trackedTransforms[trackedTransformIdx].position - _hexgridTransform.position).normalized * _hexgridTransform.lossyScale.x;
			// TODO There are better ways
			
			for( int i = 0; i < _faceCentresWS.Length; ++i )
			{
				if( (_faceCentresWS[i] - testProximityPositionWS).sqrMagnitude <
				    _debugTestProximityRadius * _debugTestProximityRadius )
				{
					for( int j = 0; j < EditTimeHexgridMeshData._kFaceVertexCountMax; ++j )
					{
						int vertexIdx =
							_editTimeMeshData._faceIdxToVertexIdxs[
								i * EditTimeHexgridMeshData._kFaceVertexCountMax + j];
						if( vertexIdx >= 0 )
						{
							_vertexColours[vertexIdx].a = 1.0f;
						}
					}
				}
			}
			_mesh.SetColors( _vertexColours );
		}
	}

	private void FadeOut( float deltaTime )
	{
		float fadeOutAmount = deltaTime / _fadeOutTime;
		for( int i = 0; i < _vertexColours.Length; ++i )
		{
			_vertexColours[i].a = Mathf.Max( _vertexColours[i].a - fadeOutAmount, 0.0f );
		}
		_mesh.SetColors( _vertexColours );
	}

	private void DebugTestProximity()
	{
		if( _mesh && _editTimeMeshData != null && _debugTestProximityTransform )
		{
			// Project onto out 2unit sphere
			Vector3 testProximityPositionWS = _hexgridTransform.position + (_debugTestProximityTransform.transform.position - _hexgridTransform.position).normalized * _hexgridTransform.lossyScale.x;
			// TODO There are better ways
			
			for( int i = 0; i < _faceCentresWS.Length; ++i )
			{
				if( (_faceCentresWS[i] - testProximityPositionWS).sqrMagnitude <
				    _debugTestProximityRadius * _debugTestProximityRadius )
				{
					for( int j = 0; j < EditTimeHexgridMeshData._kFaceVertexCountMax; ++j )
					{
						int vertexIdx =
							_editTimeMeshData._faceIdxToVertexIdxs[
								i * EditTimeHexgridMeshData._kFaceVertexCountMax + j];
						if( vertexIdx >= 0 )
						{
							_vertexColours[vertexIdx] = Color.blue;
						}
					}
				}
			}
			_mesh.SetColors( _vertexColours );
		}
	}

	private void DebugTestRandomColours()
	{
		if( _mesh )
		{
			for( int faceIdx = 0; faceIdx < _editTimeMeshData._faceCentres.Length; ++faceIdx )
			{
				Color randomColour = Random.ColorHSV( 0.0f, 1.0f, 0.3f, 0.8f, 0.5f, 0.8f, 1.0f, 1.0f );
				Color normalColour = new Color(
					_editTimeMeshData._faceNormals[faceIdx].x,
					_editTimeMeshData._faceNormals[faceIdx].y,
					_editTimeMeshData._faceNormals[faceIdx].z,
					1.0f );
				for( int i = 0; i < EditTimeHexgridMeshData._kFaceVertexCountMax; ++i )
				{
					int vertexIdx = _editTimeMeshData._faceIdxToVertexIdxs[faceIdx * EditTimeHexgridMeshData._kFaceVertexCountMax + i];
					if( vertexIdx >= 0 )
					{
						_vertexColours[vertexIdx] = randomColour;
					}
				}
			}

			_mesh.SetColors( _vertexColours );
		}
	}
	
	/*
	 * TODOs
	 * - Struct to class
	 * - Array of face radii
	 * - Move all this stuff into edit time
	 * - Edit time SO with button
	 * - New runtime transformations: face centres, face normals
	 * - Proximity colour
	 * -- Cone? proximity rejection on normals? Something like that?
	 * - Persistent colours
	 * - Timeout colours
	 * - We could spatially map the vertices to speed up the lookups
	 */
}