using System;
using System.Collections.Generic;
using MM;
using NaughtyAttributes;
using UnityEngine;

/**
 * Component of GameManager for orchestration
 */
[System.Serializable]
public class StormTimer
{

	public enum EStormState
	{
		Disabled,
		Calm,
		Warning,
		Active,
	}
	
	[Header( "State Values (Runtime)" )]
	[SerializeField, ReadOnly] private float _stateTimeRemaining = 0.0f;
	[SerializeField, ReadOnly] private EStormState _currentState = EStormState.Disabled;
	
	public float StateTimeRemaining => _stateTimeRemaining;
	public bool _bStateTimerActive = false;
	public bool BStormIsActive => _currentState == EStormState.Active;
	public EStormState CurrentState => _currentState;
	
	/*
	 * I'm also applying damage here. Need to apply damage 1ce to each satellite in _any_ damage zone - does not stack.
	 * Options for where this goes:
	 * 1 Planet (has the zones and satellites, but not the storm state)
	 * 2 GameManager (has the storm state, and indirect access to zones and satellite)
	 * 3 Satellite (has its own data but would need to get the zone and storm state separately)
	 * 4 Here (needs to track the zones and satellites separately, but handles other storm-related code)
	 * I decided on 4. I'm not too sure, but it's a game jam!
	 */
	[Header( "Storm Values (Runtime)")]
	[SerializeField] public SO_StormTimings _stormTimings;
	[SerializeField, ReadOnly] private List<Vector3> _stormCentres = new List<Vector3>();
	[SerializeField] private float _stormZoneRadius = 1.0f;
	private IndexedHashSet<Satellite3D> _trackedSatellites = new IndexedHashSet<Satellite3D>();
	private IndexedHashSet<Satellite3D> _currentlyDamagingSatellites = new IndexedHashSet<Satellite3D>();
	[SerializeField] private float _stormDamagePerSecond = 0.1f;

#region Callbacks

	private void OnGlobalEvent_StartTrackingSatellite( EventBus.EventContext context, object obj = null )
	{
		if( obj is (SO_Satellite satelliteData, Satellite3D satellite) )
		{
			if( satellite )
			{
				_trackedSatellites.Add( satellite );
			}
		}
	}

	private void OnGlobalEvent_StopTrackingSatellite( EventBus.EventContext context, object obj = null )
	{
		if( obj is (SO_Satellite satelliteData, Satellite3D satellite) )
		{
			if( satellite )
			{
				_trackedSatellites.Remove( satellite );
				_currentlyDamagingSatellites.Remove( satellite );
			}
		}
	}

#endregion

#region Interface
	
	public void Initialise()
	{
		EventBus.StartListening( EventBus.EEventType.StartTrackingSatellite, OnGlobalEvent_StartTrackingSatellite );
		EventBus.StartListening( EventBus.EEventType.StopTrackingSatellite, OnGlobalEvent_StopTrackingSatellite );
	}

	public void CleanUp()
	{
		EventBus.StopListening( EventBus.EEventType.StartTrackingSatellite, OnGlobalEvent_StartTrackingSatellite );
		EventBus.StopListening( EventBus.EEventType.StopTrackingSatellite, OnGlobalEvent_StopTrackingSatellite );
	}

	// Use this to test timers without needing storm function values
	public void Reset( SO_StormTimings stormTimings )
	{
		Clear();
		
		_stormTimings = stormTimings;
		_stormDamagePerSecond = DEBUG_Globals.ActiveProfile._debugStormDamagePerSecond;
	}
	
	public void Reset( SO_StormTimings stormTimings, List<Transform> stormCentres, float stormRadius, float stormDamagePerSecond )
	{
		List<Vector3> stormCentrePositions = new List<Vector3>( stormCentres.Count );
		for( int i = 0; i < stormCentres.Count; ++i )
		{
			stormCentrePositions.Add( stormCentres[ i ].position );
		}
		Reset( stormTimings, stormCentrePositions, stormRadius, stormDamagePerSecond );
	}
	
	public void Reset( SO_StormTimings stormTimings, List<Vector3> stormCentres, float stormRadius, float stormDamagePerSecond )
	{
		Clear();
		
		_stormCentres = stormCentres;
		_stormZoneRadius = stormRadius;
		_stormTimings = stormTimings;
		_stormDamagePerSecond = stormDamagePerSecond;
	}

	public void Reset( Planet planet )
	{
		if( planet?.PlanetConfig == null )
		{
			Clear();
			return;
		}

		Reset( planet.PlanetConfig._stormTimings,
			planet._planetStormZones,
			planet.PlanetConfig._stormZoneRadius,
			planet.PlanetConfig._stormDamagePerSecond );
	}

	public void Clear()
	{
		Deactivate();
		_stormTimings = null;
		_stormCentres.Clear();
		_trackedSatellites.Clear();
		_currentlyDamagingSatellites.Clear();
	}

	public void Start( bool bForceReset = false )
	{
		if( bForceReset || _currentState == EStormState.Disabled )
		{
			ChangeState( EStormState.Calm );
		}
	}

	[Button("Skip State (Runtime)")]
	public void SkipState()
	{
		if( _bStateTimerActive )
		{
			// Queue transition from the timer update
			_stateTimeRemaining = -1.0f;
		}
		else
		{
			switch( _currentState )
			{
				// Manually assigned state transitions for debug tool
				case EStormState.Calm:
					ChangeState( EStormState.Warning );
					break;
				case EStormState.Warning:
					ChangeState( EStormState.Active );
					break;
				case EStormState.Active:
					ChangeState( EStormState.Calm );
					break;
				case EStormState.Disabled:
					ChangeState( EStormState.Calm );
					break;
			}
		}
	}

