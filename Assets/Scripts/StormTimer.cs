using System;
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

	public SO_StormTimings _stormTimings;
	
	[Header( "Runtime" )]
	[SerializeField, ReadOnly] private float _stateTimeRemaining = 0.0f;
	[SerializeField, ReadOnly] private EStormState _currentState = EStormState.Disabled;
	[SerializeField, ReadOnly] private EStormState _previousState = EStormState.Disabled;
	
	public float StateTimeRemaining => _stateTimeRemaining;
	public bool BStormIsActive => _currentState == EStormState.Active;
	public EStormState CurrentState => _currentState;
	public EStormState PreviousState => _previousState;

	public void Reset( SO_StormTimings stormTimings )
	{
		if( stormTimings != null )
		{
			_stormTimings = stormTimings;
			ChangeState( EStormState.Calm );
		}
	}

	public void Deactivate()
	{
		ChangeState( EStormState.Disabled );
	}

	// Returns true if timed out current state
	public bool UpdateTimer( float deltaTime )
	{
		if( _currentState == EStormState.Disabled || !_stormTimings )
		{
			return false;
		}
		
		_stateTimeRemaining -= deltaTime;
		
		// Check timer state
		EStormState nextState = _currentState;
		if( _stateTimeRemaining < 0.0f )
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
			return ChangeState( nextState );
		}

		return false;
	}

	private bool ChangeState( EStormState nextState )
	{
		if( nextState == _currentState || !_stormTimings )
		{
			return false;
		}
		
		switch( nextState )
		{
			case EStormState.Calm:
			{
				_stateTimeRemaining = _stormTimings._calmTime;
				break;
			}
			case EStormState.Warning:
			{
				_stateTimeRemaining = _stormTimings._warningTime;
				break;
			}
			case EStormState.Active:
			{
				_stateTimeRemaining = _stormTimings._stormTime;
				break;
			}
		}
		_previousState = _currentState;
		_currentState = nextState;
		return true;
	}
	
}
