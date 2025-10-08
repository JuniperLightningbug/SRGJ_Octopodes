using MM;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * Runtime instance of a planet layer.
 * Responds to satellite proximity events, records progress, applies visuals.
 */
[System.Serializable]
public class PlanetLayerInstance
{
	public bool _bInitialised = false;
	
	// Input data
	public Transform _transform;
	public Mesh _meshInstance;
	public HexgridMeshData _meshData;
	public float _fadeTime = 0.5f;
	public SO_PlanetConfig.ESensorType _sensorType;
	public bool _bHasTexture = false;
	
	// Interpreted data
	[SerializeField, ReadOnly] private Color[] _vertexColoursOriginal;
	[SerializeField, ReadOnly] private Color[] _vertexColours; // Cached to avoid allocations, but values are recalculated before use
	[SerializeField, ReadOnly] private float[] _faceDiscoveryValues;
	private Color[] _debugColours;
	[SerializeField, ReadOnly] public float _discoveryValue = 0.0f;
	
	// TODO: We can precalculate this or cache on init.
	// At the moment it's useful to expose it to the inspector in this format
	private float FadeAmountPerSecond => _fadeTime <= 0.0f || Mathf.Approximately( _fadeTime, 0.0f ) ?
		0.0f :
		1.0f / _fadeTime;
	
	// Testing - do we want angles instead? Change it depending on parameters? Cone from satellite pos ws?
	[SerializeField] private float _satelliteDiscoveryRadius = 0.3f;
	
	public IndexedHashSet<Transform> _trackedSatellites = new IndexedHashSet<Transform>();

	public bool Initialise(
		Transform inTransform,
		Mesh inMeshInstance,
		HexgridMeshData inMeshData,
		SO_PlanetConfig.ESensorType inSensorType )
	{
		_transform = inTransform;
		_meshInstance = inMeshInstance;
		_meshData = inMeshData;
		_sensorType = inSensorType;
		return Initialise();
	}
	
	public bool Initialise()
	{
		if( !_transform || !_meshInstance || _meshData == null )
		{
			return false;
		}
		
		_vertexColoursOriginal = _meshInstance.colors;
		_vertexColours = new Color[_vertexColoursOriginal.Length];
		
		_faceDiscoveryValues = new float[_meshData._faceCentres.Length];

		_bInitialised = true;
		return true;
	}

#region Debug Interface
	
	public void DebugDrawMeshDataGizmos()
	{
		if( _bInitialised )
		{
			Vector3[] debugMeshNormals = _meshData.GetDebugFaceNormals( 0.1f );
			_transform.TransformVectors( debugMeshNormals );
			Gizmos.color = Color.yellow;
			Gizmos.DrawLineList( debugMeshNormals );
		}
	}
	
	public void DebugShowFaces()
	{
		if( _bInitialised )
		{
			if( _debugColours == null || _debugColours.Length != _vertexColours.Length )
			{
				_debugColours = new Color[_vertexColours.Length];
				for( int faceIdx = 0; faceIdx < _meshData._faceCentres.Length; ++faceIdx )
				{
					Color randomColour = Random.ColorHSV( 0.0f, 1.0f, 0.3f, 0.8f, 0.5f, 0.8f, 1.0f, 1.0f );

					for( int i = 0; i < HexgridMeshData.kFaceVertexCountMax; ++i )
					{
						int vertexIdx = _meshData._faceIdxToVertexIdxs[faceIdx * HexgridMeshData.kFaceVertexCountMax + i];
						if( vertexIdx >= 0 )
						{
							_debugColours[vertexIdx] = randomColour;
						}
					}
				}
			}
			
			_meshInstance.SetColors( _debugColours );
		}
	}

	public void DebugShowFaceNormals()
	{
		if( _bInitialised )
		{
			for( int vertexIdx = 0; vertexIdx < _meshData._vertices.Length; ++vertexIdx )
			{
				Vector3 faceNormal = _meshData._faceNormals[_meshData._vertexIdxToFaceIdx[vertexIdx]];
				Color normalColour = new Color(
					faceNormal.x,
					faceNormal.y,
					faceNormal.z,
					1.0f );
				_vertexColours[vertexIdx] = normalColour;
			}

			_meshInstance.SetColors( _vertexColours );
		}
	}

	public void DebugClearDiscoveryAlphas()
	{
		if( _bInitialised )
		{
			_meshInstance.SetColors( _vertexColoursOriginal );
		}
	}
	
#endregion

	public void ToggleView( bool bOn )
	{
		if( _transform )
		{
			_transform.gameObject.SetActive( bOn );
		}
	}

