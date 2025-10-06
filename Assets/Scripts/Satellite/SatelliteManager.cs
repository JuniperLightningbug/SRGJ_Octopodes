using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/**
 * Singleton class handles orbital creation and bookkeeping, satellite deployment, etc. from player inputs
 */
public class SatelliteManager : MM.StandaloneSingletonBase<SatelliteManager>
{
	protected override bool BPersistent
	{
		get { return false; }
	}

	[Header("Project data")]
	[SerializeField] private Object _orbitPrefab;

	[Header("Scene data")]
	[SerializeField] private Transform _orbitCentreTransform;
	[SerializeField] private Collider _orbitClickableCollider;
	[SerializeField] private Camera _camera;
	
	[Header("Behaviour")]
	[SerializeField] private bool _bMeasureOrbitRadiusFromPlanetSurface = false;
	[SerializeField] private float _orbitRadius = 1.0f;
	[SerializeField, Tooltip("Rotations Per Second")] private float _orbitSpeed = 1.0f;

	private List<SatelliteOrbit> _orbits = new List<SatelliteOrbit>();
	private SatelliteOrbit _newOrbit;
	private SatelliteOrbit _lastOrbit;

	protected override void Initialise()
	{
		if( !_camera )
		{
			_camera = Camera.main;
		}

		if( !_orbitClickableCollider && _orbitCentreTransform )
		{
			_orbitClickableCollider = _orbitCentreTransform.gameObject.GetComponent<Collider>();
			Debug.LogWarningFormat( "Collider isn't assigned in inspector - we're using {0} on {1} instead",
				_orbitClickableCollider.name, _orbitClickableCollider.gameObject.name );
		}
		
		// We'll be keeping the spawned objects in a local hierarchy - don't apply any transformations
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		transform.localScale = Vector3.one;
	}

	void Update()
	{
		if( !_orbitPrefab || !_orbitCentreTransform )
		{
			return;
		}
		
		if( Mouse.current.leftButton.wasPressedThisFrame )
		{
			TryCreateOrbit();
		}
		else if( Mouse.current.leftButton.isPressed )
		{
			TryUpdateOrbitDirection();
		}
		else if( Mouse.current.leftButton.wasReleasedThisFrame )
		{
			TryReleaseOrbit();
		}

		if( Keyboard.current.spaceKey.wasPressedThisFrame )
		{
			TryLaunchSatellites( 1 ); // TODO: This is temporary - needs a design decision
		}
	}

	private void TryCreateOrbit()
	{
		if( !_orbitPrefab || !_orbitCentreTransform )
		{
			Debug.LogError( "Orbit scene reference not initialised correctly. Ignoring orbit prefab instantiation." );
			return;
		}
		
		if( _newOrbit )
		{
			Debug.LogErrorFormat( "Orbit {0} was not finalised before creating a new one", _newOrbit.gameObject.name );
			Destroy( _newOrbit.gameObject );
		}
		
		// First check raycast
		if( TryGetInputReferencePosition( out Vector3 referencePosition ) )
		{
			GameObject newOrbitObject = GameObject.Instantiate(
				_orbitPrefab,
				transform ) as GameObject;
			_newOrbit = newOrbitObject?.GetComponent<SatelliteOrbit>();

			if( _newOrbit )
			{
				_newOrbit.Initialise(
					_orbitCentreTransform.position,
					_orbitCentreTransform.rotation,
					_orbitRadius,
					_bMeasureOrbitRadiusFromPlanetSurface,
					_orbitSpeed,
					referencePosition
					);
			}
			else
			{
				Destroy( newOrbitObject );
			}
		}
	}

	private void TryUpdateOrbitDirection()
	{
		if( _newOrbit && TryGetInputReferencePosition( out Vector3 newReferencePosition ) )
		{
			_newOrbit.UpdateSecondReferencePosition( newReferencePosition );
		}
	}

	private void TryReleaseOrbit()
	{
		if( _newOrbit )
		{
			TryUpdateOrbitDirection();

			_orbits.Add( _newOrbit );
			_lastOrbit = _newOrbit;
			_newOrbit = null;
		}
	}

	private void TryLaunchSatellites( int num )
	{
		if( _lastOrbit )
		{
			for( int i = 0; i < num; ++i )
			{
				Transform satelliteTransform = _lastOrbit.LaunchSatellite();
				if( satelliteTransform && PlanetManager.Instance?.ActivePlanet )
				{
					PlanetManager.Instance.ActivePlanet.StartTrackingSatellite( satelliteTransform );
					// TODO: Make this better. Events, higher singleton. Cache the planet. Something.
					// TODO: Need to remove satellites as well.
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