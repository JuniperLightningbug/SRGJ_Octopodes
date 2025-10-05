using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Planet : MonoBehaviour
{
	[Header("Planet config")]
	[SerializeField, Expandable] private SO_PlanetConfig _planetConfig;

	[Header( "Prefab hierarchy references" )]
	[SerializeField] private MeshFilter _auroraMeshFilter;
	[SerializeField] private MeshFilter _radarMeshFilter;
	[SerializeField] private MeshFilter _magnetoMeshFilter;
	[SerializeField] private MeshFilter _plasmaMeshFilter;
	[SerializeField] private MeshFilter _massSpecMeshFilter;
	[SerializeField] private MeshFilter _planetMeshRenderer;
	[SerializeField] private Transform _rotationTransform; // TODO do we ever rotate around something other than y? Are the poles ever not the axis?

	[Header( "Global data references" )]
	[SerializeField] private SO_CompiledPlanetLayerMeshes _layerMeshesCache;

	[Header( "Runtime instance data" )]
	[ShowNonSerializedField, NonSerialized]
	public bool _bInitialised = false;
	[SerializeField, ReadOnly, AllowNesting] private List<PlanetLayerInstance> _planetLayerInstances = new List<PlanetLayerInstance>();
	private Dictionary<SO_PlanetConfig.ESensorType, int> _planetLayerInstanceIdxMap = new Dictionary<SO_PlanetConfig.ESensorType, int>(); // Synced with _planetLayerInstances
	private Dictionary<SO_PlanetConfig.ESensorType, MeshFilter> _meshFilterMap = new Dictionary<SO_PlanetConfig.ESensorType, MeshFilter>();
	
	private float RotationSpeed =>
		!_bInitialised || _planetConfig._rotationTime <= 0.0f || Mathf.Approximately( _planetConfig._rotationTime, 0.0f ) ?
		0.0f :
		1.0f / _planetConfig._rotationTime;
	
	
	public List<Transform> _debugTrackTransforms = new List<Transform>();
	[OnValueChanged("DebugChangedCurrentSensorType")]
	public SO_PlanetConfig.ESensorType _currentSensorType = SO_PlanetConfig.ESensorType.INVALID;

#region Debug

	public void DebugChangedCurrentSensorType()
	{
		// Value changed in the inspector - simulate the switch
		TurnOffAllSensorViews(); // We don't know the previous value here - turn everything off
		
		// Turn the right one back on
		ToggleSensorView( _currentSensorType, true );
	}

	private void TurnOffAllSensorViews()
	{
		for( int i = 0; i < (int)SO_PlanetConfig.ESensorType.COUNT; ++i )
		{
			ToggleSensorView( (SO_PlanetConfig.ESensorType)i, false );
		}
	}

#endregion
	
#region Interface
	public void InitialisePlanet( SO_PlanetConfig planetConfig )
	{
		if( !Application.isPlaying )
		{
			// We'll be creating a mesh instance - don't do this in edit mode
			return;
		}
		
		_planetConfig = planetConfig;
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

	public void ChangeSensorType( SO_PlanetConfig.ESensorType newSensorType )
	{
		if( _currentSensorType != newSensorType )
		{
			ToggleSensorView( _currentSensorType, false );
			_currentSensorType = newSensorType;
			ToggleSensorView( _currentSensorType, true );
		}
	}
	
	private void ToggleSensorView( SO_PlanetConfig.ESensorType type, bool bOn )
	{
		if( _bInitialised && _planetLayerInstanceIdxMap.TryGetValue( type, out int idx ) )
		{
			if( _planetLayerInstances[idx] != null )
			{
				_planetLayerInstances[idx].ToggleView( bOn );
			}
		}
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
		
		if( !_planetConfig || !_layerMeshesCache )
		{
			return;
		}
		
		InitialiseLayerInspectorMap();

		_planetConfig._planetLayers.Clear();
		foreach( KeyValuePair<SO_PlanetConfig.ESensorType, MeshFilter> tuple in _meshFilterMap )
		{
			if( tuple.Value.sharedMesh != null )
			{
				MeshRenderer meshRenderer = tuple.Value.GetComponent<MeshRenderer>();
				_planetConfig._planetLayers.Add( new SO_PlanetConfig.PlanetLayerTuple()
				{
					_sensorType = tuple.Key,
					_mesh = tuple.Value.sharedMesh,
					_material = meshRenderer ? meshRenderer.sharedMaterial : null,
				} );
			}
		}

#if UNITY_EDITOR
		EditorUtility.SetDirty( _planetConfig );
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
		_meshFilterMap = new Dictionary<SO_PlanetConfig.ESensorType, MeshFilter>();
		if( _auroraMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetConfig.ESensorType.Aurora, _auroraMeshFilter );
		}
		if( _radarMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetConfig.ESensorType.Radar, _radarMeshFilter );
		}
		if( _magnetoMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetConfig.ESensorType.Magneto, _magnetoMeshFilter );
		}
		if( _plasmaMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetConfig.ESensorType.Plasma, _plasmaMeshFilter );
		}
		if( _massSpecMeshFilter )
		{
			_meshFilterMap.Add( SO_PlanetConfig.ESensorType.MassSpec, _massSpecMeshFilter );
		}
	}

	private void InitialiseInternal()
	{
		if( !_planetConfig || !_layerMeshesCache || _bInitialised )
		{
			return;
		}

		InitialiseLayerInspectorMap();

		_planetLayerInstances.Clear();
		for( int i = 0; i < _planetConfig._planetLayers.Count; ++i )
		{
			if( _meshFilterMap.TryGetValue(
				   _planetConfig._planetLayers[i]._sensorType,
				   out MeshFilter meshFilter ) )
			{
				Mesh meshAsset = _planetConfig._planetLayers[i]._mesh;
				Mesh meshInstance = Instantiate<Mesh>( meshAsset );
				meshInstance.MarkDynamic();
				
				TryInitialiseLayer(
					_planetConfig._planetLayers[i]._sensorType,
					meshFilter,
					meshAsset,
					meshInstance,
					_planetConfig._planetLayers[i]._material
				);
			}
		}

		TurnOffAllSensorViews();
		
		// TODO start listening for satellites?

		_bInitialised = true;
	}

	private bool TryInitialiseLayer(
		SO_PlanetConfig.ESensorType type,
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
		if( !_bInitialised )
		{
			return;
		}
		
		// DEBUG TODO - update layers with satellite data
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
		
		// Rotate planet if necessary
		if( _rotationTransform )
		{
			_rotationTransform.Rotate( Vector3.up, RotationSpeed );
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
