using System.Collections.Generic;
using MM;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputModeManager : StandaloneSingletonBase<InputModeManager>
{
	public enum GameplayInputMode
	{
		INVALID = 0,
		
		Selection,
		LaunchingSatellite,
		UIPopup,
	}

	public bool _bQueuedSatelliteForLaunch = false;
	public HashSet<GameObject> _numActiveUIBlockingObjects = new HashSet<GameObject>();
	public static GameplayInputMode Mode => Instance ? Instance.GetMode() : GameplayInputMode.INVALID;
	public GameplayInputMode GetMode()
	{
		if( _numActiveUIBlockingObjects.Count > 0 )
		{
			return GameplayInputMode.UIPopup;
		}
		
		if( _bQueuedSatelliteForLaunch )
		{
			return GameplayInputMode.LaunchingSatellite;
		}

		return GameplayInputMode.Selection;
	}

	public void AddUIBlockingObject( GameObject inObj )
	{
		if( inObj )
		{
			_numActiveUIBlockingObjects.Add( inObj );
		}
		UpdatePauseTime();
	}
	
	public void RemoveUIBlockingObject( GameObject inObj )
	{
		if( inObj )
		{
			_numActiveUIBlockingObjects.Remove( inObj );
		}
		UpdatePauseTime();
	}

	public void UpdatePauseTime()
	{
		if( _numActiveUIBlockingObjects.Count > 0 )
		{
			EventBus.Invoke( this, EventBus.EEventType.UI_PauseTime );
		}
		else
		{
			EventBus.Invoke( this, EventBus.EEventType.UI_UnpauseTime );
		}
	}

	public void ToggleSatelliteLaunch( bool bOn )
	{
		_bQueuedSatelliteForLaunch = bOn;
	}
	
}
