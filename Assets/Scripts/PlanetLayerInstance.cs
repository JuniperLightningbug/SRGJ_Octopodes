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
	public float _fadeTime = 0.0f;
	
	// Interpreted data
	private float _fadeAmountPerSecond = 0.0f;
	[SerializeField, ReadOnly] private Color[] _vertexColoursOriginal;
	[SerializeField, ReadOnly] private Color[] _vertexColours; // Cached to avoid allocations, but values are recalculated before use
	[SerializeField, ReadOnly] private float[] _satelliteDiscoveryAlphas;
	
	// Testing - do we want angles instead? Change it depending on parameters? Cone from satellite pos ws?
	[SerializeField] private float _satelliteDiscoveryRadius = 0.1f;

	public bool Initialise( Transform inTransform, Mesh inMeshInstance, HexgridMeshData inMeshData )
	{
		_transform = inTransform;
		_meshInstance = inMeshInstance;
		_meshData = inMeshData;
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
		_satelliteDiscoveryAlphas = new float[_vertexColoursOriginal.Length];
		
		_fadeAmountPerSecond = _fadeTime <= 0.0f || Mathf.Approximately( _fadeTime, 0.0f ) ?
			0.0f :
			1.0f / _fadeTime;

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
			for( int faceIdx = 0; faceIdx < _meshData._faceCentres.Length; ++faceIdx )
			{
				Color randomColour = Random.ColorHSV( 0.0f, 1.0f, 0.3f, 0.8f, 0.5f, 0.8f, 1.0f, 1.0f );

				for( int i = 0; i < HexgridMeshData.kFaceVertexCountMax; ++i )
				{
					int vertexIdx = _meshData._faceIdxToVertexIdxs[faceIdx * HexgridMeshData.kFaceVertexCountMax + i];
					if( vertexIdx >= 0 )
					{
						_vertexColours[vertexIdx] = randomColour;
					}
				}
			}

			_meshInstance.SetColors( _vertexColours );
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
	
#endregion

	public void ToggleView( bool bOn )
	{
		if( _transform )
		{
			_transform.gameObject.SetActive( bOn );
		}
	}

	public void UpdateDiscoveryFromSatellitePosition( Vector3 satellitePosition )
	{
		UpdateDiscoveryFromSatellitePositions( new Vector3[] { satellitePosition } );
	}

	public void UpdateDiscoveryFromSatellitePositions( Vector3[] satellitePositions )
	{
		if( !_bInitialised )
		{
			return;
		}

		float satelliteDiscoveryRadiusSqr = _satelliteDiscoveryRadius * _satelliteDiscoveryRadius;
		
		for( int satelliteIdx = 0; satelliteIdx < satellitePositions.Length; ++satelliteIdx )
		{
			// Transform the satellite positions to avoid recalculating the mesh normals array
			_transform.InverseTransformPoints( satellitePositions );
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
					for( int j = 0; j < HexgridMeshData.kFaceVertexCountMax; ++j )
					{
						int vertexIdx = _meshData._faceIdxToVertexIdxs[
							faceIdx * HexgridMeshData.kFaceVertexCountMax + j];
						if( vertexIdx >= 0 )
						{
							_satelliteDiscoveryAlphas[vertexIdx] = 1.0f;
						}
					}
				}
			}
		}
	}

	public void UpdateFadeOut( float deltaTime )
	{
		// TODO: Once we're not using the vertex colours for actual colour, we can apply a custom remap ramp in the shader
		float alpha; // Ignore rider suggestion - caching here is better for big loops
		for( int i = 0; i < _satelliteDiscoveryAlphas.Length; ++i )
		{
			alpha = _satelliteDiscoveryAlphas[i];
			_satelliteDiscoveryAlphas[i] = Mathf.Max( alpha, alpha - _fadeAmountPerSecond * deltaTime );
		}
	}

	public void RefreshMeshColours()
	{
		if( !_meshInstance )
		{
			return;
		}
		
		// For now, we're also using the vertex colours to colour the material.
		// Ideally we wouldn't need this intermediate step & we'd just apply the discovery alphas directly to the vertex colour array cache
		for( int i = 0; i < _vertexColours.Length; ++i )
		{
			_vertexColours[i] = _vertexColoursOriginal[i];
			_vertexColours[i].a *= _satelliteDiscoveryAlphas[i];
		}
		
		_meshInstance.colors = _vertexColours;
	}
}
