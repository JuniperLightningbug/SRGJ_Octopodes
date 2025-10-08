using System;
using System.Collections.Generic;
using MM;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

public class Planet : MonoBehaviour
{
	[Header( "Planet config" )]
	[SerializeField, Expandable] private SO_PlanetConfig _planetConfig;

	[Header( "Prefab hierarchy references" )]
	[SerializeField] private MeshFilter _auroraMeshFilter;

	[SerializeField] private MeshFilter _radarMeshFilter;
	[SerializeField] private MeshFilter _magnetoMeshFilter;
	[SerializeField] private MeshFilter _plasmaMeshFilter;
	[SerializeField] private MeshFilter _massSpecMeshFilter;
	[SerializeField] private MeshFilter _planetMeshFilter;
	[SerializeField] private MeshRenderer _planetMeshRenderer;

	[SerializeField]
	private Transform _rotationTransform;

	[Header( "Global data references" )]
	[SerializeField] private SO_CompiledPlanetLayerMeshes _layerMeshesCache;

	[Header( "Runtime instance data" )]
	[ShowNonSerializedField, NonSerialized]
	public bool _bInitialised = false;

	[SerializeField, AllowNesting]
	private List<PlanetLayerInstance> _planetLayerInstances = new List<PlanetLayerInstance>();
	private Dictionary<SO_PlanetConfig.ESensorType, int> _planetLayerInstanceIdxMap =
		new Dictionary<SO_PlanetConfig.ESensorType, int>(); // Synced with _planetLayerInstances

	// This mirrors data in the GameManager if it's present
	// Caching here means we aren't dependent on the GameManager for testing
	[OnValueChanged( "DebugChangedCurrentSensorType" )]
	public SO_PlanetConfig.ESensorType _currentSensorType = SO_PlanetConfig.ESensorType.INVALID;
	
	private float RotationSpeed =>
		!_bInitialised || _planetConfig._rotationTime <= 0.0f ||
		Mathf.Approximately( _planetConfig._rotationTime, 0.0f ) ?
			0.0f :
			1.0f / _planetConfig._rotationTime;

#region Debug
	
	public Transform _debugSatelliteTransform;

	[Button( "Add debug satellite" )]
	private void AddDebugSatellite()
	{
		StartTrackingSatellite( _debugSatelliteTransform );
	}
	
	public void DebugChangedCurrentSensorType()
	{
		OnUpdateSensorType();
	}

#endregion

#region Interface

	public bool GetActiveLayerInstance( out PlanetLayerInstance outInstance )
	{
		if( _planetLayerInstanceIdxMap.TryGetValue( _currentSensorType, out int idx ) )
		{
			outInstance = _planetLayerInstances[idx];
			return outInstance != null;
		}

		outInstance = null;
		return false;
	}
	


	public void InitialisePlanet( SO_PlanetConfig planetConfig )
	{
		_planetConfig = planetConfig;
		InitialiseInternal_Runtime();
	}

	public void InitialisePlanet()
	{
		InitialiseInternal_Runtime();
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
				_planetLayerInstances[idx].RefreshMeshColours();
				_planetLayerInstances[idx].ToggleView( bOn );
			}
		}
	}

	public void StartTrackingSatellite( Transform satellite )
	{
		StartTrackingSatellite( _currentSensorType, satellite );
	}
	public void StartTrackingSatellite( SO_PlanetConfig.ESensorType type, Transform satellite )
	{
		if( _bInitialised && GetActiveLayerInstance( out PlanetLayerInstance activeInstance ) )
		{
			activeInstance.StartTrackingSatellite( satellite );
		}
	}
	
	public void StopTrackingSatellite( Transform satellite )
	{
		StartTrackingSatellite( _currentSensorType, satellite );
	}
	public void StopTrackingSatellite( SO_PlanetConfig.ESensorType type, Transform satellite )
	{
		if( _bInitialised && GetActiveLayerInstance( out PlanetLayerInstance activeInstance ) )
		{
			activeInstance.StopTrackingSatellite( satellite );
		}
	}

#endregion

