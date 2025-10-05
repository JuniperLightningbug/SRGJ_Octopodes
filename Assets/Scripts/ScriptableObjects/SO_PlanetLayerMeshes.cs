using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

/**
 * Collection of serialised SO_HexgridMeshData assets stored on disc
 */
[CreateAssetMenu(fileName = "PlanetLayerMeshes", menuName = "Scriptable Objects/PlanetLayerMeshes")]
public class SO_PlanetLayerMeshes : ScriptableObject
{

	public enum EHexgridMeshKey
	{
		INVALID = -1,
		
		// NOTE: THESE WILL BE PLANET-SPECIFIC! they're just to create a lookup table
		Aurora,
		Magneto,
		MassSpec,
		Plasma,
		// Add others here
		
		COUNT
	}
	
	[System.Serializable]
	public class HexgridMeshDataTuple
	{
		// Keys
		[SerializeField] public EHexgridMeshKey _key;
		[SerializeField] public Mesh _meshAsset;

		// Values
		[SerializeField, ReadOnly, AllowNesting] public HexgridMeshData _meshData;

		public void RefreshMeshData( bool bForce = false )
		{
			if( _meshData == null || bForce )
			{
				_meshData = new HexgridMeshData();
			}
			_meshData.InitialiseFromMesh( _meshAsset );
		}
	}
	
	// Edit-time info (serialisable list with manually enforced uniqueness of 'HexgridLayerData._key' key)
	[SerializeField]
	public HexgridMeshDataTuple[] _hexgridLayerList = new HexgridMeshDataTuple[(int)EHexgridMeshKey.COUNT];
	
	// Runtime info (lazily init this on poll)
	private Dictionary<EHexgridMeshKey, HexgridMeshData> _hexgridMeshDataByKey = new Dictionary<EHexgridMeshKey, HexgridMeshData>();
	private Dictionary<Mesh, HexgridMeshData> _hexgridMeshDataBySharedMesh = new Dictionary<Mesh, HexgridMeshData>();

	[Button( "Refresh Hexgrid Layers" )]
	public void Editor_RefreshHexgridLayers()
	{
#if UNITY_EDITOR

		/*
		 * Cross-reference with '_EHexgridMeshKey' to:
		 * - Clear duplicate elements (or just warn?)
		 * - Add missing layer keys
		 * - Refresh mesh data if necessary
		 */
		Undo.RecordObject( this, "Refreshed hexgrid layer list" );

		HexgridMeshDataTuple[] newLayers = new HexgridMeshDataTuple[(int)EHexgridMeshKey.COUNT];
		
		for( int layerKey = 0; layerKey < (int)EHexgridMeshKey.COUNT; ++layerKey )
		{
			bool bCopiedExisting = false;
			for( int i = 0; i < _hexgridLayerList.Length; ++i )
			{
				if( _hexgridLayerList[i] != null && _hexgridLayerList[i]._key == (EHexgridMeshKey)layerKey )
				{
					// If this layer exists already, copy it from the previous list
					newLayers[layerKey] = _hexgridLayerList[i];
					bCopiedExisting = true;
					break; // Only keep the first one to avoid duplicates
				}
			}

			if( !bCopiedExisting )
			{
				newLayers[layerKey] = new HexgridMeshDataTuple
				{
					_key = (EHexgridMeshKey)layerKey
				};
			}
		}
		
		_hexgridLayerList = newLayers;
		TryRecompileHexgridMeshes( false, true );

		if( !ValidateData() )
		{
			Debug.LogWarning( "Validation failed" );
		}
		
#endif
	}

	[Button( "Recompile hexgrid meshes" )]
	public void Editor_RecompileHexgridMeshes()
	{
#if UNITY_EDITOR
		Undo.RecordObject(this, "Recompiled hexgrid meshes" );
#endif
		TryRecompileHexgridMeshes( false, true );
	}
	
	[Button( "Force recompile hexgrid meshes" )]
	public void Editor_ForceRecompileHexgridMeshes()
	{
#if UNITY_EDITOR
		Undo.RecordObject( this, "Force Recompile hexgrid meshes" );
#endif
		TryRecompileHexgridMeshes( true, true );
	}

	private void TryRecompileHexgridMeshes( bool bForce, bool bWarnIfMissing )
	{
		for( int i = 0; i < _hexgridLayerList.Length; ++i )
		{
			if( _hexgridLayerList[i]._meshData != null )
			{
				_hexgridLayerList[i].RefreshMeshData( bForce );
			}

			if( bWarnIfMissing )
			{
				if( !_hexgridLayerList[i]._meshAsset )
				{
					Debug.LogWarningFormat( "Hexgrid data [{0}] has not been assigned a mesh",
						_hexgridLayerList[i]._key.ToString() );
				}
				else if( _hexgridLayerList[i]._meshData == null || !_hexgridLayerList[i]._meshData._bInitialised )
				{
					Debug.LogWarningFormat( "Hexgrid data [{0}] has not successfully compiled the hexgrid mesh",
						_hexgridLayerList[i]._key.ToString() );
				}
			}
		}
	}

	public bool ValidateData()
	{
		bool bValid = _hexgridLayerList.Length == (int)EHexgridMeshKey.COUNT;

		if( !bValid )
		{
			return false;
		}
		
		for( int i = 0; i < _hexgridLayerList.Length; ++i )
		{
			bValid &= _hexgridLayerList[i]._meshAsset &&
			          _hexgridLayerList[i]._meshData != null &&
			          _hexgridLayerList[i]._meshData._bInitialised &&
			          _hexgridLayerList[i]._meshData._mesh == _hexgridLayerList[i]._meshAsset;
		}
		
		return bValid;
	}

	public bool TryGetMeshDataFromKey( EHexgridMeshKey inKey, out HexgridMeshData outLayerData )
	{
		LazyInitRuntimeMeshData();
		return _hexgridMeshDataByKey.TryGetValue( inKey, out outLayerData );
	}
	
	public bool TryGetMeshDataFromKey( Mesh inSharedMesh, out HexgridMeshData outLayerData )
	{
		LazyInitRuntimeMeshData();
		return _hexgridMeshDataBySharedMesh.TryGetValue( inSharedMesh, out outLayerData );
	}

	public void LazyInitRuntimeMeshData()
	{
		if( _hexgridMeshDataByKey == null || _hexgridMeshDataBySharedMesh == null )
		{
			// De-serialize to a dictionary (for the game jam, let's do it as a lazy init at runtime)
			Debug.LogFormat( "Initialising {0} runtime mesh dictionary entries", _hexgridLayerList.Length );
			_hexgridMeshDataByKey = new Dictionary<EHexgridMeshKey, HexgridMeshData>();
			_hexgridMeshDataBySharedMesh = new Dictionary<Mesh, HexgridMeshData>();
			for( int i = 0; i < _hexgridLayerList.Length; ++i )
			{
				if( !(_hexgridMeshDataByKey.TryAdd(
					      _hexgridLayerList[i]._key,
					      _hexgridLayerList[i]._meshData ) &&
				      _hexgridMeshDataBySharedMesh.TryAdd(
					      _hexgridLayerList[i]._meshAsset,
					      _hexgridLayerList[i]._meshData )) )
				{
					Debug.LogErrorFormat( "Can't deserialise hexgrid layer mesh data for: {0}",
						_hexgridLayerList[i]._key.ToString() );
				}
			}
		}
	}

}
