using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

/**
 * Object representing a satellite orbital
 * Orbits are all represented as circles (in this project, for the time being) with consistent periods
 * Multiple satellites can share the same orbit, with different time offsets
 */
public class SatelliteOrbit : MonoBehaviour
{
	/*
	 * A note on intended transform hierarchy:
	 *
	 * -> Root (with attached script): world space, moves to planet position but does not rotate
	 * |-> _orbitalTransformRoot: Rotates to the correct direction
	 * |--> _orbitalProjectionCircleVisuals: Scales to the projection radius
	 * |--> _orbitalOuterCircleVisuals: Scales to the orbit radius (TODO: We should change the circle renderer radius instead)
	 * |--> _satelliteRootTransform: Scales to the orbit radius and rotates over time
	 * |---> [n] spawned satellites
	 *
	 * We're using the root's Y-up for the orbitals (i.e. the visualisation and inputs are for XZ, the axis is Y)
	 */
	
	// Rotate the orbital circle to the input direction
	[SerializeField] private Transform _orbitalTransformRoot;
	
	// Transforms to apply a _scale_ to in order to create the desired radius (e.g. a circle from a line renderer)
	[SerializeField] private Transform _orbitalProjectionCircleVisuals;
	[SerializeField] private Transform _orbitalOuterCircleVisuals;
	
	// Transform parenting all the satellites. Since we're rotating them at the same rate, we can do it once.
	// But we don't necessarily want to rotate the whole object (e.g. the line renderer, other visuals)
	[SerializeField] private Transform _satelliteTransformRoot;
	
	// This will be moved to the projection radius (without being scaled)
	[SerializeField] private Transform _satelliteLaunchProjectionIndicator;
		
	// This will be moved to the orbit radius (without being scaled)
	[SerializeField] private Transform _satelliteLaunchPositionIndicator;
	
	[SerializeField] public Satellite3D _satelliteComponent;
	[SerializeField] public Transform _satelliteTransform;

	[SerializeField, Tooltip("Set by instantiating manager")] private float _projectionRadius = 1.0f;
	[SerializeField, Tooltip("Set by instantiating manager")] private float _orbitRadius = 1.0f;
	[SerializeField, Tooltip("Set by instantiating manager (seconds per rotation)")] private float _orbitTime = 10.0f;

	[SerializeField] private SO_Satellite _satelliteData = null;

	private bool _bOrbitIsActive = false;
	private Vector3 _directionReference0 = Vector3.right;
	private Vector3 _directionReference1 = Vector3.forward;
	private Vector3 _cachedTransformUp = Vector3.up;
	
	[Header("Runtime Health Data")]
	[SerializeField]
	private bool _bIsAlive = true;
	public bool BIsAlive => _bIsAlive;
	[SerializeField] private bool _bIsInSafeMode = false;
	public bool BIsInSafeMode => _bIsInSafeMode;
	[SerializeField] private bool _bLayerIsSelected = false;
	[SerializeField] private bool _bLayerIsHovered = false;
	[SerializeField] private bool _bSatelliteIsHighlightedByCursor = false;
	
	
	[SerializeField, ReadOnly, Range( 0.0f, 1.0f )]
	private float _safeModeProgress = 0.0f;
	[SerializeField, Range(0.0f,1.0f)]
	public float _currentHealth = 1.0f;