	public void StartTrackingSatellite( Transform inTransform )
	{
		_trackedSatellites.Add( inTransform );
	}

	public void StopTrackingSatellite( Transform inTransform )
	{
		_trackedSatellites.Remove( inTransform );
	}

	private void ClearNullSatellites()
	{
		foreach( Transform transform in _trackedSatellites )
		{
			if( !_transform )
			{
				_trackedSatellites.Remove(transform);
			}
		}
	}

	// Returns value 0..1 for proportion of planet discovered
	public void AddDiscoveryFromTrackedSatellites()
	{
		Vector3[] positions = new Vector3[_trackedSatellites.Count];
		for( int i = 0; i < _trackedSatellites.Count; ++i )
		{
			positions[i] = _trackedSatellites[i].position;
		}
		AddDiscoveryFromSatellitePositions( positions );
	}
	
	public void UpdateDiscoveryFromSatellitePosition( Vector3 satellitePosition )
	{
		AddDiscoveryFromSatellitePositions( new Vector3[] { satellitePosition } );
	}

	public void AddDiscoveryFromSatellitePositions( Vector3[] satellitePositions )
	{
		if( !_bInitialised )
		{
			return;
		}

		float satelliteDiscoveryRadiusSqr = _satelliteDiscoveryRadius * _satelliteDiscoveryRadius;
		
		// Transform the satellite positions to avoid recalculating the mesh normals array
		_transform.InverseTransformPoints( satellitePositions );
		
		for( int satelliteIdx = 0; satelliteIdx < satellitePositions.Length; ++satelliteIdx )
		{
			for( int i = 0; i < satellitePositions.Length; ++i )
			{
				// Project onto sphere
				satellitePositions[i].Normalize();
			}
			
			// TODO this can be optimised a lot. e.g. we could spatially map the hexgrid data to reduce lookups
			for( int faceIdx = 0; faceIdx < _meshData._faceCentres.Length; ++faceIdx )
			{
				if( (_meshData._faceCentres[faceIdx] - satellitePositions[satelliteIdx]).sqrMagnitude <
				    satelliteDiscoveryRadiusSqr )
				{
					_faceDiscoveryValues[faceIdx] = 1.0f;
				}
			}
		}
	}

	public void FadeDiscovery( float deltaTime )
	{
		// TODO: Once we're not using the vertex colours for actual colour, we can apply a custom remap ramp in the shader
		float fadeAmount = FadeAmountPerSecond * deltaTime;
		
		for( int i = 0; i < _faceDiscoveryValues.Length; ++i )
		{
			_faceDiscoveryValues[i] = Mathf.Max( 0.0f, _faceDiscoveryValues[i] - fadeAmount );
		}
	}

	public float RecalculateCurrentDiscoveryValue()
	{
		if( _faceDiscoveryValues == null || _faceDiscoveryValues.Length == 0 )
		{
			return 0.0f;
		}

		float totalDiscovery = 0.0f;
		for( int i = 0; i < _faceDiscoveryValues.Length; ++i )
		{
			totalDiscovery += _faceDiscoveryValues[i];
		}

		_discoveryValue = totalDiscovery / _faceDiscoveryValues.Length;
		return _discoveryValue;
	}

	public float UpdateDiscovery( float deltaTime )
	{
		FadeDiscovery( deltaTime );
		AddDiscoveryFromTrackedSatellites();
		return RecalculateCurrentDiscoveryValue();
	}

	public void RefreshMeshColours( bool bShowUndiscovered = false )
	{
		if( !_meshInstance )
		{
			return;
		}

		if( bShowUndiscovered )
		{
			if( _bHasTexture )
			{
				for( int i = 0; i < _vertexColours.Length; ++i )
				{
					_vertexColours[i].a = 1.0f;
				}
			}
			else
			{
				_meshInstance.colors = _vertexColoursOriginal;
			}
		}
		else
		{
			if( _bHasTexture )
			{
				// TODO: Ideally this is the only case in future, in which case we can apply the vertex colours as the
				// alphas are calculated and avoid a separate loop
				for( int i = 0; i < _vertexColours.Length; ++i )
				{
					_vertexColours[i].a = _faceDiscoveryValues[_meshData._vertexIdxToFaceIdx[i]];
				}
			}
			else
			{
				// Else, we're colouring the mesh using vertex colours. Take the original values into account.
				for( int i = 0; i < _vertexColours.Length; ++i )
				{
					_vertexColours[i] = _vertexColoursOriginal[i];
					_vertexColours[i].a *= _faceDiscoveryValues[_meshData._vertexIdxToFaceIdx[i]];
				}
			}

			_meshInstance.colors = _vertexColours;
		}
	}
}