#region Editor Initialisations

	[Tooltip( "Includes: Layer Meshes, Layer Materials, Planet Material, etc. (TODO)" )]
	[Button( "Save prefab data to SO (edit-time)" )]
	private void Editor_SavePrefabMeshesToPlanetData()
	{
		// This is intended for edit-time only! Optional data pipeline from the prefab rather than SO.
		// Loads the current meshes from the prefab hierarchy into the attached scriptable object.
		// Will likely fail if attempted at runtime because the meshes will be instanced.

		if( !_planetConfig || !_layerMeshesCache )
		{
			return;
		}
		
		_planetConfig._planetLayers.Clear();

		for( int i = 0; i < (int)SO_PlanetConfig.ESensorType.COUNT; ++i )
		{
			MeshFilter meshFilter = GetPrefabAssignedMeshFilter( (SO_PlanetConfig.ESensorType)i );
			if( meshFilter?.sharedMesh )
			{
				MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
				_planetConfig._planetLayers.Add( new SO_PlanetConfig.PlanetLayerTuple()
				{
					_sensorType = (SO_PlanetConfig.ESensorType)i,
					_mesh = meshFilter.sharedMesh,
					_material = meshRenderer ? meshRenderer.sharedMaterial : null,
				} );
			}
		}

#if UNITY_EDITOR
		EditorUtility.SetDirty( _planetConfig );
#endif
	}
	
	[Button( "Initialise" )]
	private void Editor_InitialisePlanet()
	{
#if UNITY_EDITOR
		Undo.RecordObject( this, "Initialise planet" );
		if( !Application.isPlaying )
		{
			InitialiseInternal_EditTime();
		}
		else
		{
			InitialiseInternal_Runtime();
		}
#endif
	}

#endregion

#region Runtime Initialisations

	/**
	 * Loads data from scriptable object SO_PlanetConfig for working in edit time
	 */
	private void InitialiseInternal_EditTime()
	{
		if( !_planetConfig || !_layerMeshesCache )
		{
			return;
		}

		InitialiseConfigComponentProperties();
	}
	
	/**
	 * Loads data from scriptable object SO_PlanetConfig, and instances runtime data
	 */
	private void InitialiseInternal_Runtime()
	{
		if( !_planetConfig || !_layerMeshesCache || _bInitialised )
		{
			return;
		}
		
		InitialiseConfigComponentProperties();

		_planetLayerInstances.Clear();
		_planetLayerInstanceIdxMap.Clear();
		for( int i = 0; i < _planetConfig._planetLayers.Count; ++i )
		{
			PlanetLayerInstance newLayer = TryMakeRuntimeLayerInstance( _planetConfig._planetLayers[i] );
			if( newLayer != null &&
			    _planetLayerInstanceIdxMap.TryAdd( _planetConfig._planetLayers[i]._sensorType, _planetLayerInstances.Count ))
			{
				_planetLayerInstances.Add( newLayer );
				newLayer.Initialise();
			}
		}
		
		OnUpdateSensorType();
		
		_bInitialised = true;
	}

	private MeshFilter GetPrefabAssignedMeshFilter( SO_PlanetConfig.ESensorType type )
	{
		switch( type )
		{
			case SO_PlanetConfig.ESensorType.Aurora:
				return _auroraMeshFilter;
			case SO_PlanetConfig.ESensorType.Radar:
				return _radarMeshFilter;
			case SO_PlanetConfig.ESensorType.Magneto:
				return _magnetoMeshFilter;
			case SO_PlanetConfig.ESensorType.Plasma:
				return _plasmaMeshFilter;
			case SO_PlanetConfig.ESensorType.MassSpec:
				return _massSpecMeshFilter;
			default:
				return null;
		}
	}
	private bool TryGetPrefabAssignedMeshFilter( SO_PlanetConfig.ESensorType type, out MeshFilter outMeshFilter )
	{
		outMeshFilter = GetPrefabAssignedMeshFilter( type );
		return outMeshFilter != null;
	}

	private void InitialiseConfigComponentProperties()
	{
		// Prefab assignments from the planet config SO
		// Valid at edit time or runtime

		for( int i = 0; i < _planetConfig._planetLayers.Count; ++i )
		{
			if( _planetConfig._planetLayers[i]._bActive )
			{
				if( TryGetPrefabAssignedMeshFilter(_planetConfig._planetLayers[i]._sensorType, out MeshFilter meshFilter) )
				{
					meshFilter.sharedMesh = _planetConfig._planetLayers[i]._mesh;
					MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
					if( _planetConfig._planetLayers[i]._material && meshRenderer )
					{
						meshRenderer.sharedMaterial = _planetConfig._planetLayers[i]._material;
					}
				}
			}
		}

		if( _planetMeshFilter && _planetConfig._planetMesh )
		{
			_planetMeshFilter.sharedMesh = _planetConfig._planetMesh;
		}

		if( _planetMeshRenderer && _planetConfig._planetMaterial )
		{
			_planetMeshRenderer.sharedMaterial = _planetConfig._planetMaterial;
		}
	}

	private PlanetLayerInstance TryMakeRuntimeLayerInstance( SO_PlanetConfig.PlanetLayerTuple layerConfig )
	{
		if( !_layerMeshesCache.TryGetMeshData( layerConfig._mesh, out HexgridMeshData hexgridMeshData ) )
		{
			Debug.LogErrorFormat( "Can't find {0} hexgrid mesh data for {1}!",
				layerConfig._sensorType.ToString(),
				layerConfig._mesh.name );
			return null;
		}

		if( layerConfig._bActive &&
		    TryGetPrefabAssignedMeshFilter( layerConfig._sensorType, out MeshFilter meshFilter ) )
		{
			// Instance the mesh - we'll be modifying the vertex colours
			Mesh meshInstance = meshFilter.mesh;
			meshInstance.MarkDynamic();

			// TODO: Do we need to instance the material?

			PlanetLayerInstance newLayer = new PlanetLayerInstance()
			{
				_meshInstance = meshInstance, // The runtime copy
				_meshData = hexgridMeshData,
				_transform = meshFilter.transform,
				_sensorType = layerConfig._sensorType,
				_bHasTexture = layerConfig._bHasTexture,
			};
			return newLayer;

		}

		return null;
	}