	public void Initialise(
		SO_Satellite satelliteData,
		Vector3 centre,
		Quaternion rotation,
		float projectionRadius,
		float orbitRadius,
		float orbitTime,
		Vector3 startReferencePosition )
	{
		_satelliteData = satelliteData;
		transform.position = centre;
		transform.rotation = rotation;
		transform.localScale = Vector3.one;
		_cachedTransformUp = transform.up;
		_projectionRadius = projectionRadius;
		_orbitRadius = orbitRadius;
		
		if( _orbitalProjectionCircleVisuals )
		{
			_orbitalProjectionCircleVisuals.localScale = Vector3.one * _projectionRadius;
		}
		if( _orbitalOuterCircleVisuals )
		{
			_orbitalOuterCircleVisuals.localScale = Vector3.one * _orbitRadius;
		}
		_orbitTime = orbitTime;

		// Move the launch projection visual to the projection radius without scaling it 
		if( _satelliteLaunchProjectionIndicator )
		{
			_satelliteLaunchProjectionIndicator.localPosition = new Vector3(
				_satelliteLaunchProjectionIndicator.localPosition.x,
				_satelliteLaunchProjectionIndicator.localPosition.y,
				_projectionRadius );
			_satelliteLaunchProjectionIndicator.gameObject.SetActive( true );
		}
		
		// Move the launch position visual to the orbit radius without scaling it 
		if( _satelliteLaunchPositionIndicator )
		{
			_satelliteLaunchPositionIndicator.localPosition = new Vector3(
				_satelliteLaunchPositionIndicator.localPosition.x,
				_satelliteLaunchPositionIndicator.localPosition.y,
				_orbitRadius );
			_satelliteLaunchPositionIndicator.gameObject.SetActive( true );
		}
				
		if( _satelliteComponent )
		{
			_satelliteComponent.Initialise( this, _satelliteData );
			UpdateSatelliteComponentOutline();
		}

		Vector3 startDirection = transform.InverseTransformPoint( startReferencePosition ).normalized;
		_directionReference0 = startDirection;
		_directionReference1 = startDirection;
		
		UpdateOrbitRotationToReferencePositions();
	}

	public void UpdateSecondReferencePosition( Vector3 pos1 )
	{
		_directionReference1 = transform.InverseTransformPoint( pos1 ).normalized;
		UpdateOrbitRotationToReferencePositions();
	}

	public void UpdateOrbitRotationToReferencePositions()
	{
		// Note: we're using Y-up for the orbitals (i.e. the visualisation and inputs are for XZ, the axis is Y)
		if( ApproximatelyEqualOrOpposed( _directionReference0, _directionReference1 ) )
		{
			// Flatten on first radial position instead - this will happen on first frame & can happen again later
			_directionReference1 = Vector3.Cross( _directionReference0, _cachedTransformUp ).normalized;
			// Obviously this won't work for a polar reference 0. TODO corner-case
		}
		
		// New up direction is the normal of the plane formed by (0,0,0), _directionReference0 and _directionReference1
		Vector3 upDirection = Vector3.Cross( _directionReference1, _directionReference0 ).normalized;
		
		//_orbitalTransformRoot.rotation = Quaternion.FromToRotation( _cachedTransformUp, upDirection );
		
		// Ideally, we keep a consistent forward pointing at the first reference point. That way, we can show the
		// satellite launch location in the visuals at local position 0,0,r.
		_orbitalTransformRoot.rotation = Quaternion.LookRotation( _directionReference0.normalized, upDirection );
	}
	
	// Extend MM.MathsUtils - TODO
	// Essentially it's "are these two normalised vectors parallel to each other"
	public static bool ApproximatelyEqualOrOpposed( Vector3 a, Vector3 b, float epsilon = MM.MathsUtils.kFloatEpsilon )
	{
		return MM.MathsUtils.Approximately( Mathf.Abs( a.x ), Mathf.Abs( b.x ), epsilon ) &&
		       MM.MathsUtils.Approximately( Mathf.Abs( a.y ), Mathf.Abs( b.y ), epsilon ) &&
		       MM.MathsUtils.Approximately( Mathf.Abs( a.z ), Mathf.Abs( b.z ), epsilon );
	}

