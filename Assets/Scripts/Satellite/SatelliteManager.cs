using System;
using System.Collections.Generic;
using MM;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

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
	[SerializeField, Tooltip("Seconds per rotation")] private float _orbitTime = 10.0f;
	[SerializeField, Tooltip("Elevation above the clicked collider")] private float _orbitRadiusOffset = 0.3f;
	[SerializeField] private bool _bAutomaticallyLaunchOnceOnOrbitRelease = true;

	private readonly List<SatelliteOrbit> _orbits = new List<SatelliteOrbit>();
	private SatelliteOrbit _activeOrbit;
	private SatelliteOrbit _lastOrbit;

	public SO_Satellite _queuedSatellite;
	public bool BHasQueuedSatellite => _queuedSatellite != null;
	
	private float OrbitRadiusProjection => _orbitClickableCollider ? _orbitClickableCollider.radius : 1.0f;
	private float OrbitRadiusOuter => OrbitRadiusProjection + Mathf.Max( _orbitRadiusOffset, 0.0f );

	[SerializeField, ReadOnly] private SatelliteOrbit _currentCursorHoverSatellite;

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
		if( !_orbitClickableCollider || !_orbitPrefab || Mathf.Approximately( Time.deltaTime, 0.0f ) )
		{
			return;
		}

		// Anchor the orbit collider to the planet if necessary
		UpdateOrbitAnchor();
		
		ProcessInputs();
	}
	
	void OnEnable()
	{
		EventBus.StartListening( EventBus.EEventType.UI_QueueSatelliteCard, OnGlobalEvent_UIQueueSatelliteCard );
		EventBus.StartListening( EventBus.EEventType.UI_DequeueSatelliteCard, OnGlobalEvent_UIDequeueSatelliteCard );
	}

	void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.UI_QueueSatelliteCard, OnGlobalEvent_UIQueueSatelliteCard );
		EventBus.StopListening( EventBus.EEventType.UI_DequeueSatelliteCard, OnGlobalEvent_UIDequeueSatelliteCard );
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

	public void QueueSatellite( SO_Satellite inSatellite )
	{
		if( inSatellite._sensorType == SO_PlanetConfig.ESensorType.INVALID )
		{
			DequeueSatellite();
			return;
		}

		if( _activeOrbit != null )
		{
			Debug.LogErrorFormat( "Trying to queue new sensor without deploying previous orbit. Skipping." );
			return;
		}
		
		_queuedSatellite = inSatellite;
		InputModeManager.Instance?.ToggleSatelliteLaunch( true );
	}

	public void DequeueSatellite( SO_Satellite inSatellite )
	{
		if( _queuedSatellite == inSatellite )
		{
			DequeueSatellite();
		}
	}

	public void DequeueSatellite()
	{
		_queuedSatellite = null;
		InputModeManager.Instance?.ToggleSatelliteLaunch( false );
	}

#endregion

#region Callbacks

	private void OnGlobalEvent_UIQueueSatelliteCard( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_Satellite toQueue )
		{
			QueueSatellite( toQueue );
		}
		else
		{
			DequeueSatellite();
		}
	}
	
	private void OnGlobalEvent_UIDequeueSatelliteCard( EventBus.EventContext context, object obj = null )
	{
		DequeueSatellite();
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
		InputModeManager.GameplayInputMode currentMode = InputModeManager.Mode;

		if( currentMode == InputModeManager.GameplayInputMode.Selection )
		{
			SelectionUpdateCursorHover();
			if( Mouse.current.leftButton.wasPressedThisFrame )
			{
				SatelliteSelectionClicked();
			}
		}
		else
		{
			ClearSelectionHover();
			
			if( currentMode == InputModeManager.GameplayInputMode.LaunchingSatellite && BHasQueuedSatellite )
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
	}

	private void SelectionUpdateCursorHover()
	{
		if( _camera )
		{
			// Update cursor target
			Ray ray = _camera.ScreenPointToRay( Mouse.current.position.ReadValue() );
			SatelliteOrbit nextHoverTarget = null;
			if( Physics.Raycast( ray, out RaycastHit hitInfo, Mathf.Infinity ) )
			{
				nextHoverTarget = hitInfo.collider.gameObject.GetComponent<Satellite3D>()?.Orbit;
			}

			if( !nextHoverTarget )
			{
				ClearSelectionHover();
			}
			else if( nextHoverTarget != _currentCursorHoverSatellite)
			{
				ClearSelectionHover();
				StartSelectionHover( nextHoverTarget );
			}
		}
	}

	private void SatelliteSelectionClicked()
	{
		if( _currentCursorHoverSatellite )
		{
			_currentCursorHoverSatellite.ToggleSafeMode( true );
		}
	}

	private void ClearSelectionHover()
	{
		if( _currentCursorHoverSatellite )
		{
			_currentCursorHoverSatellite.ToggleHoverHighlightVisuals( false );
			_currentCursorHoverSatellite = null;
		}
	}

	private void StartSelectionHover(SatelliteOrbit newTarget)
	{
		ClearSelectionHover();
		_currentCursorHoverSatellite = newTarget;
		if( _currentCursorHoverSatellite )
		{
			_currentCursorHoverSatellite.ToggleHoverHighlightVisuals( true );
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
					_queuedSatellite,
					_orbitClickableCollider.transform.position,
					_orbitClickableCollider.transform.rotation,
					OrbitRadiusProjection,
					OrbitRadiusOuter,
					_orbitTime,
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
			DequeueSatellite( _queuedSatellite );
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