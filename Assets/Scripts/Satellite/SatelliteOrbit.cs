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

	[SerializeField] private Object _satellitePrefab;

	[ReadOnly, SerializeField] public Transform _satelliteTransform;

	[SerializeField, Tooltip("Set by instantiating manager")] private float _projectionRadius = 1.0f;
	[SerializeField, Tooltip("Set by instantiating manager")] private float _orbitRadius = 1.0f;
	[SerializeField, Tooltip("Set by instantiating manager (Rotations Per Second)")] private float _orbitSpeed = 1.0f;

	[SerializeField] private SO_Satellite _satelliteData = null;

	private bool _bOrbitIsActive = false;
	private Vector3 _directionReference0 = Vector3.right;
	private Vector3 _directionReference1 = Vector3.forward;
	private Vector3 _cachedTransformUp = Vector3.up;

	[Header( "Health Config" )]
	[SerializeField]
	private float _safeModeTime = 3.0f;
	
	[Header("Runtime Health Data")]
	[SerializeField]
	public bool _bIsAlive = true;
	[SerializeField]
	public bool _bIsInSafeMode = false;
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
		float orbitSpeed,
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
		_orbitSpeed = orbitSpeed;

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

		Vector3 startDirection = transform.InverseTransformPoint( startReferencePosition ).normalized;
		_directionReference0 = startDirection;
		_directionReference1 = startDirection;
		
		UpdateOrbitRotationToReferencePositions();
	}

	void Update()
	{
		// Rotate satellites
		if( _bOrbitIsActive )
		{
			float frameRotationDegrees = _orbitSpeed * Time.deltaTime * 360.0f;
			_satelliteTransformRoot.Rotate( new Vector3( 0.0f, -frameRotationDegrees, 0.0f ), Space.Self );
		}
		
		if( _bIsAlive )
		{
			// Update safe mode
			if( _bIsInSafeMode )
			{
				_safeModeProgress += Time.deltaTime / _safeModeTime;
				if( _safeModeProgress >= 1.0f )
				{
					_bIsInSafeMode = false;
					_safeModeProgress = 0.0f;
				}
			}
			
			// Handle incoming damage
			if( DEBUG_Globals.ActiveProfile._bDebugStormIsActive && !_bIsInSafeMode )
			{
				_currentHealth -= DEBUG_Globals.ActiveProfile._debugStormDamagePerSecond * Time.deltaTime;
				
				if( _currentHealth <= 0.0f )
				{
					_currentHealth = 0.0f;
					_bIsAlive = false;
				}
			}
		}
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

	public Transform LaunchSatellite()
	{
		if( _satellitePrefab && !_satelliteTransform )
		{
			Transform satelliteRoot = _satelliteTransformRoot ? _satelliteTransformRoot : transform;
			GameObject newSatelliteObj = Instantiate( _satellitePrefab, satelliteRoot ) as GameObject;
			if( newSatelliteObj )
			{
				_satelliteTransform = newSatelliteObj.transform;
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
				
				Satellite3D satellite3d = newSatelliteObj.GetComponent<Satellite3D>();
				if( satellite3d )
				{
					satellite3d.Initialise( _satelliteData );
				}
				
				_bOrbitIsActive = true;
				
				(SO_Satellite satellite, Transform satelliteTransform) eventPackage = new(
					_satelliteData,
					_satelliteTransform );
				EventBus.Invoke( this, EventBus.EEventType.LaunchedSatellite, eventPackage );

				if( _satelliteLaunchProjectionIndicator )
				{
					_satelliteLaunchProjectionIndicator.gameObject.SetActive( false );
				}
				if( _satelliteLaunchPositionIndicator )
				{
					_satelliteLaunchPositionIndicator.gameObject.SetActive( false );
				}
				
				return _satelliteTransform;
			}
		}

		return null;
	}

	private void StopTrackingSatellite()
	{
		if( _satelliteTransform )
		{
			(SO_Satellite satelliteData, Transform satelliteTransform) eventPackage = new(
				_satelliteData,
				_satelliteTransform );
			EventBus.Invoke( this, EventBus.EEventType.StopTrackingSatellite, eventPackage );
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
	
#endregion
	
}