	public bool LaunchSatellite()
	{
		if( !_bOrbitIsActive && _satelliteComponent )
		{
			if( !_satelliteTransform )
			{
				_satelliteTransform = _satelliteComponent.transform;
			}

			Transform satelliteRoot = _satelliteTransformRoot ? _satelliteTransformRoot : transform;

			if( _satelliteLaunchPositionIndicator )
			{
				_satelliteTransform.position = _satelliteLaunchPositionIndicator.position;
				_satelliteTransform.rotation = _satelliteLaunchPositionIndicator.rotation;
			}
			else
			{
				_satelliteTransform.localPosition =
					satelliteRoot.InverseTransformDirection( _directionReference0 ) * _orbitRadius;
			}

			_bOrbitIsActive = true;

			(SO_Satellite satelliteData, Satellite3D satellite) eventPackage = new(
				_satelliteData,
				_satelliteComponent );
			EventBus.Invoke( this, EventBus.EEventType.LaunchedSatellite, eventPackage );
			
			StartTrackingSatellite();

			if( _satelliteLaunchProjectionIndicator )
			{
				_satelliteLaunchProjectionIndicator.gameObject.SetActive( false );
			}

			if( _satelliteLaunchPositionIndicator )
			{
				_satelliteLaunchPositionIndicator.gameObject.SetActive( false );
			}

			_bLayerIsSelected = true; // This is always true at this point
			UpdateSatelliteComponentOutline();

			return _satelliteTransform;
		}


		return false;
	}

	private void StopTrackingSatellite()
	{
		if( _satelliteComponent )
		{
			(SO_Satellite satelliteData, Satellite3D satellite) eventPackage = new(
				_satelliteData,
				_satelliteComponent );
			EventBus.Invoke( this, EventBus.EEventType.StopTrackingSatellite, eventPackage );
		}
	}
	
	private void StartTrackingSatellite()
	{
		if( _satelliteComponent )
		{
			(SO_Satellite satelliteData, Satellite3D satellite) eventPackage = new(
				_satelliteData,
				_satelliteComponent );
			EventBus.Invoke( this, EventBus.EEventType.StartTrackingSatellite, eventPackage );
		}
	}

	public void ApplyDamage( float damage )
	{
		if( _bIsInSafeMode || !_bIsAlive )
		{
			return;
		}

		if( !DEBUG_Globals.ActiveProfile._bInfiniteHealth )
		{
			_currentHealth -= damage;
		}
				
		if( _currentHealth <= 0.0f )
		{
			_currentHealth = 0.0f;
			_bIsAlive = false;
			_satelliteComponent?.Kill();
			UpdateSatelliteComponentOutline();
			UpdateOrbitVisualsActiveState();
			StopTrackingSatellite();
		}
	}

	void OnDestroy()
	{
		StopTrackingSatellite();
	}

#region Visuals Interface
	
	public void ToggleActivePositioningVisuals( bool bActive )
	{
		if( _orbitalProjectionCircleVisuals )
		{
			_orbitalProjectionCircleVisuals.gameObject.SetActive( bActive );
		}
	}

	public void ToggleHoverHighlightVisuals( bool bActive )
	{
		if( _satelliteComponent )
		{
			_satelliteComponent.Highlight( bActive );
		}

		_bSatelliteIsHighlightedByCursor = bActive;
		UpdateOrbitVisualsActiveState();
	}

	public void ToggleSafeMode( bool bActive )
	{
		if( bActive == _bIsInSafeMode )
		{
			return;
		}

		_bIsInSafeMode = bActive;
		if( !bActive )
		{
			_safeModeProgress = 0.0f;
		}
		
		if( _satelliteComponent )
		{
			_satelliteComponent.ToggleSafeMode( DEBUG_Globals.ActiveProfile._satelliteSafeModeTime, bActive );
			UpdateSatelliteComponentOutline();
		}

		UpdateOrbitVisualsActiveState();

		if( bActive )
		{
			StopTrackingSatellite();
		}
		else
		{
			StartTrackingSatellite();
		}
	}

	public void UpdateSatelliteComponentOutline()
	{
		_satelliteComponent?.UpdateOutline(
			_bOrbitIsActive,
			_bIsAlive,
			_bLayerIsSelected,
			_bLayerIsHovered,
			_bIsInSafeMode,
			_bSatelliteIsHighlightedByCursor );
	}