	public void Deactivate()
	{
		ChangeState( EStormState.Disabled );
	}

	// Returns true if timed out current state
	public void Update( float deltaTime )
	{
		if( Mathf.Approximately( deltaTime, 0.0f ) )
		{
			return;
		}

		if( _currentState != EStormState.Disabled &&
		    _stormTimings &&
		    _bStateTimerActive )
		{
			_stateTimeRemaining -= deltaTime;
			StateUpdate( deltaTime );
		}
	}

	public void ChangeState( EStormState nextState )
	{
		if( nextState == _currentState )
		{
			return;
		}

		StateExit();
		_currentState = nextState;
		StateEnter();
	}

	public void DrawGizmos()
	{
		Color stormColor = Color.white;
		switch( _currentState )
		{
			case EStormState.Calm:
				stormColor = Color.green;
				break;
			case EStormState.Warning:
				stormColor = Color.yellow;
				break;
			case EStormState.Active:
				stormColor = Color.red;
				break;
			case EStormState.Disabled:
				stormColor = Color.grey;
				break;
		}
		float satelliteRadius = DEBUG_Globals.ActiveProfile._satelliteRadius;
		
		Gizmos.color = stormColor;
		for( int i = 0; i < _stormCentres.Count; ++i )
		{
			Gizmos.DrawWireSphere( _stormCentres[i], _stormZoneRadius );
		}
		Gizmos.color = Color.cyan;
		for( int i = 0; i < _trackedSatellites.Count; ++i )
		{
			if( _currentlyDamagingSatellites.Contains( _trackedSatellites[i] ) )
			{
				Gizmos.DrawSphere( _trackedSatellites[i].transform.position, satelliteRadius );
			}
			else
			{
				Gizmos.DrawWireSphere( _trackedSatellites[i].transform.position, satelliteRadius );
			}
		}
	}

#endregion
	
#region States

	private void StateUpdate( float deltaTime )
	{
		EStormState nextState = _currentState;

		// Update timer
		if( _bStateTimerActive && _stateTimeRemaining < 0.0f )
		{
			switch( _currentState )
			{
				case EStormState.Calm:
					nextState = EStormState.Warning;
					break;
				case EStormState.Warning:
					nextState = EStormState.Active;
					break;
				case EStormState.Active:
					nextState = EStormState.Calm;
					break;
			}
		}

		if( nextState != _currentState )
		{
			ChangeState( nextState );
		}
		else if( _currentState == EStormState.Active )
		{
			// Update storm damage for frames during the storm
			ApplyStormDamage( deltaTime );
		}
	}

	private void StateExit()
	{
		switch( _currentState )
		{
			case EStormState.Calm:
				break;
			case EStormState.Warning:
				break;
			case EStormState.Active:
				_currentlyDamagingSatellites.Clear();
				EventBus.Invoke( this, EventBus.EEventType.StormEnded );
				break;
			case EStormState.Disabled:
				break;
		}
		// TODO I could make this a generic stateexit invoke passing in the old state, but I think this reads clearer
	}
	
	private void StateEnter()
	{
		if( !_stormTimings || _currentState == EStormState.Disabled )
		{
			_bStateTimerActive = false;
		}
		else
		{
			_bStateTimerActive = true;
			switch( _currentState )
			{
				case EStormState.Calm:
					_stateTimeRemaining = _stormTimings._calmTime;
					break;
				case EStormState.Warning:
					_stateTimeRemaining = _stormTimings._warningTime;
					EventBus.Invoke( this, EventBus.EEventType.StormWarningStarted, _stateTimeRemaining );
					break;
				case EStormState.Active:
					_stateTimeRemaining = _stormTimings._stormTime;
					EventBus.Invoke( this, EventBus.EEventType.StormStarted, _stateTimeRemaining );
					break;
			}
		}
		// TODO I could make this a generic stateenter invoke passing in the new state, but I think this reads clearer
	}

#endregion

#region Storm

	private void ApplyStormDamage( float deltaTime )
	{
		float satelliteRadius = DEBUG_Globals.ActiveProfile._satelliteRadius;

		IndexedHashSet<Satellite3D> thisFrameDamagingSatellites = new IndexedHashSet<Satellite3D>();
		for( int i = 0; i < _stormCentres.Count; ++i )
		{
			for( int j = 0; j < _trackedSatellites.Count; ++j )
			{
				SatelliteOrbit orbit = _trackedSatellites[j].Orbit;
				if( orbit &&
				    !orbit.BIsInSafeMode &&
				    orbit.BIsAlive &&
				    (_stormCentres[i] - _trackedSatellites[j].transform.position).magnitude <
				    (_stormZoneRadius + satelliteRadius) )
				{
					thisFrameDamagingSatellites.Add( _trackedSatellites[j] );
				}
			}
		}

		// Process enter and exit events and update persistent list
		for( int i = _currentlyDamagingSatellites.Count - 1; i >= 0; --i )
		{
			if( !thisFrameDamagingSatellites.Contains( _currentlyDamagingSatellites[i] ) )
			{
				_currentlyDamagingSatellites[i].DeactivateStormDamage();
				_currentlyDamagingSatellites.RemoveAt( i );
			}
		}

		for( int i = 0; i < thisFrameDamagingSatellites.Count; ++i )
		{
			if( _currentlyDamagingSatellites.Add( thisFrameDamagingSatellites[i] ) )
			{
				thisFrameDamagingSatellites[i].ActivateStormDamage();
			}
		}

		// Process update events
		for( int i = 0; i < _currentlyDamagingSatellites.Count; ++i )
		{
			_currentlyDamagingSatellites[i].Orbit?.ApplyDamage( deltaTime * _stormDamagePerSecond );
		}
	}

#endregion

}
