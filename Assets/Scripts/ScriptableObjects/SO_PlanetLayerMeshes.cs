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
	[System.Serializable]
	public class HexgridMeshDataTuple
	{
		// Keys
		[SerializeField] public Mesh _mesh;

		// Values
		[SerializeField, ReadOnly, AllowNesting] public HexgridMeshData _hexgridMeshData;

		public void RefreshMeshData( bool bForce = false )
		{
			if( _hexgridMeshData == null || bForce )
			{
				_hexgridMeshData = new HexgridMeshData();
			}
			_hexgridMeshData.InitialiseFromMesh( _mesh );
		}
	}
	
	// Edit-time info (serialisable list with manually enforced uniqueness of 'HexgridLayerData._key' key)
	[SerializeField]
	public HexgridMeshDataTuple[] _hexgridLayerList = Array.Empty<HexgridMeshDataTuple>();
	
	// Runtime info (lazily init this on poll)
	private Dictionary<string, HexgridMeshData> _hexgridMeshDataBySharedMeshName;

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
			if( _hexgridLayerList[i]._hexgridMeshData != null )
			{
				_hexgridLayerList[i].RefreshMeshData( bForce );
				
				// TEMP TODO
				Debug.LogWarningFormat( "{0} : {1}", _hexgridLayerList[i]._mesh.GetInstanceID().ToString(),
					_hexgridLayerList[i]._mesh.name );
			}

			if( bWarnIfMissing )
			{
				if( !_hexgridLayerList[i]._mesh )
				{
					Debug.LogWarningFormat( "Hexgrid data at [{0}] has not been assigned a mesh", i );
				}
				else if( _hexgridLayerList[i]._hexgridMeshData == null || !_hexgridLayerList[i]._hexgridMeshData._bInitialised )
				{
					Debug.LogWarningFormat( "Hexgrid data [{0}] has not successfully compiled the hexgrid mesh",
						_hexgridLayerList[i]._mesh.name );
				}
			}
		}
	}
	
	public bool TryGetMeshData( Mesh inSharedMesh, out HexgridMeshData outLayerData )
	{
		return TryGetMeshData( inSharedMesh.name, out outLayerData );
	}
	
	public bool TryGetMeshData( string inSharedMeshName, out HexgridMeshData outLayerData )
	{
		LazyInitRuntimeMeshData();
		return _hexgridMeshDataBySharedMeshName.TryGetValue( inSharedMeshName, out outLayerData );
	}

	public void LazyInitRuntimeMeshData()
	{
		if( _hexgridMeshDataBySharedMeshName == null )
		{
			// De-serialize to a dictionary (for the game jam, let's do it as a lazy init at runtime)
			Debug.LogFormat( "Initialising {0} runtime mesh dictionary entries", _hexgridLayerList.Length );
			_hexgridMeshDataBySharedMeshName = new Dictionary<string, HexgridMeshData>();
			for( int i = 0; i < _hexgridLayerList.Length; ++i )
			{
				if( !_hexgridMeshDataBySharedMeshName.TryAdd(
					      _hexgridLayerList[i]._mesh.name,
					      _hexgridLayerList[i]._hexgridMeshData ) )
				{
					Debug.LogErrorFormat( "Can't deserialise hexgrid layer mesh data for: {0}",
						_hexgridLayerList[i]._mesh.name );
				}
			}
		}
	}

}
