using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

/**
 * Singleton class handles orbital creation and bookkeeping, satellite deployment, etc. from player inputs
 */
public class SatelliteManager : MonoBehaviour
{

	[Header("Project data")]
	[SerializeField] private Object _orbitPrefab;

	[Header("Scene components")]
	[SerializeField] private Transform _orbitCentreAnchor;
	[SerializeField] private Camera _camera;
	
	[Header("Prefab components")]
	[SerializeField] private SphereCollider _orbitClickableCollider;
	
	[Header("Behaviour")]
	[SerializeField, Tooltip("Rotations Per Second")] private float _orbitSpeed = 1.0f;
	[SerializeField, Tooltip("Elevation above the clicked collider")] private float _orbitRadiusOffset = 0.3f;
	[SerializeField] private bool _bAutomaticallyLaunchOnceOnOrbitRelease = true;

	private readonly List<SatelliteOrbit> _orbits = new List<SatelliteOrbit>();
	private SatelliteOrbit _activeOrbit;
	private SatelliteOrbit _lastOrbit;
	
	private float OrbitRadiusProjection => _orbitClickableCollider ? _orbitClickableCollider.radius : 1.0f;
	private float OrbitRadiusOuter => OrbitRadiusProjection + Mathf.Max( _orbitRadiusOffset, 0.0f );

	[Button( "Debug Reset (Runtime)" )]
	private void Inspector_Reset()
	{
#if UNITY_EDITOR
		ClearSatellites();
#endif
	}

#region MonoBehaviour

	void Awake()
	{
		if( !_camera )
		{
			_camera = Camera.main;
		}
		
		// We'll be keeping the spawned objects in a local hierarchy - don't apply any transformations
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		transform.localScale = Vector3.one;
	}

	void Update()
	{
		if( !_orbitClickableCollider || !_orbitPrefab )
		{
			return;
		}
		
		// Anchor the orbit collider to the planet if necessary
		UpdateOrbitAnchor();
		
		ProcessInputs();
	}

#endregion

#region Interface

	public void ClearSatellites()
	{
		if( _activeOrbit )
		{
			ReleaseActiveOrbit( false );
		}
		
		for( int i = _orbits.Count - 1; i >= 0; --i )
		{
			if( _orbits[i] )
			{
				MM.ComponentUtils.DestroyPlaymodeSafe( _orbits[i].gameObject );
			}
		}
		_orbits.Clear();
	}

#endregion

	private void UpdateOrbitAnchor()
	{
		if( _orbitCentreAnchor )
		{
			_orbitClickableCollider.transform.SetPositionAndRotation(
				_orbitCentreAnchor.position,
				_orbitCentreAnchor.rotation );
		}
	}

	private void ProcessInputs()
	{
		if( Mouse.current.leftButton.wasPressedThisFrame )
		{
			TryCreateOrbit();
		}
		else if( Mouse.current.leftButton.isPressed )
		{
			UpdateActiveOrbitDirection();
		}
		else if( Mouse.current.leftButton.wasReleasedThisFrame )
		{
			ReleaseActiveOrbit( _bAutomaticallyLaunchOnceOnOrbitRelease );
		}

		if( Keyboard.current.spaceKey.wasPressedThisFrame )
		{
			LaunchSatellites( _lastOrbit, 1 ); // TODO: This is temporary - needs a design decision
		}
	}

	private void TryCreateOrbit()
	{
		if( _activeOrbit )
		{
			Debug.LogErrorFormat( "Orbit {0} was not finalised before creating a new one", _activeOrbit.gameObject.name );
		}
		
		// First check raycast
		if( TryGetInputReferencePosition( out Vector3 referencePosition ) )
		{
			GameObject newOrbitObject = GameObject.Instantiate(
				_orbitPrefab,
				transform ) as GameObject;
			_activeOrbit = newOrbitObject?.GetComponent<SatelliteOrbit>();

			if( _activeOrbit )
			{
				_activeOrbit.Initialise(
					_orbitClickableCollider.transform.position,
					_orbitClickableCollider.transform.rotation,
					OrbitRadiusProjection,
					OrbitRadiusOuter,
					_orbitSpeed,
					referencePosition
					);

				_activeOrbit.ToggleActivePositioningVisuals( true );
			}
			else
			{
				Destroy( newOrbitObject );
			}
		}
	}

	private void UpdateActiveOrbitDirection()
	{
		if( _activeOrbit && TryGetInputReferencePosition( out Vector3 newReferencePosition ) )
		{
			_activeOrbit.UpdateSecondReferencePosition( newReferencePosition );
		}
	}

	private void ReleaseActiveOrbit( bool bWithLaunch )
	{
		if( _activeOrbit )
		{
			UpdateActiveOrbitDirection();
			
			// TODO: Trying out a different (simpler) input scheme: Launch one single satellite on orbital creation
			if( bWithLaunch )
			{
				LaunchSatellites( _activeOrbit, 1 );
			}

			_activeOrbit.ToggleActivePositioningVisuals( false );
			_orbits.Add( _activeOrbit );
			_lastOrbit = _activeOrbit;
			_activeOrbit = null;
		}
	}

	private void LaunchSatellites( SatelliteOrbit orbit, int num = 1 )
	{
		if( orbit )
		{
			SO_PlanetConfig.ESensorType sensorType = GameManager.TryGetCurrentSensorType();
			for( int i = 0; i < num; ++i )
			{
				Transform newSatelliteTransform = orbit.LaunchSatellite();
				
				if( newSatelliteTransform )
				{
					(SO_PlanetConfig.ESensorType type, Transform satelliteTransform) eventPackage = new(
						sensorType,
						newSatelliteTransform );
					EventBus.Invoke( this, EventBus.EEventType.LaunchedSatellite, eventPackage );
				}
			}
		}
	}

	private bool TryGetInputReferencePosition( out Vector3 outReferencePosition )
	{
		if( _orbitClickableCollider )
		{
			Ray ray = _camera.ScreenPointToRay( Mouse.current.position.ReadValue() );
			if( _orbitClickableCollider.Raycast( ray, out RaycastHit hitInfo, Mathf.Infinity ) )
			{
				outReferencePosition = hitInfo.point;
				return true;
			}
		}
		
		outReferencePosition = Vector3.zero;
		return false;
	}
}