#endregion

#region Runtime Behaviour

	private void OnUpdateSensorType()
	{
		// We don't know the previous value here - turn everything off
		for( int i = 0; i < (int)SO_PlanetConfig.ESensorType.COUNT; ++i )
		{
			ToggleSensorView( (SO_PlanetConfig.ESensorType)i, false );
		}

		// Turn the right one back on
		ToggleSensorView( _currentSensorType, true );
	}

	private void UpdateSatelliteDiscovery( float deltaTime )
	{
		for( int i = 0; i < _planetLayerInstances.Count; ++i )
		{
			float layerDiscovery = _planetLayerInstances[i].UpdateDiscovery( Time.deltaTime );
			(SO_PlanetConfig.ESensorType type, float newValue) eventPackage =
				new ValueTuple<SO_PlanetConfig.ESensorType, float>(
					_planetConfig._planetLayers[i]._sensorType,
					layerDiscovery );
			EventBus.Invoke( this, EventBus.EEventType.OnChanged_LayerDiscovery, eventPackage );
		}
	}

	private void UpdateActiveLayerVisuals()
	{
		// Refresh the visuals on the active layer
		if( GetActiveLayerInstance( out PlanetLayerInstance activeInstance ) )
		{
#if UNITY_EDITOR
			if( DEBUG_Globals.ActiveProfile._bShowPlanetLayerFaces )
			{
				activeInstance.DebugShowFaces();
			}
			else if( DEBUG_Globals.ActiveProfile._bRemovePlanetLayerFOW )
			{
				activeInstance.RefreshMeshColours( true );
			}
			else
			{
#endif
				// Release version behaviour
				activeInstance.RefreshMeshColours( false );
				// End release version behaviour
#if UNITY_EDITOR
			}
#endif
		}
	}

	private void UpdateRotatePlanet( float deltaTime )
	{
		if( _rotationTransform )
		{
			_rotationTransform.Rotate( Vector3.up, RotationSpeed );
		}
	}

#endregion

#region Callbacks

	private void OnGlobalEvent_ActiveSensorTypeChanged( EventBus.EventContext context, object obj = null )
	{
		if( obj != null )
		{
			_currentSensorType = (SO_PlanetConfig.ESensorType)obj;
			OnUpdateSensorType();
		}
	}

	private void OnGlobalEvent_LaunchedSatellite( EventBus.EventContext context, object obj = null )
	{
		if( obj != null )
		{
			(SO_PlanetConfig.ESensorType type, Transform satelliteTransform) newSatellite =
				(ValueTuple<SO_PlanetConfig.ESensorType, Transform>)obj;
			StartTrackingSatellite( newSatellite.type, newSatellite.satelliteTransform );
		}
	}

#endregion

#region MonoBehaviour

	void OnEnable()
	{
		EventBus.StartListening( EventBus.EEventType.ActiveSensorTypeChanged, OnGlobalEvent_ActiveSensorTypeChanged );
		EventBus.StartListening( EventBus.EEventType.LaunchedSatellite, OnGlobalEvent_LaunchedSatellite );
	}

	void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.ActiveSensorTypeChanged, OnGlobalEvent_ActiveSensorTypeChanged );
		EventBus.StopListening( EventBus.EEventType.LaunchedSatellite, OnGlobalEvent_LaunchedSatellite );
	}

	void Update()
	{
		if( !_bInitialised )
		{
			return;
		}
		
		float deltaTime = Time.deltaTime;
		
		UpdateSatelliteDiscovery( deltaTime );
		UpdateActiveLayerVisuals();
		UpdateRotatePlanet( deltaTime );
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

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		if( DEBUG_Globals.ActiveProfile._bShowPlanetLayerFaceNormals &&
		    GetActiveLayerInstance( out PlanetLayerInstance activeInstance ) )
		{
			activeInstance.DebugDrawMeshDataGizmos();
		}
	}
#endif

#endregion
}