	public void UpdateOrbitVisualsActiveState()
	{
		if( _orbitalOuterCircleVisuals )
		{
			_orbitalOuterCircleVisuals.gameObject.SetActive( _bIsAlive && 
			                                                 (_bLayerIsHovered || _bLayerIsSelected ||
			                                                  _bSatelliteIsHighlightedByCursor ) );
		}
	}
	
#endregion

#region Callbacks

	private void OnGlobalEvent_UIActivateSensorView( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType inType && _satelliteData )
		{
			_bLayerIsSelected = _satelliteData._sensorType == inType;
			UpdateSatelliteComponentOutline();
			UpdateOrbitVisualsActiveState();
		}
	}
	
	private void OnGlobalEvent_UIDeactivateSensorView( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType inType && _satelliteData && _satelliteData._sensorType == inType )
		{
			_bLayerIsSelected = false;
			UpdateSatelliteComponentOutline();
			UpdateOrbitVisualsActiveState();
		}
	}
	
	private void OnGlobalEvent_UIHoverSensorView( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType inType && _satelliteData )
		{
			_bLayerIsHovered = _satelliteData._sensorType == inType;
			UpdateSatelliteComponentOutline();
			UpdateOrbitVisualsActiveState();
		}
	}
	
	private void OnGlobalEvent_UIStopHoverSensorView( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType inType && _satelliteData && _satelliteData._sensorType == inType )
		{
			_bLayerIsHovered = false;
			UpdateSatelliteComponentOutline();
			UpdateOrbitVisualsActiveState();
		}
	}

#endregion

#region MoneBehaviour
	
	void Update()
	{
		// Rotate satellites
		if( _bOrbitIsActive && !Mathf.Approximately(_orbitTime, 0.0f) )
		{
			float frameRotationDegrees = Time.deltaTime * 360.0f / _orbitTime;
			_satelliteTransformRoot.Rotate( new Vector3( 0.0f, -frameRotationDegrees, 0.0f ), Space.Self );
		}
		
		if( _bIsAlive )
		{
			// Update safe mode
			if( _bIsInSafeMode )
			{
				_safeModeProgress += Time.deltaTime / DEBUG_Globals.ActiveProfile._satelliteSafeModeTime;
				if( _safeModeProgress >= 1.0f )
				{
					ToggleSafeMode( false );
				}
			}
			
			// Handle incoming damage
			if( DEBUG_Globals.ActiveProfile._bDebugStormIsActive )
			{
				ApplyDamage( DEBUG_Globals.ActiveProfile._debugStormDamagePerSecond * Time.deltaTime );
			}
		}
	}

	void OnEnable()
	{
		EventBus.StartListening( EventBus.EEventType.UI_ActivateSensorView, OnGlobalEvent_UIActivateSensorView );
		EventBus.StartListening( EventBus.EEventType.UI_DeactivateSensorView, OnGlobalEvent_UIDeactivateSensorView );
		EventBus.StartListening( EventBus.EEventType.UI_HoverSensorView, OnGlobalEvent_UIHoverSensorView );
		EventBus.StartListening( EventBus.EEventType.UI_StopHoverSensorView, OnGlobalEvent_UIStopHoverSensorView );
	}
	void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.UI_ActivateSensorView, OnGlobalEvent_UIActivateSensorView );
		EventBus.StopListening( EventBus.EEventType.UI_DeactivateSensorView, OnGlobalEvent_UIDeactivateSensorView );
		EventBus.StopListening( EventBus.EEventType.UI_HoverSensorView, OnGlobalEvent_UIHoverSensorView );
		EventBus.StopListening( EventBus.EEventType.UI_StopHoverSensorView, OnGlobalEvent_UIStopHoverSensorView );
	}

#endregion
	
}
