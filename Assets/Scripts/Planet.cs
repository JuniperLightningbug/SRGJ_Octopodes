using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

public class Planet : MonoBehaviour
{
	[Header("Planet config")]
	[SerializeField, Expandable] private SO_PlanetData _planetData;

	[Header( "Prefab hierarchy references" )]
	[SerializeField] private MeshFilter _auroraMeshFilter;
	[SerializeField] private MeshFilter _radarMeshFilter;
	[SerializeField] private MeshFilter _magnetoMeshFilter;
	[SerializeField] private MeshFilter _plasmaMeshFilter;
	[SerializeField] private MeshFilter _massSpecMeshFilter;
	[SerializeField] private MeshFilter _planetMeshRenderer;
	[SerializeField] private Transform _rotationTransform; // TODO do we ever rotate around something other than y? Are the poles ever not the axis?

	[Header( "Global data references" )]
	[SerializeField] private SO_PlanetLayerMeshes _layerMeshesCache;
	
	[Header("Runtime instance data")]
	[SerializeField, ReadOnly, AllowNesting] private List<PlanetLayerInstance> _planetLayerInstances = new List<PlanetLayerInstance>();
	private Dictionary<SO_PlanetData.ESensorType, int> _planetLayerInstanceIdxMap = new Dictionary<SO_PlanetData.ESensorType, int>(); // Synced with _planetLayerInstances
	
	private Dictionary<SO_PlanetData.ESensorType, MeshFilter> _meshFilterMap = new Dictionary<SO_PlanetData.ESensorType, MeshFilter>();

	public List<Transform> _debugTrackTransforms = new List<Transform>();

#region Interface
	public void InitialisePlanet( SO_PlanetData planetData )
	{
		if( !Application.isPlaying )
		{
			// We'll be creating a mesh instance - don't do this in edit mode
			return;
		}
		
		_planetData = planetData;
		InitialiseInternal();
	}
	
	public void InitialisePlanet()
	{
		if( !Application.isPlaying )
		{
			// We'll be creating a mesh instance - don't do this in edit mode
			return;
		}
		
		InitialiseInternal();
	}

#endregion

#region Edit-Time Initialisations

	[Tooltip("Includes: Layer Meshes, Layer Materials, Planet Material, etc. (TODO)")]
	[Button("Save prefab data to SO (edit-time)")]
	private void Editor_SavePrefabMeshesToPlanetData()
	{
		// This is intended for edit-time only! Optional data pipeline from the prefab rather than SO.
		// Loads the current meshes from the prefab hierarchy into the attached scriptable object.
		// Will likely fail if attempted at runtime because the meshes will be instanced.
		
		if( !_planetData || !_layerMeshesCache )
		{
			return;
		}
		
		InitialiseLayerInspectorMap();

		_planetData._planetLayers.Clear();
		foreach( KeyValuePair<SO_PlanetData.ESensorType, MeshFilter> tuple in _meshFilterMap )
		{
			if( tuple.Value.sharedMesh != null )
			{
				MeshRenderer meshRenderer = tuple.Value.GetComponent<MeshRenderer>();
				_planetData._planetLayers.Add( new SO_PlanetData.PlanetLayerTuple()
				{
					_sensorType = tuple.Key,
					_mesh = tuple.Value.sharedMesh,
					_material = meshRenderer ? meshRenderer.sharedMaterial : null,
				} );
			}
		}

#if UNITY_EDITOR
		EditorUtility.SetDirty( _planetData );
#endif
	}

#endregion

#region Runtime Initialisations

	[Button("Initialise (runtime)")]
	private void Editor_InitialisePlanetRuntime()
	{
#if UNITY_EDITOR
		Undo.RecordObject( this, "Initialise planet" );
#endif
		InitialisePlanet();
	}

	private void InitialiseLayerInspectorMap()
	{
		_meshFilterMap = new Dictionary<SO_PlanetData.ESensorType, MeshFilter>();
		if( _auroraMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetData.ESensorType.Aurora, _auroraMeshFilter );
		}
		if( _radarMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetData.ESensorType.Radar, _radarMeshFilter );
		}
		if( _magnetoMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetData.ESensorType.Magneto, _magnetoMeshFilter );
		}
		if( _plasmaMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetData.ESensorType.Plasma, _plasmaMeshFilter );
		}
		if( _massSpecMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetData.ESensorType.MassSpec, _massSpecMeshFilter );
		}
	}

	private void InitialiseInternal()
	{
		if( !_planetData || !_layerMeshesCache )
		{
			return;
		}

		InitialiseLayerInspectorMap();

		_planetLayerInstances.Clear();
		for( int i = 0; i < _planetData._planetLayers.Count; ++i )
		{
			if( _meshFilterMap.TryGetValue(
				   _planetData._planetLayers[i]._sensorType,
				   out MeshFilter meshFilter ) )
			{
				Mesh meshAsset = _planetData._planetLayers[i]._mesh;
				Mesh meshInstance = Instantiate<Mesh>( meshAsset );
				meshInstance.MarkDynamic();
				
				TryInitialiseLayer(
					_planetData._planetLayers[i]._sensorType,
					meshFilter,
					meshAsset,
					meshInstance,
					_planetData._planetLayers[i]._material
				);
			}
		}
		
		// TODO start listening for satellites?
	}

	private bool TryInitialiseLayer(
		SO_PlanetData.ESensorType type,
		MeshFilter meshFilter,
		Mesh meshAsset,
		Mesh meshInstance,
		Material material /*, etc*/ )
	{
		Debug.Log( meshAsset.name + " " + meshAsset.GetInstanceID() );
		if( !_layerMeshesCache.TryGetMeshData( meshAsset, out HexgridMeshData hexgridMeshData ) )
		{
			Debug.LogErrorFormat( "Can't find {0} hexgrid mesh data for {1}!",
				type.ToString(),
				meshAsset.name );
			return false;
		}

		meshFilter.mesh = meshInstance;
		
		// Try to assign material instance
		MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
		if( material && meshRenderer )
		{
			meshRenderer.sharedMaterial = material;
		}

		PlanetLayerInstance newLayer = new PlanetLayerInstance()
		{
			_meshInstance = meshInstance, // The runtime copy
			_meshData = hexgridMeshData,
			_transform = meshFilter.transform,
			// TODO the rest here
		};

		if( _planetLayerInstanceIdxMap.TryAdd( type, _planetLayerInstances.Count ) )
		{
			_planetLayerInstances.Add( newLayer );
			newLayer.Initialise();
			return true;
		}
		return false;
	}
	
#endregion

#region MonoBehaviour

	void Update()
	{
		// DEBUG TODO
		Vector3[] satellites = new Vector3[_debugTrackTransforms.Count];
		for( int i = 0; i < _debugTrackTransforms.Count; ++i )
		{
			satellites[i] = _debugTrackTransforms[i].position;
		}
		
		for( int i = 0; i < _planetLayerInstances.Count; ++i )
		{
			if( satellites.Length > 0 )
			{
				_planetLayerInstances[i].UpdateDiscoveryFromSatellitePositions( satellites );
			}

			_planetLayerInstances[i].UpdateFadeOut( Time.deltaTime );
			
			//todo: only need to do this for the active one
			_planetLayerInstances[i].RefreshMeshColours();
		}
	}
	
	void OnDestroy()
	{
		for( int i = 0; i < _planetLayerInstances.Count; ++i )
		{
			if( _planetLayerInstances[i]._meshInstance != null )
			{
				// Clean up mesh instances created - this is not done automatically
				Destroy( _planetLayerInstances[i]._meshInstance );
			}
		}
	}

#endregion
}
