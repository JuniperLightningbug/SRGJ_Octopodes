using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

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

	public SO_PlanetConfig.ESensorType _queuedSatelliteType = SO_PlanetConfig.ESensorType.INVALID;
	public bool BHasQueuedSatellite => _queuedSatelliteType != SO_PlanetConfig.ESensorType.INVALID;
	
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

	public void QueueSatellite( SO_PlanetConfig.ESensorType inType )
	{
		if( inType == SO_PlanetConfig.ESensorType.INVALID )
		{
			DequeueSatellite();
			return;
		}
		
		if( BHasQueuedSatellite )
		{
			Debug.LogErrorFormat(
				"Queueing sensor of type {0} but another of type {1} already exists! Replacing with the new type.",
				_queuedSatelliteType.ToString(), inType.ToString() );
		}

		if( _activeOrbit != null )
		{
			Debug.LogErrorFormat( "Trying to queue new sensor without deploying previous orbit. Skipping." );
			return;
		}
		
		_queuedSatelliteType = inType;
	}

	public void DequeueSatellite( SO_PlanetConfig.ESensorType inType )
	{
		if( _queuedSatelliteType == inType )
		{
			DequeueSatellite();
		}
	}

	public void DequeueSatellite()
	{
		_queuedSatelliteType = SO_PlanetConfig.ESensorType.INVALID;
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
		if( BHasQueuedSatellite )
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

			if( _activeOrbit && BHasQueuedSatellite )
			{
				_activeOrbit.Initialise(
					_queuedSatelliteType,
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
				LaunchSatellite( _activeOrbit );
			}

			_activeOrbit.ToggleActivePositioningVisuals( false );
			_orbits.Add( _activeOrbit );
			_lastOrbit = _activeOrbit;
			DequeueSatellite( _queuedSatelliteType );
			_activeOrbit = null;
		}
	}

	private void LaunchSatellite( SatelliteOrbit orbit )
	{
		if( orbit && BHasQueuedSatellite )
		{
			orbit.LaunchSatellite();
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